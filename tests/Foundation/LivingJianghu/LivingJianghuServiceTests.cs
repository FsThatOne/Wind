using FengZhi.Foundation.LivingJianghu;
using Xunit;

namespace Foundation.Tests.LivingJianghu;

public class LivingJianghuServiceTests
{
    private readonly TestContext _ctx = new();
    private readonly TestNpcHandler _npcHandler = new();
    private readonly TestPresenter _presenter = new();
    private LivingJianghuService _service;

    public LivingJianghuServiceTests()
    {
        _service = new LivingJianghuService(_ctx, _npcHandler, _presenter);
    }

    [Fact]
    public void OnDayAdvanced_NoEvents_NothingHappens()
    {
        _service.OnDayAdvanced();
        Assert.Empty(_service.GetPendingDeliveryQueue());
    }

    [Fact]
    public void OnDayAdvanced_ConditionsMet_EventTriggered()
    {
        _ctx.Flags.Add("quest_done");
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "rumor_1",
            Type = WorldEventType.Rumor,
            Priority = 50,
            Preconditions = [new Precondition { Type = PreconditionType.Flag, StringValue = "quest_done" }]
        });

        _service.OnDayAdvanced();

        var instance = _service.GetInstance("rumor_1");
        Assert.NotNull(instance);
        Assert.Equal(WorldEventState.Triggered, instance.State);
        Assert.Single(_service.GetPendingDeliveryQueue());
    }

    [Fact]
    public void OnDayAdvanced_ConditionsNotMet_EventStaysInactive()
    {
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "rumor_2",
            Type = WorldEventType.Rumor,
            Preconditions = [new Precondition { Type = PreconditionType.Flag, StringValue = "missing" }]
        });

        _service.OnDayAdvanced();

        var instance = _service.GetInstance("rumor_2");
        Assert.Equal(WorldEventState.Inactive, instance!.State);
    }

    [Fact]
    public void RumorPropagationDelay_SameRegion()
    {
        int delay = _service.ComputeRumorDelay("jiangnan");
        Assert.Equal(1, delay); // base_delay=1, distance=0
    }

    [Fact]
    public void RumorPropagationDelay_DifferentRegion()
    {
        _ctx.RegionDistances[("north", "jiangnan")] = 3;
        int delay = _service.ComputeRumorDelay("north");
        Assert.Equal(4, delay); // 1 + 3*1.0
    }

    [Fact]
    public void RumorDelay_EventNotSelectedUntilReady()
    {
        _ctx.Day = 1;
        _ctx.Flags.Add("trigger");
        _ctx.RegionDistances[("north", "jiangnan")] = 2;

        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "delayed_rumor",
            Type = WorldEventType.Rumor,
            SourceRegion = "north",
            Priority = 90,
            Preconditions = [new Precondition { Type = PreconditionType.Flag, StringValue = "trigger" }]
        });

        _service.OnDayAdvanced(); // day 1: goes pending, propagation_ready = 1 + 3 = 4
        Assert.Empty(_service.GetPendingDeliveryQueue());

        _ctx.Day = 2;
        _service.OnDayAdvanced();
        Assert.Empty(_service.GetPendingDeliveryQueue());

        _ctx.Day = 4;
        _service.OnDayAdvanced();
        Assert.Single(_service.GetPendingDeliveryQueue());
    }

    [Fact]
    public void OnDayAdvanced_RespectsCapacity()
    {
        for (int i = 1; i <= 5; i++)
        {
            _service.RegisterEvent(new WorldEventConfig
            {
                Id = $"evt_{i}",
                Type = WorldEventType.Rumor,
                Priority = 50 + i
            });
        }

        _service.DailyEventCap = 3;
        _service.MaxTypePerDay = 5;
        _service.OnDayAdvanced();

        Assert.Equal(3, _service.GetPendingDeliveryQueue().Count);
    }

    [Fact]
    public void BreathingPeriod_DoublesCapacity()
    {
        _ctx.Breathing = true;
        for (int i = 1; i <= 8; i++)
        {
            _service.RegisterEvent(new WorldEventConfig
            {
                Id = $"evt_{i}",
                Type = (WorldEventType)((i % 5) + 1),
                Priority = 50
            });
        }

        _service.DailyEventCap = 3;
        _service.BreathingMultiplier = 2;
        _service.MaxTypePerDay = 5;
        _service.OnDayAdvanced();

        Assert.Equal(6, _service.GetPendingDeliveryQueue().Count);
    }

    [Fact]
    public void BreathingOnly_NotTriggeredOutsideBreathing()
    {
        _ctx.Breathing = false;
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "breath_evt",
            Type = WorldEventType.Rumor,
            BreathingOnly = true,
            Priority = 90
        });

        _service.OnDayAdvanced();
        Assert.Empty(_service.GetPendingDeliveryQueue());
    }

    [Fact]
    public void BreathingOnly_TriggeredDuringBreathing()
    {
        _ctx.Breathing = true;
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "breath_evt",
            Type = WorldEventType.Rumor,
            BreathingOnly = true,
            Priority = 90
        });

        _service.OnDayAdvanced();
        Assert.Single(_service.GetPendingDeliveryQueue());
    }

    [Fact]
    public void ChapterCleanup_ExpiresOutOfRangeEvents()
    {
        _ctx.CurrentChapter = 1;
        _ctx.Flags.Add("ok");
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "ch1_evt",
            Type = WorldEventType.Rumor,
            ChapterMin = 1,
            ChapterMax = 1,
            Priority = 50,
            Preconditions = [new Precondition { Type = PreconditionType.Flag, StringValue = "ok" }]
        });

        _service.OnDayAdvanced(); // becomes pending
        // Don't trigger it yet, just verify it's pending
        // Actually since it passes conditions AND is within cap, it gets triggered immediately.
        // Let's test with a lower cap scenario:

        // Recreate with a scenario where event stays pending
        _service = new LivingJianghuService(_ctx, _npcHandler, _presenter);
        _ctx.RegionDistances[("far_away", "jiangnan")] = 3;
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "ch1_delayed",
            Type = WorldEventType.Rumor,
            ChapterMin = 1,
            ChapterMax = 1,
            SourceRegion = "far_away",
            Priority = 50,
            Preconditions = [new Precondition { Type = PreconditionType.Flag, StringValue = "ok" }]
        });

        _service.OnDayAdvanced(); // becomes pending with delay
        var inst = _service.GetInstance("ch1_delayed");
        Assert.Equal(WorldEventState.Pending, inst!.State);

        _ctx.CurrentChapter = 2;
        _service.OnChapterChanged();

        Assert.Equal(WorldEventState.Expired, inst.State);
    }

    [Fact]
    public void ExpireDay_EventExpiresWhenDayReached()
    {
        _ctx.Day = 5;
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "expiring",
            Type = WorldEventType.Rumor,
            ExpireDay = 5,
            Priority = 50,
            OnExpire = [new TriggerAction { ActionType = TriggerActionType.SetFlag, FlagToSet = "expired_flag" }]
        });

        _service.OnDayAdvanced();
        // Event goes inactive→pending (no preconditions), then expire check happens
        Assert.Contains("expired_flag", _ctx.Flags);
    }

    [Fact]
    public void OnTrigger_SetsFlagAndNpcChange()
    {
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "action_evt",
            Type = WorldEventType.WorldEvent,
            Priority = 50,
            OnTrigger =
            [
                new TriggerAction { ActionType = TriggerActionType.SetFlag, FlagToSet = "triggered_flag" },
                new TriggerAction
                {
                    ActionType = TriggerActionType.NpcStateChange,
                    NpcChange = new NpcStateChange { NpcId = "npc_a", Field = "attitude", Value = "-1" }
                }
            ]
        });

        _service.OnDayAdvanced();

        Assert.Contains("triggered_flag", _ctx.Flags);
        Assert.Single(_npcHandler.Changes);
        Assert.Equal("npc_a", _npcHandler.Changes[0].NpcId);
    }

    [Fact]
    public void DeliverPendingEvents_TransitionsToDelivered()
    {
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "deliver_me",
            Type = WorldEventType.Letter,
            Priority = 50
        });

        _service.OnDayAdvanced();
        _service.DeliverPendingEvents();

        var inst = _service.GetInstance("deliver_me");
        Assert.Equal(WorldEventState.Delivered, inst!.State);
        Assert.Single(_presenter.DeliveredEvents);
    }

    [Fact]
    public void SaveAndLoad_PreservesState()
    {
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "save_evt",
            Type = WorldEventType.Rumor,
            Priority = 50
        });

        _service.OnDayAdvanced();
        var saveData = _service.ExportSaveData();

        // New service, re-register config, load save
        var newService = new LivingJianghuService(_ctx, _npcHandler, _presenter);
        newService.RegisterEvent(new WorldEventConfig
        {
            Id = "save_evt",
            Type = WorldEventType.Rumor,
            Priority = 50
        });
        newService.LoadSaveData(saveData);

        var inst = newService.GetInstance("save_evt");
        Assert.Equal(WorldEventState.Triggered, inst!.State);
        Assert.Single(newService.GetPendingDeliveryQueue());
    }

    [Fact]
    public void TypeBalancing_SkipsExcessSameType()
    {
        for (int i = 1; i <= 4; i++)
        {
            _service.RegisterEvent(new WorldEventConfig
            {
                Id = $"rumor_{i}",
                Type = WorldEventType.Rumor,
                Priority = 90 - i
            });
        }
        _service.RegisterEvent(new WorldEventConfig
        {
            Id = "world_1",
            Type = WorldEventType.WorldEvent,
            Priority = 50
        });

        _service.MaxTypePerDay = 2;
        _service.DailyEventCap = 3;
        _service.OnDayAdvanced();

        var queue = _service.GetPendingDeliveryQueue();
        Assert.Equal(3, queue.Count);
        Assert.Contains("world_1", queue);
    }

    [Fact]
    public void BacklogIncrementsForUnselectedCandidates()
    {
        _service.DailyEventCap = 1;
        _service.MaxTypePerDay = 5;

        _service.RegisterEvent(new WorldEventConfig { Id = "high", Type = WorldEventType.Rumor, Priority = 90 });
        _service.RegisterEvent(new WorldEventConfig { Id = "low", Type = WorldEventType.Letter, Priority = 10 });

        _service.OnDayAdvanced();

        var lowInst = _service.GetInstance("low");
        Assert.True(lowInst!.BacklogDays > 0);
    }
}

