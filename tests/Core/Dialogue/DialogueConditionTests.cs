using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueConditionTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void ChoiceNode_ReturnsOnlySatisfiedOptionsInAuthoringOrder()
    {
        var evaluator = CreateEvaluator(new Dictionary<string, string>
        {
            ["mindset.firmness"] = "35",
            ["flag.has_letter"] = "true"
        });
        var node = LoadChoice("""
id: option_filter
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "退让"
        next: END
        conditions:
          - { source: mindset.firmness, op: lt, value: 20 }
      - text: "直问"
        next: END
        conditions:
          - { source: mindset.firmness, op: gte, value: 30 }
      - text: "递信"
        next: END
        conditions:
          - { source: flag.has_letter, op: ==, value: true }
      - text: "默认"
        next: END
""");

        var visible = evaluator.GetVisibleOptions(node);

        Assert.Equal(new[] { "直问", "递信", "默认" }, visible.Select(o => o.Option.Text));
        Assert.Equal(new[] { 1, 2, 3 }, visible.Select(o => o.Index));
    }

    [Theory]
    [InlineData("10", "==", "10", true)]
    [InlineData("10", "!=", "20", true)]
    [InlineData("30", ">", "20", true)]
    [InlineData("20", ">=", "20", true)]
    [InlineData("10", "<", "20", true)]
    [InlineData("20", "<=", "20", true)]
    [InlineData("10", ">", "20", false)]
    public void Evaluate_ComparisonOperators_ReturnExpectedResult(string actual, string op, string expected, bool expectedResult)
    {
        var evaluator = CreateEvaluator(new Dictionary<string, string> { ["source"] = actual });
        var result = evaluator.Evaluate(
            new[] { new DialogueConditionSpec { Source = "source", Op = op, Value = expected } },
            Array.Empty<DialogueConditionSpec>());

        Assert.Equal(expectedResult, result.IsMet);
    }

    [Fact]
    public void Evaluate_AndAndOrConditions_CombineCorrectly()
    {
        var evaluator = CreateEvaluator(new Dictionary<string, string>
        {
            ["power"] = "25",
            ["route_a"] = "false",
            ["route_b"] = "true"
        });

        var result = evaluator.Evaluate(
            new[] { new DialogueConditionSpec { Source = "power", Op = ">=", Value = "20" } },
            new[]
            {
                new DialogueConditionSpec { Source = "route_a", Op = "==", Value = "true" },
                new DialogueConditionSpec { Source = "route_b", Op = "==", Value = "true" }
            });

        Assert.True(result.IsMet);
    }

    [Fact]
    public void ChoiceNode_WhenOnlyDefaultMatches_ShowsDefaultOption()
    {
        var evaluator = CreateEvaluator(new Dictionary<string, string> { ["power"] = "1" });
        var node = LoadChoice("""
id: default_choice
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "强攻"
        next: END
        conditions:
          - { source: power, op: gte, value: 30 }
      - text: "……"
        next: END
""");

        var visible = evaluator.GetVisibleOptions(node);

        Assert.Single(visible);
        Assert.Equal("……", visible[0].Option.Text);
        Assert.False(visible[0].IsFallback);
    }

    [Fact]
    public void ChoiceNode_WhenNoOptionsMatchAndNoDefault_AddsDiagnosticFallback()
    {
        var evaluator = CreateEvaluator(new Dictionary<string, string> { ["power"] = "1" });
        var node = new DialogueNode
        {
            Id = "no_default",
            Type = "choice",
            Fallback = "silent"
        };
        node.Options.Add(new DialogueOption
        {
            Text = "强攻",
            Next = "fight",
            Conditions = { new DialogueConditionSpec { Source = "power", Op = ">=", Value = "30" } }
        });

        var visible = evaluator.GetVisibleOptions(node);

        Assert.Single(visible);
        Assert.Equal(DialogueConditionEvaluator.FallbackOptionText, visible[0].Option.Text);
        Assert.Equal("silent", visible[0].Option.Next);
        Assert.True(visible[0].IsFallback);
        Assert.Contains(evaluator.Errors, e => e.Contains("无可见选项", StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_ChoiceNodeUsesVisibleOptionsWithoutGrayEntries()
    {
        var runtime = new DialogueRuntime(CreateEvaluator(new Dictionary<string, string> { ["power"] = "40" }));
        var sequence = Load("""
id: runtime_choice
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    options:
      - text: "退让"
        next: END
        conditions:
          - { source: power, op: lt, value: 10 }
      - text: "出剑"
        next: END
        conditions:
          - { source: power, op: gte, value: 30 }
      - text: "沉默"
        next: END
""");

        runtime.Start(sequence);

        Assert.Equal(DialogueRuntimeState.ProcessingChoice, runtime.State);
        Assert.Equal(new[] { "出剑", "沉默" }, runtime.VisibleOptions.Select(o => o.Option.Text));
        Assert.DoesNotContain(runtime.VisibleOptions, o => o.Option.Text == "退让");
    }

    [Fact]
    public void Runtime_NodeEntryConditionFails_UsesFallbackBeforeDisplay()
    {
        var runtime = new DialogueRuntime(CreateEvaluator(new Dictionary<string, string> { ["flag.allowed"] = "false" }))
        {
            CharactersPerTick = 100
        };
        var sequence = Load("""
id: node_fallback
version: 1
entry_node: gated
nodes:
  - id: gated
    type: narration
    text: "不可见。"
    fallback: fallback_node
    conditions:
      - { source: flag.allowed, op: ==, value: true }
  - id: fallback_node
    type: narration
    text: "他没有开口。"
    next: END
""");

        runtime.Start(sequence);

        Assert.Equal("fallback_node", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);
    }

    [Fact]
    public void ConditionQueryFailure_ReturnsDiagnosticError()
    {
        var evaluator = new DialogueConditionEvaluator(new MissingValueProvider());

        var result = evaluator.Evaluate(
            new[] { new DialogueConditionSpec { Source = "unknown.flag", Op = "==", Value = "true" } },
            Array.Empty<DialogueConditionSpec>());

        Assert.False(result.IsMet);
        Assert.Contains("unknown.flag", result.Error);
    }

    private DialogueNode LoadChoice(string yaml)
    {
        return Load(yaml).Nodes.Single(n => n.Type == "choice");
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/conditions.yaml");
    }

    private static DialogueConditionEvaluator CreateEvaluator(IReadOnlyDictionary<string, string> values)
    {
        return new DialogueConditionEvaluator(new DictionaryValueProvider(values));
    }

    private sealed class DictionaryValueProvider : IDialogueConditionValueProvider
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public DictionaryValueProvider(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public bool TryGetValue(DialogueConditionSpec condition, out string? value, out string? error)
        {
            var key = condition.Source;
            if (condition.Key != null)
                key = $"{key}.{condition.Key}";
            if (condition.Id != null)
                key = $"{key}.{condition.Id}";

            if (_values.TryGetValue(key, out value) || _values.TryGetValue(condition.Source, out value))
            {
                error = null;
                return true;
            }

            error = $"missing {condition.Source}";
            value = null;
            return false;
        }
    }

    private sealed class MissingValueProvider : IDialogueConditionValueProvider
    {
        public bool TryGetValue(DialogueConditionSpec condition, out string? value, out string? error)
        {
            value = null;
            error = $"missing {condition.Source}";
            return false;
        }
    }
}
