using FengZhi.Foundation.StateMachine;

namespace FengZhi.Foundation.Dialogue;

/// <summary>
/// 对话运行时状态。
/// </summary>
public enum DialogueRuntimeState
{
    Idle,
    Entering,
    Displaying,
    WaitingForInput,
    ProcessingChoice,
    LetterReading,
    Exiting
}

/// <summary>
/// UI 可读取的当前对话显示快照。
/// </summary>
public sealed record DialogueDisplaySnapshot(
    DialogueRuntimeState State,
    string? NodeId,
    string? NodeType,
    string Text,
    string VisibleText,
    bool IsTextComplete
);

/// <summary>对话洞察属性查询接口。对话系统只读取最终 insight 值。</summary>
public interface IDialogueInsightValueProvider
{
    /// <summary>玩家当前洞察值。</summary>
    int GetPlayerInsight();
}

/// <summary>UI 可读取的洞察提示状态。</summary>
public sealed record DialogueInsightCue(
    string SourceNodeId,
    string InsightNodeId,
    int RequiredInsight,
    int PlayerInsight,
    bool CanInvestigate
);

/// <summary>
/// 纯 C# 对话图运行器，负责节点推进、打字机阶段和死循环保护。
/// </summary>
public sealed class DialogueRuntime
{
    private const string Enter = "enter";
    private const string Display = "display";
    private const string Wait = "wait";
    private const string ProcessChoice = "process_choice";
    private const string ReadLetter = "read_letter";
    private const string Exit = "exit";
    private const string Reset = "reset";

    private readonly StateMachine<DialogueRuntimeState> _stateMachine;
    private readonly List<string> _errors = new();
    private readonly DialogueConditionEvaluator? _conditionEvaluator;
    private readonly DialogueEventQueue? _eventQueue;
    private readonly IDialogueInsightValueProvider? _insightValueProvider;
    private readonly IDialogueCodePhraseProvider? _codePhraseProvider;
    private readonly DialogueCodePhraseBook? _codePhraseBook;
    private readonly DialogueLetterInbox? _letterInbox;
    private DialogueSequence? _sequence;
    private Dictionary<string, DialogueNode> _nodes = new(StringComparer.Ordinal);
    private DialogueNode? _currentNode;
    private IReadOnlyList<VisibleDialogueOption> _visibleOptions = Array.Empty<VisibleDialogueOption>();
    private DialogueInsightCue? _insightCue;
    private DialogueLetterSnapshot? _currentLetter;
    private readonly HashSet<string> _discoveredInsightNodeIds = new(StringComparer.Ordinal);
    private int _visibleCharacters;
    private int _visitedNodeCount;
    private int _lastConfirmFrame = -1;

    /// <summary>每次推进显示的字符数；小于等于 0 时视为瞬间显示。</summary>
    public int CharactersPerTick { get; set; } = 1;

    /// <summary>当前运行时状态。</summary>
    public DialogueRuntimeState State => _stateMachine.CurrentState;

    /// <summary>当前节点。</summary>
    public DialogueNode? CurrentNode => _currentNode;

    /// <summary>当前 Choice 节点可见选项。</summary>
    public IReadOnlyList<VisibleDialogueOption> VisibleOptions => _visibleOptions;

    /// <summary>当前洞察提示；仅在 speech / narration 的 WaitingForInput 阶段可能存在。</summary>
    public DialogueInsightCue? CurrentInsightCue => _insightCue;

    /// <summary>当前是否显示洞察提示。</summary>
    public bool HasInsightCue => _insightCue != null;

    /// <summary>当前是否允许玩家追查洞察分支。</summary>
    public bool CanInvestigateInsight => _insightCue?.CanInvestigate == true;

    /// <summary>当前打开的书信快照；仅 LetterReading 阶段存在。</summary>
    public DialogueLetterSnapshot? CurrentLetter => _currentLetter;

