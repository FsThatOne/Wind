using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;
using Xunit;

namespace FengZhi.Tests.integration.exploration;

public sealed class InsightRewardDispatchTest
{
    [Fact]
    public void OnPlayerInvestigate_WhenClueDetected_SetsQuestFlagAndPublishesDiscovery()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.True(result.Succeeded);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("clue_node"));
        Assert.Equal("true", narrative.QuestFlags["narrative_clue_old_letter"]);
        Assert.Equal("exploration.inner_monologue.clue_node", Assert.Single(narrative.InnerMonologues));
        Assert.Equal("clue_node", Assert.Single(bus.Discovered).NodeId);
        Assert.Equal("clue_node", Assert.Single(bus.Hidden).NodeId);
        Assert.Empty(codePhrases.LearnedPhrases);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenCodePhraseDetected_LearnsPhraseAndPublishesDiscovery()
    {
        var registry = ActiveRegistry(CreateCodePhraseNode("phrase_node", "inn_beam_mark"));
        Assert.True(registry.TrySetState("phrase_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("phrase_node");

        Assert.True(result.Succeeded);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("phrase_node"));
        Assert.Contains("inn_beam_mark", codePhrases.LearnedPhrases);
        Assert.Equal("exploration.inner_monologue.phrase_node", Assert.Single(narrative.InnerMonologues));
        Assert.Equal(DiscoveryType.CodePhrase, Assert.Single(bus.Discovered).DiscoveryType);
        Assert.Empty(narrative.QuestFlags);
    }

    [Theory]
    [InlineData(DiscoveryState.Undiscovered)]
    [InlineData(DiscoveryState.Ignored)]
    [InlineData(DiscoveryState.Investigated)]
    public void OnPlayerInvestigate_WhenNodeNotDetected_ProducesNoSideEffects(DiscoveryState state)
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", state));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.NotDetected, result.Status);
        Assert.Equal(state, registry.GetState("clue_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenNodeUnknown_ProducesNoSideEffects()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("missing_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.UnknownNode, result.Status);
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenRewardPreflightFails_DoesNotMarkInvestigated()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort { CanSetQuestFlags = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.DownstreamUnavailable, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenMonologuePreflightFails_DoesNotDispatchReward()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort { CanPlayInnerMonologues = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.DownstreamUnavailable, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenMonologueCommitFails_DoesNotDispatchReward()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort { TryPlayInnerMonologues = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.DownstreamRejected, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenClueRewardCommitFails_DoesNotCompleteDiscovery()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "narrative_clue_old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort { TrySetQuestFlags = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.DownstreamRejected, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_node"));
        Assert.Equal("exploration.inner_monologue.clue_node", Assert.Single(narrative.InnerMonologues));
        Assert.Empty(narrative.QuestFlags);
        Assert.Empty(codePhrases.LearnedPhrases);
        Assert.Empty(bus.Discovered);
        Assert.Empty(bus.Hidden);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenCodePhraseCommitFails_DoesNotCompleteDiscovery()
    {
        var registry = ActiveRegistry(CreateCodePhraseNode("phrase_node", "inn_beam_mark"));
        Assert.True(registry.TrySetState("phrase_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort { TryLearnPhrases = false };
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("phrase_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.DownstreamRejected, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("phrase_node"));
        Assert.Equal("exploration.inner_monologue.phrase_node", Assert.Single(narrative.InnerMonologues));
        Assert.Empty(narrative.QuestFlags);
        Assert.Empty(codePhrases.LearnedPhrases);
        Assert.Empty(bus.Discovered);
        Assert.Empty(bus.Hidden);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenClueFlagHasNoRegisteredPrefix_RejectsReward()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_node", "old_letter"));
        Assert.True(registry.TrySetState("clue_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.InvalidReward, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenRepeated_DoesNotDispatchSideEffectsTwice()
    {
        var registry = ActiveRegistry(CreateCodePhraseNode("phrase_node", "inn_beam_mark"));
        Assert.True(registry.TrySetState("phrase_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var first = dispatcher.OnPlayerInvestigate("phrase_node");
        var second = dispatcher.OnPlayerInvestigate("phrase_node");

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.NotDetected, second.Status);
        Assert.Single(narrative.InnerMonologues);
        Assert.Single(codePhrases.LearnedPhrases);
        Assert.Single(bus.Discovered);
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenPhraseAlreadyKnown_TreatsDuplicateLearnAsSafeNoOp()
    {
        var registry = ActiveRegistry(CreateCodePhraseNode("phrase_node", "inn_beam_mark"));
        Assert.True(registry.TrySetState("phrase_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        Assert.True(codePhrases.LearnedPhrases.Add("inn_beam_mark"));
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("phrase_node");

        Assert.True(result.Succeeded);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("phrase_node"));
        Assert.Single(codePhrases.LearnedPhrases);
        Assert.Single(narrative.InnerMonologues);
        Assert.Single(bus.Discovered);
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenDiscoveryTypeUnsupported_FailsExplicitly()
    {
        var registry = ActiveRegistry(CreateUnsupportedNode("loot_node"));
        Assert.True(registry.TrySetState("loot_node", DiscoveryState.Detected));
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new RecordingEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("loot_node");

        Assert.False(result.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.UnsupportedDiscoveryType, result.Status);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("loot_node"));
        AssertNoSideEffects(narrative, codePhrases, bus);
    }

    private static void AssertNoSideEffects(
        RecordingNarrativePort narrative,
        RecordingCodePhraseBookPort codePhrases,
        RecordingEventBus bus)
    {
        Assert.Empty(narrative.InnerMonologues);
        Assert.Empty(narrative.QuestFlags);
        Assert.Empty(codePhrases.LearnedPhrases);
        Assert.Empty(bus.Discovered);
        Assert.Empty(bus.Hidden);
    }

    private static InsightNodeRegistry ActiveRegistry(params InsightNode[] nodes)
    {
        var registry = new InsightNodeRegistry(nodes);
        registry.OnSceneLoaded("scene_a");
        return registry;
    }

    private static InsightNode CreateClueNode(string id, string flagId)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = Vector2.Zero,
            DetectionRadius = 2f,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.Clue,
            NarrativeContext = $"exploration.inner_monologue.{id}",
            Reward = new DiscoveryReward(FlagId: flagId, FlagValue: "true")
        };
    }

    private static InsightNode CreateCodePhraseNode(string id, string phraseId)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = Vector2.Zero,
            DetectionRadius = 2f,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.CodePhrase,
            NarrativeContext = $"exploration.inner_monologue.{id}",
            Reward = new DiscoveryReward(PhraseId: phraseId)
        };
    }

    private static InsightNode CreateUnsupportedNode(string id)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = Vector2.Zero,
            DetectionRadius = 2f,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.Loot,
            NarrativeContext = $"exploration.inner_monologue.{id}",
            Reward = new DiscoveryReward(ItemId: "hidden_token", Quantity: 1)
        };
    }

    private sealed class RecordingNarrativePort : IInsightNarrativePort
    {
        public bool CanPlayInnerMonologues { get; init; } = true;

        public bool CanSetQuestFlags { get; init; } = true;

        public bool TryPlayInnerMonologues { get; init; } = true;

        public bool TrySetQuestFlags { get; init; } = true;

        public List<string> InnerMonologues { get; } = new();

        public Dictionary<string, string> QuestFlags { get; } = new(StringComparer.Ordinal);

        public bool CanPlayInnerMonologue(string narrativeContext)
        {
            return CanPlayInnerMonologues && !string.IsNullOrWhiteSpace(narrativeContext);
        }

        public bool TryPlayInnerMonologue(string narrativeContext)
        {
            if (!TryPlayInnerMonologues || !CanPlayInnerMonologue(narrativeContext))
            {
                return false;
            }

            InnerMonologues.Add(narrativeContext);
            return true;
        }

        public bool CanSetQuestFlag(string flagId)
        {
            return CanSetQuestFlags && !string.IsNullOrWhiteSpace(flagId);
        }

        public bool TrySetQuestFlag(string flagId, string value)
        {
            if (!TrySetQuestFlags || !CanSetQuestFlag(flagId))
            {
                return false;
            }

            QuestFlags[flagId] = value;
            return true;
        }
    }

    private sealed class RecordingCodePhraseBookPort : IInsightCodePhraseBookPort
    {
        public bool CanLearnPhrases { get; init; } = true;

        public bool TryLearnPhrases { get; init; } = true;

        public HashSet<string> LearnedPhrases { get; } = new(StringComparer.Ordinal);

        public bool CanLearnPhrase(string phraseId)
        {
            return CanLearnPhrases && !string.IsNullOrWhiteSpace(phraseId);
        }

        public bool TryLearnPhrase(string phraseId)
        {
            if (!TryLearnPhrases || !CanLearnPhrase(phraseId))
            {
                return false;
            }

            LearnedPhrases.Add(phraseId);
            return true;
        }
    }

    private sealed class RecordingEventBus : IEventBus
    {
        private readonly EventBus _inner = new();

        public List<InsightDiscoveredEvent> Discovered { get; } = new();

        public List<InsightCueHiddenEvent> Hidden { get; } = new();

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            if (gameEvent is InsightDiscoveredEvent discovered)
            {
                Discovered.Add(discovered);
            }
            else if (gameEvent is InsightCueHiddenEvent hidden)
            {
                Hidden.Add(hidden);
            }

            _inner.Publish(gameEvent);
        }

        public Action Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            return _inner.Subscribe(handler);
        }
    }
}
