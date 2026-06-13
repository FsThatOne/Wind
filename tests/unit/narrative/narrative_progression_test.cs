using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeProgressionTest
{
    [Fact]
    public void Initialize_ActivatesEntryNodeAndPublishesEvent()
    {
        var bus = new EventBus();
        var activated = new List<NarrativeNodeActivatedEvent>();
        bus.Subscribe<NarrativeNodeActivatedEvent>(activated.Add);
        var service = new NarrativeProgressionService(CreateLinearGraph(), bus);

        var result = service.Initialize();

        Assert.True(result);
        Assert.True(service.State.IsActive("start"));
        Assert.Single(activated);
        Assert.Equal("start", activated[0].NodeId);
        Assert.Equal(NarrativeNodeType.Dialogue, activated[0].NodeType);
    }

    [Fact]
    public void CompleteNode_CompletesActiveNodeAndPublishesEvent()
    {
        var bus = new EventBus();
        var completed = new List<NarrativeNodeCompletedEvent>();
        bus.Subscribe<NarrativeNodeCompletedEvent>(completed.Add);
        var service = new NarrativeProgressionService(CreateLinearGraph(), bus);
        service.Initialize();

        var result = service.CompleteNode("start");

        Assert.True(result);
        Assert.True(service.State.IsCompleted("start"));
        Assert.False(service.State.IsActive("start"));
        Assert.Single(completed);
        Assert.Equal("start", completed[0].NodeId);
    }

    [Fact]
    public void CompleteNode_ActivatesNextNodeWhenPreconditionsAreMet()
    {
        var bus = new EventBus();
        var activated = new List<NarrativeNodeActivatedEvent>();
        bus.Subscribe<NarrativeNodeActivatedEvent>(activated.Add);
        var service = new NarrativeProgressionService(CreateLinearGraph(), bus);
        service.Initialize();

        service.CompleteNode("start");

        Assert.True(service.State.IsActive("combat"));
        Assert.Equal(new[] { "start", "combat" }, activated.Select(e => e.NodeId));
    }

    [Fact]
    public void CompleteNode_DoesNotActivateNextNodeWhenPreconditionsFail()
    {
        var graph = new NarrativeGraph
        {
            Id = "blocked",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Chapter = 1, Type = NarrativeNodeType.Dialogue, Next = "arrival" },
                new NarrativeNode
                {
                    Id = "arrival",
                    Chapter = 1,
                    Type = NarrativeNodeType.Arrival,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.Flag, Key = "has_letter" }
                    }
                }
            }
        };
        var service = new NarrativeProgressionService(graph, new EventBus());
        service.Initialize();

        service.CompleteNode("start");

        Assert.True(service.State.IsCompleted("start"));
        Assert.False(service.State.IsActive("arrival"));
    }

    [Theory]
    [InlineData(NarrativeNodeType.Dialogue)]
    [InlineData(NarrativeNodeType.Combat)]
    [InlineData(NarrativeNodeType.Arrival)]
    [InlineData(NarrativeNodeType.Choice)]
    [InlineData(NarrativeNodeType.Gate)]
    public void CompleteNode_AllNodeTypesCanBeActivatedAndCompleted(NarrativeNodeType type)
    {
        var graph = new NarrativeGraph
        {
            Id = $"single_{type}",
            EntryNode = "node",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "node",
                    Chapter = 1,
                    Type = type,
                    Gate = type == NarrativeNodeType.Gate
                        ? new GateRequirement { RequiredBranches = 0, BranchNodeIds = Array.Empty<string>() }
                        : null
                }
            }
        };
        var service = new NarrativeProgressionService(graph, new EventBus());

        Assert.True(service.Initialize());
        Assert.True(service.CompleteNode("node"));
        Assert.True(service.State.IsCompleted("node"));
    }

    [Fact]
    public void CompleteNode_RejectsInactiveNode()
    {
        var service = new NarrativeProgressionService(CreateLinearGraph(), new EventBus());

        var result = service.CompleteNode("combat");

        Assert.False(result);
        Assert.False(service.State.IsCompleted("combat"));
    }

    private static NarrativeGraph CreateLinearGraph()
    {
        return new NarrativeGraph
        {
            Id = "linear",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Chapter = 1, Type = NarrativeNodeType.Dialogue, Next = "combat" },
                new NarrativeNode
                {
                    Id = "combat",
                    Chapter = 1,
                    Type = NarrativeNodeType.Combat,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "start" }
                    }
                }
            }
        };
    }
}
