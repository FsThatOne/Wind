namespace FengZhi.Foundation.TimeSystem;

/// <summary>延迟事件条目</summary>
public sealed class DelayedEvent
{
    public string Id { get; }
    public int TriggerDay { get; }
    public Action Callback { get; }
    public int Priority { get; }
    internal int RegistrationOrder { get; set; }

    public DelayedEvent(string id, int triggerDay, Action callback, int priority = 0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TriggerDay = triggerDay;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        Priority = priority;
    }
}

/// <summary>
/// 延迟事件调度器。
/// GDD: §Core Rules 5 — 每日推进时按优先级排序执行到期事件。
/// </summary>
public sealed class DelayedEventScheduler
{
    private readonly List<DelayedEvent> _events = new();
    private int _nextOrder = 0;
    private bool _isProcessing = false;

    /// <summary>当前注册事件数</summary>
    public int Count => _events.Count;

    /// <summary>是否正在处理事件（防递归标志）</summary>
    public bool IsProcessing => _isProcessing;

    /// <summary>
    /// 注册延迟事件。
    /// AC1: register_delayed_event(id, trigger_day, callback, priority)
    /// </summary>
    public void RegisterDelayedEvent(string id, int triggerDay, Action callback, int priority = 0)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Event id cannot be null or empty.", nameof(id));

        // 去重：同 id 覆盖
        _events.RemoveAll(e => e.Id == id);

        var evt = new DelayedEvent(id, triggerDay, callback, priority)
        {
            RegistrationOrder = _nextOrder++
        };
        _events.Add(evt);
    }

    /// <summary>
    /// 取消延迟事件。AC5。
    /// 返回是否找到并移除。
    /// </summary>
    public bool CancelEvent(string id)
    {
        return _events.RemoveAll(e => e.Id == id) > 0;
    }

    /// <summary>
    /// 处理指定日到期的事件。
    /// AC2: 按优先级升序执行。AC3: 同优先级按注册顺序 FIFO。
    /// AC4: 处理期间禁止再次调用 ProcessDay（防递归）。
    /// AC6: 驿站传送跨多日时外部逐日调用此方法。
    /// 返回本次执行的事件 id 列表。
    /// </summary>
    public List<string> ProcessDay(int currentDay)
    {
        if (_isProcessing)
            throw new InvalidOperationException("Cannot call ProcessDay recursively during event processing.");

        _isProcessing = true;
        var executed = new List<string>();

        try
        {
            // 收集到期事件
            var due = _events
                .Where(e => e.TriggerDay <= currentDay)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.RegistrationOrder)
                .ToList();

            // 先从列表移除，再执行（避免回调内取消干扰）
            foreach (var evt in due)
            {
                _events.Remove(evt);
            }

            foreach (var evt in due)
            {
                evt.Callback();
                executed.Add(evt.Id);
            }
        }
        finally
        {
            _isProcessing = false;
        }

        return executed;
    }

    /// <summary>查询是否存在指定 id 事件</summary>
    public bool HasEvent(string id) => _events.Any(e => e.Id == id);

    /// <summary>获取指定 id 事件的触发日（调试用）</summary>
    public int? GetTriggerDay(string id) => _events.FirstOrDefault(e => e.Id == id)?.TriggerDay;
}
