using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Xunit;

namespace FengZhi.Tests.Foundation.Mindset;

public class MindsetDialogueBridgeTest
{
    [Fact]
    public void DialogueMindsetShift_IsCollectedAndAppliedOnFlush()
    {
        var eventBus = new EventBus();
        var service = new MindsetService(eventBus: eventBus);
        var bridge = new MindsetDialogueBridge(eventBus, service);

        eventBus.Publish(new DialogueMindsetShiftEvent("resolve", 8));

        Assert.Equal(1, bridge.PendingCount);
        Assert.Equal(-5, service.State.Resolve);

        var applied = bridge.ApplyPending();

        Assert.Equal(1, applied);
        Assert.Equal(3, service.State.Resolve);
        Assert.Equal(0, bridge.PendingCount);
    }

    [Fact]
    public void ApplyPending_BatchesMultipleDialogueShiftsBeforeZoneCheck()
    {
        var eventBus = new EventBus();
        var zoneEvents = new List<MindsetZoneChangedEvent>();
        eventBus.Subscribe<MindsetZoneChangedEvent>(zoneEvents.Add);
        var service = new MindsetService(MindsetState.FromValues(14, 0, 0, 0), eventBus);
        var bridge = new MindsetDialogueBridge(eventBus, service);

        eventBus.Publish(new DialogueMindsetShiftEvent("resolve", 2));
        eventBus.Publish(new DialogueMindsetShiftEvent("resolve", -2));
        bridge.ApplyPending();

        Assert.Equal(14, service.State.Resolve);
        Assert.Equal(AxisPolarity.Neutral, service.State.CurrentResolveZone);
        Assert.Empty(zoneEvents);
    }

    [Fact]
    public void DialogueEventQueue_CanDriveMindsetBridge()
    {
        var eventBus = new EventBus();
        var service = new MindsetService(eventBus: eventBus);
        var bridge = new MindsetDialogueBridge(eventBus, service);
        var queue = new DialogueEventQueue(eventBus);

        queue.Enqueue(new DialogueEventSpec
        {
            Type = "mindset_shift",
            Axis = "morality",
            Delta = 3
        });

        var dispatched = queue.DispatchPending();
        var applied = bridge.ApplyPending();

        Assert.Equal(1, dispatched);
        Assert.Equal(1, applied);
        Assert.Equal(3, service.State.Morality);
    }

    [Fact]
    public void UnknownAxis_IsReportedAndNotApplied()
    {
        var eventBus = new EventBus();
        var service = new MindsetService(eventBus: eventBus);
        var bridge = new MindsetDialogueBridge(eventBus, service);

        eventBus.Publish(new DialogueMindsetShiftEvent("kindness", 2));

        Assert.Equal(0, bridge.PendingCount);
        Assert.Single(bridge.Errors);
        Assert.Equal(0, service.State.Morality);
    }

    [Fact]
    public void Dispose_UnsubscribesFromDialogueEvents()
    {
        var eventBus = new EventBus();
        var service = new MindsetService(eventBus: eventBus);
        var bridge = new MindsetDialogueBridge(eventBus, service);

        bridge.Dispose();
        eventBus.Publish(new DialogueMindsetShiftEvent("resolve", 8));

        Assert.Equal(0, bridge.PendingCount);
    }
}
