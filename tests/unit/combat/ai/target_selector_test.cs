using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-006: 目标选择 (F3) 验证。
/// GDD AC#6: 破绽达标目标得分 ≥ 100，满血目标得分 = 0。
/// </summary>
public class TargetSelectorTests
{
    private static readonly IReadOnlySet<string> NoTargeted = new HashSet<string>();

    // --- ComputeScore ---

    [Fact]
    public void ComputeScore_DecisiveTarget_Returns100()
    {
        var target = new TargetCandidate
        {
            Id = "A", CurrentStagger = 5, StaggerThreshold = 5,
            HpRatio = 0.8f, LastRoundDamageToSelf = 0
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        Assert.Equal(100, score);
    }

    [Fact]
    public void ComputeScore_FullHpNoDamage_ReturnsZero()
    {
        var target = new TargetCandidate
        {
            Id = "B", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 1.0f, LastRoundDamageToSelf = 0
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        Assert.Equal(0, score);
    }

    [Fact]
    public void ComputeScore_LowHp_AddsKillBonus()
    {
        var target = new TargetCandidate
        {
            Id = "C", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 0.25f, LastRoundDamageToSelf = 0
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        Assert.Equal(50, score); // kill_bonus only
    }

    [Fact]
    public void ComputeScore_HighDamage_AddsThreatCapped()
    {
        var target = new TargetCandidate
        {
            Id = "D", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 0.9f, LastRoundDamageToSelf = 200
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        // threat = min(50, floor(200 × 0.3)) = min(50, 60) = 50
        Assert.Equal(50, score);
    }

    [Fact]
    public void ComputeScore_ThreatBelowCap_UsesRawValue()
    {
        var target = new TargetCandidate
        {
            Id = "E", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 0.9f, LastRoundDamageToSelf = 100
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        // threat = min(50, floor(100 × 0.3)) = min(50, 30) = 30
        Assert.Equal(30, score);
    }

    [Fact]
    public void ComputeScore_SpreadPenalty_WhenAlreadyTargetedAndNotDecisive()
    {
        var target = new TargetCandidate
        {
            Id = "F", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 0.5f, LastRoundDamageToSelf = 0
        };
        var targeted = new HashSet<string> { "F" };
        int score = TargetSelector.ComputeScore(target, targeted);
        Assert.Equal(-20, score); // spread_penalty only
    }

    [Fact]
    public void ComputeScore_SpreadPenalty_IgnoredIfDecisive()
    {
        var target = new TargetCandidate
        {
            Id = "G", CurrentStagger = 5, StaggerThreshold = 5,
            HpRatio = 0.5f, LastRoundDamageToSelf = 0
        };
        var targeted = new HashSet<string> { "G" };
        int score = TargetSelector.ComputeScore(target, targeted);
        // decisive = 100, no spread penalty (is decisive)
        Assert.Equal(100, score);
    }

    [Fact]
    public void ComputeScore_AllBonusesCombined()
    {
        var target = new TargetCandidate
        {
            Id = "H", CurrentStagger = 5, StaggerThreshold = 5,
            HpRatio = 0.2f, LastRoundDamageToSelf = 200
        };
        int score = TargetSelector.ComputeScore(target, NoTargeted);
        // decisive(100) + kill(50) + threat(min(50,60)=50) = 200
        Assert.Equal(200, score);
    }

    // --- GDD AC#6 ---

    [Fact]
    public void AC6_DecisiveTarget_ScoreAtLeast100_FullHpTarget_ScoreZero()
    {
        var targetA = new TargetCandidate
        {
            Id = "A", CurrentStagger = 5, StaggerThreshold = 5,
            HpRatio = 0.8f, LastRoundDamageToSelf = 0
        };
        var targetB = new TargetCandidate
        {
            Id = "B", CurrentStagger = 0, StaggerThreshold = 5,
            HpRatio = 1.0f, LastRoundDamageToSelf = 0
        };

        int scoreA = TargetSelector.ComputeScore(targetA, NoTargeted);
        int scoreB = TargetSelector.ComputeScore(targetB, NoTargeted);

        Assert.True(scoreA >= 100);
        Assert.Equal(0, scoreB);
    }

    // --- SelectTarget ---

    [Fact]
    public void SelectTarget_EmptyCandidates_ReturnsNull()
    {
        var random = new FixedAIRandom(0.5f);
        var result = TargetSelector.SelectTarget(Array.Empty<TargetCandidate>(), NoTargeted, random);
        Assert.Null(result);
    }

    [Fact]
    public void SelectTarget_ClearWinner_SelectsHighestScore()
    {
        var candidates = new[]
        {
            new TargetCandidate { Id = "low", CurrentStagger = 0, HpRatio = 1.0f },
            new TargetCandidate { Id = "high", CurrentStagger = 5, StaggerThreshold = 5, HpRatio = 0.5f }
        };
        var random = new FixedAIRandom(0.5f);
        var result = TargetSelector.SelectTarget(candidates, NoTargeted, random);
        Assert.Equal("high", result!.Id);
    }

    [Fact]
    public void SelectTarget_TiedScores_RandomBreaksTie()
    {
        var candidates = new[]
        {
            new TargetCandidate { Id = "t1", CurrentStagger = 0, HpRatio = 1.0f },
            new TargetCandidate { Id = "t2", CurrentStagger = 0, HpRatio = 1.0f }
        };
        // 两个都是 score=0，随机选
        var random1 = new FixedAIRandom(0.0f);
        var result1 = TargetSelector.SelectTarget(candidates, NoTargeted, random1);
        Assert.Equal("t1", result1!.Id);

        var random2 = new FixedAIRandom(0.99f);
        var result2 = TargetSelector.SelectTarget(candidates, NoTargeted, random2);
        Assert.Equal("t2", result2!.Id);
    }

    [Fact]
    public void SelectTarget_SpreadDiversifiesTargets()
    {
        // 两个满血无伤目标，t1 已被队友攻击
        var candidates = new[]
        {
            new TargetCandidate { Id = "t1", CurrentStagger = 0, HpRatio = 1.0f },
            new TargetCandidate { Id = "t2", CurrentStagger = 0, HpRatio = 1.0f }
        };
        var targeted = new HashSet<string> { "t1" };
        var random = new FixedAIRandom(0.5f);
        var result = TargetSelector.SelectTarget(candidates, targeted, random);
        // t1 score = -20, t2 score = 0 → 选 t2
        Assert.Equal("t2", result!.Id);
    }

    [Fact]
    public void SelectTarget_DecisiveOverridesSpread()
    {
        // t1 破绽达标但已被队友攻击（但 decisive 豁免 spread）
        var candidates = new[]
        {
            new TargetCandidate { Id = "t1", CurrentStagger = 5, StaggerThreshold = 5, HpRatio = 0.8f },
            new TargetCandidate { Id = "t2", CurrentStagger = 0, HpRatio = 1.0f }
        };
        var targeted = new HashSet<string> { "t1" };
        var random = new FixedAIRandom(0.5f);
        var result = TargetSelector.SelectTarget(candidates, targeted, random);
        // t1 = 100 (no spread penalty), t2 = 0
        Assert.Equal("t1", result!.Id);
    }
}
