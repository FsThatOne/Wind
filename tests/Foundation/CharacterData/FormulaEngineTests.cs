using Xunit;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Tests.Foundation.CharacterData;

public class FormulaEngineTests
{
    // ─── F1: 体系攻击力 ─────────────────────────────────────

    [Fact]
    public void F1_GddExample_Strength20_Base10_Sf1_Mod5_Returns35()
    {
        // GDD: 力量=20, base=10, sf=1.0, mod=5 → 10 + 20×1.0 + 5 = 35
        int result = FormulaEngine.AttackForType(10, 20, 1.0f, 5);
        Assert.Equal(35, result);
    }

    [Fact]
    public void F1_GangType_UsesStrength()
    {
        int result = FormulaEngine.AttackForType(MoveType.Gang, 10, 1.0f, 0,
            strength: 20, agility: 5, innerPower: 5);
        Assert.Equal(30, result); // 10 + 20×1.0 + 0
    }

    [Fact]
    public void F1_QiaoType_UsesAgility()
    {
        int result = FormulaEngine.AttackForType(MoveType.Qiao, 10, 1.0f, 0,
            strength: 5, agility: 20, innerPower: 5);
        Assert.Equal(30, result); // 10 + 20×1.0 + 0
    }

    [Fact]
    public void F1_RouType_UsesInnerPower()
    {
        int result = FormulaEngine.AttackForType(MoveType.Rou, 10, 1.0f, 0,
            strength: 5, agility: 5, innerPower: 20);
        Assert.Equal(30, result); // 10 + 20×1.0 + 0
    }

    [Fact]
    public void F1_ScalingFactor_Affects_Result()
    {
        int result = FormulaEngine.AttackForType(10, 20, 1.2f, 0);
        Assert.Equal(34, result); // 10 + 20×1.2 + 0 = 34
    }

    // ─── F2: 防御力 ─────────────────────────────────────────

    [Fact]
    public void F2_GddExample_Con15_Str12_Mod3_Returns14()
    {
        // GDD: 体魄=15, 力量=12, mod=3 → 15×0.6 + 12×0.2 + 3 = 9+2.4+3 = 14.4 → 14
        int result = FormulaEngine.Defense(15, 12, 3);
        Assert.Equal(14, result);
    }

    [Fact]
    public void F2_MinValues()
    {
        int result = FormulaEngine.Defense(1, 1, 0);
        // 1×0.6 + 1×0.2 = 0.8 → rounds to 1
        Assert.Equal(1, result);
    }

    // ─── F4: HP 上限 ────────────────────────────────────────

    [Fact]
    public void F4_GddExample_Base100_Con10_Mod0_Returns140()
    {
        // GDD: base=100, 体魄=10, mod=0 → 100 + 40 = 140
        int result = FormulaEngine.MaxHp(100, 10, 0);
        Assert.Equal(140, result);
    }

    [Fact]
    public void F4_WithModifier()
    {
        int result = FormulaEngine.MaxHp(100, 10, 20);
        Assert.Equal(160, result); // 100 + 40 + 20
    }

    // ─── F5: 内息上限 ───────────────────────────────────────

    [Fact]
    public void F5_GddExample_Base20_Inner10_Mod0_Returns40()
    {
        // GDD: base=20, 内力=10, mod=0 → 20 + 20 = 40
        int result = FormulaEngine.MaxNeiXi(20, 10, 0);
        Assert.Equal(40, result);
    }

    // ─── F6: 内息回复 ───────────────────────────────────────

    [Fact]
    public void F6_GddExample_Inner15_Mod0_Returns5()
    {
        // GDD: 内力=15, mod=0 → 2 + 15×0.2 = 5
        int result = FormulaEngine.NeiXiRecovery(15, 0f);
        Assert.Equal(5, result);
    }

    [Fact]
    public void F6_MinimumIs2_WhenInnerPower0()
    {
        // 2 + 0×0.2 = 2 → max(2,2) = 2
        int result = FormulaEngine.NeiXiRecovery(0, 0f);
        Assert.Equal(2, result);
    }

    [Fact]
    public void F6_MinimumProtection_NegativeModifier()
    {
        // 2 + 5×0.2 + (-5) = 2 + 1 - 5 = -2 → max(2, -2) = 2
        int result = FormulaEngine.NeiXiRecovery(5, -5f);
        Assert.Equal(2, result);
    }

    // ─── F7: 暴击率 ─────────────────────────────────────────

