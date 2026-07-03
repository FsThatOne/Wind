using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using Xunit;

namespace FengZhi.Tests.Foundation.Dialogue;

public sealed class DialogueEventQueueMisunderstandingTest
{
    [Fact]
    public void RegisterMisunderstandingSpec_PublishesTypedEvent()
    {
        var eventBus = new EventBus();
        var queue = new DialogueEventQueue(eventBus);
        DialogueRegisterMisunderstandingEvent? received = null;
        eventBus.Subscribe<DialogueRegisterMisunderstandingEvent>(e => received = e);

        queue.Enqueue(new DialogueEventSpec
        {
            Type = "register_misunderstanding",
            Key = "mis_senior_brother_survivor_suspicion",
            Value = "active",
        });

        Assert.Equal(1, queue.PendingCount);
        Assert.Equal(1, queue.DispatchPending());
        Assert.NotNull(received);
        Assert.Equal("mis_senior_brother_survivor_suspicion", received.Key);
        Assert.Equal("active", received.Value);
    }
}
