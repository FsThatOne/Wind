namespace FengZhi.Foundation.Dialogue;

/// <summary>可展示给信笺 UI 的书信快照。</summary>
public sealed record DialogueLetterSnapshot(
    string LetterId,
    string? Sender,
    string? Recipient,
    string Text,
    IReadOnlyList<string> CodePhraseIds,
    IReadOnlyList<string> EmotionalClues,
    bool IsRead);

/// <summary>对话进行中到达但尚未通知玩家的书信。</summary>
public sealed record PendingDialogueLetter(DialogueLetterSnapshot Letter, bool IsDuplicate);

/// <summary>书信匣：保存已收书信、已读状态，并处理对话中到达的通知队列。</summary>
public sealed class DialogueLetterInbox
{
    private readonly Dictionary<string, DialogueLetterRecord> _letters = new(StringComparer.Ordinal);
    private readonly Queue<PendingDialogueLetter> _pendingNotifications = new();

    /// <summary>书信总数。</summary>
    public int Count => _letters.Count;

    /// <summary>等待对话结束后通知的书信数量。</summary>
    public int PendingNotificationCount => _pendingNotifications.Count;

    /// <summary>全部书信快照，按首次收到顺序排列。</summary>
    public IReadOnlyList<DialogueLetterSnapshot> Letters =>
        _letters.Values.Select(record => record.ToSnapshot()).ToList();

    /// <summary>收到一封书信。重复到达不会重复插入，但可返回已有书信快照。</summary>
    public DialogueLetterSnapshot Receive(DialogueNode letterNode, DialogueCodePhraseBook? codePhraseBook = null)
    {
        if (!string.Equals(letterNode.Type, "letter", StringComparison.Ordinal))
            throw new ArgumentException("只有 letter 节点可以加入书信匣。", nameof(letterNode));

        if (_letters.TryGetValue(letterNode.Id, out var existing))
            return existing.ToSnapshot();

        var record = DialogueLetterRecord.FromNode(letterNode);
        _letters.Add(record.LetterId, record);
        MarkRead(record.LetterId, codePhraseBook);
        return record.ToSnapshot();
    }

    /// <summary>对话进行中收到书信，先排队，稍后通知玩家。</summary>
    public PendingDialogueLetter QueueArrival(DialogueNode letterNode, DialogueCodePhraseBook? codePhraseBook = null)
    {
        var isDuplicate = _letters.ContainsKey(letterNode.Id);
        var snapshot = Receive(letterNode, codePhraseBook);
        var pending = new PendingDialogueLetter(snapshot, isDuplicate);
        if (!isDuplicate)
            _pendingNotifications.Enqueue(pending);
        return pending;
    }

    /// <summary>对话结束后取出下一封待通知书信。</summary>
    public PendingDialogueLetter? DequeuePendingNotification()
    {
        return _pendingNotifications.Count == 0 ? null : _pendingNotifications.Dequeue();
    }

    /// <summary>从书信匣重读书信；不触发事件，也不重复学习暗号。</summary>
    public DialogueLetterSnapshot Reread(string letterId)
    {
        if (!_letters.TryGetValue(letterId, out var record))
            throw new KeyNotFoundException($"书信不存在: '{letterId}'");

        return record.ToSnapshot();
    }

    /// <summary>导出书信匣状态，供存档系统序列化。</summary>
    public DialogueLetterInboxState ToState()
    {
        return new DialogueLetterInboxState
        {
            Letters = _letters.Values.Select(record => record.ToState()).ToList(),
            PendingNotificationIds = _pendingNotifications.Select(pending => pending.Letter.LetterId).ToList()
        };
    }

    /// <summary>从存档状态恢复书信匣。</summary>
    public static DialogueLetterInbox FromState(DialogueLetterInboxState state)
    {
        var inbox = new DialogueLetterInbox();
        foreach (var letterState in state.Letters)
        {
            var record = DialogueLetterRecord.FromState(letterState);
            inbox._letters.Add(record.LetterId, record);
        }

        foreach (var letterId in state.PendingNotificationIds)
        {
            if (inbox._letters.TryGetValue(letterId, out var record))
                inbox._pendingNotifications.Enqueue(new PendingDialogueLetter(record.ToSnapshot(), IsDuplicate: false));
        }

        return inbox;
    }

    private void MarkRead(string letterId, DialogueCodePhraseBook? codePhraseBook)
    {
        var record = _letters[letterId];
        if (record.IsRead)
            return;

        record.IsRead = true;
        if (codePhraseBook == null)
            return;

        foreach (var phraseId in record.CodePhraseIds)
            codePhraseBook.Learn(phraseId);
    }
}

/// <summary>书信匣存档状态。</summary>
public sealed class DialogueLetterInboxState
{
    public List<DialogueLetterState> Letters { get; set; } = new();

    public List<string> PendingNotificationIds { get; set; } = new();
}

/// <summary>单封书信存档状态。</summary>
public sealed class DialogueLetterState
{
    public string LetterId { get; set; } = string.Empty;

    public string? Sender { get; set; }

    public string? Recipient { get; set; }

    public string Text { get; set; } = string.Empty;

    public List<string> CodePhraseIds { get; set; } = new();

    public List<string> EmotionalClues { get; set; } = new();

    public bool IsRead { get; set; }
}

internal sealed class DialogueLetterRecord
{
    public string LetterId { get; init; } = string.Empty;

    public string? Sender { get; init; }

    public string? Recipient { get; init; }

    public string Text { get; init; } = string.Empty;

    public List<string> CodePhraseIds { get; init; } = new();

    public List<string> EmotionalClues { get; init; } = new();

    public bool IsRead { get; set; }

    public DialogueLetterSnapshot ToSnapshot()
    {
        return new DialogueLetterSnapshot(
            LetterId,
            Sender,
            Recipient,
            Text,
            CodePhraseIds,
            EmotionalClues,
            IsRead);
    }

    public DialogueLetterState ToState()
    {
        return new DialogueLetterState
        {
            LetterId = LetterId,
            Sender = Sender,
            Recipient = Recipient,
            Text = Text,
            CodePhraseIds = CodePhraseIds.ToList(),
            EmotionalClues = EmotionalClues.ToList(),
            IsRead = IsRead
        };
    }

    public static DialogueLetterRecord FromNode(DialogueNode node)
    {
        return new DialogueLetterRecord
        {
            LetterId = node.Id,
            Sender = node.Sender,
            Recipient = node.Recipient,
            Text = node.Text ?? string.Empty,
            CodePhraseIds = node.CodePhraseIds.ToList(),
            EmotionalClues = node.EmotionalClues.ToList(),
            IsRead = false
        };
    }

    public static DialogueLetterRecord FromState(DialogueLetterState state)
    {
        return new DialogueLetterRecord
        {
            LetterId = state.LetterId,
            Sender = state.Sender,
            Recipient = state.Recipient,
            Text = state.Text,
            CodePhraseIds = state.CodePhraseIds.ToList(),
            EmotionalClues = state.EmotionalClues.ToList(),
            IsRead = state.IsRead
        };
    }
}
