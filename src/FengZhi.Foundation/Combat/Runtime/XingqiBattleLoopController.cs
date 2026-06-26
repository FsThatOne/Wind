using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Combat.Xingqi;

namespace FengZhi.Foundation.Combat.Runtime;

public enum TurnSubPhase
{
    None,
    WaitingForMovement,
    WaitingForAction,
}

/// <summary>
/// 行气脉冲驱动的交互式战斗循环控制器。
/// 每个脉冲推进所有角色行气 → 满条角色按 F2 排序逐一行动 → 玩家方暂停等输入。
/// </summary>
public sealed class XingqiBattleLoopController
{
    private readonly IReadOnlyList<BattleCombatant> _playerParty;
    private readonly IReadOnlyList<BattleCombatant> _enemyGroup;
    private readonly BattleEventBus _bus;
    private readonly IBattleAI _enemyAI;
    private readonly ResolutionService _resolutionService;
    private readonly XingqiPulseEngine _pulseEngine;
    private readonly XingqiConfig _config;
    private readonly BattleGrid? _grid;

    private readonly Dictionary<string, XingqiGauge> _gauges = new();
    private readonly HashSet<string> _playerSideIds = new();
    private readonly Dictionary<string, BattleCombatant> _combatantLookup = new();

    private List<string> _pendingActors = new();
    private int _pendingIndex;
    private int _pulseNumber;
    private IReadOnlySet<GridPosition>? _currentReachable;

    public bool WaitingForPlayer { get; private set; }
    public bool IsFinished { get; private set; }
    public BattleResult Result { get; private set; } = BattleResult.InProgress;
    public string? CurrentActorId { get; private set; }
    public int PulseNumber => _pulseNumber;
    public TurnSubPhase CurrentSubPhase { get; private set; }

    public IReadOnlyList<BattleCombatant> PlayerParty => _playerParty;
    public IReadOnlyList<BattleCombatant> EnemyGroup => _enemyGroup;
    public IReadOnlyDictionary<string, XingqiGauge> Gauges => _gauges;
    public BattleGrid? Grid => _grid;

    public XingqiBattleLoopController(
        IReadOnlyList<BattleCombatant> playerParty,
        IReadOnlyList<BattleCombatant> enemyGroup,
        BattleEventBus bus,
        IBattleAI enemyAI,
        XingqiConfig? config = null,
        IDamageRandomSource? random = null,
        BattleGrid? grid = null)
    {
        _playerParty = playerParty;
        _enemyGroup = enemyGroup;
        _bus = bus;
        _enemyAI = enemyAI;
        _config = config ?? new XingqiConfig();
        _pulseEngine = new XingqiPulseEngine(_config);
        _grid = grid;

        var counterService = new CounterService();
        _resolutionService = new ResolutionService(counterService, random ?? new DefaultDamageRandom());

        foreach (var c in _playerParty)
        {
            _playerSideIds.Add(c.Id);
            _combatantLookup[c.Id] = c;
        }
        foreach (var c in _enemyGroup)
        {
            _combatantLookup[c.Id] = c;
        }
    }

    public void Start()
    {
        foreach (var c in _playerParty.Concat(_enemyGroup))
        {
            _gauges[c.Id] = new XingqiGauge(c.Id, 0);
        }

        if (_grid != null)
        {
            foreach (var c in _playerParty.Concat(_enemyGroup))
            {
                _grid.SetOccupant(c.Position, c.Id);
            }
        }

        _pulseNumber = 0;
        RunPulseLoop();
    }

    /// <summary>
    /// 玩家提交移动选择。仅在 WaitingForMovement 时有效。
    /// null 或当前位置 = 原地停留。
    /// </summary>
    public bool SubmitPlayerMovement(GridPosition? moveTarget)
    {
        if (!WaitingForPlayer || IsFinished) return false;
        if (CurrentSubPhase != TurnSubPhase.WaitingForMovement) return false;
        if (CurrentActorId == null) return false;

        var actor = _combatantLookup[CurrentActorId];
        var target = moveTarget ?? actor.Position;

        if (target != actor.Position)
        {
            if (_currentReachable == null || !_currentReachable.Contains(target))
                return false;

            ExecuteMovement(actor, target);
        }

        CurrentSubPhase = TurnSubPhase.WaitingForAction;
        return true;
    }

    /// <summary>
    /// 玩家提交行动意图。仅在 WaitingForAction 时有效（或无 grid 时为 WaitingForPlayer 兼容模式）。
    /// </summary>
    public bool SubmitPlayerIntent(BattleAction action)
    {
        if (!WaitingForPlayer || IsFinished) return false;
        if (_grid != null && CurrentSubPhase != TurnSubPhase.WaitingForAction) return false;
        if (CurrentActorId == null) return false;

        WaitingForPlayer = false;
        CurrentSubPhase = TurnSubPhase.None;
        _currentReachable = null;

        var actor = _combatantLookup[CurrentActorId];
        ResolveAction(actor, action);
        ResetGaugeAfterAction(CurrentActorId);

        var endResult = CheckEndCondition();
        if (endResult != BattleResult.InProgress)
        {
            FinishBattle(endResult);
            return true;
        }

        _pendingIndex++;
        ProcessNextInQueue();
        return true;
    }

