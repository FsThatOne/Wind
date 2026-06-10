using Xunit;
using FengZhi.Foundation.NpcState;

namespace FengZhi.Tests.Foundation.NpcStates;

public class LetterQueueTests
{
    private static Letter MakeLetter(string id, string sender, LetterPriority priority, bool posthumous = false)
        => new() { Id = id, SenderId = sender, Priority = priority, Content = $"Letter {id}", PosthumousAllowed = posthumous };

    // ─── AC1: Enqueue 正确入队，按优先级排序 ────────────────

    [Fact]
    public void Enqueue_SortsbyPriorityDescending()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "a", LetterPriority.Low));
        queue.Enqueue(MakeLetter("2", "b", LetterPriority.Urgent));
        queue.Enqueue(MakeLetter("3", "c", LetterPriority.Normal));

        Assert.Equal("2", queue.Queue[0].Id); // Urgent
        Assert.Equal("3", queue.Queue[1].Id); // Normal
        Assert.Equal("1", queue.Queue[2].Id); // Low
    }

    // ─── AC2: IsDeliveryReady 正确实现 F6 ──────────────────

    [Theory]
    [InlineData(true, 3, 3, true)]    // freeRoam + steps >= delay
    [InlineData(true, 5, 3, true)]    // freeRoam + steps > delay
    [InlineData(true, 2, 3, false)]   // freeRoam + steps < delay
    [InlineData(false, 5, 3, false)]  // 非 freeRoam
    [InlineData(false, 0, 0, false)]  // 非 freeRoam，即使 delay=0
    public void IsDeliveryReady_CorrectLogic(bool freeRoam, int steps, int delay, bool expected)
    {
        Assert.Equal(expected, LetterQueue.IsDeliveryReady(freeRoam, steps, delay));
    }

    // ─── AC3: 战斗中入队不触发（通过 freeRoam=false 验证） ──

    [Fact]
    public void IsDeliveryReady_InCombat_ReturnsFalse()
    {
        Assert.False(LetterQueue.IsDeliveryReady(freeRoam: false, stepsAfterUnblock: 10, stepDelay: 0));
    }

    // ─── AC4: 送达时取出最高优先级 ─────────────────────────

    [Fact]
    public void Dequeue_ReturnsHighestPriority()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "a", LetterPriority.Low));
        queue.Enqueue(MakeLetter("2", "b", LetterPriority.High));
        queue.Enqueue(MakeLetter("3", "c", LetterPriority.Normal));

        var delivered = queue.Dequeue();
        Assert.Equal("2", delivered!.Id);
        Assert.Equal(2, queue.Count); // 其余保留
    }

    [Fact]
    public void Dequeue_EmptyQueue_ReturnsNull()
    {
        var queue = new LetterQueue();
        Assert.Null(queue.Dequeue());
    }

    // ─── AC5: NPC 死亡时取消非 posthumous_allowed 的飞书 ────

    [Fact]
    public void CancelBySenderDeath_RemovesNonPosthumous()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "npc_a", LetterPriority.Normal, posthumous: false));
        queue.Enqueue(MakeLetter("2", "npc_a", LetterPriority.High, posthumous: true));
        queue.Enqueue(MakeLetter("3", "npc_b", LetterPriority.Normal));

        int removed = queue.CancelBySenderDeath("npc_a");

        Assert.Equal(1, removed);
        Assert.Equal(2, queue.Count);
        Assert.Contains(queue.Queue, l => l.Id == "2"); // posthumous 保留
        Assert.Contains(queue.Queue, l => l.Id == "3"); // 不同 sender 保留
    }

    [Fact]
    public void CancelBySenderDeath_NoMatch_RemovesNothing()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "npc_a", LetterPriority.Normal));

        int removed = queue.CancelBySenderDeath("npc_x");
        Assert.Equal(0, removed);
        Assert.Equal(1, queue.Count);
    }

    // ─── AC6: 队列上限 5 封，超出时最低优先级被挤出 ─────────

    [Fact]
    public void Enqueue_ExceedsCap_EvictsLowestPriority()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "a", LetterPriority.Normal));
        queue.Enqueue(MakeLetter("2", "a", LetterPriority.High));
        queue.Enqueue(MakeLetter("3", "a", LetterPriority.Normal));
        queue.Enqueue(MakeLetter("4", "a", LetterPriority.Urgent));
        queue.Enqueue(MakeLetter("5", "a", LetterPriority.High));

        Assert.Equal(5, queue.Count);

        // 第 6 封入队，Low 优先级
        var evicted = queue.Enqueue(MakeLetter("6", "a", LetterPriority.Low));

        Assert.Equal(5, queue.Count);
        Assert.NotNull(evicted);
        Assert.Equal("6", evicted!.Id); // Low 被挤出
    }

    [Fact]
    public void Enqueue_ExceedsCap_EvictsExistingLow()
    {
        var queue = new LetterQueue();
        queue.Enqueue(MakeLetter("1", "a", LetterPriority.Low));
        queue.Enqueue(MakeLetter("2", "a", LetterPriority.Normal));
        queue.Enqueue(MakeLetter("3", "a", LetterPriority.Normal));
        queue.Enqueue(MakeLetter("4", "a", LetterPriority.High));
        queue.Enqueue(MakeLetter("5", "a", LetterPriority.High));

        // 入队 Urgent，挤出 Low
        var evicted = queue.Enqueue(MakeLetter("6", "a", LetterPriority.Urgent));

        Assert.Equal(5, queue.Count);
        Assert.NotNull(evicted);
        Assert.Equal("1", evicted!.Id); // 原来的 Low 被挤出
        Assert.DoesNotContain(queue.Queue, l => l.Id == "1");
    }
}
