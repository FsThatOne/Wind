using FengZhi.Foundation.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.Dialogue;

/// <summary>
/// 对话 YAML 图结构的加载、索引和校验入口。
/// </summary>
public sealed class DialogueConfigLoader
{
    /// <summary>运行时单次对话最多访问的节点数，用于防御死循环。</summary>
    public const int MaxNodeVisitsPerRun = 500;

    private const string EndNodeId = "END";
    private static readonly HashSet<string> ValidNodeTypes = new(StringComparer.Ordinal)
    {
        "speech",
        "choice",
        "inner_monologue",
        "narration",
        "letter",
        "insight_prompt",
        "code_phrase"
    };

    private readonly IDeserializer _deserializer;

    /// <summary>创建 YAML 1.2 风格的对话配置加载器。</summary>
    public DialogueConfigLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 从 YAML 字符串加载并校验单个对话序列。
    /// </summary>
    public DialogueSequence LoadSequence(string yaml, string filePath)
    {
        DialogueSequence? sequence;
        try
        {
            sequence = _deserializer.Deserialize<DialogueSequence>(yaml);
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, ex.Message, ex);
        }

        if (sequence == null)
            throw new DataLoadException(filePath, null, "YAML 反序列化结果为 null");

        var errors = DialogueGraphValidator.Validate(sequence);
        if (errors.Count > 0)
            throw new DataLoadException(filePath, null, string.Join("; ", errors));

        return sequence;
    }

    internal static bool IsEndReference(string? nodeId)
    {
        return string.Equals(nodeId, EndNodeId, StringComparison.Ordinal);
    }

    internal static bool IsKnownNodeType(string? type)
    {
        return !string.IsNullOrWhiteSpace(type) && ValidNodeTypes.Contains(type);
    }
}

/// <summary>
/// 一个完整的对话序列，来自 `assets/data/dialogues/{chapter}/` 下的 YAML 文件。
/// </summary>
public sealed class DialogueSequence
{
    /// <summary>对话序列唯一 ID。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>内容版本号，用于后续存档兼容。</summary>
    public int Version { get; set; }

    /// <summary>进入对话时首先访问的节点 ID。</summary>
    public string EntryNode { get; set; } = string.Empty;

    /// <summary>对话图中的全部节点。</summary>
    public List<DialogueNode> Nodes { get; set; } = new();
}

/// <summary>
/// 对话图节点，支持 speech / choice / inner_monologue / narration / letter / insight_prompt / code_phrase。
/// </summary>
public sealed class DialogueNode
{
    /// <summary>节点唯一 ID。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>节点类型。</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>说话者 ID，speech 节点使用。</summary>
    public string? Speaker { get; set; }

    /// <summary>节点正文。</summary>
    public string? Text { get; set; }

    /// <summary>choice 节点的提示文字。</summary>
    public string? Prompt { get; set; }

    /// <summary>letter 节点的寄信者。</summary>
    public string? Sender { get; set; }

    /// <summary>letter 节点的收信者。</summary>
    public string? Recipient { get; set; }

    /// <summary>letter 节点包含的暗号 ID；首次查看时学习。</summary>
    public List<string> CodePhraseIds { get; set; } = new();

    /// <summary>letter 节点包含的情感线索标签。</summary>
    public List<string> EmotionalClues { get; set; } = new();

    /// <summary>洞察门槛。speech / narration 节点达到门槛后可追查；insight_prompt 可记录建议门槛。</summary>
    public int? InsightLevelRequired { get; set; }

    /// <summary>洞察追查成功后的跳转目标，通常指向 insight_prompt 节点。</summary>
    public string? InsightNext { get; set; }

    /// <summary>code_phrase 节点的正确暗号。</summary>
    public string? CorrectPhrase { get; set; }

    /// <summary>code_phrase 节点的回应文本。</summary>
    public string? Response { get; set; }

    /// <summary>普通跳转目标；`END` 表示对话结束。</summary>
    public string? Next { get; set; }

    /// <summary>入口条件不满足时的兜底跳转目标。</summary>
    public string? Fallback { get; set; }

