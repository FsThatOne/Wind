using FengZhi.Foundation.Exploration;
using Godot;
using Xunit;

namespace FengZhi.Tests.unit.exploration;

public sealed class InsightNodeRegistryTest
{
    [Fact]
    public void InsightNode_ModelPreservesGddFields()
    {
        var reward = new DiscoveryReward(
            FlagId: "insight_main_clue",
            FlagValue: "true",
            ItemId: "hidden_token",
            Quantity: 2,
            MartialId: "sword_fragment",
            PhraseId: "moon_gate",
            QuestNodeId: "quest_hidden_path");
        var prerequisite = new InsightPrerequisite("flag", "eq", "true", Key: "insight_intro_done");
        var node = new InsightNode
        {
            Id = "node_a",
            SceneId = "scene_forest",
            Position = new Vector2(3, 4),
            DetectionRadius = 2.25f,
            InsightThreshold = 12,
            DiscoveryType = DiscoveryType.Clue,
            Reward = reward,
            NarrativeContext = "exploration.inner_monologue.hidden_trace",
            Prerequisite = new[] { prerequisite },
            OneTime = false
        };

        Assert.Equal("node_a", node.Id);
        Assert.Equal("scene_forest", node.SceneId);
        Assert.Equal(new Vector2(3, 4), node.Position);
        Assert.Equal(2.25f, node.DetectionRadius);
        Assert.Equal(12, node.InsightThreshold);
        Assert.Equal(DiscoveryType.Clue, node.DiscoveryType);
        Assert.Equal(reward, node.Reward);
        Assert.Equal("exploration.inner_monologue.hidden_trace", node.NarrativeContext);
        Assert.Equal(prerequisite, Assert.Single(node.Prerequisite));
        Assert.False(node.OneTime);
    }

    [Fact]
    public void DiscoveryType_DefinesSixDiscoveryTypes()
    {
        var values = Enum.GetValues<DiscoveryType>();

        Assert.Contains(DiscoveryType.Clue, values);
        Assert.Contains(DiscoveryType.Loot, values);
        Assert.Contains(DiscoveryType.MartialFragment, values);
        Assert.Contains(DiscoveryType.CodePhrase, values);
        Assert.Contains(DiscoveryType.SideQuestEntry, values);
        Assert.Contains(DiscoveryType.EnvironmentDetail, values);
        Assert.Equal(6, values.Length);
    }

    [Fact]
    public void DiscoveryState_DefinesFourLifecycleStates()
    {
        var values = Enum.GetValues<DiscoveryState>();

        Assert.Contains(DiscoveryState.Undiscovered, values);
        Assert.Contains(DiscoveryState.Detected, values);
        Assert.Contains(DiscoveryState.Ignored, values);
        Assert.Contains(DiscoveryState.Investigated, values);
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void OnSceneLoaded_ActivatesOnlyMatchingSceneNodes()
    {
        var nodeA = CreateNode("node_a", "scene_a");
        var nodeB = CreateNode("node_b", "scene_b");
        var registry = new InsightNodeRegistry(new[] { nodeA, nodeB });

        registry.OnSceneLoaded("scene_a");

        var active = registry.GetActiveNodes();
        var activeNode = Assert.Single(active);
        Assert.Equal("node_a", activeNode.Id);
        Assert.Equal("scene_a", registry.CurrentSceneId);
    }

    [Fact]
    public void OnSceneLoaded_SkipsInvestigatedOneTimeNodesButKeepsRepeatableNodes()
    {
        var oneTime = CreateNode("one_time", "scene_a", oneTime: true);
        var repeatable = CreateNode("repeatable", "scene_a", oneTime: false);
        var registry = new InsightNodeRegistry(new[] { oneTime, repeatable });
        Assert.True(registry.TrySetState("one_time", DiscoveryState.Investigated));
        Assert.True(registry.TrySetState("repeatable", DiscoveryState.Investigated));

        registry.OnSceneLoaded("scene_a");

        var active = registry.GetActiveNodes();
        var activeNode = Assert.Single(active);
        Assert.Equal("repeatable", activeNode.Id);
    }

    [Fact]
    public void OnSceneUnloaded_ResetsTransientStatesAndClearsActiveNodes()
    {
        var detected = CreateNode("detected", "scene_a");
        var ignored = CreateNode("ignored", "scene_a");
        var investigated = CreateNode("investigated", "scene_a");
        var registry = new InsightNodeRegistry(new[] { detected, ignored, investigated });
        Assert.True(registry.TrySetState("detected", DiscoveryState.Detected));
        Assert.True(registry.TrySetState("ignored", DiscoveryState.Ignored));
        Assert.True(registry.TrySetState("investigated", DiscoveryState.Investigated));
        registry.OnSceneLoaded("scene_a");

        registry.OnSceneUnloaded();

        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("detected"));
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("ignored"));
        Assert.Equal(DiscoveryState.Investigated, registry.GetState("investigated"));
        Assert.Empty(registry.GetActiveNodes());
        Assert.Null(registry.CurrentSceneId);
    }

    [Fact]
    public void OnSceneLoaded_UnknownSceneAndRepeatedUnloadAreSafe()
    {
        var registry = new InsightNodeRegistry(new[] { CreateNode("node_a", "scene_a") });

        registry.OnSceneLoaded("missing_scene");
        registry.OnSceneUnloaded();
        registry.OnSceneUnloaded();

        Assert.Empty(registry.GetActiveNodes());
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("node_a"));
        Assert.Null(registry.CurrentSceneId);
    }

    [Fact]
    public void QueryInterfaces_DoNotExposeMutableInternalCollections()
    {
        var nodeA = CreateNode("node_a", "scene_a");
        var nodeB = CreateNode("node_b", "scene_a");
        var registry = new InsightNodeRegistry(new[] { nodeA, nodeB });
        registry.OnSceneLoaded("scene_a");

        var activeSnapshot = (InsightNode[])registry.GetActiveNodes();
        activeSnapshot[0] = CreateNode("external", "scene_a");

        Assert.Equal(new[] { "node_a", "node_b" }, registry.GetActiveNodes().Select(node => node.Id));
    }

    [Fact]
    public void UnknownNodeQueries_ReturnSafeDefaultAndExplicitFailure()
    {
        var registry = new InsightNodeRegistry(new[] { CreateNode("node_a", "scene_a") });

        var found = registry.TryGetNode("missing", out var node);
        var updated = registry.TrySetState("missing", DiscoveryState.Investigated);

        Assert.False(found);
        Assert.Null(node);
        Assert.False(updated);
        Assert.Equal(DiscoveryState.Undiscovered, registry.GetState("missing"));
    }

    private static InsightNode CreateNode(string id, string sceneId, bool oneTime = true)
    {
        return new InsightNode
        {
            Id = id,
            SceneId = sceneId,
            Position = Vector2.Zero,
            DetectionRadius = 1.5f,
            InsightThreshold = 10,
            DiscoveryType = DiscoveryType.EnvironmentDetail,
            Reward = new DiscoveryReward(),
            NarrativeContext = string.Empty,
            Prerequisite = Array.Empty<InsightPrerequisite>(),
            OneTime = oneTime
        };
    }
}
