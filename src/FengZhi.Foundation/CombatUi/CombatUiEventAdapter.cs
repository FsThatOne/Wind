using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 将 Core Combat 事件适配为战斗 UI 只读快照。
/// </summary>
public sealed class CombatUiEventAdapter : IDisposable
{
    public const double HudRefreshBudgetMilliseconds = 1.0;

    private readonly BattleEventBus _eventBus;
    private readonly Action<RoundStartEvent> _onRoundStart;
    private readonly Action<IntentRevealedEvent> _onIntentRevealed;
    private readonly Action<DamageDealtEvent> _onDamageDealt;
    private readonly Action<StaggerChangedEvent> _onStaggerChanged;
    private readonly Action<NeixiChangedEvent> _onNeixiChanged;
    private readonly Action<DecisiveStrikeAvailableEvent> _onDecisiveStrikeAvailable;
    private readonly Action<SynergyDeclaredEvent> _onSynergyDeclared;
    private readonly Action<RoundEndEvent> _onRoundEnd;
    private readonly Action<BattleEndEvent> _onBattleEnd;
    private readonly List<CombatUiIntentEntry> _intentEntries = new();
    private readonly Dictionary<string, CombatUiIntentDisplayEntry> _intentDisplays = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IntentVisibility> _previousIntentVisibility = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _intentStableOrder = new(StringComparer.Ordinal);
    private readonly HashSet<string> _defeatedCombatants = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CombatantResourceState> _combatantResources = new(StringComparer.Ordinal);
    private readonly List<CombatUiDamageEntry> _damageEntries = new();
    private readonly List<CombatUiStaggerEntry> _staggerEntries = new();
    private readonly List<CombatUiNeixiEntry> _neixiEntries = new();
    private readonly List<CombatUiDamageNumberDisplayEntry> _damageNumberEntries = new();
    private readonly List<CombatUiStaggerCueEntry> _staggerCueEntries = new();
    private readonly List<CombatUiSynergyCueEntry> _synergyCueEntries = new();
    private readonly List<string> _decisiveStrikeTargets = new();
    private int _damageNumberSequence;
    private bool _subscribed;
    private bool _disposed;
    private bool _isDirty;
    private int _refreshCount;
    private CombatUiTurnWarningDisplayEntry _turnWarning =
        CombatUiTurnWarningDisplayEntry.ForRound(0, false);
    private CombatUiTurnWarningKind _previousTurnWarningKind = CombatUiTurnWarningKind.Normal;

    public CombatUiEventAdapter(BattleEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _onRoundStart = HandleRoundStart;
        _onIntentRevealed = HandleIntentRevealed;
        _onDamageDealt = HandleDamageDealt;
        _onStaggerChanged = HandleStaggerChanged;
        _onNeixiChanged = HandleNeixiChanged;
        _onDecisiveStrikeAvailable = HandleDecisiveStrikeAvailable;
        _onSynergyDeclared = HandleSynergyDeclared;
        _onRoundEnd = HandleRoundEnd;
        _onBattleEnd = HandleBattleEnd;

        Layers = new[]
        {
            CombatUiLayerDescriptor.WorldIntentLayer,
            CombatUiLayerDescriptor.HudLayer
        };

        State = CombatUiState.Inactive;
        BattleResult = BattleResult.InProgress;
    }

    /// <summary>
    /// 战斗 UI 需要创建的基础层。
    /// </summary>
    public IReadOnlyList<CombatUiLayerDescriptor> Layers { get; }

    public CombatUiState State { get; private set; }

    public int RoundNumber { get; private set; }

    public BattleResult BattleResult { get; private set; }

