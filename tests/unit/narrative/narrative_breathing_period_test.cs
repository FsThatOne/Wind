using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeBreathingPeriodTest
{
    [Fact]
    public void CompleteNode_EntersBreathingAndDoesNotAutoActivateNextNode()
    {
        var bus = new EventBus();
        var entered = new List<NarrativeBreathingEnteredEvent>();
        bus.Subscribe<NarrativeBreathingEnteredEvent>(entered.Add);
        var service = new NarrativeProgressionService(CreateBreathingGraph(), bus);
        service.Initialize();

        var result = service.CompleteNode("key_node", currentDay: 10);

        Assert.True(result);
        Assert.True(service.State.IsInBreathingPeriod);
        Assert.False(service.State.IsActive("next_main"));
        Assert.Single(entered);
        Assert.Equal("key_node", entered[0].SourceNodeId);
        Assert.Equal("next_main", entered[0].NextNodeId);
        Assert.Equal(10, entered[0].StartedDay);
    }

    [Fact]
    public void TryActivateNode_DoesNotBypassActiveBreathingPeriod()
    {
        var service = new NarrativeProgressionService(CreateBreathingGraph(), new EventBus());
        service.Initialize();
        service.CompleteNode("key_node", currentDay: 1);

        var result = service.TryActivateNode("next_main");

        Assert.False(result);
        Assert.False(service.State.IsActive("next_main"));
    }

    [Fact]
    public void TriggerNextFromBreathing_ExitsBreathingAndActivatesNextNode()
    {
        var bus = new EventBus();
        var exited = new List<NarrativeBreathingExitedEvent>();
        bus.Subscribe<NarrativeBreathingExitedEvent>(exited.Add);
        var service = new NarrativeProgressionService(CreateBreathingGraph(), bus);
        service.Initialize();
        service.CompleteNode("key_node", currentDay: 1);

        var result = service.TriggerNextFromBreathing();

        Assert.True(result);
        Assert.False(service.State.IsInBreathingPeriod);
        Assert.True(service.State.IsActive("next_main"));
        Assert.Single(exited);
        Assert.Equal("key_node", exited[0].SourceNodeId);
    }

    [Fact]
    public void BreathingReminder_FiresOnlyAfterConfiguredDays()
    {
        var bus = new EventBus();
        var reminders = new List<NarrativeBreathingReminderEvent>();
        bus.Subscribe<NarrativeBreathingReminderEvent>(reminders.Add);
        var service = new NarrativeProgressionService(CreateBreathingGraph(), bus);
        service.Initialize();
        service.CompleteNode("key_node", currentDay: 5);

        var early = service.CheckBreathingReminder(currentDay: 11);
        var onTime = service.CheckBreathingReminder(currentDay: 12);

        Assert.False(early);
        Assert.True(onTime);
        Assert.Single(reminders);
        Assert.Equal(12, reminders[0].CurrentDay);
    }

    [Fact]
    public void BreathingReminder_FiresOnlyOnce()
    {
        var bus = new EventBus();
        var reminders = new List<NarrativeBreathingReminderEvent>();
        bus.Subscribe<NarrativeBreathingReminderEvent>(reminders.Add);
        var service = new NarrativeProgressionService(CreateBreathingGraph(), bus);
        service.Initialize();
        service.CompleteNode("key_node", currentDay: 1);

        var first = service.CheckBreathingReminder(currentDay: 8);
        var second = service.CheckBreathingReminder(currentDay: 20);

        Assert.True(first);
        Assert.False(second);
        Assert.Single(reminders);
    }

    [Fact]
    public void CompleteNode_WithoutBreathingStillAutoActivatesNextNode()
    {
        var graph = new NarrativeGraph
        {
            Id = "linear",
            EntryNode = "start",
            Nodes = new[]
            {
                new NarrativeNode { Id = "start", Chapter = 1, Type = NarrativeNodeType.Dialogue, Next = "next" },
                new NarrativeNode
                {
                    Id = "next",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "start" }
                    }
                }
            }
        };
        var service = new NarrativeProgressionService(graph, new EventBus());
        service.Initialize();

        service.CompleteNode("start");

        Assert.False(service.State.IsInBreathingPeriod);
        Assert.True(service.State.IsActive("next"));
    }

    private static NarrativeGraph CreateBreathingGraph()
    {
        return new NarrativeGraph
        {
            Id = "breathing",
            EntryNode = "key_node",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "key_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Next = "next_main",
                    OnComplete = new[]
                    {
                        new NarrativeEventSpec
                        {
                            Type = "enter_breathing",
                            Delta = 7
                        }
                    }
                },
                new NarrativeNode
                {
                    Id = "next_main",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "key_node" }
                    }
                }
            }
        };
    }
}
