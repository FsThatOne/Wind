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

/// <summary>
/// 协同声明事件。由战斗结算/服务层在判定多人对同一目标使用克制体系招式时发布；
/// Combat UI 仅订阅该事件，不自行重新判定协同收益。
/// </summary>
public readonly record struct SynergyDeclaredEvent(
    IReadOnlyList<string> SourceActorIds,
    string TargetId,
    int RoundNumber);

/// <summary>
/// 一击决胜演出开始事件。由 Combat UI 的 CombatAnimationDirector 发布，
/// 让音效（ADR-0009）与体系动画层挂载，不暴露 director 句柄。
/// </summary>
public readonly record struct DecisiveStrikeStartedEvent(
    string SourceId,
    string TargetId,
    int PrecomputedDamage,
    MoveType MoveType);

/// <summary>
/// 一击决胜演出阶段推进事件。每进入一个阶段发布一次；
/// 浮字层订阅 Phase5 触发深金最大尺寸样式。
/// </summary>
public readonly record struct DecisiveStrikePhaseAdvancedEvent(
    string SourceId,
    string TargetId,
    int PhaseIndex,
    string PhaseName,
    MoveType MoveType);

/// <summary>
/// 一击决胜演出完成事件。Phase 7 结束、CinematicLock 释放后发布；
/// 战斗系统据此恢复正常回合推进。
/// </summary>
public readonly record struct DecisiveStrikeCompletedEvent(
    string SourceId,
    string TargetId,
    bool WasCancelled);

public readonly record struct BattleEndEvent(BattleResult Result);

public readonly record struct RoundEndEvent(int RoundNumber);
