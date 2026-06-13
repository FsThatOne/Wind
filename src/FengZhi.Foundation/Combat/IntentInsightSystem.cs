namespace FengZhi.Foundation.Combat;

/// <summary>
/// 意图可见度等级。
/// </summary>
public enum IntentVisibility
{
    /// <summary>完全看破：体系+具体招式名（功力比 > 1.5）</summary>
    FullReveal,

    /// <summary>正常看破：仅体系类型</summary>
    Normal,

    /// <summary>隐藏："?"（未看穿）</summary>
    Hidden
}

/// <summary>
/// 洞察判定随机源接口。
/// </summary>
public interface IInsightRandomSource
{
    /// <summary>返回 [0, 1) 之间的随机值。</summary>
    float NextRoll();
}

/// <summary>
/// 默认洞察随机源。
/// </summary>
public sealed class DefaultInsightRandom : IInsightRandomSource
{
    private readonly Random _rng = new();
    public float NextRoll() => (float)_rng.NextDouble();
}

/// <summary>
/// 意图洞察系统。
/// GDD §Formulas F9:
///   insight_chance = base_chance + insight × 1%
///   功力比分档决定 base_chance。
/// </summary>
public static class IntentInsightSystem
{
    public const float InsightBonusPerPoint = 0.01f; // 每点洞察 +1%

    /// <summary>
    /// 判定对单个敌人的意图可见度。每回合每敌人独立调用。
    /// </summary>
    /// <param name="powerRatio">功力比 = 己方功力 / 敌方功力</param>
    /// <param name="insightStat">己方洞察属性值</param>
    /// <param name="random">随机源</param>
    public static IntentVisibility DetermineVisibility(float powerRatio, int insightStat, IInsightRandomSource random)
    {
        // 功力比 > 1.5：完全看破（100%，显示招式名）
        if (powerRatio > 1.5f)
            return IntentVisibility.FullReveal;

        // 功力比 1.0-1.5：正常看破（100%）
        if (powerRatio >= 1.0f)
            return IntentVisibility.Normal;

        // 以下需要概率判定
        float baseChance = GetBaseChance(powerRatio);
        float totalChance = Math.Min(1.0f, baseChance + insightStat * InsightBonusPerPoint);

        float roll = random.NextRoll();
        return roll < totalChance ? IntentVisibility.Normal : IntentVisibility.Hidden;
    }

    /// <summary>
    /// 根据功力比获取基础看穿概率。
    /// </summary>
    public static float GetBaseChance(float powerRatio)
    {
        if (powerRatio >= 1.0f) return 1.0f;
        if (powerRatio >= 0.7f) return 0.80f;
        if (powerRatio >= 0.5f) return 0.50f;
        return 0.30f;
    }
}
