using Xunit;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.SceneManagement;

namespace FengZhi.Tests.Foundation.SceneManagement;

public class SceneGraphTests
{
    // ─── AC1: SceneConnection 字段完整 ──────────────────────

    [Fact]
    public void SceneConnection_AllFieldsAccessible()
    {
        var conn = new SceneConnection
        {
            FromScene = "town_gate",
            ToScene = "town_market",
            ExitPoint = "north_exit",
            EntryPoint = "south_entry",
            ConnectionType = ConnectionType.Internal,
            UnlockConditions = new List<string> { "chapter_1_complete" },
            TransitionType = TransitionType.Fade,
            Bidirectional = true
        };

        Assert.Equal("town_gate", conn.FromScene);
        Assert.Equal("town_market", conn.ToScene);
        Assert.Equal("north_exit", conn.ExitPoint);
        Assert.Equal("south_entry", conn.EntryPoint);
        Assert.Equal(ConnectionType.Internal, conn.ConnectionType);
        Assert.Single(conn.UnlockConditions);
        Assert.Equal(TransitionType.Fade, conn.TransitionType);
        Assert.True(conn.Bidirectional);
    }

    // ─── AC2: GetConnections 返回从该场景出发的所有连接 ─────

    [Fact]
    public void GetConnections_ReturnsAllFromScene()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", Bidirectional = false
        });
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "C", Bidirectional = false
        });
        graph.AddConnection(new SceneConnection
        {
            FromScene = "B", ToScene = "C", Bidirectional = false
        });

        var connections = graph.GetConnections("A");
        Assert.Equal(2, connections.Count);
    }

    [Fact]
    public void GetConnections_UnknownScene_ReturnsEmpty()
    {
        var graph = new SceneGraph();
        Assert.Empty(graph.GetConnections("nonexistent"));
    }

    // ─── AC3: GetAdjacentScenes 邻接列表 ────────────────────

    [Fact]
    public void GetAdjacentScenes_ReturnsInternalOnly()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "inn", ToScene = "market", ConnectionType = ConnectionType.Internal, Bidirectional = false
        });
        graph.AddConnection(new SceneConnection
        {
            FromScene = "inn", ToScene = "world_map", ConnectionType = ConnectionType.ExitToMap, Bidirectional = false
        });

        var adjacent = graph.GetAdjacentScenes("inn");
        Assert.Single(adjacent);
        Assert.Equal("market", adjacent[0]);
    }

    [Fact]
    public void GetAdjacentScenes_NoDuplicates()
    {
        var graph = new SceneGraph();
        // 两条不同出口通往同一场景
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", ExitPoint = "east", Bidirectional = false
        });
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", ExitPoint = "south", Bidirectional = false
        });

        var adjacent = graph.GetAdjacentScenes("A");
        Assert.Single(adjacent);
    }

    // ─── AC4: bidirectional=true 自动注册反向 ────────────────

    [Fact]
    public void AddConnection_Bidirectional_CreatesReverse()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B",
            ExitPoint = "exit_north", EntryPoint = "entry_south",
            Bidirectional = true
        });

        // A→B 存在
        var fromA = graph.GetConnections("A");
        Assert.Single(fromA);
        Assert.Equal("B", fromA[0].ToScene);

        // B→A 也存在（反向）
        var fromB = graph.GetConnections("B");
        Assert.Single(fromB);
        Assert.Equal("A", fromB[0].ToScene);
        Assert.Equal("entry_south", fromB[0].ExitPoint); // 反向交换
        Assert.Equal("exit_north", fromB[0].EntryPoint);
    }

    [Fact]
    public void AddConnection_Unidirectional_NoReverse()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", Bidirectional = false
        });

        Assert.Single(graph.GetConnections("A"));
        Assert.Empty(graph.GetConnections("B"));
    }

    [Fact]
    public void AddConnection_ExitToMap_NeverBidirectional()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "inn", ToScene = "world_map",
            ConnectionType = ConnectionType.ExitToMap,
            Bidirectional = true // 标记为双向但 ExitToMap 不应反向
        });

        // ExitToMap 不生成反向
        Assert.Empty(graph.GetConnections("world_map"));
    }

    // ─── AC5: YAML 加载 ─────────────────────────────────────

    [Fact]
    public void LoadFromYaml_ValidConfig_ParsesCorrectly()
    {
        var yaml = @"
connections:
  - from_scene: town_gate
    to_scene: town_market
    exit_point: north
    entry_point: south
    connection_type: internal
    transition_type: fade
    bidirectional: true
  - from_scene: town_gate
    to_scene: world_map
    exit_point: south_gate
    entry_point: town_pos
    connection_type: exit_to_map
    bidirectional: false
";
        var graph = SceneGraphLoader.LoadFromYaml(yaml, "test.yaml");

        // town_gate 有 2 条直连 + 1 条反向 = 3 条总计
        Assert.Equal(3, graph.ConnectionCount);
        Assert.True(graph.HasMapExit("town_gate"));
    }

    [Fact]
    public void LoadFromYaml_MultipleBidirectional()
    {
        var yaml = @"
connections:
  - from_scene: A
    to_scene: B
    exit_point: e1
    entry_point: e2
    bidirectional: true
  - from_scene: B
    to_scene: C
    exit_point: e3
    entry_point: e4
    bidirectional: true
";
        var graph = SceneGraphLoader.LoadFromYaml(yaml, "test.yaml");

        // A→B + B→A + B→C + C→B = 4 条
        Assert.Equal(4, graph.ConnectionCount);
        Assert.Contains("A", graph.AllSceneIds);
        Assert.Contains("B", graph.AllSceneIds);
        Assert.Contains("C", graph.AllSceneIds);
    }

    // ─── AC6: 格式错误抛 DataLoadException ──────────────────

    [Fact]
    public void LoadFromYaml_EmptyContent_Throws()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            SceneGraphLoader.LoadFromYaml("", "empty.yaml"));
        Assert.Equal("empty.yaml", ex.FilePath);
    }

    [Fact]
    public void LoadFromYaml_InvalidYaml_Throws()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            SceneGraphLoader.LoadFromYaml("connections: [broken", "bad.yaml"));
        Assert.Equal("bad.yaml", ex.FilePath);
    }

    [Fact]
    public void LoadFromYaml_EmptyFromScene_Throws()
    {
        var yaml = @"
connections:
  - from_scene: """"
    to_scene: B
    bidirectional: false
";
        Assert.Throws<DataLoadException>(() =>
            SceneGraphLoader.LoadFromYaml(yaml, "bad.yaml"));
    }

    // ─── 辅助查询 ───────────────────────────────────────────

    [Fact]
    public void FindConnection_Exists_ReturnsConnection()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", TransitionType = TransitionType.InkWash, Bidirectional = false
        });

        var conn = graph.FindConnection("A", "B");
        Assert.NotNull(conn);
        Assert.Equal(TransitionType.InkWash, conn!.TransitionType);
    }

    [Fact]
    public void FindConnection_NotExists_ReturnsNull()
    {
        var graph = new SceneGraph();
        Assert.Null(graph.FindConnection("X", "Y"));
    }

    [Fact]
    public void HasMapExit_NoExit_ReturnsFalse()
    {
        var graph = new SceneGraph();
        graph.AddConnection(new SceneConnection
        {
            FromScene = "A", ToScene = "B", ConnectionType = ConnectionType.Internal, Bidirectional = false
        });
        Assert.False(graph.HasMapExit("A"));
    }
}
