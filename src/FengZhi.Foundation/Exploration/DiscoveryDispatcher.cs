using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Boundary used by exploration discovery to request narrative side effects without owning narrative state.
/// </summary>
public interface IInsightNarrativePort
{
    /// <summary>Returns whether the narrative layer can play the configured inner monologue.</summary>
    bool CanPlayInnerMonologue(string narrativeContext);

    /// <summary>Attempts to request playback of the configured inner monologue after preflight has succeeded.</summary>
    bool TryPlayInnerMonologue(string narrativeContext);

    /// <summary>Returns whether the narrative layer can set the configured quest flag.</summary>
    bool CanSetQuestFlag(string flagId);

    /// <summary>Attempts to set a narrative-owned quest flag after preflight has succeeded.</summary>
    bool TrySetQuestFlag(string flagId, string value);
}

/// <summary>
/// Boundary used by exploration discovery to teach code phrases without owning the phrase book.
/// </summary>
public interface IInsightCodePhraseBookPort
{
    /// <summary>Returns whether the phrase book can learn or safely ignore the configured phrase.</summary>
    bool CanLearnPhrase(string phraseId);

    /// <summary>Attempts to learn the configured phrase id after preflight has succeeded; duplicates are safe no-ops.</summary>
    bool TryLearnPhrase(string phraseId);
}

/// <summary>
/// Outcome category for a player investigate request.
/// </summary>
public enum DiscoveryDispatchStatus
{
    Success,
    UnknownNode,
    InactiveNode,
    NotDetected,
    InvalidReward,
    UnsupportedDiscoveryType,
    DownstreamUnavailable,
    DownstreamRejected,
    StateCommitFailed
}

/// <summary>
/// Result returned by <see cref="DiscoveryDispatcher.OnPlayerInvestigate"/> for tests and adapters.
/// </summary>
public sealed record DiscoveryDispatchResult(
    bool Succeeded,
    DiscoveryDispatchStatus Status,
    string NodeId,
    string Message)
{
    public static DiscoveryDispatchResult Success(string nodeId)
    {
        return new DiscoveryDispatchResult(true, DiscoveryDispatchStatus.Success, nodeId, "Discovery dispatched.");
    }

    public static DiscoveryDispatchResult Failure(string nodeId, DiscoveryDispatchStatus status, string message)
    {
        return new DiscoveryDispatchResult(false, status, nodeId, message);
    }
}

/// <summary>
/// Dispatches detected insight nodes into narrative and reward side effects.
/// </summary>
public sealed class DiscoveryDispatcher
{
    private const string InsightFlagPrefix = "insight_";
    private const string NarrativeFlagPrefix = "narrative_";

    private readonly InsightNodeRegistry _registry;
    private readonly IInsightNarrativePort _narrativePort;
    private readonly IInsightCodePhraseBookPort _codePhraseBookPort;
    private readonly IEventBus? _eventBus;

    public DiscoveryDispatcher(
        InsightNodeRegistry registry,
        IInsightNarrativePort narrativePort,
        IInsightCodePhraseBookPort codePhraseBookPort,
        IEventBus? eventBus = null)
    {
        _registry = registry;
        _narrativePort = narrativePort;
        _codePhraseBookPort = codePhraseBookPort;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Investigates one detected insight node and dispatches its configured reward atomically at the state layer.
    /// </summary>
    public DiscoveryDispatchResult OnPlayerInvestigate(string nodeId)
    {
        if (!_registry.TryGetNode(nodeId, out var node) || node is null)
        {
            return DiscoveryDispatchResult.Failure(
                nodeId,
                DiscoveryDispatchStatus.UnknownNode,
                "Insight node is not registered.");
        }

        if (!_registry.GetActiveNodes().Any(active => active.Id == nodeId))
        {
            return DiscoveryDispatchResult.Failure(
                nodeId,
                DiscoveryDispatchStatus.InactiveNode,
                "Insight node is not active in the current scene.");
        }

        if (_registry.GetState(nodeId) != DiscoveryState.Detected)
        {
            return DiscoveryDispatchResult.Failure(
                nodeId,
                DiscoveryDispatchStatus.NotDetected,
                "Only detected insight nodes can be investigated.");
        }

        var preflight = ValidatePreconditions(node);
        if (!preflight.Succeeded)
        {
            return preflight;
        }

        var dispatch = DispatchSideEffects(node);
        if (!dispatch.Succeeded)
        {
            return dispatch;
        }

        if (!_registry.TrySetState(nodeId, DiscoveryState.Investigated))
        {
            return DiscoveryDispatchResult.Failure(
                nodeId,
                DiscoveryDispatchStatus.StateCommitFailed,
                "Insight node state could not be committed.");
        }

        _eventBus?.Publish(new InsightDiscoveredEvent(node.Id, node.DiscoveryType, node.NarrativeContext));
        _eventBus?.Publish(new InsightCueHiddenEvent(node.Id));
        return DiscoveryDispatchResult.Success(nodeId);
    }

    private DiscoveryDispatchResult ValidatePreconditions(InsightNode node)
    {
        if (string.IsNullOrWhiteSpace(node.NarrativeContext)
            || !_narrativePort.CanPlayInnerMonologue(node.NarrativeContext))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamUnavailable,
                "Narrative inner monologue cannot be played.");
        }

