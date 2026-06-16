using Xunit;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Tests.Foundation.CharacterData;

public class RealmSystemTests
{
    // ─── AC1: 境界名称映射 ──────────────────────────────────

    [Theory]
    [InlineData(20, "初学乍练")]   // < 25
    [InlineData(24, "初学乍练")]
    [InlineData(25, "初窥门径")]   // 25-34
    [InlineData(34, "初窥门径")]
    [InlineData(35, "登堂入室")]   // 35-49
    [InlineData(40, "登堂入室")]   // GDD AC#1: 五维总和=40 → 登堂入室? 不对, 40∈[35,50) → 登堂入室
    [InlineData(49, "登堂入室")]
    [InlineData(50, "融会贯通")]   // 50-69
    [InlineData(70, "驾轻就熟")]   // 70-89
    [InlineData(90, "炉火纯青")]   // 90-114
    [InlineData(115, "出神入化")]  // 115-139
    [InlineData(140, "登峰造极")]  // 140-169
    [InlineData(170, "返璞归真")]  // 170-199
    [InlineData(200, "返璞归真")]  // 200+ 仍为返璞归真
    [InlineData(250, "返璞归真")]
    public void GetRealmName_CorrectMapping(int totalPower, string expected)
    {
        Assert.Equal(expected, RealmSystem.GetRealmName(totalPower));
    }

    // ─── AC2: 功力跨阈值 + 叙事满足 → Triggered ────────────

    [Fact]
    public void CheckBreakthrough_PowerCrossesThreshold_NarrativeMet_Triggered()
    {
        // 当前: 炉火纯青 (index 5, 90-114), 功力升至 116 → 出神入化 (index 6)
        var result = RealmSystem.CheckBreakthrough(
            currentRealmIndex: 5,
            totalPower: 116,
            narrativeConditionMet: true);

        Assert.Equal(BreakthroughResult.Triggered, result);
    }

    // ─── AC3: 功力达标但叙事未满足 → PendingNarrative ───────

    [Fact]
    public void CheckBreakthrough_PowerCrossesThreshold_NarrativeNotMet_Pending()
    {
        // 功力=115 → 出神入化阈值, 叙事未满足
        var result = RealmSystem.CheckBreakthrough(
            currentRealmIndex: 5,
            totalPower: 115,
            narrativeConditionMet: false);

        Assert.Equal(BreakthroughResult.PendingNarrative, result);
    }

    // ─── AC4: ComparePower 返回等级和文学描述 ────────────────

    [Fact]
    public void ComparePower_FarWeaker_ReturnsCorrectDescription()
    {
        // 40/100 = 0.4 < 0.5 → FarWeaker
        var result = RealmSystem.ComparePower(40, 100);
        Assert.Equal(PowerLevel.FarWeaker, result.Level);
        Assert.Equal("此人气势如渊，你感到难以抗衡", result.Description);
    }

    [Fact]
    public void ComparePower_Weaker()
    {
        // 60/100 = 0.6 ∈ [0.5, 0.8) → Weaker
        var result = RealmSystem.ComparePower(60, 100);
        Assert.Equal(PowerLevel.Weaker, result.Level);
        Assert.Equal("此人内力深厚，不可小觑", result.Description);
    }

    [Fact]
    public void ComparePower_Comparable()
    {
        // 90/100 = 0.9 ∈ [0.8, 1.2) → Comparable
        var result = RealmSystem.ComparePower(90, 100);
        Assert.Equal(PowerLevel.Comparable, result.Level);
        Assert.Equal("你与此人旗鼓相当", result.Description);
    }

    [Fact]
    public void ComparePower_Stronger()
    {
        // 150/100 = 1.5 ∈ [1.2, 2.0) → Stronger
        var result = RealmSystem.ComparePower(150, 100);
        Assert.Equal(PowerLevel.Stronger, result.Level);
        Assert.Equal("此人功力尚浅", result.Description);
    }

    [Fact]
    public void ComparePower_FarStronger()
    {
        // 200/100 = 2.0 ≥ 2.0 → FarStronger
        var result = RealmSystem.ComparePower(200, 100);
        Assert.Equal(PowerLevel.FarStronger, result.Level);
        Assert.Equal("此人不足为惧", result.Description);
    }

    // ─── AC5: 临时buff不触发突破（只看永久层总值） ──────────

    [Fact]
    public void CheckBreakthrough_NoChange_WhenPowerBelowNextThreshold()
    {
        // 当前: 炉火纯青 (index 5), 功力仍为 110 (未达 115)
        var result = RealmSystem.CheckBreakthrough(
            currentRealmIndex: 5,
            totalPower: 110,
            narrativeConditionMet: true);

        Assert.Equal(BreakthroughResult.NoChange, result);
    }

    // ─── AC6: 200+ 无更高境界 ───────────────────────────────

    [Fact]
    public void GetRealmIndex_At200Plus_ReturnsMaxIndex()
    {
        // 200+ 仍为 index 8 (返璞归真)，不溢出
        int index = RealmSystem.GetRealmIndex(250);
        Assert.Equal(8, index);
    }

    [Fact]
    public void CheckBreakthrough_AtMaxRealm_NoChange()
    {
        // 已在返璞归真 (index 8), 功力=250, 无法再突破
        var result = RealmSystem.CheckBreakthrough(
            currentRealmIndex: 8,
            totalPower: 250,
            narrativeConditionMet: true);

        Assert.Equal(BreakthroughResult.NoChange, result);
    }

    // ─── 边界测试 ───────────────────────────────────────────

    [Fact]
    public void GetRealmIndex_Zero_ReturnsInitial()
    {
        Assert.Equal(0, RealmSystem.GetRealmIndex(0));
    }

    [Fact]
    public void CheckBreakthrough_MultipleRealmJump_StillTriggered()
    {
        // 跳跃式成长（理论上不应发生，但验证正确性）
        // 当前: 初学乍练 (index 0), 功力突然=100 → 应为炉火纯青 (index 5)
        var result = RealmSystem.CheckBreakthrough(
            currentRealmIndex: 0,
            totalPower: 100,
            narrativeConditionMet: true);

        Assert.Equal(BreakthroughResult.Triggered, result);
    }
}
