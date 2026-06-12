using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.unit.martial_arts;

public sealed class MoveDamageAndScalingTest
{
    // === AC-1：真传招式 + 炉火纯青 ===

    [Fact]
    public void Calculate_MasteredMove_LuHuoChunQing_Returns45Point5()
    {
        // base_multiplier=1.0, attack=35, completion=1.0, realm=炉火纯青(1.3)
        // 1.0 × 35 × 1.0 × 1.3 = 45.5
        var input = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (output, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.None, error);
        Assert.Equal(1.0f, output.EffectiveBaseMultiplier, precision: 2);
        Assert.Equal(1.3f, output.RealmScalingUsed, precision: 2);
        Assert.Equal(45.5f, output.TotalDamage, precision: 1);
    }

    [Fact]
    public void Calculate_FragmentMove_ChuXueZhaLian_LowerOutput()
    {
        // base_multiplier=0.6, attack=20, completion=0.55, realm=初学乍练(0.7)
        // 0.6 × 20 × 0.55 × 0.7 = 4.62
        var input = new MoveDamageInput
        {
            BaseMultiplier = 0.6f,
            AnnotationMult = 1.0f,
            AttackForType = 20f,
            Completion = 0.55f,
            Realm = RealmTier.ChuXueZhaLian
        };

        var (output, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.None, error);
        Assert.Equal(4.62f, output.TotalDamage, precision: 1);
    }

    // === AC-2：批注倍率 ===

    [Fact]
    public void Calculate_AnnotatedBasicMove_EffectiveBaseIs1Point26()
    {
        // base_multiplier=0.7, annotation_mult=1.8 → effective_base=1.26
        var input = new MoveDamageInput
        {
            BaseMultiplier = 0.7f,
            AnnotationMult = 1.8f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (output, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.None, error);
        Assert.Equal(1.26f, output.EffectiveBaseMultiplier, precision: 2);
    }

    [Fact]
    public void Calculate_AnnotatedBasicMove_HigherThanNonAnnotatedAdvanced()
    {
        // 批注版普通: base=0.7, annotation=1.8, attack=35, completion=1.0, realm=炉火纯青
        // 0.7×1.8 × 35 × 1.0 × 1.3 = 57.33
        var annotated = new MoveDamageInput
        {
            BaseMultiplier = 0.7f,
            AnnotationMult = 1.8f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        // 未批注高级武学(真传): base=1.1, attack=35, completion=1.0, realm=炉火纯青
        // 1.1 × 35 × 1.0 × 1.3 = 50.05
        var advanced = new MoveDamageInput
        {
            BaseMultiplier = 1.1f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (annotatedOut, _) = MoveDamageCalculator.Calculate(annotated);
        var (advancedOut, _) = MoveDamageCalculator.Calculate(advanced);

        Assert.True(annotatedOut.TotalDamage > advancedOut.TotalDamage,
            $"批注版 {annotatedOut.TotalDamage} 应 > 高级 {advancedOut.TotalDamage}");
    }

    // === AC-3：返璞归真 + 批注版最高值 ===

    [Fact]
    public void Calculate_AnnotatedMove_FanPuGuiZhen_BothMultipliersStack()
    {
        // base=0.7, annotation=1.8, attack=35, completion=1.0, realm=返璞归真(2.0)
        // 0.7×1.8 × 35 × 1.0 × 2.0 = 88.2
        var input = new MoveDamageInput
        {
            BaseMultiplier = 0.7f,
            AnnotationMult = 1.8f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.FanPuGuiZhen
        };

        var (output, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.None, error);
        Assert.Equal(2.0f, output.RealmScalingUsed, precision: 2);
        Assert.Equal(88.2f, output.TotalDamage, precision: 1);
    }

    [Fact]
    public void Calculate_NonAnnotatedMastered_FanPuGuiZhen_LessThanAnnotated()
    {
        // 未批注真传: base=1.0, attack=35, completion=1.0, realm=返璞归真(2.0)
        // 1.0 × 35 × 1.0 × 2.0 = 70
        var nonAnnotated = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.FanPuGuiZhen
        };

        var annotated = new MoveDamageInput
        {
            BaseMultiplier = 0.7f,
            AnnotationMult = 1.8f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.FanPuGuiZhen
        };

        var (nonOut, _) = MoveDamageCalculator.Calculate(nonAnnotated);
        var (annOut, _) = MoveDamageCalculator.Calculate(annotated);

        Assert.Equal(70.0f, nonOut.TotalDamage, precision: 1);
        Assert.True(annOut.TotalDamage > nonOut.TotalDamage);
    }

    // === 校验：非法输入 ===

    [Fact]
    public void Calculate_ZeroBaseMultiplier_ReturnsError()
    {
        var input = new MoveDamageInput
        {
            BaseMultiplier = 0f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (_, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.InvalidBaseMultiplier, error);
    }

    [Fact]
    public void Calculate_NegativeAnnotationMult_ReturnsError()
    {
        var input = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = -1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (_, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.InvalidAnnotationMult, error);
    }

    [Fact]
    public void Calculate_CompletionAboveOne_ReturnsError()
    {
        var input = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.5f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (_, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.InvalidCompletion, error);
    }

    [Fact]
    public void Calculate_InvalidRealm_ReturnsError()
    {
        var input = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = 1.0f,
            AttackForType = 35f,
            Completion = 1.0f,
            Realm = (RealmTier)99
        };

        var (_, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.InvalidRealm, error);
    }

    [Fact]
    public void Calculate_ZeroAttack_ReturnsZeroDamage()
    {
        var input = new MoveDamageInput
        {
            BaseMultiplier = 1.0f,
            AnnotationMult = 1.0f,
            AttackForType = 0f,
            Completion = 1.0f,
            Realm = RealmTier.LuHuoChunQing
        };

        var (output, error) = MoveDamageCalculator.Calculate(input);

        Assert.Equal(DamageCalcError.None, error);
        Assert.Equal(0f, output.TotalDamage);
    }

    // === 境界缩放表完整性 ===

    [Theory]
    [InlineData(RealmTier.ChuXueZhaLian, 0.7f)]
    [InlineData(RealmTier.ChuKuiMenJing, 0.8f)]
    [InlineData(RealmTier.DengTangRuShi, 0.9f)]
    [InlineData(RealmTier.RongHuiGuanTong, 1.0f)]
    [InlineData(RealmTier.JiaQingJiuShu, 1.1f)]
    [InlineData(RealmTier.LuHuoChunQing, 1.3f)]
    [InlineData(RealmTier.ChuShenRuHua, 1.5f)]
    [InlineData(RealmTier.DengFengZaoJi, 1.8f)]
    [InlineData(RealmTier.FanPuGuiZhen, 2.0f)]
    public void RealmScaling_AllTiers_ReturnExpectedValue(RealmTier tier, float expected)
    {
        Assert.Equal(expected, RealmScaling.GetScaling(tier), precision: 2);
    }
}
