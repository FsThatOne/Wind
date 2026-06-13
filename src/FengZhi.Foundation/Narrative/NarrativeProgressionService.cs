using FengZhi.Foundation.Events;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Mindset;
using FengZhi.Foundation.SaveSystem;
using System.Text.Json;

namespace FengZhi.Foundation.Narrative;

public sealed class NarrativeRuntimeState
{
    private readonly HashSet<string> _activeNodeIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedNodeIds = new(StringComparer.Ordinal);
    private readonly List<ChoiceLogEntry> _choiceLog = new();
    private readonly Dictionary<string, NarrativeTimeLimitState> _timeLimitsByNodeId = new(StringComparer.Ordinal);

    public int CurrentChapter { get; private set; }
    public IReadOnlySet<string> ActiveNodeIds => _activeNodeIds;
    public IReadOnlySet<string> CompletedNodeIds => _completedNodeIds;
    public IReadOnlyList<ChoiceLogEntry> ChoiceLog => _choiceLog;
    public BreathingPeriodState? BreathingPeriod { get; private set; }
    public IReadOnlyDictionary<string, NarrativeTimeLimitState> TimeLimitsByNodeId => _timeLimitsByNodeId;
    public bool IsInBreathingPeriod => BreathingPeriod != null;

    public bool IsActive(string nodeId) => _activeNodeIds.Contains(nodeId);

    public bool IsCompleted(string nodeId) => _completedNodeIds.Contains(nodeId);

    public bool HasChoice(string nodeId, string branchId)
    {
        return _choiceLog.Any(entry =>
            entry.NodeId == nodeId &&
            entry.BranchId == branchId);
    }

    public bool HasActiveTimeLimit(string nodeId)
    {
        return _timeLimitsByNodeId.ContainsKey(nodeId);
    }

    public NarrativeTimeLimitState? GetTimeLimit(string nodeId)
    {
        return _timeLimitsByNodeId.GetValueOrDefault(nodeId);
    }

    internal void Activate(NarrativeNode node)
    {
        _activeNodeIds.Add(node.Id);
        if (node.Chapter > CurrentChapter)
            CurrentChapter = node.Chapter;
    }

    internal void Complete(NarrativeNode node)
    {
        _activeNodeIds.Remove(node.Id);
        _completedNodeIds.Add(node.Id);
        _timeLimitsByNodeId.Remove(node.Id);
        if (node.Chapter > CurrentChapter)
            CurrentChapter = node.Chapter;
    }

    internal void LogChoice(ChoiceLogEntry entry)
    {
        _choiceLog.Add(entry);
    }

    internal void EnterBreathing(BreathingPeriodState state)
    {
        BreathingPeriod = state;
    }

    internal BreathingPeriodState? ExitBreathing()
    {
        var previous = BreathingPeriod;
        BreathingPeriod = null;
        return previous;
    }

    internal void MarkBreathingReminderTriggered()
    {
        if (BreathingPeriod == null)
            return;

        BreathingPeriod = BreathingPeriod with { ReminderTriggered = true };
    }

    internal void StartTimeLimit(NarrativeTimeLimitState state)
    {
        _timeLimitsByNodeId[state.NodeId] = state;
    }

    internal void MarkForcedProgressionTriggered(string nodeId)
    {
        if (!_timeLimitsByNodeId.TryGetValue(nodeId, out var state))
            return;

        _timeLimitsByNodeId[nodeId] = state with { ForcedProgressionTriggered = true };
    }

    internal NarrativeRuntimeSaveData CreateSaveData()
    {
        return new NarrativeRuntimeSaveData
        {
            CurrentChapter = CurrentChapter,
            ActiveNodeIds = _activeNodeIds.ToArray(),
            CompletedNodeIds = _completedNodeIds.ToArray(),
            ChoiceLog = _choiceLog.ToArray(),
            BreathingPeriod = BreathingPeriod,
            TimeLimits = _timeLimitsByNodeId.Values.ToArray()
        };
    }

    internal void Restore(NarrativeRuntimeSaveData data)
    {
        _activeNodeIds.Clear();
        _completedNodeIds.Clear();
        _choiceLog.Clear();
        _timeLimitsByNodeId.Clear();

        CurrentChapter = data.CurrentChapter;
        foreach (var nodeId in data.ActiveNodeIds)
            _activeNodeIds.Add(nodeId);
        foreach (var nodeId in data.CompletedNodeIds)
            _completedNodeIds.Add(nodeId);
        _choiceLog.AddRange(data.ChoiceLog);
        BreathingPeriod = data.BreathingPeriod;
        foreach (var timeLimit in data.TimeLimits)
            _timeLimitsByNodeId[timeLimit.NodeId] = timeLimit;
    }
}

