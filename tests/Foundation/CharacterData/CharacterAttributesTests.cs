using Xunit;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Tests.Foundation.CharacterData;

public class CharacterAttributesTests
{
    // ─── AC1: 三层属性完整性 ─────────────────────────────────

    [Fact]
    public void NewCharacter_HasAllThreeLayers()
    {
        var attrs = new CharacterAttributes();

        // 资源属性
        Assert.Equal(100, attrs.MaxHp);
        Assert.Equal(100, attrs.CurrentHp);
        Assert.Equal(20, attrs.MaxNeiXi);
        Assert.Equal(20, attrs.CurrentNeiXi);
        Assert.Equal(5, attrs.StaggerThreshold);
        Assert.Equal(0, attrs.CurrentStagger);

        // 基础属性（五维默认 10）
        Assert.Equal(10, attrs.Strength);
        Assert.Equal(10, attrs.Agility);
        Assert.Equal(10, attrs.InnerPower);
        Assert.Equal(10, attrs.Insight);
        Assert.Equal(10, attrs.Constitution);

        // 派生属性
        Assert.Equal(50, attrs.TotalPower); // 五维之和 10×5
    }

    // ─── AC2: ApplyDamage ────────────────────────────────────

    [Fact]
    public void ApplyDamage_ReducesHp()
    {
        var attrs = new CharacterAttributes();
        attrs.ApplyDamage(30);
        Assert.Equal(70, attrs.CurrentHp);
    }

    [Fact]
    public void ApplyDamage_ClampsToZero()
    {
        var attrs = new CharacterAttributes();
        attrs.SetCurrentHp(10);
        attrs.ApplyDamage(20);
        Assert.Equal(0, attrs.CurrentHp);
    }

    [Fact]
    public void ApplyDamage_NeverGoesNegative()
    {
        var attrs = new CharacterAttributes();
        attrs.SetCurrentHp(5);
        attrs.ApplyDamage(100);
        Assert.Equal(0, attrs.CurrentHp);
    }

    // ─── AC3: SpendNeiXi ────────────────────────────────────

    [Fact]
    public void SpendNeiXi_ReducesAndReturnsTrue()
    {
        var attrs = new CharacterAttributes();
        bool result = attrs.SpendNeiXi(5);
        Assert.True(result);
        Assert.Equal(15, attrs.CurrentNeiXi);
    }

    [Fact]
    public void SpendNeiXi_InsufficientReturnsFalse()
    {
        var attrs = new CharacterAttributes();
        attrs.SpendNeiXi(17); // 20→3
        bool result = attrs.SpendNeiXi(5); // 3 < 5
        Assert.False(result);
        Assert.Equal(3, attrs.CurrentNeiXi); // 不变
    }

    // ─── AC4: Stagger ───────────────────────────────────────

    [Fact]
    public void AddStagger_Accumulates()
    {
        var attrs = new CharacterAttributes();
        attrs.AddStagger(3);
        Assert.Equal(3, attrs.CurrentStagger);
        attrs.AddStagger(2);
        Assert.Equal(5, attrs.CurrentStagger);
    }

    [Fact]
    public void DecayStagger_ReducesByOne()
    {
        var attrs = new CharacterAttributes();
        attrs.AddStagger(3);
        attrs.DecayStagger();
        Assert.Equal(2, attrs.CurrentStagger);
    }

    [Fact]
    public void DecayStagger_ClampsToZero()
    {
        var attrs = new CharacterAttributes();
        attrs.DecayStagger(); // 从 0 衰减
        Assert.Equal(0, attrs.CurrentStagger);
    }

    [Fact]
    public void IsStaggered_WhenThresholdReached()
    {
        var attrs = new CharacterAttributes();
        attrs.AddStagger(5);
        Assert.True(attrs.IsStaggered);
    }

    // ─── AC5: ResetForCombat ────────────────────────────────

    [Fact]
    public void ResetForCombat_RestoresMaxAndClearsStagger()
    {
        var attrs = new CharacterAttributes();
        attrs.ApplyDamage(50);
        attrs.SpendNeiXi(10);
        attrs.AddStagger(3);

        attrs.ResetForCombat();

        Assert.Equal(attrs.MaxHp, attrs.CurrentHp);
        Assert.Equal(attrs.MaxNeiXi, attrs.CurrentNeiXi);
        Assert.Equal(0, attrs.CurrentStagger);
    }

    // ─── AC6: 属性钳位 ──────────────────────────────────────

    [Fact]
    public void BaseAttribute_ClampedToMin1()
    {
        var attrs = new CharacterAttributes();
        attrs.Strength = -5;
        Assert.Equal(1, attrs.Strength);
    }

    [Fact]
    public void BaseAttribute_ClampedToCap50()
    {
        var attrs = new CharacterAttributes();
        attrs.Agility = 99;
        Assert.Equal(50, attrs.Agility);
    }

    [Fact]
    public void AllBaseAttributes_ClampCorrectly()
    {
        var attrs = new CharacterAttributes();
        attrs.Strength = 0;
        attrs.Agility = -10;
        attrs.InnerPower = 0;
        attrs.Insight = -100;
        attrs.Constitution = 0;

        Assert.Equal(1, attrs.Strength);
        Assert.Equal(1, attrs.Agility);
        Assert.Equal(1, attrs.InnerPower);
        Assert.Equal(1, attrs.Insight);
        Assert.Equal(1, attrs.Constitution);
    }

    // ─── AC7: HP上限 stub (F4 公式) ─────────────────────────

    [Fact]
    public void ComputedMaxHp_Constitution10_Returns140()
    {
        var attrs = new CharacterAttributes
        {
            MaxHp = 100, // base_hp
            Constitution = 10
        };
        // F4: base_hp + constitution × 4 = 100 + 40 = 140
        Assert.Equal(140, attrs.ComputedMaxHp);
    }

    // ─── 额外边界测试 ───────────────────────────────────────

    [Fact]
    public void RecoverNeiXi_ClampsToMax()
    {
        var attrs = new CharacterAttributes();
        attrs.SpendNeiXi(5); // 20→15
        attrs.RecoverNeiXi(100); // 不超过 20
        Assert.Equal(20, attrs.CurrentNeiXi);
    }

    [Fact]
    public void CharacterType_DefaultIsPlayer()
    {
        var attrs = new CharacterAttributes();
        Assert.Equal(CharacterType.Player, attrs.Type);
    }

    [Fact]
    public void TotalPower_SumOfFiveAttributes()
    {
        var attrs = new CharacterAttributes
        {
            Strength = 15,
            Agility = 12,
            InnerPower = 20,
            Insight = 8,
            Constitution = 18
        };
        Assert.Equal(15 + 12 + 20 + 8 + 18, attrs.TotalPower);
    }
}
