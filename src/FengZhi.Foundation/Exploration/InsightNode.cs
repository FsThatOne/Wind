using Godot;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Insight discovery reward category consumed by later discovery dispatch work.
/// </summary>
public enum DiscoveryType
{
    Clue,
    Loot,
    MartialFragment,
    CodePhrase,
    SideQuestEntry,
    EnvironmentDetail
}

/// <summary>
/// Runtime discovery state for an insight node.
/// </summary>
public enum DiscoveryState
{
    Undiscovered,
    Detected,
    Ignored,
    Investigated
}

/// <summary>
/// Serializable prerequisite triple for later shared condition evaluation.
/// </summary>
public sealed record InsightPrerequisite(
    string Source,
    string? Op = null,
    string? Value = null,
    string? Id = null,
    string? Key = null);

/// <summary>
/// Reward payload configured on an insight node. Dispatch is implemented by later stories.
/// </summary>
public sealed record DiscoveryReward(
    string FlagId = "",
    string FlagValue = "",
    string ItemId = "",
    int Quantity = 1,
    string MartialId = "",
    string PhraseId = "",
    string QuestNodeId = "");

/// <summary>
/// Data-only scene insight node definition.
/// </summary>
public sealed partial class InsightNode
{
    public string Id { get; init; } = string.Empty;

    public string SceneId { get; init; } = string.Empty;

    public Vector2 Position { get; init; }

    public float DetectionRadius { get; init; } = 1.5f;

    public int InsightThreshold { get; init; } = 10;

    public DiscoveryType DiscoveryType { get; init; }

    public DiscoveryReward Reward { get; init; } = new();

    public string NarrativeContext { get; init; } = string.Empty;

    public IReadOnlyList<InsightPrerequisite> Prerequisite { get; init; } = Array.Empty<InsightPrerequisite>();

    public bool OneTime { get; init; } = true;
}
