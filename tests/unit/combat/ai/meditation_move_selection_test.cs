using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-004: 调息决策与选招逻辑验证。
/// GDD AC#7: 内息=1 → 调息概率50%, 1000次在469-531范围内。
/// GDD AC#16: 体系无可用招式时回退。
/// GDD AC#19: 连续3回合调息后强制跳过。
/// </summary>
public class MeditationAndMoveSelectionTests
{
    // --- MeditationDecision ---

    [Fact]
    public void ShouldMeditate_NeixiAboveThreshold_ReturnsFalse()
    {
        var random = new FixedAIRandom(0.0f); // 即使随机值=0也不触发
        Assert.False(MeditationDecision.ShouldMeditate(3, random, 0));
    }

    [Fact]
    public void ComputeChance_NeixiEqualsThreshold_Returns40Percent()
    {
        float chance = MeditationDecision.ComputeChance(currentNeixi: 2, threshold: 2);
        Assert.Equal(0.4f, chance, 0.001f);
    }

    [Fact]
    public void ComputeChance_Neixi1_Returns50Percent()
    {
        float chance = MeditationDecision.ComputeChance(currentNeixi: 1, threshold: 2);
        Assert.Equal(0.5f, chance, 0.001f); // 0.4 + (2-1)×0.1 = 0.5
    }

    [Fact]
    public void ComputeChance_Neixi0_Returns60Percent()
    {
        float chance = MeditationDecision.ComputeChance(currentNeixi: 0, threshold: 2);
        Assert.Equal(0.6f, chance, 0.001f); // 0.4 + (2-0)×0.1 = 0.6
    }

    [Fact]
    public void ComputeChance_HighThreshold_CapsAt100()
    {
        float chance = MeditationDecision.ComputeChance(currentNeixi: 0, threshold: 4, baseChance: 0.7f);
        Assert.Equal(1.0f, chance, 0.001f); // min(1.0, 0.7 + 0.4) = 1.0
    }

    [Fact]
    public void ShouldMeditate_Neixi1_1000Trials_InRange469To531()
    {
        // GDD AC#7
        var random = new SeededAIRandom(42);
        int meditateCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (MeditationDecision.ShouldMeditate(1, random, 0))
                meditateCount++;
        }
        Assert.InRange(meditateCount, 469, 531);
    }

    [Fact]
    public void ShouldMeditate_DeadlockProtection_ForcedSkipAfter3()
    {
        // GDD AC#19: 连续3回合后强制跳过
        var random = new FixedAIRandom(0.0f); // 总是触发
        Assert.True(MeditationDecision.ShouldMeditate(0, random, 0));
        Assert.True(MeditationDecision.ShouldMeditate(0, random, 1));
        Assert.True(MeditationDecision.ShouldMeditate(0, random, 2));
        Assert.False(MeditationDecision.ShouldMeditate(0, random, 3)); // 强制跳过
    }

    // --- MoveSelector ---

    private static readonly List<AIMoveEntry> TestMoves = new()
    {
        new AIMoveEntry { Id = "gang1", Name = "猛虎下山", Type = MoveType.Gang, NeixiCost = 2, DamageMultiplier = 1.2f },
        new AIMoveEntry { Id = "gang2", Name = "开山掌", Type = MoveType.Gang, NeixiCost = 3, DamageMultiplier = 1.4f },
        new AIMoveEntry { Id = "rou1", Name = "绵掌", Type = MoveType.Rou, NeixiCost = 2, DamageMultiplier = 1.0f },
        new AIMoveEntry { Id = "qiao1", Name = "燕子穿林", Type = MoveType.Qiao, NeixiCost = 1, DamageMultiplier = 0.8f },
        new AIMoveEntry { Id = "qiao2", Name = "暗器连发", Type = MoveType.Qiao, NeixiCost = 4, DamageMultiplier = 1.5f },
    };

    [Fact]
    public void SelectMove_TypeHasAffordableMoves_PicksFromType()
    {
        var random = new FixedAIRandom(0.0f);
        var move = MoveSelector.SelectMove(TestMoves, MoveType.Gang, currentNeixi: 5, random);
        Assert.NotNull(move);
        Assert.Equal(MoveType.Gang, move.Type);
    }

    [Fact]
    public void SelectMove_TypeNoAffordableMoves_FallsBackToOtherTypes()
    {
        // 选定Gang，但只有1内息，Gang招都不够（2和3）
        var random = new FixedAIRandom(0.0f);
        var move = MoveSelector.SelectMove(TestMoves, MoveType.Gang, currentNeixi: 1, random);
        Assert.NotNull(move);
        // 应该 fallback 到 qiao1 (cost=1)
        Assert.Equal("qiao1", move.Id);
    }

    [Fact]
    public void SelectMove_NoAffordableMoves_ReturnsNull()
    {
        // GDD AC#16
        var expensiveMoves = new List<AIMoveEntry>
        {
            new() { Id = "x", Type = MoveType.Gang, NeixiCost = 10 }
        };
        var random = new FixedAIRandom(0.5f);
        var move = MoveSelector.SelectMove(expensiveMoves, MoveType.Gang, currentNeixi: 1, random);
        Assert.Null(move); // 无招可用 → 应调息
    }

    [Fact]
    public void SelectMove_MultipleCandidates_RandomPicks()
    {
        // 有两个 Gang 招可用（neixi=5 足够二者），验证随机选择
        var random = new FixedAIRandom(0.99f); // high roll → second candidate
        var move = MoveSelector.SelectMove(TestMoves, MoveType.Gang, currentNeixi: 5, random);
        Assert.NotNull(move);
        Assert.Equal(MoveType.Gang, move.Type);
        // index = floor(0.99 * 2) = 1, min(1,1) → "gang2"
        Assert.Equal("gang2", move.Id);
    }

    [Fact]
    public void ShouldUseZeroCostMove_80PercentChance()
    {
        // 验证 80% 概率使用零消耗招式
        var random = new SeededAIRandom(42);
        int useMove = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (MoveSelector.ShouldUseZeroCostMove(random)) useMove++;
        }
        Assert.InRange(useMove, 770, 830); // ~80% ± margin
    }
}
