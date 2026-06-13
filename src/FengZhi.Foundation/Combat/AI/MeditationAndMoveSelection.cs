using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// AI 可用招式定义（简化版，不走玩家残卷流程）。
/// </summary>
public sealed class AIMoveEntry
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MoveType Type { get; init; }
    public int NeixiCost { get; init; }
    public float DamageMultiplier { get; init; } = 1.0f;
}

/// <summary>
/// 调息决策逻辑。GDD §Core Rules 1 前置检查 + §Formulas F4。
/// </summary>
public static class MeditationDecision
{
    public const int DefaultMeditationThreshold = 2;
    public const float DefaultMeditationBaseChance = 0.4f;
    public const int DeadlockMaxConsecutive = 3;

    /// <summary>
    /// 判断是否应该调息。
    /// F4: if neixi <= threshold: chance = min(1.0, base_chance + (threshold - neixi) × 0.1)
    /// </summary>
    /// <param name="currentNeixi">当前内息</param>
    /// <param name="random">随机源</param>
    /// <param name="consecutiveMeditationCount">连续调息次数（防死锁）</param>
    /// <param name="threshold">调息阈值</param>
    /// <param name="baseChance">基础概率</param>
    /// <returns>true=选择调息</returns>
    public static bool ShouldMeditate(
        int currentNeixi,
        IAIRandomSource random,
        int consecutiveMeditationCount,
        int threshold = DefaultMeditationThreshold,
        float baseChance = DefaultMeditationBaseChance)
    {
        // 死锁防护：连续3回合调息后强制跳过
        if (consecutiveMeditationCount >= DeadlockMaxConsecutive)
            return false;

        if (currentNeixi > threshold)
            return false;

        float chance = MathF.Min(1.0f, baseChance + (threshold - currentNeixi) * 0.1f);
        return random.NextFloat() < chance;
    }

    /// <summary>
    /// 计算调息概率（用于测试验证）。
    /// </summary>
    public static float ComputeChance(int currentNeixi, int threshold = DefaultMeditationThreshold, float baseChance = DefaultMeditationBaseChance)
    {
        if (currentNeixi > threshold) return 0f;
        return MathF.Min(1.0f, baseChance + (threshold - currentNeixi) * 0.1f);
    }
}

/// <summary>
/// 选招逻辑。Phase A 选定体系后从可用招式列表中选择一个。
/// GDD §Core Rules 1: Phase B 选招。
/// </summary>
public static class MoveSelector
{
    /// <summary>
    /// 从选定体系中选招。如果该体系无可用招式，回退到其他体系。
    /// </summary>
    /// <param name="moves">所有可用招式</param>
    /// <param name="selectedType">Phase A 选定的体系</param>
    /// <param name="currentNeixi">当前内息</param>
    /// <param name="random">随机源</param>
    /// <returns>选中的招式，或 null 表示无招可用（应调息）</returns>
    public static AIMoveEntry? SelectMove(
        IReadOnlyList<AIMoveEntry> moves,
        MoveType selectedType,
        int currentNeixi,
        IAIRandomSource random)
    {
        // 先尝试选定体系中内息足够的招式
        var candidates = moves
            .Where(m => m.Type == selectedType && m.NeixiCost <= currentNeixi)
            .ToList();

        if (candidates.Count > 0)
            return PickRandom(candidates, random);

        // 回退：其他体系中内息足够的招式
        var fallback = moves
            .Where(m => m.NeixiCost <= currentNeixi)
            .ToList();

        if (fallback.Count > 0)
            return PickRandom(fallback, random);

        // 无招可用
        return null;
    }

    /// <summary>
    /// 处理零消耗招式 vs 调息的选择（GDD Edge Case）。
    /// 当所有非零消耗招式不可用时：80% 用零消耗招式，20% 调息。
    /// </summary>
    public static bool ShouldUseZeroCostMove(IAIRandomSource random)
    {
        return random.NextFloat() < 0.8f;
    }

    private static AIMoveEntry PickRandom(List<AIMoveEntry> candidates, IAIRandomSource random)
    {
        int index = (int)(random.NextFloat() * candidates.Count);
        index = Math.Min(index, candidates.Count - 1); // 防止浮点边界
        return candidates[index];
    }
}
