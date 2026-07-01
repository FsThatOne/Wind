using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.AI;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Combat.Fixtures;
using FengZhi.Foundation.Combat.Runtime;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class XingqiTacticsSliceFixtureTest
{
    [Fact]
    public void TacticsSlice_UsesFiveByFiveBoardAndOneVersusOneSetup()
    {
        var config = XingqiTacticsSliceFixture.CreateBattleConfig();
        var grid = XingqiTacticsSliceFixture.CreateGrid();

        Assert.Equal(5, grid.Width);
        Assert.Equal(5, grid.Height);
        Assert.Single(config.PlayerParty);
        Assert.Single(config.EnemyGroup);
        Assert.Equal("tactics_slice", config.BattleType);
        Assert.Equal(new GridPosition(1, 2), config.PlayerParty[0].InitialPosition);
        Assert.Equal(new GridPosition(3, 2), config.EnemyGroup[0].InitialPosition);
    }

    [Fact]
    public void TacticsSlice_PlayerActsFirstAndReceivesMovementPhase()
    {
        var player = XingqiTacticsSliceFixture.CreateProtagonistConfig();
        var bandit = XingqiTacticsSliceFixture.CreateBanditConfig();

        Assert.True(player.Agility > bandit.Agility);

        var controller = new XingqiBattleLoopController(
            new[] { new BattleCombatant(
                player.Id, player.Name, player.MaxHP, player.MaxNeixi,
                player.AttackGang, player.AttackRou, player.AttackQiao,
                player.Defense, player.Speed, player.CritRate,
                player.InsightStat, player.NeixiRecovery,
                player.StaggerThreshold, agility: player.Agility,
                initialPosition: player.InitialPosition,
                initialFacing: player.InitialFacing,
                moveRange: player.MoveRange) },
            new[] { new BattleCombatant(
                bandit.Id, bandit.Name, bandit.MaxHP, bandit.MaxNeixi,
                bandit.AttackGang, bandit.AttackRou, bandit.AttackQiao,
                bandit.Defense, bandit.Speed, bandit.CritRate,
                bandit.InsightStat, bandit.NeixiRecovery,
                bandit.StaggerThreshold, agility: bandit.Agility,
                initialPosition: bandit.InitialPosition,
                initialFacing: bandit.InitialFacing,
                moveRange: bandit.MoveRange) },
            new BattleEventBus(),
            XingqiTacticsSliceFixture.CreateBanditAI(),
            XingqiTacticsSliceFixture.CreateXingqiConfig(),
            grid: XingqiTacticsSliceFixture.CreateGrid());

        controller.Start();

        Assert.True(controller.WaitingForPlayer);
        Assert.Equal(player.Id, controller.CurrentActorId);
        Assert.Equal(TurnSubPhase.WaitingForMovement, controller.CurrentSubPhase);
    }

    [Fact]
    public void TacticsSlice_BanditAiUsesChasingMovement()
    {
        var ai = XingqiTacticsSliceFixture.CreateBanditAI();
        var grid = XingqiTacticsSliceFixture.CreateGrid();
        var player = XingqiTacticsSliceFixture.CreateProtagonistConfig();
        var bandit = XingqiTacticsSliceFixture.CreateBanditConfig();
        var banditCombatant = new BattleCombatant(
            bandit.Id, bandit.Name, bandit.MaxHP, bandit.MaxNeixi,
            bandit.AttackGang, bandit.AttackRou, bandit.AttackQiao,
            bandit.Defense, bandit.Speed, bandit.CritRate,
            bandit.InsightStat, bandit.NeixiRecovery,
            bandit.StaggerThreshold, agility: bandit.Agility,
            initialPosition: bandit.InitialPosition,
            initialFacing: bandit.InitialFacing,
            moveRange: bandit.MoveRange);

        grid.SetOccupant(player.InitialPosition, player.Id);
        grid.SetOccupant(bandit.InitialPosition, bandit.Id);
        var reachable = MovementService.GetReachableCells(grid, bandit.InitialPosition, bandit.MoveRange);

        var target = ai.DecideMovement(banditCombatant, grid, reachable);

        Assert.IsType<ChasingAI>(ai);
        Assert.NotNull(target);
        Assert.True(target!.Value.ManhattanDistance(player.InitialPosition) < bandit.InitialPosition.ManhattanDistance(player.InitialPosition));
    }
}
