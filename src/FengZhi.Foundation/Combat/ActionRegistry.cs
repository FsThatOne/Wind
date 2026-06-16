using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 战斗行动类型枚举。GDD §Action Registry SSoT。
/// </summary>
public enum ActionType
{
    /// <summary>使用招式</summary>
    Move,

    /// <summary>反制（克制+指定目标）</summary>
    Counter,

    /// <summary>决胜一击</summary>
    Decisive,

    /// <summary>调息（回复内息）</summary>
    Breathe,

    /// <summary>普通攻击（零消耗、无体系）</summary>
    BasicAttack,

    /// <summary>使用道具</summary>
    Item
}

/// <summary>
/// 战斗行动 DTO。描述一个角色的行动选择。
/// </summary>
public sealed class BattleAction
{
    public string ActorId { get; init; } = string.Empty;
    public ActionType Type { get; init; }
    public string? TargetId { get; init; }
    public string? MoveId { get; init; }
    public MoveType? MoveType { get; init; }
    public int NeixiCost { get; init; }
}

/// <summary>
/// 行动执行结果。
/// </summary>
public enum ActionOutcome
{
    Success,
    InsufficientNeixi,
    InvalidTarget,
    NotImplemented
}

/// <summary>
/// 行动执行结果 DTO。
/// </summary>
public sealed class ActionResult
{
    public ActionOutcome Outcome { get; init; }
    public int DamageDealt { get; init; }
    public int NeixiRecovered { get; init; }
    public bool IsCrit { get; init; }

    public static ActionResult Fail(ActionOutcome reason) => new() { Outcome = reason };
    public static ActionResult Ok(int damage = 0, int neixiRecovered = 0, bool isCrit = false) => new()
    {
        Outcome = ActionOutcome.Success,
        DamageDealt = damage,
        NeixiRecovered = neixiRecovered,
        IsCrit = isCrit
    };
}

/// <summary>
/// 行动执行框架。统一处理所有战斗行动的前置检查和执行。
/// </summary>
public static class ActionExecutor
{
    /// <summary>
    /// 调息效率系数 (GDD F8: meditation_efficiency = 0.5)
    /// </summary>
    public const float MeditationEfficiency = 0.5f;

    /// <summary>
    /// 普通攻击倍率 (GDD: basic_attack_damage = attack × 0.3)
    /// </summary>
    public const float BasicAttackMultiplier = 0.3f;

    /// <summary>
    /// 执行调息行动。F8: meditation_recovery = ceil(neixi_recovery × 0.5)
    /// </summary>
    public static ActionResult ExecuteBreathe(BattleCombatant actor)
    {
        int recovery = (int)MathF.Ceiling(actor.NeixiRecovery * MeditationEfficiency);
        actor.RecoverNeixi(recovery);
        return ActionResult.Ok(neixiRecovered: recovery);
    }

    /// <summary>
    /// 执行普通攻击。伤害 = 最高体系攻击力 × 0.3，无体系属性。
    /// </summary>
    public static ActionResult ExecuteBasicAttack(BattleCombatant actor, BattleCombatant target)
    {
        int maxAttack = Math.Max(actor.AttackGang, Math.Max(actor.AttackRou, actor.AttackQiao));
        int damage = Math.Max(1, (int)MathF.Round(maxAttack * BasicAttackMultiplier));
        target.ApplyDamage(damage);
        return ActionResult.Ok(damage: damage);
    }

    /// <summary>
    /// 执行使用招式前的内息检查。返回 true 表示可以继续。
    /// </summary>
    public static bool TrySpendNeixi(BattleCombatant actor, int cost)
    {
        return actor.SpendNeixi(cost);
    }

    /// <summary>
    /// 执行道具使用（占位）。
    /// </summary>
    public static ActionResult ExecuteItem(BattleCombatant actor, string? itemId)
    {
        return ActionResult.Fail(ActionOutcome.NotImplemented);
    }
}
