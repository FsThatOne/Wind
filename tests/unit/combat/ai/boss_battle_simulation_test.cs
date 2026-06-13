using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;
using Xunit.Abstractions;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// Boss 战斗完整模拟（可视化验证 AI 决策管线行为）。
/// </summary>
public class BossBattleSimulation
{
    private readonly ITestOutputHelper _output;
    public BossBattleSimulation(ITestOutputHelper output) => _output = output;

    [Fact]
    public void SimulateBossBattle_15Rounds_FullPipeline()
    {
        var random = new SeededAIRandom(123);
        var brain = new EnemyBrain(BossPhaseScript.StandardFourPhase, null, random);

        var moves = new List<AIMoveEntry>
        {
            new() { Id = "g1", Name = "猛虎下山", Type = MoveType.Gang, NeixiCost = 2, DamageMultiplier = 1.2f },
            new() { Id = "g2", Name = "开山掌", Type = MoveType.Gang, NeixiCost = 4, DamageMultiplier = 1.6f },
            new() { Id = "r1", Name = "绵掌", Type = MoveType.Rou, NeixiCost = 2, DamageMultiplier = 1.0f },
            new() { Id = "r2", Name = "太极云手", Type = MoveType.Rou, NeixiCost = 3, DamageMultiplier = 1.3f },
            new() { Id = "q1", Name = "燕子穿林", Type = MoveType.Qiao, NeixiCost = 1, DamageMultiplier = 0.8f },
            new() { Id = "q2", Name = "暗器连发", Type = MoveType.Qiao, NeixiCost = 3, DamageMultiplier = 1.4f },
        };

        float bossHp = 1.0f;
        int neixi = 10;
        int prevPhase = 0;

        _output.WriteLine("=== Boss 战斗模拟 (15 回合) ===");
        _output.WriteLine($"{"回合",-4} {"HP",-7} {"阶段",-6} {"行动",-14} {"体系",-6} {"招式",-10} {"反读",-4} {"优先",-4}");
        _output.WriteLine(new string('-', 70));

        for (int round = 1; round <= 15; round++)
        {
            brain.AdvanceRound();

            // 模拟玩家持续使用柔系（测试反读触发）
            brain.RecordPlayerAction("player1", MoveType.Rou);

            var ctx = new AIBattleContext
            {
                CurrentNeixi = neixi,
                CurrentRound = round,
                HpRatio = bossHp,
                AvailableMoves = moves,
                Targets = new[] { new TargetCandidate { Id = "player1", HpRatio = 0.7f } }
            };

            var decision = brain.Decide(ctx);

            // 检测阶段转换
            string phaseLabel = brain.BossManager!.CurrentPhase.Label;
            if (brain.BossManager.CurrentPhaseIndex != prevPhase)
            {
                _output.WriteLine($"  >>> 阶段转换！进入 Phase {brain.BossManager.CurrentPhaseIndex}: {phaseLabel} <<<");
                prevPhase = brain.BossManager.CurrentPhaseIndex;
            }

            string actionStr = decision.ActionType switch
            {
                AIActionType.Meditate => "调息",
                AIActionType.ChargeAnnounce => $"蓄力预告[{decision.ChargeAnnounceName}]",
                _ => "攻击"
            };
            string typeStr = decision.SelectedType?.ToString() ?? "-";
            string moveStr = decision.SelectedMove?.Name ?? "-";

            _output.WriteLine($"R{round:D2}   {bossHp:P0}  {phaseLabel,-5} {actionStr,-12} {typeStr,-5} {moveStr,-9} {(decision.CounterReadTriggered ? "是" : ""),-3} {(decision.IsPriorityStrike ? "是" : ""),-3}");

            // 模拟战斗推进
            if (decision.ActionType == AIActionType.Attack)
                neixi = Math.Max(1, neixi - (decision.SelectedMove?.NeixiCost ?? 0));
            else if (decision.ActionType == AIActionType.Meditate)
                neixi = Math.Min(10, neixi + 3);

            bossHp -= 0.065f; // 模拟每回合扣血
            brain.UpdateHp(bossHp);
        }

        _output.WriteLine(new string('-', 70));
        _output.WriteLine($"最终状态: HP={bossHp:P0}, Phase={brain.BossManager.CurrentPhaseIndex}({brain.BossManager.CurrentPhase.Label}), 内息={neixi}");

        // 基本断言：管线运行完毕无异常
        Assert.True(brain.BossManager.CurrentPhaseIndex >= 2, "15回合后应至少进入 Phase 3a");
    }
}
