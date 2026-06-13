using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-008: EnemyBrain 管线集成验证。
/// 验证完整决策管线的串联正确性。
/// GDD AC#1: 刚猛型 1000 次模拟，刚系 57%-63%。
/// GDD AC#7: 内息=1 时调息率 ~50%。
/// GDD AC#4: 反读 40% 触发率。
/// </summary>
public class EnemyBrainIntegrationTests
{
    private static readonly List<AIMoveEntry> StandardMoves = new()
    {
        new AIMoveEntry { Id = "g1", Name = "猛虎下山", Type = MoveType.Gang, NeixiCost = 2 },
        new AIMoveEntry { Id = "r1", Name = "绵掌", Type = MoveType.Rou, NeixiCost = 2 },
        new AIMoveEntry { Id = "q1", Name = "燕子穿林", Type = MoveType.Qiao, NeixiCost = 1 },
    };

    private static AIBattleContext DefaultContext(int neixi = 10) => new()
    {
        SelfId = "enemy1",
        HpRatio = 1.0f,
        CurrentNeixi = neixi,
        CurrentRound = 1,
        AvailableMoves = StandardMoves,
        Targets = new[] { new TargetCandidate { Id = "player1", HpRatio = 1.0f } }
    };

    // --- 基本管线流通 ---

    [Fact]
    public void Decide_NormalEnemy_ReturnsAttackDecision()
    {
        var brain = new EnemyBrain(PersonalityTemplate.Fierce, SignaturePattern.Fierce, new SeededAIRandom(42));
        var ctx = DefaultContext();
        var decision = brain.Decide(ctx);

        Assert.Equal(AIActionType.Attack, decision.ActionType);
        Assert.NotNull(decision.SelectedType);
        Assert.NotNull(decision.SelectedMove);
        Assert.Equal("player1", decision.TargetId);
    }

    // --- GDD AC#1: 刚猛型 1000 次统计 ---

