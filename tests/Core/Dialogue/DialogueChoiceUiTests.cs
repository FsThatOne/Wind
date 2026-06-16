using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueChoiceUiTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void ChoiceNode_ShowsVisibleOptionsInEditOrderWithoutHiddenPlaceholders()
    {
        var runtime = new DialogueRuntime(
            conditionEvaluator: new DialogueConditionEvaluator(new ChoiceValueProvider(("flag.hidden", "false"))));
        runtime.Start(Load("""
id: choice_order
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "第一句", next: END }
      - text: "隐藏句"
        next: END
        conditions:
          - { source: flag.hidden, op: eq, value: true }
      - { text: "第三句", next: END }
"""));
        var presenter = new DialogueUiPresenter(runtime);

        var snapshot = presenter.GetSnapshot();

        Assert.True(snapshot.ShowChoicePanel);
        Assert.Equal(new[] { "第一句", "第三句" }, snapshot.Options.Select(o => o.Text));
        Assert.Equal(new[] { 0, 2 }, snapshot.Options.Select(o => o.SourceIndex));
        Assert.DoesNotContain(snapshot.Options, option => option.Text == "隐藏句");
    }

    [Fact]
    public void MindsetOption_ShowsLiteraryHintWithoutAxisDeltaOrFormula()
    {
        var presenter = StartPresenter("""
id: mindset_choice
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "我偏要查个水落石出。"
        next: END
        events:
          - { type: mindset_shift, axis: firmness, delta: 2 }
      - { text: "暂且放下。", next: END }
""");

        var mindset = presenter.GetSnapshot().Options[0];

        Assert.Equal(DialogueUiOptionStyle.Mindset, mindset.Style);
        Assert.Equal("（此言带几分执念）", mindset.HintText);
        Assert.DoesNotContain("firmness", mindset.HintText);
        Assert.DoesNotContain("2", mindset.HintText);
        Assert.DoesNotContain("delta", mindset.HintText);
    }

    [Fact]
    public void InsightCue_PersistsUntilPlayerInvestigatesOrAdvances()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(20))
        {
            CharactersPerTick = 100
        };
        runtime.Start(Load("""
id: insight_choice_ui
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "话里藏针。"
    insight_level_required: 10
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "他故意避开了师门旧案。", next: END }
  - { id: normal, type: narration, text: "你没有追问。", next: END }
"""));
        var presenter = new DialogueUiPresenter(runtime);

        var first = presenter.Tick();
        var second = presenter.GetSnapshot();

        Assert.True(first.HasInsightPrompt);
        Assert.True(second.HasInsightPrompt);
        Assert.Equal(DialogueUiFocusTarget.InsightPrompt, first.FocusTarget);
        Assert.Equal("追查", first.InsightPromptText);

        var investigated = presenter.InvestigateInsight(DialogueUiInputSource.Gamepad);

        Assert.Equal("hidden", investigated.NodeId);
        Assert.False(investigated.HasInsightPrompt);
    }

    [Fact]
    public void CodePhraseOption_UsesDistinctStyleWithoutRevealingMatchRule()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "白蘋渡口，芦花如雪。", "hidden")
            }),
            codePhraseBook: book);
        runtime.Start(Load("""
id: code_phrase_choice_ui
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "寒暄", next: END }
    code_phrase_nexts: [hidden]
  - { id: hidden, type: code_phrase, correct_phrase: white_reed, response: "故人果然还记得。", next: END }
"""));
        var presenter = new DialogueUiPresenter(runtime);

        var codePhrase = presenter.GetSnapshot().Options[1];

        Assert.Equal(DialogueUiOptionStyle.CodePhrase, codePhrase.Style);
        Assert.Equal("white_reed", codePhrase.CodePhraseId);
        Assert.Null(codePhrase.HintText);
        Assert.DoesNotContain("npc_bai@inn", codePhrase.Text);
    }

    [Fact]
    public void ConfirmSelection_ReturnsHighlightSnapshotAndImmediatelyAdvancesBranch()
    {
        var runtime = new DialogueRuntime();
        runtime.Start(Load("""
id: confirm_choice_ui
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "留在原地", next: stay }
      - { text: "追上去", next: chase }
  - { id: stay, type: narration, text: "你留在原地。", next: END }
  - { id: chase, type: narration, text: "你追了上去。", next: END }
"""));
        var presenter = new DialogueUiPresenter(runtime);
        presenter.MoveSelection(1, DialogueUiInputSource.KeyboardMouse);

        var highlight = presenter.ConfirmSelection(DialogueUiInputSource.KeyboardMouse);

        Assert.Equal(DialogueUiMode.Choice, highlight.Mode);
        Assert.True(highlight.Options[1].IsConfirming);
        Assert.Equal("chase", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);
    }

    [Fact]
    public void KeyboardMouseAndGamepad_CanNavigateAndConfirmWithoutChangingTarget()
    {
        var keyboard = StartPresenter("""
id: keyboard_choice_ui
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "甲", next: a }
      - { text: "乙", next: b }
  - { id: a, type: narration, text: "甲。", next: END }
  - { id: b, type: narration, text: "乙。", next: END }
""");
        var gamepad = StartPresenter("""
id: gamepad_choice_ui
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "甲", next: a }
      - { text: "乙", next: b }
  - { id: a, type: narration, text: "甲。", next: END }
  - { id: b, type: narration, text: "乙。", next: END }
""");

        keyboard.MoveSelection(1, DialogueUiInputSource.KeyboardMouse);
        var keyboardHighlight = keyboard.ConfirmSelection(DialogueUiInputSource.KeyboardMouse);
        gamepad.MoveSelection(1, DialogueUiInputSource.Gamepad);
        var gamepadHighlight = gamepad.ConfirmSelection(DialogueUiInputSource.Gamepad);

        Assert.Equal(1, keyboardHighlight.SelectedOptionIndex);
        Assert.Equal(keyboardHighlight.SelectedOptionIndex, gamepadHighlight.SelectedOptionIndex);
        Assert.Equal(keyboardHighlight.Options[1].Text, gamepadHighlight.Options[1].Text);
    }

    private DialogueUiPresenter StartPresenter(string yaml)
    {
        var runtime = new DialogueRuntime();
        runtime.Start(Load(yaml));
        return new DialogueUiPresenter(runtime);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/choice-ui.yaml");
    }

    private sealed class FixedInsightProvider : IDialogueInsightValueProvider
    {
        private readonly int _insight;

        public FixedInsightProvider(int insight)
        {
            _insight = insight;
        }

        public int GetPlayerInsight()
        {
            return _insight;
        }
    }

    private sealed class ChoiceValueProvider : IDialogueConditionValueProvider
    {
        private readonly Dictionary<string, string> _values;

        public ChoiceValueProvider(params (string Source, string Value)[] values)
        {
            _values = values.ToDictionary(value => value.Source, value => value.Value, StringComparer.Ordinal);
        }

        public bool TryGetValue(DialogueConditionSpec condition, out string? value, out string? error)
        {
            if (_values.TryGetValue(condition.Source, out value))
            {
                error = null;
                return true;
            }

            value = null;
            error = $"未配置条件来源: {condition.Source}";
            return false;
        }
    }
}
