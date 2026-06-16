namespace FengZhi.Foundation.Events;

/// <summary>
/// 游戏事件基类。所有跨层事件必须继承此类。
/// ADR-0001: 跨层通知走 EventBus，不直接引用上层。
/// </summary>
public abstract record GameEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
