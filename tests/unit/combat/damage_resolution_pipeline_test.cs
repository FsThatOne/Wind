using FengZhi.Foundation.Combat;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

/// <summary>
/// 固定随机源，用于确定性测试。
/// </summary>
internal sealed class FixedDamageRandom : IDamageRandomSource
{
    public float Variance { get; set; } = 1.0f; // 无波动
    public float CritRoll { get; set; } = 1.0f; // 不暴击（roll >= critRate）

    public float NextVariance() => Variance;
    public float NextCritRoll() => CritRoll;
}

public class DamageResolutionPipelineTest
{
    private readonly FixedDamageRandom _rng = new();

    private DamageInput MakeInput(
        float attackForType = 50f,
        float baseMultiplier = 1.0f,
        float completion = 1.0f,
        float realmScaling = 1.0f,
        float defense = 10f,
        CounterRelation counter = CounterRelation.Neutral,
        float critRate = 0f)
        => new()
        {
            AttackForType = attackForType,
            BaseMultiplier = baseMultiplier,
            Completion = completion,
            RealmScaling = realmScaling,
            Defense = defense,
            CounterRelation = counter,
            CritRate = critRate
        };

    // --- F1: effective_attack ---

    [Fact]
    public void F1_EffectiveAttack_FullFormula()
    {
        // 50 × 0.8 × 0.88 × 1.3 = 45.76
        var input = MakeInput(attackForType: 50, baseMultiplier: 0.8f, completion: 0.88f, realmScaling: 1.3f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(45.76f, output.EffectiveAttack, precision: 2);
    }

    [Fact]
    public void F1_EffectiveAttack_SimpleCase()
    {
        // 50 × 1.0 × 1.0 × 1.0 = 50
        var input = MakeInput(attackForType: 50);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(50f, output.EffectiveAttack, precision: 2);
    }

    // --- F2: base_damage ---

    [Fact]
    public void F2_BaseDamage_NormalCase()
    {
        // effective_attack=50 - defense=10 = 40
        var input = MakeInput(attackForType: 50, defense: 10);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(40f, output.BaseDamage, precision: 2);
    }

    [Fact]
    public void F2_BaseDamage_MinClampsTo1()
    {
        // effective_attack=5 - defense=100 → clamped to 1
        var input = MakeInput(attackForType: 5, defense: 100);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(1f, output.BaseDamage, precision: 2);
    }

    [Fact]
    public void F2_BaseDamage_DefenseEqualsAttack()
    {
        var input = MakeInput(attackForType: 30, defense: 30);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(1f, output.BaseDamage, precision: 2);
    }

    // --- F3: damage variance ---

    [Fact]
    public void F3_Variance_LowEnd()
    {
        _rng.Variance = 0.95f;
        var input = MakeInput(attackForType: 50, defense: 10); // base=40
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(38f, output.ActualDamage, precision: 2); // 40 × 0.95
    }

    [Fact]
    public void F3_Variance_HighEnd()
    {
        _rng.Variance = 1.05f;
        var input = MakeInput(attackForType: 50, defense: 10); // base=40
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(42f, output.ActualDamage, precision: 2); // 40 × 1.05
    }

    // --- F4: counter multiplier ---

    [Fact]
    public void F4_Counter_Advantage()
    {
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Advantage);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // actual=40, final = 40 × 1.3 = 52
        Assert.Equal(52, output.FinalDamage);
        Assert.Equal(1.3f, output.CounterMultiplier, precision: 2);
    }

    [Fact]
    public void F4_Counter_Disadvantage()
    {
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Disadvantage);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // actual=40, final = 40 × 0.7 = 28
        Assert.Equal(28, output.FinalDamage);
        Assert.Equal(0.7f, output.CounterMultiplier, precision: 2);
    }

    [Fact]
    public void F4_Counter_Neutral()
    {
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Neutral);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(40, output.FinalDamage);
        Assert.Equal(1.0f, output.CounterMultiplier, precision: 2);
    }

    // --- F4: crit ---

    [Fact]
    public void F4_Crit_Triggers()
    {
        _rng.CritRoll = 0.05f; // roll < critRate → crit
        var input = MakeInput(attackForType: 50, defense: 10, critRate: 0.1f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // actual=40, final = 40 × 1.0 × 1.5 = 60
        Assert.Equal(60, output.FinalDamage);
        Assert.True(output.IsCrit);
        Assert.Equal(1.5f, output.CritMultiplier, precision: 2);
    }

    [Fact]
    public void F4_Crit_NoCrit()
    {
        _rng.CritRoll = 0.5f; // roll >= critRate → no crit
        var input = MakeInput(attackForType: 50, defense: 10, critRate: 0.1f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        Assert.Equal(40, output.FinalDamage);
        Assert.False(output.IsCrit);
        Assert.Equal(1.0f, output.CritMultiplier, precision: 2);
    }

    // --- Combined scenarios ---

    [Fact]
    public void Combined_Advantage_Crit()
    {
        _rng.CritRoll = 0.01f;
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Advantage, critRate: 0.1f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // actual=40, final = 40 × 1.3 × 1.5 = 78
        Assert.Equal(78, output.FinalDamage);
        Assert.True(output.IsCrit);
    }

    [Fact]
    public void Combined_Disadvantage_Crit()
    {
        _rng.CritRoll = 0.01f;
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Disadvantage, critRate: 0.1f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // actual=40, final = 40 × 0.7 × 1.5 = 42
        Assert.Equal(42, output.FinalDamage);
    }

    [Fact]
    public void Combined_Variance_Counter_Crit()
    {
        _rng.Variance = 1.05f;
        _rng.CritRoll = 0.01f;
        var input = MakeInput(attackForType: 50, defense: 10, counter: CounterRelation.Advantage, critRate: 0.1f);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // base=40, actual=40×1.05=42, final = 42×1.3×1.5 = 81.9 → 82
        Assert.Equal(82, output.FinalDamage);
    }

    [Fact]
    public void FinalDamage_MinClampsTo1()
    {
        // Even with disadvantage and variance, final should be >= 1
        _rng.Variance = 0.95f;
        var input = MakeInput(attackForType: 1, defense: 100, counter: CounterRelation.Disadvantage);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // base=1(clamped), actual=0.95, final = 0.95×0.7 = 0.665 → clamped to 1
        Assert.Equal(1, output.FinalDamage);
    }

    [Fact]
    public void GDD_Example_RealmAndCompletion()
    {
        // 模拟 GDD 场景：攻击力30，招式倍率0.8，完整度0.88(完本)，境界1.3(炉火纯青)，防御15
        _rng.Variance = 1.0f;
        var input = MakeInput(
            attackForType: 30, baseMultiplier: 0.8f, completion: 0.88f,
            realmScaling: 1.3f, defense: 15, counter: CounterRelation.Advantage);
        var output = DamageResolutionPipeline.Resolve(input, _rng);
        // effective = 30 × 0.8 × 0.88 × 1.3 = 27.456
        // base = max(1, 27.456 - 15) = 12.456
        // actual = 12.456 × 1.0 = 12.456
        // final = 12.456 × 1.3 = 16.19 → 16
        Assert.Equal(27.456f, output.EffectiveAttack, precision: 2);
        Assert.Equal(12.456f, output.BaseDamage, precision: 2);
        Assert.Equal(16, output.FinalDamage);
    }
}
