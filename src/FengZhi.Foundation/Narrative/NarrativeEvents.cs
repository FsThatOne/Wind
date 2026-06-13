using FengZhi.Foundation.Events;
using FengZhi.Foundation.Combat;

namespace FengZhi.Foundation.Narrative;

public sealed record NarrativeNodeActivatedEvent(
    string GraphId,
    string NodeId,
    NarrativeNodeType NodeType,
    int Chapter) : GameEvent;

public sealed record NarrativeNodeCompletedEvent(
    string GraphId,
    string NodeId,
    NarrativeNodeType NodeType,
    int Chapter) : GameEvent;

public sealed record NarrativeChoiceLoggedEvent(
    string GraphId,
    string NodeId,
    string BranchId,
    string BranchTitle) : GameEvent;

public sealed record NarrativeChapterChangedEvent(
    string GraphId,
    int OldChapter,
    int NewChapter,
    string ChapterTitle) : GameEvent;

public sealed record NarrativeToneChangedEvent(
    string GraphId,
    int Chapter,
    string Tone) : GameEvent;

public sealed record NarrativeLocationUnlockedEvent(
    string GraphId,
    int Chapter,
    string LocationId) : GameEvent;

public sealed record NarrativeChapterTransitionRequestedEvent(
    string GraphId,
    int OldChapter,
    int NewChapter,
    string TransitionKey) : GameEvent;

public sealed record NarrativeBreathingEnteredEvent(
    string GraphId,
    string SourceNodeId,
    string? NextNodeId,
    int StartedDay,
    int? GentleRemindAfterDays) : GameEvent;

public sealed record NarrativeBreathingExitedEvent(
    string GraphId,
    string SourceNodeId,
    string? NextNodeId) : GameEvent;

public sealed record NarrativeBreathingReminderEvent(
    string GraphId,
    string SourceNodeId,
    string? NextNodeId,
    int CurrentDay) : GameEvent;

public sealed record NarrativeTimeLimitStartedEvent(
    string GraphId,
    string NodeId,
    int StartedDay,
    int Days,
    string? ForcedNextNodeId) : GameEvent;

public sealed record NarrativeForcedProgressionEvent(
    string GraphId,
    string SourceNodeId,
    string? ForcedNextNodeId,
    int CurrentDay) : GameEvent;

public sealed record NarrativeDeathEndingRequestedEvent(
    string GraphId,
    string CombatNodeId,
    BattleResult Result) : GameEvent;

public sealed record NarrativeScriptedCombatRewardEvent(
    string GraphId,
    string CombatNodeId,
    string RewardKey) : GameEvent;
