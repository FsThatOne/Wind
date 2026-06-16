using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 武学配置槽位索引（0~5 为招式槽）。
/// </summary>
public static class LoadoutSlots
{
    public const int MoveSlotCount = 6;
}

/// <summary>
/// 角色武学配置（运行时/存档状态）。6 个招式槽 + 1 心法槽 + 1 轻功槽。
/// 适用于主角和同伴，结构完全一致。
/// </summary>
public sealed class CharacterLoadout
{
    public string CharacterId { get; }
    private readonly string?[] _moveSlots = new string?[LoadoutSlots.MoveSlotCount];

    public string? XinfaSlot { get; internal set; }
    public string? QinggongSlot { get; internal set; }

    public CharacterLoadout(string characterId)
    {
        CharacterId = characterId;
    }

    /// <summary>获取指定招式槽内容（0~5）。</summary>
    public string? GetMoveSlot(int index)
    {
        ValidateSlotIndex(index);
        return _moveSlots[index];
    }

    internal void SetMoveSlot(int index, string? moveId)
    {
        ValidateSlotIndex(index);
        _moveSlots[index] = moveId;
    }

    /// <summary>获取当前装备的所有招式 ID（稳定顺序，null 槽位跳过）。</summary>
    public IReadOnlyList<string> GetEquippedMoves()
    {
        var result = new List<string>(LoadoutSlots.MoveSlotCount);
        for (int i = 0; i < LoadoutSlots.MoveSlotCount; i++)
        {
            if (_moveSlots[i] != null)
                result.Add(_moveSlots[i]!);
        }
        return result;
    }

    /// <summary>获取所有招式槽快照（含 null）。</summary>
    public IReadOnlyList<string?> GetAllMoveSlots() => _moveSlots.ToArray();

    private static void ValidateSlotIndex(int index)
    {
        if (index < 0 || index >= LoadoutSlots.MoveSlotCount)
            throw new ArgumentOutOfRangeException(nameof(index), $"招式槽索引必须为 0~{LoadoutSlots.MoveSlotCount - 1}");
    }
}

/// <summary>
/// 槽位锁定查询接口。由叙事/队伍状态系统实现。
/// </summary>
public interface ISlotLockProvider
{
    /// <summary>查询指定角色指定招式槽是否被锁定。</summary>
    bool IsLocked(string characterId, int slotIndex);

    /// <summary>获取锁定原因文本。</summary>
    string? GetLockReason(string characterId, int slotIndex);
}

/// <summary>
/// 默认锁定提供者：无锁定。
/// </summary>
public sealed class NoLockProvider : ISlotLockProvider
{
    public static readonly NoLockProvider Instance = new();
    public bool IsLocked(string characterId, int slotIndex) => false;
    public string? GetLockReason(string characterId, int slotIndex) => null;
}

/// <summary>
/// 角色武学偏好标签。
/// </summary>
public sealed class CharacterPreference
{
    public string CharacterId { get; init; } = string.Empty;
    public List<string> PreferredTags { get; init; } = new();
    public List<string> DislikedTags { get; init; } = new();
}

/// <summary>
/// 体系覆盖检查结果。
/// </summary>
public sealed class CoverageWarning
{
    public IReadOnlyList<MoveType> MissingTypes { get; init; } = Array.Empty<MoveType>();
    public bool HasWarning => MissingTypes.Count > 0;
}

/// <summary>
/// 装备操作结果。
/// </summary>
public enum LoadoutResult
{
    Success,
    MoveNotLearned,
    SlotLocked,
    MoveNotFound,
    DuplicateMove
}

/// <summary>
/// 装备操作详细返回。
/// </summary>
public sealed class LoadoutOperationResult
{
    public LoadoutResult Result { get; init; }
    public string? LockReason { get; init; }
    public string? PreferenceReminder { get; init; }

    public static LoadoutOperationResult Ok(string? reminder = null) => new()
    {
        Result = LoadoutResult.Success,
        PreferenceReminder = reminder
    };

