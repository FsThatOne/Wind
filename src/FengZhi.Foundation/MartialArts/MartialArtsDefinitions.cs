using FengZhi.Foundation.CharacterData;
using YamlDotNet.Serialization;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 招式类别。用于区分普通招式、高级武学和绝学。
/// </summary>
public enum MoveCategory
{
    Basic,
    Advanced,
    Ultimate
}

/// <summary>
/// 刚柔巧克制关系。
/// </summary>
public enum CounterRelation
{
    Disadvantage,
    Neutral,
    Advantage
}

/// <summary>
/// 批注版三组件静态定义。由武学数据库逐一配置。
/// </summary>
public sealed class AnnotationDefinition
{
    [YamlMember(Alias = "condition_override")]
    public string ConditionOverride { get; set; } = string.Empty;

    [YamlMember(Alias = "effect_override")]
    public string EffectOverride { get; set; } = string.Empty;

    [YamlMember(Alias = "annotation_mult")]
    public float AnnotationMult { get; set; } = 1.0f;
}

/// <summary>
/// 武学招式静态定义。
/// </summary>
public sealed class MoveDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "type")]
    public MoveType Type { get; set; }

    [YamlMember(Alias = "category")]
    public MoveCategory Category { get; set; }

    [YamlMember(Alias = "neixi_cost")]
    public int NeixiCost { get; set; }

    [YamlMember(Alias = "base_multiplier")]
    public float BaseMultiplier { get; set; }

    [YamlMember(Alias = "trigger_conditions")]
    public List<string> TriggerConditions { get; set; } = new();

    [YamlMember(Alias = "special_effects")]
    public List<string> SpecialEffects { get; set; } = new();

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = new();

    [YamlMember(Alias = "annotatable")]
    public bool Annotatable { get; set; }

    [YamlMember(Alias = "annotation")]
    public AnnotationDefinition? Annotation { get; set; }
}

/// <summary>
/// 心法被动加成配置。
/// </summary>
public sealed class XinfaPassiveBonus
{
    [YamlMember(Alias = "stat")]
    public string Stat { get; set; } = string.Empty;

    [YamlMember(Alias = "value")]
    public float Value { get; set; }
}

/// <summary>
/// 心法属性门槛配置。
/// </summary>
public sealed class XinfaRequirement
{
    [YamlMember(Alias = "stat")]
    public string Stat { get; set; } = string.Empty;

    [YamlMember(Alias = "min")]
    public int Min { get; set; }
}

/// <summary>
/// 心法静态定义。
/// </summary>
public sealed class XinfaDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "requirements")]
    public List<XinfaRequirement> Requirements { get; set; } = new();

    [YamlMember(Alias = "passive_bonuses")]
    public List<XinfaPassiveBonus> PassiveBonuses { get; set; } = new();

    [YamlMember(Alias = "exclusive_moves")]
    public List<string> ExclusiveMoves { get; set; } = new();
}

/// <summary>
/// 轻功主动机动配置占位。
/// </summary>
public sealed class QinggongActiveManeuver
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 轻功静态定义。
/// </summary>
public sealed class QinggongDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "move_range_bonus")]
    public int MoveRangeBonus { get; set; }

    [YamlMember(Alias = "active_maneuvers")]
    public List<QinggongActiveManeuver> ActiveManeuvers { get; set; } = new();
}

/// <summary>
/// 单条刚柔巧克制矩阵记录。
/// </summary>
public sealed class CounterMatrixEntry
{
    [YamlMember(Alias = "attacker")]
    public MoveType Attacker { get; set; }

    [YamlMember(Alias = "defender")]
    public MoveType Defender { get; set; }

    [YamlMember(Alias = "relation")]
    public CounterRelation Relation { get; set; }

    [YamlMember(Alias = "multiplier")]
    public float Multiplier { get; set; } = 1.0f;

    /// <summary>矩阵记录的稳定 key，形如 "rou_vs_gang"。</summary>
    public string Id => MakeId(Attacker, Defender);

    /// <summary>由攻防体系组合生成矩阵 key。</summary>
    public static string MakeId(MoveType attacker, MoveType defender) =>
        $"{attacker.ToString().ToLowerInvariant()}_vs_{defender.ToString().ToLowerInvariant()}";
}
