namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 结构化触发条件类型。
/// </summary>
public enum ConditionType
{
    /// <summary>无条件，命中即触发。</summary>
    Always,

    /// <summary>需要相邻敌人。</summary>
    AdjacentEnemy,

    /// <summary>目标未被定身。</summary>
    TargetNotImmobilized,

    /// <summary>本回合第 N 次克制命中同一目标。</summary>
    NthCounterHitSameTarget
}

/// <summary>
/// 结构化特殊效果类型。
/// </summary>
public enum EffectType
{
    /// <summary>破绽叠加。参数: 层数。</summary>
    FlawExpose,

    /// <summary>防御姿态提升。</summary>
    GuardUp,

    /// <summary>定身概率。</summary>
    ImmobilizeChance,

    /// <summary>震慑加成。</summary>
    StaggerBonus,

    /// <summary>破防。</summary>
    ShatterGuard,

    /// <summary>毁灭打击。</summary>
    Devastate,

    /// <summary>连击。</summary>
    MultiStrike
}

/// <summary>
/// 解析后的结构化触发条件。
/// </summary>
public sealed class TriggerCondition
{
    public ConditionType Type { get; }
    public int Parameter { get; }

    public TriggerCondition(ConditionType type, int parameter = 0)
    {
        Type = type;
        Parameter = parameter;
    }

    /// <summary>
    /// 从 YAML 字符串标签解析为结构化条件。
    /// </summary>
    public static TriggerCondition Parse(string tag) => tag switch
    {
        "always" or "no_condition" => new TriggerCondition(ConditionType.Always),
        "adjacent_enemy" => new TriggerCondition(ConditionType.AdjacentEnemy),
        "target_not_immobilized" => new TriggerCondition(ConditionType.TargetNotImmobilized),
        "second_counter_hit" => new TriggerCondition(ConditionType.NthCounterHitSameTarget, 2),
        "first_counter_hit" => new TriggerCondition(ConditionType.NthCounterHitSameTarget, 1),
        _ => throw new ArgumentException($"未知触发条件标签: '{tag}'", nameof(tag))
    };
}

/// <summary>
/// 解析后的结构化特殊效果。
/// </summary>
public sealed class SpecialEffect
{
    public EffectType Type { get; }
    public int Parameter { get; }

    /// <summary>该效果所需的解锁等级。None 表示无需解锁。</summary>
    public EffectUnlockFlags RequiredUnlock { get; init; }

    public SpecialEffect(EffectType type, int parameter = 0)
    {
        Type = type;
        Parameter = parameter;
    }

    /// <summary>
    /// 从 YAML 字符串标签解析为结构化效果。
    /// </summary>
    public static SpecialEffect Parse(string tag) => tag switch
    {
        "flaw_expose" => new SpecialEffect(EffectType.FlawExpose, 1),
        "flaw_expose_2" => new SpecialEffect(EffectType.FlawExpose, 2),
        "flaw_expose_3" => new SpecialEffect(EffectType.FlawExpose, 3),
        "guard_up" => new SpecialEffect(EffectType.GuardUp),
        "immobilize_chance" => new SpecialEffect(EffectType.ImmobilizeChance),
        "stagger_bonus" => new SpecialEffect(EffectType.StaggerBonus),
        "shatter_guard" => new SpecialEffect(EffectType.ShatterGuard) { RequiredUnlock = EffectUnlockFlags.EffectV1 },
        "shatter_guard_enhanced" => new SpecialEffect(EffectType.ShatterGuard, 1) { RequiredUnlock = EffectUnlockFlags.EffectV1 },
        "devastate" => new SpecialEffect(EffectType.Devastate) { RequiredUnlock = EffectUnlockFlags.EffectV2 },
        "multi_strike" => new SpecialEffect(EffectType.MultiStrike),
        _ => throw new ArgumentException($"未知特殊效果标签: '{tag}'", nameof(tag))
    };
}

