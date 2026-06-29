using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;
using Xunit;

namespace FengZhi.Tests.unit.exploration;

public sealed class EiDebtPaydownTest
{
    [Fact]
    public void Detect_DelegatesToTick_RegistersLingerImmediately()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 5f);

#pragma warning disable CS0618
        var shown = detector.Detect(Vector2.Zero, 10);
#pragma warning restore CS0618

        Assert.Equal("node_a", Assert.Single(shown).NodeId);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));

        detector.Tick(Vector2.Zero, 10, 5f);
        Assert.Equal(DiscoveryState.Ignored, registry.GetState("node_a"));
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void Detect_AndTick_ProduceSameResultForSameNode()
    {
        var registryA = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var busA = new DebtPaydownEventBus();
        var detectorA = CreateDetector(registryA, busA, stagger: 0f, linger: 5f);

        var registryB = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var busB = new DebtPaydownEventBus();
        var detectorB = CreateDetector(registryB, busB, stagger: 0f, linger: 5f);

#pragma warning disable CS0618
        var shownDetect = detectorA.Detect(Vector2.Zero, 10);
#pragma warning restore CS0618
        var shownTick = detectorB.Tick(Vector2.Zero, 10, 0f);

        Assert.Equal(shownDetect.Count, shownTick.Count);
        Assert.Equal(shownDetect[0].NodeId, shownTick[0].NodeId);
        Assert.Equal(registryA.GetState("node_a"), registryB.GetState("node_a"));
    }

    [Fact]
    public void Tick_WithDeltaZero_DoesNotAdvanceLingerTimer()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 5f);

        detector.Tick(Vector2.Zero, 10, 0f);
        detector.Tick(Vector2.Zero, 10, 0f);
        detector.Tick(Vector2.Zero, 10, 0f);

        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
        Assert.Empty(bus.Hidden);
    }

    [Fact]
    public void Tick_WhenNodeAlreadyInvestigated_EmitsNoEvents()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        registry.TrySetState("node_a", DiscoveryState.Investigated);
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 5f);

        var shown = detector.Tick(Vector2.Zero, 10, 1f);

        Assert.Empty(shown);
        Assert.Empty(bus.Shown);
        Assert.Empty(bus.Hidden);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenRewardSucceeds_PublishesPendingThenCommitted()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_a", "insight_test_flag"));
        registry.TrySetState("clue_a", DiscoveryState.Detected);
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new DebtPaydownEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_a");

        Assert.True(result.Succeeded);
        Assert.Equal("clue_a", Assert.Single(bus.MonologuePending).NodeId);
        Assert.Equal("clue_a", Assert.Single(bus.MonologueCommitted).NodeId);
        Assert.Empty(bus.MonologueCanceled);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("clue_a"));
    }

    [Fact]
    public void OnPlayerInvestigate_WhenRewardFails_PublishesPendingThenCanceled()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_a", "insight_test_flag"));
        registry.TrySetState("clue_a", DiscoveryState.Detected);
        var narrative = new RecordingNarrativePort { TrySetQuestFlags = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new DebtPaydownEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_a");

        Assert.False(result.Succeeded);
        Assert.Equal("clue_a", Assert.Single(bus.MonologuePending).NodeId);
        Assert.Equal("clue_a", Assert.Single(bus.MonologueCanceled).NodeId);
        Assert.Empty(bus.MonologueCommitted);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenRewardFails_NodeRemainsDETECTED()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_a", "insight_test_flag"));
        registry.TrySetState("clue_a", DiscoveryState.Detected);
        var narrative = new RecordingNarrativePort { TrySetQuestFlags = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new DebtPaydownEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        dispatcher.OnPlayerInvestigate("clue_a");

        Assert.Equal(DiscoveryState.Detected, registry.GetState("clue_a"));
    }

    [Fact]
    public void OnPlayerInvestigate_WhenPreflightFails_NoPendingEventPublished()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_a", "insight_test_flag"));
        registry.TrySetState("clue_a", DiscoveryState.Detected);
        var narrative = new RecordingNarrativePort { CanPlayInnerMonologues = false };
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new DebtPaydownEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var result = dispatcher.OnPlayerInvestigate("clue_a");

        Assert.False(result.Succeeded);
        Assert.Empty(bus.MonologuePending);
        Assert.Empty(bus.MonologueCommitted);
        Assert.Empty(bus.MonologueCanceled);
    }

    [Fact]
    public void OnPlayerInvestigate_WhenNodeAlreadyInvestigated_SecondCallFails()
    {
        var registry = ActiveRegistry(CreateClueNode("clue_a", "insight_test_flag"));
        registry.TrySetState("clue_a", DiscoveryState.Detected);
        var narrative = new RecordingNarrativePort();
        var codePhrases = new RecordingCodePhraseBookPort();
        var bus = new DebtPaydownEventBus();
        var dispatcher = new DiscoveryDispatcher(registry, narrative, codePhrases, bus);

        var first = dispatcher.OnPlayerInvestigate("clue_a");
        Assert.True(first.Succeeded);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("clue_a"));

        var second = dispatcher.OnPlayerInvestigate("clue_a");
        Assert.False(second.Succeeded);
        Assert.Equal(DiscoveryDispatchStatus.NotDetected, second.Status);
        Assert.Single(bus.MonologuePending);
        Assert.Single(bus.MonologueCommitted);
    }

    [Fact]
    public void HideEvent_WhenNodeLeavesRange_PublishedOnlyOnce()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero, radius: 1f));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 10f);

        detector.Tick(Vector2.Zero, 10, 0f);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));

        detector.Tick(new Vector2(5, 0), 10, 0f);
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void HideEvent_WhenIgnoredNodeLeavesRange_PublishedOnlyOnce()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero, radius: 1f));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 10f);

        detector.Tick(Vector2.Zero, 10, 0f);
        detector.IgnoreCue("node_a");
        bus.Hidden.Clear();

        detector.Tick(new Vector2(5, 0), 10, 0f);
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void HideEvent_OnSceneUnloaded_NoDuplicateForAlreadyHiddenNode()
    {
        var registry = ActiveRegistry(
            CreateNode("node_a", Vector2.Zero, radius: 1f),
            CreateNode("node_b", new Vector2(0.5f, 0), radius: 1f));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 10f);

        detector.Tick(Vector2.Zero, 10, 0f);
        detector.Tick(Vector2.Zero, 10, 0f);

        detector.OnSceneUnloaded();

        var hiddenIds = bus.Hidden.Select(h => h.NodeId).ToList();
        Assert.Equal(hiddenIds.Distinct().Count(), hiddenIds.Count);
    }

    [Fact]
    public void HideEvent_PauseDetection_NoDuplicateWithinSameCycle()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new DebtPaydownEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 10f);

        detector.Tick(Vector2.Zero, 10, 0f);
        detector.PauseDetection();

        Assert.Single(bus.Hidden.Where(h => h.NodeId == "node_a"));
    }

    private static ProximityDetector CreateDetector(
        InsightNodeRegistry registry,
        DebtPaydownEventBus bus,
        float stagger,
        float linger)
    {
        return new ProximityDetector(
            registry,
            new StubConditionEvaluator(true),
            bus,
            multiNodeStaggerSeconds: stagger,
            cueLingerSeconds: linger);
    }

    private static InsightNodeRegistry ActiveRegistry(params InsightNode[] nodes)
    {
        var registry = new InsightNodeRegistry(nodes);
        registry.OnSceneLoaded("scene_a");
        return registry;
    }

    private static InsightNode CreateNode(string id, Vector2 position, float radius = 5f)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = position,
            DetectionRadius = radius,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.EnvironmentDetail,
            NarrativeContext = $"exploration.inner_monologue.{id}"
        };
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

    private sealed class StubConditionEvaluator : IInsightConditionEvaluator
    {
        public StubConditionEvaluator(bool result) => Result = result;
        public bool Result { get; }
        public bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites) => Result;
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
            => CanPlayInnerMonologues && !string.IsNullOrWhiteSpace(narrativeContext);

        public bool TryPlayInnerMonologue(string narrativeContext)
        {
            if (!TryPlayInnerMonologues || !CanPlayInnerMonologue(narrativeContext)) return false;
            InnerMonologues.Add(narrativeContext);
            return true;
        }

        public bool CanSetQuestFlag(string flagId)
            => CanSetQuestFlags && !string.IsNullOrWhiteSpace(flagId);

        public bool TrySetQuestFlag(string flagId, string value)
        {
            if (!TrySetQuestFlags || !CanSetQuestFlag(flagId)) return false;
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
            => CanLearnPhrases && !string.IsNullOrWhiteSpace(phraseId);

        public bool TryLearnPhrase(string phraseId)
        {
            if (!TryLearnPhrases || !CanLearnPhrase(phraseId)) return false;
            LearnedPhrases.Add(phraseId);
            return true;
        }
    }

    private sealed class DebtPaydownEventBus : IEventBus
    {
        private readonly EventBus _inner = new();
        public List<InsightCueShownEvent> Shown { get; } = new();
        public List<InsightCueHiddenEvent> Hidden { get; } = new();
        public List<MonologueRequestPendingEvent> MonologuePending { get; } = new();
        public List<MonologueRequestCommittedEvent> MonologueCommitted { get; } = new();
        public List<MonologueRequestCanceledEvent> MonologueCanceled { get; } = new();

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            switch (gameEvent)
            {
                case InsightCueShownEvent shown: Shown.Add(shown); break;
                case InsightCueHiddenEvent hidden: Hidden.Add(hidden); break;
                case MonologueRequestPendingEvent pending: MonologuePending.Add(pending); break;
                case MonologueRequestCommittedEvent committed: MonologueCommitted.Add(committed); break;
                case MonologueRequestCanceledEvent canceled: MonologueCanceled.Add(canceled); break;
            }
            _inner.Publish(gameEvent);
        }

        public Action Subscribe<T>(Action<T> handler) where T : GameEvent
            => _inner.Subscribe(handler);
    }
}
