using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 体系权重输入，用于 Phase A 概率计算。
/// </summary>
public readonly struct TypeWeights
{
    public int Gang { get; init; }
    public int Rou { get; init; }
    public int Qiao { get; init; }

    public int Get(MoveType type) => type switch
    {
        MoveType.Gang => Gang,
        MoveType.Rou => Rou,
        MoveType.Qiao => Qiao,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public TypeWeights With(MoveType type, int value) => type switch
    {
        MoveType.Gang => this with { Gang = value },
        MoveType.Rou => this with { Rou = value },
        MoveType.Qiao => this with { Qiao = value },
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}

/// <summary>
/// 归一化后的体系概率。
/// </summary>
public readonly struct TypeProbabilities
{
    public float Gang { get; init; }
    public float Rou { get; init; }
    public float Qiao { get; init; }

    public float Get(MoveType type) => type switch
    {
        MoveType.Gang => Gang,
        MoveType.Rou => Rou,
        MoveType.Qiao => Qiao,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}

/// <summary>
/// 体系选择引擎。GDD F1 公式的纯函数实现。
/// 
/// F1: raw_weight[type] = base_weight[type] + modifiers
/// clamped_weight[type] = max(min_type_weight, raw_weight[type])
/// P(type) = clamped_weight[type] / Σ clamped_weight[all types]
/// </summary>
public static class TypeSelectionEngine
{
    public const int DefaultMinTypeWeight = 5;

    /// <summary>
    /// 对 raw weights 执行钳位和归一化，返回概率分布。
    /// </summary>
    public static TypeProbabilities ComputeProbabilities(TypeWeights rawWeights, int minTypeWeight = DefaultMinTypeWeight)
    {
        int clampedGang = Math.Max(minTypeWeight, rawWeights.Gang);
        int clampedRou = Math.Max(minTypeWeight, rawWeights.Rou);
        int clampedQiao = Math.Max(minTypeWeight, rawWeights.Qiao);

        float total = clampedGang + clampedRou + clampedQiao;

        return new TypeProbabilities
        {
            Gang = clampedGang / total,
            Rou = clampedRou / total,
            Qiao = clampedQiao / total
        };
    }

    /// <summary>
    /// 基于性格模板的基础权重计算概率（无修正）。
    /// </summary>
    public static TypeProbabilities ComputeFromTemplate(PersonalityTemplate template, int minTypeWeight = DefaultMinTypeWeight)
    {
        var weights = new TypeWeights
        {
            Gang = template.WeightGang,
            Rou = template.WeightRou,
            Qiao = template.WeightQiao
        };
        return ComputeProbabilities(weights, minTypeWeight);
    }

    /// <summary>
    /// 按概率分布选择一个体系。
    /// </summary>
    public static MoveType SelectType(TypeProbabilities probabilities, IAIRandomSource random)
    {
        float roll = random.NextFloat();

        if (roll < probabilities.Gang)
            return MoveType.Gang;
        if (roll < probabilities.Gang + probabilities.Rou)
            return MoveType.Rou;
        return MoveType.Qiao;
    }

    /// <summary>
    /// 从 raw weights 直接选择体系（钳位 + 归一化 + 随机选择）。
    /// </summary>
    public static MoveType SelectFromWeights(TypeWeights rawWeights, IAIRandomSource random, int minTypeWeight = DefaultMinTypeWeight)
    {
        var probs = ComputeProbabilities(rawWeights, minTypeWeight);
        return SelectType(probs, random);
    }
}
