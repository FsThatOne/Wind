using YamlDotNet.Serialization;

namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 角色模板 — YAML 反序列化目标。
/// 定义角色的基础配置，由 DataRegistry 在启动时加载。
/// </summary>
public sealed class CharacterTemplate
{
    /// <summary>唯一标识符（文件名去掉 .yaml）</summary>
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>角色类型: player / companion / enemy</summary>
    [YamlMember(Alias = "type")]
    public string TypeName { get; set; } = "enemy";

    // ─── 资源属性基础值 ─────────────────────────────────────

    [YamlMember(Alias = "base_hp")]
    public int BaseHp { get; set; } = 100;

    [YamlMember(Alias = "base_neixi")]
    public int BaseNeiXi { get; set; } = 20;

    [YamlMember(Alias = "base_stagger_threshold")]
    public int BaseStaggerThreshold { get; set; } = 5;

    // ─── 五维初始值 ─────────────────────────────────────────

    [YamlMember(Alias = "strength")]
    public int Strength { get; set; } = 10;

    [YamlMember(Alias = "agility")]
    public int Agility { get; set; } = 10;

    [YamlMember(Alias = "inner_power")]
    public int InnerPower { get; set; } = 10;

    [YamlMember(Alias = "insight")]
    public int Insight { get; set; } = 10;

    [YamlMember(Alias = "constitution")]
    public int Constitution { get; set; } = 10;

    // ─── 战斗参数 ───────────────────────────────────────────

    [YamlMember(Alias = "base_attack")]
    public int BaseAttack { get; set; } = 10;

    [YamlMember(Alias = "scaling_factor")]
    public float ScalingFactor { get; set; } = 1.0f;

    /// <summary>所属体系: gang / qiao / rou</summary>
    [YamlMember(Alias = "primary_style")]
    public string PrimaryStyle { get; set; } = "gang";

    // ─── 可用招式引用 ───────────────────────────────────────

    [YamlMember(Alias = "moves")]
    public List<string> Moves { get; set; } = new();

    // ─── 工具方法 ───────────────────────────────────────────

    /// <summary>解析 TypeName 为 CharacterType 枚举。</summary>
    public CharacterType GetCharacterType() => TypeName?.ToLowerInvariant() switch
    {
        "player" => CharacterType.Player,
        "companion" => CharacterType.Companion,
        _ => CharacterType.Enemy
    };
}
