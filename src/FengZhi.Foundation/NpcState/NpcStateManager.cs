using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.NpcState;

/// <summary>NPC 状态变更事件</summary>
public sealed record NpcStateChangedEvent(
    string NpcId,
    string Field,
    string OldValue,
    string NewValue,
    string Source
) : GameEvent;

/// <summary>NPC 状态管理接口</summary>
public interface INpcStateManager
{
    NpcState? GetState(string npcId);
    IReadOnlyList<NpcState> GetAll();
    bool UpdateLife(string npcId, LifeStatus value, string source);
    bool UpdatePresence(string npcId, PresenceStatus value, string source);
    bool UpdateLocation(string npcId, LocationStatus value, string source);
    bool UpdateInteraction(string npcId, InteractionStatus value, string source);
    bool UpdateJourney(string npcId, JourneyStage value, string source);
    bool UpdateRelationship(string npcId, RelationshipStage value, string source);
    bool UpdateFlag(string npcId, string key, string value, string source);
    bool RemoveFlag(string npcId, string key, string source);
    IReadOnlyList<NpcState> GetByPresence(PresenceStatus status);
    IReadOnlyList<NpcState> GetByAttitude(AttitudeLevel level);
    void RegisterNpc(string npcId);
    void SetDialogueLocked(string npcId, bool locked);
}

/// <summary>
/// Internal raw attitude writer. Romance is the public rule boundary for attitude deltas.
/// </summary>
internal interface INpcStateAttitudeWriter
{
    bool UpdateAttitude(string npcId, AttitudeLevel value, string source);
}

/// <summary>
/// Internal raw flag writer. Owning systems use transforms when queued writes must observe latest state.
/// </summary>
internal interface INpcStateFlagWriter
{
    bool UpdateFlag(string npcId, string key, string value, string source);
    bool RemoveFlag(string npcId, string key, string source);
    bool TransformFlag(string npcId, string key, Func<string?, string> transform, string source);
}

/// <summary>
/// NPC 状态管理器实现。
/// 通过 EventBus 发布变更事件；对话锁定时排队，解锁后批量生效。
/// </summary>
public sealed class NpcStateManager : INpcStateManager, INpcStateAttitudeWriter, INpcStateFlagWriter
{
    private readonly Dictionary<string, NpcState> _states = new();
    private readonly Dictionary<string, List<PendingChange>> _pendingChanges = new();
    private readonly HashSet<string> _dialogueLocked = new();
    private readonly IEventBus _eventBus;

