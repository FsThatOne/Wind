using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using Godot;
using System.Reflection;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiResourceBarsDamageFeedbackTest
{
    [Fact]
    public void CombatantResources_BuildHealthNeixiAndStaggerEntriesForAllCombatants()
    {
        var adapter = CreateEnteredAdapter(out _);
        adapter.UpsertCombatantResources("hero", 80, 100, 35, 50, 2);
        adapter.UpsertCombatantResources("bandit", 45, 60, 20, 30, 4);

        var snapshot = adapter.GetSnapshot();

        Assert.Equal(6, snapshot.ResourceEntries.Count);
        Assert.Contains(snapshot.ResourceEntries, entry =>
            entry.CombatantId == "hero"
            && entry.Kind == CombatUiResourceKind.Health
            && entry.Current == 80
            && entry.Maximum == 100
            && entry.FillRatio == 0.8);
        Assert.Contains(snapshot.ResourceEntries, entry =>
            entry.CombatantId == "bandit"
            && entry.Kind == CombatUiResourceKind.Stagger
            && entry.ColorKey == "stagger_jade_crack");
    }

    [Fact]
    public void DamageEvent_UsesResolvedAmountAndDoesNotCreatePredictionNumbers()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        adapter.UpsertCombatantResources("bandit", 100, 100, 20, 30, 0);

        bus.Publish(new DamageDealtEvent("hero", "bandit", 37, false, false));

        var snapshot = adapter.GetSnapshot();
        Assert.Equal(37, snapshot.DamageEntries.Single().Amount);
        Assert.Equal(37, snapshot.DamageNumberEntries.Single().Amount);
        Assert.Equal(0, snapshot.DamageNumberEntries.Single().SequenceId);
        Assert.Contains(snapshot.ResourceEntries, entry =>
            entry.CombatantId == "bandit"
            && entry.Kind == CombatUiResourceKind.Health
            && entry.Current == 100);
        Assert.DoesNotContain(
            typeof(CombatUiSnapshot).GetProperties(),
            property => property.Name.Contains("Prediction", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CombatantResourceSnapshot_MarksHealthFlashWhiteAndSmoothTween()
    {
        var adapter = CreateEnteredAdapter(out _);
        adapter.UpsertCombatantResources("bandit", 100, 100, 20, 30, 0);

        adapter.UpsertCombatantResources("bandit", 75, 100, 20, 30, 0);

        var health = adapter.GetSnapshot().ResourceEntries.Single(entry =>
            entry.CombatantId == "bandit" && entry.Kind == CombatUiResourceKind.Health);
        Assert.Equal(75, health.Current);
        Assert.True(health.ShouldFlashWhite);
        Assert.True(health.ShouldTweenValue);
        Assert.Equal(CombatUiFeedbackTuning.ResourceBarUpdateDurationSeconds, health.UpdateDurationSeconds);
    }

    [Fact]
    public void NeixiChange_MarksImmediateVisibleChange()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        adapter.UpsertCombatantResources("hero", 100, 100, 40, 50, 0);

        bus.Publish(new NeixiChangedEvent("hero", 15));

        var neixi = adapter.GetSnapshot().ResourceEntries.Single(entry =>
            entry.CombatantId == "hero" && entry.Kind == CombatUiResourceKind.Neixi);
        Assert.Equal(15, neixi.Current);
        Assert.True(neixi.IsImmediateValueVisible);
        Assert.True(neixi.ShouldTweenValue);
        Assert.Equal("neixi_ink_blue", neixi.ColorKey);
    }

    [Fact]
    public void StaggerAtThreshold_BuildsPulseAndStampCue()
    {
        var adapter = CreateEnteredAdapter(out var bus);
        adapter.UpsertCombatantResources("bandit", 100, 100, 20, 30, 4);

        bus.Publish(new StaggerChangedEvent("bandit", 5));

        var snapshot = adapter.GetSnapshot();
        var stagger = snapshot.ResourceEntries.Single(entry =>
            entry.CombatantId == "bandit" && entry.Kind == CombatUiResourceKind.Stagger);
        Assert.True(stagger.IsExposed);
        Assert.Equal("stagger_exposed_deep_red", stagger.ColorKey);
        Assert.Equal("resource_stagger_pulse", stagger.VisualKey);

        var cue = snapshot.StaggerCueEntries.Single();
        Assert.Equal("破绽！", cue.Glyph);
        Assert.True(cue.ShouldPulse);
        Assert.Equal(CombatUiFeedbackTuning.StaggerPulseIntervalSeconds, cue.PulseIntervalSeconds);
    }

    [Theory]
    [InlineData(DamageVisualRelation.Advantage, false, CombatUiDamageNumberStyleKind.Advantage, 1.4, "#FFD700", false)]
    [InlineData(DamageVisualRelation.Neutral, false, CombatUiDamageNumberStyleKind.Neutral, 1.0, "#FFFFFF", false)]
    [InlineData(DamageVisualRelation.Disadvantage, false, CombatUiDamageNumberStyleKind.Disadvantage, 0.75, "#AAAAAA", false)]
    [InlineData(DamageVisualRelation.Neutral, true, CombatUiDamageNumberStyleKind.Critical, 1.3, "#FF6600", false)]
    [InlineData(DamageVisualRelation.Decisive, true, CombatUiDamageNumberStyleKind.Decisive, 2.0, "#FF8C00", true)]
    public void DamageNumberStyles_MatchGddFormulaTable(
        DamageVisualRelation relation,
        bool isCrit,
        CombatUiDamageNumberStyleKind expectedKind,
        double expectedScale,
        string expectedColor,
        bool expectedOutline)
    {
        var adapter = CreateEnteredAdapter(out var bus);

        bus.Publish(new DamageDealtEvent("hero", "bandit", 10, isCrit, false, relation));

        var entry = adapter.GetSnapshot().DamageNumberEntries.Single();
        Assert.Equal(expectedKind, entry.StyleKind);
        Assert.Equal(expectedScale, entry.FontScale);
        Assert.Equal(expectedColor, entry.ColorHex);
        Assert.Equal(expectedOutline, entry.HasBlackOutline);
    }

    [Fact]
    public void SameFrameDamage_AssignsReadableStableLanesWithoutDroppingSixEntries()
    {
        var adapter = CreateEnteredAdapter(out var bus);

        for (var i = 0; i < CombatUiFeedbackTuning.MaxConcurrentDamageNumbers; i++)
            bus.Publish(new DamageDealtEvent("hero", $"bandit_{i}", 10 + i, false, false));

        var entries = adapter.GetSnapshot().DamageNumberEntries;
        Assert.Equal(6, entries.Count);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, entries.Select(entry => entry.LaneIndex));
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, entries.Select(entry => entry.SequenceId));
        Assert.Equal(6, entries.Select(entry => entry.VerticalOffsetPixels).Distinct().Count());
    }

    [Fact]
    public void DamageNumberPool_PreallocatesTwelveNonInteractiveLabels()
    {
        Assert.True(typeof(Control).IsAssignableFrom(typeof(DamageNumberPool)));
        Assert.True(typeof(Label).IsAssignableFrom(typeof(DamageNumberLabel)));
        Assert.Equal(CombatUiFeedbackTuning.DamageNumberPoolSize, DamageNumberPool.RequiredPoolSize);
        Assert.Equal(Control.FocusModeEnum.None, DamageNumberLabel.RequiredFocusMode);

        var release = typeof(DamageNumberPool).GetMethod(nameof(DamageNumberPool.Release));
        Assert.NotNull(release);
        Assert.NotNull(typeof(DamageNumberLabel).GetMethod(nameof(DamageNumberLabel.ResetForPool)));
        Assert.NotNull(typeof(DamageNumberLabel).GetProperty(nameof(DamageNumberLabel.LastAcquiredSequence)));
    }

    [Fact]
    public void HudPanel_AppliesResourcesDamageNumbersAndStaggerCues()
    {
        Assert.True(typeof(Control).IsAssignableFrom(typeof(CombatResourceBarSlot)));
        Assert.True(typeof(Label).IsAssignableFrom(typeof(StaggerCueLabel)));
        Assert.Equal(Control.FocusModeEnum.None, CombatResourceBarSlot.RequiredFocusMode);

        Assert.NotNull(typeof(CombatHudPanel).GetMethod(nameof(CombatHudPanel.ApplyResourceSnapshot)));
        Assert.NotNull(typeof(CombatHudPanel).GetMethod(nameof(CombatHudPanel.ApplyDamageNumbers)));
        Assert.NotNull(typeof(CombatHudPanel).GetMethod(nameof(CombatHudPanel.ApplyStaggerCues)));
        Assert.Equal(typeof(DamageNumberPool), typeof(CombatHudPanel).GetProperty(nameof(CombatHudPanel.DamageNumbers))?.PropertyType);

        AssertHasPublicSetterFreeProperty(nameof(CombatResourceBarSlot.LastFlashWhiteRequested));
        AssertHasPublicSetterFreeProperty(nameof(CombatResourceBarSlot.LastTweenRequested));
        AssertHasPublicSetterFreeProperty(nameof(CombatResourceBarSlot.LastPulseRequested));
    }

    private static CombatUiEventAdapter CreateEnteredAdapter(out BattleEventBus bus)
    {
        bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();
        return adapter;
    }

    private static void AssertHasPublicSetterFreeProperty(string propertyName)
    {
        var property = typeof(CombatResourceBarSlot).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(property);
        Assert.Null(property.GetSetMethod(false));
    }
}
