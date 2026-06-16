using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 战斗事件总线。类型安全的订阅/发布机制。
/// GDD §Event Bus: 战斗系统向所有订阅方广播状态变更。
/// </summary>
public sealed class BattleEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    /// <summary>
    /// 订阅事件。按注册顺序接收。
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _subscribers[type] = list;
        }
        list.Add(handler);
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        var type = typeof(TEvent);
        if (_subscribers.TryGetValue(type, out var list))
            list.Remove(handler);
    }

    /// <summary>
    /// 发布事件。按订阅顺序调用所有处理器。
    /// </summary>
    public void Publish<TEvent>(TEvent evt)
    {
        var type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out var list)) return;
        foreach (var handler in list.ToList()) // ToList 防迭代修改
            ((Action<TEvent>)handler)(evt);
    }

    /// <summary>
    /// 清除所有订阅。战斗结束时调用。
    /// </summary>
    public void ClearAll()
    {
        _subscribers.Clear();
    }
}

// --- 事件 DTO ---

public readonly record struct RoundStartEvent(int RoundNumber);

public readonly record struct IntentRevealedEvent(IReadOnlyList<EnemyIntent> Enemies);

public readonly record struct EnemyIntent(
    string EnemyId,
    IntentVisibility Visibility,
    string? MoveTypeName,
    MoveType? MoveType = null);

/// <summary>
/// 伤害事件的战斗语义提示。Presentation 层可据此选择样式，但不得反推伤害数值。
/// </summary>
public enum DamageVisualRelation
{
    Neutral,
    Advantage,
    Disadvantage,
    Decisive
}

public readonly record struct DamageDealtEvent(
    string SourceId,
    string TargetId,
    int Amount,
    bool IsCrit,
    bool IsCounter,
    DamageVisualRelation VisualRelation = DamageVisualRelation.Neutral);

public readonly record struct StaggerChangedEvent(string TargetId, int NewStagger);

public readonly record struct NeixiChangedEvent(string ActorId, int NewValue);

public readonly record struct DecisiveStrikeAvailableEvent(string TargetId);

public readonly record struct BattleEndEvent(BattleResult Result);

public readonly record struct RoundEndEvent(int RoundNumber);
