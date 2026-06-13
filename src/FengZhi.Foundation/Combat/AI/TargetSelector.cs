namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 目标候选信息（由战斗系统提供）。
/// </summary>
public sealed class TargetCandidate
{
    public string Id { get; init; } = string.Empty;
    public int CurrentStagger { get; init; }
    public int StaggerThreshold { get; init; } = 5;
    public float HpRatio { get; init; } = 1.0f;
    public int LastRoundDamageToSelf { get; init; }
}

/// <summary>
/// F3 目标优先级评分配置（Tuning Knobs）。
/// </summary>
public sealed class TargetPriorityConfig
{
    public int DecisiveBonus { get; init; } = 100;
    public int KillBonus { get; init; } = 50;
    public float ThreatPercent { get; init; } = 0.3f;
    public int ThreatCap { get; init; } = 50;
    public int SpreadPenalty { get; init; } = -20;
    public float KillThreshold { get; init; } = 0.3f;
}

/// <summary>
/// F3 目标选择逻辑。GDD §Formulas F3。
/// score = decisive_bonus + kill_bonus + threat_bonus + spread_penalty
/// </summary>
public static class TargetSelector
{
    public static readonly TargetPriorityConfig DefaultConfig = new();

    /// <summary>
    /// 计算单个目标的优先级评分。
    /// </summary>
    /// <param name="target">目标候选</param>
    /// <param name="alreadyTargetedIds">已被前序队友选为目标的角色 ID</param>
    /// <param name="config">评分配置</param>
    /// <returns>评分（越高越优先）</returns>
    public static int ComputeScore(
        TargetCandidate target,
        IReadOnlySet<string> alreadyTargetedIds,
        TargetPriorityConfig? config = null)
    {
        config ??= DefaultConfig;

        int score = 0;

        // decisive_bonus: 破绽 ≥ 阈值
        bool isDecisive = target.CurrentStagger >= target.StaggerThreshold;
        if (isDecisive)
            score += config.DecisiveBonus;

        // kill_bonus: HP < 30%
        if (target.HpRatio < config.KillThreshold)
            score += config.KillBonus;

        // threat_bonus: min(cap, damage × pct) 向下取整
        int threatRaw = (int)(target.LastRoundDamageToSelf * config.ThreatPercent);
        score += Math.Min(config.ThreatCap, threatRaw);

        // spread_penalty: 已有队友攻击该目标且非破绽达标
        if (!isDecisive && alreadyTargetedIds.Contains(target.Id))
            score += config.SpreadPenalty;

        return score;
    }

    /// <summary>
    /// 从候选列表中选择目标。得分最高者获选，平分时随机。
    /// </summary>
    /// <param name="candidates">所有可用目标</param>
    /// <param name="alreadyTargetedIds">已被前序队友选定的目标 ID（spread_penalty 用）</param>
    /// <param name="random">随机源</param>
    /// <param name="config">评分配置</param>
    /// <returns>选中的目标，或 null（无候选时）</returns>
    public static TargetCandidate? SelectTarget(
        IReadOnlyList<TargetCandidate> candidates,
        IReadOnlySet<string> alreadyTargetedIds,
        IAIRandomSource random,
        TargetPriorityConfig? config = null)
    {
        if (candidates.Count == 0) return null;

        config ??= DefaultConfig;

        int bestScore = int.MinValue;
        var bestCandidates = new List<TargetCandidate>();

        foreach (var target in candidates)
        {
            int score = ComputeScore(target, alreadyTargetedIds, config);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidates.Clear();
                bestCandidates.Add(target);
            }
            else if (score == bestScore)
            {
                bestCandidates.Add(target);
            }
        }

        if (bestCandidates.Count == 1)
            return bestCandidates[0];

        // 平分时随机
        int index = (int)(random.NextFloat() * bestCandidates.Count);
        index = Math.Min(index, bestCandidates.Count - 1);
        return bestCandidates[index];
    }
}