public interface INarrativeConditionEvaluator
{
    bool IsMet(NarrativeCondition condition, NarrativeRuntimeState state);
}

public sealed class DefaultNarrativeConditionEvaluator : INarrativeConditionEvaluator
{
    private readonly IReadOnlySet<string> _flags;

    public DefaultNarrativeConditionEvaluator(IReadOnlySet<string>? flags = null)
    {
        _flags = flags ?? new HashSet<string>();
    }

    public bool IsMet(NarrativeCondition condition, NarrativeRuntimeState state)
    {
        return condition.Kind switch
        {
            NarrativeConditionKind.NodeCompleted => state.IsCompleted(condition.Key),
            NarrativeConditionKind.Flag => _flags.Contains(condition.Key),
            NarrativeConditionKind.Chapter => int.TryParse(condition.Value ?? condition.Key, out var chapter) &&
                                              state.CurrentChapter >= chapter,
            NarrativeConditionKind.ChoiceLogged => state.HasChoice(condition.Key, condition.Value ?? string.Empty),
            _ => false
        };
    }
}

public sealed class NarrativeProgressionService : ISaveable
{
    public const string NarrativeSaveKey = "main-narrative";

    private readonly NarrativeGraph _graph;
    private readonly IEventBus _eventBus;
    private readonly INarrativeConditionEvaluator _conditionEvaluator;
    private readonly Dictionary<string, NarrativeNode> _nodesById;
    private readonly Dictionary<int, NarrativeChapter> _chaptersByNumber;

    public NarrativeProgressionService(
        NarrativeGraph graph,
        IEventBus eventBus,
        INarrativeConditionEvaluator? conditionEvaluator = null,
        NarrativeRuntimeState? state = null)
    {
        NarrativeGraphValidator.ThrowIfInvalid(graph);

        _graph = graph;
        _eventBus = eventBus;
        _conditionEvaluator = conditionEvaluator ?? new DefaultNarrativeConditionEvaluator();
        _nodesById = graph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        _chaptersByNumber = graph.Chapters.ToDictionary(chapter => chapter.Chapter);
        State = state ?? new NarrativeRuntimeState();
    }

    public NarrativeRuntimeState State { get; }

    public string SaveKey => NarrativeSaveKey;

    public NarrativeNode? GetNode(string nodeId)
    {
        return _nodesById.GetValueOrDefault(nodeId);
    }

    public bool Initialize(int currentDay = 0)
    {
        return TryActivateNode(_graph.EntryNode, currentDay);
    }

    public bool CompleteNode(string nodeId, int currentDay = 0)
    {
        if (!_nodesById.TryGetValue(nodeId, out var node))
            return false;
        if (!State.IsActive(nodeId) || State.IsCompleted(nodeId))
            return false;
        if (node.Type == NarrativeNodeType.Gate && !CanCompleteGate(node))
            return false;

        State.Complete(node);
        _eventBus.Publish(new NarrativeNodeCompletedEvent(_graph.Id, node.Id, node.Type, node.Chapter));

        if (TryEnterBreathing(node, currentDay))
            return true;

        if (!string.IsNullOrWhiteSpace(node.Next))
            TryActivateNode(node.Next, currentDay);

        return true;
    }

    public bool SelectBranch(string nodeId, string branchId, int currentDay = 0)
    {
        if (!_nodesById.TryGetValue(nodeId, out var node))
            return false;
        if (node.Type != NarrativeNodeType.Choice || !State.IsActive(nodeId))
            return false;

        var branch = node.Branches.FirstOrDefault(candidate => candidate.Id == branchId);
        if (branch == null || !AreConditionsMet(branch.Preconditions))
            return false;

        var entry = new ChoiceLogEntry(node.Id, branch.Id, branch.Title);
        State.LogChoice(entry);
        _eventBus.Publish(new NarrativeChoiceLoggedEvent(_graph.Id, node.Id, branch.Id, branch.Title));

        State.Complete(node);
        _eventBus.Publish(new NarrativeNodeCompletedEvent(_graph.Id, node.Id, node.Type, node.Chapter));

        TryActivateNode(branch.Next, currentDay);
        return true;
    }