    /// <summary>本次对话已访问的节点数。</summary>
    public int VisitedNodeCount => _visitedNodeCount;

    /// <summary>运行时错误日志，如死循环保护触发。</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>状态转换历史。</summary>
    public IReadOnlyList<TransitionRecord<DialogueRuntimeState>> StateHistory => _stateMachine.History;

    public DialogueRuntime(
        DialogueConditionEvaluator? conditionEvaluator = null,
        DialogueEventQueue? eventQueue = null,
        IDialogueInsightValueProvider? insightValueProvider = null,
        IDialogueCodePhraseProvider? codePhraseProvider = null,
        DialogueCodePhraseBook? codePhraseBook = null,
        DialogueLetterInbox? letterInbox = null)
    {
        _conditionEvaluator = conditionEvaluator;
        _eventQueue = eventQueue;
        _insightValueProvider = insightValueProvider;
        _codePhraseProvider = codePhraseProvider;
        _codePhraseBook = codePhraseBook;
        _letterInbox = letterInbox;
        _stateMachine = new StateMachine<DialogueRuntimeState>(DialogueRuntimeState.Idle)
        {
            MaxHistoryLength = 128
        };

        _stateMachine.AddTransition(DialogueRuntimeState.Idle, Enter, DialogueRuntimeState.Entering);
        _stateMachine.AddTransition(DialogueRuntimeState.Entering, Display, DialogueRuntimeState.Displaying);
        _stateMachine.AddTransition(DialogueRuntimeState.Entering, ProcessChoice, DialogueRuntimeState.ProcessingChoice);
        _stateMachine.AddTransition(DialogueRuntimeState.Entering, ReadLetter, DialogueRuntimeState.LetterReading);
        _stateMachine.AddTransition(DialogueRuntimeState.Displaying, Wait, DialogueRuntimeState.WaitingForInput);
        _stateMachine.AddTransition(DialogueRuntimeState.WaitingForInput, Enter, DialogueRuntimeState.Entering);
        _stateMachine.AddTransition(DialogueRuntimeState.ProcessingChoice, Enter, DialogueRuntimeState.Entering);
        _stateMachine.AddTransition(DialogueRuntimeState.LetterReading, Enter, DialogueRuntimeState.Entering);
        _stateMachine.AddTransition(DialogueRuntimeState.Entering, Exit, DialogueRuntimeState.Exiting);
        _stateMachine.AddTransition(DialogueRuntimeState.WaitingForInput, Exit, DialogueRuntimeState.Exiting);
        _stateMachine.AddTransition(DialogueRuntimeState.ProcessingChoice, Exit, DialogueRuntimeState.Exiting);
        _stateMachine.AddTransition(DialogueRuntimeState.LetterReading, Exit, DialogueRuntimeState.Exiting);
        _stateMachine.AddTransition(DialogueRuntimeState.Exiting, Reset, DialogueRuntimeState.Idle);
    }

    /// <summary>
    /// 启动一个对话序列。
    /// </summary>
    public void Start(DialogueSequence sequence)
    {
        if (State != DialogueRuntimeState.Idle)
            throw new InvalidOperationException("对话运行时必须处于 Idle 才能启动新对话。");

        _sequence = sequence;
        _nodes = sequence.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        _currentNode = null;
        _visibleOptions = Array.Empty<VisibleDialogueOption>();
        _insightCue = null;
        _currentLetter = null;
        _discoveredInsightNodeIds.Clear();
        _visibleCharacters = 0;
        _visitedNodeCount = 0;
        _lastConfirmFrame = -1;
        _errors.Clear();
        _eventQueue?.Clear();

        _stateMachine.TryTransition(Enter);
        EnterNode(sequence.EntryNode);
    }

