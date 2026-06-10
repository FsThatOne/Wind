using Xunit;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class CalendarCoreTests
{
    // ─── AC1: Shichen 枚举 12 个 + LightPhase ──────────────

    [Fact]
    public void Shichen_Has12Values()
    {
        Assert.Equal(12, Enum.GetValues<Shichen>().Length);
    }

    [Fact]
    public void LightPhase_Has4Values()
    {
        Assert.Equal(4, Enum.GetValues<LightPhase>().Length);
    }

    // ─── AC2: Season 枚举 ──────────────────────────────────

    [Fact]
    public void Season_Has4Values()
    {
        Assert.Equal(4, Enum.GetValues<Season>().Length);
        Assert.Equal(0, (int)Season.Spring);
        Assert.Equal(3, (int)Season.Winter);
    }

    // ─── AC3: GameCalendar 初始状态 ─────────────────────────

    [Fact]
    public void GameCalendar_DefaultState()
    {
        var cal = new GameCalendar();
        Assert.Equal(1, cal.CurrentDay);
        Assert.Equal(Shichen.Mao, cal.CurrentShichen);
        Assert.Equal(0f, cal.DayProgress);
        Assert.Equal(Season.Spring, cal.CurrentSeason);
        Assert.Equal(1, cal.SeasonDay);
    }

    // ─── AC4: F2 时间推进 ──────────────────────────────────

    [Fact]
    public void AdvanceProgress_SmallDelta_NoShichenChange()
    {
        var cal = new GameCalendar();
        var result = cal.AdvanceProgress(0.01f); // 不够 1/12≈0.0833

        Assert.Empty(result.ShichenChanges);
        Assert.False(result.DayAdvanced);
        Assert.Equal(Shichen.Mao, cal.CurrentShichen);
    }

    [Fact]
    public void AdvanceProgress_CrossShichenBoundary_TriggersShichenChange()
    {
        var cal = new GameCalendar();
        cal.AdvanceProgress(0.08f); // 接近但未超过
        var result = cal.AdvanceProgress(0.01f); // 0.08+0.01=0.09 > 1/12

        Assert.Single(result.ShichenChanges);
        Assert.Equal(Shichen.Mao, result.ShichenChanges[0].From);
        Assert.Equal(Shichen.Chen, result.ShichenChanges[0].To);
        Assert.Equal(Shichen.Chen, cal.CurrentShichen);
    }

    [Fact]
    public void AdvanceProgress_LargeDelta_MultipleShichenChanges()
    {
        var cal = new GameCalendar();
        // 0.25 天 = 3 个时辰
        var result = cal.AdvanceProgress(0.25f);

        Assert.Equal(3, result.ShichenChanges.Count);
        Assert.Equal(Shichen.Wu, cal.CurrentShichen); // 卯→辰→巳→午
    }

    [Fact]
    public void AdvanceProgress_FullDay_TriggersDayChange()
    {
        var cal = new GameCalendar();
        // 初始在卯(index 3)，需要经过 9 个时辰到达亥末 → 日切换
        // 9 * 1/12 = 0.75 天
        var result = cal.AdvanceProgress(0.75f);

        Assert.True(result.DayAdvanced);
        Assert.Equal(2, result.NewDay);
        Assert.Equal(2, cal.CurrentDay);
    }

    [Fact]
    public void AdvanceProgress_GDD_Example_DayProgress0075_Plus001()
    {
        var cal = new GameCalendar();
        cal.AdvanceProgress(0.075f); // 接近 1/12≈0.0833
        Assert.Equal(Shichen.Mao, cal.CurrentShichen); // 还没超

        var result = cal.AdvanceProgress(0.01f); // 0.085 > 0.0833 → 切换
        Assert.Single(result.ShichenChanges);
        Assert.Equal(Shichen.Chen, cal.CurrentShichen);
    }

    // ─── AC5: F4 季节计算 ──────────────────────────────────

    [Theory]
    [InlineData(1, 30, Season.Spring)]
    [InlineData(30, 30, Season.Spring)]
    [InlineData(31, 30, Season.Summer)]
    [InlineData(60, 30, Season.Summer)]
    [InlineData(61, 30, Season.Autumn)]
    [InlineData(91, 30, Season.Winter)]
    [InlineData(121, 30, Season.Spring)] // 循环
    public void GetSeasonForDay_CorrectMapping(int day, int daysPerSeason, Season expected)
    {
        Assert.Equal(expected, GameCalendar.GetSeasonForDay(day, daysPerSeason));
    }

    [Fact]
    public void AdvanceProgress_SeasonChange_AtDay31()
    {
        var cal = new GameCalendar();
        // 推进到第30天结束 → 第31天 → 夏季
        // 需要 29 天 + 9时辰(从卯到亥末) = 29 + 0.75 = 29.75 天
        // 但更简单：先设置状态到 day=30, seasonDay=30
        cal.SetState(30, Shichen.Hai, 0f, 30);
        var result = cal.AdvanceProgress(GameCalendar.ShichenDuration); // 1 时辰 → 日切换

        Assert.True(result.DayAdvanced);
        Assert.True(result.SeasonChanged);
        Assert.Equal(Season.Spring, result.OldSeason);
        Assert.Equal(Season.Summer, result.NewSeason);
        Assert.Equal(Season.Summer, cal.CurrentSeason);
    }

    // ─── AC6: 光照分类映射 ─────────────────────────────────

    [Theory]
    [InlineData(Shichen.Yin, LightPhase.Dawn)]
    [InlineData(Shichen.Mao, LightPhase.Dawn)]
    [InlineData(Shichen.Chen, LightPhase.Day)]
    [InlineData(Shichen.Si, LightPhase.Day)]
    [InlineData(Shichen.Wu, LightPhase.Day)]
    [InlineData(Shichen.Wei, LightPhase.Day)]
    [InlineData(Shichen.Shen, LightPhase.Day)]
    [InlineData(Shichen.You, LightPhase.Dusk)]
    [InlineData(Shichen.Xu, LightPhase.Dusk)]
    [InlineData(Shichen.Zi, LightPhase.Night)]
    [InlineData(Shichen.Chou, LightPhase.Night)]
    [InlineData(Shichen.Hai, LightPhase.Night)]
    public void GetLightPhase_CorrectMapping(Shichen shichen, LightPhase expected)
    {
        Assert.Equal(expected, GameCalendar.GetLightPhase(shichen));
    }

    // ─── AC7: 安全默认值 ───────────────────────────────────

    [Fact]
    public void ResetToDefaults_RestoresDefaultState()
    {
        var cal = new GameCalendar();
        cal.AdvanceProgress(1.0f); // 推进一天
        cal.ResetToDefaults();

        Assert.Equal(1, cal.CurrentDay);
        Assert.Equal(Shichen.Mao, cal.CurrentShichen);
        Assert.Equal(0f, cal.DayProgress);
        Assert.Equal(Season.Spring, cal.CurrentSeason);
    }

    [Fact]
    public void SetState_InvalidDay_ResetsToDefaults()
    {
        var cal = new GameCalendar();
        cal.SetState(-1, Shichen.Mao, 0f, 30);

        Assert.Equal(1, cal.CurrentDay);
        Assert.Equal(Shichen.Mao, cal.CurrentShichen);
    }

    [Fact]
    public void SetState_ValidState_SetsCorrectly()
    {
        var cal = new GameCalendar();
        cal.SetState(45, Shichen.Wu, 0.02f, 30);

        Assert.Equal(45, cal.CurrentDay);
        Assert.Equal(Shichen.Wu, cal.CurrentShichen);
        Assert.Equal(Season.Summer, cal.CurrentSeason);
        Assert.Equal(15, cal.SeasonDay); // (45-1)%30 + 1 = 15
    }
}
