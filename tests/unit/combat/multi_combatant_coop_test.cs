using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

/// <summary>
/// cb-009: 多人战斗与协同破绽测试。
/// </summary>
public class MultiCombatantCoopTest
{
    private static BattleCombatant MakePlayer(string id, int attack = 50, int defense = 20, int neixi = 30)
        => new(id, id, 100, neixi, attack, attack, attack, defense, 10, 0.1f, 5, 3, 5);

    private static BattleCombatant MakeEnemy(string id, int attack = 40, int defense = 15)
        => new(id, id, 80, 20, attack, attack, attack, defense, 8, 0.05f, 3, 2, 5);

    private static readonly FixedDamageRandom _random = new() { Variance = 1.0f, CritRoll = 0.99f };

    private static ResolutionService CreateService()
    {
        var counterService = new CounterService();
        return new ResolutionService(counterService, _random);
    }

    private static Dictionary<string, BattleCombatant> BuildLookup(params BattleCombatant[] all)
        => all.ToDictionary(c => c.Id);

    private static HashSet<string> PlayerIds(params string[] ids) => new(ids);

    // --- AC1: 两己方角色同回合克制同目标：目标额外 +1 破绽 ---

    [Fact]
    public void CoopBonus_TwoPlayersCounterSameTarget_ExtraStagger()
    {
        var p1 = MakePlayer("p1", attack: 20); // lower attack so target survives
        var p2 = MakePlayer("p2", attack: 20);
        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, p2, e1);
        var pIds = PlayerIds("p1", "p2");

