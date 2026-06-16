namespace FengZhi.Foundation.NpcState;

/// <summary>
/// NPC 状态变更记录。每次变更都追踪来源。
/// </summary>
public sealed record StateChangeRecord(
    string Field,
    string OldValue,
    string NewValue,
    string Source,
    DateTime Timestamp
);

/// <summary>
/// NPC 多轴运行时状态。持有 8 维度 + 状态标记 + 变更历史。
/// </summary>
public sealed class NpcState
{
    public const int MaxHistoryLength = 20;

    /// <summary>NPC 模板 ID</summary>
    public string TemplateId { get; }

    /// <summary>最后一次变更的来源</summary>
    public string LastChangeSource { get; private set; } = "init";

    // ─── 8 维状态 ───────────────────────────────────────────

    public LifeStatus Life { get; private set; } = LifeStatus.Unknown;
    public PresenceStatus Presence { get; private set; } = PresenceStatus.Unreachable;
    public LocationStatus Location { get; private set; } = LocationStatus.Unknown;
    public InteractionStatus Interaction { get; private set; } = InteractionStatus.NotInteractable;
    public JourneyStage Journey { get; private set; } = JourneyStage.NotStarted;
    public RelationshipStage Relationship { get; private set; } = RelationshipStage.Stranger;
    public AttitudeLevel Attitude { get; private set; } = AttitudeLevel.Stranger;

    /// <summary>自定义状态标记（如 quest_flag, special_state 等）</summary>
    private readonly Dictionary<string, string> _flags = new();
    public IReadOnlyDictionary<string, string> Flags => _flags;

    /// <summary>变更历史</summary>
    private readonly List<StateChangeRecord> _history = new();
    public IReadOnlyList<StateChangeRecord> History => _history;

    public NpcState(string templateId)
    {
        TemplateId = templateId;
    }

    /// <summary>创建安全默认值状态</summary>
    public static NpcState CreateDefault(string templateId)
    {
        return new NpcState(templateId)
        {
            Life = LifeStatus.Unknown,
            Presence = PresenceStatus.Unreachable,
            Location = LocationStatus.Unknown,
            Interaction = InteractionStatus.NotInteractable,
            Journey = JourneyStage.NotStarted,
            Relationship = RelationshipStage.Stranger,
            Attitude = AttitudeLevel.Stranger
        };
    }

    // ─── 状态变更方法（带来源追踪） ─────────────────────────

    public void SetLife(LifeStatus value, string source)
    {
        RecordChange(nameof(Life), Life.ToString(), value.ToString(), source);
        Life = value;
        LastChangeSource = source;
    }

    public void SetPresence(PresenceStatus value, string source)
    {
        RecordChange(nameof(Presence), Presence.ToString(), value.ToString(), source);
        Presence = value;
        LastChangeSource = source;
    }

    public void SetLocation(LocationStatus value, string source)
    {
        RecordChange(nameof(Location), Location.ToString(), value.ToString(), source);
        Location = value;
        LastChangeSource = source;
    }

    public void SetInteraction(InteractionStatus value, string source)
    {
        RecordChange(nameof(Interaction), Interaction.ToString(), value.ToString(), source);
        Interaction = value;
        LastChangeSource = source;
    }

    public void SetJourney(JourneyStage value, string source)
    {
        RecordChange(nameof(Journey), Journey.ToString(), value.ToString(), source);
        Journey = value;
        LastChangeSource = source;
    }

    public void SetRelationship(RelationshipStage value, string source)
    {
        RecordChange(nameof(Relationship), Relationship.ToString(), value.ToString(), source);
        Relationship = value;
        LastChangeSource = source;
    }

    public void SetAttitude(AttitudeLevel value, string source)
    {
        RecordChange(nameof(Attitude), Attitude.ToString(), value.ToString(), source);
        Attitude = value;
        LastChangeSource = source;
    }

    public void SetFlag(string key, string value, string source)
    {
        string oldValue = _flags.TryGetValue(key, out var v) ? v : "(none)";
        RecordChange($"Flag:{key}", oldValue, value, source);
        _flags[key] = value;
        LastChangeSource = source;
    }

    public void RemoveFlag(string key, string source)
    {
        if (_flags.TryGetValue(key, out var oldValue))
        {
            RecordChange($"Flag:{key}", oldValue, "(removed)", source);
            _flags.Remove(key);
            LastChangeSource = source;
        }
    }

    // ─── 查询 ───────────────────────────────────────────────

    public bool IsDead => Life == LifeStatus.Dead;
    public bool IsInteractable => Interaction != InteractionStatus.NotInteractable && Interaction != InteractionStatus.Busy;

    // ─── 私有 ───────────────────────────────────────────────

    private void RecordChange(string field, string oldValue, string newValue, string source)
    {
        _history.Add(new StateChangeRecord(field, oldValue, newValue, source, DateTime.UtcNow));
        if (_history.Count > MaxHistoryLength)
            _history.RemoveAt(0);
    }
}
