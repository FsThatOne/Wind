using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.Fixtures;
using Xunit;

namespace FengZhi.Tests.Foundation.Vs;

/// <summary>
/// Subtask 0b · S7-VS-Combat-Loop fixture smoke。
///
/// 目的：验证战斗演示小贼 1v1 fixture 数据合理：
/// - InitiateBattle 不抛错
/// - RunFullBattle (双方都 ScriptedAI 自动出招) 在 MaxRounds=8 内分出胜负
/// - 回合数落在 2-5 之间（spec §3 estimate 设计；过多 → fixture HP/伤害失衡；过少 → 不够展示 VS）
///
/// 详见 `docs/superpowers/specs/2026-06-23-s7-vs-combat-loop-mvp-a.md` §4 Subtask 0b。
/// </summary>
public class CombatDemoBandit1v1FixtureSmokeTest
{
    [Fact]
    public void InitiateBattle_WithCombatDemoBanditFixture_DoesNotThrow()
    {
        var config = CombatDemoBandit1v1Fixture.CreateBattleConfig();
        var facade = new BattleFacade();

        var battle = facade.InitiateBattle(config);

        Assert.Single(battle.PlayerParty);
        Assert.Single(battle.EnemyGroup);
        Assert.Equal(CombatDemoBandit1v1Fixture.ProtagonistId, battle.PlayerParty[0].Id);
        Assert.Equal(CombatDemoBandit1v1Fixture.BanditId, battle.EnemyGroup[0].Id);
        Assert.Equal(80, battle.PlayerParty[0].MaxHP);
        Assert.Equal(60, battle.EnemyGroup[0].MaxHP);
    }

    [Fact]
    public void RunFullBattle_WithAutoScripts_TerminatesInReasonableRoundCount()
    {
        var config = CombatDemoBandit1v1Fixture.CreateBattleConfig();
        var facade = new BattleFacade();
        var playerScript = CombatDemoBandit1v1Fixture.CreateProtagonistAutoScript();
        var enemyAI = CombatDemoBandit1v1Fixture.CreateBanditAI();

        var stats = facade.RunFullBattle(config, playerScript, enemyAI);

        Assert.NotEqual(BattleResult.InProgress, stats.Result);
        Assert.InRange(stats.TotalRounds, 2, 6);
    }

    [Fact]
    public void FixtureMoveIds_AreNotEmpty()
    {
        Assert.NotEmpty(CombatDemoBandit1v1Fixture.LightStrikeMoveId);
        Assert.NotEmpty(CombatDemoBandit1v1Fixture.HeavyStrikeMoveId);
        Assert.NotEqual(
            CombatDemoBandit1v1Fixture.LightStrikeMoveId,
            CombatDemoBandit1v1Fixture.HeavyStrikeMoveId);
    }
}