    /// <summary>
    /// 推进打字机显示。返回当前显示快照。
    /// </summary>
    public DialogueDisplaySnapshot Tick()
    {
        if (State != DialogueRuntimeState.Displaying || _currentNode == null)
            return GetSnapshot();

        var text = GetNodeText(_currentNode);
        if (CharactersPerTick <= 0)
            _visibleCharacters = text.Length;
        else
            _visibleCharacters = Math.Min(text.Length, _visibleCharacters + CharactersPerTick);

        if (_visibleCharacters >= text.Length)
        {
            _stateMachine.TryTransition(Wait);
            RefreshInsightCue();
        }

        return GetSnapshot();
    }

    /// <summary>
    /// 处理确认输入；同一帧重复确认只消费第一次，避免跨过多个节点。
    /// </summary>
    public DialogueDisplaySnapshot Confirm(int frame)
    {
        if (frame == _lastConfirmFrame)
            return GetSnapshot();

        _lastConfirmFrame = frame;

        if (State == DialogueRuntimeState.Displaying && _currentNode != null)
        {
            _visibleCharacters = GetNodeText(_currentNode).Length;
            _stateMachine.TryTransition(Wait);
            RefreshInsightCue();
            return GetSnapshot();
        }

        if (State == DialogueRuntimeState.WaitingForInput && _currentNode != null)
        {
            _insightCue = null;
            _eventQueue?.EnqueueRange(_currentNode.Events);
            MoveAfterEvents(_currentNode.Next, _currentNode.Events);
            return GetSnapshot();
        }

        if (State == DialogueRuntimeState.LetterReading && _currentNode != null)
        {
            _currentLetter = null;
            _eventQueue?.EnqueueRange(_currentNode.Events);
            MoveAfterEvents(_currentNode.Next, _currentNode.Events);
            return GetSnapshot();
        }

        return GetSnapshot();
    }

    /// <summary>
    /// 玩家选择追查当前洞察提示。成功时进入 insight_next，并自动入队 insight_discovered。
    /// </summary>
    public DialogueDisplaySnapshot InvestigateInsight()
    {
        if (State != DialogueRuntimeState.WaitingForInput || _currentNode == null || _insightCue == null)
            return GetSnapshot();

        var cue = _insightCue;
        _insightCue = null;
        _eventQueue?.EnqueueRange(_currentNode.Events);
        EnqueueInsightDiscoveredOnce(cue.InsightNodeId);
        MoveAfterEvents(cue.InsightNodeId, _currentNode.Events);
        return GetSnapshot();
    }

    /// <summary>
    /// 选择当前 Choice 节点的可见选项。
    /// </summary>
    public DialogueDisplaySnapshot SelectOption(int visibleOptionIndex)
    {
        if (State != DialogueRuntimeState.ProcessingChoice || _currentNode == null)
            return GetSnapshot();

        if (visibleOptionIndex < 0 || visibleOptionIndex >= _visibleOptions.Count)
            throw new ArgumentOutOfRangeException(nameof(visibleOptionIndex), "可见选项下标越界。");

        var selected = _visibleOptions[visibleOptionIndex].Option;
        _eventQueue?.EnqueueRange(_currentNode.Events);
        _eventQueue?.EnqueueRange(selected.Events);
        var visibleOption = _visibleOptions[visibleOptionIndex];
        if (visibleOption.Kind == DialogueOptionKind.CodePhrase &&
            visibleOption.CodePhraseId != null &&
            visibleOption.CodePhraseContextKey != null)
        {
            _codePhraseBook?.MarkUsed(visibleOption.CodePhraseId, visibleOption.CodePhraseContextKey);
        }

        var combinedEvents = _currentNode.Events.Concat(selected.Events);
        MoveAfterEvents(selected.Next, combinedEvents);
        return GetSnapshot();
    }

    /// <summary>
    /// 结束 Exiting 状态并回到 Idle。
    /// </summary>
    public void CompleteExit()
    {
        if (State == DialogueRuntimeState.Exiting)
            _stateMachine.TryTransition(Reset);
    }

