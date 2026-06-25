using System;
using System.Collections.Generic;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.Fixtures;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;
using FengZhi.Foundation.Presentation.Shared;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// 把 Foundation 的 <see cref="CombatMoveSelectionPanel"/> + <see cref="CombatUiMoveSelectionPresenter"/>
/// 桥接到 VS battle scene 与 <see cref="BattleEventBus"/>。
///
/// 职责：
/// - 持有 presenter + panel 实例，并在玩家决策阶段 Open / 在解算阶段 Close。
/// - 监听战斗事件刷新 panel context（NeixiChangedEvent / IntentRevealedEvent）。
/// - 处理 D-pad 上下导航 + ui_accept 确认输入。
/// - 把 <see cref="CombatUiMoveSelectionIntent"/> 翻译成可结算的 <see cref="BattleAction"/>，
///   再回调宿主提交（owner Q2=a 决策：demo_* 招式统一路由回 luo_han_quan/tie_bi_heng_lan）。
///
/// 后续 cu-005/006/008 的反制 / 决胜 / dual-focus 视觉细节都在本文件扩展。
/// </summary>
public sealed class CombatMoveSelectionBinder : IDisposable
{
    private readonly CombatUiMoveSelectionPresenter _presenter = new();
    private readonly CombatMoveSelectionPanel _panel;
    private readonly BattleEventBus _bus;
    private readonly Func<BattleAction, bool> _submitAction;
    private readonly string _actorId;
    private readonly string _defaultTargetId;
    private readonly Action<NeixiChangedEvent> _onNeixiChanged;
    private readonly Action<IntentRevealedEvent> _onIntentRevealed;
    private readonly Action<DecisiveStrikeAvailableEvent> _onDecisiveStrikeAvailable;
    private readonly Action<RoundStartEvent> _onRoundStart;
    private readonly List<string> _decisiveStrikeTargetIds = new();

    private BattlePanelDisplayData _displayData = new();
    private MoveType? _revealedEnemyMoveType;
    private int _playerNeixi;
    private bool _isXinfaSealed;
    private int _usableCombatItemCount;
    private bool _isOpen;
    private bool _disposed;

    public CombatMoveSelectionBinder(
        BattleEventBus bus,
        Node uiHost,
        string actorId,
        string defaultTargetId,
        Func<BattleAction, bool> submitAction,
        IFocusManager? focusManager = null)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        ArgumentNullException.ThrowIfNull(uiHost);
        _submitAction = submitAction ?? throw new ArgumentNullException(nameof(submitAction));
        _actorId = string.IsNullOrWhiteSpace(actorId)
            ? throw new ArgumentException("actorId required", nameof(actorId))
            : actorId;
        _defaultTargetId = string.IsNullOrWhiteSpace(defaultTargetId)
            ? throw new ArgumentException("defaultTargetId required", nameof(defaultTargetId))
            : defaultTargetId;

        _panel = new CombatMoveSelectionPanel(focusManager)
        {
            Name = "CombatMoveSelectionPanel",
            Visible = false,
        };
        uiHost.AddChild(_panel);

