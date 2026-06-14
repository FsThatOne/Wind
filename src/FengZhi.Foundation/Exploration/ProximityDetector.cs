using FengZhi.Foundation.Events;
using Godot;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Adapter seam for ADR-0014's shared ConditionEvaluator.
/// </summary>
public interface IInsightConditionEvaluator
{
    /// <summary>Evaluates every prerequisite against the latest runtime state.</summary>
    bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites);
}

/// <summary>
/// Pure C# proximity detector for insight threshold checks.
/// </summary>
public sealed partial class ProximityDetector
{
    public const float DefaultMultiNodeStaggerSeconds = 1.5f;

    public const float DefaultCueLingerSeconds = 5f;

    private readonly InsightNodeRegistry _registry;
    private readonly IInsightConditionEvaluator _conditionEvaluator;
    private readonly IEventBus? _eventBus;
    private readonly float _multiNodeStaggerSeconds;
    private readonly float _cueLingerSeconds;
    private readonly Queue<string> _pendingTriggers = new();
    private readonly HashSet<string> _queuedNodeIds = new();
    private readonly Dictionary<string, float> _detectedElapsedSeconds = new();
    private float _staggerRemainingSeconds;
    private bool _isPaused;

    public ProximityDetector(
        InsightNodeRegistry registry,
        IInsightConditionEvaluator conditionEvaluator,
        IEventBus? eventBus = null,
        float multiNodeStaggerSeconds = DefaultMultiNodeStaggerSeconds,
        float cueLingerSeconds = DefaultCueLingerSeconds)
    {
        _registry = registry;
        _conditionEvaluator = conditionEvaluator;
        _eventBus = eventBus;
        _multiNodeStaggerSeconds = Math.Max(0f, multiNodeStaggerSeconds);
        _cueLingerSeconds = Math.Max(0f, cueLingerSeconds);
    }

    /// <summary>True while a combat, dialogue, or transition lock is suppressing detection.</summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Detects active nodes in range and emits cues only when prerequisites and insight threshold pass.
    /// </summary>
    public IReadOnlyList<InsightCueShownEvent> Detect(Vector2 playerPosition, int playerInsight)
    {
        if (_isPaused)
        {
            return Array.Empty<InsightCueShownEvent>();
        }

        var shown = new List<InsightCueShownEvent>();

        foreach (var node in _registry.GetActiveNodes())
        {
            if (_registry.GetState(node.Id) != DiscoveryState.Undiscovered)
            {
                continue;
            }

            if (!IsInRange(playerPosition, node))
            {
                continue;
            }

            if (!_conditionEvaluator.AreMet(node.Prerequisite))
            {
                continue;
            }

            if (playerInsight < node.InsightThreshold)
            {
                continue;
            }

            if (!_registry.TrySetState(node.Id, DiscoveryState.Detected))
            {
                continue;
            }

            var cue = new InsightCueShownEvent(
                node.Id,
                node.Position,
                node.NarrativeContext,
                node.DiscoveryType);
            shown.Add(cue);
            _eventBus?.Publish(cue);
        }

        return shown;
    }

    /// <summary>
    /// Advances deterministic cue timing for active scene nodes.
    /// </summary>
    public IReadOnlyList<InsightCueShownEvent> Tick(Vector2 playerPosition, int playerInsight, float deltaSeconds)
    {
        if (_isPaused)
        {
            return Array.Empty<InsightCueShownEvent>();
        }

        var delta = Math.Max(0f, deltaSeconds);
        ResetTransientNodesOutsideRange(playerPosition);
        AdvanceLingerTimers(delta);
        EnqueueCurrentCandidates(playerPosition);
        AdvanceStagger(delta);

        if (_staggerRemainingSeconds > 0f)
        {
            return Array.Empty<InsightCueShownEvent>();
        }

        return TryProcessNextQueuedTrigger(playerPosition, playerInsight);
    }

    /// <summary>
    /// Dismisses a detected cue without marking the node as permanently discovered.
    /// </summary>
    public bool IgnoreCue(string nodeId)
    {
        if (_registry.GetState(nodeId) != DiscoveryState.Detected)
        {
            return false;
        }

        if (!_registry.TrySetState(nodeId, DiscoveryState.Ignored))
        {
            return false;
        }

        _detectedElapsedSeconds.Remove(nodeId);
        PublishHidden(nodeId);
        return true;
    }

    /// <summary>Pauses detection and hides currently visible insight cues without clearing ignored nodes.</summary>
    public IReadOnlyList<InsightCueHiddenEvent> PauseDetection()
    {
        _isPaused = true;
        ClearPendingTriggers();
        return HideVisibleCuesForLockPause();
    }

    /// <summary>Resumes detection; callers should tick again with the latest player position.</summary>
    public void ResumeDetection()
    {
        _isPaused = false;
    }

    /// <summary>Clears scene-scoped detector state and unloads the registry's active scene nodes.</summary>
    public IReadOnlyList<InsightCueHiddenEvent> OnSceneUnloaded()
    {
        var hidden = HideActiveCuesAndResetTransientStates();
        ClearPendingTriggers();
        _registry.OnSceneUnloaded();
        return hidden;
    }

    /// <summary>Returns true when the player is inside or on the edge of the node detection radius.</summary>
    public static bool IsInRange(Vector2 playerPosition, InsightNode node)
    {
        return playerPosition.DistanceSquaredTo(node.Position) <= node.DetectionRadius * node.DetectionRadius;
    }

