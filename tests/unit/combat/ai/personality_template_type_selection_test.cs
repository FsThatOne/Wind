using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-001: 性格模板与体系选择 (Phase A) 验证。
/// GDD AC#1: 刚猛型 1000 次模拟，刚系频率 57%-63%。
/// </summary>
public class PersonalityTemplateAndTypeSelectionTests
{
    // --- PersonalityTemplate 数据正确性 ---

    [Fact]
    public void FierceTemplate_HasCorrectWeights()
    {
        var t = PersonalityTemplate.Fierce;
        Assert.Equal("刚猛型", t.Name);
        Assert.Equal(60, t.WeightGang);
        Assert.Equal(15, t.WeightRou);
        Assert.Equal(25, t.WeightQiao);
        Assert.Equal(MoveType.Rou, t.WeaknessType);
        Assert.False(t.CounterReadEnabled);
    }

    [Fact]
    public void ResilientTemplate_HasCorrectWeights()
    {
        var t = PersonalityTemplate.Resilient;
        Assert.Equal("柔韧型", t.Name);
        Assert.Equal(15, t.WeightGang);
        Assert.Equal(55, t.WeightRou);
        Assert.Equal(30, t.WeightQiao);
        Assert.Equal(MoveType.Qiao, t.WeaknessType);
    }

    [Fact]
    public void CunningTemplate_HasCorrectWeights()
    {
        var t = PersonalityTemplate.Cunning;
        Assert.Equal("诡诈型", t.Name);
        Assert.Equal(25, t.WeightGang);
        Assert.Equal(20, t.WeightRou);
        Assert.Equal(55, t.WeightQiao);
        Assert.Equal(MoveType.Gang, t.WeaknessType);
    }

    [Fact]
    public void GetBaseWeight_ReturnsCorrectValues()
    {
        var t = PersonalityTemplate.Fierce;
        Assert.Equal(60, t.GetBaseWeight(MoveType.Gang));
        Assert.Equal(15, t.GetBaseWeight(MoveType.Rou));
        Assert.Equal(25, t.GetBaseWeight(MoveType.Qiao));
    }

