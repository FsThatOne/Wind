using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.Runtime;

/// <summary>
/// VS Sprint 7 交互式战斗循环控制器（MVP-A 范围）。
///
/// 与 BattleFacade.RunFullBattle 的区别：
/// - RunFullBattle 是同步全自动，所有 playerDecisions 预先填入队列
/// - 本 controller 在 PlayerDecision 阶段 **等待** 外部调用 SubmitPlayerIntent
///   适合 Godot scene 内一帧帧推进的交互场景
///
/// 事件流（基于 BattleEventBus）：
/// 1. Start()  → 推到 IntentReveal → 发 RoundStartEvent + IntentRevealedEvent → 推到 PlayerDecision → 设 WaitingForPlayer=true
/// 2. SubmitPlayerIntent(action) →
///    a. 推到 Resolution → 收集（player cached + enemy AI）+ ResolutionService.ResolveRound
///    b. 为每个 ResolvedAction 发 DamageDealtEvent + StaggerChangedEvent + NeixiChangedEvent
///    c. 推到 RoundEnd → 发 RoundEndEvent → 推到下个 RoundStart 或 BattleOver
///    d. 若 BattleOver → 发 BattleEndEvent；否则重入循环（从 IntentReveal 开始）
///
/// 详见 docs/superpowers/specs/2026-06-23-s7-vs-combat-loop-mvp-a.md §4 Subtask 2 / §5。
/// </summary>
public sealed class VsBattleLoopController
{
    private readonly BattleInstance _battle;
    private readonly BattleEventBus _bus;
    private readonly IBattleAI _enemyAI;
    private readonly CounterService _counterService;
    private readonly ResolutionService _resolutionService;

    /// <summary>本回合敌方 AI 已 peek 的 actions（在 IntentReveal 阶段缓存，Resolution 阶段消费）。</summary>
    private List<BattleAction>? _cachedEnemyActions;
    /// <summary>本回合玩家提交的 action（SubmitPlayerIntent 时缓存；null 表示尚未提交）。</summary>
    private BattleAction? _cachedPlayerAction;

    /// <summary>当前是否在等待玩家输入。</summary>
    public bool WaitingForPlayer { get; private set; }

    /// <summary>是否已结束（BattleOver）。</summary>
    public bool IsFinished => _battle.CurrentPhase == BattlePhase.BattleOver;

    /// <summary>战斗结果（BattleOver 后才有意义）。</summary>
    public BattleResult Result => _battle.Result;

    /// <summary>暴露给 view binder / UI 的只读战斗实例。</summary>
    public BattleInstance Battle => _battle;

    public VsBattleLoopController(
        BattleInstance battle,
        BattleEventBus bus,
        IBattleAI enemyAI,
        IDamageRandomSource? random = null)
    {
        _battle = battle;
        _bus = bus;
        _enemyAI = enemyAI;
        _counterService = new CounterService();
        _resolutionService = new ResolutionService(_counterService, random ?? new DefaultDamageRandom());
    }

    /// <summary>
    /// 启动战斗：初始化 → 首回合 RoundStart → IntentReveal → 等待玩家输入。
    /// </summary>
    public void Start()
    {
        if (_battle.CurrentPhase != BattlePhase.Initializing)
        {
            throw new InvalidOperationException(
                $"Start() called from non-Initializing phase: {_battle.CurrentPhase}");
        }

        EnterNextRoundUntilPlayerDecision();
    }

    /// <summary>
    /// 玩家提交意图（招式 / 反制 / 决胜 / 调息 / 普通攻击）。
    /// </summary>
    /// <returns>true 表示接受并推进；false 表示当前未等待玩家输入或战斗已结束。</returns>
    public bool SubmitPlayerIntent(BattleAction action)
    {
        if (!WaitingForPlayer || IsFinished)
        {
            return false;
        }

        _cachedPlayerAction = action;
        WaitingForPlayer = false;

        ResolveRoundAndAdvance();

        if (!IsFinished)
        {
            EnterNextRoundUntilPlayerDecision();
        }

        return true;
    }

    /// <summary>
    /// 从 Initializing 或 RoundStart/RoundEnd 推进到 PlayerDecision（含发布对应事件）。
    /// </summary>
    private void EnterNextRoundUntilPlayerDecision()
    {
        // 推到 RoundStart（从 Initializing 来）或保持（从 RoundEnd 已经 advance 到 RoundStart 来）
        if (_battle.CurrentPhase == BattlePhase.Initializing)
        {
            _battle.AdvancePhase(); // Init → RoundStart
        }

        // RoundStart：发 RoundStartEvent，并 peek 敌方 AI
        if (_battle.CurrentPhase == BattlePhase.RoundStart)
        {
            _bus.Publish(new RoundStartEvent(_battle.CurrentRound));
            PublishResourceSnapshots();
            _battle.AdvancePhase(); // → IntentReveal
        }

        // IntentReveal：peek 敌方 AI 缓存，并发 IntentRevealedEvent
        if (_battle.CurrentPhase == BattlePhase.IntentReveal)
        {
            _cachedEnemyActions = PeekEnemyActions();
            _bus.Publish(BuildIntentRevealedEvent(_cachedEnemyActions));
            _battle.AdvancePhase(); // → PlayerDecision
        }

        // PlayerDecision：等待
        if (_battle.CurrentPhase == BattlePhase.PlayerDecision)
        {
            WaitingForPlayer = true;
            _cachedPlayerAction = null;
        }
    }

