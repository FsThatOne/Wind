using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Save-system adapter for exploration insight discovery history.
/// </summary>
public sealed class ExplorationPersistenceAdapter : ISaveable
{
    public const string ExplorationSaveKey = "exploration_insight";
    public const string InvestigatedNodesField = "investigated_nodes";

    private readonly InsightNodeRegistry _registry;
    private readonly List<ExplorationSaveWarning> _warnings = new();

    public ExplorationPersistenceAdapter(InsightNodeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public string SaveKey => ExplorationSaveKey;

    /// <summary>Warnings captured during the most recent restore attempt.</summary>
    public IReadOnlyList<ExplorationSaveWarning> Warnings => _warnings.ToArray();

    /// <inheritdoc />
    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var investigated = _registry.GetAllStates()
            .Where(state => state.Value == DiscoveryState.Investigated)
            .Select(state => new ExplorationNodeSaveRecord(state.Key, DiscoveryState.Investigated.ToString()))
            .OrderBy(record => record.NodeId, StringComparer.Ordinal)
            .ToArray();

        snapshot.Values[InvestigatedNodesField] = JsonSerializer.SerializeToElement(investigated);
        return snapshot;
    }

    /// <inheritdoc />
    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        _warnings.Clear();

        if (!snapshot.Values.TryGetValue(InvestigatedNodesField, out var element))
        {
            _registry.RestoreInvestigatedStates(Array.Empty<string>());
            return;
        }

        var records = ReadRecords(element);
        var validNodeIds = new List<string>();
        var seenNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.NodeId))
            {
                Warn(record.NodeId, "node_id_missing");
                continue;
            }

            if (!seenNodeIds.Add(record.NodeId))
            {
                Warn(record.NodeId, "duplicate_record");
                continue;
            }

            if (!Enum.TryParse<DiscoveryState>(record.State, ignoreCase: false, out var state)
                || state != DiscoveryState.Investigated)
            {
                Warn(record.NodeId, $"invalid_state:{record.State}");
                continue;
            }

            if (!_registry.TryGetNode(record.NodeId, out _))
            {
                Warn(record.NodeId, "unknown_node_id");
                continue;
            }

            validNodeIds.Add(record.NodeId);
        }

        _registry.RestoreInvestigatedStates(validNodeIds);
    }

    private IReadOnlyList<ExplorationNodeSaveRecord> ReadRecords(JsonElement element)
    {
        try
        {
            return element.Deserialize<ExplorationNodeSaveRecord[]>() ?? Array.Empty<ExplorationNodeSaveRecord>();
        }
        catch (JsonException)
        {
            Warn(string.Empty, "malformed_investigated_nodes");
            return Array.Empty<ExplorationNodeSaveRecord>();
        }
    }

    private void Warn(string nodeId, string reason)
    {
        _warnings.Add(new ExplorationSaveWarning(nodeId, reason));
    }
}

/// <summary>Single save record for a permanently investigated insight node.</summary>
public sealed record ExplorationNodeSaveRecord(string NodeId, string State);

/// <summary>Non-fatal warning emitted while restoring exploration insight save data.</summary>
public sealed record ExplorationSaveWarning(string NodeId, string Reason);
