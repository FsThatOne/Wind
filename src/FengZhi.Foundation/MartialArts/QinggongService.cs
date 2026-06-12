using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 轻功移动契约 DTO。供战棋系统读取。
/// </summary>
public sealed class MovementContract
{
    /// <summary>当前装备的轻功 ID。未装备时为 null。</summary>
    public string? QinggongId { get; init; }

    /// <summary>移动范围修正（未装备轻功时为 0）。</summary>
    public int MoveRangeBonus { get; init; }

    /// <summary>主动机动效果列表（结构化数据，由战斗系统解释）。</summary>
    public IReadOnlyList<QinggongActiveManeuver> ActiveManeuvers { get; init; } = Array.Empty<QinggongActiveManeuver>();

    /// <summary>默认契约：未装备轻功。</summary>
    public static readonly MovementContract Default = new()
    {
        QinggongId = null,
        MoveRangeBonus = 0,
        ActiveManeuvers = Array.Empty<QinggongActiveManeuver>()
    };
}

/// <summary>
/// 轻功装备操作结果。
/// </summary>
public enum QinggongEquipResult
{
    Success,
    QinggongNotFound,
    QinggongNotLearned,
    SlotLocked
}

/// <summary>
/// 轻功装备操作返回。
/// </summary>
public sealed class QinggongOperationResult
{
    public QinggongEquipResult Result { get; init; }
    public string? LockReason { get; init; }

    public static QinggongOperationResult Ok() => new() { Result = QinggongEquipResult.Success };
    public static QinggongOperationResult NotFound() => new() { Result = QinggongEquipResult.QinggongNotFound };
    public static QinggongOperationResult NotLearned() => new() { Result = QinggongEquipResult.QinggongNotLearned };
    public static QinggongOperationResult Locked(string reason) => new()
    {
        Result = QinggongEquipResult.SlotLocked,
        LockReason = reason
    };
}

/// <summary>
/// 轻功槽锁定查询接口。由叙事系统实现。
/// </summary>
public interface IQinggongLockProvider
{
    bool IsLocked(string characterId);
    string? GetLockReason(string characterId);
}

/// <summary>
/// 默认轻功锁定提供者：从不锁定。
/// </summary>
public sealed class NoQinggongLockProvider : IQinggongLockProvider
{
    public static readonly NoQinggongLockProvider Instance = new();
    public bool IsLocked(string characterId) => false;
    public string? GetLockReason(string characterId) => null;
}

/// <summary>
/// 轻功习得状态查询接口。
/// </summary>
public interface IQinggongProgressionProvider
{
    bool HasLearned(string characterId, string qinggongId);
}

/// <summary>
/// 轻功装备与移动契约服务。
/// </summary>
public sealed class QinggongService
{
    private readonly IDataTable<QinggongDefinition> _qinggongTable;
    private readonly IQinggongProgressionProvider _progressionProvider;
    private readonly IQinggongLockProvider _lockProvider;

    public QinggongService(
        IDataTable<QinggongDefinition> qinggongTable,
        IQinggongProgressionProvider progressionProvider,
        IQinggongLockProvider? lockProvider = null)
    {
        _qinggongTable = qinggongTable ?? throw new ArgumentNullException(nameof(qinggongTable));
        _progressionProvider = progressionProvider ?? throw new ArgumentNullException(nameof(progressionProvider));
        _lockProvider = lockProvider ?? NoQinggongLockProvider.Instance;
    }

    /// <summary>
    /// 尝试装备轻功到角色轻功槽。
    /// </summary>
    public QinggongOperationResult EquipQinggong(CharacterLoadout loadout, string qinggongId)
    {
        if (_lockProvider.IsLocked(loadout.CharacterId))
        {
            var reason = _lockProvider.GetLockReason(loadout.CharacterId) ?? "轻功槽已锁定";
            return QinggongOperationResult.Locked(reason);
        }

        var def = _qinggongTable.Get(qinggongId);
        if (def == null)
            return QinggongOperationResult.NotFound();

        if (!_progressionProvider.HasLearned(loadout.CharacterId, qinggongId))
            return QinggongOperationResult.NotLearned();

        loadout.QinggongSlot = qinggongId;
        return QinggongOperationResult.Ok();
    }

    /// <summary>
    /// 卸下轻功。
    /// </summary>
    public QinggongOperationResult UnequipQinggong(CharacterLoadout loadout)
    {
        if (_lockProvider.IsLocked(loadout.CharacterId))
        {
            var reason = _lockProvider.GetLockReason(loadout.CharacterId) ?? "轻功槽已锁定";
            return QinggongOperationResult.Locked(reason);
        }

        loadout.QinggongSlot = null;
        return QinggongOperationResult.Ok();
    }

    /// <summary>
    /// 获取角色当前移动契约。未装备轻功时返回默认契约。
    /// </summary>
    public MovementContract GetMovementContract(CharacterLoadout loadout)
    {
        if (loadout.QinggongSlot == null)
            return MovementContract.Default;

        var def = _qinggongTable.Get(loadout.QinggongSlot);
        if (def == null)
            return MovementContract.Default;

        return new MovementContract
        {
            QinggongId = def.Id,
            MoveRangeBonus = def.MoveRangeBonus,
            ActiveManeuvers = def.ActiveManeuvers
        };
    }
}