    /// <summary>
    /// 收集行动 → 结算 → 发事件 → 推到下个 RoundStart 或 BattleOver。
    /// </summary>
    private void ResolveRoundAndAdvance()
    {
        _battle.AdvancePhase(); // PlayerDecision → Resolution

        var allActions = CollectAllActions();
        var combatantLookup = _battle.PlayerParty
            .Concat(_battle.EnemyGroup)
            .ToDictionary(c => c.Id);
        var playerIds = new HashSet<string>(_battle.PlayerParty.Select(p => p.Id));

        var results = _resolutionService.ResolveRound(allActions, combatantLookup, playerIds);

        PublishResolveEvents(results);
        PublishResourceSnapshots();

        _battle.AdvancePhase(); // Resolution → RoundEnd
        _bus.Publish(new RoundEndEvent(_battle.CurrentRound));

        _battle.AdvancePhase(); // RoundEnd → RoundStart (next) or BattleOver

        if (_battle.CurrentPhase == BattlePhase.BattleOver)
        {
            _bus.Publish(new BattleEndEvent(_battle.Result));
        }
    }

    /// <summary>
    /// peek 敌方 AI（消费一次）。在 IntentReveal 阶段调用。
    /// 重要：ScriptedAI 用 Queue.Dequeue，故"peek"实际是 "consume + cache"。
    /// </summary>
    private List<BattleAction> PeekEnemyActions()
    {
        var actions = new List<BattleAction>();
        foreach (var enemy in _battle.EnemyGroup.Where(c => c.IsAlive))
        {
            actions.Add(_enemyAI.DecideAction(enemy, _battle));
        }
        return actions;
    }

    /// <summary>
    /// 组合玩家 + 敌方缓存 action 为 ResolveRound 输入。
    /// </summary>
    private List<BattleAction> CollectAllActions()
    {
        var all = new List<BattleAction>();
        if (_cachedPlayerAction != null)
        {
            all.Add(_cachedPlayerAction);
        }
        else
        {
            // 兜底：第一个活着的玩家调息
            var firstPlayer = _battle.PlayerParty.FirstOrDefault(c => c.IsAlive);
            if (firstPlayer != null)
            {
                all.Add(new BattleAction { ActorId = firstPlayer.Id, Type = ActionType.Breathe });
            }
        }

        if (_cachedEnemyActions != null)
        {
            all.AddRange(_cachedEnemyActions);
        }

        return all;
    }

    /// <summary>
    /// 发布意图揭示事件（基于敌方 cached actions）。
    /// </summary>
    private static IntentRevealedEvent BuildIntentRevealedEvent(IReadOnlyList<BattleAction> enemyActions)
    {
        var intents = enemyActions.Select(a => new EnemyIntent(
            EnemyId: a.ActorId,
            Visibility: IntentVisibility.Normal, // 仅体系（MoveType）级别可见
            MoveTypeName: a.MoveType?.ToString(),
            MoveType: a.MoveType
        )).ToList();
        return new IntentRevealedEvent(intents);
    }

    /// <summary>
    /// 为本回合结算的 actions 发布 Damage / Stagger 事件。
    /// </summary>
    private void PublishResolveEvents(IReadOnlyList<ResolvedAction> results)
    {
        foreach (var r in results)
        {
            if (r.DamageDealt > 0 && !string.IsNullOrEmpty(r.TargetId) && r.TargetId != r.ActorId)
            {
                _bus.Publish(new DamageDealtEvent(
                    SourceId: r.ActorId,
                    TargetId: r.TargetId,
                    Amount: r.DamageDealt,
                    IsCrit: r.IsCrit,
                    IsCounter: r.ActionType == ActionType.Counter,
                    VisualRelation: MapVisualRelation(r)));
            }
        }
    }

    /// <summary>
    /// 为所有角色发布资源快照（HP/Neixi/Stagger）。
    /// </summary>
    private void PublishResourceSnapshots()
    {
        foreach (var combatant in _battle.PlayerParty.Concat(_battle.EnemyGroup))
        {
            _bus.Publish(new NeixiChangedEvent(combatant.Id, combatant.Neixi));
            _bus.Publish(new StaggerChangedEvent(combatant.Id, combatant.Stagger));
        }
    }

    /// <summary>
    /// ResolvedAction → DamageVisualRelation 映射。
    /// </summary>
    private static DamageVisualRelation MapVisualRelation(ResolvedAction r)
    {
        if (r.ActionType == ActionType.Decisive) return DamageVisualRelation.Decisive;
        return r.Relation switch
        {
            MartialArts.CounterRelation.Advantage => DamageVisualRelation.Advantage,
            MartialArts.CounterRelation.Disadvantage => DamageVisualRelation.Disadvantage,
            _ => DamageVisualRelation.Neutral,
        };
    }
}
