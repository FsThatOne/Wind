using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.unit.martial_arts;

public sealed class SpecialConditionEffectsTest
{
    // === 测试用招式定义 ===

    private static MoveDefinition LuoHanQuan() => new()
    {
        Id = "luo_han_quan",
        Name = "罗汉拳",
        Type = MoveType.Gang,
        Category = MoveCategory.Basic,
        NeixiCost = 2,
        BaseMultiplier = 0.7f,
        TriggerConditions = new List<string> { "second_counter_hit" },
        SpecialEffects = new List<string> { "flaw_expose" },
        Tags = new List<string> { "direct" },
        Annotatable = true,
        Annotation = new AnnotationDefinition
        {
            ConditionOverride = "first_counter_hit",
            EffectOverride = "flaw_expose_3",
            AnnotationMult = 1.8f
        }
    };

    private static MoveDefinition TianGangLiePo() => new()
    {
        Id = "tian_gang_lie_po",
        Name = "天罡裂魄",
        Type = MoveType.Gang,
        Category = MoveCategory.Ultimate,
        NeixiCost = 8,
        BaseMultiplier = 1.4f,
        TriggerConditions = new List<string> { "no_condition" },
        SpecialEffects = new List<string> { "shatter_guard", "devastate" },
        Tags = new List<string> { "direct" },
        Annotatable = true,
        Annotation = new AnnotationDefinition
        {
            ConditionOverride = "no_condition",
            EffectOverride = "shatter_guard_enhanced",
            AnnotationMult = 1.2f
        }
    };

    private static MoveDefinition TieBiHengLan() => new()
    {
        Id = "tie_bi_heng_lan",
        Name = "铁臂横拦",
        Type = MoveType.Gang,
        Category = MoveCategory.Basic,
        NeixiCost = 4,
        BaseMultiplier = 1.15f,
        TriggerConditions = new List<string> { "adjacent_enemy" },
        SpecialEffects = new List<string> { "stagger_bonus" },
        Tags = new List<string> { "direct" },
        Annotatable = false
    };

    // === AC-1：批注版条件覆盖 ===

