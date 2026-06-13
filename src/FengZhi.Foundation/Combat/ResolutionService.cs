using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 单个行动的结算记录。
/// </summary>
public sealed class ResolvedAction
{
    public string ActorId { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public ActionType ActionType { get; init; }
    public MoveType? MoveType { get; init; }
    public CounterRelation Relation { get; init; }
    public int DamageDealt { get; init; }
    public bool IsCrit { get; init; }
    public bool IsPlayerSide { get; init; }
}

/// <summary>
/// 结算阶段服务。统一处理一回合所有行动的结算，并检测协同破绽加成。
/// GDD §Core Rules 7: 两个己方角色同回合克制同一目标 → 额外 +1 破绽。
/// 敌方不享受协同加成（非对称设计）。
/// </summary>
public sealed class ResolutionService
{
    private readonly CounterService _counterService;
    private readonly IDamageRandomSource _random;

    public ResolutionService(CounterService counterService, IDamageRandomSource random)
    {
        _counterService = counterService;
        _random = random;
    }

    /// <summary>
    /// 结算一批行动并返回结果列表。
    /// 自动检测协同条件并应用加成。
    /// </summary>
    /// <param name="actions">本回合收集的所有行动</param>
    /// <param name="combatants">所有参战角色（按 ID 索引）</param>
    /// <param name="playerIds">己方角色 ID 集合</param>
    public List<ResolvedAction> ResolveRound(
        IReadOnlyList<BattleAction> actions,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        IReadOnlySet<string> playerIds)
    {
        // 构建意图映射：每个角色的 MoveType（来自其行动选择）
        var intentMap = BuildIntentMap(actions);

        var results = new List<ResolvedAction>();

        // Phase 1: 执行所有行动
        foreach (var action in actions)
        {
            if (!combatants.TryGetValue(action.ActorId, out var actor))
                continue;
            if (!actor.IsAlive)
                continue;

            var resolved = ExecuteAction(action, actor, combatants, intentMap, playerIds.Contains(action.ActorId));
            if (resolved != null)
                results.Add(resolved);
        }

        // Phase 2: 检测协同破绽加成（仅己方）
        ApplyCoopBonuses(results, combatants);

        return results;
    }

    /// <summary>
    /// 从行动列表构建各角色的意图体系映射。
    /// </summary>
    private static Dictionary<string, MoveType> BuildIntentMap(IReadOnlyList<BattleAction> actions)
    {
        var map = new Dictionary<string, MoveType>();
        foreach (var action in actions)
        {
            if (action.MoveType.HasValue)
                map[action.ActorId] = action.MoveType.Value;
        }
        return map;
    }

    private ResolvedAction? ExecuteAction(
        BattleAction action,
        BattleCombatant actor,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        Dictionary<string, MoveType> intentMap,
        bool isPlayerSide)
    {
        switch (action.Type)
        {
            case ActionType.Move:
                return ExecuteMove(action, actor, combatants, intentMap, isPlayerSide);

            case ActionType.Counter:
                return ExecuteCounter(action, actor, combatants, intentMap, isPlayerSide);

            case ActionType.Decisive:
                return ExecuteDecisive(action, actor, combatants, isPlayerSide);

            case ActionType.Breathe:
                ActionExecutor.ExecuteBreathe(actor);
                return new ResolvedAction
                {
                    ActorId = actor.Id,
                    TargetId = actor.Id,
                    ActionType = ActionType.Breathe,
                    IsPlayerSide = isPlayerSide
                };

            case ActionType.BasicAttack:
                return ExecuteBasicAttack(action, actor, combatants, isPlayerSide);

            default:
                return null;
        }
    }

    private ResolvedAction? ExecuteMove(
        BattleAction action,
        BattleCombatant actor,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        Dictionary<string, MoveType> intentMap,
        bool isPlayerSide)
    {
        if (action.TargetId == null || !combatants.TryGetValue(action.TargetId, out var target))
            return null;
        if (!target.IsAlive) return null;
        if (!ActionExecutor.TrySpendNeixi(actor, action.NeixiCost))
            return null;

        var moveType = action.MoveType ?? CharacterData.MoveType.Gang;

        // 目标的意图体系：从其本回合行动中获取
        var targetMoveType = intentMap.TryGetValue(action.TargetId, out var tmt)
            ? tmt
            : CharacterData.MoveType.Gang;

        var relation = CounterService.GetCounterRelation(moveType, targetMoveType);

        var damageInput = new DamageInput
        {
            AttackForType = actor.GetAttackForType(moveType),
            BaseMultiplier = 1.0f,
            Completion = 1.0f,
            RealmScaling = 1.0f,
            Defense = target.Defense,
            CounterRelation = relation,
            CritRate = actor.CritRate
        };

        var output = DamageResolutionPipeline.Resolve(damageInput, _random);
        target.ApplyDamage(output.FinalDamage);

        // 应用破绽
        var stagger = StaggerService.CalculateStaggerChange(relation);
        target.AddStagger(stagger.DefenderDelta);
        actor.AddStagger(stagger.AttackerDelta);

        return new ResolvedAction
        {
            ActorId = actor.Id,
            TargetId = target.Id,
            ActionType = ActionType.Move,
            MoveType = moveType,
            Relation = relation,
            DamageDealt = output.FinalDamage,
            IsCrit = output.IsCrit,
            IsPlayerSide = isPlayerSide
        };
    }