        _onNeixiChanged = HandleNeixiChanged;
        _onIntentRevealed = HandleIntentRevealed;
        _onDecisiveStrikeAvailable = HandleDecisiveStrikeAvailable;
        _onRoundStart = HandleRoundStart;
        _bus.Subscribe(_onNeixiChanged);
        _bus.Subscribe(_onIntentRevealed);
        _bus.Subscribe(_onDecisiveStrikeAvailable);
        _bus.Subscribe(_onRoundStart);
    }

    /// <summary>
    /// 由宿主在战斗外（测试 / demo seed 预置）直接注入决胜目标 ID。
    /// 也供 fixture demo seed 强制设置敌方破绽 ≥5 状态。
    /// </summary>
    public void SetDecisiveStrikeTargets(IEnumerable<string> targetIds)
    {
        _decisiveStrikeTargetIds.Clear();
        foreach (var id in targetIds)
        {
            if (!string.IsNullOrWhiteSpace(id) && !_decisiveStrikeTargetIds.Contains(id))
                _decisiveStrikeTargetIds.Add(id);
        }
        if (_isOpen)
        {
            var snapshot = _presenter.RefreshResources(BuildContext());
            _panel.ApplySnapshot(snapshot);
        }
    }

    /// <summary>面板节点（仅做断言/调试可见用）。</summary>
    public CombatMoveSelectionPanel Panel => _panel;

    /// <summary>当前面板是否已打开（玩家决策阶段）。</summary>
    public bool IsOpen => _isOpen;

    /// <summary>
    /// 设置 demo / 真实战斗下的面板基础数据。通常在战斗开始时调用一次。
    /// </summary>
    public void SetDisplayData(BattlePanelDisplayData displayData, bool isXinfaSealed, int usableCombatItemCount)
    {
        _displayData = displayData ?? throw new ArgumentNullException(nameof(displayData));
        _isXinfaSealed = isXinfaSealed;
        _usableCombatItemCount = Math.Max(0, usableCombatItemCount);
    }

    /// <summary>
    /// 玩家决策阶段开始时打开面板。重复调用幂等。
    /// </summary>
    public void OpenForPlayerDecision(int playerNeixi)
    {
        _playerNeixi = Math.Max(0, playerNeixi);
        var snapshot = _presenter.OpenForState(CombatUiState.PlayerDecision, _displayData, BuildContext());
        _panel.ApplySnapshot(snapshot);
        _isOpen = snapshot.IsOpen;
    }

    /// <summary>
    /// 玩家提交后或战斗结束时关闭面板。
    /// </summary>
    public void Close()
    {
        if (!_isOpen)
            return;

        var snapshot = _presenter.Close();
        _panel.ApplySnapshot(snapshot);
        _isOpen = false;
    }

    /// <summary>
    /// 在面板打开期间外部主动刷资源（例如初始化时 Neixi 还没有事件推送）。
    /// </summary>
    public void RefreshResources(int playerNeixi)
    {
        _playerNeixi = Math.Max(0, playerNeixi);
        if (!_isOpen)
            return;

        var snapshot = _presenter.RefreshResources(BuildContext());
        _panel.ApplySnapshot(snapshot);
    }

    /// <summary>
    /// Demo 模式专用：直接覆盖 panel 显示的 Neixi 值，并立即刷新（不动战斗状态）。
    /// 用于 cu-005 demo 演示反制 enable/disable 切换的 hotkey 路径。
    /// </summary>
    public void OverridePlayerNeixiForDemo(int playerNeixi)
    {
        _playerNeixi = Math.Max(0, playerNeixi);
        if (!_isOpen)
            return;

        var snapshot = _presenter.RefreshResources(BuildContext());
        _panel.ApplySnapshot(snapshot);
    }

    /// <summary>当前 binder 持有的 UI 内息显示值（demo 调试用）。</summary>
    public int CurrentDisplayNeixi => _playerNeixi;

    /// <summary>
    /// cu-008 dual-focus: 切换当前输入模式（鼠标 / 键盘 / 手柄），不丢 focus/hover 状态。
    /// 宿主在 _UnhandledInput 检测 InputEvent 类型变化时调用。
    /// </summary>
    public void SetInputMode(InputMode inputMode)
    {
        if (!_isOpen)
            return;
        _panel.ChangeInputMode(inputMode);
    }

    /// <summary>
    /// D-pad / 方向键上。
    /// </summary>
    public void NavigateUp()
    {
        if (!_isOpen)
            return;
        _panel.NavigateUp();
    }

    /// <summary>
    /// D-pad / 方向键下。
    /// </summary>
    public void NavigateDown()
    {
        if (!_isOpen)
            return;
        _panel.NavigateDown();
    }

    /// <summary>
    /// 鼠标 hover（cu-008 dual-focus 用，cu-004 阶段先 wire，不强求 UI 表现）。
    /// </summary>
    public void HoverAction(string? actionId)
    {
        if (!_isOpen)
            return;
        _panel.HoverAction(actionId);
    }

    /// <summary>
    /// 按 ui_accept 确认当前聚焦行动 → 翻译成 <see cref="BattleAction"/> → 调 submit 回调。
    /// </summary>
    /// <returns>true 表示已提交战斗系统；false 表示无可提交（面板未开 / 行动不可用 / 翻译失败）。</returns>
    public bool ConfirmFocused()
    {
        if (!_isOpen)
            return false;

        var actionId = _panel.ConfirmFocusedAction();
        if (actionId is null)
            return false;

        _panel.HoverAction(actionId);
        var snapshotAfterFocus = _presenter.FocusOrHover(actionId);
        _panel.ApplySnapshot(snapshotAfterFocus);

        var intent = _presenter.ConfirmSelected(_actorId, _defaultTargetId);
        if (intent is null)
            return false;

        var battleAction = TranslateIntent(intent);
        if (battleAction is null)
            return false;

        Close();
        _submitAction(battleAction);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _bus.Unsubscribe(_onNeixiChanged);
        _bus.Unsubscribe(_onIntentRevealed);
        _bus.Unsubscribe(_onDecisiveStrikeAvailable);
        _bus.Unsubscribe(_onRoundStart);
        _disposed = true;
    }

    private CombatUiMoveSelectionContext BuildContext() => new(
        PlayerNeixi: _playerNeixi,
        IsXinfaSealed: _isXinfaSealed,
        UsableCombatItemCount: _usableCombatItemCount,
        RevealedEnemyMoveType: _revealedEnemyMoveType,
        CurrentTargetId: _defaultTargetId,
        DecisiveStrikeTargetIds: _decisiveStrikeTargetIds.Count == 0 ? null : _decisiveStrikeTargetIds.ToArray());

    private void HandleNeixiChanged(NeixiChangedEvent evt)
    {
        if (!string.Equals(evt.ActorId, _actorId, StringComparison.Ordinal))
            return;
        RefreshResources(evt.NewValue);
    }

    private void HandleIntentRevealed(IntentRevealedEvent evt)
    {
        // VS Sprint 7 MVP-A 只有 1 个敌人，直接读第一个有 MoveType 的揭示
        MoveType? revealed = null;
        foreach (var enemy in evt.Enemies)
        {
            if (enemy.MoveType is not null)
            {
                revealed = enemy.MoveType;
                break;
            }
        }

        _revealedEnemyMoveType = revealed;
        if (!_isOpen)
            return;

        var snapshot = _presenter.RefreshResources(BuildContext());
        _panel.ApplySnapshot(snapshot);
    }

    private void HandleDecisiveStrikeAvailable(DecisiveStrikeAvailableEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.TargetId))
            return;
        if (_decisiveStrikeTargetIds.Contains(evt.TargetId))
            return;

        _decisiveStrikeTargetIds.Add(evt.TargetId);
        if (!_isOpen)
            return;

        var snapshot = _presenter.RefreshResources(BuildContext());
        _panel.ApplySnapshot(snapshot);
    }

    private void HandleRoundStart(RoundStartEvent evt)
    {
        // 每个回合开始重置决胜目标 list（adapter 也会在 RoundStart 清空内部 _decisiveStrikeTargets）
        // demo seed 预置的决胜目标在 RoundStart 之前注入，会被这里清掉 — 所以宿主需要在
        // RoundStart 之后通过 SetDecisiveStrikeTargets 重新注入，或依靠 DecisiveStrikeAvailableEvent。
        _decisiveStrikeTargetIds.Clear();
    }

    /// <summary>
    /// 把 UI 选择意图翻译成 <see cref="BattleAction"/>。
    ///
    /// 翻译规则（owner Q2=a 2026-06-24）：
    /// - rest_meditate → ActionType.Breathe
    /// - 决胜一击行 (IsDecisiveStrike) → ActionType.Decisive
    /// - 反制 (IsCounter) → ActionType.Counter + 真招 ID（cost = CounterNeixiThreshold = 3）
    /// - move:luo_han_quan / move:tie_bi_heng_lan → 真招直接结算
    /// - move:demo_*（cu-004 演示用招）→ 按 NeixiCost 路由回真招
    ///     - cost ≤ 3 → luo_han_quan (轻击)
    ///     - cost > 3 → tie_bi_heng_lan (重击)
    /// - 心法专属 / 高消耗 / 无道具 在 presenter 层就被置灰，不会走到这里
    /// </summary>
    private BattleAction? TranslateIntent(CombatUiMoveSelectionIntent intent)
    {
        if (intent.ActionId == "rest_meditate")
        {
            return new BattleAction
            {
                ActorId = intent.ActorId,
                Type = ActionType.Breathe,
            };
        }

        if (intent.IsDecisiveStrike)
        {
            return new BattleAction
            {
                ActorId = intent.ActorId,
                Type = ActionType.Decisive,
                TargetId = intent.TargetId,
            };
        }

        if (intent.MoveId is null)
            return null; // use_item 在 demo 里恒置灰，不会到这；其它未知 actionId 也走 null

        var moveEntry = FindMoveEntry(intent.MoveId);
        if (moveEntry is null)
            return null;

        var (resolvedMoveId, resolvedCost) = ResolveResolutionMove(intent.MoveId, moveEntry.NeixiCost);

        if (intent.IsCounter)
        {
            return new BattleAction
            {
                ActorId = intent.ActorId,
                Type = ActionType.Counter,
                TargetId = intent.TargetId,
                MoveId = resolvedMoveId,
                MoveType = MoveType.Gang,
                // 反制内息阈值在 presenter 定义为 CounterNeixiThreshold=3
                NeixiCost = CombatUiMoveSelectionPresenter.CounterNeixiThreshold,
            };
        }

        return new BattleAction
        {
            ActorId = intent.ActorId,
            Type = ActionType.Move,
            TargetId = intent.TargetId,
            MoveId = resolvedMoveId,
            MoveType = MoveType.Gang,
            NeixiCost = resolvedCost,
        };
    }

    private BattlePanelMoveEntry? FindMoveEntry(string moveId)
    {
        foreach (var entry in _displayData.Entries)
        {
            if (string.Equals(entry.MoveId, moveId, StringComparison.Ordinal))
                return entry;
        }
        return null;
    }

    private static (string MoveId, int NeixiCost) ResolveResolutionMove(string demoMoveId, int demoCost)
    {
        if (demoMoveId == JiangnanBandit1v1Fixture.LightStrikeMoveId)
            return (JiangnanBandit1v1Fixture.LightStrikeMoveId, 2);
        if (demoMoveId == JiangnanBandit1v1Fixture.HeavyStrikeMoveId)
            return (JiangnanBandit1v1Fixture.HeavyStrikeMoveId, 4);

        // demo_* 招式按 cost 路由到真招
        return demoCost <= 3
            ? (JiangnanBandit1v1Fixture.LightStrikeMoveId, 2)
            : (JiangnanBandit1v1Fixture.HeavyStrikeMoveId, 4);
    }
}