    /// <summary>
    /// 世界恢复后发布本次对话累计事件。
    /// </summary>
    public int RestoreWorldAndDispatchEvents()
    {
        return State == DialogueRuntimeState.Idle ? _eventQueue?.DispatchPending() ?? 0 : 0;
    }

    /// <summary>
    /// 获取当前显示快照。
    /// </summary>
    public DialogueDisplaySnapshot GetSnapshot()
    {
        var text = _currentNode == null ? string.Empty : GetNodeText(_currentNode);
        var visibleLength = Math.Clamp(_visibleCharacters, 0, text.Length);
        return new DialogueDisplaySnapshot(
            State,
            _currentNode?.Id,
            _currentNode?.Type,
            text,
            text[..visibleLength],
            visibleLength == text.Length
        );
    }

    private void EnterNode(string? nodeId)
    {
        if (_sequence == null || string.IsNullOrWhiteSpace(nodeId) || DialogueConfigLoader.IsEndReference(nodeId))
        {
            ExitDialogue();
            return;
        }

        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            _errors.Add($"对话节点不存在: '{nodeId}'");
            ExitDialogue();
            return;
        }

        _visitedNodeCount++;
        if (_visitedNodeCount >= DialogueConfigLoader.MaxNodeVisitsPerRun)
        {
            _errors.Add($"对话访问节点数达到上限 {DialogueConfigLoader.MaxNodeVisitsPerRun}: '{node.Id}'");
            ExitDialogue();
            return;
        }

        _currentNode = node;
        _visibleOptions = Array.Empty<VisibleDialogueOption>();
        _insightCue = null;
        _currentLetter = null;
        _visibleCharacters = 0;

        if (State != DialogueRuntimeState.Entering)
            _stateMachine.TryTransition(Enter);

        if (_conditionEvaluator != null)
        {
            var conditionResult = _conditionEvaluator.EvaluateNode(node);
            if (!conditionResult.IsMet)
            {
                if (conditionResult.Error != null)
                    _errors.Add($"节点 '{node.Id}' 入口条件失败: {conditionResult.Error}");
                MoveToNext(node.Fallback ?? node.Next);
                return;
            }
        }

        if (string.Equals(node.Type, "choice", StringComparison.Ordinal))
        {
            if (_conditionEvaluator != null)
            {
                _visibleOptions = AppendCodePhraseOptions(node, _conditionEvaluator.GetVisibleOptions(node));
                foreach (var error in _conditionEvaluator.Errors)
                    _errors.Add(error);
            }
            else
            {
                var standardOptions = node.Options
                    .Select((option, index) => new VisibleDialogueOption(index, option, IsFallback: false))
                    .ToList();
                _visibleOptions = AppendCodePhraseOptions(node, standardOptions);
            }

            _stateMachine.TryTransition(ProcessChoice);
            return;
        }

        if (string.Equals(node.Type, "letter", StringComparison.Ordinal))
        {
            _visibleCharacters = GetNodeText(node).Length;
            _currentLetter = _letterInbox?.Receive(node, _codePhraseBook) ??
                new DialogueLetterSnapshot(
                    node.Id,
                    node.Sender,
                    node.Recipient,
                    GetNodeText(node),
                    node.CodePhraseIds,
                    node.EmotionalClues,
                    IsRead: true);
            _stateMachine.TryTransition(ReadLetter);
            return;
        }

