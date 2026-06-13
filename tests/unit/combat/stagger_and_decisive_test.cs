using FengZhi.Foundation.Combat;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class StaggerAndDecisiveTest
{
    private static BattleCombatant MakeCombatant(int hp = 100, int defense = 10)
        => new("test_1", "测试", hp, 50, 30, 25, 20, defense, 5, 0.1f, 3, 5);

    // --- 破绽累积规则 ---

    [Fact]
    public void Advantage_DefenderGets2Stagger()
    {
        var change = StaggerService.CalculateStaggerChange(CounterRelation.Advantage);
        Assert.Equal(2, change.DefenderDelta);
        Assert.Equal(0, change.AttackerDelta);
    }

    [Fact]
    public void Disadvantage_AttackerGets1Stagger()
    {
        var change = StaggerService.CalculateStaggerChange(CounterRelation.Disadvantage);
        Assert.Equal(0, change.DefenderDelta);
        Assert.Equal(1, change.AttackerDelta);
    }

    [Fact]
    public void Neutral_NoStaggerChange()
    {
        var change = StaggerService.CalculateStaggerChange(CounterRelation.Neutral);
        Assert.Equal(0, change.DefenderDelta);
        Assert.Equal(0, change.AttackerDelta);
    }

    [Fact]
    public void CounterAction_ExtraStagger()
    {
        // 反制（克制+指定目标）：守方总计 +3
        var change = StaggerService.CalculateStaggerChange(CounterRelation.Advantage, isCounterAction: true);
        Assert.Equal(3, change.DefenderDelta);
        Assert.Equal(0, change.AttackerDelta);
    }

    [Fact]
    public void CounterAction_NotAdvantage_NoExtra()
    {
        // 反制但未克制（降级普攻场景）
        var change = StaggerService.CalculateStaggerChange(CounterRelation.Neutral, isCounterAction: true);
        Assert.Equal(0, change.DefenderDelta);
    }

    // --- 破绽暴露判定 ---

    [Fact]
    public void StaggerExposed_AtThreshold5()
    {
        var target = MakeCombatant();
        target.AddStagger(5);
        Assert.True(StaggerService.CanExecuteDecisiveStrike(target));
    }

    [Fact]
    public void StaggerExposed_BelowThreshold()
    {
        var target = MakeCombatant();
        target.AddStagger(4);
        Assert.False(StaggerService.CanExecuteDecisiveStrike(target));
    }

    // --- 一击决胜伤害 F6 ---

    [Fact]
    public void DecisiveDamage_BaseDamageTimesTwo()
    {
        // effective=50, defense=10 → base=40, decisive=80
        int damage = StaggerService.CalculateDecisiveDamage(50f, 10f);
        Assert.Equal(80, damage);
    }

    [Fact]
    public void DecisiveDamage_MinClamps()
    {
        // effective=5, defense=100 → base=1(clamped), decisive=2
        int damage = StaggerService.CalculateDecisiveDamage(5f, 100f);
        Assert.Equal(2, damage);
    }

    // --- 执行一击决胜 ---

    [Fact]
    public void ExecuteDecisive_DamageAndClearStagger()
    {
        var attacker = MakeCombatant();
        var target = MakeCombatant(hp: 100, defense: 10);
        target.AddStagger(5);

        // effective_attack = 50 (attackGang=30 isn't used directly here, effective is passed in)
        int damage = StaggerService.ExecuteDecisiveStrike(attacker, target, 50f);

        Assert.Equal(80, damage); // (50-10)×2
        Assert.Equal(20, target.HP); // 100-80
        Assert.Equal(0, target.Stagger); // 清空
    }

    [Fact]
    public void ExecuteDecisive_ThrowsIfNotExposed()
    {
        var attacker = MakeCombatant();
        var target = MakeCombatant();
        target.AddStagger(3); // below threshold

        Assert.Throws<InvalidOperationException>(
            () => StaggerService.ExecuteDecisiveStrike(attacker, target, 50f));
    }

    // --- 衰减 ---

    [Fact]
    public void Decay_ReducesByOne()
    {
        var target = MakeCombatant();
        target.AddStagger(3);
        target.DecayStagger();
        Assert.Equal(2, target.Stagger);
    }

    [Fact]
    public void Decay_ClampsToZero()
    {
        var target = MakeCombatant();
        target.DecayStagger();
        Assert.Equal(0, target.Stagger);
    }

    // --- 协同 ---

    [Fact]
    public void CoopBonus_AddsOneStagger()
    {
        var target = MakeCombatant();
        target.AddStagger(2);
        StaggerService.ApplyCoopBonus(target);
        Assert.Equal(3, target.Stagger);
    }

    // --- ClearStagger ---

    [Fact]
    public void ClearStagger_ResetsToZero()
    {
        var target = MakeCombatant();
        target.AddStagger(7);
        target.ClearStagger();
        Assert.Equal(0, target.Stagger);
    }
}