    [Fact]
    public void F7_GddExample_Agi20_Mod0_Returns10Percent()
    {
        // GDD: 敏捷=20, mod=0% → 20×0.5% = 10%
        float result = FormulaEngine.CritRate(20, 0f);
        Assert.Equal(0.10f, result, precision: 4);
    }

    [Fact]
    public void F7_CapAt30Percent()
    {
        // 敏捷=100 → 100×0.5% = 50% → cap at 30%
        float result = FormulaEngine.CritRate(100, 0f);
        Assert.Equal(0.30f, result, precision: 4);
    }

    [Fact]
    public void F7_CapWithModifier()
    {
        // 敏捷=50 → 50×0.5% = 25%, mod=10% → 35% → cap at 30%
        float result = FormulaEngine.CritRate(50, 0.10f);
        Assert.Equal(0.30f, result, precision: 4);
    }

    // ─── F8: 相对强度比较 ───────────────────────────────────

    [Fact]
    public void F8_RatioBelow05_FarWeaker()
    {
        // 40/100 = 0.4 < 0.5 → FarWeaker
        var result = FormulaEngine.ComparePower(40, 100);
        Assert.Equal(PowerLevel.FarWeaker, result);
    }

    [Fact]
    public void F8_RatioExactly05_Weaker()
    {
        // GDD: 0.5-0.8 = 弱; 80/160=0.5 → Weaker
        var result = FormulaEngine.ComparePower(80, 160);
        Assert.Equal(PowerLevel.Weaker, result);
    }

    [Fact]
    public void F8_Ratio08_Comparable()
    {
        // 80/100 = 0.8 → Comparable (0.8-1.2)
        var result = FormulaEngine.ComparePower(80, 100);
        Assert.Equal(PowerLevel.Comparable, result);
    }

    [Fact]
    public void F8_Ratio12_Stronger()
    {
        // 120/100 = 1.2 → Stronger (1.2-2.0)
        var result = FormulaEngine.ComparePower(120, 100);
        Assert.Equal(PowerLevel.Stronger, result);
    }

    [Fact]
    public void F8_Ratio20_FarStronger()
    {
        // 200/100 = 2.0 → FarStronger (≥2.0)
        var result = FormulaEngine.ComparePower(200, 100);
        Assert.Equal(PowerLevel.FarStronger, result);
    }

    [Fact]
    public void F8_TargetPowerZero_FarStronger()
    {
        var result = FormulaEngine.ComparePower(50, 0);
        Assert.Equal(PowerLevel.FarStronger, result);
    }

    [Fact]
    public void F8_SelfPowerZero_FarWeaker()
    {
        var result = FormulaEngine.ComparePower(0, 50);
        Assert.Equal(PowerLevel.FarWeaker, result);
    }

    // ─── TotalPower ─────────────────────────────────────────

    [Fact]
    public void TotalPower_SumOfFive()
    {
        int result = FormulaEngine.TotalPower(15, 12, 20, 8, 18);
        Assert.Equal(73, result);
    }

    // ─── CharacterAttributes 集成 ───────────────────────────

    [Fact]
    public void CharacterAttributes_ComputedMaxHp_UsesFormulaEngine()
    {
        var attrs = new CharacterAttributes { MaxHp = 100, Constitution = 10 };
        Assert.Equal(140, attrs.ComputedMaxHp);
    }

    [Fact]
    public void CharacterAttributes_ComputedDefense_UsesFormulaEngine()
    {
        var attrs = new CharacterAttributes { Constitution = 15, Strength = 12 };
        // 15×0.6 + 12×0.2 = 9 + 2.4 = 11.4 → 11
        Assert.Equal(11, attrs.ComputedDefense);
    }

    [Fact]
    public void CharacterAttributes_GetAttackForType_Gang()
    {
        var attrs = new CharacterAttributes { BaseAttack = 10, Strength = 20, ScalingFactor = 1.0f };
        Assert.Equal(30, attrs.GetAttackForType(MoveType.Gang));
    }

    [Fact]
    public void CharacterAttributes_ComputedCritRate()
    {
        var attrs = new CharacterAttributes { Agility = 20 };
        Assert.Equal(0.10f, attrs.ComputedCritRate, precision: 4);
    }

    [Fact]
    public void CharacterAttributes_ComputedNeiXiRecovery()
    {
        var attrs = new CharacterAttributes { InnerPower = 15 };
        Assert.Equal(5, attrs.ComputedNeiXiRecovery);
    }
}
