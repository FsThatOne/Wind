using System.Collections.Generic;
using FengZhi.Foundation.CharacterData;

namespace Sprint5CombatUiHarness.TestData;

/// <summary>
/// cu-006 决胜演出 harness fixture。每个 fixture 描述一种推进 / 暂停场景，
/// 不持有 director 引用：harness panel 自己构造 director + 4 controller，
/// 然后按 fixture 的初始请求和暂停脚本驱动。
/// </summary>
public sealed record Sprint5CombatUiDecisiveFixture(
    string Id,
    string DisplayName,
    string Description,
    string SourceId,
    string TargetId,
    int PrecomputedDamage,
    MoveType MoveType,
    bool TriggersExternalPause);

public static class Sprint5CombatUiDecisiveFixtures
{
    public static IReadOnlyList<Sprint5CombatUiDecisiveFixture> All { get; } = new[]
    {
        new Sprint5CombatUiDecisiveFixture(
            "decisive_gang",
            "DecisiveGang / 刚体系决胜",
            "hero_a 用刚招对 bandit 触发 7 阶段决胜，全程不被打断；TimeScale 应压栈到 0.2，相机锁定 bandit。",
            SourceId: "hero_a",
            TargetId: "bandit",
            PrecomputedDamage: 88,
            MoveType: MoveType.Gang,
            TriggersExternalPause: false),

        new Sprint5CombatUiDecisiveFixture(
            "decisive_rou",
            "DecisiveRou / 柔体系决胜",
            "hero_b 用柔招对 ironcrown 触发决胜；用于确认 PhaseAdvanced 事件携带 MoveType=Rou。",
            SourceId: "hero_b",
            TargetId: "ironcrown",
            PrecomputedDamage: 72,
            MoveType: MoveType.Rou,
            TriggersExternalPause: false),

        new Sprint5CombatUiDecisiveFixture(
            "decisive_qiao",
            "DecisiveQiao / 巧体系决胜",
            "hero_c 用巧招对 elder_qiao 触发决胜；体系颜色与浮字样式都走 Decisive 样式表。",
            SourceId: "hero_c",
            TargetId: "elder_qiao",
            PrecomputedDamage: 95,
            MoveType: MoveType.Qiao,
            TriggersExternalPause: false),

        new Sprint5CombatUiDecisiveFixture(
            "decisive_pause_conflict",
            "DecisivePauseConflict / 决胜遇暂停",
            "决胜进入慢动作后再触发 ui_pause 优先级 100；TimeScale 应被覆盖为 0，sequence 暂停推进；释放暂停后回到 0.2 并继续。",
            SourceId: "hero_a",
            TargetId: "bandit",
            PrecomputedDamage: 88,
            MoveType: MoveType.Gang,
            TriggersExternalPause: true)
    };
}