/// <summary>
/// 战斗回合上下文 DTO。由战斗系统传入，供条件判定使用。
/// </summary>
public sealed class BattleContext
{
    /// <summary>本回合对同一目标的克制命中次数。</summary>
    public int CounterHitsSameTarget { get; init; }

    /// <summary>是否有相邻敌人。</summary>
    public bool HasAdjacentEnemy { get; init; }

    /// <summary>目标是否处于定身状态。</summary>
    public bool TargetIsImmobilized { get; init; }

    /// <summary>本次攻击是否命中。</summary>
    public bool DidHit { get; init; }
}

/// <summary>
/// 特殊效果解析结果。
/// </summary>
public sealed class EffectResolution
{
    public bool Triggered { get; init; }
    public IReadOnlyList<SpecialEffect> Effects { get; init; } = Array.Empty<SpecialEffect>();
}

/// <summary>
/// 特殊效果解析器。根据战斗上下文判定条件是否满足，输出应用的效果列表。
/// 纯逻辑，不修改战斗状态。
/// </summary>
public static class SpecialEffectResolver
{
    /// <summary>
    /// 解析招式的特殊触发与效果。
    /// </summary>
    /// <param name="move">招式静态定义。</param>
    /// <param name="progression">招式成长状态（可 null 表示未习得，但战斗中不应为 null）。</param>
    /// <param name="context">本回合战斗上下文。</param>
    /// <returns>触发结果和效果列表。</returns>
    public static EffectResolution Resolve(
        MoveDefinition move,
        MoveProgressionEntry? progression,
        BattleContext context)
    {
        ArgumentNullException.ThrowIfNull(move);
        ArgumentNullException.ThrowIfNull(context);

        // 确定使用的条件和效果标签
        var (conditionTags, effectTags) = ResolveOverrides(move, progression);

        // 解析条件
        var conditions = conditionTags.Select(TriggerCondition.Parse).ToList();

        // 判定所有条件是否满足
        if (!conditions.All(c => EvaluateCondition(c, context)))
            return new EffectResolution { Triggered = false };

        // 解析效果并过滤未解锁的
        var unlockedFlags = progression?.UnlockedEffects ?? EffectUnlockFlags.None;
        var effects = effectTags
            .Select(SpecialEffect.Parse)
            .Where(e => IsEffectUnlocked(e, unlockedFlags))
            .ToList();

        return new EffectResolution
        {
            Triggered = effects.Count > 0,
            Effects = effects
        };
    }

    private static (IReadOnlyList<string> conditions, IReadOnlyList<string> effects) ResolveOverrides(
        MoveDefinition move,
        MoveProgressionEntry? progression)
    {
        // 批注版：使用 override 替换原版
        if (progression?.Stage == MoveProgressionStage.Annotated
            && move.Annotatable
            && move.Annotation != null)
        {
            IReadOnlyList<string> condOverride = !string.IsNullOrEmpty(move.Annotation.ConditionOverride)
                ? new[] { move.Annotation.ConditionOverride }
                : move.TriggerConditions;

            IReadOnlyList<string> effectOverride = !string.IsNullOrEmpty(move.Annotation.EffectOverride)
                ? new[] { move.Annotation.EffectOverride }
                : move.SpecialEffects;

            return (condOverride, effectOverride);
        }

        return (move.TriggerConditions, move.SpecialEffects);
    }

    private static bool EvaluateCondition(TriggerCondition condition, BattleContext context) => condition.Type switch
    {
        ConditionType.Always => true,
        ConditionType.AdjacentEnemy => context.HasAdjacentEnemy,
        ConditionType.TargetNotImmobilized => !context.TargetIsImmobilized,
        ConditionType.NthCounterHitSameTarget => context.CounterHitsSameTarget >= condition.Parameter,
        _ => false
    };

    private static bool IsEffectUnlocked(SpecialEffect effect, EffectUnlockFlags unlocked)
    {
        if (effect.RequiredUnlock == EffectUnlockFlags.None)
            return true;
        return (unlocked & effect.RequiredUnlock) == effect.RequiredUnlock;
    }
}
