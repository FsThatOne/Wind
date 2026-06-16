using FengZhi.Foundation.Combat;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

internal sealed class FixedInsightRandom : IInsightRandomSource
{
    public float Roll { get; set; } = 0.5f;
    public float NextRoll() => Roll;
}

public class IntentAndInsightTest
{
    private readonly FixedInsightRandom _rng = new();

    // --- 功力比 > 1.5：完全看破 ---

    [Theory]
    [InlineData(1.51f)]
    [InlineData(2.0f)]
    [InlineData(5.0f)]
    public void PowerRatio_Above15_FullReveal(float ratio)
    {
        var result = IntentInsightSystem.DetermineVisibility(ratio, 0, _rng);
        Assert.Equal(IntentVisibility.FullReveal, result);
    }

    // --- 功力比 1.0-1.5：总是 Normal ---

    [Theory]
    [InlineData(1.0f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void PowerRatio_1to15_Normal(float ratio)
    {
        var result = IntentInsightSystem.DetermineVisibility(ratio, 0, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    // --- 功力比 0.7-1.0：80% base_chance ---

    [Fact]
    public void PowerRatio_07to10_RollBelow80_Normal()
    {
        _rng.Roll = 0.79f; // < 0.80 → Normal
        var result = IntentInsightSystem.DetermineVisibility(0.8f, 0, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    [Fact]
    public void PowerRatio_07to10_RollAbove80_Hidden()
    {
        _rng.Roll = 0.81f; // >= 0.80 → Hidden
        var result = IntentInsightSystem.DetermineVisibility(0.8f, 0, _rng);
        Assert.Equal(IntentVisibility.Hidden, result);
    }

    // --- 功力比 0.5-0.7：50% base_chance ---

    [Fact]
    public void PowerRatio_05to07_RollBelow50_Normal()
    {
        _rng.Roll = 0.49f;
        var result = IntentInsightSystem.DetermineVisibility(0.6f, 0, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    [Fact]
    public void PowerRatio_05to07_RollAbove50_Hidden()
    {
        _rng.Roll = 0.51f;
        var result = IntentInsightSystem.DetermineVisibility(0.6f, 0, _rng);
        Assert.Equal(IntentVisibility.Hidden, result);
    }

    // --- 功力比 < 0.5：30% base_chance ---

    [Fact]
    public void PowerRatio_Below05_RollBelow30_Normal()
    {
        _rng.Roll = 0.29f;
        var result = IntentInsightSystem.DetermineVisibility(0.3f, 0, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    [Fact]
    public void PowerRatio_Below05_RollAbove30_Hidden()
    {
        _rng.Roll = 0.31f;
        var result = IntentInsightSystem.DetermineVisibility(0.3f, 0, _rng);
        Assert.Equal(IntentVisibility.Hidden, result);
    }

    // --- 洞察加成 ---

    [Fact]
    public void Insight_AddsPercentBonus()
    {
        // base=30%, insight=10 → +10% = 40%
        _rng.Roll = 0.35f; // < 0.40 → Normal
        var result = IntentInsightSystem.DetermineVisibility(0.3f, 10, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    [Fact]
    public void Insight_CapsAt100Percent()
    {
        // base=80%, insight=50 → +50% = 130% capped to 100%
        _rng.Roll = 0.99f; // < 1.0 → always Normal
        var result = IntentInsightSystem.DetermineVisibility(0.8f, 50, _rng);
        Assert.Equal(IntentVisibility.Normal, result);
    }

    // --- GetBaseChance ---

    [Fact]
    public void GetBaseChance_Values()
    {
        Assert.Equal(1.00f, IntentInsightSystem.GetBaseChance(1.2f));
        Assert.Equal(0.80f, IntentInsightSystem.GetBaseChance(0.8f));
        Assert.Equal(0.50f, IntentInsightSystem.GetBaseChance(0.6f));
        Assert.Equal(0.30f, IntentInsightSystem.GetBaseChance(0.3f));
    }
}
