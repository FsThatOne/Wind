using System.Collections.Generic;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// SFX 优先级。P0/P1 不可被淘汰且可临时突破池上限。
/// </summary>
public enum SfxPriority
{
    P0 = 0,
    P1 = 1,
    P2 = 2,
    P3 = 3,
    P4 = 4
}

/// <summary>
/// PlaySfx 调用结果。
/// </summary>
public enum PlaySfxResult
{
    Played,
    CooldownBlocked,
    SameSourceLimitReached,
    Rejected
}

/// <summary>
/// 池内单个 slot 状态。
/// </summary>
public sealed class SfxSlot
{
    public string? SfxId { get; set; }
    public SfxPriority Priority { get; set; }
    public string? SourceId { get; set; }
    public float StartTimeMs { get; set; }
    public bool IsActive { get; set; }

    public void Assign(string sfxId, SfxPriority priority, string sourceId, float startTimeMs)
    {
        SfxId = sfxId;
        Priority = priority;
        SourceId = sourceId;
        StartTimeMs = startTimeMs;
        IsActive = true;
    }

    public void Clear()
    {
        SfxId = null;
        Priority = SfxPriority.P4;
        SourceId = null;
        StartTimeMs = 0f;
        IsActive = false;
    }
}

/// <summary>
/// TryPlay 返回值。
/// </summary>
public readonly record struct SfxPlayResponse(
    PlaySfxResult Result,
    int SlotIndex,
    int EvictedSlotIndex
);

/// <summary>
/// SFX 优先级仲裁 + 并发池纯逻辑引擎。
/// 不依赖 Godot 运行时，可由 xUnit 直接测试。
/// </summary>
public sealed class SfxPoolEngine
{
    public const int NormalPoolSize = 8;
    public const int MaxPoolSize = 12;
    public const float CooldownMs = 50f;
    public const int MaxPerSource = 2;

    private readonly SfxSlot[] _slots;
    private readonly Dictionary<string, float> _lastPlayTime = new();

    public SfxPoolEngine()
    {
        _slots = new SfxSlot[MaxPoolSize];
        for (int i = 0; i < MaxPoolSize; i++)
            _slots[i] = new SfxSlot();
    }

    public SfxSlot GetSlot(int index) => _slots[index];
    public int ActiveCount => CountActive();

    /// <summary>
    /// 尝试播放一个 SFX。返回结果和 slot 信息。
    /// </summary>
    public SfxPlayResponse TryPlay(string sfxId, SfxPriority priority, string sourceId, float currentTimeMs, bool bypassCooldown = false)
    {
        // 1. Cooldown 检查
        if (!bypassCooldown &&
            _lastPlayTime.TryGetValue(sfxId, out float lastTime) &&
            currentTimeMs - lastTime < CooldownMs)
            return new SfxPlayResponse(PlaySfxResult.CooldownBlocked, -1, -1);

        // 2. 同源检查（P0/P1 豁免）
        if (priority > SfxPriority.P1)
        {
            int sourceCount = 0;
            for (int i = 0; i < MaxPoolSize; i++)
                if (_slots[i].IsActive && _slots[i].SourceId == sourceId)
                    sourceCount++;
            if (sourceCount >= MaxPerSource)
                return new SfxPlayResponse(PlaySfxResult.SameSourceLimitReached, -1, -1);
        }

        // 3. 查找空闲 slot（优先普通池 0-7）
        int freeSlot = FindFreeSlot(NormalPoolSize);
        if (freeSlot >= 0)
        {
            _slots[freeSlot].Assign(sfxId, priority, sourceId, currentTimeMs);
            _lastPlayTime[sfxId] = currentTimeMs;
            return new SfxPlayResponse(PlaySfxResult.Played, freeSlot, -1);
        }

        // 4. 普通池满 — P0/P1 可使用溢出 slot (8-11)
        if (priority <= SfxPriority.P1)
        {
            int overflowSlot = FindFreeSlot(MaxPoolSize, NormalPoolSize);
            if (overflowSlot >= 0)
            {
                _slots[overflowSlot].Assign(sfxId, priority, sourceId, currentTimeMs);
                _lastPlayTime[sfxId] = currentTimeMs;
                return new SfxPlayResponse(PlaySfxResult.Played, overflowSlot, -1);
            }
        }

        // 5. 淘汰：找到优先级最低且比当前请求低的 slot
        int evictTarget = FindEvictionTarget(priority);
        if (evictTarget >= 0)
        {
            int evicted = evictTarget;
            _slots[evictTarget].Assign(sfxId, priority, sourceId, currentTimeMs);
            _lastPlayTime[sfxId] = currentTimeMs;
            return new SfxPlayResponse(PlaySfxResult.Played, evictTarget, evicted);
        }

        // 6. 无法播放
        return new SfxPlayResponse(PlaySfxResult.Rejected, -1, -1);
    }

    /// <summary>
    /// 标记某 slot 播放完毕（由 Presentation 层在检测到 !Playing 时调用）。
    /// </summary>
    public void MarkSlotFinished(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < MaxPoolSize)
            _slots[slotIndex].Clear();
    }

    /// <summary>
    /// 重置全部 slot 和 cooldown 记录。
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < MaxPoolSize; i++)
            _slots[i].Clear();
        _lastPlayTime.Clear();
    }

    /// <summary>
    /// 获取所有匹配指定 sourceId 的活跃 slot 索引。
    /// </summary>
    public List<int> GetActiveSlotsBySource(string sourceId)
    {
        var result = new List<int>();
        for (int i = 0; i < MaxPoolSize; i++)
            if (_slots[i].IsActive && _slots[i].SourceId == sourceId)
                result.Add(i);
        return result;
    }

    private int FindFreeSlot(int searchEnd, int searchStart = 0)
    {
        for (int i = searchStart; i < searchEnd; i++)
            if (!_slots[i].IsActive)
                return i;
        return -1;
    }

    private int FindEvictionTarget(SfxPriority requestPriority)
    {
        int worstIndex = -1;
        SfxPriority worstPriority = requestPriority;

        for (int i = 0; i < NormalPoolSize; i++)
        {
            if (!_slots[i].IsActive) continue;
            // P0/P1 不可被淘汰
            if (_slots[i].Priority <= SfxPriority.P1) continue;
            if (_slots[i].Priority > worstPriority)
            {
                worstPriority = _slots[i].Priority;
                worstIndex = i;
            }
        }

        return worstIndex;
    }

    private int CountActive()
    {
        int count = 0;
        for (int i = 0; i < MaxPoolSize; i++)
            if (_slots[i].IsActive) count++;
        return count;
    }
}
