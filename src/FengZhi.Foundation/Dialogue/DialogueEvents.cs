using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Dialogue;

/// <summary>对话心境位移事件。</summary>
public sealed record DialogueMindsetShiftEvent(string Axis, int Delta) : GameEvent;

/// <summary>对话关系变化事件。</summary>
public sealed record DialogueRelationshipChangeEvent(string NpcId, int Delta) : GameEvent;

/// <summary>对话任务标记事件。</summary>
public sealed record DialogueQuestFlagEvent(string Key, string? Value) : GameEvent;

/// <summary>对话 NPC 状态变化事件。</summary>
public sealed record DialogueNpcStateChangeEvent(string NpcId, string NewState) : GameEvent;

/// <summary>对话洞察发现事件。</summary>
public sealed record DialogueInsightDiscoveredEvent(string InsightId) : GameEvent;

/// <summary>对话暗号习得事件。</summary>
public sealed record DialogueCodePhraseLearnedEvent(string PhraseId) : GameEvent;

/// <summary>对话物品获得事件。</summary>
public sealed record DialogueItemGrantEvent(string ItemId, int Quantity) : GameEvent;

/// <summary>对话触发战斗事件。</summary>
public sealed record DialogueCombatTriggerEvent(string CombatId) : GameEvent;

/// <summary>对话章节推进事件。</summary>
public sealed record DialogueChapterAdvanceEvent(string ChapterId) : GameEvent;

/// <summary>对话注册误会事件。</summary>
public sealed record DialogueRegisterMisunderstandingEvent(string Key, string? Value) : GameEvent;

/// <summary>
/// 对话事件队列：收集节点/选项事件，并在世界恢复后按编辑顺序发布。
/// </summary>
public sealed class DialogueEventQueue
{
    private readonly IEventBus _eventBus;
    private readonly Queue<GameEvent> _pendingEvents = new();
    private readonly List<string> _errors = new();

    public DialogueEventQueue(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>待分发事件数量。</summary>
    public int PendingCount => _pendingEvents.Count;

    /// <summary>事件映射错误。</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>清理当前批次。</summary>
    public void Clear()
    {
        _pendingEvents.Clear();
        _errors.Clear();
    }

    /// <summary>按编辑顺序入队一组 YAML 事件。</summary>
    public void EnqueueRange(IEnumerable<DialogueEventSpec> specs)
    {
        foreach (var spec in specs)
            Enqueue(spec);
    }

    /// <summary>入队单个 YAML 事件。</summary>
    public void Enqueue(DialogueEventSpec spec)
    {
        var gameEvent = CreateEvent(spec);
        if (gameEvent == null)
            return;
        _pendingEvents.Enqueue(gameEvent);
    }

    /// <summary>世界恢复后分发当前批次，返回实际发布数量。</summary>
    public int DispatchPending()
    {
        var dispatched = 0;
        while (_pendingEvents.Count > 0)
        {
            Publish(_pendingEvents.Dequeue());
            dispatched++;
        }

        return dispatched;
    }

    private GameEvent? CreateEvent(DialogueEventSpec spec)
    {
        switch (spec.Type)
        {
            case "mindset_shift":
                if (Require(spec.Axis, spec.Type, "axis", out var axis) &&
                    Require(spec.Delta, spec.Type, "delta", out var delta))
                    return new DialogueMindsetShiftEvent(axis, delta);
                return null;

            case "relationship_change":
                if (Require(spec.Key, spec.Type, "key", out var npcId) &&
                    Require(spec.Delta, spec.Type, "delta", out var relationshipDelta))
                    return new DialogueRelationshipChangeEvent(npcId, relationshipDelta);
                return null;

            case "quest_flag":
            case "set_flag":
                if (Require(spec.Key, spec.Type, "key", out var key))
                    return new DialogueQuestFlagEvent(key, spec.Value);
                return null;

            case "npc_state_change":
                if (Require(spec.Key, spec.Type, "key", out var npcStateId) &&
                    Require(spec.Value, spec.Type, "value", out var newState))
                    return new DialogueNpcStateChangeEvent(npcStateId, newState);
                return null;

            case "insight_discovered":
                if (Require(spec.Key, spec.Type, "key", out var insightId))
                    return new DialogueInsightDiscoveredEvent(insightId);
                return null;

            case "code_phrase_learned":
                if (Require(spec.Key, spec.Type, "key", out var phraseId))
                    return new DialogueCodePhraseLearnedEvent(phraseId);
                return null;

            case "item_grant":
                if (Require(spec.Key, spec.Type, "key", out var itemId))
                    return new DialogueItemGrantEvent(itemId, spec.Delta ?? 1);
                return null;

            case "combat_trigger":
                if (Require(spec.Key, spec.Type, "key", out var combatId))
                    return new DialogueCombatTriggerEvent(combatId);
                return null;

            case "chapter_advance":
                if (Require(spec.Key, spec.Type, "key", out var chapterId))
                    return new DialogueChapterAdvanceEvent(chapterId);
                return null;

            case "register_misunderstanding":
                if (Require(spec.Key, spec.Type, "key", out var misunderstandingKey))
                    return new DialogueRegisterMisunderstandingEvent(misunderstandingKey, spec.Value);
                return null;

            default:
                _errors.Add($"未知对话事件类型: '{spec.Type}'");
                return null;
        }
    }

    private static void Publish(IEventBus eventBus, GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case DialogueMindsetShiftEvent e:
                eventBus.Publish(e);
                break;
            case DialogueRelationshipChangeEvent e:
                eventBus.Publish(e);
                break;
            case DialogueQuestFlagEvent e:
                eventBus.Publish(e);
                break;
            case DialogueNpcStateChangeEvent e:
                eventBus.Publish(e);
                break;
            case DialogueInsightDiscoveredEvent e:
                eventBus.Publish(e);
                break;
            case DialogueCodePhraseLearnedEvent e:
                eventBus.Publish(e);
                break;
            case DialogueItemGrantEvent e:
                eventBus.Publish(e);
                break;
            case DialogueCombatTriggerEvent e:
                eventBus.Publish(e);
                break;
            case DialogueChapterAdvanceEvent e:
                eventBus.Publish(e);
                break;
            case DialogueRegisterMisunderstandingEvent e:
                eventBus.Publish(e);
                break;
        }
    }

    private void Publish(GameEvent gameEvent)
    {
        Publish(_eventBus, gameEvent);
    }

    private bool Require(string? value, string eventType, string field, out string result)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            result = value;
            return true;
        }

        _errors.Add($"事件 '{eventType}' 缺少字段 '{field}'");
        result = string.Empty;
        return false;
    }

    private bool Require(int? value, string eventType, string field, out int result)
    {
        if (value.HasValue)
        {
            result = value.Value;
            return true;
        }

        _errors.Add($"事件 '{eventType}' 缺少字段 '{field}'");
        result = 0;
        return false;
    }
}
