using FengZhi.Foundation.Data;
using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueSchemaTests
{
    private readonly DialogueConfigLoader _loader = new();

    private const string ValidAllNodeTypesYaml = """
id: meeting_master_001
version: 3
entry_node: node_01
nodes:
  - id: node_01
    type: speech
    speaker: master_li
    text: "你来得正好。"
    next: node_02
    events:
      - { type: set_flag, key: "met_master", value: "true" }
  - id: node_02
    type: choice
    prompt: "师父欲言又止。"
    options:
      - text: "师父请讲。"
        next: node_03
        events:
          - { type: mindset_shift, axis: kindness, delta: 1 }
      - text: "（沉默等待）"
        next: node_04
        conditions:
          - { source: mindset.firmness, op: gte, value: 30 }
  - id: node_03
    type: inner_monologue
    text: "师父眼中有忧色。"
    next: node_05
  - id: node_04
    type: narration
    text: "山风掠过松梢。"
    next: node_05
  - id: node_05
    type: letter
    sender: bai_ling
    recipient: player
    text: "见字如面。"
    next: node_06
  - id: node_06
    type: insight_prompt
    text: "他话里藏着另一层意思。"
    insight_level_required: 15
    next: node_07
  - id: node_07
    type: code_phrase
    correct_phrase: "风过无痕"
    response: "故人仍在。"
    next: END
""";

    [Fact]
    public void LoadSequence_ValidYamlWithSevenNodeTypes_PreservesFields()
    {
        var sequence = _loader.LoadSequence(ValidAllNodeTypesYaml, "assets/data/dialogues/ch01/meeting-master.yaml");

        Assert.Equal("meeting_master_001", sequence.Id);
        Assert.Equal(3, sequence.Version);
        Assert.Equal("node_01", sequence.EntryNode);
        Assert.Equal(7, sequence.Nodes.Count);
        Assert.Contains(sequence.Nodes, n => n.Type == "speech" && n.Speaker == "master_li");
        Assert.Contains(sequence.Nodes, n => n.Type == "inner_monologue");
        Assert.Contains(sequence.Nodes, n => n.Type == "narration");
        Assert.Contains(sequence.Nodes, n => n.Type == "letter" && n.Sender == "bai_ling");
        Assert.Contains(sequence.Nodes, n => n.Type == "insight_prompt" && n.InsightLevelRequired == 15);
        Assert.Contains(sequence.Nodes, n => n.Type == "code_phrase" && n.CorrectPhrase == "风过无痕");

        var choice = sequence.Nodes.Single(n => n.Type == "choice");
        Assert.Equal(2, choice.Options.Count);
        Assert.Equal("师父请讲。", choice.Options[0].Text);
        Assert.Equal("node_03", choice.Options[0].Next);
        Assert.Equal("mindset_shift", choice.Options[0].Events[0].Type);
        Assert.Equal("mindset.firmness", choice.Options[1].Conditions[0].Source);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("version")]
    [InlineData("entry_node")]
    [InlineData("nodes")]
    public void LoadSequence_MissingRequiredHeaderField_FailsFastWithFileAndField(string missingField)
    {
        var yaml = missingField switch
        {
            "id" => """
version: 1
entry_node: start
nodes:
  - { id: start, type: narration, text: "起。", next: END }
""",
            "version" => """
id: missing_version
entry_node: start
nodes:
  - { id: start, type: narration, text: "起。", next: END }
""",
            "entry_node" => """
id: missing_entry
version: 1
nodes:
  - { id: start, type: narration, text: "起。", next: END }
""",
            _ => """
id: missing_nodes
version: 1
entry_node: start
"""
        };

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadSequence(yaml, "assets/data/dialogues/ch01/broken.yaml"));

        Assert.Contains("assets/data/dialogues/ch01/broken.yaml", ex.Message);
        Assert.Contains(missingField, ex.Message);
    }

    [Fact]
    public void Validate_DuplicateInvalidReferenceAndOrphanNodes_ReportsEachProblem()
    {
        var sequence = new DialogueSequence
        {
            Id = "broken_graph",
            Version = 1,
            EntryNode = "start",
            Nodes =
            {
                new DialogueNode { Id = "start", Type = "speech", Text = "起。", Next = "missing" },
                new DialogueNode { Id = "start", Type = "narration", Text = "重复。", Next = "END" },
                new DialogueNode { Id = "orphan", Type = "narration", Text = "无人抵达。", Next = "END" }
            }
        };

        var errors = DialogueGraphValidator.Validate(sequence);

        Assert.Contains(errors, e => e.Contains("重复", StringComparison.Ordinal) && e.Contains("start", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("next", StringComparison.Ordinal) && e.Contains("missing", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("孤立节点", StringComparison.Ordinal) && e.Contains("orphan", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ChoiceWithoutDefaultOption_ReportsMissingDefaultOption()
    {
        var yaml = """
id: no_default_choice
version: 1
entry_node: start
nodes:
  - id: start
    type: choice
    prompt: "如何回应？"
    options:
      - text: "强硬"
        next: END
        conditions:
          - { source: mindset.firmness, op: gte, value: 30 }
      - text: "退让"
        next: END
        conditions_any_of:
          - { source: flag, key: has_promised }
""";

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadSequence(yaml, "assets/data/dialogues/ch01/no-default.yaml"));

        Assert.Contains("选择节点无默认选项", ex.Message);
        Assert.Contains("start", ex.Message);
    }

    [Fact]
    public void Validate_CyclicGraph_DoesNotTreatCycleAsInvalid()
    {
        var yaml = """
id: cyclic_dialogue
version: 1
entry_node: a
nodes:
  - id: a
    type: narration
    text: "甲。"
    next: b
  - id: b
    type: narration
    text: "乙。"
    next: a
""";

        var sequence = _loader.LoadSequence(yaml, "assets/data/dialogues/ch01/cycle.yaml");

        Assert.Equal(2, sequence.Nodes.Count);
        Assert.Equal(DialogueConfigLoader.MaxNodeVisitsPerRun, 500);
    }

    [Fact]
    public void Validate_EndReference_IsLegalTarget()
    {
        var yaml = """
id: end_reference
version: 1
entry_node: start
nodes:
  - id: start
    type: narration
    text: "终。"
    next: END
""";

        var sequence = _loader.LoadSequence(yaml, "assets/data/dialogues/ch01/end.yaml");

        Assert.Single(sequence.Nodes);
        Assert.Equal("END", sequence.Nodes[0].Next);
    }
}
