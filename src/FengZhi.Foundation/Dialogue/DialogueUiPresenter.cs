namespace FengZhi.Foundation.Dialogue;

/// <summary>对话 UI 当前应使用的视觉通道。</summary>
public enum DialogueUiMode
{
    None,
    Speech,
    Narration,
    InnerMonologue,
    Letter,
    Choice
}

/// <summary>对话 UI 当前建议聚焦的交互目标。</summary>
public enum DialogueUiFocusTarget
{
    None,
    DialoguePanel,
    InsightPrompt,
    LetterPanel,
    ChoicePanel
}

/// <summary>对话 UI 输入设备来源，用于记录键鼠/手柄等价输入契约。</summary>
public enum DialogueUiInputSource
{
    KeyboardMouse,
    Gamepad
}

/// <summary>对话 UI 层向运行时提交的基础输入意图。</summary>
public enum DialogueUiInputIntent
{
    Confirm,
    CloseLetter
}

/// <summary>对话选项在 UI 中应使用的语义样式。</summary>
public enum DialogueUiOptionStyle
{
    Standard,
    Mindset,
    CodePhrase,
    Fallback
}

/// <summary>对话 UI 可显示的单个选项。</summary>
public sealed record DialogueUiOptionSnapshot(
    int VisibleIndex,
    int SourceIndex,
    string Text,
    DialogueUiOptionStyle Style,
    string? HintText,
    bool IsSelected,
    bool IsConfirming,
    string? CodePhraseId);

/// <summary>可供 Godot Control 层直接绑定的对话 UI 快照。</summary>
public sealed record DialogueUiSnapshot(
    DialogueUiMode Mode,
    DialogueUiFocusTarget FocusTarget,
    string? NodeId,
    string? SpeakerId,
    string? NameplateText,
    string Text,
    string VisibleText,
    bool IsTypewriterActive,
    bool ShowContinueIndicator,
    bool ShowPortrait,
    bool HighlightCurrentSpeaker,
    bool ShowChoicePanel,
    IReadOnlyList<DialogueUiOptionSnapshot> Options,
    int SelectedOptionIndex,
    bool HasInsightPrompt,
    string? InsightPromptText,
    bool ShowLetterOverlay,
    string? LetterSender,
    string? LetterRecipient);

/// <summary>
/// 对话 UI 呈现控制器：把 DialogueRuntime 状态翻译成可测试的 UI DTO，并处理基础确认/关闭输入。
/// </summary>
public sealed class DialogueUiPresenter
{
    private readonly DialogueRuntime _runtime;
    private int _selectedOptionIndex;
    private int? _confirmingOptionIndex;
    private int _inputFrame;

    public DialogueUiPresenter(DialogueRuntime runtime)
    {
        _runtime = runtime;
    }

    /// <summary>推进打字机并返回新的 UI 快照。</summary>
    public DialogueUiSnapshot Tick()
    {
        _runtime.Tick();
        return GetSnapshot();
    }

    /// <summary>读取当前 UI 快照，不改变运行时。</summary>
    public DialogueUiSnapshot GetSnapshot()
    {
        var display = _runtime.GetSnapshot();
        var node = _runtime.CurrentNode;
        var nodeType = node?.Type ?? display.NodeType;
        var mode = GetMode(_runtime.State, nodeType);
        var isTypewriterActive = _runtime.State == DialogueRuntimeState.Displaying && !display.IsTextComplete;
        var isWaitingText = _runtime.State == DialogueRuntimeState.WaitingForInput && display.IsTextComplete;
        var showChoicePanel = _runtime.State == DialogueRuntimeState.ProcessingChoice;
        var letter = _runtime.CurrentLetter;
        var options = BuildOptions();
        if (options.Count == 0 && _selectedOptionIndex != 0)
            _selectedOptionIndex = 0;
        else if (options.Count > 0 && _selectedOptionIndex >= options.Count)
            _selectedOptionIndex = options.Count - 1;

        return new DialogueUiSnapshot(
            mode,
            GetFocusTarget(_runtime.State, _runtime.HasInsightCue),
            display.NodeId,
            node?.Speaker,
            GetNameplateText(nodeType, node),
            display.Text,
            display.VisibleText,
            isTypewriterActive,
            isWaitingText,
            ShowPortrait(nodeType, node),
            HighlightCurrentSpeaker(nodeType, node),
            showChoicePanel,
            options,
            options.Count == 0 ? -1 : _selectedOptionIndex,
            _runtime.HasInsightCue,
            _runtime.HasInsightCue ? "追查" : null,
            _runtime.State == DialogueRuntimeState.LetterReading && letter != null,
            letter?.Sender,
            letter?.Recipient);
    }

    /// <summary>
    /// 处理键鼠或手柄确认意图。逐字显示时补全文字；文字完整后推进；信笺关闭返回流程。
    /// </summary>
    public DialogueUiSnapshot HandleInput(DialogueUiInputIntent intent, DialogueUiInputSource source)
    {
        _ = source;
        _inputFrame++;

        if (intent == DialogueUiInputIntent.Confirm)
        {
            _runtime.Confirm(_inputFrame);
            return GetSnapshot();
        }

        if (intent == DialogueUiInputIntent.CloseLetter && _runtime.State == DialogueRuntimeState.LetterReading)
            _runtime.Confirm(_inputFrame);

        return GetSnapshot();
    }