    private ResolvedAction? ExecuteCounter(
        BattleAction action,
        BattleCombatant actor,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        Dictionary<string, MoveType> intentMap,
        bool isPlayerSide)
    {
        if (action.TargetId == null || !combatants.TryGetValue(action.TargetId, out var target))
            return null;
        if (!target.IsAlive) return null;

        var moveType = action.MoveType ?? CharacterData.MoveType.Gang;
        // 反制时使用目标的意图体系来判定克制
        var targetIntent = intentMap.TryGetValue(action.TargetId, out var tmt)
            ? tmt
            : CharacterData.MoveType.Gang;
        var result = _counterService.Execute(actor, target, moveType, targetIntent, _random);

        var relation = result.Outcome == CounterOutcome.Success
            ? CounterRelation.Advantage
            : CounterRelation.Neutral;

        return new ResolvedAction
        {
            ActorId = actor.Id,
            TargetId = target.Id,
            ActionType = ActionType.Counter,
            MoveType = moveType,
            Relation = relation,
            DamageDealt = result.DamageDealt,
            IsCrit = result.IsCrit,
            IsPlayerSide = isPlayerSide
        };
    }

    private ResolvedAction? ExecuteDecisive(
        BattleAction action,
        BattleCombatant actor,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        bool isPlayerSide)
    {
        if (action.TargetId == null || !combatants.TryGetValue(action.TargetId, out var target))
            return null;
        if (!target.IsAlive) return null;
        if (!StaggerService.CanExecuteDecisiveStrike(target))
            return null;

        var moveType = action.MoveType ?? CharacterData.MoveType.Gang;
        float effectiveAttack = actor.GetAttackForType(moveType);
        int damage = StaggerService.ExecuteDecisiveStrike(actor, target, effectiveAttack);

        return new ResolvedAction
        {
            ActorId = actor.Id,
            TargetId = target.Id,
            ActionType = ActionType.Decisive,
            DamageDealt = damage,
            IsPlayerSide = isPlayerSide
        };
    }

    private ResolvedAction? ExecuteBasicAttack(
        BattleAction action,
        BattleCombatant actor,
        IReadOnlyDictionary<string, BattleCombatant> combatants,
        bool isPlayerSide)
    {
        if (action.TargetId == null || !combatants.TryGetValue(action.TargetId, out var target))
            return null;
        if (!target.IsAlive) return null;

        var result = ActionExecutor.ExecuteBasicAttack(actor, target);

        return new ResolvedAction
        {
            ActorId = actor.Id,
            TargetId = target.Id,
            ActionType = ActionType.BasicAttack,
            DamageDealt = result.DamageDealt,
            IsPlayerSide = isPlayerSide
        };
    }

    /// <summary>
    /// 检测并应用协同破绽加成。
    /// 条件：两个己方角色同回合攻击同一目标，且均为克制关系。
    /// </summary>
    private static void ApplyCoopBonuses(
        List<ResolvedAction> results,
        IReadOnlyDictionary<string, BattleCombatant> combatants)
    {
        // 只检查己方的进攻性行动
        var playerAttacks = results
            .Where(r => r.IsPlayerSide
                     && r.Relation == CounterRelation.Advantage
                     && (r.ActionType == ActionType.Move || r.ActionType == ActionType.Counter))
            .ToList();

        // 按目标分组
        var byTarget = playerAttacks.GroupBy(r => r.TargetId);

        foreach (var group in byTarget)
        {
            // 需要至少两个不同角色同回合克制同一目标
            var distinctActors = group.Select(r => r.ActorId).Distinct().ToList();
            if (distinctActors.Count >= 2)
            {
                if (combatants.TryGetValue(group.Key, out var target) && target.IsAlive)
                {
                    StaggerService.ApplyCoopBonus(target);
                }
            }
        }
    }

    /// <summary>
    /// 过滤死亡角色，返回存活角色列表。
    /// </summary>
    public static IReadOnlyList<BattleCombatant> FilterAlive(IEnumerable<BattleCombatant> combatants)
    {
        return combatants.Where(c => c.IsAlive).ToList();
    }
}
