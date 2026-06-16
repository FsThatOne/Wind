namespace FengZhi.Foundation.SceneManagement;

/// <summary>7 种天气类型</summary>
public enum WeatherType
{
    Clear,
    Cloudy,
    LightRain,
    HeavyRain,
    Snow,
    Fog,
    Sandstorm
}

/// <summary>区域类型（用于天气过滤）</summary>
public enum RegionType
{
    JiangNan,   // 江南：禁止风沙
    ZhongYuan,  // 中原：全部允许
    SaiBei,     // 塞北：禁止雨
    GoBi        // 戈壁：禁止雨雪
}

/// <summary>
/// 天气系统纯逻辑。
/// GDD: §Core Rules 5b, §Formulas 4
/// </summary>
public sealed class WeatherSystem
{
    private static readonly Random _rng = new();

    public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
    public int RemainingDays { get; private set; } = 1;
    public bool IsForced { get; private set; } = false;

    private WeatherType _forcedWeather;

    /// <summary>
    /// AC3: 日刷新。每日调用，倒计时结束后重新随机天气。
    /// AC4: 强制天气期间跳过刷新。
    /// </summary>
    public void DailyRefresh(TimeSystem.Season season, RegionType region)
    {
        if (IsForced) return; // AC4

        RemainingDays--;
        if (RemainingDays > 0) return; // AC5: 持续天数未到

        // 随机新天气
        var pool = BuildProbabilityPool(season, region);
        CurrentWeather = WeightedRandom(pool);
        RemainingDays = _rng.Next(1, 4); // 1-3 天
    }

    /// <summary>可测试版本：注入随机结果</summary>
    public void DailyRefresh(TimeSystem.Season season, RegionType region, float randomValue, int durationDays)
    {
        if (IsForced) return;

        RemainingDays--;
        if (RemainingDays > 0) return;

        var pool = BuildProbabilityPool(season, region);
        CurrentWeather = SelectByValue(pool, randomValue);
        RemainingDays = Math.Clamp(durationDays, 1, 3);
    }

    /// <summary>AC4: 强制天气（剧情用）</summary>
    public void ForceWeather(WeatherType weather)
    {
        _forcedWeather = weather;
        CurrentWeather = weather;
        IsForced = true;
    }

    /// <summary>结束强制天气</summary>
    public void EndForceWeather()
    {
        IsForced = false;
        RemainingDays = 0; // 下次 DailyRefresh 立即刷新
    }

    /// <summary>
    /// AC2: 构建概率池 = 季节基础权重 × 区域过滤掩码 → 归一化。
    /// </summary>
    public static Dictionary<WeatherType, float> BuildProbabilityPool(TimeSystem.Season season, RegionType region)
    {
        var baseWeights = GetSeasonWeights(season);
        var filter = GetRegionFilter(region);

        var filtered = new Dictionary<WeatherType, float>();
        float total = 0;

        foreach (var (weather, weight) in baseWeights)
        {
            if (filter.Contains(weather)) continue; // AC6: 被区域过滤
            filtered[weather] = weight;
            total += weight;
        }

        // 归一化
        if (total > 0)
        {
            var keys = filtered.Keys.ToList();
            foreach (var key in keys)
                filtered[key] /= total;
        }

        return filtered;
    }

    /// <summary>季节基础权重表</summary>
    public static Dictionary<WeatherType, float> GetSeasonWeights(TimeSystem.Season season)
    {
        return season switch
        {
            TimeSystem.Season.Spring => new()
            {
                [WeatherType.Clear] = 30, [WeatherType.Cloudy] = 20,
                [WeatherType.LightRain] = 25, [WeatherType.HeavyRain] = 10,
                [WeatherType.Snow] = 0, [WeatherType.Fog] = 10, [WeatherType.Sandstorm] = 5
            },
            TimeSystem.Season.Summer => new()
            {
                [WeatherType.Clear] = 25, [WeatherType.Cloudy] = 15,
                [WeatherType.LightRain] = 15, [WeatherType.HeavyRain] = 25,
                [WeatherType.Snow] = 0, [WeatherType.Fog] = 10, [WeatherType.Sandstorm] = 10
            },
            TimeSystem.Season.Autumn => new()
            {
                [WeatherType.Clear] = 30, [WeatherType.Cloudy] = 25,
                [WeatherType.LightRain] = 15, [WeatherType.HeavyRain] = 5,
                [WeatherType.Snow] = 5, [WeatherType.Fog] = 15, [WeatherType.Sandstorm] = 5
            },
            _ => new() // Winter
            {
                [WeatherType.Clear] = 20, [WeatherType.Cloudy] = 15,
                [WeatherType.LightRain] = 0, [WeatherType.HeavyRain] = 0,
                [WeatherType.Snow] = 40, [WeatherType.Fog] = 15, [WeatherType.Sandstorm] = 10
            }
        };
    }

    /// <summary>区域过滤掩码（返回禁止的天气集合）</summary>
    public static HashSet<WeatherType> GetRegionFilter(RegionType region)
    {
        return region switch
        {
            RegionType.JiangNan => new() { WeatherType.Sandstorm },
            RegionType.SaiBei => new() { WeatherType.LightRain, WeatherType.HeavyRain },
            RegionType.GoBi => new() { WeatherType.LightRain, WeatherType.HeavyRain, WeatherType.Snow },
            _ => new() // ZhongYuan: 全部允许
        };
    }

    private static WeatherType WeightedRandom(Dictionary<WeatherType, float> pool)
    {
        float roll = (float)_rng.NextDouble();
        return SelectByValue(pool, roll);
    }

    private static WeatherType SelectByValue(Dictionary<WeatherType, float> pool, float value)
    {
        float cumulative = 0;
        foreach (var (weather, prob) in pool)
        {
            cumulative += prob;
            if (value <= cumulative) return weather;
        }
        return pool.Keys.Last();
    }
}
