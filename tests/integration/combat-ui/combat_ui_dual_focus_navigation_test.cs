using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;
using FengZhi.Foundation.Presentation.Shared;
using Godot;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiDualFocusNavigationTest
{
    [Fact]
    public void MovePanelNavigation_DefaultFocusLandsOnFirstAvailableAction()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        var navigation = new CombatUiNavigationController();
        var snapshot = presenter.Open(PanelWithThreeMoves(), Context(playerNeixi: 9));

        var navSnapshot = navigation.ApplySnapshot(snapshot);

        Assert.Equal("move:move_1", navSnapshot.FocusedActionId);
        Assert.True(navSnapshot.IsFocusContained);
        Assert.Equal("keyboard_focus", navSnapshot.VisualState.FocusStyleKey);
    }

    [Fact]
    public void DpadNavigation_ReachesEveryInteractiveAction()
    {
        var navigation = new CombatUiNavigationController();
        navigation.ApplySnapshot(OpenWithCounterAndDecisive());
        var visited = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < 6; index++)
        {
            var snapshot = navigation.Snapshot;
            Assert.NotNull(snapshot.FocusedActionId);
            visited.Add(snapshot.FocusedActionId!);
            navigation.Move(CombatUiNavigationDirection.Down);
        }

        Assert.Contains("decisive_strike", visited);
        Assert.Contains("move:move_1", visited);
        Assert.Contains("move:move_2", visited);
        Assert.Contains("move:move_3", visited);
        Assert.Contains("rest_meditate", visited);
        Assert.Contains("use_item", visited);
    }

    [Fact]
    public void DpadNavigation_WrapsFromLastToFirstAndFirstToLast()
    {
        var navigation = new CombatUiNavigationController();
        navigation.ApplySnapshot(OpenWithCounterAndDecisive());

        var first = navigation.Snapshot.FocusedActionId;
        var upWrapped = navigation.Move(CombatUiNavigationDirection.Up);
        var last = upWrapped.FocusedActionId;
        var downWrapped = navigation.Move(CombatUiNavigationDirection.Down);

        Assert.Equal(first, downWrapped.FocusedActionId);
        Assert.NotEqual(first, last);
        Assert.Equal(first, upWrapped.Nodes.Single(node => node.ActionId == last).DownNeighborActionId);
        Assert.Equal(last, downWrapped.Nodes.Single(node => node.ActionId == first).UpNeighborActionId);
    }

    [Fact]
    public void NavigationGraph_ContainsFocusInsideMovePanelOnly()
    {
        var navigation = new CombatUiNavigationController();
        var snapshot = navigation.ApplySnapshot(OpenWithCounterAndDecisive());
        var actionIds = snapshot.Nodes.Select(node => node.ActionId).ToHashSet(StringComparer.Ordinal);

        Assert.True(snapshot.IsFocusContained);
        Assert.All(snapshot.Nodes, node =>
        {
            Assert.True(node.CanReceiveFocus);
            Assert.Contains(node.UpNeighborActionId, actionIds);
            Assert.Contains(node.DownNeighborActionId, actionIds);
            Assert.DoesNotContain("hud", node.UpNeighborActionId, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pause", node.DownNeighborActionId, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ConfirmButton_SubmitsCurrentlyFocusedAction()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        var navigation = new CombatUiNavigationController();
        var snapshot = presenter.Open(PanelWithThreeMoves(), Context(playerNeixi: 9, currentTargetId: "bandit_a"));
        navigation.ApplySnapshot(snapshot);
        navigation.Move(CombatUiNavigationDirection.Down);

        var intent = navigation.ConfirmFocused(presenter, "hero", "fallback_target");

        Assert.NotNull(intent);
        Assert.Equal("move:move_2", intent!.ActionId);
        Assert.Equal("move_2", intent.MoveId);
        Assert.Equal("bandit_a", intent.TargetId);
    }

    [Fact]
    public void CounterAndDecisiveRows_AreReachableWithGamepadNavigation()
    {
        var navigation = new CombatUiNavigationController();
        navigation.ApplySnapshot(OpenWithCounterAndDecisive());
        var reached = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < 6; index++)
        {
            reached.Add(navigation.Snapshot.FocusedActionId!);
            navigation.Move(CombatUiNavigationDirection.Down);
        }

        Assert.Contains("decisive_strike", reached);
        Assert.Contains("move:move_1", reached);
    }

    [Fact]
    public void CounterActionConfirm_SubmitsCounterIntent()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        var navigation = new CombatUiNavigationController();
        var snapshot = presenter.Open(
            PanelWithThreeMoves(),
            Context(playerNeixi: 9, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));
        navigation.ApplySnapshot(snapshot);

        var intent = navigation.ConfirmFocused(presenter, "hero", "fallback_target");

        Assert.NotNull(intent);
        Assert.Equal("move:move_1", intent!.ActionId);
        Assert.True(intent.IsCounter);
    }

    [Fact]
    public void MouseHoverAndGamepadFocus_CanCoexistAsSeparateVisualStates()
    {
        var navigation = new CombatUiNavigationController();
        navigation.ApplySnapshot(OpenWithCounterAndDecisive());
        navigation.Move(CombatUiNavigationDirection.Down);
        navigation.Move(CombatUiNavigationDirection.Down);

        var hoverSnapshot = navigation.Hover("move:move_1");

        Assert.Equal("move:move_2", hoverSnapshot.FocusedActionId);
        Assert.Equal("move:move_1", hoverSnapshot.HoveredActionId);
        Assert.Equal(hoverSnapshot.FocusedActionId, hoverSnapshot.VisualState.FocusedActionId);
        Assert.Equal(hoverSnapshot.HoveredActionId, hoverSnapshot.VisualState.HoveredActionId);
        Assert.Equal("mouse_hover", hoverSnapshot.VisualState.HoverStyleKey);
    }

    [Fact]
    public void InputModeChanges_UpdateFocusStylingWithoutLosingFocusOrHover()
    {
        var navigation = new CombatUiNavigationController();
        navigation.ApplySnapshot(OpenWithCounterAndDecisive());
        navigation.Move(CombatUiNavigationDirection.Down);
        navigation.Hover("move:move_1");

        var gamepad = navigation.ChangeInputMode(InputMode.Gamepad);
        var keyboard = navigation.ChangeInputMode(InputMode.Keyboard);

        Assert.Equal("move:move_1", gamepad.HoveredActionId);
        Assert.Equal("move:move_1", keyboard.HoveredActionId);
        Assert.Equal(gamepad.FocusedActionId, keyboard.FocusedActionId);
        Assert.Equal("gamepad_focus", gamepad.VisualState.FocusStyleKey);
        Assert.Equal("keyboard_focus", keyboard.VisualState.FocusStyleKey);
    }

    [Fact]
    public void MoveSelectionPanel_BindsNavigationSnapshotAndExposesRequiredControlContracts()
    {
        Assert.True(typeof(BaseUiPanel).IsAssignableFrom(typeof(CombatMoveSelectionPanel)));
        Assert.Equal(Control.FocusModeEnum.All, CombatMoveActionSlot.RequiredFocusMode);
        Assert.Equal(Control.FocusModeEnum.None, CombatMovePreviewCardControl.RequiredFocusMode);
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.NavigateDown)));
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.NavigateUp)));
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.HoverAction)));
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.ChangeInputMode)));
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.ConfirmFocusedAction)));
    }

    [Fact]
    public void MoveSelectionPanel_BuildsCircularGodotFocusNeighborBindings()
    {
        var navigation = new CombatUiNavigationController();
        var snapshot = navigation.ApplySnapshot(OpenWithCounterAndDecisive());

        var bindings = CombatMoveSelectionPanel.BuildFocusNeighborBindings(snapshot);
        var first = bindings[0];
        var last = bindings[^1];

        Assert.Equal(last.ActionId, first.UpNeighborActionId);
        Assert.Equal(bindings[1].ActionId, first.DownNeighborActionId);
        Assert.Equal(bindings[^2].ActionId, last.UpNeighborActionId);
        Assert.Equal(first.ActionId, last.DownNeighborActionId);
    }

    [Fact]
    public void MovePanelNavigation_DefaultFocusSkipsDisabledActions()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        var navigation = new CombatUiNavigationController();
        var snapshot = presenter.Open(
            PanelWithMoves(
                Move("move_1", "断岳拳", TypeColorTheme.WarmGold, neixiCost: 99),
                Move("move_2", "听雨掌", TypeColorTheme.CoolCyan, neixiCost: 1),
                Move("move_3", "流云步", TypeColorTheme.NeutralGray, neixiCost: 1)),
            Context(playerNeixi: 9));

        var navSnapshot = navigation.ApplySnapshot(snapshot);

        Assert.Equal("move:move_2", navSnapshot.FocusedActionId);
        Assert.DoesNotContain(navSnapshot.Nodes, node => node.ActionId == "move:move_1");
    }

    [Fact]
    public void MoveActionSlot_DisabledEntriesCannotReceiveNativeFocus()
    {
        Assert.Equal(Control.FocusModeEnum.All, CombatMoveActionSlot.ResolveFocusMode(isEnabled: true));
        Assert.Equal(Control.FocusModeEnum.None, CombatMoveActionSlot.ResolveFocusMode(isEnabled: false));
    }

    [Fact]
    public void MoveSelectionPanel_DefaultConstructorStillProvidesFocusManager()
    {
        var focusManagerField = typeof(CombatMoveSelectionPanel)
            .GetField("_focusManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(focusManagerField);
        Assert.Equal(typeof(IFocusManager), focusManagerField!.FieldType);
    }

    private static CombatUiMoveSelectionSnapshot OpenWithCounterAndDecisive()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        return presenter.Open(
            PanelWithThreeMoves(),
            Context(
                playerNeixi: 9,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));
    }

    private static CombatUiMoveSelectionContext Context(
        int playerNeixi,
        MoveType? enemyType = null,
        string? currentTargetId = "bandit_a",
        IReadOnlyList<string>? decisiveTargets = null)
    {
        return new CombatUiMoveSelectionContext(
            PlayerNeixi: playerNeixi,
            IsXinfaSealed: false,
            UsableCombatItemCount: 1,
            RevealedEnemyMoveType: enemyType,
            CurrentTargetId: currentTargetId,
            DecisiveStrikeTargetIds: decisiveTargets);
    }

    private static BattlePanelDisplayData PanelWithThreeMoves()
    {
        return PanelWithMoves(
            Move("move_1", "断岳拳", TypeColorTheme.WarmGold),
            Move("move_2", "听雨掌", TypeColorTheme.CoolCyan),
            Move("move_3", "流云步", TypeColorTheme.NeutralGray));
    }

    private static BattlePanelDisplayData PanelWithMoves(params BattlePanelMoveEntry[] moves)
    {
        return new BattlePanelDisplayData
        {
            Entries = moves
        };
    }

    private static BattlePanelMoveEntry Move(string moveId, string name, TypeColorTheme theme, int neixiCost = 1)
    {
        return new BattlePanelMoveEntry
        {
            MoveId = moveId,
            Name = name,
            Source = MoveSource.BaseSlot,
            ColorTheme = theme,
            NeixiCost = neixiCost,
            TriggerConditions = new[] { "always" },
            SpecialEffects = new[] { "effect" }
        };
    }
}
