using Xunit;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class TimeAdvanceIntegrationTests
{
    private readonly EventBus _eventBus = new();

    private TimeManager CreateManager(int daysPerSeason = 30)
    {
        return new TimeManager(_eventBus, constitution: 10, daysPerSeason: daysPerSeason);
    }

    // ─── AC1: AdvanceTime 推进日历 ──────────────────────────

    [Fact]
    public void AdvanceTime_PositiveDelta_ReturnsTrue()
    {
        var tm = CreateManager();
        Assert.True(tm.AdvanceTime(0.01f));
    }

    [Fact]
    public void AdvanceTime_ZeroDelta_ReturnsFalse()
    {
        var tm = CreateManager();
        Assert.False(tm.AdvanceTime(0f));
    }

    [Fact]
    public void AdvanceTime_AdvancesCalendar()
    {
        var tm = CreateManager();
        tm.AdvanceTime(GameCalendar.ShichenDuration); // 1 时辰
        Assert.Equal(Shichen.Chen, tm.Calendar.CurrentShichen);
    }

    // ─── AC2: 时辰切换事件 ──────────────────────────────────

    [Fact]
    public void AdvanceTime_ShichenChange_PublishesEvent()
    {
        var tm = CreateManager();
        ShichenChangedEvent? received = null;
        _eventBus.Subscribe<ShichenChangedEvent>(e => received = e);

        tm.AdvanceTime(GameCalendar.ShichenDuration);

        Assert.NotNull(received);
        Assert.Equal(Shichen.Mao, received!.OldShichen);
        Assert.Equal(Shichen.Chen, received.NewShichen);
    }

    [Fact]
    public void AdvanceTime_MultipleShichen_PublishesMultipleEvents()
    {
        var tm = CreateManager();
        var events = new List<ShichenChangedEvent>();
        _eventBus.Subscribe<ShichenChangedEvent>(e => events.Add(e));

        tm.AdvanceTime(GameCalendar.ShichenDuration * 3 + 0.001f); // 3 时辰

        Assert.Equal(3, events.Count);
    }

    // ─── AC3: 日切换事件 + 延迟事件触发 ─────────────────────

    [Fact]
    public void AdvanceTime_DayChange_PublishesDayEvent()
    {
        var tm = CreateManager();
        // 从 Mao(3) 到 Zi(0)次日 = 9 个时辰
        DayAdvancedEvent? received = null;
        _eventBus.Subscribe<DayAdvancedEvent>(e => received = e);

        tm.AdvanceTime(GameCalendar.ShichenDuration * 9 + 0.001f);

        Assert.NotNull(received);
        Assert.Equal(2, received!.NewDay);
    }

    [Fact]
    public void AdvanceTime_DayChange_TriggersScheduledEvents()
    {
        var tm = CreateManager();
        bool triggered = false;
        tm.Scheduler.RegisterDelayedEvent("test_evt", 2, () => triggered = true);

        // 推进到 day 2
        tm.AdvanceTime(GameCalendar.ShichenDuration * 9 + 0.001f);

        Assert.True(triggered);
    }

    // ─── AC4: 季节切换事件 ──────────────────────────────────

    [Fact]
    public void AdvanceTime_SeasonChange_PublishesEvent()
    {
        var tm = CreateManager(daysPerSeason: 30);
        // 设置日历到 day 30, Hai 时辰末
        tm.Calendar.SetState(30, Shichen.Hai, 0f, 30);

        SeasonChangedEvent? received = null;
        _eventBus.Subscribe<SeasonChangedEvent>(e => received = e);

        tm.AdvanceTime(GameCalendar.ShichenDuration); // 触发日切换 → 第 31 天 → 夏

        Assert.NotNull(received);
        Assert.Equal(Season.Spring, received!.OldSeason);
        Assert.Equal(Season.Summer, received.NewSeason);
    }

    // ─── AC5: 消耗体力并发布事件 ─────────────────────────────

    [Fact]
    public void ConsumeStamina_PublishesStaminaEvent()
    {
        var tm = CreateManager();
        StaminaChangedEvent? received = null;
        _eventBus.Subscribe<StaminaChangedEvent>(e => received = e);

        tm.ConsumeStamina(30f);

        Assert.NotNull(received);
        Assert.Equal(150f, received!.OldStamina);
        Assert.Equal(120f, received.NewStamina);
        Assert.Equal(StaminaState.Vigorous, received.NewState);
    }

    [Fact]
    public void ConsumeStamina_NoConsumption_NoEvent()
    {
        var tm = CreateManager();
        // 先消耗至0
        tm.ConsumeStamina(150f);

        StaminaChangedEvent? received = null;
        _eventBus.Subscribe<StaminaChangedEvent>(e => received = e);

        tm.ConsumeStamina(10f); // 力竭不扣

        Assert.Null(received);
    }

    [Fact]
    public void RestoreStamina_PublishesEvent()
    {
        var tm = CreateManager();
        tm.ConsumeStamina(50f);

        StaminaChangedEvent? received = null;
        _eventBus.Subscribe<StaminaChangedEvent>(e => received = e);

        tm.RestoreStamina(20f);

        Assert.NotNull(received);
        Assert.Equal(120f, received!.NewStamina);
    }

    // ─── AC6: 防递归 ────────────────────────────────────────

    [Fact]
    public void AdvanceTime_DuringAdvance_ReturnsFalse()
    {
        var tm = CreateManager();
        bool recursiveResult = true;

        // 注册在时辰变化时尝试递归推进的事件
        _eventBus.Subscribe<ShichenChangedEvent>(_ =>
        {
            recursiveResult = tm.AdvanceTime(0.01f);
        });

        tm.AdvanceTime(GameCalendar.ShichenDuration);

        Assert.False(recursiveResult);
    }

    [Fact]
    public void IsAdvancing_FalseAfterComplete()
    {
        var tm = CreateManager();
        tm.AdvanceTime(0.01f);
        Assert.False(tm.IsAdvancing);
    }
}
