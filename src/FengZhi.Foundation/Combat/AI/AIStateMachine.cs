using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 普通敌人行为状态。GDD §States and Transitions。
/// 状态转换为单向不可逆。
/// </summary>
public enum AIBehaviorState
{
    /// <summary>正常：战斗开始时的默认状态</summary>
    Normal,

    /// <summary>警觉：累计被克制 ≥ 2 次后</summary>
    Alert,

    /// <summary>防御：HP < 40% 后</summary>
    Defensive,

    /// <summary>绝境：HP < 15% 后</summary>
    Desperate
}

/// <summary>
/// AI 状态机。管理状态转换和状态修正。
/// GDD: 正常 → 警觉(被克≥2) → 防御(HP<40%) → 绝境(HP<15%)
/// 单向不可逆，同场战斗内不回退。
/// </summary>
public sealed class AIStateMachine
{
    public const float DefensiveHPThreshold = 0.4f;
    public const float DesperateHPThreshold = 0.15f;
    public const int AlertCounterThreshold = 2;

    public AIBehaviorState CurrentState { get; private set; } = AIBehaviorState.Normal;

    /// <summary>被克制次数累计</summary>
    public int TimesCountered { get; private set; }

    /// <summary>
    /// 记录一次被克制。如果达到阈值且当前在正常状态，转入警觉。
    /// </summary>
    public void RecordCountered()
    {
        TimesCountered++;
        if (CurrentState == AIBehaviorState.Normal && TimesCountered >= AlertCounterThreshold)
        {
            CurrentState = AIBehaviorState.Alert;
        }
    }

    /// <summary>
    /// 根据当前 HP 比值更新状态（单向不可逆）。
    /// </summary>
    public void UpdateHP(float hpRatio)
    {
        if (hpRatio < DesperateHPThreshold && CurrentState != AIBehaviorState.Desperate)
        {
            CurrentState = AIBehaviorState.Desperate;
        }
        else if (hpRatio < DefensiveHPThreshold && CurrentState < AIBehaviorState.Defensive)
        {
            CurrentState = AIBehaviorState.Defensive;
        }
    }

    /// <summary>
    /// 获取当前状态的类型权重修正。
    /// GDD: 正常=无, 警觉=上回合体系-20, 防御=柔+20, 绝境=刚+30
    /// </summary>
    public TypeWeights GetStateModifier(MoveType? lastRoundType)
    {
        return CurrentState switch
        {
            AIBehaviorState.Normal => default,
            AIBehaviorState.Alert when lastRoundType.HasValue =>
                new TypeWeights().With(lastRoundType.Value, -20),
            AIBehaviorState.Alert => default,
            AIBehaviorState.Defensive => new TypeWeights { Rou = 20 },
            AIBehaviorState.Desperate => new TypeWeights { Gang = 30 },
            _ => default
        };
    }
}

/// <summary>
/// 情境修正计算器。GDD §Core Rules 3: 情境修正。
/// </summary>
public static class SituationalModifiers
{
    public const int SelfStaggerThreshold = 3;
    public const int TargetStaggerThreshold = 3;

    /// <summary>
    /// 计算情境修正。
    /// - 自身破绽 ≥ 3: 柔+15
    /// - 目标破绽 ≥ 3: 刚+20
    /// - 上回合被克制: 上回合体系-30
    /// </summary>
    public static TypeWeights Compute(
        int selfStagger,
        int targetStagger,
        bool wasCounteredLastRound,
        MoveType? lastRoundType)
    {
        var mod = new TypeWeights();

        if (selfStagger >= SelfStaggerThreshold)
            mod = mod with { Rou = mod.Rou + 15 };

        if (targetStagger >= TargetStaggerThreshold)
            mod = mod with { Gang = mod.Gang + 20 };

        if (wasCounteredLastRound && lastRoundType.HasValue)
        {
            int current = mod.Get(lastRoundType.Value);
            mod = mod.With(lastRoundType.Value, current - 30);
        }

        return mod;
    }
}

/// <summary>
/// 连续同体系惩罚计算。GDD §Core Rules 3。
/// 首次重复(连续2回合)=0，之后每回合-15。
/// </summary>
public static class ConsecutivePenalty
{
    public const int PenaltyPerRound = -15;

    /// <summary>
    /// 计算连续同体系惩罚。
    /// </summary>
    /// <param name="consecutiveCount">连续使用同体系的回合数（1=仅本回合，无连续）</param>
    /// <param name="type">连续使用的体系</param>
    /// <returns>惩罚权重</returns>
    public static TypeWeights Compute(int consecutiveCount, MoveType type)
    {
        if (consecutiveCount <= 1) return default; // 无连续
        if (consecutiveCount == 2) return default; // 首次重复，无惩罚

        // 从第3回合开始：(连续回合数 - 1) × -15，但首次重复免惩罚
        // 实际为 (consecutiveCount - 2) × -15
        int penalty = (consecutiveCount - 2) * PenaltyPerRound;
        return new TypeWeights().With(type, penalty);
    }
}

/// <summary>
/// 综合权重计算器，整合所有修正源。
/// </summary>
public static class WeightAggregator
{
    /// <summary>
    /// 计算综合 raw weights = 基础权重 + 状态修正 + 情境修正 + 连续惩罚。
    /// </summary>
    public static TypeWeights ComputeRawWeights(
        PersonalityTemplate template,
        TypeWeights stateModifier,
        TypeWeights situationalModifier,
        TypeWeights consecutivePenalty)
    {
        return new TypeWeights
        {
            Gang = template.WeightGang + stateModifier.Gang + situationalModifier.Gang + consecutivePenalty.Gang,
            Rou = template.WeightRou + stateModifier.Rou + situationalModifier.Rou + consecutivePenalty.Rou,
            Qiao = template.WeightQiao + stateModifier.Qiao + situationalModifier.Qiao + consecutivePenalty.Qiao
        };
    }
}
