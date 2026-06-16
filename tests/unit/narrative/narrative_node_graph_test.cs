using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeNodeGraphTest
{
    [Fact]
    public void Validate_AcceptsGraphWithAllNodeTypes()
    {
        var graph = CreateValidGraph();

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsDuplicateNodeIds()
    {
        var graph = new NarrativeGraph
        {
            Id = "duplicate",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Type = NarrativeNodeType.Dialogue },
                new NarrativeNode { Id = "start", Type = NarrativeNodeType.Arrival }
            }
        };

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("重复节点 id"));
    }

    [Fact]
    public void Validate_RejectsMissingEntryNode()
    {
        var graph = CreateValidGraph().WithEntry("missing");

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("entry_node"));
    }

    [Fact]
    public void Validate_RejectsInvalidNextReference()
    {
        var graph = new NarrativeGraph
        {
            Id = "invalid_next",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Type = NarrativeNodeType.Dialogue, Next = "missing" }
            }
        };

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("next 引用不存在"));
    }

    [Fact]
    public void Validate_RejectsInvalidBranchReference()
    {
        var graph = new NarrativeGraph
        {
            Id = "invalid_branch",
            EntryNode = "choice",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "choice",
                    Type = NarrativeNodeType.Choice,
                    Branches = new[]
                    {
                        new NarrativeBranch { Id = "route_a", Title = "先查靖川", Next = "missing" }
                    }
                }
            }
        };

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("branch 'route_a' next 引用不存在"));
    }

    [Fact]
    public void Validate_RejectsGateWithoutRequirement()
    {
        var graph = new NarrativeGraph
        {
            Id = "invalid_gate",
            EntryNode = "gate",
            Nodes = new[]
            {
                new NarrativeNode { Id = "gate", Type = NarrativeNodeType.Gate }
            }
        };

        var result = NarrativeGraphValidator.Validate(graph);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("缺少 gate requirement"));
    }

    [Fact]
    public void LoadFromYaml_ParsesAndValidatesNarrativeGraph()
    {
        const string yaml = """
id: ch01_main
version: 1
entry_node: start
nodes:
  - id: start
    chapter: 1
    type: dialogue
    title: 初至江南
    next: choice
  - id: choice
    chapter: 1
    type: choice
    title: 先查哪条线
    branches:
      - id: route_a
        title: 追查旧信使
        next: branch_a
      - id: route_b
        title: 追查铜钱
        next: branch_b
  - id: branch_a
    chapter: 1
    type: arrival
    title: 城西韩家
    next: gate
  - id: branch_b
    chapter: 1
    type: combat
    title: 小巷伏击
    next: gate
  - id: gate
    chapter: 1
    type: gate
    title: 靖川合流
    gate:
      required_branches: 1
      branch_node_ids: [branch_a, branch_b]
""";
        var loader = new NarrativeGraphLoader();

        var graph = loader.LoadFromYaml(yaml);

        Assert.Equal("ch01_main", graph.Id);
        Assert.Equal("start", graph.EntryNode);
        Assert.Equal(5, graph.Nodes.Count);
        Assert.Contains(graph.Nodes, n => n.Type == NarrativeNodeType.Dialogue);
        Assert.Contains(graph.Nodes, n => n.Type == NarrativeNodeType.Combat);
        Assert.Contains(graph.Nodes, n => n.Type == NarrativeNodeType.Arrival);
        Assert.Contains(graph.Nodes, n => n.Type == NarrativeNodeType.Choice);
        Assert.Contains(graph.Nodes, n => n.Type == NarrativeNodeType.Gate);
    }

    private static NarrativeGraph CreateValidGraph()
    {
        return new NarrativeGraph
        {
            Id = "main",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Type = NarrativeNodeType.Dialogue, Next = "choice" },
                new NarrativeNode
                {
                    Id = "choice",
                    Type = NarrativeNodeType.Choice,
                    Branches = new[]
                    {
                        new NarrativeBranch { Id = "a", Title = "追查旧信使", Next = "arrival" },
                        new NarrativeBranch { Id = "b", Title = "小巷伏击", Next = "combat" }
                    }
                },
                new NarrativeNode { Id = "arrival", Type = NarrativeNodeType.Arrival, Next = "gate" },
                new NarrativeNode { Id = "combat", Type = NarrativeNodeType.Combat, Next = "gate" },
                new NarrativeNode
                {
                    Id = "gate",
                    Type = NarrativeNodeType.Gate,
                    Gate = new GateRequirement
                    {
                        RequiredBranches = 1,
                        BranchNodeIds = new[] { "arrival", "combat" }
                    }
                }
            }
        };
    }
}

internal static class NarrativeGraphTestExtensions
{
    public static NarrativeGraph WithEntry(this NarrativeGraph graph, string entry)
    {
        return new NarrativeGraph
        {
            Id = graph.Id,
            Version = graph.Version,
            EntryNode = entry,
            Nodes = graph.Nodes
        };
    }
}
