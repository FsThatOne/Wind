using Xunit;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class DelayedEventSchedulerTests
{
    // ─── AC1: 注册延迟事件 ──────────────────────────────────

    [Fact]
    public void Register_AddsEvent()
    {
        var scheduler = new DelayedEventScheduler();
        scheduler.RegisterDelayedEvent("npc_letter", 5, () => { });
        Assert.Equal(1, scheduler.Count);
        Assert.True(scheduler.HasEvent("npc_letter"));
    }

    [Fact]
    public void Register_SameId_Overwrites()
    {
        var scheduler = new DelayedEventScheduler();
        scheduler.RegisterDelayedEvent("evt1", 3, () => { });
        scheduler.RegisterDelayedEvent("evt1", 7, () => { });
        Assert.Equal(1, scheduler.Count);
        Assert.Equal(7, scheduler.GetTriggerDay("evt1"));
    }

    [Fact]
    public void Register_NullId_Throws()
    {
        var scheduler = new DelayedEventScheduler();
        Assert.Throws<ArgumentException>(() =>
            scheduler.RegisterDelayedEvent("", 5, () => { }));
    }

    // ─── AC2: 按优先级升序执行 ──────────────────────────────

    [Fact]
    public void ProcessDay_ExecutesDueEvents_ByPriorityAscending()
    {
        var scheduler = new DelayedEventScheduler();
        var order = new List<string>();

        scheduler.RegisterDelayedEvent("low", 1, () => order.Add("low"), priority: 10);
        scheduler.RegisterDelayedEvent("high", 1, () => order.Add("high"), priority: 1);
        scheduler.RegisterDelayedEvent("mid", 1, () => order.Add("mid"), priority: 5);

        scheduler.ProcessDay(1);

        Assert.Equal(new[] { "high", "mid", "low" }, order);
    }

    [Fact]
    public void ProcessDay_OnlyDueEvents_NotFuture()
    {
        var scheduler = new DelayedEventScheduler();
        var executed = new List<string>();

        scheduler.RegisterDelayedEvent("day1", 1, () => executed.Add("day1"));
        scheduler.RegisterDelayedEvent("day5", 5, () => executed.Add("day5"));

        scheduler.ProcessDay(1);

        Assert.Single(executed);
        Assert.Equal("day1", executed[0]);
        Assert.Equal(1, scheduler.Count); // day5 仍在
    }

    [Fact]
    public void ProcessDay_PastDueEvents_AlsoExecuted()
    {
        var scheduler = new DelayedEventScheduler();
        scheduler.RegisterDelayedEvent("past", 2, () => { });

        var result = scheduler.ProcessDay(5); // day=5, event triggerDay=2 → due
        Assert.Single(result);
        Assert.Equal("past", result[0]);
    }

    // ─── AC3: 同优先级按注册顺序 FIFO ──────────────────────

    [Fact]
    public void ProcessDay_SamePriority_FIFO()
    {
        var scheduler = new DelayedEventScheduler();
        var order = new List<string>();

        scheduler.RegisterDelayedEvent("first", 1, () => order.Add("first"), priority: 0);
        scheduler.RegisterDelayedEvent("second", 1, () => order.Add("second"), priority: 0);
        scheduler.RegisterDelayedEvent("third", 1, () => order.Add("third"), priority: 0);

        scheduler.ProcessDay(1);

        Assert.Equal(new[] { "first", "second", "third" }, order);
    }

    // ─── AC4: 防递归 ────────────────────────────────────────

    [Fact]
    public void ProcessDay_RecursiveCall_Throws()
    {
        var scheduler = new DelayedEventScheduler();
        scheduler.RegisterDelayedEvent("recursive", 1, () =>
        {
            // 尝试在回调内再次 ProcessDay
            Assert.Throws<InvalidOperationException>(() => scheduler.ProcessDay(1));
        });

        scheduler.ProcessDay(1); // 不抛出，但内部的递归调用抛出
    }

    [Fact]
    public void IsProcessing_TrueDuringCallback()
    {
        var scheduler = new DelayedEventScheduler();
        bool wasProcessing = false;

        scheduler.RegisterDelayedEvent("check", 1, () =>
        {
            wasProcessing = scheduler.IsProcessing;
        });

        scheduler.ProcessDay(1);
        Assert.True(wasProcessing);
        Assert.False(scheduler.IsProcessing); // 完成后重置
    }

    // ─── AC5: 取消事件 ──────────────────────────────────────

    [Fact]
    public void CancelEvent_RemovesEvent()
    {
        var scheduler = new DelayedEventScheduler();
        scheduler.RegisterDelayedEvent("evt1", 5, () => { });
        Assert.True(scheduler.CancelEvent("evt1"));
        Assert.Equal(0, scheduler.Count);
    }

    [Fact]
    public void CancelEvent_NonExistent_ReturnsFalse()
    {
        var scheduler = new DelayedEventScheduler();
        Assert.False(scheduler.CancelEvent("nonexistent"));
    }

    [Fact]
    public void CancelEvent_DuringProcessing_RegisterNewEvent()
    {
        var scheduler = new DelayedEventScheduler();
        var executed = new List<string>();

        scheduler.RegisterDelayedEvent("a", 1, () =>
        {
            executed.Add("a");
            // 在回调中注册新事件（允许，不是递归 ProcessDay）
            scheduler.RegisterDelayedEvent("late", 2, () => executed.Add("late"));
        });

        scheduler.ProcessDay(1);
        Assert.Single(executed); // 只有 "a"
        Assert.True(scheduler.HasEvent("late")); // 新事件已注册
    }

    // ─── AC6: 逐日触发（驿站传送场景）─────────────────────

    [Fact]
    public void ProcessMultipleDays_ExecutesEachDaySequentially()
    {
        var scheduler = new DelayedEventScheduler();
        var executed = new List<string>();

        scheduler.RegisterDelayedEvent("day2", 2, () => executed.Add("day2"));
        scheduler.RegisterDelayedEvent("day3", 3, () => executed.Add("day3"));
        scheduler.RegisterDelayedEvent("day5", 5, () => executed.Add("day5"));

        // 模拟驿站传送从 day1 到 day4（逐日调用）
        for (int day = 2; day <= 4; day++)
        {
            scheduler.ProcessDay(day);
        }

        Assert.Equal(2, executed.Count);
        Assert.Contains("day2", executed);
        Assert.Contains("day3", executed);
        Assert.False(scheduler.HasEvent("day2"));
        Assert.False(scheduler.HasEvent("day3"));
        Assert.True(scheduler.HasEvent("day5")); // 未到期
    }
}
