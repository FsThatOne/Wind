namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 招式伤害计算输入参数。
/// </summary>
public readonly struct MoveDamageInput
{
    /// <summary>招式基础倍率（来自 MoveDefinition.BaseMultiplier）。</summary>
    public float BaseMultiplier { get; init; }

    /// <summary>批注倍率系数。非批注版传 1.0。</summary>
    public float AnnotationMult { get; init; }

    /// <summary>该体系对应的攻击力（来自角色属性系统）。</summary>
    public float AttackForType { get; init; }

    /// <summary>招式完整度（来自 MoveProgressionEntry.Completion）。</summary>
    public float Completion { get; init; }

    /// <summary>使用者当前境界。</summary>
    public RealmTier Realm { get; init; }
}

/// <summary>
/// 招式伤害计算结果。
/// </summary>
public readonly struct MoveDamageOutput
{
    public float EffectiveBaseMultiplier { get; init; }
    public float RealmScalingUsed { get; init; }
    public float TotalDamage { get; init; }
}

/// <summary>
/// 招式伤害校验失败原因。
/// </summary>
public enum DamageCalcError
{
    None,
    InvalidBaseMultiplier,
    InvalidAnnotationMult,
    InvalidAttack,
    InvalidCompletion,
    InvalidRealm
}

/// <summary>
/// 招式伤害纯函数计算器。
/// move_damage = effective_base_multiplier × attack_for_type × completion × realm_scaling
/// effective_base_multiplier = base_multiplier × annotation_mult
/// </summary>
public static class MoveDamageCalculator
{
    /// <summary>
    /// 计算招式伤害。成功返回 (output, None)，校验失败返回 (default, error)。
    /// </summary>
    public static (MoveDamageOutput Output, DamageCalcError Error) Calculate(MoveDamageInput input)
    {
        if (input.BaseMultiplier <= 0)
            return (default, DamageCalcError.InvalidBaseMultiplier);

        if (input.AnnotationMult <= 0)
            return (default, DamageCalcError.InvalidAnnotationMult);

        if (input.AttackForType < 0)
            return (default, DamageCalcError.InvalidAttack);

        if (input.Completion < 0 || input.Completion > 1.0f)
            return (default, DamageCalcError.InvalidCompletion);

        if (!Enum.IsDefined(input.Realm))
            return (default, DamageCalcError.InvalidRealm);

        float effectiveBase = input.BaseMultiplier * input.AnnotationMult;
        float realmScaling = RealmScaling.GetScaling(input.Realm);
        float totalDamage = effectiveBase * input.AttackForType * input.Completion * realmScaling;

        return (new MoveDamageOutput
        {
            EffectiveBaseMultiplier = effectiveBase,
            RealmScalingUsed = realmScaling,
            TotalDamage = totalDamage
        }, DamageCalcError.None);
    }
}
