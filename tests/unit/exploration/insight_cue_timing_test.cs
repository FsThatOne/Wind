using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;
using Xunit;

namespace FengZhi.Tests.unit.exploration;

public sealed class InsightCueTimingTest
{
    [Fact]
    public void Tick_WhenMultipleNodesEnterRange_TriggersNearestFirstWithStagger()
    {
        var registry = ActiveRegistry(
            CreateNode("far", new Vector2(3, 0)),
            CreateNode("near", new Vector2(1, 0)),
            CreateNode("middle", new Vector2(2, 0)));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1.5f, linger: 10f);

        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0f), "near");
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 1.49f));
        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0.01f), "middle");
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 1.49f));
        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0.01f), "far");

        Assert.Equal(new[] { "near", "middle", "far" }, bus.Shown.Select(cue => cue.NodeId).ToArray());
    }

    [Fact]
    public void Tick_WhenNodesHaveEqualDistance_UsesStableIdTieBreaker()
    {
        var registry = ActiveRegistry(
            CreateNode("node_b", new Vector2(1, 0)),
            CreateNode("node_a", new Vector2(-1, 0)));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1f, linger: 10f);

        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0f), "node_a");
        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 1f), "node_b");
    }

    [Fact]
    public void Tick_WhenCueLingerExpires_HidesCueAndSetsIgnored()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1f, linger: 5f);

        detector.Tick(Vector2.Zero, 10, 0f);
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 4.99f));
        detector.Tick(Vector2.Zero, 10, 0.01f);

        Assert.Equal(DiscoveryState.Ignored, registry.GetState("node_a"));
        Assert.Equal("node_a", Assert.Single(bus.Hidden).NodeId);
    }

    [Fact]
    public void Tick_WhenDetectedNodeBecomesInvestigatedBeforeLinger_DoesNotHideOrDowngrade()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1f, linger: 5f);

        detector.Tick(Vector2.Zero, 10, 0f);
        Assert.True(registry.TrySetState("node_a", DiscoveryState.Investigated));
        detector.Tick(Vector2.Zero, 10, 5f);

        Assert.Equal(DiscoveryState.Investigated, registry.GetState("node_a"));
        Assert.Empty(bus.Hidden);
    }

    [Fact]
    public void IgnoreCue_WhenNodeDetected_HidesCueAndSetsIgnoredWithoutDiscovery()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 5f);

        detector.Tick(Vector2.Zero, 10, 0f);
        var ignored = detector.IgnoreCue("node_a");

        Assert.True(ignored);
        Assert.Equal(DiscoveryState.Ignored, registry.GetState("node_a"));
        Assert.Equal("node_a", Assert.Single(bus.Hidden).NodeId);
    }

    [Theory]
    [InlineData(DiscoveryState.Detected)]
    [InlineData(DiscoveryState.Ignored)]
    public void Tick_WhenTransientNodeLeavesRange_ResetsToUndiscoveredAndCanRetrigger(DiscoveryState transientState)
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero, radius: 1f));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 10f);

        detector.Tick(Vector2.Zero, 10, 0f);
        if (transientState == DiscoveryState.Ignored)
        {
            detector.IgnoreCue("node_a");
        }

        detector.Tick(new Vector2(2, 0), 10, 0f);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("node_a"));

        detector.Tick(Vector2.Zero, 10, 0f);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
        Assert.Equal(2, bus.Shown.Count);
    }

    [Fact]
    public void Tick_WhenPendingNodeBecomesInvestigated_SkipsItAndContinuesQueue()
    {
        var registry = ActiveRegistry(
            CreateNode("first", new Vector2(1, 0)),
            CreateNode("skip", new Vector2(2, 0)),
            CreateNode("third", new Vector2(3, 0)));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1f, linger: 10f);

        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0f), "first");
        Assert.True(registry.TrySetState("skip", DiscoveryState.Investigated));
        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 1f), "third");

        Assert.Equal(new[] { "first", "third" }, bus.Shown.Select(cue => cue.NodeId).ToArray());
    }

    [Fact]
    public void Tick_WhenCalledRepeatedlyBeforeStagger_DoesNotDuplicatePendingTriggers()
    {
        var registry = ActiveRegistry(
            CreateNode("first", new Vector2(1, 0)),
            CreateNode("second", new Vector2(2, 0)));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 1f, linger: 10f);

        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 0f), "first");
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 0f));
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 0f));
        AssertCueOrder(detector.Tick(Vector2.Zero, 10, 1f), "second");
        Assert.Empty(detector.Tick(Vector2.Zero, 10, 1f));

        Assert.Equal(new[] { "first", "second" }, bus.Shown.Select(cue => cue.NodeId).ToArray());
    }

    [Fact]
    public void Tick_WhenNodeIsInvestigatedBeforeQueueing_DoesNotTrigger()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        Assert.True(registry.TrySetState("node_a", DiscoveryState.Investigated));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f, linger: 5f);

        var shown = detector.Tick(Vector2.Zero, 10, 0f);

        Assert.Empty(shown);
        Assert.Empty(bus.Shown);
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("node_a"));
    }

    private static ProximityDetector CreateDetector(
        InsightNodeRegistry registry,
        RecordingEventBus bus,
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

    private static void AssertCueOrder(IReadOnlyList<InsightCueShownEvent> shown, string expectedNodeId)
    {
        Assert.Equal(expectedNodeId, Assert.Single(shown).NodeId);
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

    private sealed class StubConditionEvaluator : IInsightConditionEvaluator
    {
        public StubConditionEvaluator(bool result)
        {
            Result = result;
        }

        public bool Result { get; }

        public bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites)
        {
            return Result;
        }
    }

    private sealed class RecordingEventBus : IEventBus
    {
        private readonly EventBus _inner = new();

        public List<InsightCueShownEvent> Shown { get; } = new();

        public List<InsightCueHiddenEvent> Hidden { get; } = new();

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            if (gameEvent is InsightCueShownEvent shown)
            {
                Shown.Add(shown);
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
