using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class CounterMechanismTest
{
    private readonly FixedDamageRandom _rng = new() { Variance = 1.0f, CritRoll = 1.0f };

    private static BattleCombatant MakeActor(int neixi = 50)
        => new("actor_1", "主角", 100, neixi, 30, 25, 20, 10, 5, 0.1f, 3, 5);

    private static BattleCombatant MakeTarget(int hp = 100, int defense = 10)
        => new("target_1", "敌人", hp, 40, 20, 25, 30, defense, 4, 0.05f, 1, 5);

    // --- 反制成功 ---

    [Fact]
    public void Counter_Success_DealsDamageWithAdvantage()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 50);
        var target = MakeTarget(hp: 100, defense: 10);

        // 柔克刚：actor uses Rou against target intent Gang
        var result = service.Execute(actor, target, MoveType.Rou, MoveType.Gang, _rng);

        Assert.Equal(CounterOutcome.Success, result.Outcome);
        // damage = (25-10) × 1.0(variance) × 1.3(advantage) × 1.0(no crit) = 19.5 → 20
        Assert.Equal(20, result.DamageDealt);
        Assert.Equal(80, target.HP);
    }

    [Fact]
    public void Counter_Success_Applies3Stagger()
    {
        var service = new CounterService();
        var actor = MakeActor();
        var target = MakeTarget();

        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, _rng);

        Assert.Equal(CounterOutcome.Success, result.Outcome);
        Assert.Equal(3, result.StaggerApplied);
        Assert.Equal(3, target.Stagger);
    }

    [Fact]
    public void Counter_Success_Costs3Neixi()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 10);
        var target = MakeTarget();

        service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, _rng);

        Assert.Equal(7, actor.Neixi); // 10 - 3
    }

    [Fact]
    public void Counter_Success_WithCrit()
    {
        var rng = new FixedDamageRandom { Variance = 1.0f, CritRoll = 0.01f };
        var service = new CounterService();
        var actor = MakeActor();
        var target = MakeTarget(defense: 10);

        // Gang(30) vs Qiao target → advantage
        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, rng);

        Assert.True(result.IsCrit);
        // damage = (30-10) × 1.3 × 1.5 = 39
        Assert.Equal(39, result.DamageDealt);
    }

    // --- 反制失败（降级） ---

    [Fact]
    public void Counter_WrongType_DegradesToBasic()
    {
        var service = new CounterService();
        var actor = MakeActor();
        var target = MakeTarget(hp: 100);

        // Gang vs Gang = Neutral → 反制失败
        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Gang, _rng);

        Assert.Equal(CounterOutcome.FailedDegradedToBasic, result.Outcome);
        Assert.Equal(0, result.StaggerApplied);
        // basic attack = round(max(30,25,20) × 0.3) = 9
        Assert.Equal(9, result.DamageDealt);
    }

    [Fact]
    public void Counter_Disadvantage_DegradesToBasic()
    {
        var service = new CounterService();
        var actor = MakeActor();
        var target = MakeTarget();

        // Gang vs Rou = Disadvantage → 反制失败
        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Rou, _rng);

        Assert.Equal(CounterOutcome.FailedDegradedToBasic, result.Outcome);
    }

    // --- 内息不足 ---

    [Fact]
    public void Counter_InsufficientNeixi_Fails()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 2); // < 3
        var target = MakeTarget();

        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, _rng);

        Assert.Equal(CounterOutcome.InsufficientNeixi, result.Outcome);
        Assert.Equal(2, actor.Neixi); // unchanged
    }

    // --- 每回合只能反制一次 ---

    [Fact]
    public void Counter_SameRound_SecondAttemptFails()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 50);
        var target1 = MakeTarget();
        var target2 = new BattleCombatant("target_2", "敌人B", 100, 40, 20, 25, 30, 10, 4, 0.05f, 1, 5);

        service.Execute(actor, target1, MoveType.Gang, MoveType.Qiao, _rng);
        var result2 = service.Execute(actor, target2, MoveType.Rou, MoveType.Gang, _rng);

        Assert.Equal(CounterOutcome.AlreadyUsedThisRound, result2.Outcome);
    }

    [Fact]
    public void Counter_AfterReset_CanCounterAgain()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 50);
        var target = MakeTarget();

        service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, _rng);
        service.ResetRound();
        var result = service.Execute(actor, target, MoveType.Gang, MoveType.Qiao, _rng);

        Assert.Equal(CounterOutcome.Success, result.Outcome);
    }

    // --- CanCounter 查询 ---

    [Fact]
    public void CanCounter_True_WhenAffordable()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 5);
        Assert.True(service.CanCounter(actor));
    }

    [Fact]
    public void CanCounter_False_WhenPoor()
    {
        var service = new CounterService();
        var actor = MakeActor(neixi: 2);
        Assert.False(service.CanCounter(actor));
    }

    // --- 克制关系判定 ---

    [Theory]
    [InlineData(MoveType.Gang, MoveType.Qiao, CounterRelation.Advantage)]
    [InlineData(MoveType.Qiao, MoveType.Rou, CounterRelation.Advantage)]
    [InlineData(MoveType.Rou, MoveType.Gang, CounterRelation.Advantage)]
    [InlineData(MoveType.Gang, MoveType.Rou, CounterRelation.Disadvantage)]
    [InlineData(MoveType.Rou, MoveType.Qiao, CounterRelation.Disadvantage)]
    [InlineData(MoveType.Qiao, MoveType.Gang, CounterRelation.Disadvantage)]
    [InlineData(MoveType.Gang, MoveType.Gang, CounterRelation.Neutral)]
    public void GetCounterRelation_AllCombinations(MoveType attacker, MoveType defender, CounterRelation expected)
    {
        Assert.Equal(expected, CounterService.GetCounterRelation(attacker, defender));
    }
}