    [Fact]
    public void AC1_FierceTemplate_1000Rounds_GangType57To63Percent()
    {
        int gangCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var brain = new EnemyBrain(PersonalityTemplate.Fierce, null, new SeededAIRandom(i));
            var ctx = DefaultContext();
            var decision = brain.Decide(ctx);
            if (decision.SelectedType == MoveType.Gang) gangCount++;
        }
        Assert.InRange(gangCount, 570, 630);
    }

    // --- 调息触发 ---

    [Fact]
    public void Decide_LowNeixi_CanTriggerMeditation()
    {
        var brain = new EnemyBrain(PersonalityTemplate.Fierce, null, new FixedAIRandom(0.0f));
        var ctx = DefaultContext(neixi: 1); // ≤ threshold=2, chance=50%, roll=0 → 触发
        var decision = brain.Decide(ctx);
        Assert.Equal(AIActionType.Meditate, decision.ActionType);
    }

    [Fact]
    public void Decide_HighNeixi_NeverMeditates()
    {
        var brain = new EnemyBrain(PersonalityTemplate.Fierce, null, new FixedAIRandom(0.0f));
        var ctx = DefaultContext(neixi: 10);
        var decision = brain.Decide(ctx);
        Assert.Equal(AIActionType.Attack, decision.ActionType);
    }

    // --- 反读否决 ---

    [Fact]
    public void Decide_CounterReadTriggered_OverridesType()
    {
        // Boss with 40% counter-read, player used Rou twice
        var brain = new EnemyBrain(BossPhaseScript.StandardFourPhase, null, new FixedAIRandom(0.1f));
        brain.UpdateHp(0.55f); // Phase 2 (counter_read_chance=0.4)
        brain.RecordPlayerAction("player1", MoveType.Rou);
        brain.RecordPlayerAction("player1", MoveType.Rou);

        var ctx = DefaultContext();
        var decision = brain.Decide(ctx);
        Assert.True(decision.CounterReadTriggered);
        Assert.Equal(MoveType.Qiao, decision.SelectedType); // 巧克柔
    }

    // --- Boss 蓄力预告 ---

    [Fact]
    public void Decide_BossCharge_ReturnsChargeAnnounce()
    {
        var brain = new EnemyBrain(BossPhaseScript.StandardFourPhase, null, new SeededAIRandom(42));
        brain.UpdateHp(0.55f); // Phase 2 (chargeFreq=4)
        for (int i = 0; i < 4; i++) brain.AdvanceRound();

        var ctx = DefaultContext();
        var decision = brain.Decide(ctx);
        Assert.Equal(AIActionType.ChargeAnnounce, decision.ActionType);
        Assert.NotNull(decision.ChargeAnnounceName);
    }

    [Fact]
    public void Decide_BossPendingCharge_ExecutesDirectly()
    {
        var brain = new EnemyBrain(BossPhaseScript.StandardFourPhase, null, new SeededAIRandom(42));
        brain.UpdateHp(0.55f);
        for (int i = 0; i < 4; i++) brain.AdvanceRound();

        // 第一次决策 → 蓄力预告
        var ctx = DefaultContext();
        var decision1 = brain.Decide(ctx);
        Assert.Equal(AIActionType.ChargeAnnounce, decision1.ActionType);

        // 第二次决策 → 执行已承诺招式
        var decision2 = brain.Decide(ctx);
        Assert.Equal(AIActionType.Attack, decision2.ActionType);
        Assert.NotNull(decision2.SelectedMove);
    }

    // --- Boss 阶段转换 ---

    [Fact]
    public void UpdateHp_TransitionsPhase_ReturnsTrue()
    {
        var brain = new EnemyBrain(BossPhaseScript.StandardFourPhase, null, new SeededAIRandom(42));
        Assert.True(brain.UpdateHp(0.55f));
        Assert.Equal(1, brain.BossManager!.CurrentPhaseIndex);
    }

    // --- 招式预兆加成 ---

    [Fact]
    public void Decide_WithSignature_FirstRound_IncreasesMatchingTypeChance()
    {
        // 刚猛 signature 在第1回合有 +37 加成
        int gangCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var brain = new EnemyBrain(PersonalityTemplate.Fierce, SignaturePattern.Fierce, new SeededAIRandom(i));
            var ctx = new AIBattleContext
            {
                CurrentNeixi = 10, CurrentRound = 1,
                AvailableMoves = StandardMoves,
                Targets = new[] { new TargetCandidate { Id = "p1", HpRatio = 1.0f } }
            };
            var decision = brain.Decide(ctx);
            if (decision.SelectedType == MoveType.Gang) gangCount++;
        }
        // 有 signature 加成时刚系应该 > 63%（原始 60%+37 加成后概率更高）
        Assert.True(gangCount > 630, $"Gang count with signature should be > 630, was {gangCount}");
    }

    // --- 状态机集成 ---

    [Fact]
    public void RecordCountered_Twice_EntersAlertState()
    {
        var brain = new EnemyBrain(PersonalityTemplate.Fierce, null, new SeededAIRandom(42));
        brain.RecordCountered();
        brain.RecordCountered();
        Assert.Equal(AIBehaviorState.Alert, brain.StateMachine.CurrentState);
    }

    // --- 目标选择集成 ---

    [Fact]
    public void Decide_MultipleTargets_SelectsDecisiveTarget()
    {
        var brain = new EnemyBrain(PersonalityTemplate.Fierce, null, new SeededAIRandom(42));
        var ctx = new AIBattleContext
        {
            CurrentNeixi = 10, CurrentRound = 1,
            AvailableMoves = StandardMoves,
            Targets = new[]
            {
                new TargetCandidate { Id = "t1", HpRatio = 1.0f, CurrentStagger = 0 },
                new TargetCandidate { Id = "t2", HpRatio = 0.5f, CurrentStagger = 5, StaggerThreshold = 5 }
            }
        };
        var decision = brain.Decide(ctx);
        Assert.Equal("t2", decision.TargetId);
    }
}
