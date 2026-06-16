using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Requests an attitude delta so Romance can apply milestone floor rules before NPC State mutates.
/// </summary>
public sealed record AttitudeChangeRequestEvent(
    string NpcId,
    int Delta,
    string Source
) : GameEvent;

/// <summary>
/// Published after Romance applies a request and writes the final attitude through NPC State.
/// </summary>
public sealed record RomanceAttitudeChangedEvent(
    string NpcId,
    int Delta,
    int Current,
    int Proposed,
    int Clamped,
    string Source
) : GameEvent;

/// <summary>
/// Published when Romance writes a normal milestone through NPC State.
/// </summary>
public sealed record RomanceMilestoneUnlockedEvent(
    string NpcId,
    RomanceMilestone Milestone,
    string Source
) : GameEvent;

/// <summary>
/// Published when force break places an NPC into the terminal broken state.
/// </summary>
public sealed record RomanceForceBreakEvent(
    string NpcId,
    string Source
) : GameEvent;

/// <summary>
/// Published when a heroine bond is confirmed after the player accepts the choice.
/// </summary>
public sealed record RomanceBondConfirmedEvent(
    string NpcId,
    string Source
) : GameEvent;

/// <summary>
/// Published when a heroine bond choice is declined and locked from repeat triggers.
/// </summary>
public sealed record RomanceBondDeclinedEvent(
    string NpcId,
    string DeclineFlag,
    string Source
) : GameEvent;
