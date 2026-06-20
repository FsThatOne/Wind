using System.Collections.Generic;
using FengZhi.Foundation.Combat;

namespace Sprint5CombatUiHarness.TestData;

public sealed record Sprint5CombatUiAdapterFixture(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<AdapterFixtureEvent> Events);

public abstract record AdapterFixtureEvent;

public sealed record RoundStartFixtureEvent(int Round) : AdapterFixtureEvent;

public sealed record RoundEndFixtureEvent(int Round) : AdapterFixtureEvent;

public sealed record SynergyDeclaredFixtureEvent(
    IReadOnlyList<string> Sources,
    string Target,
    int Round) : AdapterFixtureEvent;

public sealed record DamageDealtFixtureEvent(
    string Source,
    string Target,
    int Amount,
    bool IsCrit,
    bool IsCounter) : AdapterFixtureEvent;

public static class Sprint5CombatUiAdapterFixtures
{
    public static IReadOnlyList<Sprint5CombatUiAdapterFixture> All { get; } = new[]
    {
        new Sprint5CombatUiAdapterFixture(
            "synergy_gold",
            "SynergyGold / 协同金色双拳",
            "round 3，hero_a + hero_b 同回合对 bandit 使用克制招式 → 金色 synergy_double_fist 浮字。",
            new AdapterFixtureEvent[]
            {
                new RoundStartFixtureEvent(3),
                new DamageDealtFixtureEvent("hero_a", "bandit", 22, false, true),
                new SynergyDeclaredFixtureEvent(new[] { "hero_a", "hero_b" }, "bandit", 3)
            }),

        new Sprint5CombatUiAdapterFixture(
            "no_synergy_baseline",
            "NoSynergyBaseline / 单人无协同",
            "round 3，仅 hero_a 出招命中 bandit，没有 SynergyDeclared 事件 → 协同区为空，turn warning 仍 default。",
            new AdapterFixtureEvent[]
            {
                new RoundStartFixtureEvent(3),
                new DamageDealtFixtureEvent("hero_a", "bandit", 18, false, false)
            }),

        new Sprint5CombatUiAdapterFixture(
            "round_12_caution",
            "Round12Caution / 12 回合橙色警戒",
            "RoundStart(11) → RoundStart(12)：turn warning 切到 caution_orange，ShouldFlashOnEnter=true。",
            new AdapterFixtureEvent[]
            {
                new RoundStartFixtureEvent(11),
                new RoundStartFixtureEvent(12)
            }),

        new Sprint5CombatUiAdapterFixture(
            "round_14_critical",
            "Round14Critical / 14 回合红色危急",
            "RoundStart(12) → RoundStart(14)：turn warning 切到 critical_red，ShouldFlashOnEnter=true。",
            new AdapterFixtureEvent[]
            {
                new RoundStartFixtureEvent(12),
                new RoundStartFixtureEvent(14)
            })
    };
}
