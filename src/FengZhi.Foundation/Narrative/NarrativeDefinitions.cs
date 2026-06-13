using FengZhi.Foundation.Mindset;

namespace FengZhi.Foundation.Narrative;

public enum NarrativeNodeType
{
    Dialogue,
    Combat,
    Arrival,
    Choice,
    Gate
}

public enum NarrativeConditionKind
{
    NodeCompleted,
    Flag,
    Chapter,
    ChoiceLogged
}

public enum NarrativeCombatOutcomeType
{
    Lethal,
    Scripted,
    NonLethal
}

public enum NarrativeCompanionEndingState
{
    Solo,
    Companion,
    Farewell
}

public sealed class NarrativeGraph
{
    public string Id { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public string EntryNode { get; init; } = string.Empty;
    public IList<NarrativeChapter> Chapters { get; init; } = new List<NarrativeChapter>();
    public IList<NarrativeNode> Nodes { get; init; } = new List<NarrativeNode>();
}

public sealed class NarrativeChapter
{
    public int Chapter { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
    public string? TransitionKey { get; init; }
    public IList<string> UnlockedLocations { get; init; } = new List<string>();
}

public sealed class NarrativeNode
{
    public string Id { get; init; } = string.Empty;
    public int Chapter { get; init; }
    public NarrativeNodeType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Next { get; init; }
    public IList<NarrativeCondition> Preconditions { get; init; } = new List<NarrativeCondition>();
    public IList<NarrativeCondition> SoftPreconditions { get; init; } = new List<NarrativeCondition>();
    public IList<NarrativeEventSpec> OnComplete { get; init; } = new List<NarrativeEventSpec>();
    public IList<NarrativeBranch> Branches { get; init; } = new List<NarrativeBranch>();
    public NarrativeTimeLimit? TimeLimit { get; init; }
    public NarrativeCombatOutcome? CombatOutcome { get; init; }
    public GateRequirement? Gate { get; init; }
}

public sealed class NarrativeCondition
{
    public NarrativeConditionKind Kind { get; init; }
    public string Key { get; init; } = string.Empty;
    public string? Value { get; init; }
}

public sealed class NarrativeEventSpec
{
    public string Type { get; init; } = string.Empty;
    public string? Key { get; init; }
    public string? Value { get; init; }
    public int? Delta { get; init; }
}

public sealed class NarrativeBranch
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Next { get; init; } = string.Empty;
    public IList<NarrativeCondition> Preconditions { get; init; } = new List<NarrativeCondition>();
    public IList<NarrativeEventSpec> Events { get; init; } = new List<NarrativeEventSpec>();
}

public sealed class NarrativeTimeLimit
{
    public int Days { get; init; }
    public string? ForcedNext { get; init; }
    public string? NormalTextKey { get; init; }
    public string? UrgentTextKey { get; init; }
    public string? FinalTextKey { get; init; }
}

public sealed class NarrativeCombatOutcome
{
    public NarrativeCombatOutcomeType Type { get; init; } = NarrativeCombatOutcomeType.Lethal;
    public string? ScriptedVictoryRewardKey { get; init; }
}

public sealed class GateRequirement
{
    public int RequiredBranches { get; init; }
    public IList<string> BranchNodeIds { get; init; } = new List<string>();
}

public sealed record ChoiceLogEntry(
    string NodeId,
    string BranchId,
    string BranchTitle);

public sealed record BreathingPeriodState(
    string SourceNodeId,
    string? NextNodeId,
    int StartedDay,
    int? GentleRemindAfterDays,
    bool ReminderTriggered);

public sealed record NarrativeTimeLimitState(
    string NodeId,
    int StartedDay,
    int Days,
    string? ForcedNext,
    bool ForcedProgressionTriggered);

public sealed record NarrativeCompanionBond(
    string HeroineId,
    NarrativeCompanionEndingState State);

public sealed record NarrativeEndingSelection(
    BaseEnding BaseEnding,
    MoralityTier MoralityTier,
    NarrativeCompanionEndingState CompanionState,
    string? CompanionId,
    string ScriptKey,
    IReadOnlyList<string> DialogueKeys);

public sealed record NarrativeEndingResolveResult(
    bool Success,
    NarrativeEndingSelection? Selection,
    string? Error)
{
    public static NarrativeEndingResolveResult Ok(NarrativeEndingSelection selection)
    {
        return new NarrativeEndingResolveResult(true, selection, null);
    }

    public static NarrativeEndingResolveResult Fail(string error)
    {
        return new NarrativeEndingResolveResult(false, null, error);
    }
}

public sealed class NarrativeRuntimeSaveData
{
    public int CurrentChapter { get; init; }
    public IList<string> ActiveNodeIds { get; init; } = new List<string>();
    public IList<string> CompletedNodeIds { get; init; } = new List<string>();
    public IList<ChoiceLogEntry> ChoiceLog { get; init; } = new List<ChoiceLogEntry>();
    public BreathingPeriodState? BreathingPeriod { get; init; }
    public IList<NarrativeTimeLimitState> TimeLimits { get; init; } = new List<NarrativeTimeLimitState>();
}