    /// <summary>
    /// 写入单个参战者当前资源。调用方传入战斗层已结算数据，UI 只做展示绑定。
    /// </summary>
    public void UpsertCombatantResources(
        string combatantId,
        int health,
        int maxHealth,
        int neixi,
        int maxNeixi,
        int stagger,
        int staggerThreshold = CombatUiFeedbackTuning.StaggerExposureThreshold)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(combatantId))
            return;

        var previous = _combatantResources.TryGetValue(combatantId, out var existing)
            ? existing
            : null;
        var state = new CombatantResourceState(
            combatantId,
            Math.Max(0, health),
            Math.Max(1, maxHealth),
            Math.Max(0, neixi),
            Math.Max(1, maxNeixi),
            Math.Max(0, stagger),
            Math.Max(1, staggerThreshold),
            previous?.Health,
            previous?.Neixi,
            previous?.Stagger);

        _combatantResources[combatantId] = state;
        MarkDirty();
    }

    /// <summary>
    /// 从战斗角色对象复制资源状态。不会持有 BattleCombatant 引用。
    /// </summary>
    public void UpsertCombatantResources(BattleCombatant combatant)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        UpsertCombatantResources(
            combatant.Id,
            combatant.HP,
            combatant.MaxHP,
            combatant.Neixi,
            combatant.MaxNeixi,
            combatant.Stagger,
            combatant.StaggerThreshold);
    }

    /// <summary>
    /// 标记目标已落败。后续意图事件不会访问战场节点，只会让 UI 槽位变暗。
    /// </summary>
    public void MarkCombatantDefeated(string combatantId)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(combatantId))
            return;

        _defeatedCombatants.Add(combatantId);
        if (_intentDisplays.TryGetValue(combatantId, out var entry))
        {
            _intentDisplays[combatantId] = entry with
            {
                SlotState = CombatUiIntentSlotState.Dimmed,
                ShouldPlayRevealTransition = false
            };
            MarkDirty();
        }
    }

    /// <summary>
    /// 进入战斗 UI 生命周期并开始订阅战斗事件。
    /// </summary>
    public void EnterBattle()
    {
        ThrowIfDisposed();
        if (_subscribed)
            return;

        _eventBus.Subscribe(_onRoundStart);
        _eventBus.Subscribe(_onIntentRevealed);
        _eventBus.Subscribe(_onDamageDealt);
        _eventBus.Subscribe(_onStaggerChanged);
        _eventBus.Subscribe(_onNeixiChanged);
        _eventBus.Subscribe(_onDecisiveStrikeAvailable);
        _eventBus.Subscribe(_onSynergyDeclared);
        _eventBus.Subscribe(_onRoundEnd);
        _eventBus.Subscribe(_onBattleEnd);
        _subscribed = true;
        MarkDirty();
    }

    /// <summary>
    /// 如果快照已变脏，则执行一次合批刷新。
    /// </summary>
    public bool RefreshIfDirty()
    {
        ThrowIfDisposed();
        if (!_isDirty)
            return false;

        _refreshCount++;
        _isDirty = false;
        return true;
    }

    /// <summary>
    /// 获取当前 UI 只读快照。集合会复制，避免调用方回写适配器内部状态。
    /// </summary>
    public CombatUiSnapshot GetSnapshot()
    {
        ThrowIfDisposed();
        return BuildSnapshot();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _eventBus.Unsubscribe(_onRoundStart);
        _eventBus.Unsubscribe(_onIntentRevealed);
        _eventBus.Unsubscribe(_onDamageDealt);
        _eventBus.Unsubscribe(_onStaggerChanged);
        _eventBus.Unsubscribe(_onNeixiChanged);
        _eventBus.Unsubscribe(_onDecisiveStrikeAvailable);
        _eventBus.Unsubscribe(_onSynergyDeclared);
        _eventBus.Unsubscribe(_onRoundEnd);
        _eventBus.Unsubscribe(_onBattleEnd);
        _subscribed = false;
        _disposed = true;
    }

    private CombatUiSnapshot BuildSnapshot()
    {
        var orderedIntentDisplays = OrderedIntentDisplays();
        return new CombatUiSnapshot(
            State,
            RoundNumber,
            BattleResult,
            _intentEntries.ToArray(),
            orderedIntentDisplays,
            orderedIntentDisplays,
            _damageEntries.ToArray(),
            _staggerEntries.ToArray(),
            _neixiEntries.ToArray(),
            BuildResourceEntries(),
            _damageNumberEntries.ToArray(),
            _staggerCueEntries.ToArray(),
            _decisiveStrikeTargets.ToArray(),
            _synergyCueEntries.ToArray(),
            _turnWarning,
            _isDirty,
            _refreshCount);
    }

    private void HandleRoundStart(RoundStartEvent evt)
    {
        RoundNumber = evt.RoundNumber;
        State = CombatUiState.RoundStart;
        _intentEntries.Clear();
        _intentDisplays.Clear();
        _previousIntentVisibility.Clear();
        _intentStableOrder.Clear();
        _defeatedCombatants.Clear();
        _combatantResources.Clear();
        _damageEntries.Clear();
        _staggerEntries.Clear();
        _neixiEntries.Clear();
        _damageNumberEntries.Clear();
        _staggerCueEntries.Clear();
        _synergyCueEntries.Clear();
        _decisiveStrikeTargets.Clear();
        UpdateTurnWarning(evt.RoundNumber);
        MarkDirty();
    }

    private void HandleIntentRevealed(IntentRevealedEvent evt)
    {
        _intentEntries.Clear();
        var activeEnemyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var enemy in evt.Enemies)
        {
            activeEnemyIds.Add(enemy.EnemyId);
            _intentEntries.Add(new CombatUiIntentEntry(enemy.EnemyId, enemy.Visibility, enemy.MoveTypeName, enemy.MoveType));
            UpsertIntentDisplay(enemy);
        }

        RemoveAbsentIntentDisplays(activeEnemyIds);
        State = CombatUiState.PlayerDecision;
        MarkDirty();
    }

    private void RemoveAbsentIntentDisplays(HashSet<string> activeEnemyIds)
    {
        foreach (var enemyId in _intentDisplays.Keys.ToArray())
        {
            if (activeEnemyIds.Contains(enemyId))
                continue;

            _intentDisplays.Remove(enemyId);
            _previousIntentVisibility.Remove(enemyId);
        }
    }

    private void UpsertIntentDisplay(EnemyIntent enemy)
    {
        if (!_intentStableOrder.ContainsKey(enemy.EnemyId))
            _intentStableOrder[enemy.EnemyId] = _intentStableOrder.Count;

        _previousIntentVisibility.TryGetValue(enemy.EnemyId, out var previousVisibility);
        var shouldReveal = previousVisibility == IntentVisibility.Hidden
            && enemy.Visibility != IntentVisibility.Hidden;
        var slotState = _defeatedCombatants.Contains(enemy.EnemyId)
            ? CombatUiIntentSlotState.Dimmed
            : CombatUiIntentSlotState.Visible;

        _intentDisplays[enemy.EnemyId] = BuildDisplayEntry(
            enemy,
            shouldReveal && slotState == CombatUiIntentSlotState.Visible,
            slotState,
            _intentStableOrder[enemy.EnemyId]);
        _previousIntentVisibility[enemy.EnemyId] = enemy.Visibility;
    }

    private static CombatUiIntentDisplayEntry BuildDisplayEntry(
        EnemyIntent enemy,
        bool shouldPlayRevealTransition,
        CombatUiIntentSlotState slotState,
        int stableOrder)
    {
        if (enemy.Visibility == IntentVisibility.Hidden || enemy.MoveType is null)
        {
            return new CombatUiIntentDisplayEntry(
                enemy.EnemyId,
                CombatUiIntentIconKind.Unknown,
                "?",
                "intent_unknown_fog",
                enemy.Visibility,
                null,
                true,
                false,
                slotState,
                stableOrder);
        }

        var iconKind = ToIconKind(enemy.MoveType.Value);
        return new CombatUiIntentDisplayEntry(
            enemy.EnemyId,
            iconKind,
            ToGlyph(iconKind),
            ToColorKey(iconKind),
            enemy.Visibility,
            enemy.Visibility == IntentVisibility.FullReveal ? enemy.MoveTypeName : null,
            false,
            shouldPlayRevealTransition,
            slotState,
            stableOrder);
    }

    private IReadOnlyList<CombatUiIntentDisplayEntry> OrderedIntentDisplays()
    {
        return _intentDisplays.Values
            .OrderBy(entry => entry.StableOrder)
            .ToArray();
    }

    private static CombatUiIntentIconKind ToIconKind(MoveType moveType) => moveType switch
    {
        MoveType.Gang => CombatUiIntentIconKind.Gang,
        MoveType.Rou => CombatUiIntentIconKind.Rou,
        MoveType.Qiao => CombatUiIntentIconKind.Qiao,
        _ => CombatUiIntentIconKind.Unknown
    };

    private static string ToGlyph(CombatUiIntentIconKind iconKind) => iconKind switch
    {
        CombatUiIntentIconKind.Gang => "拳",
        CombatUiIntentIconKind.Rou => "水",
        CombatUiIntentIconKind.Qiao => "风",
        _ => "?"
    };

    private static string ToColorKey(CombatUiIntentIconKind iconKind) => iconKind switch
    {
        CombatUiIntentIconKind.Gang => "intent_gang_red",
        CombatUiIntentIconKind.Rou => "intent_rou_blue",
        CombatUiIntentIconKind.Qiao => "intent_qiao_green",
        _ => "intent_unknown_fog"
    };

    private void HandleDamageDealt(DamageDealtEvent evt)
    {
        _damageEntries.Add(new CombatUiDamageEntry(
            evt.SourceId,
            evt.TargetId,
            evt.Amount,
            evt.IsCrit,
            evt.IsCounter));
        _damageNumberEntries.Add(BuildDamageNumberEntry(evt, _damageNumberSequence++));
        State = CombatUiState.Resolving;
        MarkDirty();
    }

    private void HandleStaggerChanged(StaggerChangedEvent evt)
    {
        _staggerEntries.Add(new CombatUiStaggerEntry(evt.TargetId, evt.NewStagger));
        MarkStaggerChanged(evt.TargetId, evt.NewStagger);
        if (evt.NewStagger >= ResolveStaggerThreshold(evt.TargetId))
        {
            _staggerCueEntries.Add(new CombatUiStaggerCueEntry(
                evt.TargetId,
                evt.NewStagger,
                true,
                "破绽！",
                "stagger_exposed_deep_red",
                true,
                CombatUiFeedbackTuning.StaggerPulseIntervalSeconds));
        }

        State = CombatUiState.Resolving;
        MarkDirty();
    }

    private void HandleNeixiChanged(NeixiChangedEvent evt)
    {
        _neixiEntries.Add(new CombatUiNeixiEntry(evt.ActorId, evt.NewValue));
        MarkNeixiChanged(evt.ActorId, evt.NewValue);
        State = CombatUiState.Resolving;
        MarkDirty();
    }

    private void HandleDecisiveStrikeAvailable(DecisiveStrikeAvailableEvent evt)
    {
        if (!_decisiveStrikeTargets.Contains(evt.TargetId, StringComparer.Ordinal))
            _decisiveStrikeTargets.Add(evt.TargetId);

        State = CombatUiState.Resolving;
        MarkDirty();
    }

    private void HandleSynergyDeclared(SynergyDeclaredEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.TargetId))
            return;
        if (evt.SourceActorIds is null || evt.SourceActorIds.Count < 2)
            return;

        var sources = evt.SourceActorIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();
        if (sources.Length < 2)
            return;

        _synergyCueEntries.Add(new CombatUiSynergyCueEntry(
            evt.TargetId,
            sources,
            evt.RoundNumber,
            "synergy_double_fist",
            "synergy_gold",
            true,
            true,
            CombatUiFeedbackTuning.SynergyCueDurationSeconds));
        State = CombatUiState.Resolving;
        MarkDirty();
    }

    private void UpdateTurnWarning(int roundNumber)
    {
        var next = CombatUiTurnWarningDisplayEntry.ForRound(
            roundNumber,
            shouldFlashOnEnter: false);
        var shouldFlash = next.Kind != CombatUiTurnWarningKind.Normal
            && next.Kind != _previousTurnWarningKind;
        _turnWarning = shouldFlash
            ? next with { ShouldFlashOnEnter = true }
            : next;
        _previousTurnWarningKind = next.Kind;
    }

    private void HandleRoundEnd(RoundEndEvent evt)
    {
        RoundNumber = evt.RoundNumber;
        State = CombatUiState.RoundEnd;
        UpdateTurnWarning(evt.RoundNumber);
        MarkDirty();
    }

    private void HandleBattleEnd(BattleEndEvent evt)
    {
        BattleResult = evt.Result;
        State = CombatUiState.BattleEnd;
        MarkDirty();
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private IReadOnlyList<CombatUiResourceDisplayEntry> BuildResourceEntries()
    {
        return _combatantResources.Values
            .OrderBy(resource => resource.CombatantId, StringComparer.Ordinal)
            .SelectMany(BuildResourceEntries)
            .ToArray();
    }

    private static IEnumerable<CombatUiResourceDisplayEntry> BuildResourceEntries(CombatantResourceState resource)
    {
        yield return new CombatUiResourceDisplayEntry(
            resource.CombatantId,
            CombatUiResourceKind.Health,
            resource.Health,
            resource.MaxHealth,
            Ratio(resource.Health, resource.MaxHealth),
            "health_life_flame",
            "resource_health_flash_then_tween",
            resource.PreviousHealth is not null && resource.Health < resource.PreviousHealth,
            resource.PreviousHealth is not null && resource.Health != resource.PreviousHealth,
            true,
            false,
            CombatUiFeedbackTuning.ResourceBarUpdateDurationSeconds);

        yield return new CombatUiResourceDisplayEntry(
            resource.CombatantId,
            CombatUiResourceKind.Neixi,
            resource.Neixi,
            resource.MaxNeixi,
            Ratio(resource.Neixi, resource.MaxNeixi),
            "neixi_ink_blue",
            "resource_neixi_visible_change",
            false,
            resource.PreviousNeixi is not null && resource.Neixi != resource.PreviousNeixi,
            true,
            false,
            CombatUiFeedbackTuning.ResourceBarUpdateDurationSeconds);

        yield return new CombatUiResourceDisplayEntry(
            resource.CombatantId,
            CombatUiResourceKind.Stagger,
            resource.Stagger,
            resource.StaggerThreshold,
            Ratio(resource.Stagger, resource.StaggerThreshold),
            resource.Stagger >= resource.StaggerThreshold ? "stagger_exposed_deep_red" : "stagger_jade_crack",
            resource.Stagger >= resource.StaggerThreshold ? "resource_stagger_pulse" : "resource_stagger_jade_crack",
            false,
            resource.PreviousStagger is not null && resource.Stagger != resource.PreviousStagger,
            true,
            resource.Stagger >= resource.StaggerThreshold,
            resource.Stagger >= resource.StaggerThreshold
                ? CombatUiFeedbackTuning.StaggerPulseIntervalSeconds
                : CombatUiFeedbackTuning.ResourceBarUpdateDurationSeconds);
    }

    private static CombatUiDamageNumberDisplayEntry BuildDamageNumberEntry(DamageDealtEvent evt, int sequence)
    {
        var styleKind = ResolveDamageStyleKind(evt);
        var style = CombatUiDamageNumberStyle.ForKind(styleKind);
        var laneIndex = sequence % CombatUiFeedbackTuning.MaxConcurrentDamageNumbers;
        return new CombatUiDamageNumberDisplayEntry(
            sequence,
            evt.SourceId,
            evt.TargetId,
            evt.Amount,
            style.Kind,
            style.FontScale,
            style.ColorHex,
            style.HasBlackOutline,
            style.UsesMicroBurst,
            laneIndex,
            laneIndex * 14f,
            CombatUiFeedbackTuning.DamageFloatDurationSeconds);
    }

    private static CombatUiDamageNumberStyleKind ResolveDamageStyleKind(DamageDealtEvent evt)
    {
        if (evt.VisualRelation == DamageVisualRelation.Decisive)
            return CombatUiDamageNumberStyleKind.Decisive;
        if (evt.IsCrit)
            return CombatUiDamageNumberStyleKind.Critical;
        if (evt.VisualRelation == DamageVisualRelation.Advantage || evt.IsCounter)
            return CombatUiDamageNumberStyleKind.Advantage;
        if (evt.VisualRelation == DamageVisualRelation.Disadvantage)
            return CombatUiDamageNumberStyleKind.Disadvantage;
        return CombatUiDamageNumberStyleKind.Neutral;
    }

    private void MarkNeixiChanged(string actorId, int newValue)
    {
        if (!_combatantResources.TryGetValue(actorId, out var resource))
            return;

        _combatantResources[actorId] = resource with
        {
            PreviousNeixi = resource.Neixi,
            Neixi = Math.Clamp(newValue, 0, resource.MaxNeixi)
        };
    }

    private void MarkStaggerChanged(string targetId, int newValue)
    {
        if (!_combatantResources.TryGetValue(targetId, out var resource))
            return;

        _combatantResources[targetId] = resource with
        {
            PreviousStagger = resource.Stagger,
            Stagger = Math.Max(0, newValue)
        };
    }

    private int ResolveStaggerThreshold(string targetId)
    {
        return _combatantResources.TryGetValue(targetId, out var resource)
            ? resource.StaggerThreshold
            : CombatUiFeedbackTuning.StaggerExposureThreshold;
    }

    private static double Ratio(int current, int maximum)
    {
        if (maximum <= 0)
            return 0;

        return Math.Clamp((double)current / maximum, 0, 1);
    }

    private sealed record CombatantResourceState(
        string CombatantId,
        int Health,
        int MaxHealth,
        int Neixi,
        int MaxNeixi,
        int Stagger,
        int StaggerThreshold,
        int? PreviousHealth,
        int? PreviousNeixi,
        int? PreviousStagger);
}
