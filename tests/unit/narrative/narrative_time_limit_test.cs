using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeTimeLimitTest
{
    [Fact]
    public void TimeLimit_StartsWhenNodeActivatesAfterBreathingPeriod()
    {
        var bus = new EventBus();
        var started = new List<NarrativeTimeLimitStartedEvent>();
        bus.Subscribe<NarrativeTimeLimitStartedEvent>(started.Add);
        var service = new NarrativeProgressionService(CreateBreathingToTimeLimitGraph(), bus);
        service.Initialize(currentDay: 1);

        service.CompleteNode("key_node", currentDay: 2);
        var beforeExit = service.GetRemainingDays("timed_node", currentDay: 9);
        service.TriggerNextFromBreathing(currentDay: 10);

        Assert.Null(beforeExit);
        Assert.True(service.State.IsActive("timed_node"));
        Assert.True(service.State.HasActiveTimeLimit("timed_node"));
        Assert.Equal(5, service.GetRemainingDays("timed_node", currentDay: 10));
        Assert.Single(started);
        Assert.Equal(10, started[0].StartedDay);
    }

    [Fact]
    public void CheckTimeLimits_WhenExpiredPublishesForcedProgressionAndActivatesForcedNode()
    {
        var bus = new EventBus();
        var forced = new List<NarrativeForcedProgressionEvent>();
        bus.Subscribe<NarrativeForcedProgressionEvent>(forced.Add);
        var service = new NarrativeProgressionService(CreateTimedGraph(), bus);
        service.Initialize(currentDay: 3);

        var result = service.CheckTimeLimits(currentDay: 7);

        Assert.True(result);
        Assert.True(service.State.IsCompleted("timed_node"));
        Assert.True(service.State.IsActive("forced_node"));
        Assert.Single(forced);
        Assert.Equal("timed_node", forced[0].SourceNodeId);
        Assert.Equal("forced_node", forced[0].ForcedNextNodeId);
        Assert.Equal(7, forced[0].CurrentDay);
    }

    [Fact]
    public void CheckTimeLimits_DoesNotForceBeforeDeadline()
    {
        var service = new NarrativeProgressionService(CreateTimedGraph(), new EventBus());
        service.Initialize(currentDay: 3);

        var result = service.CheckTimeLimits(currentDay: 6);

        Assert.False(result);
        Assert.True(service.State.IsActive("timed_node"));
        Assert.False(service.State.IsActive("forced_node"));
    }

    [Fact]
    public void CheckTimeLimits_FiresOnlyOnce()
    {
        var bus = new EventBus();
        var forced = new List<NarrativeForcedProgressionEvent>();
        bus.Subscribe<NarrativeForcedProgressionEvent>(forced.Add);
        var service = new NarrativeProgressionService(CreateTimedGraph(), bus);
        service.Initialize(currentDay: 0);

        var first = service.CheckTimeLimits(currentDay: 4);
        var second = service.CheckTimeLimits(currentDay: 10);

        Assert.True(first);
        Assert.False(second);
        Assert.Single(forced);
    }

    [Fact]
    public void GetTimeLimitDialogueKey_ReturnsThreeUrgencyBands()
    {
        var service = new NarrativeProgressionService(CreateTimedGraph(), new EventBus());
        service.Initialize(currentDay: 0);

        var normal = service.GetTimeLimitDialogueKey("timed_node", currentDay: 1);
        var urgent = service.GetTimeLimitDialogueKey("timed_node", currentDay: 2);
        var final = service.GetTimeLimitDialogueKey("timed_node", currentDay: 3);

        Assert.Equal("npc.deadline.normal", normal);
        Assert.Equal("npc.deadline.urgent", urgent);
        Assert.Equal("npc.deadline.final", final);
    }

    private static NarrativeGraph CreateTimedGraph()
    {
        return new NarrativeGraph
        {
            Id = "timed",
            EntryNode = "timed_node",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "timed_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    TimeLimit = new NarrativeTimeLimit
                    {
                        Days = 4,
                        ForcedNext = "forced_node",
                        NormalTextKey = "npc.deadline.normal",
                        UrgentTextKey = "npc.deadline.urgent",
                        FinalTextKey = "npc.deadline.final"
                    }
                },
                new NarrativeNode
                {
                    Id = "forced_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "timed_node" }
                    }
                }
            }
        };
    }

    private static NarrativeGraph CreateBreathingToTimeLimitGraph()
    {
        return new NarrativeGraph
        {
            Id = "breathing_to_timed",
            EntryNode = "key_node",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "key_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Next = "timed_node",
                    OnComplete = new[]
                    {
                        new NarrativeEventSpec { Type = "enter_breathing" }
                    }
                },
                new NarrativeNode
                {
                    Id = "timed_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "key_node" }
                    },
                    TimeLimit = new NarrativeTimeLimit { Days = 5 }
                }
            }
        };
    }
}
