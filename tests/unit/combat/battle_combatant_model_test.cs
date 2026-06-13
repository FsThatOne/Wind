using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class BattleCombatantModelTest
{
    private static BattleCombatant MakeCombatant(
        int maxHP = 100, int maxNeixi = 50, int staggerThreshold = 5)
        => new("test_1", "测试角色", maxHP, maxNeixi,
            attackGang: 30, attackRou: 25, attackQiao: 20,
            defense: 10, speed: 5, critRate: 0.1f,
            insightStat: 3, neixiRecovery: 5,
            staggerThreshold: staggerThreshold);

    [Fact]
    public void Init_FullResources()
    {
        var c = MakeCombatant(maxHP: 100, maxNeixi: 50);
        Assert.Equal(100, c.HP);
        Assert.Equal(100, c.MaxHP);
        Assert.Equal(50, c.Neixi);
        Assert.Equal(50, c.MaxNeixi);
        Assert.Equal(0, c.Stagger);
    }

    [Fact]
    public void ApplyDamage_ReducesHP()
    {
        var c = MakeCombatant();
        c.ApplyDamage(30);
        Assert.Equal(70, c.HP);
        Assert.True(c.IsAlive);
    }

    [Fact]
    public void ApplyDamage_ClampsToZero()
    {
        var c = MakeCombatant(maxHP: 50);
        c.ApplyDamage(999);
        Assert.Equal(0, c.HP);
        Assert.False(c.IsAlive);
    }

    [Fact]
    public void ApplyDamage_NegativeThrows()
    {
        var c = MakeCombatant();
        Assert.Throws<ArgumentOutOfRangeException>(() => c.ApplyDamage(-1));
    }

    [Fact]
    public void SpendNeixi_Success()
    {
        var c = MakeCombatant(maxNeixi: 50);
        bool result = c.SpendNeixi(20);
        Assert.True(result);
        Assert.Equal(30, c.Neixi);
    }

    [Fact]
    public void SpendNeixi_InsufficientFails()
    {
        var c = MakeCombatant(maxNeixi: 10);
        bool result = c.SpendNeixi(20);
        Assert.False(result);
        Assert.Equal(10, c.Neixi); // Unchanged
    }

    [Fact]
    public void RecoverNeixi_ClampsToMax()
    {
        var c = MakeCombatant(maxNeixi: 50);
        c.SpendNeixi(10); // 40
        c.RecoverNeixi(999);
        Assert.Equal(50, c.Neixi);
    }

    [Fact]
    public void DecayStagger_ReducesByOne()
    {
        var c = MakeCombatant();
        c.AddStagger(3);
        c.DecayStagger();
        Assert.Equal(2, c.Stagger);
    }

    [Fact]
    public void DecayStagger_ClampsToZero()
    {
        var c = MakeCombatant();
        c.DecayStagger();
        Assert.Equal(0, c.Stagger);
    }

    [Fact]
    public void IsStaggerExposed_AtThreshold()
    {
        var c = MakeCombatant(staggerThreshold: 5);
        Assert.False(c.IsStaggerExposed);

        c.AddStagger(4);
        Assert.False(c.IsStaggerExposed);

        c.AddStagger(1);
        Assert.True(c.IsStaggerExposed);
    }

    [Fact]
    public void IsStaggerExposed_AboveThreshold()
    {
        var c = MakeCombatant(staggerThreshold: 5);
        c.AddStagger(7);
        Assert.True(c.IsStaggerExposed);
    }

    [Fact]
    public void CanAfford_ExactAmount()
    {
        var c = MakeCombatant(maxNeixi: 10);
        Assert.True(c.CanAfford(10));
        Assert.False(c.CanAfford(11));
    }

    [Fact]
    public void GetAttackForType_ReturnsCorrectValue()
    {
        var c = new BattleCombatant("id", "name", 100, 50,
            attackGang: 30, attackRou: 25, attackQiao: 20,
            defense: 10, speed: 5, critRate: 0.1f,
            insightStat: 3, neixiRecovery: 5);

        Assert.Equal(30, c.GetAttackForType(MoveType.Gang));
        Assert.Equal(25, c.GetAttackForType(MoveType.Rou));
        Assert.Equal(20, c.GetAttackForType(MoveType.Qiao));
    }

    [Fact]
    public void NeixiRecovery_PropertyAccessible()
    {
        var c = MakeCombatant();
        Assert.Equal(5, c.NeixiRecovery);
    }
}
