using YamlDotNet.Serialization;

namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 属性调优配置 — 外部化的数值参数。
/// 加载自 assets/data/balance/attribute-tuning.yaml。
/// </summary>
public sealed class AttributeTuningConfig
{
    [YamlMember(Alias = "base_hp")]
    public int BaseHp { get; set; } = 100;

    [YamlMember(Alias = "base_neixi")]
    public int BaseNeiXi { get; set; } = 20;

    [YamlMember(Alias = "base_stagger_threshold")]
    public int BaseStaggerThreshold { get; set; } = 5;

    [YamlMember(Alias = "attribute_min")]
    public int AttributeMin { get; set; } = 1;

    [YamlMember(Alias = "attribute_cap")]
    public int AttributeCap { get; set; } = 50;

    [YamlMember(Alias = "max_total_attributes")]
    public int MaxTotalAttributes { get; set; } = 250;

    [YamlMember(Alias = "realm_thresholds")]
    public List<int> RealmThresholds { get; set; } = new() { 25, 35, 50, 70, 90, 115, 140, 170, 200 };

    [YamlMember(Alias = "crit_rate_cap")]
    public float CritRateCap { get; set; } = 0.30f;

    [YamlMember(Alias = "neixi_recovery_floor")]
    public int NeiXiRecoveryFloor { get; set; } = 2;

    [YamlMember(Alias = "stagger_decay_per_turn")]
    public int StaggerDecayPerTurn { get; set; } = 1;
}