    private void ResetTransientNodesOutsideRange(Vector2 playerPosition)
    {
        foreach (var node in _registry.GetActiveNodes())
        {
            var state = _registry.GetState(node.Id);
            if (state is not (DiscoveryState.Detected or DiscoveryState.Ignored) || IsInRange(playerPosition, node))
            {
                continue;
            }

            _registry.TrySetState(node.Id, DiscoveryState.Undiscovered);
            _detectedElapsedSeconds.Remove(node.Id);
            _queuedNodeIds.Remove(node.Id);
            PublishHidden(node.Id);
        }
    }

    private void AdvanceLingerTimers(float deltaSeconds)
    {
        if (_cueLingerSeconds <= 0f)
        {
            return;
        }

        foreach (var nodeId in _detectedElapsedSeconds.Keys.ToArray())
        {
            if (_registry.GetState(nodeId) != DiscoveryState.Detected)
            {
                _detectedElapsedSeconds.Remove(nodeId);
                continue;
            }

            var elapsed = _detectedElapsedSeconds[nodeId] + deltaSeconds;
            if (elapsed < _cueLingerSeconds)
            {
                _detectedElapsedSeconds[nodeId] = elapsed;
                continue;
            }

            if (_registry.TrySetState(nodeId, DiscoveryState.Ignored))
            {
                PublishHidden(nodeId);
            }

            _detectedElapsedSeconds.Remove(nodeId);
        }
    }

    private void EnqueueCurrentCandidates(Vector2 playerPosition)
    {
        var candidates = _registry.GetActiveNodes()
            .Where(node => _registry.GetState(node.Id) == DiscoveryState.Undiscovered)
            .Where(node => IsInRange(playerPosition, node))
            .OrderBy(node => node.Position.DistanceSquaredTo(playerPosition))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        foreach (var node in candidates)
        {
            if (_queuedNodeIds.Add(node.Id))
            {
                _pendingTriggers.Enqueue(node.Id);
            }
        }
    }

    private void AdvanceStagger(float deltaSeconds)
    {
        if (_staggerRemainingSeconds <= 0f)
        {
            return;
        }

        _staggerRemainingSeconds = Math.Max(0f, _staggerRemainingSeconds - deltaSeconds);
    }

    private IReadOnlyList<InsightCueShownEvent> TryProcessNextQueuedTrigger(Vector2 playerPosition, int playerInsight)
    {
        while (_pendingTriggers.Count > 0)
        {
            var nodeId = _pendingTriggers.Dequeue();
            _queuedNodeIds.Remove(nodeId);

            if (!_registry.TryGetNode(nodeId, out var node) || node is null)
            {
                continue;
            }

            if (_registry.GetState(nodeId) != DiscoveryState.Undiscovered || !IsInRange(playerPosition, node))
            {
                continue;
            }

            if (!_conditionEvaluator.AreMet(node.Prerequisite) || playerInsight < node.InsightThreshold)
            {
                continue;
            }

            if (!_registry.TrySetState(nodeId, DiscoveryState.Detected))
            {
                continue;
            }

            var cue = new InsightCueShownEvent(
                node.Id,
                node.Position,
                node.NarrativeContext,
                node.DiscoveryType);
            _detectedElapsedSeconds[node.Id] = 0f;
            _staggerRemainingSeconds = _multiNodeStaggerSeconds;
            _eventBus?.Publish(cue);
            return new[] { cue };
        }

        return Array.Empty<InsightCueShownEvent>();
    }

    private void PublishHidden(string nodeId)
    {
        _eventBus?.Publish(new InsightCueHiddenEvent(nodeId));
    }

    private void ClearPendingTriggers()
    {
        _pendingTriggers.Clear();
        _queuedNodeIds.Clear();
        _detectedElapsedSeconds.Clear();
        _staggerRemainingSeconds = 0f;
    }

    private IReadOnlyList<InsightCueHiddenEvent> HideActiveCuesAndResetTransientStates()
    {
        var hidden = new List<InsightCueHiddenEvent>();
        foreach (var node in _registry.GetActiveNodes())
        {
            var state = _registry.GetState(node.Id);
            if (state is not (DiscoveryState.Detected or DiscoveryState.Ignored))
            {
                continue;
            }

            if (state == DiscoveryState.Detected)
            {
                var hiddenEvent = new InsightCueHiddenEvent(node.Id);
                hidden.Add(hiddenEvent);
                _eventBus?.Publish(hiddenEvent);
            }

            _registry.TrySetState(node.Id, DiscoveryState.Undiscovered);
        }

        _detectedElapsedSeconds.Clear();
        return hidden;
    }

    private IReadOnlyList<InsightCueHiddenEvent> HideVisibleCuesForLockPause()
    {
        var hidden = new List<InsightCueHiddenEvent>();
        foreach (var node in _registry.GetActiveNodes())
        {
            if (_registry.GetState(node.Id) != DiscoveryState.Detected)
            {
                continue;
            }

            var hiddenEvent = new InsightCueHiddenEvent(node.Id);
            hidden.Add(hiddenEvent);
            _eventBus?.Publish(hiddenEvent);
            _registry.TrySetState(node.Id, DiscoveryState.Undiscovered);
        }

        _detectedElapsedSeconds.Clear();
        return hidden;
    }
}
