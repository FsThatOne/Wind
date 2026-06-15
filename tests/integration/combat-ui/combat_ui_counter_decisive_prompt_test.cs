using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiCounterDecisivePromptTest
{
    [Fact]
    public void CounterPrompt_WhenMoveCountersPublicIntentAndNeixiEnough_ShowsEnabledGoldLabel()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 3, enemyType: MoveType.Qiao));

        var move = snapshot.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.NotNull(move.CounterPrompt);
        Assert.True(move.CounterPrompt!.IsVisible);
        Assert.True(move.CounterPrompt.IsEnabled);
        Assert.Equal("反制", move.CounterPrompt.Label);
        Assert.Equal("counter_gold", move.CounterPrompt.ColorKey);
        Assert.Null(move.CounterPrompt.DisabledReason);
    }

    [Fact]
    public void CounterPrompt_WhenNeixiTooLow_ShowsDisabledInsufficientNeixiLabel()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 2, enemyType: MoveType.Qiao));

        var move = snapshot.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.NotNull(move.CounterPrompt);
        Assert.True(move.CounterPrompt!.IsVisible);
        Assert.False(move.CounterPrompt.IsEnabled);
        Assert.Equal("反制 / 内息不足", move.CounterPrompt.Label);
        Assert.Equal("counter_disabled_gray", move.CounterPrompt.ColorKey);
        Assert.Equal("内息不足", move.CounterPrompt.DisabledReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(MoveType.Gang)]
    [InlineData(MoveType.Rou)]
    public void CounterPrompt_WhenIntentUnknownNeutralOrDisadvantage_DoesNotShowCounterLabel(MoveType? enemyType)
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 3, enemyType: enemyType));

        var move = snapshot.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.Null(move.CounterPrompt);
    }

    [Fact]
    public void ConfirmSelected_WhenCounterAvailable_EmitsMoveIntentWithIsCounterTrue()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 3, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));
        presenter.FocusOrHover("move:gang_break");

        var intent = presenter.ConfirmSelected("hero", "fallback_target");

        Assert.NotNull(intent);
        Assert.Equal("hero", intent!.ActorId);
        Assert.Equal("bandit_a", intent.TargetId);
        Assert.Equal("move:gang_break", intent.ActionId);
        Assert.Equal("gang_break", intent.MoveId);
        Assert.True(intent.IsCounter);
        Assert.False(intent.IsDecisiveStrike);
    }

    [Fact]
    public void ConfirmSelected_WhenCounterDisabledByLowNeixi_DoesNotEmitCounterIntent()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 2, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));
        presenter.FocusOrHover("move:gang_break");

        var intent = presenter.ConfirmSelected("hero", "fallback_target");

        Assert.NotNull(intent);
        Assert.False(intent!.IsCounter);
        Assert.False(intent.IsDecisiveStrike);
    }

    [Fact]
    public void DecisivePrompt_WhenCurrentTargetStaggerExposed_InsertsTopHighlightRow()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));

        var decisive = snapshot.Entries.First();
        Assert.Equal(CombatUiMoveActionKind.DecisiveStrike, decisive.ActionKind);
        Assert.Equal("decisive_strike", decisive.ActionId);
        Assert.Equal("▶ 决胜一击", decisive.DisplayName);
        Assert.True(decisive.IsDecisiveStrike);
        Assert.Equal("bandit_a", decisive.TargetId);
        Assert.Equal(0, decisive.StableOrder);
        Assert.Equal("decisive_strike", snapshot.DefaultFocusActionId);
    }

    [Fact]
    public void DecisivePrompt_WhenNoTargetQualifies_DoesNotInsertDecisiveRow()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 3, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));

        Assert.DoesNotContain(snapshot.Entries, entry => entry.IsDecisiveStrike);
        Assert.DoesNotContain(snapshot.Entries, entry => entry.ActionId == "decisive_strike");
        Assert.Equal("move:gang_break", snapshot.DefaultFocusActionId);
    }

    [Fact]
    public void Refresh_WhenCurrentTargetNoLongerQualifies_RemovesDecisiveRow()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));

        var refreshed = presenter.RefreshResources(
            Context(playerNeixi: 3, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));

        Assert.True(refreshed.IsOpen);
        Assert.DoesNotContain(refreshed.Entries, entry => entry.IsDecisiveStrike);
        Assert.DoesNotContain(refreshed.Entries, entry => entry.ActionId == "decisive_strike");
    }

    [Fact]
    public void Refresh_WhenCurrentTargetRemoved_ClearsCounterAndDecisivePromptsSafely()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));

        var refreshed = presenter.RefreshResources(
            Context(playerNeixi: 3, enemyType: null, currentTargetId: null));

        var move = refreshed.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.True(refreshed.IsOpen);
        Assert.Null(move.CounterPrompt);
        Assert.Null(move.TargetId);
        Assert.DoesNotContain(refreshed.Entries, entry => entry.IsDecisiveStrike);
    }

    [Fact]
    public void DecisivePrompt_WhenCurrentTargetNotQualified_DefaultsToFirstQualifiedTarget()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_c",
                decisiveTargets: new[] { "bandit_a", "bandit_b" }));

        var decisive = snapshot.Entries.Single(entry => entry.IsDecisiveStrike);
        Assert.Equal("bandit_a", decisive.TargetId);
        Assert.Equal("decisive_strike", snapshot.DefaultFocusActionId);
    }

    [Fact]
    public void DecisivePrompt_WhenMultipleTargetsQualify_RefreshesHighlightAfterTargetSwitch()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a", "bandit_b" }));

        var switched = presenter.RefreshResources(
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_b",
                decisiveTargets: new[] { "bandit_a", "bandit_b" }));

        var decisive = switched.Entries.Single(entry => entry.IsDecisiveStrike);
        Assert.True(switched.IsOpen);
        Assert.Equal("bandit_b", decisive.TargetId);
        Assert.Equal("decisive_strike", decisive.ActionId);
        Assert.DoesNotContain(switched.Entries, entry => entry.IsDecisiveStrike && entry.TargetId == "bandit_a");
    }

    [Fact]
    public void CounterAndDecisivePrompts_WhenBothAvailable_CoexistWithoutOrderingConflict()
    {
        var presenter = new CombatUiMoveSelectionPresenter();

        var snapshot = presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));

        var decisive = snapshot.Entries.First();
        var move = snapshot.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.True(decisive.IsDecisiveStrike);
        Assert.Equal(0, decisive.StableOrder);
        Assert.Equal("decisive_strike", snapshot.DefaultFocusActionId);
        Assert.NotNull(move.CounterPrompt);
        Assert.True(move.CounterPrompt!.IsEnabled);
        Assert.True(move.StableOrder > decisive.StableOrder);
    }

    [Fact]
    public void ResourceRefreshWhileOpen_UpdatesCounterAvailabilityWithoutClosingPanel()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(playerNeixi: 2, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));
        presenter.FocusOrHover("move:gang_break");

        var refreshed = presenter.RefreshResources(
            Context(playerNeixi: 3, enemyType: MoveType.Qiao, currentTargetId: "bandit_a"));

        var move = refreshed.Entries.Single(entry => entry.ActionId == "move:gang_break");
        Assert.True(refreshed.IsOpen);
        Assert.Equal("move:gang_break", refreshed.SelectedActionId);
        Assert.NotNull(move.CounterPrompt);
        Assert.True(move.CounterPrompt!.IsEnabled);
        Assert.Equal("反制", move.CounterPrompt.Label);
    }

    [Fact]
    public void CombatUiContracts_DoNotExposeCounterSettlementOrDamageCalculation()
    {
        var forbiddenTokens = new[] { "Damage", "Settlement", "Expected", "WinRate", "Amount", "Deduct", "StaggerResult" };
        var entryProperties = typeof(CombatUiMoveSelectionEntry).GetProperties().Select(property => property.Name);
        var intentProperties = typeof(CombatUiMoveSelectionIntent).GetProperties().Select(property => property.Name);
        var promptProperties = typeof(CombatUiCounterPrompt).GetProperties().Select(property => property.Name);

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(entryProperties, name => name.Contains(token, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(intentProperties, name => name.Contains(token, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(promptProperties, name => name.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        Assert.Contains(intentProperties, name => name == nameof(CombatUiMoveSelectionIntent.IsCounter));
        Assert.Contains(intentProperties, name => name == nameof(CombatUiMoveSelectionIntent.IsDecisiveStrike));
    }

    [Fact]
    public void ConfirmSelected_WhenDecisiveRowSelected_EmitsDecisiveIntentWithoutCounter()
    {
        var presenter = new CombatUiMoveSelectionPresenter();
        presenter.Open(
            DisplayWithMove("gang_break", TypeColorTheme.WarmGold),
            Context(
                playerNeixi: 3,
                enemyType: MoveType.Qiao,
                currentTargetId: "bandit_a",
                decisiveTargets: new[] { "bandit_a" }));
        presenter.FocusOrHover("decisive_strike");

        var intent = presenter.ConfirmSelected("hero", "fallback_target");

        Assert.NotNull(intent);
        Assert.Equal("hero", intent!.ActorId);
        Assert.Equal("bandit_a", intent.TargetId);
        Assert.Equal("decisive_strike", intent.ActionId);
        Assert.Null(intent.MoveId);
        Assert.False(intent.IsCounter);
        Assert.True(intent.IsDecisiveStrike);
    }

    private static CombatUiMoveSelectionContext Context(
        int playerNeixi,
        MoveType? enemyType,
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

    private static BattlePanelDisplayData DisplayWithMove(string moveId, TypeColorTheme theme)
    {
        return new BattlePanelDisplayData
        {
            Entries = new[]
            {
                new BattlePanelMoveEntry
                {
                    MoveId = moveId,
                    Name = "断岳拳",
                    Source = MoveSource.BaseSlot,
                    ColorTheme = theme,
                    NeixiCost = 1,
                    TriggerConditions = new[] { "always" },
                    SpecialEffects = new[] { "break_guard" }
                }
            }
        };
    }
}
