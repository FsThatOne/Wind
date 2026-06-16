using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueUiPresenterTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void SpeechNode_ShowsSpeakerPortraitNameplateAndText()
    {
        var presenter = StartPresenter("""
id: speech_ui
version: 1
entry_node: intro
nodes:
  - { id: intro, type: speech, speaker: master_li, text: "风过竹林。", next: END }
""", charactersPerTick: 100);

        var snapshot = presenter.Tick();

        Assert.Equal(DialogueUiMode.Speech, snapshot.Mode);
        Assert.Equal(DialogueUiFocusTarget.DialoguePanel, snapshot.FocusTarget);
        Assert.Equal("master_li", snapshot.SpeakerId);
        Assert.Equal("master_li", snapshot.NameplateText);
        Assert.True(snapshot.ShowPortrait);
        Assert.True(snapshot.HighlightCurrentSpeaker);
        Assert.Equal("风过竹林。", snapshot.VisibleText);
    }

    [Fact]
    public void Typewriter_HidesChoicePanelUntilTextIsComplete()
    {
        var presenter = StartPresenter("""
id: typewriter_ui
version: 1
entry_node: intro
nodes:
  - { id: intro, type: speech, text: "尚未说完的话。", next: choose }
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - { text: "继续。", next: END }
""", charactersPerTick: 1);

        var typing = presenter.Tick();

        Assert.True(typing.IsTypewriterActive);
        Assert.False(typing.ShowChoicePanel);

        var completed = presenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.KeyboardMouse);

        Assert.False(completed.IsTypewriterActive);
        Assert.False(completed.ShowChoicePanel);
        Assert.True(completed.ShowContinueIndicator);

        var choice = presenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.KeyboardMouse);

        Assert.Equal(DialogueUiMode.Choice, choice.Mode);
        Assert.Equal(DialogueUiFocusTarget.ChoicePanel, choice.FocusTarget);
        Assert.True(choice.ShowChoicePanel);
    }

    [Fact]
    public void Confirm_FirstCompletesText_SecondAdvancesNode()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 1 };
        runtime.Start(Load("""
id: confirm_ui
version: 1
entry_node: first
nodes:
  - { id: first, type: speech, text: "第一句话。", next: second }
  - { id: second, type: narration, text: "第二句话。", next: END }
"""));
        var presenter = new DialogueUiPresenter(runtime);

        var firstConfirm = presenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.Gamepad);

        Assert.Equal("first", firstConfirm.NodeId);
        Assert.Equal("第一句话。", firstConfirm.VisibleText);
        Assert.True(firstConfirm.ShowContinueIndicator);

        var secondConfirm = presenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.Gamepad);

        Assert.Equal("second", secondConfirm.NodeId);
        Assert.Equal(DialogueUiMode.Narration, secondConfirm.Mode);
    }

    [Fact]
    public void InnerMonologue_UsesIndependentStyleWithoutNpcPortrait()
    {
        var presenter = StartPresenter("""
id: inner_ui
version: 1
entry_node: thought
nodes:
  - { id: thought, type: inner_monologue, text: "此事另有蹊跷。", next: END }
""", charactersPerTick: 100);

        var snapshot = presenter.Tick();

        Assert.Equal(DialogueUiMode.InnerMonologue, snapshot.Mode);
        Assert.Equal("内心", snapshot.NameplateText);
        Assert.False(snapshot.ShowPortrait);
        Assert.False(snapshot.HighlightCurrentSpeaker);
    }

    [Fact]
    public void Narration_HasNoNameplateOrPortrait()
    {
        var presenter = StartPresenter("""
id: narration_ui
version: 1
entry_node: scene
nodes:
  - { id: scene, type: narration, text: "夜雨敲窗。", next: END }
""", charactersPerTick: 100);

        var snapshot = presenter.Tick();

        Assert.Equal(DialogueUiMode.Narration, snapshot.Mode);
        Assert.Null(snapshot.NameplateText);
        Assert.Null(snapshot.SpeakerId);
        Assert.False(snapshot.ShowPortrait);
    }

    [Fact]
    public void LetterNode_OpensOverlayAndCloseReturnsToDialogueFlow()
    {
        var presenter = StartPresenter("""
id: letter_ui
version: 1
entry_node: letter_01
nodes:
  - id: letter_01
    type: letter
    sender: bai_ling
    recipient: player
    text: "见字如面。"
    next: after
  - { id: after, type: narration, text: "你收起书信。", next: END }
""", charactersPerTick: 100);

        var opened = presenter.GetSnapshot();

        Assert.Equal(DialogueUiMode.Letter, opened.Mode);
        Assert.Equal(DialogueUiFocusTarget.LetterPanel, opened.FocusTarget);
        Assert.True(opened.ShowLetterOverlay);
        Assert.Equal("bai_ling", opened.LetterSender);
        Assert.Equal("player", opened.LetterRecipient);

        var closed = presenter.HandleInput(DialogueUiInputIntent.CloseLetter, DialogueUiInputSource.KeyboardMouse);

        Assert.Equal("after", closed.NodeId);
        Assert.Equal(DialogueUiMode.Narration, closed.Mode);
        Assert.False(closed.ShowLetterOverlay);
    }

    [Fact]
    public void KeyboardMouseAndGamepadInputs_ShareConfirmAndLetterCloseBehavior()
    {
        var keyboardPresenter = StartPresenter("""
id: keyboard_flow
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "一。", next: b }
  - { id: b, type: speech, text: "二。", next: END }
""", charactersPerTick: 1);
        var gamepadPresenter = StartPresenter("""
id: gamepad_flow
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "一。", next: b }
  - { id: b, type: speech, text: "二。", next: END }
""", charactersPerTick: 1);

        keyboardPresenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.KeyboardMouse);
        var keyboardAdvance = keyboardPresenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.KeyboardMouse);

        gamepadPresenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.Gamepad);
        var gamepadAdvance = gamepadPresenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.Gamepad);

        Assert.Equal("b", keyboardAdvance.NodeId);
        Assert.Equal(keyboardAdvance.NodeId, gamepadAdvance.NodeId);
        Assert.Equal(keyboardAdvance.FocusTarget, gamepadAdvance.FocusTarget);
    }

    private DialogueUiPresenter StartPresenter(string yaml, int charactersPerTick)
    {
        var runtime = new DialogueRuntime { CharactersPerTick = charactersPerTick };
        runtime.Start(Load(yaml));
        return new DialogueUiPresenter(runtime);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/dialogue-ui.yaml");
    }
}
