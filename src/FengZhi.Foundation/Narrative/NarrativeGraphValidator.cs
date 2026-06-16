namespace FengZhi.Foundation.Narrative;

public sealed record NarrativeGraphValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public static class NarrativeGraphValidator
{
    public static NarrativeGraphValidationResult Validate(NarrativeGraph graph)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(graph.Id))
            errors.Add("主线图缺少 id。");
        if (string.IsNullOrWhiteSpace(graph.EntryNode))
            errors.Add("主线图缺少 entry_node。");

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add("存在空节点 id。");
                continue;
            }

            if (!nodeIds.Add(node.Id))
                errors.Add($"重复节点 id: '{node.Id}'。");
        }

        if (!string.IsNullOrWhiteSpace(graph.EntryNode) && !nodeIds.Contains(graph.EntryNode))
            errors.Add($"entry_node '{graph.EntryNode}' 不存在。");

        foreach (var node in graph.Nodes)
            ValidateNodeReferences(node, nodeIds, errors);

        return new NarrativeGraphValidationResult(errors.Count == 0, errors);
    }

    public static void ThrowIfInvalid(NarrativeGraph graph)
    {
        var result = Validate(graph);
        if (!result.IsValid)
            throw new NarrativeGraphValidationException(result.Errors);
    }

    private static void ValidateNodeReferences(
        NarrativeNode node,
        IReadOnlySet<string> nodeIds,
        ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(node.Next) && !nodeIds.Contains(node.Next))
            errors.Add($"节点 '{node.Id}' 的 next 引用不存在: '{node.Next}'。");

        foreach (var branch in node.Branches)
        {
            if (string.IsNullOrWhiteSpace(branch.Id))
                errors.Add($"节点 '{node.Id}' 存在空 branch id。");
            if (string.IsNullOrWhiteSpace(branch.Next) || !nodeIds.Contains(branch.Next))
                errors.Add($"节点 '{node.Id}' 的 branch '{branch.Id}' next 引用不存在: '{branch.Next}'。");
        }

        if (node.TimeLimit?.ForcedNext is { } forcedNext && !nodeIds.Contains(forcedNext))
            errors.Add($"节点 '{node.Id}' 的 forced_next 引用不存在: '{forcedNext}'。");

        if (node.Type == NarrativeNodeType.Gate && node.Gate == null)
            errors.Add($"gate 节点 '{node.Id}' 缺少 gate requirement。");

        if (node.Gate != null)
        {
            if (node.Gate.RequiredBranches < 0)
                errors.Add($"gate 节点 '{node.Id}' 的 required_branches 不能为负数。");

            foreach (var branchNodeId in node.Gate.BranchNodeIds)
            {
                if (!nodeIds.Contains(branchNodeId))
                    errors.Add($"gate 节点 '{node.Id}' 的 branch_node 引用不存在: '{branchNodeId}'。");
            }
        }
    }
}

public sealed class NarrativeGraphValidationException : Exception
{
    public NarrativeGraphValidationException(IReadOnlyList<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
