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

/// <summary>
/// Event emitted after an insight node has been permanently investigated.
/// </summary>
public sealed record InsightDiscoveredEvent(
    string NodeId,
    DiscoveryType DiscoveryType,
    string NarrativeContext) : GameEvent;

/// <summary>
/// Two-phase monologue lifecycle: emitted when an investigate request begins narrative playback.
/// Presentation layer should prepare monologue display upon receiving this event.
/// </summary>
public sealed record MonologueRequestPendingEvent(string NodeId) : GameEvent;

/// <summary>
/// Two-phase monologue lifecycle: emitted when reward dispatch succeeds and monologue is confirmed.
/// Presentation layer should commit the displayed monologue.
/// </summary>
public sealed record MonologueRequestCommittedEvent(string NodeId) : GameEvent;

/// <summary>
/// Two-phase monologue lifecycle: emitted when reward dispatch fails after monologue was requested.
/// Presentation layer should dismiss/cancel any pending monologue display.
/// </summary>
public sealed record MonologueRequestCanceledEvent(string NodeId) : GameEvent;