        return node.DiscoveryType switch
        {
            DiscoveryType.Clue => ValidateClueReward(node),
            DiscoveryType.CodePhrase => ValidateCodePhraseReward(node),
            _ => DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.UnsupportedDiscoveryType,
                $"Discovery type {node.DiscoveryType} is not supported by this story.")
        };
    }

    private DiscoveryDispatchResult ValidateClueReward(InsightNode node)
    {
        var flagId = node.Reward.FlagId;
        if (!IsRegisteredFlag(flagId))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.InvalidReward,
                "Clue reward flag must use an insight_ or narrative_ registered prefix.");
        }

        if (!_narrativePort.CanSetQuestFlag(flagId))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamUnavailable,
                "Narrative quest flag cannot be set.");
        }

        return DiscoveryDispatchResult.Success(node.Id);
    }

    private DiscoveryDispatchResult ValidateCodePhraseReward(InsightNode node)
    {
        var phraseId = node.Reward.PhraseId;
        if (string.IsNullOrWhiteSpace(phraseId))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.InvalidReward,
                "CodePhrase reward must include a phrase id.");
        }

        if (!_codePhraseBookPort.CanLearnPhrase(phraseId))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamUnavailable,
                "CodePhraseBook cannot learn the configured phrase.");
        }

        return DiscoveryDispatchResult.Success(node.Id);
    }

    private DiscoveryDispatchResult DispatchSideEffects(InsightNode node)
    {
        if (!_narrativePort.TryPlayInnerMonologue(node.NarrativeContext))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamRejected,
                "Narrative inner monologue playback was rejected after preflight.");
        }

        var rewardDispatch = node.DiscoveryType switch
        {
            DiscoveryType.Clue => DispatchClue(node),
            DiscoveryType.CodePhrase => DispatchCodePhrase(node),
            _ => DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.UnsupportedDiscoveryType,
                $"Discovery type {node.DiscoveryType} is not supported by this story.")
        };
        if (!rewardDispatch.Succeeded)
        {
            return rewardDispatch;
        }

        return DiscoveryDispatchResult.Success(node.Id);
    }

    private DiscoveryDispatchResult DispatchClue(InsightNode node)
    {
        if (!_narrativePort.TrySetQuestFlag(node.Reward.FlagId, node.Reward.FlagValue))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamRejected,
                "Narrative quest flag write was rejected after preflight.");
        }

        return DiscoveryDispatchResult.Success(node.Id);
    }

    private DiscoveryDispatchResult DispatchCodePhrase(InsightNode node)
    {
        if (!_codePhraseBookPort.TryLearnPhrase(node.Reward.PhraseId))
        {
            return DiscoveryDispatchResult.Failure(
                node.Id,
                DiscoveryDispatchStatus.DownstreamRejected,
                "CodePhraseBook learn was rejected after preflight.");
        }

        return DiscoveryDispatchResult.Success(node.Id);
    }

    private static bool IsRegisteredFlag(string flagId)
    {
        return !string.IsNullOrWhiteSpace(flagId)
            && (flagId.StartsWith(InsightFlagPrefix, StringComparison.Ordinal)
                || flagId.StartsWith(NarrativeFlagPrefix, StringComparison.Ordinal));
    }
}
