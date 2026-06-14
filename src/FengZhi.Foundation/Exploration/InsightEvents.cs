using FengZhi.Foundation.Events;
using Godot;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Presentation-safe cue event emitted when an insight node becomes detectable.
/// </summary>
public sealed record InsightCueShownEvent(
    string NodeId,
    Vector2 Position,
    string NarrativeContext,
    DiscoveryType DiscoveryType) : GameEvent;

/// <summary>
/// Presentation-safe cue event emitted when an active insight cue is dismissed.
/// </summary>
public sealed record InsightCueHiddenEvent(string NodeId) : GameEvent;
