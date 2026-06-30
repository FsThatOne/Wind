using FengZhi.Foundation.Epiphany;
using Xunit;

namespace Foundation.Tests.Epiphany;

public class CombatEpiphanyCheckerTests
{
    [Fact]
    public void ComputeChance_At20PercentHp_Returns062()
    {
        var checker = new CombatEpiphanyChecker();
        float chance = checker.ComputeChance(0.20f, skipCount: 0);
        Assert.Equal(0.62f, chance, 2);
    }

    [Fact]
    public void ComputeChance_At1PercentHp_HitsChanceCap()
    {
        var checker = new CombatEpiphanyChecker();
        float chance = checker.ComputeChance(0.01f, skipCount: 0);
        Assert.Equal(0.70f, chance, 2);
    }

    [Fact]
    public void ComputeChance_AfterOneSkip_ReducesBase()
    {
        var checker = new CombatEpiphanyChecker();
        float chance0 = checker.ComputeChance(0.20f, skipCount: 0);
        float chance1 = checker.ComputeChance(0.20f, skipCount: 1);
        Assert.True(chance1 < chance0);
    }

    [Fact]
    public void ComputeChance_AfterMaxSkips_ReturnsZeroOrNear()
    {
        var checker = new CombatEpiphanyChecker { SkipChanceDecay = 0.15f };
        float chance = checker.ComputeChance(0.20f, skipCount: 7);
        Assert.True(chance <= 0f || chance < 0.1f);
    }

    [Fact]
    public void MeetsCombatConditions_AllMet_ReturnsTrue()
    {
        var checker = new CombatEpiphanyChecker();
        var condition = new CombatTriggerCondition { HpThreshold = 0.2f, MinActorActions = 5 };
        var context = new FakeCombatContext { HpRatio = 0.15f, ActorActions = 6 };

        Assert.True(checker.MeetsCombatConditions(condition, context, "player"));
    }

    [Fact]
    public void MeetsCombatConditions_HpTooHigh_ReturnsFalse()
    {
        var checker = new CombatEpiphanyChecker();
        var condition = new CombatTriggerCondition { HpThreshold = 0.2f, MinActorActions = 5 };
        var context = new FakeCombatContext { HpRatio = 0.50f, ActorActions = 6 };

        Assert.False(checker.MeetsCombatConditions(condition, context, "player"));
    }

    [Fact]
    public void MeetsCombatConditions_AlreadyTriggered_ReturnsFalse()
    {
        var checker = new CombatEpiphanyChecker();
        var condition = new CombatTriggerCondition { HpThreshold = 0.2f, MinActorActions = 5 };
        var context = new FakeCombatContext { HpRatio = 0.1f, ActorActions = 10, AlreadyTriggered = true };

        Assert.False(checker.MeetsCombatConditions(condition, context, "player"));
    }

    [Fact]
    public void Roll_WithFixedRng_Deterministic()
    {
        var checker = new CombatEpiphanyChecker();
        var rng = new Random(42);
        bool result = checker.Roll(0.99f, rng);
        Assert.True(result);
    }
}

public class FakeCombatContext : IEpiphanyCombatContext
{
    public float HpRatio { get; set; } = 0.15f;
    public int ActorActions { get; set; } = 6;
    public int QiHits { get; set; }
    public bool AlreadyTriggered { get; set; }
    public bool Paused { get; set; }

    public float GetCurrentHpRatio(string characterId) => HpRatio;
    public int GetActorActionsCompleted(string characterId) => ActorActions;
    public int GetConsecutiveQiDisadvantageHits(string characterId) => QiHits;
    public bool HasTriggeredEpiphanyThisBattle() => AlreadyTriggered;
    public void MarkEpiphanyTriggered() => AlreadyTriggered = true;
    public void PauseCombat() => Paused = true;
    public void ResumeCombat() => Paused = false;
}