        _stateMachine.TryTransition(Display);
        if (CharactersPerTick <= 0)
            Tick();
    }

    private void RefreshInsightCue()
    {
        _insightCue = null;
        if (_currentNode == null || _insightValueProvider == null)
            return;
        if (State != DialogueRuntimeState.WaitingForInput)
            return;
        if (!IsInsightCueSource(_currentNode))
            return;
        if (!_currentNode.InsightLevelRequired.HasValue || string.IsNullOrWhiteSpace(_currentNode.InsightNext))
            return;

        var playerInsight = _insightValueProvider.GetPlayerInsight();
        var requiredInsight = _currentNode.InsightLevelRequired.Value;
        if (playerInsight < requiredInsight)
            return;

        _insightCue = new DialogueInsightCue(
            _currentNode.Id,
            _currentNode.InsightNext,
            requiredInsight,
            playerInsight,
            CanInvestigate: true);
    }

    private void EnqueueInsightDiscoveredOnce(string insightNodeId)
    {
        if (!_discoveredInsightNodeIds.Add(insightNodeId))
            return;

        _eventQueue?.Enqueue(new DialogueEventSpec
        {
            Type = "insight_discovered",
            Key = insightNodeId
        });
    }

    private IReadOnlyList<VisibleDialogueOption> AppendCodePhraseOptions(
        DialogueNode choiceNode,
        IReadOnlyList<VisibleDialogueOption> standardOptions)
    {
        if (_codePhraseProvider == null)
            return standardOptions;

        var result = standardOptions.ToList();
        var matches = _codePhraseProvider.GetMatches(choiceNode);
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var option = new DialogueOption
            {
                Text = match.Text,
                Next = match.Next
            };

            foreach (var condition in match.Conditions ?? Array.Empty<DialogueConditionSpec>())
                option.Conditions.Add(condition);
            foreach (var condition in match.ConditionsAnyOf ?? Array.Empty<DialogueConditionSpec>())
                option.ConditionsAnyOf.Add(condition);
            foreach (var dialogueEvent in match.Events ?? Array.Empty<DialogueEventSpec>())
                option.Events.Add(dialogueEvent);

            if (_conditionEvaluator != null)
            {
                var conditionResult = _conditionEvaluator.Evaluate(option.Conditions, option.ConditionsAnyOf);
                if (!conditionResult.IsMet)
                {
                    if (conditionResult.Error != null)
                        _errors.Add($"暗号 '{match.PhraseId}' 条件查询失败: {conditionResult.Error}");
                    continue;
                }
            }

            result.Add(new VisibleDialogueOption(
                -1000 - i,
                option,
                IsFallback: false,
                Kind: DialogueOptionKind.CodePhrase,
                CodePhraseId: match.PhraseId,
                CodePhraseContextKey: match.ContextKey));
        }

        return result;
    }

    private void MoveToNext(string? nextNodeId)
    {
        if (string.IsNullOrWhiteSpace(nextNodeId) || DialogueConfigLoader.IsEndReference(nextNodeId))
        {
            ExitDialogue();
            return;
        }

        _stateMachine.TryTransition(Enter);
        EnterNode(nextNodeId);
    }

    private void MoveAfterEvents(string? nextNodeId, IEnumerable<DialogueEventSpec> events)
    {
        if (events.Any(e => string.Equals(e.Type, "combat_trigger", StringComparison.Ordinal)))
        {
            ExitDialogue();
            return;
        }

        MoveToNext(nextNodeId);
    }

    private void ExitDialogue()
    {
        _currentNode = null;
        _visibleOptions = Array.Empty<VisibleDialogueOption>();
        _insightCue = null;
        _currentLetter = null;
        _visibleCharacters = 0;
        if (State != DialogueRuntimeState.Exiting)
            _stateMachine.TryTransition(Exit);
    }

    private static bool IsInsightCueSource(DialogueNode node)
    {
        return string.Equals(node.Type, "speech", StringComparison.Ordinal) ||
               string.Equals(node.Type, "narration", StringComparison.Ordinal);
    }

    private static string GetNodeText(DialogueNode node)
    {
        if (!string.IsNullOrEmpty(node.Text))
            return node.Text;
        if (!string.IsNullOrEmpty(node.Prompt))
            return node.Prompt;
        if (!string.IsNullOrEmpty(node.Response))
            return node.Response;
        return string.Empty;
    }
}
