namespace FengZhi.Foundation.NpcState;

/// <summary>
/// F1 态度分数计算。
/// attitude_score = clamp(base + mindset_mod + morality_mod + misunderstanding_mod, -4, +3)
/// 分数直接映射为 AttitudeLevel 枚举。
/// </summary>
public static class NpcAttitudeFormula
{
    public const int MinScore = -4;
    public const int MaxScore = 3;

    /// <summary>
    /// 计算态度分数。
    /// </summary>
    /// <param name="baseValue">基础态度值（来自关系阶段或初始配置）</param>
    /// <param name="mindsetMod">心境态度修正（来自执念/释怀轴）</param>
    /// <param name="moralityMod">道义观修正</param>
    /// <param name="misunderstandingMod">误解修正（通常为负）</param>
    /// <returns>态度计算结果</returns>
    public static AttitudeResult Calculate(int baseValue, int mindsetMod, int moralityMod, int misunderstandingMod)
    {
        int raw = baseValue + mindsetMod + moralityMod + misunderstandingMod;
        int clamped = Math.Clamp(raw, MinScore, MaxScore);
        var level = (AttitudeLevel)clamped;
        return new AttitudeResult(clamped, level, raw != clamped);
    }

    /// <summary>
    /// 从态度分数映射为 AttitudeLevel。
    /// </summary>
    public static AttitudeLevel ScoreToLevel(int score)
    {
        int clamped = Math.Clamp(score, MinScore, MaxScore);
        return (AttitudeLevel)clamped;
    }
}

/// <summary>态度计算结果</summary>
public readonly record struct AttitudeResult(
    int Score,
    AttitudeLevel Level,
    bool WasClamped
);
