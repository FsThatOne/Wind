using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using Godot;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiIntentIconsHudSummaryTest
{
    [Fact]
    public void OnIntentRevealed_BuildsWorldAndHudIntentEntries()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.Normal, null, MoveType.Gang),
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Rou)
        }));

        var snapshot = adapter.GetSnapshot();
        Assert.Equal(2, snapshot.WorldIntentEntries.Count);
        Assert.Equal(2, snapshot.HudIntentEntries.Count);
        Assert.Equal(CombatUiIntentIconKind.Gang, snapshot.WorldIntentEntries[0].IconKind);
        Assert.Equal(CombatUiIntentIconKind.Rou, snapshot.HudIntentEntries[1].IconKind);
        Assert.Equal(snapshot.WorldIntentEntries.Select(entry => entry.EnemyId), snapshot.HudIntentEntries.Select(entry => entry.EnemyId));
    }

    [Fact]
    public void HudSummary_ShowsAllEnemyIntentsFromSingleSnapshot()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.Normal, null, MoveType.Gang),
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Rou),
            new EnemyIntent("bandit_c", IntentVisibility.Normal, null, MoveType.Qiao)
        }));

        var snapshot = adapter.GetSnapshot();
        Assert.Collection(
            snapshot.HudIntentEntries,
            gang => Assert.Equal(CombatUiIntentIconKind.Gang, gang.IconKind),
            rou => Assert.Equal(CombatUiIntentIconKind.Rou, rou.IconKind),
            qiao => Assert.Equal(CombatUiIntentIconKind.Qiao, qiao.IconKind));
    }

    [Fact]
    public void HiddenIntent_ShowsUnknownFogAndDoesNotLeakMoveType()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("assassin", IntentVisibility.Hidden, "ShadowBlade", MoveType.Qiao)
        }));

        var entry = adapter.GetSnapshot().HudIntentEntries.Single();
        Assert.Equal(CombatUiIntentIconKind.Unknown, entry.IconKind);
        Assert.Equal("?", entry.Glyph);
        Assert.Equal("intent_unknown_fog", entry.ColorKey);
        Assert.True(entry.IsUnknown);
        Assert.Null(entry.RevealedMoveName);
    }

    [Fact]
    public void UnknownToRevealedIntent_MarksRevealTransition()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("assassin", IntentVisibility.Hidden, "ShadowBlade", MoveType.Qiao)
        }));
        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("assassin", IntentVisibility.FullReveal, "ShadowBlade", MoveType.Qiao)
        }));

        var entry = adapter.GetSnapshot().WorldIntentEntries.Single();
        Assert.Equal(CombatUiIntentIconKind.Qiao, entry.IconKind);
        Assert.True(entry.ShouldPlayRevealTransition);
        Assert.Equal("ShadowBlade", entry.RevealedMoveName);
    }

    [Fact]
    public void DefeatedTargetIntent_DimsSlotAndSkipsTransitionWithoutCrash()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        adapter.MarkCombatantDefeated("bandit_a");
        var exception = Record.Exception(() => bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.FullReveal, "OverheadSlash", MoveType.Gang)
        })));

        Assert.Null(exception);
        var entry = adapter.GetSnapshot().WorldIntentEntries.Single();
        Assert.Equal(CombatUiIntentSlotState.Dimmed, entry.SlotState);
        Assert.True(entry.IsDimmed);
        Assert.False(entry.ShouldPlayRevealTransition);
    }

    [Fact]
    public void MultipleIntentRefreshes_KeepHudOrderStable()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Rou),
            new EnemyIntent("bandit_a", IntentVisibility.Hidden, null, null),
            new EnemyIntent("bandit_c", IntentVisibility.Normal, null, MoveType.Qiao)
        }));
        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_c", IntentVisibility.Normal, null, MoveType.Gang),
            new EnemyIntent("bandit_a", IntentVisibility.Normal, null, MoveType.Rou),
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Qiao)
        }));

        var orderedIds = adapter.GetSnapshot().HudIntentEntries.Select(entry => entry.EnemyId).ToArray();
        Assert.Equal(new[] { "bandit_b", "bandit_a", "bandit_c" }, orderedIds);
    }

    [Fact]
    public void IntentSnapshotOmittingEnemy_RemovesStaleWorldAndHudEntries()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.Normal, null, MoveType.Gang),
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Rou),
            new EnemyIntent("bandit_c", IntentVisibility.Normal, null, MoveType.Qiao)
        }));
        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.Normal, null, MoveType.Gang),
            new EnemyIntent("bandit_b", IntentVisibility.Normal, null, MoveType.Rou)
        }));

        var snapshot = adapter.GetSnapshot();
        Assert.DoesNotContain(snapshot.WorldIntentEntries, entry => entry.EnemyId == "bandit_c");
        Assert.DoesNotContain(snapshot.HudIntentEntries, entry => entry.EnemyId == "bandit_c");
        Assert.Equal(new[] { "bandit_a", "bandit_b" }, snapshot.HudIntentEntries.Select(entry => entry.EnemyId));
    }

    [Fact]
    public void IntentSlotContract_UsesNonInteractiveFocusMode()
    {
        Assert.True(typeof(Control).IsAssignableFrom(typeof(CombatIntentIconSlot)));
        Assert.Equal(Control.FocusModeEnum.None, CombatIntentIconSlot.RequiredFocusMode);
    }

    [Fact]
    public void IntentSlotContract_DefinesRevealTweenParameters()
    {
        Assert.True(CombatIntentIconSlot.IntentIconFadeInDurationSeconds > 0);
        Assert.True(CombatIntentIconSlot.IntentIconRisePixels > 0);
    }
}