    public static LoadoutOperationResult Locked(string reason) => new()
    {
        Result = LoadoutResult.SlotLocked,
        LockReason = reason
    };

    public static LoadoutOperationResult Fail(LoadoutResult reason) => new() { Result = reason };
}

/// <summary>
/// 武学配置槽服务。管理装卸、体系覆盖检查、锁定和偏好提醒。
/// </summary>
public sealed class LoadoutService
{
    private readonly IDataTable<MoveDefinition> _moveTable;
    private readonly MoveProgressionService _progressionService;
    private readonly ISlotLockProvider _lockProvider;
    private readonly Dictionary<string, CharacterPreference> _preferences = new(StringComparer.Ordinal);

    public LoadoutService(
        IDataTable<MoveDefinition> moveTable,
        MoveProgressionService progressionService,
        ISlotLockProvider? lockProvider = null)
    {
        _moveTable = moveTable ?? throw new ArgumentNullException(nameof(moveTable));
        _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        _lockProvider = lockProvider ?? NoLockProvider.Instance;
    }

    /// <summary>注册角色偏好。</summary>
    public void RegisterPreference(CharacterPreference preference)
    {
        _preferences[preference.CharacterId] = preference;
    }

    /// <summary>
    /// 装备招式到指定槽位。
    /// </summary>
    public LoadoutOperationResult EquipMove(CharacterLoadout loadout, int slotIndex, string moveId)
    {
        // 锁定检查
        if (_lockProvider.IsLocked(loadout.CharacterId, slotIndex))
        {
            var reason = _lockProvider.GetLockReason(loadout.CharacterId, slotIndex) ?? "槽位已锁定";
            return LoadoutOperationResult.Locked(reason);
        }

        // 招式存在性
        var def = _moveTable.Get(moveId);
        if (def == null)
            return LoadoutOperationResult.Fail(LoadoutResult.MoveNotFound);

        // 是否已习得
        var entry = _progressionService.GetEntry(moveId);
        if (entry == null)
            return LoadoutOperationResult.Fail(LoadoutResult.MoveNotLearned);

        // 重复装备检查
        var equipped = loadout.GetEquippedMoves();
        if (equipped.Contains(moveId) && loadout.GetMoveSlot(slotIndex) != moveId)
            return LoadoutOperationResult.Fail(LoadoutResult.DuplicateMove);

        // 执行装备
        loadout.SetMoveSlot(slotIndex, moveId);

        // 偏好提醒
        var reminder = CheckPreferenceConflict(loadout.CharacterId, def);

        return LoadoutOperationResult.Ok(reminder);
    }

    /// <summary>
    /// 卸下指定槽位。
    /// </summary>
    public LoadoutOperationResult UnequipMove(CharacterLoadout loadout, int slotIndex)
    {
        if (_lockProvider.IsLocked(loadout.CharacterId, slotIndex))
        {
            var reason = _lockProvider.GetLockReason(loadout.CharacterId, slotIndex) ?? "槽位已锁定";
            return LoadoutOperationResult.Locked(reason);
        }

        loadout.SetMoveSlot(slotIndex, null);
        return LoadoutOperationResult.Ok();
    }

    /// <summary>
    /// 检查体系覆盖。缺体系时返回 warning，不阻止出战。
    /// </summary>
    public CoverageWarning CheckTypeCoverage(CharacterLoadout loadout)
    {
        var covered = new HashSet<MoveType>();
        foreach (var moveId in loadout.GetEquippedMoves())
        {
            var def = _moveTable.Get(moveId);
            if (def != null) covered.Add(def.Type);
        }

        var missing = Enum.GetValues<MoveType>()
            .Where(t => !covered.Contains(t))
            .ToList();

        return new CoverageWarning { MissingTypes = missing };
    }

    private string? CheckPreferenceConflict(string characterId, MoveDefinition move)
    {
        if (!_preferences.TryGetValue(characterId, out var pref))
            return null;

        foreach (var tag in move.Tags)
        {
            if (pref.DislikedTags.Contains(tag))
                return $"该角色不偏好 [{tag}] 类招式";
        }

        return null;
    }
}