    [Fact]
    public void NegativeWeight_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PersonalityTemplate("bad", -1, 50, 50, MoveType.Gang));
    }

    // --- TypeSelectionEngine 概率计算 ---

    [Fact]
    public void ComputeProbabilities_FierceTemplate_ReturnsNormalized()
    {
        var probs = TypeSelectionEngine.ComputeFromTemplate(PersonalityTemplate.Fierce);

        Assert.Equal(0.6f, probs.Gang, 0.001f);
        Assert.Equal(0.15f, probs.Rou, 0.001f);
        Assert.Equal(0.25f, probs.Qiao, 0.001f);
    }

    [Fact]
    public void ComputeProbabilities_ResilientTemplate_ReturnsNormalized()
    {
        var probs = TypeSelectionEngine.ComputeFromTemplate(PersonalityTemplate.Resilient);

        Assert.Equal(0.15f, probs.Gang, 0.001f);
        Assert.Equal(0.55f, probs.Rou, 0.001f);
        Assert.Equal(0.30f, probs.Qiao, 0.001f);
    }

    [Fact]
    public void ComputeProbabilities_CunningTemplate_ReturnsNormalized()
    {
        var probs = TypeSelectionEngine.ComputeFromTemplate(PersonalityTemplate.Cunning);

        Assert.Equal(0.25f, probs.Gang, 0.001f);
        Assert.Equal(0.20f, probs.Rou, 0.001f);
        Assert.Equal(0.55f, probs.Qiao, 0.001f);
    }

    [Fact]
    public void ComputeProbabilities_ClampingApplied_NegativeWeightClamped()
    {
        // 模拟极端修正导致负权重
        var weights = new TypeWeights { Gang = -10, Rou = 15, Qiao = 25 };
        var probs = TypeSelectionEngine.ComputeProbabilities(weights);

        // clamped: 5/15/25 → total=45
        Assert.Equal(5f / 45f, probs.Gang, 0.001f);
        Assert.Equal(15f / 45f, probs.Rou, 0.001f);
        Assert.Equal(25f / 45f, probs.Qiao, 0.001f);
    }

    [Fact]
    public void ComputeProbabilities_AllBelowMin_EqualDistribution()
    {
        var weights = new TypeWeights { Gang = -100, Rou = -50, Qiao = 0 };
        var probs = TypeSelectionEngine.ComputeProbabilities(weights);

        // All clamped to 5 → each 33.3%
        Assert.Equal(1f / 3f, probs.Gang, 0.001f);
        Assert.Equal(1f / 3f, probs.Rou, 0.001f);
        Assert.Equal(1f / 3f, probs.Qiao, 0.001f);
    }

    [Fact]
    public void ComputeProbabilities_ProbabilitiesSumToOne()
    {
        var probs = TypeSelectionEngine.ComputeFromTemplate(PersonalityTemplate.Fierce);
        float sum = probs.Gang + probs.Rou + probs.Qiao;
        Assert.Equal(1.0f, sum, 0.001f);
    }

    // --- SelectType 随机选择 ---

    [Fact]
    public void SelectType_RollBelowGang_ReturnsGang()
    {
        var probs = new TypeProbabilities { Gang = 0.6f, Rou = 0.15f, Qiao = 0.25f };
        var random = new FixedAIRandom(0.3f); // < 0.6
        Assert.Equal(MoveType.Gang, TypeSelectionEngine.SelectType(probs, random));
    }

    [Fact]
    public void SelectType_RollInRouRange_ReturnsRou()
    {
        var probs = new TypeProbabilities { Gang = 0.6f, Rou = 0.15f, Qiao = 0.25f };
        var random = new FixedAIRandom(0.7f); // >= 0.6, < 0.75
        Assert.Equal(MoveType.Rou, TypeSelectionEngine.SelectType(probs, random));
    }

    [Fact]
    public void SelectType_RollInQiaoRange_ReturnsQiao()
    {
        var probs = new TypeProbabilities { Gang = 0.6f, Rou = 0.15f, Qiao = 0.25f };
        var random = new FixedAIRandom(0.9f); // >= 0.75
        Assert.Equal(MoveType.Qiao, TypeSelectionEngine.SelectType(probs, random));
    }

    // --- GDD AC#1: 统计验证 ---

    [Fact]
    public void FierceTemplate_1000Rounds_GangFrequencyWithin57To63Percent()
    {
        var template = PersonalityTemplate.Fierce;
        var probs = TypeSelectionEngine.ComputeFromTemplate(template);
        var random = new SeededAIRandom(42);

        int gangCount = 0;
        const int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var selected = TypeSelectionEngine.SelectType(probs, random);
            if (selected == MoveType.Gang) gangCount++;
        }

        float frequency = gangCount / (float)trials;
        Assert.InRange(frequency, 0.57f, 0.63f);
    }

    [Fact]
    public void ResilientTemplate_1000Rounds_RouFrequencyWithin52To58Percent()
    {
        var template = PersonalityTemplate.Resilient;
        var probs = TypeSelectionEngine.ComputeFromTemplate(template);
        var random = new SeededAIRandom(123);

        int rouCount = 0;
        const int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var selected = TypeSelectionEngine.SelectType(probs, random);
            if (selected == MoveType.Rou) rouCount++;
        }

        float frequency = rouCount / (float)trials;
        Assert.InRange(frequency, 0.52f, 0.58f);
    }

    [Fact]
    public void CunningTemplate_1000Rounds_QiaoFrequencyWithin52To58Percent()
    {
        var template = PersonalityTemplate.Cunning;
        var probs = TypeSelectionEngine.ComputeFromTemplate(template);
        var random = new SeededAIRandom(999);

        int qiaoCount = 0;
        const int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var selected = TypeSelectionEngine.SelectType(probs, random);
            if (selected == MoveType.Qiao) qiaoCount++;
        }

        float frequency = qiaoCount / (float)trials;
        Assert.InRange(frequency, 0.52f, 0.58f);
    }

    // --- TypeWeights 辅助 ---

    [Fact]
    public void TypeWeights_GetAndWith_WorkCorrectly()
    {
        var w = new TypeWeights { Gang = 60, Rou = 15, Qiao = 25 };
        Assert.Equal(60, w.Get(MoveType.Gang));
        Assert.Equal(15, w.Get(MoveType.Rou));
        Assert.Equal(25, w.Get(MoveType.Qiao));

        var modified = w.With(MoveType.Gang, 40);
        Assert.Equal(40, modified.Gang);
        Assert.Equal(15, modified.Rou);
    }
}

/// <summary>
/// 测试用固定值随机源。
/// </summary>
internal sealed class FixedAIRandom : IAIRandomSource
{
    private readonly float _value;
    public FixedAIRandom(float value) => _value = value;
    public float NextFloat() => _value;
}
