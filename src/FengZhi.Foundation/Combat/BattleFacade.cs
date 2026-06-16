using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 战斗配置 DTO。外部系统通过此对象发起一场战斗。
/// GDD §Battle Entry Interface
/// </summary>
public sealed class BattleConfig
{
    /// <summary>战斗类型标识（用于叙事回调分流）</summary>
    public string BattleType { get; init; } = "normal";

    /// <summary>玩家方角色配置列表</summary>
    public required IReadOnlyList<CombatantConfig> PlayerParty { get; init; }

    /// <summary>敌方角色配置列表</summary>
    public required IReadOnlyList<CombatantConfig> EnemyGroup { get; init; }

    /// <summary>最大回合数（默认 15）</summary>
    public int MaxRounds { get; init; } = 15;
}

/// <summary>
/// 单个参战角色配置。从 CharacterLoadout + 属性系统初始化。
/// </summary>
public sealed class CombatantConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int MaxHP { get; init; }
    public int MaxNeixi { get; init; }
    public int AttackGang { get; init; }
    public int AttackRou { get; init; }
    public int AttackQiao { get; init; }
    public int Defense { get; init; }
    public int Speed { get; init; }
    public float CritRate { get; init; }
    public int InsightStat { get; init; }
    public int NeixiRecovery { get; init; }
    public int StaggerThreshold { get; init; } = BattleCombatant.DefaultStaggerThreshold;

    /// <summary>装备招式 ID 列表（最多 6 个）</summary>
    public IReadOnlyList<string> EquippedMoveIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 战斗统计 DTO。战斗结束后返回。
/// </summary>
public sealed class BattleStats
{
    public BattleResult Result { get; init; }
    public int TotalRounds { get; init; }
    public int TotalDamageDealt { get; init; }
    public int TotalDamageReceived { get; init; }
    public int DecisiveStrikesLanded { get; init; }
    public int CoopBonusesTriggered { get; init; }
}

/// <summary>
/// AI 决策接口。战斗系统通过此接口获取非玩家角色的行动。
/// </summary>
public interface IBattleAI
{
    /// <summary>
    /// 为指定角色生成本回合行动。
    /// </summary>
    BattleAction DecideAction(BattleCombatant actor, BattleInstance battle);
}

/// <summary>
/// 脚本 AI（测试用）。按预设序列返回行动。
/// </summary>
public sealed class ScriptedAI : IBattleAI
{
    private readonly Queue<BattleAction> _script;

    public ScriptedAI(IEnumerable<BattleAction> script)
    {
        _script = new Queue<BattleAction>(script);
    }

    public BattleAction DecideAction(BattleCombatant actor, BattleInstance battle)
    {
        if (_script.Count > 0)
            return _script.Dequeue();

        // 兜底：调息
        return new BattleAction { ActorId = actor.Id, Type = ActionType.Breathe };
    }
}

/// <summary>
/// 战斗入口门面。将所有子系统串联为一场完整战斗。
/// </summary>
public sealed class BattleFacade
{
    private readonly IDamageRandomSource _random;

    public BattleFacade(IDamageRandomSource? random = null)
    {
        _random = random ?? new DefaultDamageRandom();
    }

    /// <summary>
    /// 从配置创建战斗实例。
    /// </summary>
    public BattleInstance InitiateBattle(BattleConfig config)
    {
        var players = config.PlayerParty.Select(CreateCombatant).ToList();
        var enemies = config.EnemyGroup.Select(CreateCombatant).ToList();
        return new BattleInstance(players, enemies, config.MaxRounds);
    }

