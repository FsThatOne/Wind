using System.Text.Json;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using FengZhi.Foundation.SaveSystem;
using FengZhi.Foundation.StateMachine;
using Godot;
using Xunit;

namespace FengZhi.Tests.integration.exploration;

public sealed class InsightSaveSceneLockTest
{
    [Fact]
    public void Serialize_CapturesOnlyInvestigatedNodes()
    {
        var registry = ActiveRegistry(
            CreateNode("undiscovered"),
            CreateNode("detected"),
            CreateNode("ignored"),
            CreateNode("investigated"));
        Assert.True(registry.TrySetState("detected", DiscoveryState.Detected));
        Assert.True(registry.TrySetState("ignored", DiscoveryState.Ignored));
        Assert.True(registry.TrySetState("investigated", DiscoveryState.Investigated));
        var persistence = new ExplorationPersistenceAdapter(registry);

        var snapshot = persistence.Serialize();
        var records = ReadRecords(snapshot);

        var record = Assert.Single(records);
        Assert.Equal("investigated", record.NodeId);
        Assert.Equal(DiscoveryState.Investigated.ToString(), record.State);
    }

    [Fact]
    public void Deserialize_WhenInvestigatedOneTimeNodeRestores_SkipsItOnSceneLoad()
    {
        var registry = new InsightNodeRegistry(new[] { CreateNode("one_time", oneTime: true) });
        var persistence = new ExplorationPersistenceAdapter(registry);

        persistence.Deserialize(Snapshot(new ExplorationNodeSaveRecord("one_time", "Investigated")), version: 1);
        registry.OnSceneLoaded("scene_a");

        Assert.Equal(DiscoveryState.Investigated, registry.GetState("one_time"));
        Assert.Empty(registry.GetActiveNodes());
        Assert.Empty(persistence.Warnings);
    }

