using FengZhi.Foundation.Data;
using FengZhi.Foundation.TimeSystem;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.SceneManagement;

public sealed class SceneConfig
{
    public float TransitionFadeDuration { get; set; } = 0.5f;
    public float TransitionInkDuration { get; set; } = 1.0f;
    public int PreloadRetryCount { get; set; } = 3;
    public float TimePerGrid { get; set; } = 0.01f;
    public float StaminaPerGrid { get; set; } = 0.2f;
    public float ZeroStaminaSpeedMult { get; set; } = 0.5f;
    public float StationTimeMult { get; set; } = 0.5f;
    public float StationBaseFee { get; set; } = 10f;
    public float StationDistanceFee { get; set; } = 2f;
    public float NightDarken { get; set; } = 0.20f;
    public float SeasonTransitionDuration { get; set; } = 2.0f;
}

public sealed class SceneConfigTable : IDataTable<SceneConfig>
{
    private SceneConfig _config = new();

    public SceneConfig Get(string id) => _config;
    public IReadOnlyList<SceneConfig> GetAll() => new[] { _config };
    public int Count => 1;

    public void Load(string yamlContent, string filePath)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            _config = deserializer.Deserialize<SceneConfig>(yamlContent) ?? new SceneConfig();
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(
                filePath, (int)ex.Start.Line, $"Failed to load SceneConfig: {ex.Message}");
        }
    }
}

internal static class EnumHelper
{
    /// <summary>
    /// Parse enum from underscore_case (e.g. "light_rain" → LightRain).
    /// </summary>
    public static T ParseUnderscore<T>(string value) where T : struct, Enum
    {
        // Try direct parse first
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;
        // Convert underscore_case to PascalCase
        var pascal = string.Concat(value.Split('_').Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : s));
        return Enum.Parse<T>(pascal, ignoreCase: true);
    }
}

public sealed class WeatherWeightEntry
{
    public Season Season { get; set; }
    public Dictionary<WeatherType, float> Weights { get; set; } = new();
}

public sealed class WeatherWeightsTable : IDataTable<WeatherWeightEntry>
{
    private readonly List<WeatherWeightEntry> _entries = new();

    public WeatherWeightEntry Get(string id)
    {
        var season = Enum.Parse<Season>(id, ignoreCase: true);
        return _entries.First(e => e.Season == season);
    }

    public IReadOnlyList<WeatherWeightEntry> GetAll() => _entries;
    public int Count => _entries.Count;

    public void Load(string yamlContent, string filePath)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var raw = deserializer.Deserialize<Dictionary<string, Dictionary<string, float>>>(yamlContent);
            if (raw == null) throw new DataLoadException(filePath, 1, "Empty weather weights");

            _entries.Clear();
            foreach (var (seasonKey, weights) in raw)
            {
                var season = EnumHelper.ParseUnderscore<Season>(seasonKey);
                var entry = new WeatherWeightEntry { Season = season };
                foreach (var (weatherKey, weight) in weights)
                {
                    var weather = EnumHelper.ParseUnderscore<WeatherType>(weatherKey);
                    entry.Weights[weather] = weight;
                }
                _entries.Add(entry);
            }
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(
                filePath, (int)ex.Start.Line, $"Failed to load WeatherWeights: {ex.Message}");
        }
    }
}

public sealed class RegionFilterEntry
{
    public RegionType Region { get; set; }
    public HashSet<WeatherType> AllowedWeathers { get; set; } = new();
}

public sealed class RegionFilterTable : IDataTable<RegionFilterEntry>
{
    private readonly List<RegionFilterEntry> _entries = new();

    public RegionFilterEntry Get(string id)
    {
        var region = Enum.Parse<RegionType>(id, ignoreCase: true);
        return _entries.First(e => e.Region == region);
    }

    public IReadOnlyList<RegionFilterEntry> GetAll() => _entries;
    public int Count => _entries.Count;

    public void Load(string yamlContent, string filePath)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var raw = deserializer.Deserialize<Dictionary<string, List<string>>>(yamlContent);
            if (raw == null) throw new DataLoadException(filePath, 1, "Empty region filter");

            _entries.Clear();
            foreach (var (regionKey, weatherList) in raw)
            {
                var region = EnumHelper.ParseUnderscore<RegionType>(regionKey);
                var entry = new RegionFilterEntry { Region = region };
                foreach (var w in weatherList)
                    entry.AllowedWeathers.Add(EnumHelper.ParseUnderscore<WeatherType>(w));
                _entries.Add(entry);
            }
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(
                filePath, (int)ex.Start.Line, $"Failed to load RegionFilter: {ex.Message}");
        }
    }
}