    /// <summary>
    /// 运行一场完整战斗（测试用）。
    /// 使用 playerDecisions 和 enemyAI 控制双方行动。
    /// </summary>
    public BattleStats RunFullBattle(
        BattleConfig config,
        IReadOnlyList<BattleAction> playerDecisions,
        IBattleAI enemyAI)
    {
        var battle = InitiateBattle(config);
        var counterService = new CounterService();
        var resolution = new ResolutionService(counterService, _random);

        var playerQueue = new Queue<BattleAction>(playerDecisions);
        var playerIds = new HashSet<string>(config.PlayerParty.Select(p => p.Id));
        var combatantLookup = battle.PlayerParty.Concat(battle.EnemyGroup)
            .ToDictionary(c => c.Id);

        int totalDamageDealt = 0;
        int totalDamageReceived = 0;
        int decisiveStrikes = 0;
        int coopBonuses = 0;

        // 驱动状态机
        battle.AdvancePhase(); // Init → RoundStart

        while (battle.CurrentPhase != BattlePhase.BattleOver)
        {
            switch (battle.CurrentPhase)
            {
                case BattlePhase.RoundStart:
                    counterService.ResetRound();
                    battle.AdvancePhase(); // → IntentReveal
                    break;

                case BattlePhase.IntentReveal:
                    battle.AdvancePhase(); // → PlayerDecision
                    break;

                case BattlePhase.PlayerDecision:
                    battle.AdvancePhase(); // → Resolution
                    break;

                case BattlePhase.Resolution:
                    // 收集本回合行动
                    var actions = CollectActions(battle, playerQueue, enemyAI, playerIds);

                    // 结算
                    var results = resolution.ResolveRound(actions, combatantLookup, playerIds);

                    // 统计
                    foreach (var r in results)
                    {
                        if (r.IsPlayerSide)
                        {
                            totalDamageDealt += r.DamageDealt;
                            if (r.ActionType == ActionType.Decisive) decisiveStrikes++;
                        }
                        else
                        {
                            totalDamageReceived += r.DamageDealt;
                        }
                    }

                    // 检查协同
                    var playerAdvantages = results
                        .Where(r => r.IsPlayerSide && r.Relation == MartialArts.CounterRelation.Advantage)
                        .GroupBy(r => r.TargetId)
                        .Count(g => g.Select(r => r.ActorId).Distinct().Count() >= 2);
                    coopBonuses += playerAdvantages;

                    battle.AdvancePhase(); // → RoundEnd
                    break;

                case BattlePhase.RoundEnd:
                    battle.AdvancePhase(); // → RoundStart or BattleOver
                    break;
            }
        }

        return new BattleStats
        {
            Result = battle.Result,
            TotalRounds = battle.CurrentRound,
            TotalDamageDealt = totalDamageDealt,
            TotalDamageReceived = totalDamageReceived,
            DecisiveStrikesLanded = decisiveStrikes,
            CoopBonusesTriggered = coopBonuses
        };
    }

    private static List<BattleAction> CollectActions(
        BattleInstance battle,
        Queue<BattleAction> playerQueue,
        IBattleAI enemyAI,
        IReadOnlySet<string> playerIds)
    {
        var actions = new List<BattleAction>();

        // 玩家行动
        foreach (var p in battle.PlayerParty.Where(c => c.IsAlive))
        {
            if (playerQueue.Count > 0)
                actions.Add(playerQueue.Dequeue());
            else
                actions.Add(new BattleAction { ActorId = p.Id, Type = ActionType.Breathe });
        }

        // 敌方行动
        foreach (var e in battle.EnemyGroup.Where(c => c.IsAlive))
        {
            actions.Add(enemyAI.DecideAction(e, battle));
        }

        return actions;
    }

    private static BattleCombatant CreateCombatant(CombatantConfig config)
    {
        return new BattleCombatant(
            config.Id,
            config.Name,
            config.MaxHP,
            config.MaxNeixi,
            config.AttackGang,
            config.AttackRou,
            config.AttackQiao,
            config.Defense,
            config.Speed,
            config.CritRate,
            config.InsightStat,
            config.NeixiRecovery,
            config.StaggerThreshold);
    }
}
