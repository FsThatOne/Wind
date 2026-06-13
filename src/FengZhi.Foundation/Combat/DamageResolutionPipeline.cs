using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 伤害波动的随机源接口。测试时注入固定值。
/// </summary>
public interface IDamageRandomSource
{
    /// <summary>
    /// 返回 [0.95, 1.05] 范围内的伤害波动系数。
    /// </summary>
    float NextVariance();

    /// <summary>
    /// 返回 [0, 1) 范围内的暴击判定值。
    /// </summary>
    float NextCritRoll();
}

/// <summary>
/// 默认随机源，使用 System.Random。
/// </summary>
public sealed class DefaultDamageRandom : IDamageRandomSource
{
    private readonly Random _rng;
    public DefaultDamageRandom(int? seed = null) => _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    public float NextVariance() => 0.95f + (float)_rng.NextDouble() * 0.1f;
    public float NextCritRoll() => (float)_rng.NextDouble();
}

/// <summary>
/// 伤害结算管线输入。
/// </summary>
public readonly struct DamageInput
{
    /// <summary>攻方对应体系攻击力</summary>
    public float AttackForType { get; init; }

    /// <summary>招式基础倍率 (0.6-1.5)</summary>
    public float BaseMultiplier { get; init; }

    /// <summary>招式完整度 (0.55-1.0)</summary>
    public float Completion { get; init; }

    /// <summary>境界缩放系数 (0.7-2.0)</summary>
    public float RealmScaling { get; init; }

    /// <summary>守方防御力</summary>
    public float Defense { get; init; }

    /// <summary>克制关系</summary>
    public CounterRelation CounterRelation { get; init; }

    /// <summary>攻方暴击率</summary>
    public float CritRate { get; init; }
}

/// <summary>
/// 伤害结算管线输出。
/// </summary>
public readonly struct DamageOutput
{
    /// <summary>F1: 有效攻击力</summary>
    public float EffectiveAttack { get; init; }

    /// <summary>F2: 基础伤害 (有效攻击 - 防御, 最低 1)</summary>
    public float BaseDamage { get; init; }

    /// <summary>F3: 波动后实际伤害</summary>
    public float ActualDamage { get; init; }

    /// <summary>F4: 最终伤害 (含克制/暴击)</summary>
    public int FinalDamage { get; init; }

    /// <summary>使用的克制倍率</summary>
    public float CounterMultiplier { get; init; }

    /// <summary>使用的暴击倍率 (1.0 或 1.5)</summary>
    public float CritMultiplier { get; init; }

    /// <summary>是否暴击</summary>
    public bool IsCrit { get; init; }
}

/// <summary>
/// 伤害结算管线纯函数。
/// GDD 公式:
///   F1: effective_attack = attack_for_type × base_multiplier × completion × realm_scaling
///   F2: base_damage = max(1, effective_attack - defense)
///   F3: actual_damage = base_damage × random(0.95, 1.05)
///   F4: final_damage = actual_damage × counter_multiplier × crit_multiplier
/// </summary>
public static class DamageResolutionPipeline
{
    public const float CounterAdvantageMultiplier = 1.3f;
    public const float CounterNeutralMultiplier = 1.0f;
    public const float CounterDisadvantageMultiplier = 0.7f;
    public const float CritMultiplier = 1.5f;
    public const int MinDamage = 1;

    /// <summary>
    /// 执行完整伤害结算管线。
    /// </summary>
    public static DamageOutput Resolve(DamageInput input, IDamageRandomSource random)
    {
        // F1: effective_attack
        float effectiveAttack = input.AttackForType * input.BaseMultiplier * input.Completion * input.RealmScaling;

        // F2: base_damage = max(1, effective_attack - defense)
        float baseDamage = MathF.Max(MinDamage, effectiveAttack - input.Defense);

        // F3: actual_damage = base_damage × random(0.95, 1.05)
        float variance = random.NextVariance();
        float actualDamage = baseDamage * variance;

        // Crit判定
        float critRoll = random.NextCritRoll();
        bool isCrit = critRoll < input.CritRate;
        float critMult = isCrit ? CritMultiplier : 1.0f;

        // 克制倍率
        float counterMult = GetCounterMultiplier(input.CounterRelation);

        // F4: final_damage = actual_damage × counter_multiplier × crit_multiplier
        float rawFinal = actualDamage * counterMult * critMult;
        int finalDamage = Math.Max(MinDamage, (int)MathF.Round(rawFinal));

        return new DamageOutput
        {
            EffectiveAttack = effectiveAttack,
            BaseDamage = baseDamage,
            ActualDamage = actualDamage,
            FinalDamage = finalDamage,
            CounterMultiplier = counterMult,
            CritMultiplier = critMult,
            IsCrit = isCrit
        };
    }

    /// <summary>
    /// 获取克制关系对应的伤害倍率。
    /// </summary>
    public static float GetCounterMultiplier(CounterRelation relation) => relation switch
    {
        CounterRelation.Advantage => CounterAdvantageMultiplier,
        CounterRelation.Neutral => CounterNeutralMultiplier,
        CounterRelation.Disadvantage => CounterDisadvantageMultiplier,
        _ => CounterNeutralMultiplier
    };
}