    public NpcStateManager(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void RegisterNpc(string npcId)
    {
        if (!_states.ContainsKey(npcId))
            _states[npcId] = NpcState.CreateDefault(npcId);
    }

    public NpcState? GetState(string npcId)
    {
        return _states.TryGetValue(npcId, out var state) ? state : null;
    }

    public IReadOnlyList<NpcState> GetAll()
    {
        return _states.Values.ToList();
    }

    public IReadOnlyList<NpcState> GetByPresence(PresenceStatus status)
    {
        return _states.Values.Where(s => s.Presence == status).ToList();
    }

    public IReadOnlyList<NpcState> GetByAttitude(AttitudeLevel level)
    {
        return _states.Values.Where(s => s.Attitude == level).ToList();
    }

    public void SetDialogueLocked(string npcId, bool locked)
    {
        if (locked)
        {
            _dialogueLocked.Add(npcId);
        }
        else
        {
            _dialogueLocked.Remove(npcId);
            FlushPendingChanges(npcId);
        }
    }

    // ─── 状态更新方法 ───────────────────────────────────────

    public bool UpdateLife(string npcId, LifeStatus value, string source)
    {
        return ApplyChange(npcId, "Life", () =>
        {
            var state = _states[npcId];
            var old = state.Life.ToString();
            state.SetLife(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdatePresence(string npcId, PresenceStatus value, string source)
    {
        if (!CanModify(npcId, "Presence")) return false;
        return ApplyChange(npcId, "Presence", () =>
        {
            var state = _states[npcId];
            var old = state.Presence.ToString();
            state.SetPresence(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdateLocation(string npcId, LocationStatus value, string source)
    {
        if (!CanModify(npcId, "Location")) return false;
        return ApplyChange(npcId, "Location", () =>
        {
            var state = _states[npcId];
            var old = state.Location.ToString();
            state.SetLocation(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdateInteraction(string npcId, InteractionStatus value, string source)
    {
        if (!CanModify(npcId, "Interaction")) return false;
        return ApplyChange(npcId, "Interaction", () =>
        {
            var state = _states[npcId];
            var old = state.Interaction.ToString();
            state.SetInteraction(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdateJourney(string npcId, JourneyStage value, string source)
    {
        if (!CanModify(npcId, "Journey")) return false;
        return ApplyChange(npcId, "Journey", () =>
        {
            var state = _states[npcId];
            var old = state.Journey.ToString();
            state.SetJourney(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdateRelationship(string npcId, RelationshipStage value, string source)
    {
        if (!CanModify(npcId, "Relationship")) return false;
        return ApplyChange(npcId, "Relationship", () =>
        {
            var state = _states[npcId];
            var old = state.Relationship.ToString();
            state.SetRelationship(value, source);
            return (old, value.ToString());
        }, source);
    }

    bool INpcStateAttitudeWriter.UpdateAttitude(string npcId, AttitudeLevel value, string source)
    {
        if (!CanModify(npcId, "Attitude")) return false;
        return ApplyChange(npcId, "Attitude", () =>
        {
            var state = _states[npcId];
            var old = state.Attitude.ToString();
            state.SetAttitude(value, source);
            return (old, value.ToString());
        }, source);
    }

    public bool UpdateFlag(string npcId, string key, string value, string source)
    {
        if (!CanModify(npcId, $"Flag:{key}")) return false;
        return ApplyChange(npcId, $"Flag:{key}", () =>
        {
            var state = _states[npcId];
            var old = state.Flags.TryGetValue(key, out var existing) ? existing : "(none)";
            state.SetFlag(key, value, source);
            return (old, value);
        }, source);
    }

    public bool TransformFlag(string npcId, string key, Func<string?, string> transform, string source)
    {
        if (!CanModify(npcId, $"Flag:{key}")) return false;
        return ApplyChange(npcId, $"Flag:{key}", () =>
        {
            var state = _states[npcId];
            var old = state.Flags.TryGetValue(key, out var existing) ? existing : null;
            var next = transform(old);
            state.SetFlag(key, next, source);
            return (old ?? "(none)", next);
        }, source);
    }

    public bool RemoveFlag(string npcId, string key, string source)
    {
        if (!CanModify(npcId, $"Flag:{key}")) return false;
        if (!_states[npcId].Flags.ContainsKey(key)) return false;

        return ApplyChange(npcId, $"Flag:{key}", () =>
        {
            var state = _states[npcId];
            var old = state.Flags.TryGetValue(key, out var existing) ? existing : "(none)";
            state.RemoveFlag(key, source);
            return (old, "(removed)");
        }, source);
    }

    // ─── 私有 ───────────────────────────────────────────────

    private bool CanModify(string npcId, string field)
    {
        if (!_states.TryGetValue(npcId, out var state)) return false;
        // 死亡 NPC 只允许修改 Life 字段
        if (state.IsDead && field != "Life") return false;
        return true;
    }

    private bool ApplyChange(string npcId, string field, Func<(string old, string @new)> applyAction, string source)
    {
        if (!_states.ContainsKey(npcId)) return false;

        if (_dialogueLocked.Contains(npcId))
        {
            if (!_pendingChanges.ContainsKey(npcId))
                _pendingChanges[npcId] = new List<PendingChange>();
            _pendingChanges[npcId].Add(new PendingChange(field, applyAction, source));
            return true; // 排队成功
        }

        var (oldVal, newVal) = applyAction();
        _eventBus.Publish(new NpcStateChangedEvent(npcId, field, oldVal, newVal, source));
        return true;
    }

    private void FlushPendingChanges(string npcId)
    {
        if (!_pendingChanges.TryGetValue(npcId, out var changes)) return;

        foreach (var change in changes)
        {
            if (!CanModify(npcId, change.Field)) continue;
            var (oldVal, newVal) = change.ApplyAction();
            _eventBus.Publish(new NpcStateChangedEvent(npcId, change.Field, oldVal, newVal, change.Source));
        }

        _pendingChanges.Remove(npcId);
    }

    private sealed record PendingChange(string Field, Func<(string old, string @new)> ApplyAction, string Source);
}
