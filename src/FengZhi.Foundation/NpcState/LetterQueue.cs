namespace FengZhi.Foundation.NpcState;

/// <summary>飞书优先级</summary>
public enum LetterPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

/// <summary>
/// 一封飞书（信件）。
/// </summary>
public sealed class Letter
{
    public required string Id { get; init; }
    public required string SenderId { get; init; }
    public required LetterPriority Priority { get; init; }
    public required string Content { get; init; }

    /// <summary>是否允许死后送达（遗书类）</summary>
    public bool PosthumousAllowed { get; init; } = false;
}

/// <summary>
/// F6 飞书排队送达判定 + 队列管理。
/// delivery_ready = free_roam AND (steps_after_unblock >= step_delay)
/// </summary>
public sealed class LetterQueue
{
    /// <summary>队列上限</summary>
    public const int MaxQueueSize = 5;

    private readonly List<Letter> _queue = new();

    /// <summary>当前队列内容（只读，按优先级降序）</summary>
    public IReadOnlyList<Letter> Queue => _queue;

    /// <summary>队列长度</summary>
    public int Count => _queue.Count;

    /// <summary>
    /// 入队一封飞书，按优先级降序排列。
    /// 超出上限时挤出最低优先级的飞书。
    /// </summary>
    /// <returns>被挤出的飞书（如有），否则 null</returns>
    public Letter? Enqueue(Letter letter)
    {
        _queue.Add(letter);
        _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        Letter? evicted = null;
        if (_queue.Count > MaxQueueSize)
        {
            evicted = _queue[^1];
            _queue.RemoveAt(_queue.Count - 1);
        }

        return evicted;
    }

    /// <summary>
    /// F6: 判断是否可以送达。
    /// delivery_ready = freeRoam AND (stepsAfterUnblock >= stepDelay)
    /// </summary>
    public static bool IsDeliveryReady(bool freeRoam, int stepsAfterUnblock, int stepDelay)
    {
        return freeRoam && stepsAfterUnblock >= stepDelay;
    }

    /// <summary>
    /// 取出最高优先级的一封飞书（送达）。
    /// </summary>
    /// <returns>取出的飞书，队列为空时返回 null</returns>
    public Letter? Dequeue()
    {
        if (_queue.Count == 0) return null;
        var letter = _queue[0];
        _queue.RemoveAt(0);
        return letter;
    }

    /// <summary>
    /// NPC 死亡时取消该 NPC 所有非 posthumous_allowed 的飞书。
    /// </summary>
    /// <param name="senderId">死亡 NPC 的 ID</param>
    /// <returns>被取消的数量</returns>
    public int CancelBySenderDeath(string senderId)
    {
        int removed = _queue.RemoveAll(l => l.SenderId == senderId && !l.PosthumousAllowed);
        return removed;
    }

    /// <summary>清空队列</summary>
    public void Clear() => _queue.Clear();
}