    [Fact]
    public void Resolve_AnnotatedLuoHan_FirstCounterHit_TriggersFlawExpose3()
    {
        var move = LuoHanQuan();
        var progression = new MoveProgressionEntry("luo_han_quan", MoveProgressionStage.Annotated);
        var context = new BattleContext { CounterHitsSameTarget = 1, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        Assert.Single(result.Effects);
        Assert.Equal(EffectType.FlawExpose, result.Effects[0].Type);
        Assert.Equal(3, result.Effects[0].Parameter);
    }

    [Fact]
    public void Resolve_AnnotatedLuoHan_ZeroCounterHits_DoesNotTrigger()
    {
        var move = LuoHanQuan();
        var progression = new MoveProgressionEntry("luo_han_quan", MoveProgressionStage.Annotated);
        var context = new BattleContext { CounterHitsSameTarget = 0, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.False(result.Triggered);
    }

    // === AC-2：原版第 2 次克制 ===

    [Fact]
    public void Resolve_OriginalLuoHan_FirstCounterHit_DoesNotTrigger()
    {
        var move = LuoHanQuan();
        var progression = new MoveProgressionEntry("luo_han_quan", MoveProgressionStage.Mastered);
        var context = new BattleContext { CounterHitsSameTarget = 1, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Resolve_OriginalLuoHan_SecondCounterHit_TriggersFlawExpose1()
    {
        var move = LuoHanQuan();
        var progression = new MoveProgressionEntry("luo_han_quan", MoveProgressionStage.Mastered);
        var context = new BattleContext { CounterHitsSameTarget = 2, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        Assert.Single(result.Effects);
        Assert.Equal(EffectType.FlawExpose, result.Effects[0].Type);
        Assert.Equal(1, result.Effects[0].Parameter);
    }

    // === AC-3：绝学效果锁定 ===

    [Fact]
    public void Resolve_UltimateFragment_ConditionMet_EffectsLocked()
    {
        var move = TianGangLiePo();
        // 残卷状态 → 无特效解锁
        var progression = new MoveProgressionEntry("tian_gang_lie_po", MoveProgressionStage.Fragment);
        var context = new BattleContext { DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        // shatter_guard 需 EffectV1, devastate 需 EffectV2 → 都不解锁
        Assert.False(result.Triggered);
        Assert.Empty(result.Effects);
    }

    [Fact]
    public void Resolve_UltimateComplete_UnlocksV1Only()
    {
        var move = TianGangLiePo();
        // 完本 → EffectV1 解锁
        var progression = new MoveProgressionEntry("tian_gang_lie_po", MoveProgressionStage.Complete);
        var context = new BattleContext { DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        // shatter_guard (V1) 解锁, devastate (V2) 不解锁
        Assert.Single(result.Effects);
        Assert.Equal(EffectType.ShatterGuard, result.Effects[0].Type);
    }

    [Fact]
    public void Resolve_UltimateMastered_UnlocksV1AndV2()
    {
        var move = TianGangLiePo();
        // 真传 → EffectV1 + V2 全解锁
        var progression = new MoveProgressionEntry("tian_gang_lie_po", MoveProgressionStage.Mastered);
        var context = new BattleContext { DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        Assert.Equal(2, result.Effects.Count);
        Assert.Contains(result.Effects, e => e.Type == EffectType.ShatterGuard);
        Assert.Contains(result.Effects, e => e.Type == EffectType.Devastate);
    }

    // === 批注版效果覆盖（不与原版并行叠加）===

    [Fact]
    public void Resolve_AnnotatedUltimate_UsesOverrideNotOriginal()
    {
        var move = TianGangLiePo();
        var progression = new MoveProgressionEntry("tian_gang_lie_po", MoveProgressionStage.Annotated);
        var context = new BattleContext { DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        // 批注版只使用 effect_override: shatter_guard_enhanced（单效果替换）
        Assert.Single(result.Effects);
        Assert.Equal(EffectType.ShatterGuard, result.Effects[0].Type);
        Assert.Equal(1, result.Effects[0].Parameter); // enhanced 版参数=1
    }

    // === 基础条件判定 ===

    [Fact]
    public void Resolve_AdjacentEnemy_NotAdjacent_DoesNotTrigger()
    {
        var move = TieBiHengLan();
        var progression = new MoveProgressionEntry("tie_bi_heng_lan", MoveProgressionStage.Mastered);
        var context = new BattleContext { HasAdjacentEnemy = false, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Resolve_AdjacentEnemy_IsAdjacent_Triggers()
    {
        var move = TieBiHengLan();
        var progression = new MoveProgressionEntry("tie_bi_heng_lan", MoveProgressionStage.Mastered);
        var context = new BattleContext { HasAdjacentEnemy = true, DidHit = true };

        var result = SpecialEffectResolver.Resolve(move, progression, context);

        Assert.True(result.Triggered);
        Assert.Single(result.Effects);
        Assert.Equal(EffectType.StaggerBonus, result.Effects[0].Type);
    }

    // === TriggerCondition.Parse 边界 ===

    [Theory]
    [InlineData("always", ConditionType.Always, 0)]
    [InlineData("no_condition", ConditionType.Always, 0)]
    [InlineData("first_counter_hit", ConditionType.NthCounterHitSameTarget, 1)]
    [InlineData("second_counter_hit", ConditionType.NthCounterHitSameTarget, 2)]
    [InlineData("adjacent_enemy", ConditionType.AdjacentEnemy, 0)]
    [InlineData("target_not_immobilized", ConditionType.TargetNotImmobilized, 0)]
    public void TriggerCondition_Parse_AllKnownTags(string tag, ConditionType expectedType, int expectedParam)
    {
        var condition = TriggerCondition.Parse(tag);

        Assert.Equal(expectedType, condition.Type);
        Assert.Equal(expectedParam, condition.Parameter);
    }

    [Fact]
    public void TriggerCondition_Parse_UnknownTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => TriggerCondition.Parse("unknown_condition"));
    }

    // === SpecialEffect.Parse 边界 ===

    [Theory]
    [InlineData("flaw_expose", EffectType.FlawExpose, 1)]
    [InlineData("flaw_expose_3", EffectType.FlawExpose, 3)]
    [InlineData("guard_up", EffectType.GuardUp, 0)]
    [InlineData("shatter_guard", EffectType.ShatterGuard, 0)]
    [InlineData("devastate", EffectType.Devastate, 0)]
    [InlineData("multi_strike", EffectType.MultiStrike, 0)]
    public void SpecialEffect_Parse_AllKnownTags(string tag, EffectType expectedType, int expectedParam)
    {
        var effect = SpecialEffect.Parse(tag);

        Assert.Equal(expectedType, effect.Type);
        Assert.Equal(expectedParam, effect.Parameter);
    }

    [Fact]
    public void SpecialEffect_Parse_UnknownTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpecialEffect.Parse("unknown_effect"));
    }
}