    /// <summary>choice 节点的选项列表。</summary>
    public List<DialogueOption> Options { get; set; } = new();

    /// <summary>choice 节点可能由暗号选项进入的隐藏分支，用于静态引用校验与孤立节点检测。</summary>
    public List<string> CodePhraseNexts { get; set; } = new();

    /// <summary>节点入口条件，默认 AND。</summary>
    public List<DialogueConditionSpec> Conditions { get; set; } = new();

    /// <summary>节点入口条件，OR 语义。</summary>
    public List<DialogueConditionSpec> ConditionsAnyOf { get; set; } = new();

    /// <summary>节点完成时排队的事件。</summary>
    public List<DialogueEventSpec> Events { get; set; } = new();
}

/// <summary>
/// 对话选项，可携带条件、事件和跳转目标。
/// </summary>
public sealed class DialogueOption
{
    /// <summary>玩家看到的选项文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>选择后的跳转目标；`END` 表示对话结束。</summary>
    public string? Next { get; set; }

    /// <summary>选项可见条件，默认 AND。</summary>
    public List<DialogueConditionSpec> Conditions { get; set; } = new();

    /// <summary>选项可见条件，OR 语义。</summary>
    public List<DialogueConditionSpec> ConditionsAnyOf { get; set; } = new();

    /// <summary>选择后排队的事件。</summary>
    public List<DialogueEventSpec> Events { get; set; } = new();
}

/// <summary>
/// 对话条件三元组，`Id` / `Key` 用于部分 source 的附加参数。
/// </summary>
public sealed class DialogueConditionSpec
{
    /// <summary>条件来源，如 mindset.firmness / flag / item.has。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>比较操作符，如 gte / lt / eq。</summary>
    public string? Op { get; set; }

    /// <summary>比较值，保持为字符串以兼容数值与文本型条件。</summary>
    public string? Value { get; set; }

    /// <summary>物品、洞察、暗号等条件的 ID 参数。</summary>
    public string? Id { get; set; }

    /// <summary>flag 等条件的 key 参数。</summary>
    public string? Key { get; set; }
}

/// <summary>
/// 对话事件规格，具体解释由下游系统负责。
/// </summary>
public sealed class DialogueEventSpec
{
    /// <summary>事件类型，如 mindset_shift / set_flag / combat_trigger。</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>心境或数值事件的轴。</summary>
    public string? Axis { get; set; }

    /// <summary>数值变化量。</summary>
    public int? Delta { get; set; }

    /// <summary>flag 或状态事件的 key。</summary>
    public string? Key { get; set; }

    /// <summary>事件携带的字符串值。</summary>
    public string? Value { get; set; }
}

/// <summary>
/// 对话图结构校验工具，负责捕获构建期 schema 与引用错误。
/// </summary>
public static class DialogueGraphValidator
{
    /// <summary>
    /// 校验对话序列并返回全部错误；循环图本身不视为非法。
    /// </summary>
    public static IReadOnlyList<string> Validate(DialogueSequence sequence)
    {
        var errors = new List<string>();

        Require(sequence.Id, "id", errors);
        if (sequence.Version <= 0)
            errors.Add("缺少必需字段 'version'");
        Require(sequence.EntryNode, "entry_node", errors);
        if (sequence.Nodes == null || sequence.Nodes.Count == 0)
            errors.Add("缺少必需字段 'nodes'");

        if (errors.Count > 0)
            return errors;

        var nodes = sequence.Nodes!;
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicateIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add("节点缺少必需字段 'id'");
                continue;
            }

            if (!nodeIds.Add(node.Id))
                duplicateIds.Add(node.Id);

