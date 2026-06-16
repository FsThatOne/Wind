using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;
using FengZhi.Foundation.Presentation.Shared;
using Godot;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiMoveSelectionPanelTest
{
    [Fact]
    public void PlayerDecisionPanel_ShowsSixEquippedMovesAndBaseActions()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        var snapshot = presenter.OpenForState(
            CombatUiState.PlayerDecision,
            PanelWithSixMoves(),
            new CombatUiMoveSelectionContext(PlayerNeixi: 9, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: MoveType.Qiao));

        Assert.True(snapshot.IsOpen);
        Assert.Equal(9, snapshot.Entries.Count);
        Assert.Equal(6, snapshot.Entries.Count(entry => entry.ActionKind == CombatUiMoveActionKind.EquippedMove));
        Assert.Contains(snapshot.Entries, entry => entry.ActionKind == CombatUiMoveActionKind.RestMeditate && entry.DisplayName == "调息");
        Assert.Contains(snapshot.Entries, entry => entry.ActionKind == CombatUiMoveActionKind.BasicAttack && entry.DisplayName == "普通攻击");
        Assert.Contains(snapshot.Entries, entry => entry.ActionKind == CombatUiMoveActionKind.UseItem && entry.DisplayName == "使用道具");
    }

    [Fact]
    public void NonPlayerDecisionState_DoesNotOpenMoveSelectionPanel()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.OpenForState(
            CombatUiState.Resolving,
            PanelWithSixMoves(),
            new CombatUiMoveSelectionContext(PlayerNeixi: 9, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: MoveType.Qiao));

        Assert.False(snapshot.IsOpen);
        Assert.Empty(snapshot.Entries);
        Assert.Null(snapshot.DefaultFocusActionId);
    }

    [Fact]
    public void MoveEntry_ShowsNameTypeCostConditionEffectAndXinfaBadge()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("iron_fist", "铁臂功", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 2, "always", "flaw_expose"),
                Move("xinfa_guard", "心法护体", MoveSource.XinfaExclusive, TypeColorTheme.CoolCyan, 3, "when_guarding", "guard_up")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 9, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: null));

        var baseMove = snapshot.Entries.Single(entry => entry.ActionId == "move:iron_fist");
        Assert.Equal("铁臂功", baseMove.DisplayName);
        Assert.Equal(CombatUiIntentIconKind.Gang, baseMove.TypeIconKind);
        Assert.Equal("拳", baseMove.TypeGlyph);
        Assert.Equal(2, baseMove.NeixiCost);
        Assert.Equal("always", baseMove.TriggerConditionIcon);
        Assert.Equal("flaw_expose", baseMove.EffectSummary);
        Assert.False(baseMove.IsXinfaExclusive);

        var xinfaMove = snapshot.Entries.Single(entry => entry.ActionId == "move:xinfa_guard");
        Assert.True(xinfaMove.IsXinfaExclusive);
        Assert.Equal("心法护体", xinfaMove.DisplayName);
    }

    [Fact]
    public void DisabledActions_ExposeExactReasonText()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("expensive_move", "千叶掌", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 7, "second_hit", "multi_strike"),
                Move("xinfa_move", "心法招", MoveSource.XinfaExclusive, TypeColorTheme.NeutralGray, 2, "always", "seal_break")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 3, IsXinfaSealed: true, UsableCombatItemCount: 0, RevealedEnemyMoveType: null));

        Assert.Equal("差 4 内息", snapshot.Entries.Single(entry => entry.ActionId == "move:expensive_move").DisabledReason);
        Assert.Equal("心法封印中", snapshot.Entries.Single(entry => entry.ActionId == "move:xinfa_move").DisabledReason);
        Assert.Equal("无可用战斗道具", snapshot.Entries.Single(entry => entry.ActionId == "use_item").DisabledReason);
    }

    [Fact]
    public void FocusOrHover_UpdatesPreviewWithRelationshipAndCounterHint()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("gang_move", "断岳拳", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 2, "always", "break_guard")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 3, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: MoveType.Qiao));

        var snapshot = presenter.FocusOrHover("move:gang_move");

        Assert.NotNull(snapshot.PreviewCard);
        Assert.Equal(CombatUiMoveTypeRelationship.Advantage, snapshot.PreviewCard!.TypeRelationship);
        Assert.Equal("克制", snapshot.PreviewCard.RelationshipText);
        Assert.Equal("可反制", snapshot.PreviewCard.CounterHint);
    }

    [Theory]
    [InlineData(TypeColorTheme.CoolCyan, MoveType.Qiao, CombatUiMoveTypeRelationship.Disadvantage, "被克", "谨防被反制")]
    [InlineData(TypeColorTheme.NeutralGray, MoveType.Qiao, CombatUiMoveTypeRelationship.Neutral, "中性", "无明显克制")]
    [InlineData(TypeColorTheme.WarmGold, null, CombatUiMoveTypeRelationship.Unknown, "关系未知", "等待目标意图")]
    [InlineData(TypeColorTheme.WarmGold, MoveType.Qiao, CombatUiMoveTypeRelationship.Advantage, "克制", "反制内息不足")]
    public void FocusOrHover_CoversRelationshipMatrixAndLowNeixiHint(
        TypeColorTheme theme,
        MoveType? enemyType,
        CombatUiMoveTypeRelationship expectedRelationship,
        string expectedRelationshipText,
        string expectedCounterHint)
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("matrix_move", "矩阵招式", MoveSource.BaseSlot, theme, 1, "always", "matrix_effect")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 1, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: enemyType));

        var snapshot = presenter.FocusOrHover("move:matrix_move");

        Assert.NotNull(snapshot.PreviewCard);
        Assert.Equal(expectedRelationship, snapshot.PreviewCard!.TypeRelationship);
        Assert.Equal(expectedRelationshipText, snapshot.PreviewCard.RelationshipText);
        Assert.Equal(expectedCounterHint, snapshot.PreviewCard.CounterHint);
    }

    [Fact]
    public void PreviewCardContract_DoesNotExposePredictionOrSettlementFields()
    {
        var forbiddenTokens = new[] { "Damage", "Prediction", "WinRate", "Expected", "Settlement", "Multiplier", "Amount" };

        var dtoProperties = typeof(CombatUiMovePreviewCard).GetProperties().Select(property => property.Name);
        var controlProperties = typeof(CombatMovePreviewCardControl).GetProperties().Select(property => property.Name);

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(dtoProperties, name => name.Contains(token, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(controlProperties, name => name.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void OpenPanel_DefaultFocusLandsOnFirstAvailableAction()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("too_expensive", "昂贵招式", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 9, "always", "heavy_hit"),
                Move("available_move", "可用招式", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 1, "always", "quick_hit")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 2, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: null));

        Assert.Equal("move:available_move", snapshot.DefaultFocusActionId);
    }

    [Fact]
    public void OpenPanel_WhenAllMovesUnavailable_DefaultFocusFallsBackToBaseAction()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("too_expensive_1", "昂贵一式", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 9, "always", "heavy_hit"),
                Move("too_expensive_2", "昂贵二式", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 8, "always", "heavy_hit")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 1, IsXinfaSealed: false, UsableCombatItemCount: 0, RevealedEnemyMoveType: null));

        Assert.Equal("rest_meditate", snapshot.DefaultFocusActionId);
        Assert.True(snapshot.Entries.Single(entry => entry.ActionId == "rest_meditate").IsEnabled);
        Assert.False(snapshot.Entries.Single(entry => entry.ActionId == "use_item").IsEnabled);
    }

    [Fact]
    public void OpenPanel_ClampsEquippedMoveEntriesToSixSlots()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = Enumerable.Range(1, 7)
                .Select(index => Move($"move_{index}", $"招式{index}", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 1, "always", "effect"))
                .ToArray()
        };
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 9, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: null));

        Assert.Equal(CombatUiMoveSelectionPresenter.MaxEquippedMoveSlots, snapshot.Entries.Count(entry => entry.ActionKind == CombatUiMoveActionKind.EquippedMove));
        Assert.DoesNotContain(snapshot.Entries, entry => entry.ActionId == "move:move_7");
    }

    [Fact]
    public void ResourceRefreshWhileOpen_UpdatesAvailabilityWithoutClosingOrResettingInteraction()
    {
        var display = new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("expensive_move", "千叶掌", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 5, "always", "multi_strike")
            }
        };
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            display,
            new CombatUiMoveSelectionContext(PlayerNeixi: 3, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: MoveType.Gang));
        presenter.FocusOrHover("move:expensive_move");

        var refreshed = presenter.RefreshResources(
            new CombatUiMoveSelectionContext(PlayerNeixi: 6, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: MoveType.Gang));

        Assert.True(refreshed.IsOpen);
        Assert.Equal("move:expensive_move", refreshed.SelectedActionId);
        Assert.Null(refreshed.Entries.Single(entry => entry.ActionId == "move:expensive_move").DisabledReason);
        Assert.True(refreshed.Entries.Single(entry => entry.ActionId == "move:expensive_move").IsEnabled);
    }

    [Fact]
    public void MoveSelectionControls_UseInteractiveFocusAndBindSnapshot()
    {
        Assert.True(typeof(BaseUiPanel).IsAssignableFrom(typeof(CombatMoveSelectionPanel)));
        Assert.True(typeof(Control).IsAssignableFrom(typeof(CombatMoveActionSlot)));
        Assert.Equal(Control.FocusModeEnum.All, CombatMoveActionSlot.RequiredFocusMode);
        Assert.Equal(Control.FocusModeEnum.None, CombatMovePreviewCardControl.RequiredFocusMode);
        Assert.Contains(typeof(CombatMoveSelectionPanel).GetConstructors(), constructor =>
        {
            var parameters = constructor.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(IFocusManager);
        });

        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.ApplySnapshot)));
        Assert.NotNull(typeof(CombatMoveSelectionPanel).GetMethod(nameof(CombatMoveSelectionPanel.ClosePanel)));
        Assert.NotNull(typeof(CombatMoveActionSlot).GetMethod(nameof(CombatMoveActionSlot.Configure)));
        Assert.NotNull(typeof(CombatMovePreviewCardControl).GetMethod(nameof(CombatMovePreviewCardControl.Configure)));
    }

    [Fact]
    public void FocusManagerContract_ExposesPushAndPopFocusForPanelIntegration()
    {
        Assert.NotNull(typeof(IFocusManager).GetMethod(nameof(IFocusManager.PushFocus)));
        Assert.NotNull(typeof(IFocusManager).GetMethod(nameof(IFocusManager.PopFocus)));
        Assert.NotNull(typeof(IFocusManager).GetMethod(nameof(IFocusManager.ClearStack)));
        Assert.NotNull(typeof(IFocusManager).GetProperty(nameof(IFocusManager.CurrentMode)));
    }

    [Fact]
    public void FocusLifecycle_PushesOncePerOpenLayerAndPopsOnceOnClose()
    {
        var lifecycle = new CombatUiFocusLifecycle();

        Assert.True(lifecycle.TryBegin("move:first"));
        Assert.False(lifecycle.TryBegin("move:second"));
        Assert.True(lifecycle.IsLayerActive);
        Assert.Equal("move:first", lifecycle.FocusedActionId);

        Assert.True(lifecycle.TryEnd());
        Assert.False(lifecycle.TryEnd());
        Assert.False(lifecycle.IsLayerActive);
        Assert.Null(lifecycle.FocusedActionId);

        Assert.True(lifecycle.TryBegin("move:after_reopen"));
        Assert.Equal("move:after_reopen", lifecycle.FocusedActionId);
    }

    [Fact]
    public void PresenterRefresh_UsesDirtyFlagBatching()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            PanelWithSixMoves(),
            new CombatUiMoveSelectionContext(PlayerNeixi: 9, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: null));

        Assert.True(presenter.RefreshIfDirty());
        Assert.False(presenter.RefreshIfDirty());

        presenter.RefreshResources(new CombatUiMoveSelectionContext(PlayerNeixi: 8, IsXinfaSealed: false, UsableCombatItemCount: 1, RevealedEnemyMoveType: null));

        Assert.True(presenter.RefreshIfDirty());
    }

    private static BattlePanelDisplayData PanelWithSixMoves()
    {
        return new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("move_1", "招式一", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 1, "always", "effect_1"),
                Move("move_2", "招式二", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 2, "always", "effect_2"),
                Move("move_3", "招式三", MoveSource.BaseSlot, TypeColorTheme.NeutralGray, 3, "always", "effect_3"),
                Move("move_4", "招式四", MoveSource.BaseSlot, TypeColorTheme.WarmGold, 4, "always", "effect_4"),
                Move("move_5", "招式五", MoveSource.BaseSlot, TypeColorTheme.CoolCyan, 5, "always", "effect_5"),
                Move("move_6", "招式六", MoveSource.BaseSlot, TypeColorTheme.NeutralGray, 6, "always", "effect_6")
            }
        };
    }

    private static BattlePanelMoveEntry Move(
        string id,
        string name,
        MoveSource source,
        TypeColorTheme theme,
        int neixiCost,
        string trigger,
        string effect)
    {
        return new BattlePanelMoveEntry
        {
            MoveId = id,
            Name = name,
            Source = source,
            ColorTheme = theme,
            NeixiCost = neixiCost,
            EffectiveMultiplier = 1.0f,
            TriggerConditions = new[] { trigger },
            SpecialEffects = new[] { effect }
        };
    }
}