        // p1, p2 都用柔系 → e1 不在 intentMap → 默认 Gang → 柔克刚 = Advantage
        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 },
            new() { ActorId = "p2", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 }
        };

        var svc = CreateService();
        svc.ResolveRound(actions, lookup, pIds);

        // 每次克制 +2, 协同额外 +1 = 5
        Assert.Equal(5, e1.Stagger);
    }

    // --- AC2: 两敌方角色同回合攻击同目标：不触发协同加成 ---

    [Fact]
    public void NoCoopBonus_TwoEnemiesCounterSameTarget()
    {
        var p1 = MakePlayer("p1");
        var e1 = MakeEnemy("e1");
        var e2 = MakeEnemy("e2");
        var lookup = BuildLookup(p1, e1, e2);
        var pIds = PlayerIds("p1");

        // 两个敌人都用柔系攻击 p1，p1 不在 intentMap 中(Breathe)→ 默认 Gang
        // 柔克刚 = Advantage（对敌方），但敌方不享有协同加成
        var actions = new List<BattleAction>
        {
            new() { ActorId = "e1", Type = ActionType.Move, TargetId = "p1", MoveType = MoveType.Rou, NeixiCost = 0 },
            new() { ActorId = "e2", Type = ActionType.Move, TargetId = "p1", MoveType = MoveType.Rou, NeixiCost = 0 },
            new() { ActorId = "p1", Type = ActionType.Breathe }
        };

        var svc = CreateService();
        svc.ResolveRound(actions, lookup, pIds);

        // 每次克制 +2 × 2 = 4 (无协同加成)
        Assert.Equal(4, p1.Stagger);
    }

    [Fact]
    public void CoopBonus_RequiresAdvantageRelation()
    {
        var p1 = MakePlayer("p1");
        var p2 = MakePlayer("p2");
        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, p2, e1);
        var pIds = PlayerIds("p1", "p2");

        // p1 用刚, p2 用柔, e1 用刚 → p1: 刚vs刚=Neutral, p2: 柔vs刚=Advantage
        // 只有一个克制，不满足协同条件
        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Gang, NeixiCost = 2 },
            new() { ActorId = "p2", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 },
            new() { ActorId = "e1", Type = ActionType.Move, TargetId = "p1", MoveType = MoveType.Gang, NeixiCost = 0 }
        };

        var svc = CreateService();
        svc.ResolveRound(actions, lookup, pIds);

        // p2 柔vs刚=Advantage → +2, p1 刚vs刚=Neutral → 0, 无协同 → 总共 2
        Assert.Equal(2, e1.Stagger);
    }

    // --- AC3: 一方全灭时战斗结束 ---

    [Fact]
    public void BattleEnds_WhenAllEnemiesDead()
    {
        var p1 = MakePlayer("p1", attack: 200);
        var e1 = MakeEnemy("e1");
        e1.ApplyDamage(79); // 只剩 1 HP

        var battle = new BattleInstance(new[] { p1 }, new[] { e1 }, maxRounds: 15);
        battle.AdvancePhase(); // Init → RoundStart
        battle.AdvancePhase(); // → IntentReveal
        battle.AdvancePhase(); // → PlayerDecision
        battle.AdvancePhase(); // → Resolution

        // 模拟杀死敌人
        e1.ApplyDamage(1);

        battle.AdvancePhase(); // → RoundEnd
        battle.AdvancePhase(); // RoundEnd → ProcessRoundEnd → BattleOver

        Assert.Equal(BattlePhase.BattleOver, battle.CurrentPhase);
        Assert.Equal(BattleResult.Victory, battle.Result);
    }

    // --- AC4: 落败角色不再参与行动 ---

    [Fact]
    public void DeadCharacters_SkippedInResolution()
    {
        var p1 = MakePlayer("p1");
        var deadP = MakePlayer("p_dead");
        deadP.ApplyDamage(100); // kill

        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, deadP, e1);
        var pIds = PlayerIds("p1", "p_dead");

        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" },
            new() { ActorId = "p_dead", Type = ActionType.BasicAttack, TargetId = "e1" }
        };

        var svc = CreateService();
        var results = svc.ResolveRound(actions, lookup, pIds);

        // 死亡角色的行动被跳过
        Assert.Single(results);
        Assert.Equal("p1", results[0].ActorId);
    }

    [Fact]
    public void DeadTarget_SkippedInResolution()
    {
        var p1 = MakePlayer("p1");
        var e1 = MakeEnemy("e1");
        e1.ApplyDamage(80); // kill

        var lookup = BuildLookup(p1, e1);
        var pIds = PlayerIds("p1");

        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" }
        };

        var svc = CreateService();
        var results = svc.ResolveRound(actions, lookup, pIds);

        Assert.Empty(results);
    }

    // --- AC5: 双方同回合全灭判定为"惜败" ---

    [Fact]
    public void NarrowDefeat_BothSidesWipedOut()
    {
        var p1 = MakePlayer("p1");
        p1.ApplyDamage(99); // 1 HP left
        var e1 = MakeEnemy("e1");
        e1.ApplyDamage(79); // 1 HP left

        var battle = new BattleInstance(new[] { p1 }, new[] { e1 }, maxRounds: 15);
        battle.AdvancePhase(); // Init → RoundStart
        battle.AdvancePhase(); // → IntentReveal
        battle.AdvancePhase(); // → PlayerDecision
        battle.AdvancePhase(); // → Resolution

        // 双方同时死亡
        p1.ApplyDamage(1);
        e1.ApplyDamage(1);

        battle.AdvancePhase(); // → RoundEnd
        battle.AdvancePhase(); // RoundEnd → ProcessRoundEnd → NarrowDefeat

        Assert.Equal(BattlePhase.BattleOver, battle.CurrentPhase);
        Assert.Equal(BattleResult.NarrowDefeat, battle.Result);
    }

    // --- 补充测试 ---

    [Fact]
    public void FilterAlive_ReturnsOnlyLiving()
    {
        var p1 = MakePlayer("p1");
        var p2 = MakePlayer("p2");
        p2.ApplyDamage(100);
        var p3 = MakePlayer("p3");

        var alive = ResolutionService.FilterAlive(new[] { p1, p2, p3 });
        Assert.Equal(2, alive.Count);
        Assert.DoesNotContain(p2, alive);
    }

    [Fact]
    public void Breathe_WorksInMultiCharacterResolution()
    {
        var p1 = MakePlayer("p1");
        p1.SpendNeixi(10); // reduce neixi
        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, e1);
        var pIds = PlayerIds("p1");

        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.Breathe }
        };

        var svc = CreateService();
        var results = svc.ResolveRound(actions, lookup, pIds);

        Assert.Single(results);
        Assert.Equal(ActionType.Breathe, results[0].ActionType);
        Assert.True(p1.Neixi > 20); // recovered some
    }

    [Fact]
    public void CoopBonus_ThreePlayersCounterSameTarget_StillOnlyPlusOne()
    {
        var p1 = MakePlayer("p1", attack: 10); // low attack so target survives
        var p2 = MakePlayer("p2", attack: 10);
        var p3 = MakePlayer("p3", attack: 10);
        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, p2, p3, e1);
        var pIds = PlayerIds("p1", "p2", "p3");

        // 三人都用柔系，e1 不在 intentMap → 默认 Gang → 柔克刚 = Advantage
        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 },
            new() { ActorId = "p2", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 },
            new() { ActorId = "p3", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Rou, NeixiCost = 2 }
        };

        var svc = CreateService();
        svc.ResolveRound(actions, lookup, pIds);

        // 每个克制 +2 × 3 = 6，协同加成 +1（只触发一次），总共 7
        Assert.Equal(7, e1.Stagger);
    }

    [Fact]
    public void InsufficientNeixi_MoveSkipped()
    {
        var p1 = MakePlayer("p1", neixi: 1); // not enough for cost=5
        var e1 = MakeEnemy("e1");
        var lookup = BuildLookup(p1, e1);
        var pIds = PlayerIds("p1");

        var actions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.Move, TargetId = "e1", MoveType = MoveType.Gang, NeixiCost = 5 }
        };

        var svc = CreateService();
        var results = svc.ResolveRound(actions, lookup, pIds);

        Assert.Empty(results);
        Assert.Equal(80, e1.HP); // no damage
    }
}
