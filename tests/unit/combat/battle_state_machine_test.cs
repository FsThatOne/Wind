using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class BattleStateMachineTest
{
    private static BattleCombatant MakePlayer(int hp = 100, int neixi = 50, int neixiRecovery = 5)
        => new("player_1", "主角", hp, neixi, 30, 25, 20, 10, 5, 0.1f, 3, neixiRecovery);

    private static BattleCombatant MakeEnemy(int hp = 80, int neixi = 40, int neixiRecovery = 3)
        => new("enemy_1", "刺客", hp, neixi, 20, 25, 30, 8, 4, 0.05f, 1, neixiRecovery);

    private BattleInstance CreateBattle(int maxRounds = 15)
        => new(new[] { MakePlayer() }, new[] { MakeEnemy() }, maxRounds);

    [Fact]
    public void NewBattle_InitializingState()
    {
        var battle = CreateBattle();
        Assert.Equal(BattlePhase.Initializing, battle.CurrentPhase);
        Assert.Equal(0, battle.CurrentRound);
        Assert.Equal(BattleResult.InProgress, battle.Result);
    }

    [Fact]
    public void AdvancePhase_FullRoundCycle()
    {
        var battle = CreateBattle();

        // Initializing -> RoundStart
        Assert.Equal(BattlePhase.RoundStart, battle.AdvancePhase());
        Assert.Equal(1, battle.CurrentRound);

        // RoundStart -> IntentReveal
        Assert.Equal(BattlePhase.IntentReveal, battle.AdvancePhase());

        // IntentReveal -> PlayerDecision
        Assert.Equal(BattlePhase.PlayerDecision, battle.AdvancePhase());

        // PlayerDecision -> Resolution
        Assert.Equal(BattlePhase.Resolution, battle.AdvancePhase());

        // Resolution -> RoundEnd
        Assert.Equal(BattlePhase.RoundEnd, battle.AdvancePhase());

        // RoundEnd -> RoundStart (next round, no one dead)
        Assert.Equal(BattlePhase.RoundStart, battle.AdvancePhase());
        Assert.Equal(2, battle.CurrentRound);
    }

    [Fact]
    public void RoundStart_TriggersNeixiRecovery()
    {
        var player = MakePlayer(neixi: 50, neixiRecovery: 5);
        // Spend some neixi first
        player.SpendNeixi(20);
        Assert.Equal(30, player.Neixi);

        var battle = new BattleInstance(new[] { player }, new[] { MakeEnemy() });
        battle.AdvancePhase(); // -> RoundStart triggers recovery

        Assert.Equal(35, player.Neixi); // 30 + 5 recovery
    }

    [Fact]
    public void RoundStart_TriggersStaggerDecay()
    {
        var enemy = MakeEnemy();
        enemy.AddStagger(3);
        Assert.Equal(3, enemy.Stagger);

        var battle = new BattleInstance(new[] { MakePlayer() }, new[] { enemy });
        battle.AdvancePhase(); // -> RoundStart triggers decay

        Assert.Equal(2, enemy.Stagger); // 3 - 1
    }

    [Fact]
    public void MaxRounds_BattleOver_Draw()
    {
        var battle = CreateBattle(maxRounds: 2);

        // Complete round 1
        for (int i = 0; i < 5; i++) battle.AdvancePhase(); // Init->RS->IR->PD->Res->RE (round1)
        // RoundEnd -> Start round 2
        battle.AdvancePhase(); // -> RoundStart (round 2)
        Assert.Equal(2, battle.CurrentRound);

        // Complete round 2
        battle.AdvancePhase(); // -> IntentReveal
        battle.AdvancePhase(); // -> PlayerDecision
        battle.AdvancePhase(); // -> Resolution
        battle.AdvancePhase(); // -> RoundEnd

        // RoundEnd of round 2 (= maxRounds) -> BattleOver
        battle.AdvancePhase();
        Assert.Equal(BattlePhase.BattleOver, battle.CurrentPhase);
        Assert.Equal(BattleResult.Draw, battle.Result);
    }

    [Fact]
    public void AllEnemiesDead_Victory()
    {
        var enemy = MakeEnemy(hp: 1);
        var battle = new BattleInstance(new[] { MakePlayer() }, new[] { enemy });

        // Go to RoundEnd
        battle.AdvancePhase(); // -> RoundStart
        battle.AdvancePhase(); // -> IntentReveal
        battle.AdvancePhase(); // -> PlayerDecision
        battle.AdvancePhase(); // -> Resolution

        // Kill the enemy before RoundEnd
        enemy.ApplyDamage(100);
        Assert.False(enemy.IsAlive);

        battle.AdvancePhase(); // -> RoundEnd
        battle.AdvancePhase(); // RoundEnd -> checks -> BattleOver
        Assert.Equal(BattlePhase.BattleOver, battle.CurrentPhase);
        Assert.Equal(BattleResult.Victory, battle.Result);
    }

    [Fact]
    public void AllPlayersDead_Defeat()
    {
        var player = MakePlayer(hp: 1);
        var battle = new BattleInstance(new[] { player }, new[] { MakeEnemy() });

        battle.AdvancePhase(); // -> RoundStart
        battle.AdvancePhase(); // -> IntentReveal
        battle.AdvancePhase(); // -> PlayerDecision
        battle.AdvancePhase(); // -> Resolution

        player.ApplyDamage(100);

        battle.AdvancePhase(); // -> RoundEnd
        battle.AdvancePhase(); // -> BattleOver
        Assert.Equal(BattleResult.Defeat, battle.Result);
    }

    [Fact]
    public void BothSidesDead_NarrowDefeat()
    {
        var player = MakePlayer(hp: 1);
        var enemy = MakeEnemy(hp: 1);
        var battle = new BattleInstance(new[] { player }, new[] { enemy });

        battle.AdvancePhase(); // -> RoundStart
        battle.AdvancePhase(); // -> IntentReveal
        battle.AdvancePhase(); // -> PlayerDecision
        battle.AdvancePhase(); // -> Resolution

        player.ApplyDamage(100);
        enemy.ApplyDamage(100);

        battle.AdvancePhase(); // -> RoundEnd
        battle.AdvancePhase(); // -> BattleOver
        Assert.Equal(BattleResult.NarrowDefeat, battle.Result);
    }

    [Fact]
    public void BattleOver_NoFurtherAdvance()
    {
        var enemy = MakeEnemy(hp: 1);
        var battle = new BattleInstance(new[] { MakePlayer() }, new[] { enemy });

        battle.AdvancePhase(); // -> RoundStart
        battle.AdvancePhase(); // -> IntentReveal
        battle.AdvancePhase(); // -> PlayerDecision
        battle.AdvancePhase(); // -> Resolution
        enemy.ApplyDamage(100);
        battle.AdvancePhase(); // -> RoundEnd
        battle.AdvancePhase(); // -> BattleOver

        // Calling again should stay BattleOver
        Assert.Equal(BattlePhase.BattleOver, battle.AdvancePhase());
    }

    [Fact]
    public void DeadCombatants_SkippedDuringRoundStart()
    {
        var player = MakePlayer(neixi: 50, neixiRecovery: 5);
        var enemy = MakeEnemy(hp: 1, neixiRecovery: 3);

        // Kill the enemy
        enemy.ApplyDamage(100);
        enemy.SpendNeixi(10); // neixi now 30

        var battle = new BattleInstance(new[] { player }, new[] { enemy });
        player.SpendNeixi(10); // player neixi now 40

        battle.AdvancePhase(); // -> RoundStart

        // Player recovers
        Assert.Equal(45, player.Neixi); // 40 + 5
        // Dead enemy does NOT recover
        Assert.Equal(30, enemy.Neixi);
    }
}
