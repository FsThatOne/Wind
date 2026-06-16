using FengZhi.Foundation.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.TimeSystem;

/// <summary>
/// 时间系统调优配置。
/// GDD: §Tuning Knobs — 可外部化的数值参数。
/// </summary>
public sealed class TimeConfig
{
    // GDD 默认值
    public const int DefaultDaysPerSeason = 30;
    public const int DefaultBaseStamina = 100;
    public const int DefaultStaminaPerCon = 5;
    public const float DefaultRestRatioNap = 0.3f;
    public const float DefaultFatigueThreshold = 0.5f;
    public const float DefaultZeroStaminaSpeedMult = 0.5f;
    public const float DefaultStaminaPerGrid = 0.2f;

    [YamlMember(Alias = "days_per_season")]
    public int DaysPerSeason { get; set; } = DefaultDaysPerSeason;

    [YamlMember(Alias = "base_stamina")]
    public int BaseStamina { get; set; } = DefaultBaseStamina;

    [YamlMember(Alias = "stamina_per_con")]
    public int StaminaPerCon { get; set; } = DefaultStaminaPerCon;

    [YamlMember(Alias = "rest_ratio_nap")]
    public float RestRatioNap { get; set; } = DefaultRestRatioNap;

    [YamlMember(Alias = "fatigue_threshold")]
    public float FatigueThreshold { get; set; } = DefaultFatigueThreshold;

    [YamlMember(Alias = "zero_stamina_speed_mult")]
    public float ZeroStaminaSpeedMult { get; set; } = DefaultZeroStaminaSpeedMult;

    [YamlMember(Alias = "stamina_per_grid")]
    public float StaminaPerGrid { get; set; } = DefaultStaminaPerGrid;
}

/// <summary>
/// TimeConfig 加载器。从 YAML 文件加载并注册到 DataRegistry。
/// 配置为单例模式（只有一份全局时间配置）。
/// </summary>
public sealed class TimeConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    /// <summary>从 YAML 内容加载 TimeConfig</summary>
    public static TimeConfig LoadFromYaml(string yamlContent, string filePath)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new DataLoadException(filePath, null, "YAML content is empty.");

        try
        {
            var config = Deserializer.Deserialize<TimeConfig>(yamlContent);
            return config ?? new TimeConfig();
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, $"YAML parse error: {ex.Message}", ex);
        }
    }

    /// <summary>从文件路径加载</summary>
    public static TimeConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new DataLoadException(filePath, null, "File not found.");

        var content = File.ReadAllText(filePath);
        return LoadFromYaml(content, filePath);
    }
}

/// <summary>TimeConfig 的 IDataTable 适配（单例表，id 固定为 "default"）</summary>
public sealed class TimeConfigTable : IDataTable<TimeConfig>
{
    private readonly TimeConfig _config;

    public TimeConfigTable(TimeConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public TimeConfig? Get(string id) => id == "default" ? _config : null;
    public IReadOnlyList<TimeConfig> GetAll() => new[] { _config };
    public int Count => 1;
}
