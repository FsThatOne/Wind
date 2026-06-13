using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

public class ActionRegistryExecutionTest
{
    private static BattleCombatant MakeActor(int neixi = 50, int neixiRecovery = 10)
        => new("actor_1", "主角", 100, neixi, 30, 25, 20, 10, 5, 0.1f, 3, neixiRecovery);

    private static BattleCombatant MakeTarget(int hp = 100, int defense = 10)
        => new("target_1", "敌人", hp, 40, 20, 25, 30, defense, 4, 0.05f, 1, 5);

    // --- ActionType 枚举 ---

    [Fact]
    public void ActionType_HasAll6Types()
    {
        var values = Enum.GetValues<ActionType>();
        Assert.Equal(6, values.Length);
        Assert.Contains(ActionType.Move, values);
        Assert.Contains(ActionType.Counter, values);
        Assert.Contains(ActionType.Decisive, values);
        Assert.Contains(ActionType.Breathe, values);
        Assert.Contains(ActionType.BasicAttack, values);
        Assert.Contains(ActionType.Item, values);
    }

    // --- 内息消耗 ---

    [Fact]
    public void TrySpendNeixi_Success()
    {
        var actor = MakeActor(neixi: 50);
        bool ok = ActionExecutor.TrySpendNeixi(actor, 20);
        Assert.True(ok);
        Assert.Equal(30, actor.Neixi);
    }

    [Fact]
    public void TrySpendNeixi_InsufficientFails()
    {
        var actor = MakeActor(neixi: 5);
        bool ok = ActionExecutor.TrySpendNeixi(actor, 20);
        Assert.False(ok);
        Assert.Equal(5, actor.Neixi); // unchanged
    }

    // --- 调息 F8 ---

    [Fact]
    public void Breathe_RecoversCeilHalfNeixiRecovery()
    {
        // neixiRecovery=10, meditation = ceil(10×0.5) = 5
        var actor = MakeActor(neixi: 50, neixiRecovery: 10);
        actor.SpendNeixi(30); // neixi=20
        var result = ActionExecutor.ExecuteBreathe(actor);

        Assert.Equal(ActionOutcome.Success, result.Outcome);
        Assert.Equal(5, result.NeixiRecovered);
        Assert.Equal(25, actor.Neixi); // 20+5
    }

    [Fact]
    public void Breathe_OddRecovery_CeilsUp()
    {
        // neixiRecovery=7, meditation = ceil(7×0.5) = ceil(3.5) = 4
        var actor = MakeActor(neixi: 50, neixiRecovery: 7);
        actor.SpendNeixi(40); // neixi=10
        var result = ActionExecutor.ExecuteBreathe(actor);

        Assert.Equal(4, result.NeixiRecovered);
        Assert.Equal(14, actor.Neixi);
    }

    [Fact]
    public void Breathe_ClampsToMax()
    {
        var actor = MakeActor(neixi: 50, neixiRecovery: 100);
        // already full, ceiling(100×0.5)=50 but clamped
        var result = ActionExecutor.ExecuteBreathe(actor);

        Assert.Equal(50, result.NeixiRecovered);
        Assert.Equal(50, actor.Neixi); // still max
    }

    // --- 普通攻击 ---

    [Fact]
    public void BasicAttack_UsesMaxAttackTimes03()
    {
        // actor: Gang=30, Rou=25, Qiao=20 → max=30
        // damage = round(30 × 0.3) = 9
        var actor = MakeActor();
        var target = MakeTarget(hp: 100);
        var result = ActionExecutor.ExecuteBasicAttack(actor, target);

        Assert.Equal(ActionOutcome.Success, result.Outcome);
        Assert.Equal(9, result.DamageDealt);
        Assert.Equal(91, target.HP);
    }

    [Fact]
    public void BasicAttack_MinDamage1()
    {
        // actor with very low attack: 1,1,1 → round(1×0.3)=0 → clamped to 1
        var actor = new BattleCombatant("weak", "弱者", 100, 50, 1, 1, 1, 5, 3, 0f, 0, 3);
        var target = MakeTarget(hp: 50);
        var result = ActionExecutor.ExecuteBasicAttack(actor, target);

        Assert.Equal(1, result.DamageDealt);
        Assert.Equal(49, target.HP);
    }

    // --- 道具（占位） ---

    [Fact]
    public void Item_ReturnsNotImplemented()
    {
        var actor = MakeActor();
        var result = ActionExecutor.ExecuteItem(actor, "healing_pill");
        Assert.Equal(ActionOutcome.NotImplemented, result.Outcome);
    }

    // --- BattleAction DTO ---

    [Fact]
    public void BattleAction_CanConstruct()
    {
        var action = new BattleAction
        {
            ActorId = "player_1",
            Type = ActionType.Move,
            TargetId = "enemy_1",
            MoveId = "breaking_wave",
            MoveType = FengZhi.Foundation.CharacterData.MoveType.Gang,
            NeixiCost = 5
        };

        Assert.Equal("player_1", action.ActorId);
        Assert.Equal(ActionType.Move, action.Type);
        Assert.Equal(5, action.NeixiCost);
    }
}
