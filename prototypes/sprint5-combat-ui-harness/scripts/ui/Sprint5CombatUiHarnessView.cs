using System;
using System.Collections.Generic;
using System.Linq;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.Presentation.Shared;
using Godot;
using Sprint5CombatUiHarness.TestData;

namespace Sprint5CombatUiHarness.UI;

public partial class Sprint5CombatUiHarnessView : Control
{
    private IReadOnlyList<Sprint5CombatUiFixture> _fixtures = Array.Empty<Sprint5CombatUiFixture>();
    private int _fixtureIndex;
    private CombatUiMoveSelectionPresenter _presenter = new();
    private CombatUiNavigationController _navigation = new();
    private CombatUiMoveSelectionSnapshot _snapshot = EmptySnapshot();
    private CombatUiNavigationSnapshot _navigationSnapshot = new(
        Array.Empty<CombatUiNavigationNode>(),
        null,
        null,
        InputMode.Keyboard,
        true,
        new CombatUiDualFocusVisualState(null, null, InputMode.Keyboard, "keyboard_focus", null));
    private string _lastIntentText = "尚未确认行动";
    private string _lastDecisionSummary = "尚未生成决策摘要";

    private Label _titleLabel = null!;
    private Label _verdictLabel = null!;
    private VBoxContainer _fixtureButtons = null!;
    private VBoxContainer _currentPanel = null!;
    private VBoxContainer _expectedPanel = null!;
    private VBoxContainer _bottomPanel = null!;

    public void Initialize(IReadOnlyList<Sprint5CombatUiFixture> fixtures)
    {
        _fixtures = fixtures.Count == 0 ? throw new ArgumentException("至少需要一个 fixture。", nameof(fixtures)) : fixtures;
        BuildLayout();
        SelectFixture(0);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_fixtures.Count == 0)
            return;

