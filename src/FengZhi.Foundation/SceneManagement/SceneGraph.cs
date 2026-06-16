using FengZhi.Foundation.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.SceneManagement;

/// <summary>连接类型</summary>
public enum ConnectionType
{
    /// <summary>地点内场景连接</summary>
    Internal,
    /// <summary>通往大地图的出口</summary>
    ExitToMap
}

/// <summary>过渡类型</summary>
public enum TransitionType
{
    Fade,
    InkWash
}

/// <summary>
/// 场景连接记录。数据驱动，不硬编码。
/// GDD: §Core Rules 2
/// </summary>
public sealed class SceneConnection
{
    [YamlMember(Alias = "from_scene")]
    public string FromScene { get; set; } = "";

    [YamlMember(Alias = "to_scene")]
    public string ToScene { get; set; } = "";

    [YamlMember(Alias = "exit_point")]
    public string ExitPoint { get; set; } = "";

    [YamlMember(Alias = "entry_point")]
    public string EntryPoint { get; set; } = "";

    [YamlMember(Alias = "connection_type")]
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Internal;

    [YamlMember(Alias = "unlock_conditions")]
    public List<string> UnlockConditions { get; set; } = new();

    [YamlMember(Alias = "transition_type")]
    public TransitionType TransitionType { get; set; } = TransitionType.Fade;

    [YamlMember(Alias = "bidirectional")]
    public bool Bidirectional { get; set; } = true;
}

/// <summary>
/// 场景连接图。提供邻接查询和路径可达性判定。
/// </summary>
public sealed class SceneGraph
{
    private readonly Dictionary<string, List<SceneConnection>> _adjacency = new();
    private readonly List<SceneConnection> _allConnections = new();

    /// <summary>已注册的连接总数（含自动生成的反向连接）</summary>
    public int ConnectionCount => _allConnections.Count;

    /// <summary>已知的场景 ID 集合</summary>
    public IReadOnlyCollection<string> AllSceneIds => _adjacency.Keys;

    /// <summary>添加一条连接（若 bidirectional=true 自动添加反向）</summary>
    public void AddConnection(SceneConnection connection)
    {
        AddDirectedConnection(connection);

        if (connection.Bidirectional && connection.ConnectionType == ConnectionType.Internal)
        {
            var reverse = new SceneConnection
            {
                FromScene = connection.ToScene,
                ToScene = connection.FromScene,
                ExitPoint = connection.EntryPoint,
                EntryPoint = connection.ExitPoint,
                ConnectionType = connection.ConnectionType,
                UnlockConditions = connection.UnlockConditions,
                TransitionType = connection.TransitionType,
                Bidirectional = false // 避免无限递归
            };
            AddDirectedConnection(reverse);
        }
    }

    /// <summary>AC2: 获取从指定场景出发的所有连接</summary>
    public IReadOnlyList<SceneConnection> GetConnections(string sceneId)
    {
        return _adjacency.TryGetValue(sceneId, out var list)
            ? list
            : Array.Empty<SceneConnection>();
    }

    /// <summary>AC3: 获取邻接场景列表</summary>
    public IReadOnlyList<string> GetAdjacentScenes(string sceneId)
    {
        if (!_adjacency.TryGetValue(sceneId, out var list))
            return Array.Empty<string>();

        return list
            .Where(c => c.ConnectionType == ConnectionType.Internal)
            .Select(c => c.ToScene)
            .Distinct()
            .ToList();
    }

    /// <summary>查询两个场景之间是否有直接连接</summary>
    public SceneConnection? FindConnection(string fromScene, string toScene)
    {
        if (!_adjacency.TryGetValue(fromScene, out var list))
            return null;
        return list.FirstOrDefault(c => c.ToScene == toScene);
    }

    /// <summary>判断场景是否有通往大地图的出口</summary>
    public bool HasMapExit(string sceneId)
    {
        if (!_adjacency.TryGetValue(sceneId, out var list))
            return false;
        return list.Any(c => c.ConnectionType == ConnectionType.ExitToMap);
    }

    private void AddDirectedConnection(SceneConnection connection)
    {
        if (!_adjacency.ContainsKey(connection.FromScene))
            _adjacency[connection.FromScene] = new List<SceneConnection>();

        if (!_adjacency.ContainsKey(connection.ToScene))
            _adjacency[connection.ToScene] = new List<SceneConnection>();

        _adjacency[connection.FromScene].Add(connection);
        _allConnections.Add(connection);
    }
}

/// <summary>YAML 场景连接配置格式</summary>
public sealed class SceneGraphYaml
{
    [YamlMember(Alias = "connections")]
    public List<SceneConnection> Connections { get; set; } = new();
}

/// <summary>场景连接图加载器</summary>
public static class SceneGraphLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static SceneGraph LoadFromYaml(string yamlContent, string filePath)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new DataLoadException(filePath, null, "YAML content is empty.");

        try
        {
            var data = Deserializer.Deserialize<SceneGraphYaml>(yamlContent);
            if (data?.Connections == null || data.Connections.Count == 0)
                throw new DataLoadException(filePath, null, "No connections defined in scene graph.");

            var graph = new SceneGraph();
            foreach (var conn in data.Connections)
            {
                if (string.IsNullOrEmpty(conn.FromScene) || string.IsNullOrEmpty(conn.ToScene))
                    throw new DataLoadException(filePath, null, $"Connection has empty from_scene or to_scene.");
                graph.AddConnection(conn);
            }
            return graph;
        }
        catch (DataLoadException) { throw; }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, $"YAML parse error: {ex.Message}", ex);
        }
    }
}
