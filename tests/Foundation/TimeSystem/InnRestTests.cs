using Xunit;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class InnRestTests
{
    private readonly EventBus _eventBus = new();

    private TimeManager CreateManager()
    {
        return new TimeManager(_eventBus, constitution: 10, daysPerSeason: 30);
    }

    // ─── AC1: 小憩 ─────────────────────────────────────────

    [Fact]
    public void Nap_Advances1Shichen()
    {
        var tm = CreateManager();
        var result = InnRestLogic.Nap(tm);
        Assert.Equal(1, result.ShichenAdvanced);
        Assert.Equal(Shichen.Chen, tm.Calendar.CurrentShichen);
    }

    [Fact]
    public void Nap_Restores30Percent()
    {
        var tm = CreateManager();
        tm.ConsumeStamina(150f); // 力竭
        var result = InnRestLogic.Nap(tm);
        // 30% of 150 = 45
        Assert.Equal(45f, result.StaminaRestored);
        Assert.Equal(45f, tm.Stamina.CurrentStamina);
    }

    // ─── AC2: 过夜 ─────────────────────────────────────────

    [Fact]
    public void Overnight_AdvancesToNextMao()
    {
        var tm = CreateManager();
        // 默认在 Mao(3), 到次日 Mao = 12 时辰
        var result = InnRestLogic.Overnight(tm);
        Assert.Equal(Shichen.Mao, tm.Calendar.CurrentShichen);
        Assert.Equal(12, result.ShichenAdvanced);
        Assert.True(result.DayChanged);
    }

    [Fact]
    public void Overnight_FromYou_AdvancesToNextMao()
    {
        var tm = CreateManager();
        tm.Calendar.SetState(1, Shichen.You, 0f, 30); // 酉(9)
        var result = InnRestLogic.Overnight(tm);
        // (12-9)+3 = 6 时辰
        Assert.Equal(6, result.ShichenAdvanced);
        Assert.Equal(Shichen.Mao, tm.Calendar.CurrentShichen);
    }

    [Fact]
    public void Overnight_FullRestore()
    {
        var tm = CreateManager();
        tm.ConsumeStamina(100f); // 剩 50
        var result = InnRestLogic.Overnight(tm);
        Assert.True(result.FullRestore);
        Assert.Equal(100f, result.StaminaRestored);
        Assert.Equal(tm.Stamina.MaxStamina, tm.Stamina.CurrentStamina);
    }

    // ─── AC3: 自选时辰 ─────────────────────────────────────

    [Fact]
    public void CustomRest_AdvancesToTargetShichen()
    {
        var tm = CreateManager();
        // 从 Mao(3) 到 Wu(6) = 3 时辰
        var result = InnRestLogic.CustomRest(tm, Shichen.Wu);
        Assert.Equal(3, result.ShichenAdvanced);
        Assert.Equal(Shichen.Wu, tm.Calendar.CurrentShichen);
    }

    [Fact]
    public void CustomRest_ProportionalRestore()
    {
        var tm = CreateManager();
        tm.ConsumeStamina(150f); // 力竭
        // 从 Mao(3) 到 Wu(6) = 3 时辰 → 3/12 = 25% 恢复
        var result = InnRestLogic.CustomRest(tm, Shichen.Wu);
        // 25% of 150 = 37.5
        Assert.Equal(37.5f, result.StaminaRestored);
    }

    [Fact]
    public void CustomRest_SameShichen_FullRound()
    {
        var tm = CreateManager();
        // 从 Mao 到 Mao = 12 时辰（一整轮）
        var result = InnRestLogic.CustomRest(tm, Shichen.Mao);
        Assert.Equal(12, result.ShichenAdvanced);
    }

    // ─── AC4: 延迟事件在休息期间触发 ────────────────────────

    [Fact]
    public void Overnight_TriggersDelayedEvents()
    {
        var tm = CreateManager();
        bool triggered = false;
        tm.Scheduler.RegisterDelayedEvent("letter", 2, () => triggered = true);

        // 从 day 1 过夜到 day 2
        InnRestLogic.Overnight(tm);
        Assert.True(triggered);
    }

    // ─── AC5: (标记逻辑由上层处理，此处验证事件确实被触发) ──

    [Fact]
    public void Overnight_DayAdvancedEventFires()
    {
        var tm = CreateManager();
        DayAdvancedEvent? received = null;
        _eventBus.Subscribe<DayAdvancedEvent>(e => received = e);

        InnRestLogic.Overnight(tm);
        Assert.NotNull(received);
        Assert.Equal(2, received!.NewDay);
    }

    // ─── Helper: CalculateShichenToNextMao ───────────────────

    [Theory]
    [InlineData(Shichen.Mao, 12)]   // 3→次日3 = 12
    [InlineData(Shichen.Zi, 3)]     // 0→3 = 3 (同日? 不, 次日)
    [InlineData(Shichen.Hai, 4)]    // 11→次日3 = (12-11)+3 = 4
    [InlineData(Shichen.You, 6)]    // 9→次日3 = (12-9)+3 = 6
    [InlineData(Shichen.Chen, 11)]  // 4→次日3 = (12-4)+3 = 11
    public void CalculateShichenToNextMao_Correct(Shichen current, int expected)
    {
        Assert.Equal(expected, InnRestLogic.CalculateShichenToNextMao(current));
    }

    [Theory]
    [InlineData(Shichen.Mao, Shichen.Wu, 3)]    // 3→6 = 3
    [InlineData(Shichen.Wu, Shichen.Mao, 9)]    // 6→3 = (3-6+12) = 9
    [InlineData(Shichen.Zi, Shichen.Hai, 11)]   // 0→11 = 11
    [InlineData(Shichen.Hai, Shichen.Zi, 1)]    // 11→0 = 1
    public void CalculateShichenTo_Correct(Shichen from, Shichen to, int expected)
    {
        Assert.Equal(expected, InnRestLogic.CalculateShichenTo(from, to));
    }
}
