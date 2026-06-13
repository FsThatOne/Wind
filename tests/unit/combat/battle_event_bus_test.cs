using FengZhi.Foundation.Combat;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class BattleEventBusTest
{
    // --- Subscribe + Publish ---

    [Fact]
    public void Publish_InvokesSubscriber()
    {
        var bus = new BattleEventBus();
        RoundStartEvent? received = null;
        bus.Subscribe<RoundStartEvent>(e => received = e);

        bus.Publish(new RoundStartEvent(1));

        Assert.NotNull(received);
        Assert.Equal(1, received.Value.RoundNumber);
    }

    [Fact]
    public void Publish_MultipleSubscribers_InOrder()
    {
        var bus = new BattleEventBus();
        var order = new List<int>();
        bus.Subscribe<RoundEndEvent>(_ => order.Add(1));
        bus.Subscribe<RoundEndEvent>(_ => order.Add(2));
        bus.Subscribe<RoundEndEvent>(_ => order.Add(3));

        bus.Publish(new RoundEndEvent(5));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    // --- Unsubscribe ---

    [Fact]
    public void Unsubscribe_StopsReceiving()
    {
        var bus = new BattleEventBus();
        int count = 0;
        void Handler(RoundStartEvent _) => count++;

        bus.Subscribe<RoundStartEvent>(Handler);
        bus.Publish(new RoundStartEvent(1));
        Assert.Equal(1, count);

        bus.Unsubscribe<RoundStartEvent>(Handler);
        bus.Publish(new RoundStartEvent(2));
        Assert.Equal(1, count); // not called again
    }

    // --- ClearAll ---

    [Fact]
    public void ClearAll_RemovesAllSubscriptions()
    {
        var bus = new BattleEventBus();
        int count = 0;
        bus.Subscribe<RoundStartEvent>(_ => count++);
        bus.Subscribe<DamageDealtEvent>(_ => count++);
        bus.Subscribe<BattleEndEvent>(_ => count++);

        bus.ClearAll();

        bus.Publish(new RoundStartEvent(1));
        bus.Publish(new DamageDealtEvent("a", "b", 10, false, false));
        bus.Publish(new BattleEndEvent(BattleResult.Victory));

        Assert.Equal(0, count);
    }

    // --- 不同事件类型独立 ---

    [Fact]
    public void DifferentEventTypes_AreIndependent()
    {
        var bus = new BattleEventBus();
        int roundStartCount = 0;
        int damageCount = 0;

        bus.Subscribe<RoundStartEvent>(_ => roundStartCount++);
        bus.Subscribe<DamageDealtEvent>(_ => damageCount++);

        bus.Publish(new RoundStartEvent(1));
        bus.Publish(new RoundStartEvent(2));
        bus.Publish(new DamageDealtEvent("a", "b", 5, false, false));

        Assert.Equal(2, roundStartCount);
        Assert.Equal(1, damageCount);
    }

    // --- 所有事件 DTO 可构造 ---

    [Fact]
    public void AllEventDTOs_CanBeConstructed()
    {
        _ = new RoundStartEvent(1);
        _ = new IntentRevealedEvent(new List<EnemyIntent>());
        _ = new EnemyIntent("e1", IntentVisibility.Normal, "Gang");
        _ = new DamageDealtEvent("src", "tgt", 10, true, false);
        _ = new StaggerChangedEvent("tgt", 3);
        _ = new NeixiChangedEvent("actor", 25);
        _ = new DecisiveStrikeAvailableEvent("tgt");
        _ = new BattleEndEvent(BattleResult.Victory);
        _ = new RoundEndEvent(5);
    }

    // --- Publish 无订阅者不抛异常 ---

    [Fact]
    public void Publish_NoSubscribers_NoException()
    {
        var bus = new BattleEventBus();
        var ex = Record.Exception(() => bus.Publish(new RoundStartEvent(1)));
        Assert.Null(ex);
    }
}
