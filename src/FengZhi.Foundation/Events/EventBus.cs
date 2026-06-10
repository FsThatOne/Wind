namespace FengZhi.Foundation.Events;

/// <summary>
/// 事件总线接口。发布-订阅模式，解耦跨层通信。
/// </summary>
public interface IEventBus
{
    /// <summary>发布事件，通知所有订阅者。</summary>
    void Publish<T>(T gameEvent) where T : GameEvent;

    /// <summary>订阅指定类型的事件。返回取消订阅的 Action。</summary>
    Action Subscribe<T>(Action<T> handler) where T : GameEvent;
}

/// <summary>
/// EventBus 实现。内存同步分发，无线程安全保证（Godot 单线程）。
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<T>(T gameEvent) where T : GameEvent
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var handlers)) return;

        foreach (var handler in handlers.ToList())
        {
            ((Action<T>)handler)(gameEvent);
        }
    }

    public Action Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();

        _handlers[type].Add(handler);

        return () => _handlers[type].Remove(handler);
    }
}
