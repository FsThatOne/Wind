namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Scene-bound registry for insight nodes and their runtime discovery state.
/// </summary>
public sealed partial class InsightNodeRegistry
{
    private readonly Dictionary<string, InsightNode> _allNodes = new();
    private readonly Dictionary<string, DiscoveryState> _states = new();
    private readonly List<InsightNode> _activeSceneNodes = new();

    public InsightNodeRegistry()
    {
    }

    public InsightNodeRegistry(IEnumerable<InsightNode> nodes)
    {
        foreach (var node in nodes)
        {
            RegisterNode(node);
        }
    }

    /// <summary>Currently loaded scene id, or null when no scene is active.</summary>
    public string? CurrentSceneId { get; private set; }

    /// <summary>Registers or replaces a node definition by id.</summary>
    public bool RegisterNode(InsightNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            return false;
        }

        _allNodes[node.Id] = node;
        return true;
    }

    /// <summary>Activates nodes belonging to the loaded scene.</summary>
    public void OnSceneLoaded(string sceneId)
    {
        CurrentSceneId = sceneId;
        _activeSceneNodes.Clear();

        foreach (var node in _allNodes.Values.Where(node => node.SceneId == sceneId))
        {
            if (node.OneTime && GetState(node.Id) == DiscoveryState.Investigated)
            {
                continue;
            }

            _activeSceneNodes.Add(node);
        }
    }

    /// <summary>Unloads the active scene and clears transient discovery states.</summary>
    public void OnSceneUnloaded()
    {
        foreach (var node in _activeSceneNodes)
        {
            var state = GetState(node.Id);
            if (state is DiscoveryState.Detected or DiscoveryState.Ignored)
            {
                _states[node.Id] = DiscoveryState.Undiscovered;
            }
        }

        _activeSceneNodes.Clear();
        CurrentSceneId = null;
    }

    /// <summary>Returns a defensive snapshot of active scene nodes.</summary>
    public IReadOnlyList<InsightNode> GetActiveNodes()
    {
        return _activeSceneNodes.ToArray();
    }

    /// <summary>Returns a defensive snapshot of all registered nodes.</summary>
    public IReadOnlyList<InsightNode> GetAllNodes()
    {
        return _allNodes.Values.ToArray();
    }

    /// <summary>Returns a defensive snapshot of explicitly tracked discovery states.</summary>
    public IReadOnlyDictionary<string, DiscoveryState> GetAllStates()
    {
        return _states.ToDictionary(state => state.Key, state => state.Value, StringComparer.Ordinal);
    }

    /// <summary>Safely looks up a node by id.</summary>
    public bool TryGetNode(string nodeId, out InsightNode? node)
    {
        return _allNodes.TryGetValue(nodeId, out node);
    }

    /// <summary>Returns Undiscovered for unknown ids to keep callers safe.</summary>
    public DiscoveryState GetState(string nodeId)
    {
        return _states.GetValueOrDefault(nodeId, DiscoveryState.Undiscovered);
    }

    /// <summary>Changes state only for known nodes; unknown ids fail explicitly.</summary>
    public bool TrySetState(string nodeId, DiscoveryState state)
    {
        if (!_allNodes.ContainsKey(nodeId))
        {
            return false;
        }

        _states[nodeId] = state;
        return true;
    }

    /// <summary>Replaces runtime discovery state with validated investigated nodes from a save snapshot.</summary>
    public void RestoreInvestigatedStates(IEnumerable<string> nodeIds)
    {
        _states.Clear();
        foreach (var nodeId in nodeIds.Distinct(StringComparer.Ordinal))
        {
            if (_allNodes.ContainsKey(nodeId))
            {
                _states[nodeId] = DiscoveryState.Investigated;
            }
        }
    }
}
