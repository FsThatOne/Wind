namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 修改器层级。
/// </summary>
public enum ModifierLayer
{
    /// <summary>永久层 — 章节成长/顿悟（不可移除）</summary>
    Permanent,
    /// <summary>半永久层 — 心法/装备（卸下时移除）</summary>
    SemiPermanent,
    /// <summary>临时层 — 战斗buff（按回合衰减或战斗结束清除）</summary>
    Temporary
}

/// <summary>
/// 可被修改器影响的属性类型。
/// </summary>
public enum AttributeType
{
    Strength,
    Agility,
    InnerPower,
    Insight,
    Constitution,
    Attack,
    Defense,
    Speed,
    CritRate,
    NeiXiRecovery,
    MaxHp,
    MaxNeiXi
}

/// <summary>
/// 属性修改器。同源不叠加（取最高值），不同源正常叠加。
/// </summary>
public sealed class AttributeModifier
{
    /// <summary>来源标识（如 "equipment:iron_sword", "buff:tiger_stance"）</summary>
    public required string Source { get; init; }

    /// <summary>修改器层级</summary>
    public required ModifierLayer Layer { get; init; }

    /// <summary>目标属性</summary>
    public required AttributeType Attribute { get; init; }

    /// <summary>加算值</summary>
    public required int Value { get; init; }

    /// <summary>剩余持续回合数（仅 Temporary 层使用，-1=无限持续直到手动移除）</summary>
    public int Duration { get; set; } = -1;
}
