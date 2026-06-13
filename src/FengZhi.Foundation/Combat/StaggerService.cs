using FengZhi.Foundation.MartialArts;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 破绽变化结果 DTO。
/// </summary>
public readonly struct StaggerChange
{
    /// <summary>守方破绽变化量</summary>
    public int DefenderDelta { get; init; }

    /// <summary>攻方破绽变化量（被克时攻方+1）</summary>
    public int AttackerDelta { get; init; }
}

/// <summary>
/// 破绽系统服务。管理破绽累积规则和一击决胜判定。
/// GDD §Core Rules 6, §Formulas F6-F7
/// </summary>
public static class StaggerService
{
    // --- Tuning Knobs ---
    public const int StaggerOnCounter = 2;
    public const int StaggerOnCountered = 1;
    public const int CounterExtraStagger = 1;
    public const int CoopExtraStagger = 1;
    public const float DecisiveStrikeMultiplier = 2.0f;

    /// <summary>
    /// 根据克制关系计算破绽变化。
    /// </summary>
    public static StaggerChange CalculateStaggerChange(CounterRelation relation, bool isCounterAction = false)
    {
        int defenderDelta = 0;
        int attackerDelta = 0;

        switch (relation)
        {
            case CounterRelation.Advantage:
                defenderDelta = StaggerOnCounter; // 守方 +2
                if (isCounterAction)
                    defenderDelta += CounterExtraStagger; // 反制额外 +1 = 总计 +3
                break;
            case CounterRelation.Disadvantage:
                attackerDelta = StaggerOnCountered; // 攻方 +1
                break;
            case CounterRelation.Neutral:
                break;
        }

        return new StaggerChange
        {
            DefenderDelta = defenderDelta,
            AttackerDelta = attackerDelta
        };
    }

    /// <summary>
    /// 判定是否可以发动一击决胜。
    /// </summary>
    public static bool CanExecuteDecisiveStrike(BattleCombatant target)
        => target.IsStaggerExposed;

    /// <summary>
    /// 计算一击决胜伤害。F6: decisive_damage = base_damage × 2.0
    /// 无视克制倍率和暴击。
    /// </summary>
    public static int CalculateDecisiveDamage(float effectiveAttack, float defense)
    {
        float baseDamage = MathF.Max(DamageResolutionPipeline.MinDamage, effectiveAttack - defense);
        return Math.Max(DamageResolutionPipeline.MinDamage, (int)MathF.Round(baseDamage * DecisiveStrikeMultiplier));
    }

    /// <summary>
    /// 执行一击决胜：造成伤害并清空目标破绽。
    /// 返回实际造成的伤害值。
    /// </summary>
    public static int ExecuteDecisiveStrike(BattleCombatant attacker, BattleCombatant target, float effectiveAttack)
    {
        if (!CanExecuteDecisiveStrike(target))
            throw new InvalidOperationException("目标破绽未达阈值，无法发动一击决胜");

        int damage = CalculateDecisiveDamage(effectiveAttack, target.Defense);
        target.ApplyDamage(damage);
        target.ClearStagger();
        return damage;
    }

    /// <summary>
    /// 应用协同破绽加成（两人同回合克制同一目标时额外+1）。
    /// </summary>
    public static void ApplyCoopBonus(BattleCombatant target)
    {
        target.AddStagger(CoopExtraStagger);
    }
}
