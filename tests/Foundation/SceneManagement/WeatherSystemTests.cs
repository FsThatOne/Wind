using Xunit;
using FengZhi.Foundation.SceneManagement;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.SceneManagement;

public class WeatherSystemTests
{
    // ─── AC1: WeatherType 枚举 ──────────────────────────────

    [Fact]
    public void WeatherType_Has7Values()
    {
        Assert.Equal(7, Enum.GetValues<WeatherType>().Length);
    }

    // ─── AC2: 概率池构建 + 区域过滤 + 归一化 ────────────────

    [Fact]
    public void BuildProbabilityPool_ZhongYuan_NoFilter()
    {
        var pool = WeatherSystem.BuildProbabilityPool(Season.Summer, RegionType.ZhongYuan);
        float total = pool.Values.Sum();
        Assert.InRange(total, 0.99f, 1.01f); // 归一化
    }

    [Fact]
    public void BuildProbabilityPool_JiangNan_NoSandstorm()
    {
        var pool = WeatherSystem.BuildProbabilityPool(Season.Summer, RegionType.JiangNan);
        Assert.False(pool.ContainsKey(WeatherType.Sandstorm));
        Assert.InRange(pool.Values.Sum(), 0.99f, 1.01f);
    }

    [Fact]
    public void BuildProbabilityPool_SaiBei_NoRain()
    {
        var pool = WeatherSystem.BuildProbabilityPool(Season.Spring, RegionType.SaiBei);
        Assert.False(pool.ContainsKey(WeatherType.LightRain));
        Assert.False(pool.ContainsKey(WeatherType.HeavyRain));
    }

    [Fact]
    public void BuildProbabilityPool_GoBi_NoRainNoSnow()
    {
        var pool = WeatherSystem.BuildProbabilityPool(Season.Winter, RegionType.GoBi);
        Assert.False(pool.ContainsKey(WeatherType.LightRain));
        Assert.False(pool.ContainsKey(WeatherType.HeavyRain));
        Assert.False(pool.ContainsKey(WeatherType.Snow));
    }

    // ─── AC3: DailyRefresh 按概率池随机 + 持续天数 ──────────

    [Fact]
    public void DailyRefresh_WhenDurationExpired_ChangesWeather()
    {
        var ws = new WeatherSystem();
        // 初始 RemainingDays=1, 刷新后减到0然后重随机
        ws.DailyRefresh(Season.Summer, RegionType.ZhongYuan, 0.0f, 2);
        // randomValue=0 → 第一个概率最高的天气
        Assert.Equal(2, ws.RemainingDays);
    }

    [Fact]
    public void DailyRefresh_WhenDurationRemaining_NoChange()
    {
        var ws = new WeatherSystem();
        ws.DailyRefresh(Season.Spring, RegionType.ZhongYuan, 0.5f, 3); // 设置3天
        var weather1 = ws.CurrentWeather;

        ws.DailyRefresh(Season.Spring, RegionType.ZhongYuan, 0.9f, 1); // 还有 2 天剩余
        Assert.Equal(weather1, ws.CurrentWeather); // 不变
        Assert.Equal(2, ws.RemainingDays);
    }

    // ─── AC4: ForceWeather 覆盖刷新 ─────────────────────────

    [Fact]
    public void ForceWeather_SetsWeatherAndFlag()
    {
        var ws = new WeatherSystem();
        ws.ForceWeather(WeatherType.HeavyRain);
        Assert.Equal(WeatherType.HeavyRain, ws.CurrentWeather);
        Assert.True(ws.IsForced);
    }

    [Fact]
    public void ForceWeather_BlocksDailyRefresh()
    {
        var ws = new WeatherSystem();
        ws.ForceWeather(WeatherType.Snow);
        ws.DailyRefresh(Season.Summer, RegionType.ZhongYuan, 0.0f, 1);
        Assert.Equal(WeatherType.Snow, ws.CurrentWeather); // 不变
    }

    [Fact]
    public void EndForceWeather_AllowsNextRefresh()
    {
        var ws = new WeatherSystem();
        ws.ForceWeather(WeatherType.Fog);
        ws.EndForceWeather();
        Assert.False(ws.IsForced);
        Assert.Equal(0, ws.RemainingDays); // 下次刷新立即生效
    }

    // ─── AC5: 持续天数倒计时 ────────────────────────────────

    [Fact]
    public void DailyRefresh_CountdownCorrect()
    {
        var ws = new WeatherSystem();
        ws.DailyRefresh(Season.Spring, RegionType.ZhongYuan, 0.5f, 3);
        Assert.Equal(3, ws.RemainingDays);
        ws.DailyRefresh(Season.Spring, RegionType.ZhongYuan, 0.5f, 1);
        Assert.Equal(2, ws.RemainingDays); // 3-1=2
        ws.DailyRefresh(Season.Spring, RegionType.ZhongYuan, 0.5f, 1);
        Assert.Equal(1, ws.RemainingDays); // 2-1=1
    }

    // ─── AC6: 区域过滤验证 ──────────────────────────────────

    [Fact]
    public void GetRegionFilter_JiangNan_ContainsSandstorm()
    {
        var filter = WeatherSystem.GetRegionFilter(RegionType.JiangNan);
        Assert.Contains(WeatherType.Sandstorm, filter);
        Assert.Single(filter);
    }

    [Fact]
    public void GetRegionFilter_ZhongYuan_Empty()
    {
        var filter = WeatherSystem.GetRegionFilter(RegionType.ZhongYuan);
        Assert.Empty(filter);
    }

    // ─── 季节权重合理性 ─────────────────────────────────────

    [Fact]
    public void GetSeasonWeights_Winter_NoRain()
    {
        var weights = WeatherSystem.GetSeasonWeights(Season.Winter);
        Assert.Equal(0, weights[WeatherType.LightRain]);
        Assert.Equal(0, weights[WeatherType.HeavyRain]);
        Assert.True(weights[WeatherType.Snow] > 0);
    }
}
