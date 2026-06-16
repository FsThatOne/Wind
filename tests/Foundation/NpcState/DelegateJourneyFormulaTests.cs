using Xunit;
using FengZhi.Foundation.NpcState;

namespace FengZhi.Tests.Foundation.NpcStates;

public class DelegateJourneyFormulaTests
{
    // ═══════════════════════════════════════════════════════════
    // AC1: DelegateFormula.IsEligible 全部 5 条件 AND 逻辑
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void IsEligible_AllConditionsMet_ReturnsTrue()
    {
        Assert.True(DelegateFormula.IsEligible(
            LifeStatus.Alive,
            AttitudeLevel.Friendly,
            RelationshipStage.Acquaintance,
            PresenceStatus.NearbyVisible,
            delegateAllowed: true));
    }

    [Fact]
    public void IsEligible_Dead_ReturnsFalse()
    {
        Assert.False(DelegateFormula.IsEligible(
            LifeStatus.Dead,
            AttitudeLevel.LifeDeath,
            RelationshipStage.SwornSibling,
            PresenceStatus.InParty,
            delegateAllowed: true));
    }

    [Fact]
    public void IsEligible_AttitudeTooLow_ReturnsFalse()
    {
        Assert.False(DelegateFormula.IsEligible(
            LifeStatus.Alive,
            AttitudeLevel.Stranger, // 低于 Friendly
            RelationshipStage.Friend,
            PresenceStatus.NearbyVisible,
            delegateAllowed: true));
    }

    [Fact]
    public void IsEligible_RelationshipTooLow_ReturnsFalse()
    {
        Assert.False(DelegateFormula.IsEligible(
            LifeStatus.Alive,
            AttitudeLevel.Trusted,
            RelationshipStage.Stranger, // 低于 Acquaintance
            PresenceStatus.NearbyVisible,
            delegateAllowed: true));
    }

    [Fact]
    public void IsEligible_Unreachable_ReturnsFalse()
    {
        Assert.False(DelegateFormula.IsEligible(
            LifeStatus.Alive,
            AttitudeLevel.Friendly,
            RelationshipStage.Friend,
            PresenceStatus.Unreachable,
            delegateAllowed: true));
    }

    [Fact]
    public void IsEligible_DelegateNotAllowed_ReturnsFalse()
    {
        Assert.False(DelegateFormula.IsEligible(
            LifeStatus.Alive,
            AttitudeLevel.Trusted,
            RelationshipStage.CloseFriend,
            PresenceStatus.InParty,
            delegateAllowed: false));
    }

    [Fact]
    public void IsEligible_Injured_StillEligible()
    {
        Assert.True(DelegateFormula.IsEligible(
            LifeStatus.Injured,
            AttitudeLevel.Friendly,
            RelationshipStage.Acquaintance,
            PresenceStatus.SameRegion,
            delegateAllowed: true));
    }

    // ═══════════════════════════════════════════════════════════
    // AC2: JourneyFormula.IsTriggered (chapter OR day) AND NOT already
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void IsTriggered_ChapterMet_ReturnsTrue()
    {
        Assert.True(JourneyFormula.IsTriggered(
            currentChapter: 3, requiredChapter: 3,
            dayCount: 0, requiredDays: 10,
            alreadyTriggered: false));
    }

    [Fact]
    public void IsTriggered_DaysMet_ReturnsTrue()
    {
        Assert.True(JourneyFormula.IsTriggered(
            currentChapter: 1, requiredChapter: 5,
            dayCount: 10, requiredDays: 10,
            alreadyTriggered: false));
    }

    [Fact]
    public void IsTriggered_NeitherMet_ReturnsFalse()
    {
        Assert.False(JourneyFormula.IsTriggered(
            currentChapter: 2, requiredChapter: 3,
            dayCount: 5, requiredDays: 10,
            alreadyTriggered: false));
    }

    [Fact]
    public void IsTriggered_AlreadyTriggered_ReturnsFalse()
    {
        Assert.False(JourneyFormula.IsTriggered(
            currentChapter: 5, requiredChapter: 3,
            dayCount: 20, requiredDays: 10,
            alreadyTriggered: true));
    }

    // ═══════════════════════════════════════════════════════════
    // AC3: HelpRequestFormula.Calculate 返回 clamp(0,100)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void HelpRequest_NormalCase()
    {
        Assert.Equal(50, HelpRequestFormula.Calculate(20, 20, 10));
    }

    // ─── AC5: GDD 示例验证 ─────────────────────────────────

    [Fact]
    public void HelpRequest_GddExample_Base20_Gap30_Attitude20_Returns70()
    {
        Assert.Equal(70, HelpRequestFormula.Calculate(20, 30, 20));
    }

    [Fact]
    public void HelpRequest_ClampsToZero()
    {
        Assert.Equal(0, HelpRequestFormula.Calculate(-10, -20, 5));
    }

    [Fact]
    public void HelpRequest_ClampsTo100()
    {
        Assert.Equal(100, HelpRequestFormula.Calculate(50, 40, 30));
    }

    // ─── AC6: help_request_allowed=false 时 base=0 ─────────

    [Fact]
    public void HelpRequest_BaseZero_AlwaysZeroOrAbilityGap()
    {
        // base=0 (not allowed), gap=0, attitude=0 → 0
        Assert.Equal(0, HelpRequestFormula.Calculate(0, 0, 0));
    }

    // ═══════════════════════════════════════════════════════════
    // AC4: GuidedDelegateFormula.Calculate
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GuidedDelegate_SumsCorrectly()
    {
        Assert.Equal(35, GuidedDelegateFormula.Calculate(25, 10));
    }

    [Fact]
    public void GuidedDelegate_ZeroBonus()
    {
        Assert.Equal(20, GuidedDelegateFormula.Calculate(20, 0));
    }
}
