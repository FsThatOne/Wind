using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.SceneManagement;

/// <summary>地点访问状态</summary>
public enum LocationState
{
    /// <summary>玩家不知道存在，大地图不显示</summary>
    Locked,
    /// <summary>已知但不可前往，大地图灰色显示</summary>
    Known,
    /// <summary>已解锁可前往</summary>
    Unlocked
}

// ─── 事件定义 ──────────────────────────────────────────────

public sealed record LocationRevealedEvent(string LocationId) : GameEvent;
public sealed record LocationUnlockedEvent(string LocationId) : GameEvent;

/// <summary>
/// 地点访问控制管理器。
/// GDD: §Core Rules 4 — locked→known→unlocked 状态机。
/// </summary>
public sealed class LocationAccessManager
{
    private readonly Dictionary<string, LocationState> _states = new();
    private readonly IEventBus _eventBus;

    public LocationAccessManager(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>注册地点（初始为 Locked）</summary>
    public void RegisterLocation(string locationId, LocationState initialState = LocationState.Locked)
    {
        _states[locationId] = initialState;
    }

    /// <summary>AC6: 查询地点访问状态</summary>
    public LocationState GetAccessState(string locationId)
    {
        return _states.TryGetValue(locationId, out var state) ? state : LocationState.Locked;
    }

    /// <summary>
    /// AC2: 揭示地点 (Locked→Known)。
    /// 如果已经是 Known/Unlocked 则忽略。
    /// </summary>
    public bool RevealLocation(string locationId)
    {
        EnsureRegistered(locationId);
        var current = _states[locationId];

        if (current >= LocationState.Known) return false; // AC5: 不逆向

        _states[locationId] = LocationState.Known;
        _eventBus.Publish(new LocationRevealedEvent(locationId));
        return true;
    }

    /// <summary>
    /// AC3: 解锁地点 (Known→Unlocked)。
    /// AC4: 也支持直接从 Locked→Unlocked。
    /// </summary>
    public bool UnlockLocation(string locationId)
    {
        EnsureRegistered(locationId);
        var current = _states[locationId];

        if (current == LocationState.Unlocked) return false; // AC5: 已是最高状态

        _states[locationId] = LocationState.Unlocked;
        _eventBus.Publish(new LocationUnlockedEvent(locationId));
        return true;
    }

    /// <summary>获取所有已解锁的地点</summary>
    public IReadOnlyList<string> GetUnlockedLocations()
    {
        return _states.Where(kv => kv.Value == LocationState.Unlocked)
            .Select(kv => kv.Key).ToList();
    }

    /// <summary>获取所有已知（含已解锁）的地点</summary>
    public IReadOnlyList<string> GetKnownLocations()
    {
        return _states.Where(kv => kv.Value >= LocationState.Known)
            .Select(kv => kv.Key).ToList();
    }

    private void EnsureRegistered(string locationId)
    {
        if (!_states.ContainsKey(locationId))
            _states[locationId] = LocationState.Locked;
    }
}
