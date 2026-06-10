using YamlDotNet.Serialization;

namespace FengZhi.Foundation.NpcState;

/// <summary>
/// NPC 模板数据（YAML 反序列化目标）。
/// </summary>
public sealed class NpcTemplate
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    // ─── 初始状态 ──────────────────────────────────────────

    [YamlMember(Alias = "initial_life")]
    public string InitialLife { get; set; } = "Unknown";

    [YamlMember(Alias = "initial_presence")]
    public string InitialPresence { get; set; } = "Unreachable";

    [YamlMember(Alias = "initial_location")]
    public string InitialLocation { get; set; } = "Unknown";

    [YamlMember(Alias = "initial_interaction")]
    public string InitialInteraction { get; set; } = "NotInteractable";

    [YamlMember(Alias = "initial_relationship")]
    public string InitialRelationship { get; set; } = "Stranger";

    // ─── 态度配置 ──────────────────────────────────────────

    [YamlMember(Alias = "base_attitude")]
    public int BaseAttitude { get; set; } = 0;

    [YamlMember(Alias = "mindset_compatibility")]
    public Dictionary<string, int> MindsetCompatibility { get; set; } = new();

    [YamlMember(Alias = "morality_reaction")]
    public Dictionary<string, int> MoralityReaction { get; set; } = new();

    // ─── 代办和旅程 ────────────────────────────────────────

    [YamlMember(Alias = "delegate_allowed")]
    public bool DelegateAllowed { get; set; } = false;

    [YamlMember(Alias = "help_request_allowed")]
    public bool HelpRequestAllowed { get; set; } = false;

    [YamlMember(Alias = "journey_required_chapter")]
    public int JourneyRequiredChapter { get; set; } = -1;

    [YamlMember(Alias = "journey_required_days")]
    public int JourneyRequiredDays { get; set; } = -1;
}
