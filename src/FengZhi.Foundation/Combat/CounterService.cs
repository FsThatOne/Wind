using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 反制结果。
/// </summary>
public enum CounterOutcome
{
    /// <summary>反制成功（克制对方意图体系）</summary>
    Success,

    /// <summary>反制失败（体系不克制），降级为普通攻击</summary>
    FailedDegradedToBasic,

    /// <summary>内息不足，无法执行</summary>
    InsufficientNeixi,

    /// <summary>本回合已反制过</summary>
    AlreadyUsedThisRound
}

/// <summary>
/// 反制执行结果 DTO。
/// </summary>
public sealed class CounterResult
{
    public CounterOutcome Outcome { get; init; }
    public int DamageDealt { get; init; }
    public int StaggerApplied { get; init; }
    public bool IsCrit { get; init; }
}

/// <summary>
/// 反制机制服务。
/// GDD §Core Rules 5:
///   - 消耗 3 内息
///   - 攻方招式体系克制目标意图体系 → 伤害×1.3 + 目标+3破绽
///   - 未克制 → 降级为普通攻击
///   - 每回合每角色只能反制一个目标
/// </summary>
public sealed class CounterService
{
    public const int CounterNeixiCost = 3;

    private readonly HashSet<string> _usedThisRound = new();

    /// <summary>
    /// 重置回合反制记录。每回合开始时调用。
    /// </summary>
    public void ResetRound()
    {
        _usedThisRound.Clear();
    }

    /// <summary>
    /// 检查是否可以执行反制。
    /// </summary>
    public bool CanCounter(BattleCombatant actor)
    {
        return actor.CanAfford(CounterNeixiCost) && !_usedThisRound.Contains(actor.Id);
    }

    /// <summary>
    /// 执行反制行动。
    /// </summary>
    /// <param name="actor">攻方</param>
    /// <param name="target">守方</param>
    /// <param name="attackerMoveType">攻方选择的招式体系</param>
    /// <param name="targetIntentType">守方已公开的意图体系</param>
    /// <param name="random">伤害随机源</param>
    public CounterResult Execute(
        BattleCombatant actor,
        BattleCombatant target,
        MoveType attackerMoveType,
        MoveType targetIntentType,
        IDamageRandomSource random)
    {
        // 前置条件：内息
        if (!actor.CanAfford(CounterNeixiCost))
            return new CounterResult { Outcome = CounterOutcome.InsufficientNeixi };

        // 前置条件：本回合未用过
        if (_usedThisRound.Contains(actor.Id))
            return new CounterResult { Outcome = CounterOutcome.AlreadyUsedThisRound };

        // 扣除内息
        actor.SpendNeixi(CounterNeixiCost);
        _usedThisRound.Add(actor.Id);

        // 判定克制关系
        var relation = GetCounterRelation(attackerMoveType, targetIntentType);

        if (relation != CounterRelation.Advantage)
        {
            // 反制失败，降级为普通攻击
            var basicResult = ActionExecutor.ExecuteBasicAttack(actor, target);
            return new CounterResult
            {
                Outcome = CounterOutcome.FailedDegradedToBasic,
                DamageDealt = basicResult.DamageDealt,
                StaggerApplied = 0
            };
        }

        // 反制成功 — 走伤害管线
        float effectiveAttack = actor.GetAttackForType(attackerMoveType);
        var damageInput = new DamageInput
        {
            AttackForType = effectiveAttack,
            BaseMultiplier = 1.0f,
            Completion = 1.0f,
            RealmScaling = 1.0f,
            Defense = target.Defense,
            CounterRelation = CounterRelation.Advantage,
            CritRate = actor.CritRate
        };

        var damageOutput = DamageResolutionPipeline.Resolve(damageInput, random);
        target.ApplyDamage(damageOutput.FinalDamage);

        // 破绽：反制成功 = 克制+2 + 额外+1 = 总计+3
        var staggerChange = StaggerService.CalculateStaggerChange(CounterRelation.Advantage, isCounterAction: true);
        target.AddStagger(staggerChange.DefenderDelta);

        return new CounterResult
        {
            Outcome = CounterOutcome.Success,
            DamageDealt = damageOutput.FinalDamage,
            StaggerApplied = staggerChange.DefenderDelta,
            IsCrit = damageOutput.IsCrit
        };
    }

    /// <summary>
    /// 判定攻方体系对守方体系的克制关系。
    /// 刚克巧、巧克柔、柔克刚。
    /// </summary>
    public static CounterRelation GetCounterRelation(MoveType attacker, MoveType defender)
    {
        if (attacker == defender) return CounterRelation.Neutral;

        bool isAdvantage = (attacker == MoveType.Gang && defender == MoveType.Qiao)
                        || (attacker == MoveType.Qiao && defender == MoveType.Rou)
                        || (attacker == MoveType.Rou && defender == MoveType.Gang);

        return isAdvantage ? CounterRelation.Advantage : CounterRelation.Disadvantage;
    }
}
