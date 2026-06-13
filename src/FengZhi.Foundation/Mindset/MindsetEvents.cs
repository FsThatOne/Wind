using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Mindset;

public sealed record MindsetShiftedEvent(
    MindsetAxis Axis,
    int OldValue,
    int NewValue,
    int Delta) : GameEvent;

public sealed record MindsetZoneChangedEvent(
    MindsetZone OldZone,
    MindsetZone NewZone) : GameEvent;

public sealed record MoralityTierChangedEvent(
    MoralityTier OldTier,
    MoralityTier NewTier) : GameEvent;

public sealed record ReputationTierChangedEvent(
    MoralityTier OldTier,
    MoralityTier NewTier) : GameEvent;
