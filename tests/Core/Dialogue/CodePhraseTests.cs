using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class CodePhraseTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void ChoiceNode_WhenPhraseLearnedAndContextMatches_AppendsCodePhraseOption()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "白蘋渡口，芦花如雪。", "hidden")
            }),
            codePhraseBook: book);
        var sequence = Load("""
id: phrase_visible
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "寒暄"
        next: END
    code_phrase_nexts: [hidden]
  - { id: hidden, type: code_phrase, correct_phrase: white_reed, response: "故人果然还记得。", next: END }
""");

        runtime.Start(sequence);

        Assert.Equal(new[] { "寒暄", "白蘋渡口，芦花如雪。" }, runtime.VisibleOptions.Select(o => o.Option.Text));
        Assert.Equal(DialogueOptionKind.CodePhrase, runtime.VisibleOptions[1].Kind);
        Assert.Equal("white_reed", runtime.VisibleOptions[1].CodePhraseId);
    }

    [Fact]
    public void ChoiceNode_WhenPhraseNotLearned_DoesNotShowCodePhraseOption()
    {
        var book = new DialogueCodePhraseBook();
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "白蘋渡口，芦花如雪。", "hidden")
            }),
            codePhraseBook: book);
        var sequence = Load("""
id: phrase_hidden
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "寒暄"
        next: END
    code_phrase_nexts: [hidden]
  - { id: hidden, type: code_phrase, correct_phrase: white_reed, response: "故人果然还记得。", next: END }
""");

        runtime.Start(sequence);

        Assert.Single(runtime.VisibleOptions);
        Assert.DoesNotContain(runtime.VisibleOptions, option => option.Kind == DialogueOptionKind.CodePhrase);
    }

    [Fact]
    public void ChoiceNode_WhenMultiplePhrasesMatch_AppendsAllInProviderOrder()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        book.Learn("night_bell");
        book.Learn("wrong_scene");
        book.MarkUsed("wrong_scene", "npc_other@pier");
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "白蘋渡口，芦花如雪。", "hidden_a"),
                new DialogueCodePhraseMatch("night_bell", "npc_bai@inn", "三更无鼓。", "hidden_b"),
                new DialogueCodePhraseMatch("wrong_scene", "npc_other@pier", "不该出现。", "hidden_c")
            }),
            codePhraseBook: book);
        var sequence = Load("""
id: phrase_multiple
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "寒暄"
        next: END
    code_phrase_nexts: [hidden_a, hidden_b, hidden_c]
  - { id: hidden_a, type: code_phrase, correct_phrase: white_reed, response: "A", next: END }
  - { id: hidden_b, type: code_phrase, correct_phrase: night_bell, response: "B", next: END }
  - { id: hidden_c, type: code_phrase, correct_phrase: wrong_scene, response: "C", next: END }
""");

        runtime.Start(sequence);

        Assert.Equal(new[] { "寒暄", "白蘋渡口，芦花如雪。", "三更无鼓。" }, runtime.VisibleOptions.Select(o => o.Option.Text));
        Assert.Equal(new[] { DialogueOptionKind.Standard, DialogueOptionKind.CodePhrase, DialogueOptionKind.CodePhrase }, runtime.VisibleOptions.Select(o => o.Kind));
    }

    [Fact]
    public void ChoiceNode_WhenCodePhraseTextMatchesStandardOption_KeepsKindMarker()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "寒暄", "hidden")
            }),
            codePhraseBook: book);
        var sequence = Load("""
id: phrase_same_text
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "寒暄"
        next: END
    code_phrase_nexts: [hidden]
  - { id: hidden, type: code_phrase, correct_phrase: white_reed, response: "暗号回应。", next: END }
""");

        runtime.Start(sequence);

        Assert.Equal(new[] { "寒暄", "寒暄" }, runtime.VisibleOptions.Select(o => o.Option.Text));
        Assert.Equal(DialogueOptionKind.Standard, runtime.VisibleOptions[0].Kind);
        Assert.Equal(DialogueOptionKind.CodePhrase, runtime.VisibleOptions[1].Kind);
    }

    [Fact]
    public void SelectCodePhraseOption_MarksOnlyCurrentContextUsedAndEntersHiddenBranch()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        var runtime = new DialogueRuntime(
            codePhraseProvider: new DialogueCodePhraseProvider(book, new[]
            {
                new DialogueCodePhraseMatch("white_reed", "npc_bai@inn", "白蘋渡口，芦花如雪。", "hidden")
            }),
            codePhraseBook: book);
        var sequence = Load("""
id: phrase_use
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "寒暄"
        next: END
    code_phrase_nexts: [hidden]
  - { id: hidden, type: code_phrase, correct_phrase: white_reed, response: "故人果然还记得。", next: END }
""");

        runtime.Start(sequence);
        runtime.SelectOption(1);

        Assert.Equal("hidden", runtime.CurrentNode?.Id);
        Assert.True(book.IsUsed("white_reed", "npc_bai@inn"));
        Assert.False(book.IsUsed("white_reed", "npc_bai@another_scene"));
    }

    [Fact]
    public void CodePhraseBook_RoundTripsSerializableState()
    {
        var book = new DialogueCodePhraseBook();
        book.Learn("white_reed");
        book.Learn("night_bell");
        book.MarkUsed("white_reed", "npc_bai@inn");

        var restored = DialogueCodePhraseBook.FromState(book.ToState());

        Assert.True(restored.HasLearned("white_reed"));
        Assert.True(restored.HasLearned("night_bell"));
        Assert.True(restored.IsUsed("white_reed", "npc_bai@inn"));
        Assert.False(restored.IsUsed("night_bell", "npc_bai@inn"));
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/code-phrases.yaml");
    }
}
