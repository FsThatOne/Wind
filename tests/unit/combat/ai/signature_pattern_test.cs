using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-003: 招式预兆序列验证。
/// GDD AC#8: 战斗第1回合，刚猛型序列加成 = floor(25 × 1.5) = +37。
/// </summary>
public class SignaturePatternTests
{
    // --- SignaturePattern 数据 ---

    [Fact]
    public void FiercePattern_HasTwoSteps_GangGang()
    {
        var p = SignaturePattern.Fierce;
        Assert.Equal(2, p.Steps.Count);
        Assert.Equal(MoveType.Gang, p.Steps[0]);
        Assert.Equal(MoveType.Gang, p.Steps[1]);
        Assert.Equal(25, p.Bonus);
    }

    [Fact]
    public void ResilientPattern_HasThreeSteps_RouQiaoRou()
    {
        var p = SignaturePattern.Resilient;
        Assert.Equal(3, p.Steps.Count);
        Assert.Equal(MoveType.Rou, p.Steps[0]);
        Assert.Equal(MoveType.Qiao, p.Steps[1]);
        Assert.Equal(MoveType.Rou, p.Steps[2]);
    }

    [Fact]
    public void CunningPattern_HasThreeSteps_QiaoGangQiao()
    {
        var p = SignaturePattern.Cunning;
        Assert.Equal(3, p.Steps.Count);
        Assert.Equal(MoveType.Qiao, p.Steps[0]);
        Assert.Equal(MoveType.Gang, p.Steps[1]);
        Assert.Equal(MoveType.Qiao, p.Steps[2]);
    }

    // --- SignatureTracker 指针追踪 ---

    [Fact]
    public void Tracker_InitialPointer_IsZero()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        Assert.Equal(0, tracker.Pointer);
    }

    [Fact]
    public void Tracker_MatchingStep_AdvancesPointer()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Gang); // matches step[0]
        Assert.Equal(1, tracker.Pointer);
    }

    [Fact]
    public void Tracker_NonMatchingStep_PointerStays()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Rou); // doesn't match Gang
        Assert.Equal(0, tracker.Pointer); // 指针不动
    }

    [Fact]
    public void Tracker_CompletesSequence_ResetsToZero()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Gang); // step 0
        tracker.RecordChoice(MoveType.Gang); // step 1 → complete
        Assert.Equal(0, tracker.Pointer);
        Assert.True(tracker.JustCompleted);
    }

    [Fact]
    public void Tracker_AfterCompletion_JustCompletedResetsOnNextRecord()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Gang);
        tracker.RecordChoice(MoveType.Gang); // complete
        Assert.True(tracker.JustCompleted);
        tracker.RecordChoice(MoveType.Rou); // next round
        Assert.False(tracker.JustCompleted);
    }

    [Fact]
    public void Tracker_ThreeStepPattern_CompletesCorrectly()
    {
        var tracker = new SignatureTracker(SignaturePattern.Resilient);
        tracker.RecordChoice(MoveType.Rou);  // step 0 ✓
        Assert.Equal(1, tracker.Pointer);
        tracker.RecordChoice(MoveType.Qiao); // step 1 ✓
        Assert.Equal(2, tracker.Pointer);
        tracker.RecordChoice(MoveType.Rou);  // step 2 ✓ → complete
        Assert.Equal(0, tracker.Pointer);
        Assert.True(tracker.JustCompleted);
    }

    [Fact]
    public void Tracker_InterruptedByCounterRead_PointerStalls()
    {
        // GDD: 反读否决打断时，指针停滞
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Gang); // step 0 ✓, pointer=1
        tracker.RecordChoice(MoveType.Rou);  // 反读覆盖为柔，不匹配 step 1(Gang)
        Assert.Equal(1, tracker.Pointer); // 停滞
        tracker.RecordChoice(MoveType.Gang); // 回归，匹配 step 1
        Assert.Equal(0, tracker.Pointer); // 完成，重置
    }

    // --- 权重加成计算 ---

    [Fact]
    public void GetCurrentBonus_Round1_OpeningMultiplier_37()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        int bonus = tracker.GetCurrentBonus(currentRound: 1);
        Assert.Equal(37, bonus); // floor(25 × 1.5) = 37, GDD AC#8
    }

    [Fact]
    public void GetCurrentBonus_Round4_StillOpening_37()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        Assert.Equal(37, tracker.GetCurrentBonus(currentRound: 4));
    }

    [Fact]
    public void GetCurrentBonus_Round5_NoOpeningMultiplier_25()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        Assert.Equal(25, tracker.GetCurrentBonus(currentRound: 5));
    }

    [Fact]
    public void GetSignatureBonus_FierceRound1_GangPlus37()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        var bonus = tracker.GetSignatureBonus(currentRound: 1);
        Assert.Equal(37, bonus.Gang);
        Assert.Equal(0, bonus.Rou);
        Assert.Equal(0, bonus.Qiao);
    }

    [Fact]
    public void GetSignatureBonus_ResilientStep1_QiaoPlus37()
    {
        var tracker = new SignatureTracker(SignaturePattern.Resilient);
        tracker.RecordChoice(MoveType.Rou); // advance to step 1 (Qiao)
        var bonus = tracker.GetSignatureBonus(currentRound: 2);
        Assert.Equal(0, bonus.Gang);
        Assert.Equal(0, bonus.Rou);
        Assert.Equal(37, bonus.Qiao); // step 1 期望 Qiao
    }

    [Fact]
    public void GetCurrentStepType_ReturnsCorrectType()
    {
        var tracker = new SignatureTracker(SignaturePattern.Cunning);
        Assert.Equal(MoveType.Qiao, tracker.GetCurrentStepType()); // step 0
        tracker.RecordChoice(MoveType.Qiao);
        Assert.Equal(MoveType.Gang, tracker.GetCurrentStepType()); // step 1
    }

    [Fact]
    public void Reset_ClearsPointer()
    {
        var tracker = new SignatureTracker(SignaturePattern.Fierce);
        tracker.RecordChoice(MoveType.Gang);
        Assert.Equal(1, tracker.Pointer);
        tracker.Reset();
        Assert.Equal(0, tracker.Pointer);
    }
}