    private void RunPulseLoop()
    {
        var allCombatants = _playerParty.Concat(_enemyGroup).ToList();

        while (!IsFinished)
        {
            _pulseNumber++;
            var readyIds = _pulseEngine.AdvancePulse(allCombatants, _gauges, _playerSideIds);
            PublishXingqiSnapshots();

            if (readyIds.Count == 0) continue;

            _pendingActors = new List<string>(readyIds);
            _pendingIndex = 0;
            ProcessNextInQueue();
            return;
        }
    }

    private void ProcessNextInQueue()
    {
        while (_pendingIndex < _pendingActors.Count)
        {
            var actorId = _pendingActors[_pendingIndex];
            if (!_combatantLookup.TryGetValue(actorId, out var actor) || !actor.IsAlive)
            {
                _pendingIndex++;
                continue;
            }

            CurrentActorId = actorId;
            bool isPlayer = _playerSideIds.Contains(actorId);
            _bus.Publish(new ActorTurnStartedEvent(actorId, isPlayer));

            if (isPlayer)
            {
                WaitingForPlayer = true;
                if (_grid != null)
                {
                    _currentReachable = MovementService.GetReachableCells(_grid, actor.Position, actor.MoveRange);
                    CurrentSubPhase = TurnSubPhase.WaitingForMovement;
                    _bus.Publish(new MovementRangeCalculatedEvent(actorId, _currentReachable, actor.Position));
                }
                else
                {
                    CurrentSubPhase = TurnSubPhase.WaitingForAction;
                }
                return;
            }

            ExecuteAITurn(actor);
            _pendingIndex++;

            var endResult = CheckEndCondition();
            if (endResult != BattleResult.InProgress)
            {
                FinishBattle(endResult);
                return;
            }
        }

        _pendingActors.Clear();
        CurrentActorId = null;

        if (!IsFinished)
        {
            RunPulseLoop();
        }
    }

    private void ExecuteAITurn(BattleCombatant actor)
    {
        if (_grid != null)
        {
            var reachable = MovementService.GetReachableCells(_grid, actor.Position, actor.MoveRange);
            var aiMoveTarget = _enemyAI.DecideMovement(actor, _grid, reachable);
            if (aiMoveTarget.HasValue && aiMoveTarget.Value != actor.Position && reachable.Contains(aiMoveTarget.Value))
            {
                ExecuteMovement(actor, aiMoveTarget.Value);
            }
        }

        var battle = BuildMinimalBattleInstance();
        var action = _enemyAI.DecideAction(actor, battle);
        ResolveAction(actor, action);
        ResetGaugeAfterAction(actor.Id);
    }

    private void ExecuteMovement(BattleCombatant actor, GridPosition target)
    {
        var from = actor.Position;
        var path = MovementService.FindPath(_grid!, from, target, actor.MoveRange);
        if (path == null) path = new[] { from, target };

        if (_grid != null)
        {
            _grid.SetOccupant(from, null);
            _grid.SetOccupant(target, actor.Id);
        }

        actor.MoveTo(target);
        var newFacing = GridPosition.InferFacing(from, target);
        actor.SetFacing(newFacing);

        _bus.Publish(new ActorMovedEvent(actor.Id, from, target, path, newFacing));
    }

    private void ResolveAction(BattleCombatant actor, BattleAction action)
    {
        var actions = new List<BattleAction> { action };
        var results = _resolutionService.ResolveRound(actions, _combatantLookup, _playerSideIds);

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

            if (!string.IsNullOrEmpty(r.TargetId) && _combatantLookup.TryGetValue(r.TargetId, out var target))
            {
                _bus.Publish(new StaggerChangedEvent(r.TargetId, target.Stagger));
            }
        }

        _bus.Publish(new NeixiChangedEvent(actor.Id, actor.Neixi));
    }

    private void ResetGaugeAfterAction(string actorId)
    {
        if (_gauges.TryGetValue(actorId, out var gauge))
        {
            gauge.Reset(0f, _config.XingqiThreshold, _config.RetainedXingqiCap);
        }
    }

    private BattleResult CheckEndCondition()
    {
        bool allPlayersDead = _playerParty.All(c => !c.IsAlive);
        bool allEnemiesDead = _enemyGroup.All(c => !c.IsAlive);

        if (allPlayersDead && allEnemiesDead) return BattleResult.NarrowDefeat;
        if (allEnemiesDead) return BattleResult.Victory;
        if (allPlayersDead) return BattleResult.Defeat;
        return BattleResult.InProgress;
    }

    private void FinishBattle(BattleResult result)
    {
        Result = result;
        IsFinished = true;
        CurrentActorId = null;
        WaitingForPlayer = false;
        CurrentSubPhase = TurnSubPhase.None;
        _bus.Publish(new BattleEndEvent(result));
    }

    private void PublishXingqiSnapshots()
    {
        var snapshots = new List<XingqiSnapshot>();
        foreach (var c in _playerParty.Concat(_enemyGroup))
        {
            if (!_gauges.TryGetValue(c.Id, out var gauge)) continue;
            snapshots.Add(new XingqiSnapshot(
                c.Id,
                gauge.CurrentValue,
                _config.XingqiThreshold,
                gauge.IsReady(_config.XingqiThreshold)));
        }
        _bus.Publish(new XingqiAdvancedEvent(snapshots, _pulseNumber));
    }

    private BattleInstance BuildMinimalBattleInstance()
    {
        return new BattleInstance(_playerParty, _enemyGroup);
    }

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
