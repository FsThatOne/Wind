using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeBranchGateTest
{
    [Fact]
    public void SelectBranch_RecordsChoiceLogAndPublishesEvent()
    {
        var bus = new EventBus();
        var choices = new List<NarrativeChoiceLoggedEvent>();
        bus.Subscribe<NarrativeChoiceLoggedEvent>(choices.Add);
        var service = new NarrativeProgressionService(CreateBranchGraph(), bus);
        service.Initialize();

        var result = service.SelectBranch("choice", "route_a");

        Assert.True(result);
        Assert.Single(service.State.ChoiceLog);
        Assert.Equal("choice", service.State.ChoiceLog[0].NodeId);
        Assert.Equal("route_a", service.State.ChoiceLog[0].BranchId);
        Assert.Single(choices);
        Assert.Equal("追查旧信使", choices[0].BranchTitle);
    }

    [Fact]
    public void SelectBranch_CompletesChoiceAndActivatesBranchTarget()
    {
        var service = new NarrativeProgressionService(CreateBranchGraph(), new EventBus());
        service.Initialize();

        service.SelectBranch("choice", "route_b");

        Assert.True(service.State.IsCompleted("choice"));
        Assert.True(service.State.IsActive("branch_b"));
        Assert.False(service.State.IsActive("branch_a"));
    }

    [Fact]
    public void SelectBranch_RejectsBranchWhenBranchPreconditionsFail()
    {
        var graph = CreateBranchGraph();
        graph.Nodes.First(node => node.Id == "choice").Branches[0].Preconditions.Add(
            new NarrativeCondition { Kind = NarrativeConditionKind.Flag, Key = "hidden_flag" });
        var service = new NarrativeProgressionService(graph, new EventBus());
        service.Initialize();

        var result = service.SelectBranch("choice", "route_a");

        Assert.False(result);
        Assert.Empty(service.State.ChoiceLog);
        Assert.True(service.State.IsActive("choice"));
    }

    [Fact]
    public void Gate_CannotCompleteBeforeRequiredBranchesAreCompleted()
    {
        var service = new NarrativeProgressionService(CreateGateGraph(requiredBranches: 2), new EventBus());
        service.Initialize();
        service.CompleteNode("branch_a");
        service.TryActivateNode("gate");

        var result = service.CompleteNode("gate");

        Assert.False(result);
        Assert.False(service.State.IsCompleted("gate"));
    }

    [Fact]
    public void Gate_CanCompleteWhenCompletedBranchCountMeetsRequirement()
    {
        var service = new NarrativeProgressionService(CreateGateGraph(requiredBranches: 2), new EventBus());
        service.Initialize();
        service.CompleteNode("branch_a");
        service.TryActivateNode("branch_b");
        service.CompleteNode("branch_b");
        service.TryActivateNode("gate");

        var result = service.CompleteNode("gate");

        Assert.True(result);
        Assert.True(service.State.IsCompleted("gate"));
    }

    [Fact]
    public void SoftPreconditions_DoNotBlockNodeActivation()
    {
        var graph = new NarrativeGraph
        {
            Id = "soft",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "start",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    SoftPreconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.Flag, Key = "side_done" }
                    }
                }
            }
        };
        var service = new NarrativeProgressionService(graph, new EventBus());

        var activated = service.Initialize();

        Assert.True(activated);
        Assert.True(service.State.IsActive("start"));
        Assert.False(service.AreSoftPreconditionsMet(graph.Nodes[0]));
    }

    [Fact]
    public void SoftPreconditions_CanUnlockExtraContentWhenSatisfied()
    {
        var graph = new NarrativeGraph
        {
            Id = "soft",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "start",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    SoftPreconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.Flag, Key = "side_done" }
                    }
                }
            }
        };
        var service = new NarrativeProgressionService(
            graph,
            new EventBus(),
            new DefaultNarrativeConditionEvaluator(new HashSet<string> { "side_done" }));
        service.Initialize();

        Assert.True(service.AreSoftPreconditionsMet(graph.Nodes[0]));
    }

    [Fact]
    public void ChoiceLoggedCondition_CanBeUsedByLaterNodes()
    {
        var graph = CreateBranchGraph();
        graph.Nodes.Add(new NarrativeNode
        {
            Id = "later",
            Chapter = 1,
            Type = NarrativeNodeType.Dialogue,
            Preconditions = new[]
            {
                new NarrativeCondition
                {
                    Kind = NarrativeConditionKind.ChoiceLogged,
                    Key = "choice",
                    Value = "route_a"
                }
            }
        });
        var service = new NarrativeProgressionService(graph, new EventBus());
        service.Initialize();
        service.SelectBranch("choice", "route_a");

        var result = service.TryActivateNode("later");

        Assert.True(result);
        Assert.True(service.State.IsActive("later"));
    }

    private static NarrativeGraph CreateBranchGraph()
    {
        return new NarrativeGraph
        {
            Id = "branches",
            EntryNode = "choice",
            Nodes = new List<NarrativeNode>
            {
                new()
                {
                    Id = "choice",
                    Chapter = 1,
                    Type = NarrativeNodeType.Choice,
                    Branches = new[]
                    {
                        new NarrativeBranch { Id = "route_a", Title = "追查旧信使", Next = "branch_a" },
                        new NarrativeBranch { Id = "route_b", Title = "追查铜钱", Next = "branch_b" }
                    }
                },
                new() { Id = "branch_a", Chapter = 1, Type = NarrativeNodeType.Arrival },
                new() { Id = "branch_b", Chapter = 1, Type = NarrativeNodeType.Arrival }
            }
        };
    }

    private static NarrativeGraph CreateGateGraph(int requiredBranches)
    {
        return new NarrativeGraph
        {
            Id = "gate_graph",
            EntryNode = "branch_a",
            Nodes = new List<NarrativeNode>
            {
                new() { Id = "branch_a", Chapter = 1, Type = NarrativeNodeType.Arrival },
                new() { Id = "branch_b", Chapter = 1, Type = NarrativeNodeType.Arrival },
                new()
                {
                    Id = "gate",
                    Chapter = 1,
                    Type = NarrativeNodeType.Gate,
                    Gate = new GateRequirement
                    {
                        RequiredBranches = requiredBranches,
                        BranchNodeIds = new[] { "branch_a", "branch_b" }
                    }
                }
            }
        };
    }
}
