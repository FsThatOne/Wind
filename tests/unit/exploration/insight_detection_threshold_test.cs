using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;
using Xunit;

namespace FengZhi.Tests.unit.exploration;

public sealed class InsightDetectionThresholdTest
{
    [Fact]
    public void Detect_WhenPlayerInsideRadiusAndInsightMeetsThreshold_SetsDetectedAndPublishesCue()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var bus = new RecordingEventBus();
        var detector = new ProximityDetector(registry, new StubConditionEvaluator(true), bus);

        var shown = detector.Detect(new Vector2(1, 0), playerInsight: 15);

        var cue = Assert.Single(shown);
        Assert.Equal("node_a", cue.NodeId);
        Assert.Equal(Vector2.Zero, cue.Position);
        Assert.Equal("exploration.inner_monologue.node_a", cue.NarrativeContext);
        Assert.Equal(DiscoveryType.EnvironmentDetail, cue.DiscoveryType);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
        Assert.Equal(cue, Assert.Single(bus.Published));
    }

    [Fact]
    public void Detect_WhenPlayerAtRadiusBoundary_DetectsCandidate()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var detector = new ProximityDetector(registry, new StubConditionEvaluator(true));

        var shown = detector.Detect(new Vector2(2, 0), playerInsight: 10);

        Assert.Single(shown);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenPlayerOutsideRadius_DoesNotEvaluatePrerequisitesOrPublishCue()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var evaluator = new StubConditionEvaluator(true);
        var bus = new RecordingEventBus();
        var detector = new ProximityDetector(registry, evaluator, bus);

        var shown = detector.Detect(new Vector2(2.01f, 0), playerInsight: 15);

        Assert.Empty(shown);
        Assert.Equal(0, evaluator.CallCount);
        Assert.Empty(bus.Published);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenPrerequisiteFails_LeavesNodeUndiscoveredAndPublishesNoCue()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var evaluator = new StubConditionEvaluator(false);
        var bus = new RecordingEventBus();
        var detector = new ProximityDetector(registry, evaluator, bus);

        var shown = detector.Detect(new Vector2(1, 0), playerInsight: 15);

        Assert.Empty(shown);
        Assert.Equal(1, evaluator.CallCount);
        Assert.Equal(node.Prerequisite, evaluator.SeenPrerequisites.Single());
        Assert.Empty(bus.Published);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenInsightBelowThreshold_LeavesNodeUndiscoveredAndPublishesNoCue()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var bus = new RecordingEventBus();
        var detector = new ProximityDetector(registry, new StubConditionEvaluator(true), bus);

        var shown = detector.Detect(new Vector2(1, 0), playerInsight: 8);

        Assert.Empty(shown);
        Assert.Empty(bus.Published);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenInsightImprovesAfterFailedVisit_ReevaluatesAndDetects()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var detector = new ProximityDetector(registry, new StubConditionEvaluator(true));

        var firstVisit = detector.Detect(new Vector2(1, 0), playerInsight: 8);
        var revisit = detector.Detect(new Vector2(1, 0), playerInsight: 10);

        Assert.Empty(firstVisit);
        Assert.Single(revisit);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenPrerequisiteChangesAfterFailedVisit_ReevaluatesLatestState()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var evaluator = new StubConditionEvaluator(false);
        var detector = new ProximityDetector(registry, evaluator);

        var firstVisit = detector.Detect(new Vector2(1, 0), playerInsight: 15);
        evaluator.Result = true;
        var revisit = detector.Detect(new Vector2(1, 0), playerInsight: 15);

        Assert.Empty(firstVisit);
        Assert.Single(revisit);
        Assert.Equal(2, evaluator.CallCount);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
    }

    [Fact]
    public void Detect_WhenNodeAlreadyDetected_DoesNotPublishDuplicateCue()
    {
        var node = CreateNode("node_a", position: Vector2.Zero, radius: 2f, threshold: 10);
        var registry = ActiveRegistry(node);
        var bus = new RecordingEventBus();
        var detector = new ProximityDetector(registry, new StubConditionEvaluator(true), bus);

        detector.Detect(new Vector2(1, 0), playerInsight: 10);
        var duplicate = detector.Detect(new Vector2(1, 0), playerInsight: 10);

        Assert.Empty(duplicate);
        Assert.Single(bus.Published);
    }

    [Fact]
    public void InsightCueShownEvent_DoesNotExposeThresholdInsightOrFailureReason()
    {
        var publicPropertyNames = typeof(InsightCueShownEvent)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains(nameof(InsightCueShownEvent.NodeId), publicPropertyNames);
        Assert.Contains(nameof(InsightCueShownEvent.Position), publicPropertyNames);
        Assert.Contains(nameof(InsightCueShownEvent.NarrativeContext), publicPropertyNames);
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Threshold", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Insight", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Failure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Prerequisite", StringComparison.OrdinalIgnoreCase));
    }

    private static InsightNodeRegistry ActiveRegistry(params InsightNode[] nodes)
    {
        var registry = new InsightNodeRegistry(nodes);
        registry.OnSceneLoaded("scene_a");
        return registry;
    }

    private static InsightNode CreateNode(string id, Vector2 position, float radius, int threshold)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = position,
            DetectionRadius = radius,
            InsightThreshold = threshold,
            DiscoveryType = DiscoveryType.EnvironmentDetail,
            NarrativeContext = $"exploration.inner_monologue.{id}",
            Prerequisite = new[] { new InsightPrerequisite("flag", "eq", "true", Key: "insight_intro_done") }
        };
    }

    private sealed class StubConditionEvaluator : IInsightConditionEvaluator
    {
        public StubConditionEvaluator(bool result)
        {
            Result = result;
        }

        public bool Result { get; set; }

        public int CallCount { get; private set; }

        public List<IReadOnlyList<InsightPrerequisite>> SeenPrerequisites { get; } = new();

        public bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites)
        {
            CallCount++;
            SeenPrerequisites.Add(prerequisites);
            return Result;
        }
    }

    private sealed class RecordingEventBus : IEventBus
    {
        private readonly EventBus _inner = new();

        public List<InsightCueShownEvent> Published { get; } = new();

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            if (gameEvent is InsightCueShownEvent cue)
            {
                Published.Add(cue);
            }

            _inner.Publish(gameEvent);
        }

        public Action Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            return _inner.Subscribe(handler);
        }
    }
}