            if (!DialogueConfigLoader.IsKnownNodeType(node.Type))
                errors.Add($"节点 '{node.Id}' 使用未知 type: '{node.Type}'");
        }

        foreach (var id in duplicateIds)
            errors.Add($"节点 id 重复: '{id}'");

        if (!nodeIds.Contains(sequence.EntryNode))
            errors.Add($"entry_node 引用不存在: '{sequence.EntryNode}'");

        foreach (var node in nodes)
        {
            ValidateReference(node.Next, node.Id, "next", nodeIds, errors);
            ValidateReference(node.Fallback, node.Id, "fallback", nodeIds, errors);
            ValidateReference(node.InsightNext, node.Id, "insight_next", nodeIds, errors);
            foreach (var codePhraseNext in node.CodePhraseNexts)
                ValidateReference(codePhraseNext, node.Id, "code_phrase_nexts", nodeIds, errors);

            ValidateInsightBranch(node, nodes, nodeIds, errors);

            if (string.Equals(node.Type, "choice", StringComparison.Ordinal))
                ValidateChoiceNode(node, nodeIds, errors);
        }

        AddOrphanNodeErrors(sequence.EntryNode, nodes, nodeIds, errors);
        return errors;
    }

    private static void ValidateChoiceNode(
        DialogueNode node,
        HashSet<string> nodeIds,
        List<string> errors)
    {
        if (node.Options.Count == 0)
        {
            errors.Add($"选择节点 '{node.Id}' 缺少 options");
            return;
        }

        var hasDefaultOption = false;
        foreach (var option in node.Options)
        {
            if (option.Conditions.Count == 0 && option.ConditionsAnyOf.Count == 0)
                hasDefaultOption = true;

            ValidateReference(option.Next, node.Id, "option.next", nodeIds, errors);
        }

        if (!hasDefaultOption)
            errors.Add($"选择节点无默认选项: '{node.Id}'");
    }

    private static void AddOrphanNodeErrors(
        string entryNode,
        List<DialogueNode> nodes,
        HashSet<string> nodeIds,
        List<string> errors)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        stack.Push(entryNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!reachable.Add(current))
                continue;

            var node = nodes.FirstOrDefault(n => n.Id == current);
            if (node == null)
                continue;

            PushReference(node.InsightNext, nodeIds, stack);
            PushReference(node.Next, nodeIds, stack);
            PushReference(node.Fallback, nodeIds, stack);
            foreach (var codePhraseNext in node.CodePhraseNexts)
                PushReference(codePhraseNext, nodeIds, stack);
            foreach (var option in node.Options)
                PushReference(option.Next, nodeIds, stack);
        }

        foreach (var id in nodeIds)
        {
            if (!reachable.Contains(id))
                errors.Add($"孤立节点: '{id}'");
        }
    }

    private static void PushReference(string? target, HashSet<string> nodeIds, Stack<string> stack)
    {
        if (string.IsNullOrWhiteSpace(target) || DialogueConfigLoader.IsEndReference(target))
            return;
        if (nodeIds.Contains(target))
            stack.Push(target);
    }

    private static void ValidateInsightBranch(
        DialogueNode node,
        List<DialogueNode> nodes,
        HashSet<string> nodeIds,
        List<string> errors)
    {
        if (!node.InsightLevelRequired.HasValue)
            return;

        if (!IsInsightCueSource(node))
            return;

        if (string.IsNullOrWhiteSpace(node.InsightNext))
        {
            errors.Add($"洞察节点 '{node.Id}' 缺少 insight_next");
            return;
        }

        if (!nodeIds.Contains(node.InsightNext))
            return;

        var target = nodes.First(n => string.Equals(n.Id, node.InsightNext, StringComparison.Ordinal));
        if (!string.Equals(target.Type, "insight_prompt", StringComparison.Ordinal))
            errors.Add($"洞察节点 '{node.Id}' 的 insight_next 必须指向 insight_prompt 节点: '{node.InsightNext}'");
    }

    private static bool IsInsightCueSource(DialogueNode node)
    {
        return string.Equals(node.Type, "speech", StringComparison.Ordinal) ||
               string.Equals(node.Type, "narration", StringComparison.Ordinal);
    }

    private static void ValidateReference(
        string? target,
        string nodeId,
        string field,
        HashSet<string> nodeIds,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(target) || DialogueConfigLoader.IsEndReference(target))
            return;
        if (!nodeIds.Contains(target))
            errors.Add($"节点 '{nodeId}' 的 {field} 引用不存在: '{target}'");
    }

    private static void Require(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"缺少必需字段 '{fieldName}'");
    }
}
