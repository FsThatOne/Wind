using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiSynergyRoundWarningTest
{
    [Fact]
    public void SynergyDeclared_SameRoundSameTargetCounter_AddsGoldDoubleFistCue()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        bus.Publish(new RoundStartEvent(3));

        bus.Publish(new SynergyDeclaredEvent(
            new[] { "hero_a", "hero_b" },
            "bandit",
            3));

        var cue = adapter.GetSnapshot().SynergyCueEntries.Single();
        Assert.Equal("bandit", cue.TargetId);
        Assert.Equal(new[] { "hero_a", "hero_b" }, cue.SourceActorIds);
        Assert.Equal(3, cue.RoundNumber);
        Assert.Equal("synergy_double_fist", cue.Glyph);
        Assert.Equal("synergy_gold", cue.ColorKey);
    }

    [Fact]
    public void SynergyCue_FollowsTargetAndRunsParallelWithDamage()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        bus.Publish(new RoundStartEvent(4));
        bus.Publish(new DamageDealtEvent("hero_a", "bandit", 22, false, true, DamageVisualRelation.Advantage));

        bus.Publish(new SynergyDeclaredEvent(
            new[] { "hero_a", "hero_b" },
            "bandit",
            4));

        var snapshot = adapter.GetSnapshot();
        var cue = snapshot.SynergyCueEntries.Single();
        var damage = snapshot.DamageNumberEntries.Single();
        Assert.True(cue.FollowsTarget);
        Assert.True(cue.IsParallelWithDamage);
        Assert.Equal("bandit", cue.TargetId);
        Assert.Equal("bandit", damage.TargetId);
        Assert.Equal(CombatUiFeedbackTuning.SynergyCueDurationSeconds, cue.DurationSeconds);
    }

    [Fact]
    public void SynergyCue_NotEmittedForSingleSourceOrEmptyEvent()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        bus.Publish(new RoundStartEvent(2));

        bus.Publish(new SynergyDeclaredEvent(new[] { "hero_a" }, "bandit", 2));
        bus.Publish(new SynergyDeclaredEvent(Array.Empty<string>(), "bandit", 2));

        Assert.Empty(adapter.GetSnapshot().SynergyCueEntries);
    }

    [Fact]
    public void TurnWarning_ReflectsCurrentRoundNumber()
    {
        var adapter = CreateEnteredAdapter(out var bus);

        bus.Publish(new RoundStartEvent(7));

        var snapshot = adapter.GetSnapshot();
        Assert.Equal(7, snapshot.RoundNumber);
        Assert.Equal(7, snapshot.TurnWarning.RoundNumber);
        Assert.Equal(CombatUiTurnWarningKind.Normal, snapshot.TurnWarning.Kind);
        Assert.Equal("turn_counter_default", snapshot.TurnWarning.ColorKey);
    }

    [Fact]
    public void TurnWarning_AtRoundTwelve_SwitchesToCautionOrange()
    {
        var adapter = CreateEnteredAdapter(out var bus);

        bus.Publish(new RoundStartEvent(12));

        var warning = adapter.GetSnapshot().TurnWarning;
        Assert.Equal(CombatUiTurnWarningKind.Caution, warning.Kind);
        Assert.Equal("turn_warning_caution_orange", warning.ColorKey);
        Assert.True(warning.ShouldFlashOnEnter);
    }

    [Fact]
    public void TurnWarning_AtRoundFourteen_SwitchesToCriticalRed()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        bus.Publish(new RoundStartEvent(12));

        bus.Publish(new RoundStartEvent(14));

        var warning = adapter.GetSnapshot().TurnWarning;
        Assert.Equal(CombatUiTurnWarningKind.Critical, warning.Kind);
        Assert.Equal("turn_warning_critical_red", warning.ColorKey);
        Assert.True(warning.ShouldFlashOnEnter);
    }

    [Fact]
    public void TurnWarningTransition_DoesNotMutateResourcesOrDamageNumbers()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        bus.Publish(new RoundStartEvent(11));
        adapter.UpsertCombatantResources("hero", 80, 100, 30, 50, 1);
        bus.Publish(new DamageDealtEvent("hero", "bandit", 17, false, false, DamageVisualRelation.Neutral));

        var beforeSnapshot = adapter.GetSnapshot();
        var beforeResources = beforeSnapshot.ResourceEntries.ToArray();
        var beforeDamage = beforeSnapshot.DamageNumberEntries.ToArray();
        Assert.Equal(CombatUiTurnWarningKind.Normal, beforeSnapshot.TurnWarning.Kind);

        // RoundEnd advances the warning without wiping per-round buffers.
        bus.Publish(new RoundEndEvent(12));
        var atCaution = adapter.GetSnapshot();
        Assert.Equal(CombatUiTurnWarningKind.Caution, atCaution.TurnWarning.Kind);
        Assert.Equal(beforeResources.Length, atCaution.ResourceEntries.Count);
        Assert.Equal(beforeDamage.Length, atCaution.DamageNumberEntries.Count);
        Assert.Equal(beforeDamage[0].Amount, atCaution.DamageNumberEntries[0].Amount);
        Assert.Equal(beforeResources[0].Current, atCaution.ResourceEntries[0].Current);

        bus.Publish(new RoundEndEvent(14));
        var atCritical = adapter.GetSnapshot();
        Assert.Equal(CombatUiTurnWarningKind.Critical, atCritical.TurnWarning.Kind);
        Assert.Equal(beforeResources.Length, atCritical.ResourceEntries.Count);
        Assert.Equal(beforeDamage.Length, atCritical.DamageNumberEntries.Count);
    }

    private static CombatUiEventAdapter CreateEnteredAdapter(out BattleEventBus bus)
    {
        bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();
        return adapter;
    }
}
