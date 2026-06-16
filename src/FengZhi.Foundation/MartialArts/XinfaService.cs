using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 角色属性查询接口。心法门槛检查时使用。
/// </summary>
public interface ICharacterStatProvider
{
    /// <summary>查询角色指定属性的当前值。</summary>
    int GetStat(string characterId, string statName);
}

/// <summary>
/// 心法封印状态查询接口。战斗/状态效果系统实现。
/// </summary>
public interface IXinfaSealProvider
{
    /// <summary>查询角色当前心法是否被封印。</summary>
    bool IsSealed(string characterId);
}

/// <summary>
/// 默认封印提供者：从不封印。
/// </summary>
public sealed class NoSealProvider : IXinfaSealProvider
{
    public static readonly NoSealProvider Instance = new();
    public bool IsSealed(string characterId) => false;
}

/// <summary>
/// 心法门槛不足详情。
/// </summary>
public sealed class RequirementDeficit
{
    public string Stat { get; init; } = string.Empty;
    public int Required { get; init; }
    public int Current { get; init; }
    public int Deficit => Required - Current;
}

/// <summary>
/// 心法装备操作结果。
/// </summary>
public enum XinfaEquipResult
{
    Success,
    XinfaNotFound,
    RequirementsNotMet
}

/// <summary>
/// 心法装备操作返回详情。
/// </summary>
public sealed class XinfaOperationResult
{
    public XinfaEquipResult Result { get; init; }
    public IReadOnlyList<RequirementDeficit> Deficits { get; init; } = Array.Empty<RequirementDeficit>();

    public static XinfaOperationResult Ok() => new() { Result = XinfaEquipResult.Success };
    public static XinfaOperationResult NotFound() => new() { Result = XinfaEquipResult.XinfaNotFound };
    public static XinfaOperationResult RequirementsNotMet(IReadOnlyList<RequirementDeficit> deficits) => new()
    {
        Result = XinfaEquipResult.RequirementsNotMet,
        Deficits = deficits
    };
}

/// <summary>
/// 心法被动加成 DTO。
/// </summary>
public sealed class XinfaModifier
{
    public string Stat { get; init; } = string.Empty;
    public float Value { get; init; }
}

/// <summary>
/// 战斗可用招式查询结果。
/// </summary>
public sealed class CombatMoveSet
{
    public IReadOnlyList<string> BaseMoves { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> XinfaExclusiveMoves { get; init; } = Array.Empty<string>();
    public int TotalCount => BaseMoves.Count + XinfaExclusiveMoves.Count;
}

/// <summary>
/// 心法装备门槛、被动加成与专属招式服务。
/// </summary>
public sealed class XinfaService
{
    private readonly IDataTable<XinfaDefinition> _xinfaTable;
    private readonly IDataTable<MoveDefinition> _moveTable;
    private readonly ICharacterStatProvider _statProvider;
    private readonly IXinfaSealProvider _sealProvider;

    public XinfaService(
        IDataTable<XinfaDefinition> xinfaTable,
        IDataTable<MoveDefinition> moveTable,
        ICharacterStatProvider statProvider,
        IXinfaSealProvider? sealProvider = null)
    {
        _xinfaTable = xinfaTable ?? throw new ArgumentNullException(nameof(xinfaTable));
        _moveTable = moveTable ?? throw new ArgumentNullException(nameof(moveTable));
        _statProvider = statProvider ?? throw new ArgumentNullException(nameof(statProvider));
        _sealProvider = sealProvider ?? NoSealProvider.Instance;
    }

    /// <summary>
    /// 尝试装备心法。检查门槛并返回差值提示。
    /// </summary>
    public XinfaOperationResult EquipXinfa(CharacterLoadout loadout, string xinfaId)
    {
        var xinfa = _xinfaTable.Get(xinfaId);
        if (xinfa == null)
            return XinfaOperationResult.NotFound();

        // 门槛检查
        var deficits = new List<RequirementDeficit>();
        foreach (var req in xinfa.Requirements)
        {
            var current = _statProvider.GetStat(loadout.CharacterId, req.Stat);
            if (current < req.Min)
            {
                deficits.Add(new RequirementDeficit
                {
                    Stat = req.Stat,
                    Required = req.Min,
                    Current = current
                });
            }
        }

        if (deficits.Count > 0)
            return XinfaOperationResult.RequirementsNotMet(deficits);

        loadout.XinfaSlot = xinfaId;
        return XinfaOperationResult.Ok();
    }

    /// <summary>
    /// 卸下心法。
    /// </summary>
    public void UnequipXinfa(CharacterLoadout loadout)
    {
        loadout.XinfaSlot = null;
    }

    /// <summary>
    /// 获取心法被动加成。公式：xinfa_base_bonus × realm_scaling。
    /// 封印时返回空列表。
    /// </summary>
    public IReadOnlyList<XinfaModifier> GetPassiveBonuses(CharacterLoadout loadout, RealmTier realm)
    {
        if (loadout.XinfaSlot == null)
            return Array.Empty<XinfaModifier>();

        if (_sealProvider.IsSealed(loadout.CharacterId))
            return Array.Empty<XinfaModifier>();

        var xinfa = _xinfaTable.Get(loadout.XinfaSlot);
        if (xinfa == null)
            return Array.Empty<XinfaModifier>();

        var scaling = RealmScaling.GetScaling(realm);
        var result = new List<XinfaModifier>(xinfa.PassiveBonuses.Count);
        foreach (var bonus in xinfa.PassiveBonuses)
        {
            result.Add(new XinfaModifier
            {
                Stat = bonus.Stat,
                Value = bonus.Value * scaling
            });
        }
        return result;
    }

    /// <summary>
    /// 获取战斗可用招式集合：6 基础槽位 + 心法专属招式。
    /// 封印时心法专属招式不返回。
    /// </summary>
    public CombatMoveSet GetCombatMoveSet(CharacterLoadout loadout)
    {
        var baseMoves = loadout.GetEquippedMoves();

        if (loadout.XinfaSlot == null || _sealProvider.IsSealed(loadout.CharacterId))
        {
            return new CombatMoveSet
            {
                BaseMoves = baseMoves,
                XinfaExclusiveMoves = Array.Empty<string>()
            };
        }

        var xinfa = _xinfaTable.Get(loadout.XinfaSlot);
        if (xinfa == null)
        {
            return new CombatMoveSet
            {
                BaseMoves = baseMoves,
                XinfaExclusiveMoves = Array.Empty<string>()
            };
        }

        return new CombatMoveSet
        {
            BaseMoves = baseMoves,
            XinfaExclusiveMoves = xinfa.ExclusiveMoves
        };
    }
}
