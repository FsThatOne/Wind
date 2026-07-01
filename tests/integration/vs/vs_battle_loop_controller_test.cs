using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.Fixtures;
using FengZhi.Foundation.Combat.Runtime;
using Xunit;

namespace FengZhi.Tests.Foundation.Vs;

/// <summary>
/// Subtask 2 · VsBattleLoopController 事件驱动战斗循环验证。
///
/// 关键断言：
/// 1. Start() 后停在 PlayerDecision，WaitingForPlayer=true
/// 2. SubmitPlayerIntent 推进一回合，发布 RoundStart/IntentRevealed/Damage/RoundEnd 事件
/// 3. 多回合后触发 BattleEnd，结果有效
/// 4. 在 PlayerDecision 之外 SubmitPlayerIntent 返回 false
/// </summary>
public class VsBattleLoopControllerTest
{
    private static (VsBattleLoopController controller, BattleEventBus bus, EventRecorder recorder)
        BuildController()
    {
        var config = CombatDemoBandit1v1Fixture.CreateBattleConfig();
        var facade = new BattleFacade();
        var battle = facade.InitiateBattle(config);
        var bus = new BattleEventBus();
        var enemyAI = CombatDemoBandit1v1Fixture.CreateBanditAI();
        var controller = new VsBattleLoopController(battle, bus, enemyAI);
        var recorder = new EventRecorder(bus);
        return (controller, bus, recorder);
    }

    [Fact]
    public void Start_StopsAtPlayerDecision_AndPublishesRoundStartAndIntentReveal()
    {
        var (controller, _, recorder) = BuildController();

        controller.Start();

        Assert.True(controller.WaitingForPlayer);
        Assert.False(controller.IsFinished);
        Assert.Single(recorder.RoundStartEvents);
        Assert.Equal(1, recorder.RoundStartEvents[0].RoundNumber);
        Assert.Single(recorder.IntentRevealedEvents);
        Assert.Equal(BattlePhase.PlayerDecision, controller.Battle.CurrentPhase);
    }

    [Fact]
    public void SubmitPlayerIntent_AdvancesOneRound_AndPublishesDamageEvents()
    {
        var (controller, _, recorder) = BuildController();
        controller.Start();

        var playerAction = PlayerLightAttack();
        var accepted = controller.SubmitPlayerIntent(playerAction);

        Assert.True(accepted);
        // 至少有 1 个 DamageDealtEvent（玩家或敌方造成伤害）
        Assert.NotEmpty(recorder.DamageDealtEvents);
        Assert.Single(recorder.RoundEndEvents);
    }

    [Fact]
    public void SubmitPlayerIntent_WhenNotWaiting_ReturnsFalse()
    {
        var (controller, _, _) = BuildController();
        // 没有 Start，所以不在 PlayerDecision

        var rejected = controller.SubmitPlayerIntent(PlayerLightAttack());

        Assert.False(rejected);
    }

    [Fact]
    public void FullBattle_TerminatesAndPublishesBattleEndEvent()
    {
        var (controller, _, recorder) = BuildController();
        controller.Start();

        // 模拟玩家轮流轻重攻击直到结束
        int safetyMax = 20;
        int round = 0;
        while (!controller.IsFinished && round < safetyMax)
        {
            var action = (round % 2 == 0) ? PlayerLightAttack() : PlayerHeavyAttack();
            controller.SubmitPlayerIntent(action);
            round++;
        }

        Assert.True(controller.IsFinished);
        Assert.NotEqual(BattleResult.InProgress, controller.Result);
        Assert.Single(recorder.BattleEndEvents);
        Assert.Equal(controller.Result, recorder.BattleEndEvents[0].Result);
        // 应该在 spec §3 设计的 2-6 回合范围内
        Assert.InRange(controller.Battle.CurrentRound, 2, 6);
    }

    [Fact]
    public void SubmitPlayerIntent_AfterBattleEnd_ReturnsFalse()
    {
        var (controller, _, _) = BuildController();
        controller.Start();
        // 跑到结束
        while (!controller.IsFinished)
        {
            controller.SubmitPlayerIntent(PlayerHeavyAttack());
        }

        var rejectedAfterEnd = controller.SubmitPlayerIntent(PlayerHeavyAttack());

        Assert.False(rejectedAfterEnd);
    }

    private static BattleAction PlayerLightAttack() => new()
    {
        ActorId = CombatDemoBandit1v1Fixture.ProtagonistId,
        Type = ActionType.Move,
        TargetId = CombatDemoBandit1v1Fixture.BanditId,
        MoveId = CombatDemoBandit1v1Fixture.LightStrikeMoveId,
        MoveType = MoveType.Gang,
        NeixiCost = 2,
    };

    private static BattleAction PlayerHeavyAttack() => new()
    {
        ActorId = CombatDemoBandit1v1Fixture.ProtagonistId,
        Type = ActionType.Move,
        TargetId = CombatDemoBandit1v1Fixture.BanditId,
        MoveId = CombatDemoBandit1v1Fixture.HeavyStrikeMoveId,
        MoveType = MoveType.Gang,
        NeixiCost = 4,
    };

    /// <summary>
    /// 简易事件记录器，把订阅链路串起来。
    /// </summary>
    private sealed class EventRecorder
    {
        public List<RoundStartEvent> RoundStartEvents { get; } = new();
        public List<IntentRevealedEvent> IntentRevealedEvents { get; } = new();
        public List<DamageDealtEvent> DamageDealtEvents { get; } = new();
        public List<RoundEndEvent> RoundEndEvents { get; } = new();
        public List<BattleEndEvent> BattleEndEvents { get; } = new();

        public EventRecorder(BattleEventBus bus)
        {
            bus.Subscribe<RoundStartEvent>(e => RoundStartEvents.Add(e));
            bus.Subscribe<IntentRevealedEvent>(e => IntentRevealedEvents.Add(e));
            bus.Subscribe<DamageDealtEvent>(e => DamageDealtEvents.Add(e));
            bus.Subscribe<RoundEndEvent>(e => RoundEndEvents.Add(e));
            bus.Subscribe<BattleEndEvent>(e => BattleEndEvents.Add(e));
        }
    }
}
