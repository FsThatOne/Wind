using System.Globalization;

namespace FengZhi.Foundation.Dialogue;

/// <summary>
/// 对话条件查询接口。对话系统只读取 source 当前值，不持有下游系统实现。
/// </summary>
public interface IDialogueConditionValueProvider
{
    /// <summary>尝试读取条件来源当前值。</summary>
    bool TryGetValue(DialogueConditionSpec condition, out string? value, out string? error);
}

/// <summary>对话条件评估结果。</summary>
public sealed record DialogueConditionResult(bool IsMet, string? Error = null);

/// <summary>对话选项来源类型。</summary>
public enum DialogueOptionKind
{
    Standard,
    CodePhrase,
    Fallback
}

/// <summary>可渲染的对话选项。Index 保留 YAML 编辑顺序中的原始下标；额外注入选项使用负数。</summary>
public sealed record VisibleDialogueOption(
    int Index,
    DialogueOption Option,
    bool IsFallback,
    DialogueOptionKind Kind = DialogueOptionKind.Standard,
    string? CodePhraseId = null,
    string? CodePhraseContextKey = null);

/// <summary>
/// 条件评估与 Choice 选项过滤服务。
/// </summary>
public sealed class DialogueConditionEvaluator
{
    public const string FallbackOptionText = "……（无言以对）";

    private readonly IDialogueConditionValueProvider _valueProvider;
    private readonly List<string> _errors = new();

    public DialogueConditionEvaluator(IDialogueConditionValueProvider valueProvider)
    {
        _valueProvider = valueProvider;
    }

    /// <summary>最近一次评估期间记录的诊断错误。</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// 评估 AND 与 OR 条件组合：conditions 全部满足，conditions_any_of 至少一个满足。
    /// </summary>
    public DialogueConditionResult Evaluate(
        IReadOnlyList<DialogueConditionSpec> conditions,
        IReadOnlyList<DialogueConditionSpec> conditionsAnyOf)
    {
        _errors.Clear();

        foreach (var condition in conditions)
        {
            var result = EvaluateSingle(condition);
            if (!result.IsMet)
                return result;
        }

        if (conditionsAnyOf.Count == 0)
            return new DialogueConditionResult(true);

        var anyErrors = new List<string>();
        foreach (var condition in conditionsAnyOf)
        {
            var result = EvaluateSingle(condition);
            if (result.IsMet)
                return result;
            if (result.Error != null)
                anyErrors.Add(result.Error);
        }

        return new DialogueConditionResult(false, anyErrors.Count == 0 ? null : string.Join("; ", anyErrors));
    }

    /// <summary>评估节点入口条件。</summary>
    public DialogueConditionResult EvaluateNode(DialogueNode node)
    {
        return Evaluate(node.Conditions, node.ConditionsAnyOf);
    }

    /// <summary>
    /// 过滤 Choice 节点选项，保留满足条件的选项并维持编辑顺序。
    /// </summary>
    public IReadOnlyList<VisibleDialogueOption> GetVisibleOptions(DialogueNode choiceNode)
    {
        _errors.Clear();
        var visible = new List<VisibleDialogueOption>();

        for (var i = 0; i < choiceNode.Options.Count; i++)
        {
            var option = choiceNode.Options[i];
            var result = Evaluate(option.Conditions, option.ConditionsAnyOf);
            if (result.IsMet)
            {
                visible.Add(new VisibleDialogueOption(i, option, IsFallback: false));
            }
            else if (result.Error != null)
            {
                _errors.Add($"选项 '{option.Text}' 条件查询失败: {result.Error}");
            }
        }

        if (visible.Count > 0)
            return visible;

        var fallback = choiceNode.Options.FirstOrDefault(option =>
            option.Conditions.Count == 0 && option.ConditionsAnyOf.Count == 0);
        if (fallback != null)
            return new[] { new VisibleDialogueOption(choiceNode.Options.IndexOf(fallback), fallback, IsFallback: false) };

        _errors.Add($"选择节点 '{choiceNode.Id}' 无可见选项，使用运行时兜底选项。");
        return new[]
        {
            new VisibleDialogueOption(
                -1,
                new DialogueOption { Text = FallbackOptionText, Next = choiceNode.Fallback ?? choiceNode.Next },
                IsFallback: true,
                Kind: DialogueOptionKind.Fallback)
        };
    }

    private DialogueConditionResult EvaluateSingle(DialogueConditionSpec condition)
    {
        if (!_valueProvider.TryGetValue(condition, out var sourceValue, out var error))
        {
            var message = error ?? $"无法读取条件来源 '{condition.Source}'";
            _errors.Add(message);
            return new DialogueConditionResult(false, message);
        }

        var op = NormalizeOperator(condition.Op);
        var targetValue = condition.Value ?? "true";
        var isMet = Compare(sourceValue ?? string.Empty, op, targetValue);
        return new DialogueConditionResult(isMet);
    }

    private static string NormalizeOperator(string? op)
    {
        return op switch
        {
            null or "" => "==",
            "eq" => "==",
            "neq" or "ne" => "!=",
            "gt" => ">",
            "gte" => ">=",
            "lt" => "<",
            "lte" => "<=",
            _ => op
        };
    }

    private static bool Compare(string actual, string op, string expected)
    {
        if (decimal.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var actualNumber) &&
            decimal.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber))
        {
            return op switch
            {
                "==" => actualNumber == expectedNumber,
                "!=" => actualNumber != expectedNumber,
                ">" => actualNumber > expectedNumber,
                ">=" => actualNumber >= expectedNumber,
                "<" => actualNumber < expectedNumber,
                "<=" => actualNumber <= expectedNumber,
                _ => false
            };
        }

        var comparison = string.Compare(actual, expected, StringComparison.Ordinal);
        return op switch
        {
            "==" => comparison == 0,
            "!=" => comparison != 0,
            ">" => comparison > 0,
            ">=" => comparison >= 0,
            "<" => comparison < 0,
            "<=" => comparison <= 0,
            _ => false
        };
    }
}