internal class TestContext : ILivingJianghuContext
{
    public HashSet<string> Flags { get; } = [];
    public Dictionary<string, int> FlagDays { get; } = new();
    public int CurrentChapter { get; set; } = 1;
    public bool Breathing { get; set; }
    public int Day { get; set; } = 1;
    public Season CurrentSeason { get; set; } = Season.Spring;
    public string Region { get; set; } = "jiangnan";
    public string Mindset { get; set; } = "neutral";
    public Dictionary<(string, string), string> NpcStates { get; } = new();
    public Dictionary<(string, string), int> RegionDistances { get; } = new();

    public bool HasFlag(string flag) => Flags.Contains(flag);
    public void SetFlag(string flag) => Flags.Add(flag);
    public int GetDaysSinceFlag(string flag) => FlagDays.TryGetValue(flag, out int d) ? d : 0;
    public int GetCurrentChapter() => CurrentChapter;
    public bool IsBreathing() => Breathing;
    public int GetCurrentDay() => Day;
    public Season GetCurrentSeason() => CurrentSeason;
    public string GetPlayerRegion() => Region;
    public int GetRegionDistance(string from, string to)
    {
        if (from == to) return 0;
        return RegionDistances.TryGetValue((from, to), out int dist) ? dist : 1;
    }
    public string? GetNpcAxisValue(string npcId, string axis) =>
        NpcStates.TryGetValue((npcId, axis), out string? v) ? v : null;
    public string GetMindsetZone() => Mindset;
}

internal class TestNpcHandler : ILivingJianghuNpcHandler
{
    public List<NpcStateChange> Changes { get; } = [];
    public void ApplyNpcStateChange(NpcStateChange change) => Changes.Add(change);
}

internal class TestPresenter : ILivingJianghuPresenter
{
    public List<string> DeliveredEvents { get; } = [];
    public int PulseCount { get; set; }
    public void DeliverEvent(WorldEventConfig config, EventInstance instance) => DeliveredEvents.Add(config.Id);
    public void PlayWorldPulse() => PulseCount++;
}
