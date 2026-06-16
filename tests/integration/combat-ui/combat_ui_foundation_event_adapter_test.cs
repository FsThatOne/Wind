using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.Presentation.Shared;
using Godot;
using System.Reflection;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiFoundationEventAdapterTest
{
    [Fact]
    public void Layers_DefineWorldIntentAndHudCanvasOrders()
    {
        var adapter = new CombatUiEventAdapter(new BattleEventBus());

        Assert.Collection(
            adapter.Layers,
            world =>
            {
                Assert.Equal("WorldIntentLayer", world.Name);
                Assert.Equal(CombatUiLayerKind.WorldIntent, world.Kind);
                Assert.Equal(10, world.CanvasLayerOrder);
                Assert.False(world.IsScreenSpace);
            },
            hud =>
            {
                Assert.Equal("HUDLayer", hud.Name);
                Assert.Equal(CombatUiLayerKind.Hud, hud.Kind);
                Assert.Equal(20, hud.CanvasLayerOrder);
                Assert.True(hud.IsScreenSpace);
            });
    }

    [Fact]
    public void GodotLayers_DefineConcreteCanvasLayerAndBasePanelTypes()
    {
        Assert.True(typeof(CanvasLayer).IsAssignableFrom(typeof(WorldIntentLayer)));
        Assert.True(typeof(CanvasLayer).IsAssignableFrom(typeof(CombatHudLayer)));
        Assert.True(typeof(BaseUiPanel).IsAssignableFrom(typeof(WorldIntentPanel)));
        Assert.True(typeof(BaseUiPanel).IsAssignableFrom(typeof(CombatHudPanel)));

        Assert.Equal(typeof(WorldIntentPanel), typeof(WorldIntentLayer).GetProperty(nameof(WorldIntentLayer.Panel))?.PropertyType);
        Assert.Equal(typeof(CombatHudPanel), typeof(CombatHudLayer).GetProperty(nameof(CombatHudLayer.Panel))?.PropertyType);
    }

    [Fact]
    public void CombatUiRoot_ProvidesBattleEntryForLayerCreationAndAdapterSubscription()
    {
        Assert.True(typeof(Node).IsAssignableFrom(typeof(CombatUiRoot)));
        Assert.Equal(typeof(WorldIntentLayer), typeof(CombatUiRoot).GetProperty(nameof(CombatUiRoot.WorldIntentLayer))?.PropertyType);
        Assert.Equal(typeof(CombatHudLayer), typeof(CombatUiRoot).GetProperty(nameof(CombatUiRoot.HudLayer))?.PropertyType);
        Assert.Equal(typeof(CombatUiEventAdapter), typeof(CombatUiRoot).GetProperty(nameof(CombatUiRoot.Adapter))?.PropertyType);

        var enterBattle = typeof(CombatUiRoot).GetMethod(nameof(CombatUiRoot.EnterBattle));
        Assert.NotNull(enterBattle);
        Assert.Equal(typeof(CombatUiEventAdapter), enterBattle.ReturnType);
        Assert.Equal(typeof(BattleEventBus), enterBattle.GetParameters().Single().ParameterType);

        var exitTree = typeof(CombatUiRoot).GetMethod(
            "_ExitTree",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(exitTree);
    }

    [Fact]
    public void BattleEvents_UpdateReadonlyUiSnapshot()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new IntentRevealedEvent(new[]
        {
            new EnemyIntent("bandit_a", IntentVisibility.FullReveal, "OverheadSlash"),
            new EnemyIntent("bandit_b", IntentVisibility.Hidden, null)
        }));
        bus.Publish(new DamageDealtEvent("hero", "bandit_a", 37, true, false));
        bus.Publish(new StaggerChangedEvent("bandit_a", 3));
        bus.Publish(new NeixiChangedEvent("hero", 42));
        bus.Publish(new DecisiveStrikeAvailableEvent("bandit_a"));

        var snapshot = adapter.GetSnapshot();
        Assert.Equal(CombatUiState.Resolving, snapshot.State);
        Assert.Equal(2, snapshot.IntentEntries.Count);
        Assert.Equal("OverheadSlash", snapshot.IntentEntries[0].MoveTypeName);
        Assert.Equal(37, snapshot.DamageEntries.Single().Amount);
        Assert.True(snapshot.DamageEntries.Single().IsCrit);
        Assert.Equal(3, snapshot.StaggerEntries.Single().NewStagger);
        Assert.Equal(42, snapshot.NeixiEntries.Single().NewValue);
        Assert.Equal("bandit_a", snapshot.DecisiveStrikeTargets.Single());

        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<CombatUiDamageEntry>)snapshot.DamageEntries).Add(
                new CombatUiDamageEntry("ui", "target", 999, false, false)));
    }

    [Fact]
    public void StateFlow_AdvancesFromBattleStartToBattleEnd()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new RoundStartEvent(1));
        Assert.Equal(CombatUiState.RoundStart, adapter.GetSnapshot().State);

        bus.Publish(new IntentRevealedEvent(Array.Empty<EnemyIntent>()));
        Assert.Equal(CombatUiState.PlayerDecision, adapter.GetSnapshot().State);

        bus.Publish(new DamageDealtEvent("hero", "bandit", 10, false, false));
        Assert.Equal(CombatUiState.Resolving, adapter.GetSnapshot().State);

        bus.Publish(new RoundEndEvent(1));
        Assert.Equal(CombatUiState.RoundEnd, adapter.GetSnapshot().State);

        bus.Publish(new BattleEndEvent(BattleResult.Victory));
        var snapshot = adapter.GetSnapshot();
        Assert.Equal(CombatUiState.BattleEnd, snapshot.State);
        Assert.Equal(BattleResult.Victory, snapshot.BattleResult);
    }

    [Fact]
    public void Adapter_DoesNotComputeCombatResultOrHoldBattleInstance()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new DamageDealtEvent("hero", "bandit", 37, false, false));

        var snapshot = adapter.GetSnapshot();
        Assert.Equal(37, snapshot.DamageEntries.Single().Amount);
        Assert.Equal(BattleResult.InProgress, snapshot.BattleResult);
        Assert.DoesNotContain(
            typeof(CombatUiEventAdapter).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public),
            field => field.FieldType == typeof(BattleInstance));
    }

    [Fact]
    public void Dispose_RemovesEventSubscriptions()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();

        bus.Publish(new RoundStartEvent(1));
        Assert.Equal(CombatUiState.RoundStart, adapter.GetSnapshot().State);

        adapter.Dispose();
        bus.Publish(new BattleEndEvent(BattleResult.Defeat));

        Assert.Equal(CombatUiState.RoundStart, adapter.State);
        Assert.Equal(BattleResult.InProgress, adapter.BattleResult);
        Assert.Throws<ObjectDisposedException>(() => adapter.GetSnapshot());
    }

    [Fact]
    public void EnterBattle_IsIdempotentAndDoesNotDuplicateSubscriptions()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();
        adapter.EnterBattle();

        bus.Publish(new DamageDealtEvent("hero", "bandit", 12, false, false));

        Assert.Single(adapter.GetSnapshot().DamageEntries);
    }

    [Fact]
    public void DirtyRefresh_BatchesMultipleEventsIntoSingleRefresh()
    {
        var bus = new BattleEventBus();
        var adapter = new CombatUiEventAdapter(bus);
        adapter.EnterBattle();
        adapter.RefreshIfDirty();

        bus.Publish(new RoundStartEvent(1));
        bus.Publish(new DamageDealtEvent("hero", "bandit", 12, false, false));
        bus.Publish(new NeixiChangedEvent("hero", 30));

        var dirtySnapshot = adapter.GetSnapshot();
        Assert.True(dirtySnapshot.IsDirty);
        Assert.Equal(1, dirtySnapshot.RefreshCount);

        Assert.True(adapter.RefreshIfDirty());
        var cleanSnapshot = adapter.GetSnapshot();
        Assert.False(cleanSnapshot.IsDirty);
        Assert.Equal(2, cleanSnapshot.RefreshCount);

        Assert.False(adapter.RefreshIfDirty());
        Assert.Equal(2, adapter.GetSnapshot().RefreshCount);
        Assert.Equal(1.0, CombatUiEventAdapter.HudRefreshBudgetMilliseconds);
    }
}
