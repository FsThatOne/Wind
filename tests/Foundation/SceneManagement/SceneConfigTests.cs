using FengZhi.Foundation.Data;
using FengZhi.Foundation.SceneManagement;
using FengZhi.Foundation.TimeSystem;
using Xunit;

namespace Foundation.Tests.SceneManagement;

public class SceneConfigTests
{
    // --- AC1: SceneConfig YAML ---

    [Fact]
    public void SceneConfigTable_LoadsAllParameters()
    {
        var yaml = @"
transition_fade_duration: 0.8
transition_ink_duration: 1.5
preload_retry_count: 5
time_per_grid: 0.02
stamina_per_grid: 0.3
zero_stamina_speed_mult: 0.4
station_time_mult: 0.6
station_base_fee: 15
station_distance_fee: 3
night_darken: 0.22
season_transition_duration: 2.5
";
        var table = new SceneConfigTable();
        table.Load(yaml, "test.yaml");

        var config = table.Get("default");
        Assert.Equal(0.8f, config.TransitionFadeDuration, 0.001f);
        Assert.Equal(1.5f, config.TransitionInkDuration, 0.001f);
        Assert.Equal(5, config.PreloadRetryCount);
        Assert.Equal(0.02f, config.TimePerGrid, 0.001f);
        Assert.Equal(0.3f, config.StaminaPerGrid, 0.001f);
        Assert.Equal(0.4f, config.ZeroStaminaSpeedMult, 0.001f);
        Assert.Equal(0.6f, config.StationTimeMult, 0.001f);
        Assert.Equal(15f, config.StationBaseFee, 0.01f);
        Assert.Equal(3f, config.StationDistanceFee, 0.01f);
        Assert.Equal(0.22f, config.NightDarken, 0.001f);
        Assert.Equal(2.5f, config.SeasonTransitionDuration, 0.001f);
    }

    // --- AC2: WeatherWeights YAML by season × weather ---

    [Fact]
    public void WeatherWeightsTable_LoadsMatrix()
    {
        var yaml = @"
spring:
  clear: 0.30
  cloudy: 0.25
  light_rain: 0.25
  heavy_rain: 0.10
  snow: 0.00
  fog: 0.10
  sandstorm: 0.00
summer:
  clear: 0.25
  cloudy: 0.20
  light_rain: 0.20
  heavy_rain: 0.20
  snow: 0.00
  fog: 0.15
  sandstorm: 0.00
";
        var table = new WeatherWeightsTable();
        table.Load(yaml, "weather_weights.yaml");

        Assert.Equal(2, table.Count);
        var spring = table.Get("Spring");
        Assert.Equal(Season.Spring, spring.Season);
        Assert.Equal(0.30f, spring.Weights[WeatherType.Clear], 0.001f);
        Assert.Equal(0.25f, spring.Weights[WeatherType.LightRain], 0.001f);
    }

    // --- AC3: RegionFilter YAML by region × weather mask ---

    [Fact]
    public void RegionFilterTable_LoadsMask()
    {
        var yaml = @"
jiang_nan:
  - clear
  - cloudy
  - light_rain
  - heavy_rain
  - fog
sai_bei:
  - clear
  - cloudy
  - snow
  - fog
  - sandstorm
";
        var table = new RegionFilterTable();
        table.Load(yaml, "region_filter.yaml");

        Assert.Equal(2, table.Count);
        var jiangNan = table.Get("JiangNan");
        Assert.Contains(WeatherType.Clear, jiangNan.AllowedWeathers);
        Assert.Contains(WeatherType.Fog, jiangNan.AllowedWeathers);
        Assert.DoesNotContain(WeatherType.Snow, jiangNan.AllowedWeathers);
        Assert.DoesNotContain(WeatherType.Sandstorm, jiangNan.AllowedWeathers);

        var saiBei = table.Get("SaiBei");
        Assert.Contains(WeatherType.Snow, saiBei.AllowedWeathers);
        Assert.DoesNotContain(WeatherType.LightRain, saiBei.AllowedWeathers);
    }

    // --- AC4: DataRegistry integration ---

    [Fact]
    public void DataRegistry_RegistersSceneConfigTable()
    {
        var registry = new DataRegistry();
        var table = new SceneConfigTable();
        table.Load("preload_retry_count: 7", "test.yaml");
        registry.RegisterTable(table);

        var retrieved = registry.GetTable<SceneConfig>();
        Assert.NotNull(retrieved);
        Assert.Equal(7, retrieved!.Get("default").PreloadRetryCount);
    }

    // --- AC5: Format error → DataLoadException ---

    [Fact]
    public void SceneConfigTable_InvalidYaml_ThrowsDataLoadException()
    {
        var table = new SceneConfigTable();
        var ex = Assert.Throws<DataLoadException>(() =>
            table.Load("invalid: [unclosed", "bad.yaml"));
        Assert.Equal("bad.yaml", ex.FilePath);
    }

    [Fact]
    public void WeatherWeightsTable_InvalidYaml_ThrowsDataLoadException()
    {
        var table = new WeatherWeightsTable();
        var ex = Assert.Throws<DataLoadException>(() =>
            table.Load("invalid: [unclosed", "bad.yaml"));
        Assert.Equal("bad.yaml", ex.FilePath);
    }

    [Fact]
    public void RegionFilterTable_InvalidYaml_ThrowsDataLoadException()
    {
        var table = new RegionFilterTable();
        var ex = Assert.Throws<DataLoadException>(() =>
            table.Load("invalid: [unclosed", "bad.yaml"));
        Assert.Equal("bad.yaml", ex.FilePath);
    }

    // --- AC6: Missing optional fields use defaults ---

    [Fact]
    public void SceneConfigTable_EmptyYaml_UsesDefaults()
    {
        var table = new SceneConfigTable();
        table.Load("{}", "empty.yaml");

        var config = table.Get("default");
        Assert.Equal(0.5f, config.TransitionFadeDuration, 0.001f);
        Assert.Equal(1.0f, config.TransitionInkDuration, 0.001f);
        Assert.Equal(3, config.PreloadRetryCount);
        Assert.Equal(0.01f, config.TimePerGrid, 0.001f);
        Assert.Equal(0.20f, config.NightDarken, 0.001f);
    }

    [Fact]
    public void SceneConfigTable_PartialYaml_MergesWithDefaults()
    {
        var table = new SceneConfigTable();
        table.Load("night_darken: 0.18", "partial.yaml");

        var config = table.Get("default");
        Assert.Equal(0.18f, config.NightDarken, 0.001f);
        // Other fields keep defaults
        Assert.Equal(0.5f, config.TransitionFadeDuration, 0.001f);
    }
}
