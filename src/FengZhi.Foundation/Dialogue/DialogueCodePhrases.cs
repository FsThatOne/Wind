namespace FengZhi.Foundation.Dialogue;

/// <summary>一次暗号在特定上下文中的可用效果。</summary>
public sealed record DialogueCodePhraseMatch(
    string PhraseId,
    string ContextKey,
    string Text,
    string? Next,
    IReadOnlyList<DialogueConditionSpec>? Conditions = null,
    IReadOnlyList<DialogueConditionSpec>? ConditionsAnyOf = null,
    IReadOnlyList<DialogueEventSpec>? Events = null);

/// <summary>当前对话上下文中可注入的暗号选项来源。</summary>
public interface IDialogueCodePhraseProvider
{
    /// <summary>返回当前 Choice 节点可用的暗号候选，顺序即显示顺序。</summary>
    IReadOnlyList<DialogueCodePhraseMatch> GetMatches(DialogueNode choiceNode);
}

/// <summary>可序列化的暗号簿状态：记录已学暗号和按上下文独立使用的暗号。</summary>
public sealed class DialogueCodePhraseBook
{
    private readonly HashSet<string> _learnedPhraseIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedContextKeys = new(StringComparer.Ordinal);

    /// <summary>已学暗号 ID。</summary>
    public IReadOnlyCollection<string> LearnedPhraseIds => _learnedPhraseIds;

    /// <summary>已使用的“暗号 + 上下文”键。</summary>
    public IReadOnlyCollection<string> UsedContextKeys => _usedContextKeys;

    /// <summary>学习暗号；重复学习不会产生额外状态变化。</summary>
    public bool Learn(string phraseId)
    {
        if (string.IsNullOrWhiteSpace(phraseId))
            throw new ArgumentException("暗号 ID 不能为空。", nameof(phraseId));

        return _learnedPhraseIds.Add(phraseId);
    }

    /// <summary>是否已经学会指定暗号。</summary>
    public bool HasLearned(string phraseId)
    {
        return _learnedPhraseIds.Contains(phraseId);
    }

    /// <summary>标记指定暗号在当前上下文已使用。</summary>
    public bool MarkUsed(string phraseId, string contextKey)
    {
        if (!HasLearned(phraseId))
            return false;

        return _usedContextKeys.Add(BuildContextKey(phraseId, contextKey));
    }

    /// <summary>指定暗号在当前上下文是否已使用。</summary>
    public bool IsUsed(string phraseId, string contextKey)
    {
        return _usedContextKeys.Contains(BuildContextKey(phraseId, contextKey));
    }

    /// <summary>导出暗号簿状态，供存档系统序列化。</summary>
    public DialogueCodePhraseBookState ToState()
    {
        return new DialogueCodePhraseBookState
        {
            LearnedPhraseIds = _learnedPhraseIds.Order(StringComparer.Ordinal).ToList(),
            UsedContextKeys = _usedContextKeys.Order(StringComparer.Ordinal).ToList()
        };
    }

    /// <summary>从存档状态恢复暗号簿。</summary>
    public static DialogueCodePhraseBook FromState(DialogueCodePhraseBookState state)
    {
        var book = new DialogueCodePhraseBook();
        foreach (var phraseId in state.LearnedPhraseIds)
            book._learnedPhraseIds.Add(phraseId);
        foreach (var contextKey in state.UsedContextKeys)
            book._usedContextKeys.Add(contextKey);
        return book;
    }

    private static string BuildContextKey(string phraseId, string contextKey)
    {
        if (string.IsNullOrWhiteSpace(phraseId))
            throw new ArgumentException("暗号 ID 不能为空。", nameof(phraseId));
        if (string.IsNullOrWhiteSpace(contextKey))
            throw new ArgumentException("暗号上下文不能为空。", nameof(contextKey));

        return $"{phraseId}@{contextKey}";
    }
}

/// <summary>暗号簿存档状态。</summary>
public sealed class DialogueCodePhraseBookState
{
    public List<string> LearnedPhraseIds { get; set; } = new();

    public List<string> UsedContextKeys { get; set; } = new();
}

/// <summary>基于暗号簿与候选表的简单匹配器，供测试、内容原型和后续数据层复用。</summary>
public sealed class DialogueCodePhraseProvider : IDialogueCodePhraseProvider
{
    private readonly DialogueCodePhraseBook _book;
    private readonly IReadOnlyList<DialogueCodePhraseMatch> _matches;

    public DialogueCodePhraseProvider(DialogueCodePhraseBook book, IReadOnlyList<DialogueCodePhraseMatch> matches)
    {
        _book = book;
        _matches = matches;
    }

    public IReadOnlyList<DialogueCodePhraseMatch> GetMatches(DialogueNode choiceNode)
    {
        return _matches
            .Where(match => _book.HasLearned(match.PhraseId))
            .Where(match => !_book.IsUsed(match.PhraseId, match.ContextKey))
            .ToList();
    }
}
