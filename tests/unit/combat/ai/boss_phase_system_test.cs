using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-007: Boss 阶段与特殊机制验证。
/// GDD AC#5: 阶段转换（破绽归零 + 参数切换）。
/// GDD AC#11: 体系锁定（bias=65，其余按比例分配）。
/// GDD AC#12: 绝境模式（忽略连续惩罚 + 优先攻击冷却=1 + 反读25%）。
/// GDD AC#17: 锁定结束后惩罚计数重置。
/// GDD AC#18: 蓄力预告与执行。
/// GDD AC#22: 阶段转换清除锁定。
/// </summary>
public class BossPhaseSystemTests
{
    // --- BossPhaseScript ---

    [Fact]
    public void GetPhaseForHp_FullHp_ReturnsPhase1()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(1.0f);
        Assert.Equal(0, phase.PhaseIndex);
        Assert.Equal("试探", phase.Label);
    }

    [Fact]
    public void GetPhaseForHp_60Percent_ReturnsPhase2()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(0.6f);
        Assert.Equal(1, phase.PhaseIndex);
    }

    [Fact]
    public void GetPhaseForHp_30Percent_ReturnsPhase3a()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(0.3f);
        Assert.Equal(2, phase.PhaseIndex);
    }

    [Fact]
    public void GetPhaseForHp_15Percent_ReturnsPhase3b()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(0.15f);
        Assert.Equal(3, phase.PhaseIndex);
    }

    [Fact]
    public void GetPhaseForHp_10Percent_ReturnsPhase3b()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(0.10f);
        Assert.Equal(3, phase.PhaseIndex);
    }

    // --- BossPhaseManager: 阶段转换 ---

    [Fact]
    public void CheckPhaseTransition_HpDropsTo55_TransitionsToPhase2()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        bool transitioned = mgr.CheckPhaseTransition(0.55f);
        Assert.True(transitioned);
        Assert.Equal(1, mgr.CurrentPhaseIndex);
    }

    [Fact]
    public void CheckPhaseTransition_AlreadyInPhase_NoTransition()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f); // → Phase 2
        bool second = mgr.CheckPhaseTransition(0.55f);
        Assert.False(second);
    }

    [Fact]
    public void CheckPhaseTransition_ClearsTypeLockState()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f); // → Phase 3a
        // Advance enough to trigger type lock
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        mgr.ActivateTypeLock(MoveType.Gang);
        Assert.True(mgr.IsTypeLockActive);

        // Transition to Phase 3b clears lock (AC#22)
        bool transitioned = mgr.CheckPhaseTransition(0.10f);
        Assert.True(transitioned);
        Assert.False(mgr.IsTypeLockActive);
        Assert.Null(mgr.LockedType);
    }

    [Fact]
    public void CheckPhaseTransition_ClearsPendingCharge()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f); // Phase 2
        mgr.SetPendingCharge(new AIMoveEntry { Id = "test", Type = MoveType.Gang, NeixiCost = 3 });
        Assert.NotNull(mgr.PendingCharge);

        mgr.CheckPhaseTransition(0.25f); // Phase 3a
        Assert.Null(mgr.PendingCharge);
    }

    // --- GDD AC#12: 绝境模式属性 ---

    [Fact]
    public void AC12_DesperatePhase_IgnoresConsecutivePenalty_PriorityCooldown1_CounterRead25()
    {
        var script = BossPhaseScript.StandardFourPhase;
        var phase = script.GetPhaseForHp(0.10f); // Phase 3b
        Assert.True(phase.IgnoreConsecutivePenalty);
        Assert.Equal(1, phase.PriorityStrikeCooldown);
        Assert.Equal(0.25f, phase.CounterReadChance, 0.001f);
    }

    // --- 体系锁定 ---

    [Fact]
    public void ShouldTypeLock_CooldownMet_ReturnsTrue()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f); // Phase 3a (cooldown=2)
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        Assert.True(mgr.ShouldTypeLock());
    }

    [Fact]
    public void ShouldTypeLock_CooldownNotMet_ReturnsFalse()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f); // Phase 3a (cooldown=2)
        mgr.AdvanceRound();
        Assert.False(mgr.ShouldTypeLock());
    }

    [Fact]
    public void ShouldTypeLock_AlreadyActive_ReturnsFalse()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f);
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        mgr.ActivateTypeLock(MoveType.Gang);
        Assert.False(mgr.ShouldTypeLock());
    }

    [Fact]
    public void TypeLock_ExpiresAfterDuration()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f); // Phase 3a (duration=2)
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        mgr.ActivateTypeLock(MoveType.Gang);
        Assert.True(mgr.IsTypeLockActive);

        mgr.AdvanceRound(); // 1 of 2
        Assert.True(mgr.IsTypeLockActive);
        mgr.AdvanceRound(); // 2 of 2 → expires
        Assert.False(mgr.IsTypeLockActive);
    }

    // --- GDD AC#11: 锁定权重 ---

    [Fact]
    public void AC11_ComputeLockedWeights_BiasIs65_OthersByRatio()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.25f); // Phase 3a: 50/20/30
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        mgr.ActivateTypeLock(MoveType.Gang);

        var weights = mgr.ComputeLockedWeights();
        Assert.Equal(65, weights.Gang);
        // Rou:Qiao = 20:30 → remaining=35 → Rou=14, Qiao=21
        Assert.Equal(14, weights.Rou);
        Assert.Equal(21, weights.Qiao);
    }

    // --- 蓄力预告 ---

    [Fact]
    public void ShouldCharge_FrequencyMet_ReturnsTrue()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f); // Phase 2 (chargeFreq=4)
        for (int i = 0; i < 4; i++) mgr.AdvanceRound();
        Assert.True(mgr.ShouldCharge());
    }

    [Fact]
    public void ShouldCharge_FrequencyNotMet_ReturnsFalse()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f); // Phase 2 (chargeFreq=4)
        for (int i = 0; i < 3; i++) mgr.AdvanceRound();
        Assert.False(mgr.ShouldCharge());
    }

    [Fact]
    public void SetAndConsumePendingCharge_Works()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        var move = new AIMoveEntry { Id = "big_hit", Type = MoveType.Gang, NeixiCost = 5 };
        mgr.SetPendingCharge(move);
        Assert.NotNull(mgr.PendingCharge);

        var consumed = mgr.ConsumePendingCharge();
        Assert.Equal("big_hit", consumed!.Id);
        Assert.Null(mgr.PendingCharge);
    }

    [Fact]
    public void ShouldCharge_PendingExists_ReturnsFalse()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f);
        for (int i = 0; i < 4; i++) mgr.AdvanceRound();
        mgr.SetPendingCharge(new AIMoveEntry { Id = "x", Type = MoveType.Qiao });
        Assert.False(mgr.ShouldCharge());
    }

    // --- 优先攻击 ---

    [Fact]
    public void CanPriorityStrike_CooldownMet_ReturnsTrue()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f); // Phase 2 (cooldown=3)
        for (int i = 0; i < 3; i++) mgr.AdvanceRound();
        Assert.True(mgr.CanPriorityStrike());
    }

    [Fact]
    public void RecordPriorityStrike_ResetsCooldown()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.55f);
        for (int i = 0; i < 3; i++) mgr.AdvanceRound();
        mgr.RecordPriorityStrike();
        Assert.False(mgr.CanPriorityStrike());
        mgr.AdvanceRound();
        Assert.False(mgr.CanPriorityStrike());
        mgr.AdvanceRound();
        mgr.AdvanceRound();
        Assert.True(mgr.CanPriorityStrike());
    }

    // --- Phase 3b 绝境：冷却=1 意味着每回合都可用 ---

    [Fact]
    public void AC12_DesperatePhase_PriorityStrikeEveryRound()
    {
        var mgr = new BossPhaseManager(BossPhaseScript.StandardFourPhase);
        mgr.CheckPhaseTransition(0.10f); // Phase 3b (cooldown=1)
        mgr.AdvanceRound();
        Assert.True(mgr.CanPriorityStrike());
        mgr.RecordPriorityStrike();
        mgr.AdvanceRound();
        Assert.True(mgr.CanPriorityStrike()); // 每回合可用
    }
}
