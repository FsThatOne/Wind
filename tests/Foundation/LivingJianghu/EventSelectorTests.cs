using FengZhi.Foundation.LivingJianghu;
using Xunit;

namespace Foundation.Tests.LivingJianghu;

public class EventSelectorTests
{
    [Fact]
    public void ComputeSelectionScore_CombinesPriorityTagAndBacklog()
    {
        var config = MakeConfig("evt1", priority: 60, tags: ["jiangnan", "ch1"]);
        var instance = new EventInstance("evt1") { BacklogDays = 3 };

        int score = EventSelector.ComputeSelectionScore(config, instance, "jiangnan", 1);

        // priority=60 + tag(region=2 + chapter=1)=3 + backlog(3*5=15)=15 → 78
        Assert.Equal(78, score);
    }

    [Fact]
    public void ComputeSelectionScore_BacklogCapsAt25()
    {
        var config = MakeConfig("evt1", priority: 50);
        var instance = new EventInstance("evt1") { BacklogDays = 10 };

        int score = EventSelector.ComputeSelectionScore(config, instance, "other", 1);
        // priority=50 + tag=0 + backlog=min(10*5,25)=25 → 75
        Assert.Equal(75, score);
    }

    [Fact]
    public void SelectDailyEvents_RespectsCapacity()
    {
        var candidates = Enumerable.Range(1, 5)
            .Select(i => (MakeConfig($"evt{i}", priority: 50), new EventInstance($"evt{i}")))
            .ToList();

        var selected = EventSelector.SelectDailyEvents(
            candidates, effectiveCap: 3, maxTypePerDay: 5, "region", 1);

        Assert.Equal(3, selected.Count);
    }

    [Fact]
    public void SelectDailyEvents_HigherPriorityFirst()
    {
        var low = (MakeConfig("low", priority: 20), new EventInstance("low"));
        var high = (MakeConfig("high", priority: 80), new EventInstance("high"));

        var selected = EventSelector.SelectDailyEvents(
            [low, high], effectiveCap: 1, maxTypePerDay: 5, "r", 1);

        Assert.Single(selected);
        Assert.Equal("high", selected[0].ConfigId);
    }

    [Fact]
    public void SelectDailyEvents_TypeBalancing_LimitsPerType()
    {
        var r1 = (MakeConfig("r1", priority: 90, type: WorldEventType.Rumor), new EventInstance("r1"));
        var r2 = (MakeConfig("r2", priority: 80, type: WorldEventType.Rumor), new EventInstance("r2"));
        var r3 = (MakeConfig("r3", priority: 70, type: WorldEventType.Rumor), new EventInstance("r3"));
        var w1 = (MakeConfig("w1", priority: 60, type: WorldEventType.WorldEvent), new EventInstance("w1"));

        var selected = EventSelector.SelectDailyEvents(
            [r1, r2, r3, w1], effectiveCap: 3, maxTypePerDay: 2, "r", 1);

        Assert.Equal(3, selected.Count);
        Assert.Contains(selected, s => s.ConfigId == "r1");
        Assert.Contains(selected, s => s.ConfigId == "r2");
        Assert.Contains(selected, s => s.ConfigId == "w1");
        Assert.DoesNotContain(selected, s => s.ConfigId == "r3");
    }

    [Fact]
    public void ComputeEffectiveCap_NormalPeriod()
    {
        Assert.Equal(3, EventSelector.ComputeEffectiveCap(3, 2, false));
    }

    [Fact]
    public void ComputeEffectiveCap_BreathingPeriod()
    {
        Assert.Equal(6, EventSelector.ComputeEffectiveCap(3, 2, true));
    }

    private static WorldEventConfig MakeConfig(
        string id,
        int priority = 50,
        WorldEventType type = WorldEventType.Rumor,
        IReadOnlyList<string>? tags = null)
    {
        return new WorldEventConfig
        {
            Id = id,
            Type = type,
            Priority = priority,
            Tags = tags ?? []
        };
    }
}