    public bool HandleCombatOutcome(string nodeId, BattleResult result, int currentDay = 0)
    {
        if (!_nodesById.TryGetValue(nodeId, out var node))
            return false;
        if (node.Type != NarrativeNodeType.Combat || !State.IsActive(nodeId))
            return false;

        var outcomeType = node.CombatOutcome?.Type ?? NarrativeCombatOutcomeType.Lethal;
        return outcomeType switch
        {
            NarrativeCombatOutcomeType.Lethal => HandleLethalCombatOutcome(node, result, currentDay),
            NarrativeCombatOutcomeType.Scripted => HandleScriptedCombatOutcome(node, result, currentDay),
            NarrativeCombatOutcomeType.NonLethal => HandleNonLethalCombatOutcome(node, result, currentDay),
            _ => false
        };
    }

    public bool TryActivateNode(string nodeId, int currentDay = 0)
    {
        return TryActivateNode(nodeId, allowDuringBreathing: false, currentDay);
    }

    public bool TriggerNextFromBreathing(int currentDay = 0)
    {
        var breathing = State.ExitBreathing();
        if (breathing == null)
            return false;

        _eventBus.Publish(new NarrativeBreathingExitedEvent(_graph.Id, breathing.SourceNodeId, breathing.NextNodeId));

        if (string.IsNullOrWhiteSpace(breathing.NextNodeId))
            return true;

        return TryActivateNode(breathing.NextNodeId, allowDuringBreathing: true, currentDay);
    }

    public bool CheckBreathingReminder(int currentDay)
    {
        var breathing = State.BreathingPeriod;
        if (breathing == null || breathing.ReminderTriggered || !breathing.GentleRemindAfterDays.HasValue)
            return false;

        if (currentDay - breathing.StartedDay < breathing.GentleRemindAfterDays.Value)
            return false;

        State.MarkBreathingReminderTriggered();
        _eventBus.Publish(new NarrativeBreathingReminderEvent(
            _graph.Id,
            breathing.SourceNodeId,
            breathing.NextNodeId,
            currentDay));

        return true;
    }

    public bool CheckTimeLimits(int currentDay)
    {
        foreach (var timeLimit in State.TimeLimitsByNodeId.Values.ToArray())
        {
            if (timeLimit.ForcedProgressionTriggered || GetRemainingDays(timeLimit.NodeId, currentDay) > 0)
                continue;

            State.MarkForcedProgressionTriggered(timeLimit.NodeId);
            _eventBus.Publish(new NarrativeForcedProgressionEvent(
                _graph.Id,
                timeLimit.NodeId,
                timeLimit.ForcedNext,
                currentDay));

            if (!_nodesById.TryGetValue(timeLimit.NodeId, out var sourceNode))
                return true;

            State.Complete(sourceNode);
            _eventBus.Publish(new NarrativeNodeCompletedEvent(_graph.Id, sourceNode.Id, sourceNode.Type, sourceNode.Chapter));

            if (!string.IsNullOrWhiteSpace(timeLimit.ForcedNext))
                TryActivateNode(timeLimit.ForcedNext, allowDuringBreathing: false, currentDay);

            return true;
        }

        return false;
    }

    public int? GetRemainingDays(string nodeId, int currentDay)
    {
        var state = State.GetTimeLimit(nodeId);
        if (state == null)
            return null;

        return state.Days - (currentDay - state.StartedDay);
    }

    public string? GetTimeLimitDialogueKey(string nodeId, int currentDay)
    {
        if (!_nodesById.TryGetValue(nodeId, out var node) || node.TimeLimit == null || !State.HasActiveTimeLimit(nodeId))
            return null;

        var remainingDays = GetRemainingDays(nodeId, currentDay);
        if (!remainingDays.HasValue)
            return null;

        if (remainingDays.Value <= 1)
            return ResolveTimeLimitTextKey(node, node.TimeLimit.FinalTextKey, "final");

        var urgentThreshold = Math.Max(2, (int)Math.Ceiling(node.TimeLimit.Days / 3.0));
        if (remainingDays.Value <= urgentThreshold)
            return ResolveTimeLimitTextKey(node, node.TimeLimit.UrgentTextKey, "urgent");

        return ResolveTimeLimitTextKey(node, node.TimeLimit.NormalTextKey, "normal");
    }

