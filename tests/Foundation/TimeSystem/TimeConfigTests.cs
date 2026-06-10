using Xunit;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class TimeConfigTests
{
    private const string ValidYaml = @"
days_per_season: 25
base_stamina: 120
stamina_per_con: 8
rest_ratio_nap: 0.35
fatigue_threshold: 0.4
zero_stamina_speed_mult: 0.6
stamina_per_grid: 0.3
";

    private const string PartialYaml = @"
days_per_season: 20
base_stamina: 80
";

    private const string InvalidYaml = @"
days_per_season: [invalid
  nested: broken
";

    // ─── AC1: 完整字段加载 ──────────────────────────────────

    [Fact]
    public void LoadFromYaml_ValidConfig_AllFieldsParsed()
    {
        var config = TimeConfigLoader.LoadFromYaml(ValidYaml, "test.yaml");

        Assert.Equal(25, config.DaysPerSeason);
        Assert.Equal(120, config.BaseStamina);
        Assert.Equal(8, config.StaminaPerCon);
        Assert.Equal(0.35f, config.RestRatioNap, 3);
        Assert.Equal(0.4f, config.FatigueThreshold, 3);
        Assert.Equal(0.6f, config.ZeroStaminaSpeedMult, 3);
        Assert.Equal(0.3f, config.StaminaPerGrid, 3);
    }

    // ─── AC2: DataRegistry 集成 ─────────────────────────────

    [Fact]
    public void TimeConfigTable_GetDefault_ReturnsConfig()
    {
        var config = TimeConfigLoader.LoadFromYaml(ValidYaml, "test.yaml");
        var table = new TimeConfigTable(config);

        Assert.Equal(1, table.Count);
        Assert.NotNull(table.Get("default"));
        Assert.Equal(25, table.Get("default")!.DaysPerSeason);
    }

    [Fact]
    public void TimeConfigTable_GetNonDefault_ReturnsNull()
    {
        var config = new TimeConfig();
        var table = new TimeConfigTable(config);
        Assert.Null(table.Get("other"));
    }

    [Fact]
    public void DataRegistry_RegistersTimeConfig()
    {
        var config = new TimeConfig();
        var table = new TimeConfigTable(config);
        var registry = new DataRegistry();
        registry.RegisterTable(table);

        var loaded = registry.GetTable<TimeConfig>();
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded!.Count);
    }

    // ─── AC3: 格式错误抛 DataLoadException ──────────────────

    [Fact]
    public void LoadFromYaml_InvalidFormat_ThrowsDataLoadException()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            TimeConfigLoader.LoadFromYaml(InvalidYaml, "bad.yaml"));

        Assert.Equal("bad.yaml", ex.FilePath);
        Assert.NotNull(ex.LineNumber);
    }

    [Fact]
    public void LoadFromYaml_EmptyContent_ThrowsDataLoadException()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            TimeConfigLoader.LoadFromYaml("", "empty.yaml"));

        Assert.Equal("empty.yaml", ex.FilePath);
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void LoadFromFile_FileNotFound_ThrowsDataLoadException()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            TimeConfigLoader.LoadFromFile("/nonexistent/path.yaml"));

        Assert.Contains("not found", ex.Message.ToLower());
    }

    // ─── AC4: 缺少可选字段时使用默认值 ─────────────────────

    [Fact]
    public void LoadFromYaml_PartialConfig_DefaultsForMissing()
    {
        var config = TimeConfigLoader.LoadFromYaml(PartialYaml, "partial.yaml");

        // 显式设置的值
        Assert.Equal(20, config.DaysPerSeason);
        Assert.Equal(80, config.BaseStamina);

        // 未设置的使用 GDD 默认值
        Assert.Equal(TimeConfig.DefaultStaminaPerCon, config.StaminaPerCon);
        Assert.Equal(TimeConfig.DefaultRestRatioNap, config.RestRatioNap);
        Assert.Equal(TimeConfig.DefaultFatigueThreshold, config.FatigueThreshold);
        Assert.Equal(TimeConfig.DefaultZeroStaminaSpeedMult, config.ZeroStaminaSpeedMult);
        Assert.Equal(TimeConfig.DefaultStaminaPerGrid, config.StaminaPerGrid);
    }

    [Fact]
    public void TimeConfig_DefaultValues_MatchGDD()
    {
        var config = new TimeConfig();
        Assert.Equal(30, config.DaysPerSeason);
        Assert.Equal(100, config.BaseStamina);
        Assert.Equal(5, config.StaminaPerCon);
        Assert.Equal(0.3f, config.RestRatioNap);
        Assert.Equal(0.5f, config.FatigueThreshold);
        Assert.Equal(0.5f, config.ZeroStaminaSpeedMult);
        Assert.Equal(0.2f, config.StaminaPerGrid);
    }
}