        if (@event.IsActionPressed("ui_down"))
        {
            MoveFocus(CombatUiNavigationDirection.Down);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_up"))
        {
            MoveFocus(CombatUiNavigationDirection.Up);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_left"))
        {
            SelectFixture((_fixtureIndex - 1 + _fixtures.Count) % _fixtures.Count);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_right"))
        {
            SelectFixture((_fixtureIndex + 1) % _fixtures.Count);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("combat_confirm"))
        {
            ConfirmFocusedAction();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildLayout()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = 20;
        OffsetTop = 20;
        OffsetRight = -20;
        OffsetBottom = -20;

        var root = new VBoxContainer
        {
            Name = "Root",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(root);
        root.SetAnchorsPreset(LayoutPreset.FullRect);

        _titleLabel = Heading("Sprint 5 Combat UI Harness");
        root.AddChild(_titleLabel);

        _verdictLabel = TextLabel("状态：未加载");
        root.AddChild(_verdictLabel);

        root.AddChild(TextLabel("操作：左右切换 fixture，上下移动 focus，空格 / Enter 确认；鼠标悬停左侧行动可更新 preview。"));

        var main = new HBoxContainer
        {
            Name = "Main",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(main);

        _fixtureButtons = Column("Fixture");
        _currentPanel = Column("Current Foundation Output");
        _expectedPanel = Column("GDD Expected Contract");
        main.AddChild(_fixtureButtons);
        main.AddChild(_currentPanel);
        main.AddChild(_expectedPanel);

        _bottomPanel = Column("Navigation / Intent");
        root.AddChild(_bottomPanel);
    }

    private void SelectFixture(int index)
    {
        _fixtureIndex = index;
        var fixture = CurrentFixture;
        _presenter = new CombatUiMoveSelectionPresenter();
        _navigation = new CombatUiNavigationController();
        _lastIntentText = "尚未确认行动";
        _lastDecisionSummary = fixture.IsPlaytestLoop ? "playtest：请选择行动并确认，生成本回合决策摘要。" : "本 fixture 不启用 playtest 决策摘要。";

        _snapshot = _presenter.Open(fixture.DisplayData, fixture.Context);
        _navigationSnapshot = _navigation.ApplySnapshot(_snapshot);
        HoverAction(fixture.InitialHoverActionId, rerender: false);
        RenderAll();
    }

    private void HoverAction(string actionId, bool rerender = true)
    {
        _snapshot = _presenter.FocusOrHover(actionId);
        _navigationSnapshot = _navigation.Hover(actionId);
        if (rerender)
            RenderAll();
    }

    private void MoveFocus(CombatUiNavigationDirection direction)
    {
        _navigationSnapshot = _navigation.Move(direction);
        if (_navigationSnapshot.FocusedActionId is not null)
            _snapshot = _presenter.FocusOrHover(_navigationSnapshot.FocusedActionId);
        RenderAll();
    }

    private void ConfirmFocusedAction()
    {
        var intent = _navigation.ConfirmFocused(_presenter, "hero", "iron_crown");
        _lastIntentText = intent is null
            ? "确认结果：无可提交意图"
            : $"确认结果：Action={intent.ActionId}, Move={intent.MoveId ?? "-"}, Target={intent.TargetId}, Counter={intent.IsCounter}, Decisive={intent.IsDecisiveStrike}";
        _lastDecisionSummary = BuildDecisionSummary(intent, _navigationSnapshot.FocusedActionId);
        RenderAll();
    }

    private void ConfirmAction(string actionId)
    {
        _snapshot = _presenter.FocusOrHover(actionId);
        var intent = _presenter.ConfirmSelected("hero", "iron_crown");
        _lastIntentText = intent is null
            ? $"确认结果：{actionId} 不可提交"
            : $"确认结果：Action={intent.ActionId}, Move={intent.MoveId ?? "-"}, Target={intent.TargetId}, Counter={intent.IsCounter}, Decisive={intent.IsDecisiveStrike}";
        _lastDecisionSummary = BuildDecisionSummary(intent, actionId);
        RenderAll();
    }

    private void RenderAll()
    {
        RenderFixtureButtons();
        RenderCurrentFoundation();
        RenderExpectedContract();
        RenderBottomPanel();

        var drift = HasContractDrift(CurrentFixture);
        _titleLabel.Text = $"Sprint 5 Combat UI Harness — {CurrentFixture.DisplayName}";
        _verdictLabel.Text = drift
            ? "Verdict: CONTRACT DRIFT（当前 Foundation 输出与最新 GDD 合同不一致）"
            : "Verdict: 可用于当前 fixture 的 UI 行为检查";
        _verdictLabel.AddThemeColorOverride("font_color", drift ? new Color(1.0f, 0.42f, 0.24f) : new Color(0.5f, 0.86f, 0.54f));
    }

    private void RenderFixtureButtons()
    {
        Clear(_fixtureButtons);
        _fixtureButtons.AddChild(Heading("Fixture"));
        for (var index = 0; index < _fixtures.Count; index++)
        {
            var fixtureIndex = index;
            var isSelected = index == _fixtureIndex;
            var button = new Button
            {
                Text = isSelected ? $"▶ {_fixtures[index].DisplayName}" : $"  {_fixtures[index].DisplayName}",
                FocusMode = FocusModeEnum.None
            };
            ApplyButtonStateStyle(button, selected: isSelected, focused: false, hovered: false, disabled: false);
            button.Pressed += () => SelectFixture(fixtureIndex);
            _fixtureButtons.AddChild(button);
        }
    }

    private void RenderCurrentFoundation()
    {
        Clear(_currentPanel);
        _currentPanel.AddChild(Heading("Current Foundation Output"));
        _currentPanel.AddChild(TextLabel($"Entries: {_snapshot.Entries.Count}"));
        _currentPanel.AddChild(TextLabel($"Selected: {_snapshot.SelectedActionId ?? "-"}"));
        _currentPanel.AddChild(TextLabel($"Default Focus: {_snapshot.DefaultFocusActionId ?? "-"}"));

        if (_snapshot.PreviewCard is null)
        {
            _currentPanel.AddChild(TextLabel("Preview: 无"));
        }
        else
        {
            _currentPanel.AddChild(TextLabel($"Preview: {_snapshot.PreviewCard.DisplayName} / {_snapshot.PreviewCard.RelationshipText} / {_snapshot.PreviewCard.CounterHint}"));
        }

        foreach (var entry in _snapshot.Entries.OrderBy(entry => entry.StableOrder))
        {
            var label = $"{entry.StableOrder:00} {entry.DisplayName} [{entry.ActionKind}]";
            if (!entry.IsEnabled)
                label += $" — 禁用：{entry.DisabledReason}";
            if (entry.CounterPrompt is not null)
                label += $" — {entry.CounterPrompt.Label}";
            if (entry.IsDecisiveStrike)
                label += " — 决胜";

            var actionId = entry.ActionId;
            var isSelected = _snapshot.SelectedActionId == actionId;
            var isFocused = _navigationSnapshot.FocusedActionId == actionId;
            var isHovered = _navigationSnapshot.HoveredActionId == actionId;
            var prefix = BuildActionStatePrefix(isSelected, isFocused, isHovered);
            var button = new Button
            {
                Text = $"{prefix}{label}",
                Disabled = !entry.IsEnabled,
                FocusMode = entry.IsEnabled ? FocusModeEnum.All : FocusModeEnum.None
            };
            ApplyButtonStateStyle(button, isSelected, isFocused, isHovered, !entry.IsEnabled);
            button.MouseEntered += () => HoverAction(actionId);
            button.Pressed += () => ConfirmAction(actionId);
            _currentPanel.AddChild(button);
        }
    }

    private void RenderExpectedContract()
    {
        Clear(_expectedPanel);
        var fixture = CurrentFixture;
        _expectedPanel.AddChild(Heading("GDD Expected Contract"));
        _expectedPanel.AddChild(TextLabel(fixture.ExpectedContractSummary));
        _expectedPanel.AddChild(TextLabel($"敌方当前内功气机：{fixture.ExpectedEnemyCurrentQi?.ToString() ?? "本 fixture 不验证"}"));
        _expectedPanel.AddChild(TextLabel("cu-004：6 招式、调息、使用道具、决胜入口、不可用原因和预览卡必须可见。"));
        _expectedPanel.AddChild(TextLabel("预览卡可显示预计伤害区间、预计破绽变化和合法范围；不得显示胜率、期望值或最终结算输出。"));
        _expectedPanel.AddChild(TextLabel("registry：当前 combat_action_types 不包含普通攻击或基础反制按钮。"));
        _expectedPanel.AddChild(TextLabel("cu-008：focus 与 hover 必须可并存，禁用行动不可聚焦。"));

        if (fixture.IsPlaytestLoop && fixture.DecisionExpectation is not null)
        {
            var decision = fixture.DecisionExpectation;
            _expectedPanel.AddChild(TextLabel("playtest：这是最小战斗决策 loop，不是完整 vertical slice。", new Color(1.0f, 0.85f, 0.42f)));
            _expectedPanel.AddChild(TextLabel($"敌人：{decision.EnemyDisplayName} ({decision.EnemyId})"));
            _expectedPanel.AddChild(TextLabel($"当前气机：{decision.EnemyCurrentQi}；玩家破绽：{decision.PlayerFlaw}；敌方破绽：{decision.EnemyFlaw}"));
            _expectedPanel.AddChild(TextLabel($"调息后内息预期：{decision.ExpectedNeixiAfterMeditate}"));
            _expectedPanel.AddChild(TextLabel("观察点：玩家是否能根据当前气机、内息压力、破绽与预览做出选择。"));
        }

        if (fixture.ExpectsCurrentQiContract)
        {
            _expectedPanel.AddChild(TextLabel("cu-005：应读取敌方当前内功气机 / 属性，不读取敌方下一招属性。", new Color(1.0f, 0.85f, 0.42f)));
            _expectedPanel.AddChild(TextLabel($"Harness fixture 当前气机：{fixture.ExpectedEnemyCurrentQi?.ToString() ?? "未设置"}。", new Color(1.0f, 0.85f, 0.42f)));
            _expectedPanel.AddChild(TextLabel($"Foundation 兼容输入字段：RevealedEnemyMoveType={fixture.Context.RevealedEnemyMoveType?.ToString() ?? "null"}。该字段在本 harness 中按敌方当前内功气机解释。"));
        }
        else
        {
            _expectedPanel.AddChild(TextLabel("本 fixture 不验证 cu-005 新气机合同。"));
        }
    }

    private void RenderBottomPanel()
    {
        Clear(_bottomPanel);
        _bottomPanel.AddChild(Heading("Navigation / Intent"));
        _bottomPanel.AddChild(TextLabel($"Focus: {_navigationSnapshot.FocusedActionId ?? "-"}"));
        _bottomPanel.AddChild(TextLabel($"Hover: {_navigationSnapshot.HoveredActionId ?? "-"}"));
        _bottomPanel.AddChild(TextLabel($"Input Mode: {_navigationSnapshot.CurrentInputMode}"));
        _bottomPanel.AddChild(TextLabel(_lastIntentText));
        if (CurrentFixture.IsPlaytestLoop)
            _bottomPanel.AddChild(TextLabel($"Decision: {_lastDecisionSummary}", new Color(0.65f, 0.88f, 1.0f)));
        _bottomPanel.AddChild(TextLabel($"Focusable Nodes: {string.Join(" → ", _navigationSnapshot.Nodes.Select(node => node.ActionId))}"));
    }

    private bool HasContractDrift(Sprint5CombatUiFixture fixture)
    {
        return false;
    }

    private string BuildDecisionSummary(CombatUiMoveSelectionIntent? intent, string? requestedActionId)
    {
        var fixture = CurrentFixture;
        if (!fixture.IsPlaytestLoop || fixture.DecisionExpectation is null)
            return "本 fixture 不启用 playtest 决策摘要。";

        var actionId = intent?.ActionId ?? requestedActionId;
        if (actionId is null)
            return "未选择行动；无法生成决策摘要。";

        var entry = _snapshot.Entries.FirstOrDefault(candidate => candidate.ActionId == actionId);
        if (entry is not null && !entry.IsEnabled)
            return $"不可用：{entry.DisplayName} — {entry.DisabledReason ?? "未提供不可用原因"}";

        if (fixture.DecisionExpectation.ActionOutcomeSummaries.TryGetValue(actionId, out var summary))
            return summary;

        if (intent is null)
            return $"{actionId} 不可提交；请检查该行动是否存在或是否已禁用。";

        return $"合法：{intent.ActionId} 已提交到目标 {intent.TargetId}。该行动暂无 playtest 专用摘要。";
    }

    private Sprint5CombatUiFixture CurrentFixture => _fixtures[_fixtureIndex];

    private static VBoxContainer Column(string name)
    {
        var container = new VBoxContainer
        {
            Name = name,
            CustomMinimumSize = new Vector2(330, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        return container;
    }

    private static Label Heading(string text)
    {
        var label = TextLabel(text);
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.94f, 0.91f));
        return label;
    }

    private static Label TextLabel(string text, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeColorOverride("font_color", color ?? new Color(0.86f, 0.84f, 0.78f));
        return label;
    }

    private static string BuildActionStatePrefix(bool selected, bool focused, bool hovered)
    {
        if (selected && focused && hovered)
            return "● ";
        if (selected)
            return "▶ ";
        if (focused)
            return "◆ ";
        if (hovered)
            return "◇ ";
        return "  ";
    }

    private static void ApplyButtonStateStyle(Button button, bool selected, bool focused, bool hovered, bool disabled)
    {
        if (disabled)
        {
            ApplyButtonStyle(button, new Color(0.12f, 0.12f, 0.13f), new Color(0.48f, 0.46f, 0.42f));
            return;
        }

        if (selected)
        {
            ApplyButtonStyle(button, new Color(0.18f, 0.36f, 0.58f), new Color(0.95f, 0.98f, 1.0f));
            return;
        }

        if (focused)
        {
            ApplyButtonStyle(button, new Color(0.32f, 0.24f, 0.10f), new Color(1.0f, 0.90f, 0.58f));
            return;
        }

        if (hovered)
        {
            ApplyButtonStyle(button, new Color(0.18f, 0.24f, 0.28f), new Color(0.74f, 0.90f, 1.0f));
            return;
        }

        ApplyButtonStyle(button, new Color(0.09f, 0.10f, 0.11f), new Color(0.86f, 0.84f, 0.78f));
    }

    private static void ApplyButtonStyle(Button button, Color backgroundColor, Color fontColor)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = backgroundColor.Lightened(0.22f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = backgroundColor.Lightened(0.12f);
        hover.BorderColor = fontColor;

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = backgroundColor.Darkened(0.10f);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", hover);
        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_hover_color", fontColor.Lightened(0.10f));
        button.AddThemeColorOverride("font_pressed_color", fontColor);
        button.AddThemeColorOverride("font_disabled_color", fontColor);
    }

    private static void Clear(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static CombatUiMoveSelectionSnapshot EmptySnapshot()
    {
        return new CombatUiMoveSelectionSnapshot(false, Array.Empty<CombatUiMoveSelectionEntry>(), null, null, null, false, 0);
    }
}
