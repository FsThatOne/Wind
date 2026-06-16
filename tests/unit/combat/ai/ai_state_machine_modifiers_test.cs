using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-002: 状态机与综合修正系统验证。
/// GDD AC#2, AC#3, AC#10, AC#13, AC#14。
/// </summary>
public class AIStateMachineAndModifiersTests
{
    // --- AIStateMachine 状态转换 ---

    [Fact]
    public void InitialState_IsNormal()
    {
        var sm = new AIStateMachine();
        Assert.Equal(AIBehaviorState.Normal, sm.CurrentState);
    }

    [Fact]
    public void RecordCountered_TwicFromNormal_TransitionsToAlert()
    {
        var sm = new AIStateMachine();
        sm.RecordCountered();
        Assert.Equal(AIBehaviorState.Normal, sm.CurrentState);
        sm.RecordCountered();
        Assert.Equal(AIBehaviorState.Alert, sm.CurrentState); // GDD AC#13
    }

    [Fact]
    public void UpdateHP_Below40Percent_TransitionsToDefensive()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.39f);
        Assert.Equal(AIBehaviorState.Defensive, sm.CurrentState);
    }

    [Fact]
    public void UpdateHP_Below15Percent_TransitionsToDesperate()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.14f);
        Assert.Equal(AIBehaviorState.Desperate, sm.CurrentState); // GDD AC#14
    }

    [Fact]
    public void StateTransition_IsIrreversible_DefensiveCannotGoBackToNormal()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.35f); // → Defensive
        sm.UpdateHP(0.5f); // HP recovered, state should stay
        Assert.Equal(AIBehaviorState.Defensive, sm.CurrentState);
    }

    [Fact]
    public void StateTransition_AlertSkippedIfHPTriggersDefensive()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.35f); // Directly to Defensive, skipping Alert
        sm.RecordCountered();
        sm.RecordCountered();
        // Already Defensive, not demoted back
        Assert.Equal(AIBehaviorState.Defensive, sm.CurrentState);
    }

    [Fact]
    public void StateTransition_DesperateFromDefensive()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.35f); // Defensive
        Assert.Equal(AIBehaviorState.Defensive, sm.CurrentState);
        sm.UpdateHP(0.10f); // Desperate
        Assert.Equal(AIBehaviorState.Desperate, sm.CurrentState);
    }

    // --- 状态修正 ---

    [Fact]
    public void GetStateModifier_Normal_ReturnsZero()
    {
        var sm = new AIStateMachine();
        var mod = sm.GetStateModifier(MoveType.Gang);
        Assert.Equal(0, mod.Gang);
        Assert.Equal(0, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void GetStateModifier_Alert_LastTypeGang_GangMinus20()
    {
        var sm = new AIStateMachine();
        sm.RecordCountered();
        sm.RecordCountered(); // → Alert
        var mod = sm.GetStateModifier(MoveType.Gang);
        Assert.Equal(-20, mod.Gang); // GDD AC#13
        Assert.Equal(0, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void GetStateModifier_Defensive_RouPlus20()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.3f);
        var mod = sm.GetStateModifier(MoveType.Gang);
        Assert.Equal(0, mod.Gang);
        Assert.Equal(20, mod.Rou); // GDD AC#2
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void GetStateModifier_Desperate_GangPlus30()
    {
        var sm = new AIStateMachine();
        sm.UpdateHP(0.1f);
        var mod = sm.GetStateModifier(MoveType.Gang);
        Assert.Equal(30, mod.Gang);
        Assert.Equal(0, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    // --- 情境修正 ---

    [Fact]
    public void SituationalModifiers_SelfStagger3_RouPlus15()
    {
        var mod = SituationalModifiers.Compute(
            selfStagger: 3, targetStagger: 0,
            wasCounteredLastRound: false, lastRoundType: null);
        Assert.Equal(0, mod.Gang);
        Assert.Equal(15, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void SituationalModifiers_TargetStagger3_GangPlus20()
    {
        var mod = SituationalModifiers.Compute(
            selfStagger: 0, targetStagger: 3,
            wasCounteredLastRound: false, lastRoundType: null);
        Assert.Equal(20, mod.Gang);
        Assert.Equal(0, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void SituationalModifiers_WasCountered_LastTypeGang_GangMinus30()
    {
        var mod = SituationalModifiers.Compute(
            selfStagger: 0, targetStagger: 0,
            wasCounteredLastRound: true, lastRoundType: MoveType.Gang);
        Assert.Equal(-30, mod.Gang);
        Assert.Equal(0, mod.Rou);
        Assert.Equal(0, mod.Qiao);
    }

    [Fact]
    public void SituationalModifiers_AllActive_StacksCorrectly()
    {
        var mod = SituationalModifiers.Compute(
            selfStagger: 4, targetStagger: 5,
            wasCounteredLastRound: true, lastRoundType: MoveType.Gang);
        // Gang: +20 (target) -30 (countered) = -10
        Assert.Equal(-10, mod.Gang);
        Assert.Equal(15, mod.Rou); // self stagger
        Assert.Equal(0, mod.Qiao);
    }

    // --- 连续惩罚 ---

    [Fact]
    public void ConsecutivePenalty_Count1_NoPenalty()
    {
        var p = ConsecutivePenalty.Compute(1, MoveType.Gang);
        Assert.Equal(0, p.Gang);
    }

    [Fact]
    public void ConsecutivePenalty_Count2_FirstRepeat_NoPenalty()
    {
        var p = ConsecutivePenalty.Compute(2, MoveType.Gang);
        Assert.Equal(0, p.Gang); // 首次重复无惩罚
    }

    [Fact]
    public void ConsecutivePenalty_Count3_Minus15()
    {
        var p = ConsecutivePenalty.Compute(3, MoveType.Gang);
        Assert.Equal(-15, p.Gang); // (3-2) × -15 = -15, GDD AC#3
    }

    [Fact]
    public void ConsecutivePenalty_Count4_Minus30()
    {
        var p = ConsecutivePenalty.Compute(4, MoveType.Rou);
        Assert.Equal(-30, p.Rou); // (4-2) × -15 = -30
    }

    [Fact]
    public void ConsecutivePenalty_Count5_Minus45()
    {
        var p = ConsecutivePenalty.Compute(5, MoveType.Qiao);
        Assert.Equal(-45, p.Qiao); // (5-2) × -15 = -45
    }

    // --- 综合权重计算 ---

    [Fact]
    public void WeightAggregator_NoModifiers_ReturnsBaseWeights()
    {
        var raw = WeightAggregator.ComputeRawWeights(
            PersonalityTemplate.Fierce, default, default, default);
        Assert.Equal(60, raw.Gang);
        Assert.Equal(15, raw.Rou);
        Assert.Equal(25, raw.Qiao);
    }

    [Fact]
    public void WeightAggregator_DefensiveState_RouAdded()
    {
        var stateMod = new TypeWeights { Rou = 20 };
        var raw = WeightAggregator.ComputeRawWeights(
            PersonalityTemplate.Fierce, stateMod, default, default);
        Assert.Equal(60, raw.Gang);
        Assert.Equal(35, raw.Rou); // 15+20, GDD AC#2
        Assert.Equal(25, raw.Qiao);
    }

    [Fact]
    public void WeightAggregator_AllModifiers_Stack()
    {
        // 刚猛型 60/15/25, 警觉(Gang-20), 目标破绽高(Gang+20), 连续3回合刚(Gang-15)
        var stateMod = new TypeWeights { Gang = -20 };
        var sitMod = new TypeWeights { Gang = 20 };
        var consPen = new TypeWeights { Gang = -15 };
        var raw = WeightAggregator.ComputeRawWeights(
            PersonalityTemplate.Fierce, stateMod, sitMod, consPen);
        Assert.Equal(60 - 20 + 20 - 15, raw.Gang); // 45
        Assert.Equal(15, raw.Rou);
        Assert.Equal(25, raw.Qiao);
    }

    [Fact]
    public void WeightAggregator_ExtremePenalties_ClampedByEngine()
    {
        // 验证极端修正后经 TypeSelectionEngine 钳位到 min_type_weight
        var stateMod = new TypeWeights { Gang = -20 };
        var sitMod = new TypeWeights { Gang = -30 };
        var raw = WeightAggregator.ComputeRawWeights(
            PersonalityTemplate.Fierce, stateMod, sitMod, default);
        Assert.Equal(10, raw.Gang); // 60-20-30=10，仍 ≥ min(5)

        // 更极端
        var extremeSit = new TypeWeights { Gang = -30, Rou = -30 };
        var extremeState = new TypeWeights { Gang = -20 };
        var extremeRaw = WeightAggregator.ComputeRawWeights(
            PersonalityTemplate.Fierce, extremeState, extremeSit, default);
        // Gang: 60-20-30=10, Rou: 15-30=-15
        var probs = TypeSelectionEngine.ComputeProbabilities(extremeRaw);
        // Rou clamped to 5: 10/5/25=40 → 25%/12.5%/62.5%
        Assert.Equal(10f / 40f, probs.Gang, 0.001f);
        Assert.Equal(5f / 40f, probs.Rou, 0.001f); // GDD AC#10: clamped
        Assert.Equal(25f / 40f, probs.Qiao, 0.001f);
    }
}