    [Fact]
    public void Deserialize_WhenSnapshotHasUnknownInvalidOrDuplicateRecords_WarnsAndRestoresOnlyValidInvestigated()
    {
        var registry = ActiveRegistry(CreateNode("valid"), CreateNode("invalid_state"));
        Assert.True(registry.TrySetState("invalid_state", DiscoveryState.Detected));
        var persistence = new ExplorationPersistenceAdapter(registry);

        persistence.Deserialize(
            Snapshot(
                new ExplorationNodeSaveRecord("valid", "Investigated"),
                new ExplorationNodeSaveRecord("valid", "Investigated"),
                new ExplorationNodeSaveRecord("missing", "Investigated"),
                new ExplorationNodeSaveRecord("invalid_state", "Detected"),
                new ExplorationNodeSaveRecord("bogus", "Bogus")),
            version: 1);

        Assert.Equal(DiscoveryState.Investigated, registry.GetState("valid"));
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("invalid_state"));
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("missing"));
        Assert.Contains(persistence.Warnings, warning => warning.NodeId == "valid" && warning.Reason == "duplicate_record");
        Assert.Contains(persistence.Warnings, warning => warning.NodeId == "missing" && warning.Reason == "unknown_node_id");
        Assert.Contains(persistence.Warnings, warning => warning.NodeId == "invalid_state" && warning.Reason == "invalid_state:Detected");
        Assert.Contains(persistence.Warnings, warning => warning.NodeId == "bogus" && warning.Reason == "invalid_state:Bogus");
    }

    [Fact]
    public void OnSceneUnloaded_ClearsActiveNodesPendingQueueAndCurrentCue()
    {
        var registry = ActiveRegistry(
            CreateNode("first", new Vector2(1, 0)),
            CreateNode("pending", new Vector2(2, 0)));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 10f);
        Assert.Equal("first", Assert.Single(detector.Tick(Vector2.Zero, 10, 0f)).NodeId);

        var hidden = detector.OnSceneUnloaded();
        var afterUnload = detector.Tick(Vector2.Zero, 10, 10f);

        Assert.Equal("first", Assert.Single(hidden).NodeId);
        Assert.Equal("first", Assert.Single(bus.Hidden).NodeId);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("first"));
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("pending"));
        Assert.Empty(registry.GetActiveNodes());
        Assert.Null(registry.CurrentSceneId);
        Assert.Empty(afterUnload);
    }

    [Fact]
    public void LockGuard_WhenPartialLockAcquired_HidesCueAndResumesDetectionAfterRelease()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f);
        var lockGuard = new ExplorationLockGuard(detector);
        Assert.Equal("node_a", Assert.Single(detector.Tick(Vector2.Zero, 10, 0f)).NodeId);

        var hidden = lockGuard.OnGameStateLockAcquired(LockMode.Partial);
        var lockedTick = detector.Tick(Vector2.Zero, 10, 0f);
        lockGuard.OnGameStateLockReleased(LockMode.Partial);
        var resumed = detector.Tick(Vector2.Zero, 10, 0f);

        Assert.True(detector.IsPaused is false);
        Assert.Equal("node_a", Assert.Single(hidden).NodeId);
        Assert.Empty(lockedTick);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
        Assert.Equal("node_a", Assert.Single(resumed).NodeId);
        Assert.Equal(2, bus.Shown.Count);
        Assert.Single(bus.Hidden);
    }

    [Fact]
    public void LockGuard_WhenNodeWasIgnored_DoesNotRetriggerUntilPlayerLeavesRange()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f);
        var lockGuard = new ExplorationLockGuard(detector);
        Assert.Equal("node_a", Assert.Single(detector.Tick(Vector2.Zero, 10, 0f)).NodeId);
        Assert.True(detector.IgnoreCue("node_a"));

        lockGuard.OnGameStateLockAcquired(LockMode.Partial);
        lockGuard.OnGameStateLockReleased(LockMode.Partial);
        var stillIgnored = detector.Tick(Vector2.Zero, 10, 0f);
        detector.Tick(new Vector2(10, 0), 10, 0f);
        var retriggeredAfterLeavingRange = detector.Tick(Vector2.Zero, 10, 0f);

        Assert.Empty(stillIgnored);
        Assert.Equal(DiscoveryState.Detected, registry.GetState("node_a"));
        Assert.Equal("node_a", Assert.Single(retriggeredAfterLeavingRange).NodeId);
        Assert.Equal(2, bus.Shown.Count);
    }

    [Fact]
    public void LockGuard_WhenLocksAreNested_ResumesOnlyAfterAllBlockingLocksRelease()
    {
        var registry = ActiveRegistry(CreateNode("node_a", Vector2.Zero));
        var bus = new RecordingEventBus();
        var detector = CreateDetector(registry, bus, stagger: 0f);
        var lockGuard = new ExplorationLockGuard(detector);

        lockGuard.OnGameStateLockAcquired(LockMode.Partial);
        lockGuard.OnGameStateLockAcquired(LockMode.Full);
        lockGuard.OnGameStateLockReleased(LockMode.Partial);
        var stillLocked = detector.Tick(Vector2.Zero, 10, 0f);
        lockGuard.OnGameStateLockReleased(LockMode.Full);
        var resumed = detector.Tick(Vector2.Zero, 10, 0f);

        Assert.True(lockGuard.IsLocked is false);
        Assert.Empty(stillLocked);
        Assert.Equal("node_a", Assert.Single(resumed).NodeId);
    }

    private static ProximityDetector CreateDetector(
        InsightNodeRegistry registry,
        RecordingEventBus bus,
        float stagger)
    {
        return new ProximityDetector(
            registry,
            new StubConditionEvaluator(true),
            bus,
            multiNodeStaggerSeconds: stagger,
            cueLingerSeconds: 10f);
    }

    private static InsightNodeRegistry ActiveRegistry(params InsightNode[] nodes)
    {
        var registry = new InsightNodeRegistry(nodes);
        registry.OnSceneLoaded("scene_a");
        return registry;
    }

    private static InsightNode CreateNode(string id, Vector2? position = null, bool oneTime = true)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = "scene_a",
            Position = position ?? Vector2.Zero,
            DetectionRadius = 5f,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.EnvironmentDetail,
            NarrativeContext = $"exploration.inner_monologue.{id}",
            OneTime = oneTime
        };
    }

    private static SaveSnapshot Snapshot(params ExplorationNodeSaveRecord[] records)
    {
        var snapshot = new SaveSnapshot();
        snapshot.Values[ExplorationPersistenceAdapter.InvestigatedNodesField] =
            JsonSerializer.SerializeToElement(records);
        return snapshot;
    }

    private static ExplorationNodeSaveRecord[] ReadRecords(SaveSnapshot snapshot)
    {
        return snapshot.Values[ExplorationPersistenceAdapter.InvestigatedNodesField]
            .Deserialize<ExplorationNodeSaveRecord[]>() ?? Array.Empty<ExplorationNodeSaveRecord>();
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