    public SaveSnapshot Serialize()
    {
        return new SaveSnapshot
        {
            Values = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["runtime"] = JsonSerializer.SerializeToElement(State.CreateSaveData())
            }
        };
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        if (!snapshot.Values.TryGetValue("runtime", out var runtimeElement))
            return;

        var data = runtimeElement.Deserialize<NarrativeRuntimeSaveData>();
        if (data == null)
            return;

        State.Restore(data);
    }

    public NarrativeEndingResolveResult ResolveEnding(
        EndingResult ending,
        IEnumerable<NarrativeCompanionBond> companionBonds)
    {
        var activeBonds = companionBonds
            .Where(bond => bond.State == NarrativeCompanionEndingState.Companion)
            .ToArray();
        if (activeBonds.Length > 1)
            return NarrativeEndingResolveResult.Fail("结缘状态不互斥。");

        var companionState = NarrativeCompanionEndingState.Solo;
        string? companionId = null;
        if (ending.BaseEnding != BaseEnding.MoDao && activeBonds.Length == 1)
        {
            companionState = NarrativeCompanionEndingState.Companion;
            companionId = activeBonds[0].HeroineId;
        }

        var scriptKey = BuildEndingScriptKey(ending.BaseEnding, companionState, companionId);
        var dialogueKeys = State.ChoiceLog
            .Select(choice => $"ending.choice.{choice.NodeId}.{choice.BranchId}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return NarrativeEndingResolveResult.Ok(new NarrativeEndingSelection(
            ending.BaseEnding,
            ending.MoralityTier,
            companionState,
            companionId,
            scriptKey,
            dialogueKeys));
    }

    private bool TryActivateNode(string nodeId, bool allowDuringBreathing, int currentDay)
    {
        if (!_nodesById.TryGetValue(nodeId, out var node))
            return false;
        if (State.IsInBreathingPeriod && !allowDuringBreathing)
            return false;
        if (State.IsActive(nodeId) || State.IsCompleted(nodeId))
            return false;
        if (State.CurrentChapter > 0 && node.Chapter < State.CurrentChapter)
            return false;
        if (!ArePreconditionsMet(node))
            return false;

        var oldChapter = State.CurrentChapter;
        State.Activate(node);
        if (oldChapter > 0 && node.Chapter > oldChapter)
            PublishChapterChange(oldChapter, node.Chapter);

        _eventBus.Publish(new NarrativeNodeActivatedEvent(_graph.Id, node.Id, node.Type, node.Chapter));
        TryStartTimeLimit(node, currentDay);
        return true;
    }

    public bool ArePreconditionsMet(NarrativeNode node)
    {
        return AreConditionsMet(node.Preconditions);
    }

    public bool AreSoftPreconditionsMet(NarrativeNode node)
    {
        return AreConditionsMet(node.SoftPreconditions);
    }

    public bool CanCompleteGate(string nodeId)
    {
        return _nodesById.TryGetValue(nodeId, out var node) && CanCompleteGate(node);
    }

    private bool CanCompleteGate(NarrativeNode node)
    {
        if (node.Type != NarrativeNodeType.Gate)
            return true;
        if (node.Gate == null)
            return false;

        var completedBranches = node.Gate.BranchNodeIds.Count(State.IsCompleted);
        return completedBranches >= node.Gate.RequiredBranches;
    }

    private bool AreConditionsMet(IEnumerable<NarrativeCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!_conditionEvaluator.IsMet(condition, State))
                return false;
        }

        return true;
    }

    private bool HandleLethalCombatOutcome(NarrativeNode node, BattleResult result, int currentDay)
    {
        if (result == BattleResult.Victory)
            return CompleteNode(node.Id, currentDay);

        if (!IsDefeatResult(result))
            return false;

        _eventBus.Publish(new NarrativeDeathEndingRequestedEvent(_graph.Id, node.Id, result));
        return true;
    }

    private bool HandleScriptedCombatOutcome(NarrativeNode node, BattleResult result, int currentDay)
    {
        var rewardKey = node.CombatOutcome?.ScriptedVictoryRewardKey;
        if (result == BattleResult.Victory && !string.IsNullOrWhiteSpace(rewardKey))
            _eventBus.Publish(new NarrativeScriptedCombatRewardEvent(_graph.Id, node.Id, rewardKey));

        return CompleteNode(node.Id, currentDay);
    }

    private bool HandleNonLethalCombatOutcome(NarrativeNode node, BattleResult result, int currentDay)
    {
        var branchId = ToCombatChoiceBranchId(result);
        var entry = new ChoiceLogEntry(node.Id, branchId, result.ToString());
        State.LogChoice(entry);
        _eventBus.Publish(new NarrativeChoiceLoggedEvent(_graph.Id, node.Id, branchId, result.ToString()));

        return CompleteNode(node.Id, currentDay);
    }

    private static bool IsDefeatResult(BattleResult result)
    {
        return result is BattleResult.Defeat or BattleResult.NarrowDefeat;
    }

    private static string ToCombatChoiceBranchId(BattleResult result)
    {
        return result switch
        {
            BattleResult.Victory => "combat_victory",
            BattleResult.Defeat => "combat_defeat",
            BattleResult.NarrowDefeat => "combat_narrow_defeat",
            BattleResult.Draw => "combat_draw",
            _ => "combat_unknown"
        };
    }

    private bool TryEnterBreathing(NarrativeNode node, int currentDay)
    {
        var spec = node.OnComplete.FirstOrDefault(e =>
            string.Equals(e.Type, "enter_breathing", StringComparison.OrdinalIgnoreCase));

        if (spec == null)
            return false;

        var nextNodeId = !string.IsNullOrWhiteSpace(spec.Value) ? spec.Value : node.Next;
        var gentleRemindAfterDays = spec.Delta;

        State.EnterBreathing(new BreathingPeriodState(
            node.Id,
            nextNodeId,
            currentDay,
            gentleRemindAfterDays,
            ReminderTriggered: false));

        _eventBus.Publish(new NarrativeBreathingEnteredEvent(
            _graph.Id,
            node.Id,
            nextNodeId,
            currentDay,
            gentleRemindAfterDays));

        return true;
    }

    private void TryStartTimeLimit(NarrativeNode node, int currentDay)
    {
        if (node.TimeLimit == null || node.TimeLimit.Days <= 0 || State.HasActiveTimeLimit(node.Id))
            return;

        State.StartTimeLimit(new NarrativeTimeLimitState(
            node.Id,
            currentDay,
            node.TimeLimit.Days,
            node.TimeLimit.ForcedNext,
            ForcedProgressionTriggered: false));

        _eventBus.Publish(new NarrativeTimeLimitStartedEvent(
            _graph.Id,
            node.Id,
            currentDay,
            node.TimeLimit.Days,
            node.TimeLimit.ForcedNext));
    }

    private static string ResolveTimeLimitTextKey(NarrativeNode node, string? configuredKey, string suffix)
    {
        return string.IsNullOrWhiteSpace(configuredKey)
            ? $"{node.Id}.time_limit.{suffix}"
            : configuredKey;
    }

    private static string BuildEndingScriptKey(
        BaseEnding baseEnding,
        NarrativeCompanionEndingState companionState,
        string? companionId)
    {
        var endingKey = ToSnakeCase(baseEnding.ToString());
        var companionKey = companionState == NarrativeCompanionEndingState.Companion && !string.IsNullOrWhiteSpace(companionId)
            ? companionId
            : companionState.ToString().ToLowerInvariant();
        return $"ending.{endingKey}.{companionKey}";
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('_');
            chars.Add(char.ToLowerInvariant(c));
        }

        return new string(chars.ToArray());
    }

    private void PublishChapterChange(int oldChapter, int newChapter)
    {
        _chaptersByNumber.TryGetValue(newChapter, out var chapter);
        var title = chapter?.Title ?? string.Empty;

        _eventBus.Publish(new NarrativeChapterChangedEvent(_graph.Id, oldChapter, newChapter, title));

        if (!string.IsNullOrWhiteSpace(chapter?.Tone))
            _eventBus.Publish(new NarrativeToneChangedEvent(_graph.Id, newChapter, chapter.Tone));

        if (!string.IsNullOrWhiteSpace(chapter?.TransitionKey))
            _eventBus.Publish(new NarrativeChapterTransitionRequestedEvent(_graph.Id, oldChapter, newChapter, chapter.TransitionKey));

        if (chapter == null)
            return;

        foreach (var locationId in chapter.UnlockedLocations)
            _eventBus.Publish(new NarrativeLocationUnlockedEvent(_graph.Id, newChapter, locationId));
    }
}