    /// <summary>移动当前选项焦点。正数向下，负数向上，首尾循环。</summary>
    public DialogueUiSnapshot MoveSelection(int delta, DialogueUiInputSource source)
    {
        _ = source;
        if (_runtime.State != DialogueRuntimeState.ProcessingChoice || _runtime.VisibleOptions.Count == 0)
            return GetSnapshot();

        _confirmingOptionIndex = null;
        var count = _runtime.VisibleOptions.Count;
        _selectedOptionIndex = ((_selectedOptionIndex + delta) % count + count) % count;
        return GetSnapshot();
    }

    /// <summary>确认当前选项；短暂高亮由返回快照中的 IsConfirming 表达，随后运行时立即沿分支推进。</summary>
    public DialogueUiSnapshot ConfirmSelection(DialogueUiInputSource source)
    {
        _ = source;
        if (_runtime.State != DialogueRuntimeState.ProcessingChoice || _runtime.VisibleOptions.Count == 0)
            return GetSnapshot();

        _confirmingOptionIndex = _selectedOptionIndex;
        var snapshot = GetSnapshot();
        _runtime.SelectOption(_selectedOptionIndex);
        _selectedOptionIndex = 0;
        _confirmingOptionIndex = null;
        return snapshot;
    }

    /// <summary>追查当前洞察提示；返回值是追查后的 UI 快照。</summary>
    public DialogueUiSnapshot InvestigateInsight(DialogueUiInputSource source)
    {
        _ = source;
        if (!_runtime.CanInvestigateInsight)
            return GetSnapshot();

        _runtime.InvestigateInsight();
        return GetSnapshot();
    }

    private IReadOnlyList<DialogueUiOptionSnapshot> BuildOptions()
    {
        if (_runtime.State != DialogueRuntimeState.ProcessingChoice)
            return Array.Empty<DialogueUiOptionSnapshot>();

        return _runtime.VisibleOptions
            .Select((option, visibleIndex) => new DialogueUiOptionSnapshot(
                visibleIndex,
                option.Index,
                option.Option.Text,
                GetOptionStyle(option),
                GetMindsetHint(option.Option),
                visibleIndex == _selectedOptionIndex,
                _confirmingOptionIndex == visibleIndex,
                option.CodePhraseId))
            .ToList();
    }

    private static DialogueUiOptionStyle GetOptionStyle(VisibleDialogueOption option)
    {
        return option.Kind switch
        {
            DialogueOptionKind.CodePhrase => DialogueUiOptionStyle.CodePhrase,
            DialogueOptionKind.Fallback => DialogueUiOptionStyle.Fallback,
            _ when option.Option.Events.Any(IsMindsetShift) => DialogueUiOptionStyle.Mindset,
            _ => DialogueUiOptionStyle.Standard
        };
    }

    private static string? GetMindsetHint(DialogueOption option)
    {
        var mindsetEvent = option.Events.FirstOrDefault(IsMindsetShift);
        if (mindsetEvent == null)
            return null;

        return mindsetEvent.Axis switch
        {
            "obsession" or "firmness" => "（此言带几分执念）",
            "release" or "kindness" => "（此语似有释然之意）",
            _ => "（此言牵动心境）"
        };
    }

    private static bool IsMindsetShift(DialogueEventSpec dialogueEvent)
    {
        return string.Equals(dialogueEvent.Type, "mindset_shift", StringComparison.Ordinal);
    }

    private static DialogueUiMode GetMode(DialogueRuntimeState state, string? nodeType)
    {
        if (state is DialogueRuntimeState.Idle or DialogueRuntimeState.Exiting)
            return DialogueUiMode.None;
        if (state == DialogueRuntimeState.LetterReading)
            return DialogueUiMode.Letter;
        if (state == DialogueRuntimeState.ProcessingChoice)
            return DialogueUiMode.Choice;

        return nodeType switch
        {
            "speech" => DialogueUiMode.Speech,
            "narration" => DialogueUiMode.Narration,
            "inner_monologue" => DialogueUiMode.InnerMonologue,
            _ => DialogueUiMode.None
        };
    }

    private static DialogueUiFocusTarget GetFocusTarget(DialogueRuntimeState state, bool hasInsightPrompt)
    {
        if (hasInsightPrompt)
            return DialogueUiFocusTarget.InsightPrompt;

        return state switch
        {
            DialogueRuntimeState.Displaying or DialogueRuntimeState.WaitingForInput => DialogueUiFocusTarget.DialoguePanel,
            DialogueRuntimeState.LetterReading => DialogueUiFocusTarget.LetterPanel,
            DialogueRuntimeState.ProcessingChoice => DialogueUiFocusTarget.ChoicePanel,
            _ => DialogueUiFocusTarget.None
        };
    }

    private static string? GetNameplateText(string? nodeType, DialogueNode? node)
    {
        if (node == null)
            return null;
        if (string.Equals(nodeType, "speech", StringComparison.Ordinal))
            return node.Speaker;
        if (string.Equals(nodeType, "inner_monologue", StringComparison.Ordinal))
            return "内心";
        return null;
    }

    private static bool ShowPortrait(string? nodeType, DialogueNode? node)
    {
        if (string.IsNullOrWhiteSpace(node?.Speaker))
            return false;
        return string.Equals(nodeType, "speech", StringComparison.Ordinal) ||
               string.Equals(nodeType, "choice", StringComparison.Ordinal);
    }

    private static bool HighlightCurrentSpeaker(string? nodeType, DialogueNode? node)
    {
        return ShowPortrait(nodeType, node);
    }
}
