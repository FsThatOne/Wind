using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.MartialArts;
using FengZhi.Foundation.Presentation.Shared;
using Godot;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 招式选择面板中的行动类型。UI 仅展示，不代表战斗结算动作。
/// </summary>
public enum CombatUiMoveActionKind
{
    EquippedMove,
    RestMeditate,
    UseItem,
    DecisiveStrike
}

/// <summary>
/// 行动可用性原因。禁用原因会映射成玩家可读文本。
/// </summary>
public enum CombatUiMoveAvailabilityKind
{
    Available,
    InsufficientNeixi,
    XinfaSealed,
    NoUsableCombatItem
}

/// <summary>
/// 预览卡显示的体系关系。只表达关系，不携带伤害、胜率或结算预测。
/// </summary>
public enum CombatUiMoveTypeRelationship
{
    Unknown,
    Advantage,
    Neutral,
    Disadvantage
}

/// <summary>
/// 招式选择面板构建时需要的战斗只读状态。
/// </summary>
public sealed record CombatUiMoveSelectionContext(
    int PlayerNeixi,
    bool IsXinfaSealed,
    int UsableCombatItemCount,
    MoveType? RevealedEnemyMoveType,
    string? CurrentTargetId = null,
    IReadOnlyList<string>? DecisiveStrikeTargetIds = null);

/// <summary>
/// 反制提示展示契约。只表达是否可触发，不执行扣费或结算。
/// </summary>
public sealed record CombatUiCounterPrompt(
    bool IsVisible,
    bool IsEnabled,
    string Label,
    string ColorKey,
    string? DisabledReason);

/// <summary>
/// 单个行动槽位展示 DTO。字段全部为展示契约，不包含任何结算预测。
/// </summary>
public sealed record CombatUiMoveSelectionEntry(
    string ActionId,
    string? MoveId,
    CombatUiMoveActionKind ActionKind,
    string DisplayName,
    CombatUiIntentIconKind TypeIconKind,
    string TypeGlyph,
    int NeixiCost,
    string TriggerConditionIcon,
    string EffectSummary,
    bool IsXinfaExclusive,
    bool IsEnabled,
    string? DisabledReason,
    CombatUiCounterPrompt? CounterPrompt,
    string? TargetId,
    bool IsDecisiveStrike,
    int StableOrder);

/// <summary>
/// UI 确认行动后交给战斗系统的意图。UI 不在此执行扣费、伤害或破绽结算。
/// </summary>
public sealed record CombatUiMoveSelectionIntent(
    string ActorId,
    string TargetId,
    string ActionId,
    string? MoveId,
    bool IsCounter,
    bool IsDecisiveStrike);

/// <summary>
/// 聚焦或悬停行动后显示的预览卡。禁止加入伤害预测、胜率、期望值或结算输出字段。
/// </summary>
public sealed record CombatUiMovePreviewCard(
    string ActionId,
    string DisplayName,
    CombatUiMoveTypeRelationship TypeRelationship,
    string RelationshipText,
    string CounterHint);

/// <summary>
/// 招式选择面板只读快照。Godot Control 层只绑定该快照，不回写战斗状态。
/// </summary>
public sealed record CombatUiMoveSelectionSnapshot(
    bool IsOpen,
    IReadOnlyList<CombatUiMoveSelectionEntry> Entries,
    CombatUiMovePreviewCard? PreviewCard,
    string? DefaultFocusActionId,
    string? SelectedActionId,
    bool IsDirty,
    int RefreshCount);

/// <summary>
/// 招式面板手柄/键盘导航方向。
/// </summary>
public enum CombatUiNavigationDirection
{
    Up,
    Down
}

/// <summary>
/// 单个可聚焦行动的循环导航邻居契约。
/// </summary>
public sealed record CombatUiNavigationNode(
    string ActionId,
    string UpNeighborActionId,
    string DownNeighborActionId,
    bool CanReceiveFocus);

/// <summary>
/// 双焦点视觉状态。手柄/键盘焦点和鼠标 hover 分离记录。
/// </summary>
public sealed record CombatUiDualFocusVisualState(
    string? FocusedActionId,
    string? HoveredActionId,
    InputMode CurrentInputMode,
    string FocusStyleKey,
    string? HoverStyleKey);

/// <summary>
/// 招式面板导航快照。仅表达 UI 焦点状态，不持有战斗结算状态。
/// </summary>
public sealed record CombatUiNavigationSnapshot(
    IReadOnlyList<CombatUiNavigationNode> Nodes,
    string? FocusedActionId,
    string? HoveredActionId,
    InputMode CurrentInputMode,
    bool IsFocusContained,
    CombatUiDualFocusVisualState VisualState);

/// <summary>
/// Godot Control 层需要应用的循环焦点邻居绑定。
/// </summary>
public sealed record CombatUiFocusNeighborBinding(
    string ActionId,
    string UpNeighborActionId,
    string DownNeighborActionId);

/// <summary>
/// 纯 C# 招式面板导航控制器。用于 D-pad 循环、确认提交和 dual-focus 状态管理。
/// </summary>
public sealed class CombatUiNavigationController
{
    private readonly List<CombatUiNavigationNode> _nodes = new();
    private string? _focusedActionId;
    private string? _hoveredActionId;
    private InputMode _currentInputMode = InputMode.Keyboard;

    public CombatUiNavigationSnapshot Snapshot => BuildSnapshot();

    /// <summary>
    /// 根据面板快照重建导航图，并保留仍然有效的焦点/hover 状态。
    /// </summary>
    public CombatUiNavigationSnapshot ApplySnapshot(CombatUiMoveSelectionSnapshot snapshot)
    {
        _nodes.Clear();
        var focusableEntries = snapshot.Entries
            .Where(entry => entry.IsEnabled)
            .OrderBy(entry => entry.StableOrder)
            .ToArray();

        for (var index = 0; index < focusableEntries.Length; index++)
        {
            var current = focusableEntries[index];
            var up = focusableEntries[(index - 1 + focusableEntries.Length) % focusableEntries.Length];
            var down = focusableEntries[(index + 1) % focusableEntries.Length];
            _nodes.Add(new CombatUiNavigationNode(current.ActionId, up.ActionId, down.ActionId, true));
        }

        if (!ContainsAction(_focusedActionId))
        {
            _focusedActionId = ContainsAction(snapshot.DefaultFocusActionId)
                ? snapshot.DefaultFocusActionId
                : _nodes.FirstOrDefault()?.ActionId;
        }

        if (!ContainsAction(_hoveredActionId))
            _hoveredActionId = null;

        return BuildSnapshot();
    }

    /// <summary>
    /// 按方向移动焦点。导航图保证在面板内部循环。
    /// </summary>
    public CombatUiNavigationSnapshot Move(CombatUiNavigationDirection direction)
    {
        if (_nodes.Count == 0)
            return BuildSnapshot();

        if (!ContainsAction(_focusedActionId))
            _focusedActionId = _nodes[0].ActionId;

        var node = _nodes.First(current => current.ActionId == _focusedActionId);
        _focusedActionId = direction == CombatUiNavigationDirection.Up
            ? node.UpNeighborActionId
            : node.DownNeighborActionId;
        _currentInputMode = InputMode.Gamepad;
        return BuildSnapshot();
    }

    /// <summary>
    /// 记录鼠标 hover 目标。不会覆盖键盘/手柄焦点。
    /// </summary>
    public CombatUiNavigationSnapshot Hover(string? actionId)
    {
        _hoveredActionId = ContainsAction(actionId) ? actionId : null;
        _currentInputMode = InputMode.Mouse;
        return BuildSnapshot();
    }

    /// <summary>
    /// 切换当前输入模式。不会丢失已选焦点或 hover 状态。
    /// </summary>
    public CombatUiNavigationSnapshot ChangeInputMode(InputMode inputMode)
    {
        _currentInputMode = inputMode;
        return BuildSnapshot();
    }

    /// <summary>
    /// 确认当前聚焦行动。调用方负责把行动 ID 提交给对应 Presenter。
    /// </summary>
    public string? ConfirmFocusedAction()
    {
        return ContainsAction(_focusedActionId) ? _focusedActionId : null;
    }

    /// <summary>
    /// 以当前聚焦行动生成 MoveSelection 意图。
    /// </summary>
    public CombatUiMoveSelectionIntent? ConfirmFocused(
        CombatUiMoveSelectionPresenter presenter,
        string actorId,
        string fallbackTargetId)
    {
        var actionId = ConfirmFocusedAction();
        if (actionId is null)
            return null;

        presenter.FocusOrHover(actionId);
        return presenter.ConfirmSelected(actorId, fallbackTargetId);
    }

    private CombatUiNavigationSnapshot BuildSnapshot()
    {
        return new CombatUiNavigationSnapshot(
            _nodes.ToArray(),
            _focusedActionId,
            _hoveredActionId,
            _currentInputMode,
            IsFocusContained(),
            BuildVisualState());
    }

    private CombatUiDualFocusVisualState BuildVisualState()
    {
        return new CombatUiDualFocusVisualState(
            _focusedActionId,
            _hoveredActionId,
            _currentInputMode,
            _currentInputMode switch
            {
                InputMode.Gamepad => "gamepad_focus",
                InputMode.Mouse => "mouse_focus",
                _ => "keyboard_focus"
            },
            _hoveredActionId is null ? null : "mouse_hover");
    }

    private bool IsFocusContained()
    {
        return _focusedActionId is null || ContainsAction(_focusedActionId);
    }

    private bool ContainsAction(string? actionId)
    {
        return actionId is not null && _nodes.Any(node => node.ActionId == actionId);
    }
}

/// <summary>
/// 将 MartialArts 战斗面板数据和战斗资源快照组装为 Combat UI 招式选择快照。
/// </summary>
public sealed class CombatUiMoveSelectionPresenter
{
    public const int MaxEquippedMoveSlots = 6;
    public const int BaseActionCount = 2;
    public const int CounterNeixiThreshold = 3;

    private BattlePanelDisplayData _displayData = new();
    private CombatUiMoveSelectionContext _context = new(0, false, 0, null);
    private string? _selectedActionId;
    private bool _isOpen;
    private bool _isDirty;
    private int _refreshCount;

    /// <summary>
    /// 打开玩家行动面板，并默认聚焦第一个可用行动。
    /// </summary>
    public CombatUiMoveSelectionSnapshot Open(
        BattlePanelDisplayData displayData,
        CombatUiMoveSelectionContext context)
    {
        _displayData = displayData ?? throw new ArgumentNullException(nameof(displayData));
        _context = context;
        _isOpen = true;
        _isDirty = true;
        _selectedActionId = null;
        var entries = BuildEntries();
        _selectedActionId = entries.FirstOrDefault(entry => entry.IsEnabled)?.ActionId;
        return BuildSnapshot(entries);
    }

    /// <summary>
    /// 仅在玩家决策阶段打开面板，避免非交互战斗阶段误弹出操作 UI。
    /// </summary>
    public CombatUiMoveSelectionSnapshot OpenForState(
        CombatUiState state,
        BattlePanelDisplayData displayData,
        CombatUiMoveSelectionContext context)
    {
        return state == CombatUiState.PlayerDecision
            ? Open(displayData, context)
            : Close();
    }

    /// <summary>
    /// 在面板打开期间刷新资源状态。不会关闭面板，也不会重置当前交互焦点。
    /// </summary>
    public CombatUiMoveSelectionSnapshot RefreshResources(CombatUiMoveSelectionContext context)
    {
        _context = context;
        _isDirty = true;
        return BuildSnapshot(BuildEntries());
    }

    /// <summary>
    /// 聚焦或悬停某个行动后更新预览卡。
    /// </summary>
    public CombatUiMoveSelectionSnapshot FocusOrHover(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return BuildSnapshot(BuildEntries());

        _selectedActionId = actionId;
        _isDirty = true;
        return BuildSnapshot(BuildEntries());
    }

    /// <summary>
    /// 确认当前选中行动并生成提交给战斗系统的意图。
    /// </summary>
    public CombatUiMoveSelectionIntent? ConfirmSelected(string actorId, string fallbackTargetId)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(fallbackTargetId))
            return null;

        var entries = BuildEntries();
        var selectedEntry = entries.FirstOrDefault(entry => entry.ActionId == _selectedActionId);
        if (selectedEntry is null || !selectedEntry.IsEnabled)
            return null;

        var targetId = selectedEntry.TargetId ?? _context.CurrentTargetId ?? fallbackTargetId;
        if (string.IsNullOrWhiteSpace(targetId))
            return null;

        return new CombatUiMoveSelectionIntent(
            actorId,
            targetId,
            selectedEntry.ActionId,
            selectedEntry.MoveId,
            selectedEntry.CounterPrompt?.IsEnabled == true,
            selectedEntry.IsDecisiveStrike);
    }

    /// <summary>
    /// 关闭面板。焦点恢复由 Godot Control 适配层执行。
    /// </summary>
    public CombatUiMoveSelectionSnapshot Close()
    {
        _isOpen = false;
        _isDirty = true;
        return BuildSnapshot(Array.Empty<CombatUiMoveSelectionEntry>());
    }

    /// <summary>
    /// 合批刷新入口。与 HUD 适配器一致，只在 dirty 时计数。
    /// </summary>
    public bool RefreshIfDirty()
    {
        if (!_isDirty)
            return false;

        _refreshCount++;
        _isDirty = false;
        return true;
    }

    private CombatUiMoveSelectionSnapshot BuildSnapshot(IReadOnlyList<CombatUiMoveSelectionEntry> entries)
    {
        var selectedEntry = entries.FirstOrDefault(entry => entry.ActionId == _selectedActionId);
        var defaultFocus = entries.FirstOrDefault(entry => entry.IsEnabled)?.ActionId;
        return new CombatUiMoveSelectionSnapshot(
            _isOpen,
            entries,
            selectedEntry is null ? null : BuildPreviewCard(selectedEntry),
            defaultFocus,
            _selectedActionId,
            _isDirty,
            _refreshCount);
    }

    private IReadOnlyList<CombatUiMoveSelectionEntry> BuildEntries()
    {
        if (!_isOpen)
            return Array.Empty<CombatUiMoveSelectionEntry>();

        var moveCount = Math.Min(_displayData.Entries.Count, MaxEquippedMoveSlots);
        var decisiveTargetId = ResolveCurrentDecisiveTargetId();
        var result = new List<CombatUiMoveSelectionEntry>(moveCount + BaseActionCount + (decisiveTargetId is null ? 0 : 1));
        var order = 0;
        if (decisiveTargetId is not null)
        {
            result.Add(BuildDecisiveStrikeAction(decisiveTargetId, order));
            order++;
        }

        foreach (var move in _displayData.Entries.Take(MaxEquippedMoveSlots))
        {
            result.Add(BuildMoveEntry(move, order));
            order++;
        }

        result.Add(BuildBaseAction("rest_meditate", CombatUiMoveActionKind.RestMeditate, "调息", order++));
        result.Add(BuildUseItemAction(order));
        return result;
    }

    private CombatUiMoveSelectionEntry BuildMoveEntry(BattlePanelMoveEntry move, int stableOrder)
    {
        var disabledReason = ResolveDisabledReason(move);
        var iconKind = ToIconKind(move.ColorTheme);
        return new CombatUiMoveSelectionEntry(
            $"move:{move.MoveId}",
            move.MoveId,
            CombatUiMoveActionKind.EquippedMove,
            move.Name,
            iconKind,
            ToGlyph(iconKind),
            move.NeixiCost,
            FirstOrDefaultText(move.TriggerConditions, "always"),
            FirstOrDefaultText(move.SpecialEffects, "无特殊效果"),
            move.Source == MoveSource.XinfaExclusive,
            disabledReason is null,
            disabledReason,
            ResolveCounterPrompt(iconKind),
            _context.CurrentTargetId,
            false,
            stableOrder);
    }

    private CombatUiMoveSelectionEntry BuildBaseAction(
        string actionId,
        CombatUiMoveActionKind actionKind,
        string displayName,
        int stableOrder)
    {
        return new CombatUiMoveSelectionEntry(
            actionId,
            null,
            actionKind,
            displayName,
            CombatUiIntentIconKind.Unknown,
            "-",
            0,
            "always",
            actionKind == CombatUiMoveActionKind.RestMeditate ? "恢复内息" : "基础攻击",
            false,
            true,
            null,
            null,
            _context.CurrentTargetId,
            false,
            stableOrder);
    }

    private CombatUiMoveSelectionEntry BuildUseItemAction(int stableOrder)
    {
        var disabledReason = _context.UsableCombatItemCount <= 0 ? "无可用战斗道具" : null;
        return new CombatUiMoveSelectionEntry(
            "use_item",
            null,
            CombatUiMoveActionKind.UseItem,
            "使用道具",
            CombatUiIntentIconKind.Unknown,
            "物",
            0,
            "has_combat_item",
            "使用战斗道具",
            false,
            disabledReason is null,
            disabledReason,
            null,
            _context.CurrentTargetId,
            false,
            stableOrder);
    }

    private CombatUiMoveSelectionEntry BuildDecisiveStrikeAction(string targetId, int stableOrder)
    {
        return new CombatUiMoveSelectionEntry(
            "decisive_strike",
            null,
            CombatUiMoveActionKind.DecisiveStrike,
            "▶ 决胜一击",
            CombatUiIntentIconKind.Unknown,
            "决",
            0,
            "target_stagger_exposed",
            "抓住破绽，一击定胜负",
            false,
            true,
            null,
            null,
            targetId,
            true,
            stableOrder);
    }

    private string? ResolveDisabledReason(BattlePanelMoveEntry move)
    {
        if (move.Source == MoveSource.XinfaExclusive && _context.IsXinfaSealed)
            return "心法封印中";

        var missingNeixi = move.NeixiCost - _context.PlayerNeixi;
        return missingNeixi > 0 ? $"差 {missingNeixi} 内息" : null;
    }

    private CombatUiMovePreviewCard BuildPreviewCard(CombatUiMoveSelectionEntry entry)
    {
        var relationship = ResolveRelationship(entry.TypeIconKind, _context.RevealedEnemyMoveType);
        return new CombatUiMovePreviewCard(
            entry.ActionId,
            entry.DisplayName,
            relationship,
            RelationshipText(relationship),
            CounterHint(relationship, _context.PlayerNeixi));
    }

    private CombatUiCounterPrompt? ResolveCounterPrompt(CombatUiIntentIconKind attackerKind)
    {
        var relationship = ResolveRelationship(attackerKind, _context.RevealedEnemyMoveType);
        if (relationship != CombatUiMoveTypeRelationship.Advantage)
            return null;

        if (_context.PlayerNeixi >= CounterNeixiThreshold)
        {
            return new CombatUiCounterPrompt(
                true,
                true,
                "反制",
                "counter_gold",
                null);
        }

        return new CombatUiCounterPrompt(
            true,
            false,
            "反制 / 内息不足",
            "counter_disabled_gray",
            "内息不足");
    }

    private string? ResolveCurrentDecisiveTargetId()
    {
        var targets = _context.DecisiveStrikeTargetIds ?? Array.Empty<string>();
        if (targets.Count == 0)
            return null;

        if (_context.CurrentTargetId is not null
            && targets.Contains(_context.CurrentTargetId, StringComparer.Ordinal))
            return _context.CurrentTargetId;

        return targets.FirstOrDefault(targetId => !string.IsNullOrWhiteSpace(targetId));
    }

    private static CombatUiMoveTypeRelationship ResolveRelationship(
        CombatUiIntentIconKind attackerKind,
        MoveType? defenderType)
    {
        if (defenderType is null || attackerKind == CombatUiIntentIconKind.Unknown)
            return CombatUiMoveTypeRelationship.Unknown;

        var attackerType = ToMoveType(attackerKind);
        if (attackerType == defenderType.Value)
            return CombatUiMoveTypeRelationship.Neutral;

        var isAdvantage = (attackerType == MoveType.Gang && defenderType.Value == MoveType.Qiao)
            || (attackerType == MoveType.Qiao && defenderType.Value == MoveType.Rou)
            || (attackerType == MoveType.Rou && defenderType.Value == MoveType.Gang);

        return isAdvantage
            ? CombatUiMoveTypeRelationship.Advantage
            : CombatUiMoveTypeRelationship.Disadvantage;
    }

    private static string RelationshipText(CombatUiMoveTypeRelationship relationship) => relationship switch
    {
        CombatUiMoveTypeRelationship.Advantage => "克制",
        CombatUiMoveTypeRelationship.Disadvantage => "被克",
        CombatUiMoveTypeRelationship.Neutral => "中性",
        _ => "关系未知"
    };

    private static string CounterHint(CombatUiMoveTypeRelationship relationship, int playerNeixi) => relationship switch
    {
        CombatUiMoveTypeRelationship.Advantage when playerNeixi >= CounterNeixiThreshold => "可反制",
        CombatUiMoveTypeRelationship.Advantage => "反制内息不足",
        CombatUiMoveTypeRelationship.Disadvantage => "谨防被反制",
        CombatUiMoveTypeRelationship.Neutral => "无明显克制",
        _ => "等待目标意图"
    };

    private static CombatUiIntentIconKind ToIconKind(TypeColorTheme theme) => theme switch
    {
        TypeColorTheme.WarmGold => CombatUiIntentIconKind.Gang,
        TypeColorTheme.CoolCyan => CombatUiIntentIconKind.Rou,
        TypeColorTheme.NeutralGray => CombatUiIntentIconKind.Qiao,
        _ => CombatUiIntentIconKind.Unknown
    };

    private static MoveType ToMoveType(CombatUiIntentIconKind kind) => kind switch
    {
        CombatUiIntentIconKind.Gang => MoveType.Gang,
        CombatUiIntentIconKind.Rou => MoveType.Rou,
        CombatUiIntentIconKind.Qiao => MoveType.Qiao,
        _ => MoveType.Gang
    };

    private static string ToGlyph(CombatUiIntentIconKind iconKind) => iconKind switch
    {
        CombatUiIntentIconKind.Gang => "拳",
        CombatUiIntentIconKind.Rou => "水",
        CombatUiIntentIconKind.Qiao => "风",
        _ => "?"
    };

    private static string FirstOrDefaultText(IReadOnlyList<string> values, string fallback)
    {
        return values.Count > 0 && !string.IsNullOrWhiteSpace(values[0])
            ? values[0]
            : fallback;
    }
}

/// <summary>
/// 面板级焦点栈生命周期。保证一次打开只入栈一次，关闭只出栈一次。
/// </summary>
public sealed class CombatUiFocusLifecycle
{
    public bool IsLayerActive { get; private set; }

    public string? FocusedActionId { get; private set; }

    /// <summary>
    /// 尝试开始一个新的 UI 焦点层。返回 true 时调用方应执行 PushFocus。
    /// </summary>
    public bool TryBegin(string actionId)
    {
        if (IsLayerActive)
            return false;

        IsLayerActive = true;
        FocusedActionId = actionId;
        return true;
    }

    /// <summary>
    /// 尝试结束当前 UI 焦点层。返回 true 时调用方应执行 PopFocus。
    /// </summary>
    public bool TryEnd()
    {
        if (!IsLayerActive)
            return false;

        IsLayerActive = false;
        FocusedActionId = null;
        return true;
    }
}

/// <summary>
/// 玩家行动阶段的招式选择面板。面板只绑定快照，不提交战斗命令。
/// </summary>
public partial class CombatMoveSelectionPanel : BaseUiPanel
{
    private readonly Dictionary<string, CombatMoveActionSlot> _slots = new(StringComparer.Ordinal);
    private readonly List<CombatMoveActionSlot> _orderedSlots = new();
    private readonly IFocusManager _focusManager;
    private readonly CombatUiFocusLifecycle _focusLifecycle = new();
    private readonly CombatUiNavigationController _navigation = new();
    private CombatUiMoveSelectionSnapshot _snapshot = new(false, Array.Empty<CombatUiMoveSelectionEntry>(), null, null, null, false, 0);
    private string? _pendingInitialFocusActionId;

    public CombatMoveSelectionPanel()
        : this(null)
    {
    }

    public CombatMoveSelectionPanel(IFocusManager? focusManager)
    {
        _focusManager = focusManager ?? new LocalFocusManager();
        Name = "CombatMoveSelectionPanel";
        MouseFilter = MouseFilterEnum.Pass;
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _slotContainer = new HBoxContainer
        {
            Name = "SlotContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _slotContainer.AnchorTop = 0.48f;
        _slotContainer.AnchorRight = 1.0f;
        _slotContainer.AnchorBottom = 1.0f;
        _slotContainer.OffsetLeft = 0f;
        _slotContainer.OffsetTop = 0f;
        _slotContainer.OffsetRight = 0f;
        _slotContainer.OffsetBottom = 0f;
        _slotContainer.AddThemeConstantOverride("separation", 2);
        AddChild(_slotContainer);

        Preview = new CombatMovePreviewCardControl();
        Preview.AnchorRight = 1.0f;
        Preview.AnchorBottom = 0.44f;
        Preview.OffsetLeft = 0f;
        Preview.OffsetTop = 0f;
        Preview.OffsetRight = 0f;
        Preview.OffsetBottom = 0f;
        AddChild(Preview);
    }

    private readonly HBoxContainer _slotContainer;

    public CombatMovePreviewCardControl Preview { get; }

    public IReadOnlyList<CombatMoveActionSlot> Slots => _orderedSlots;

    public event Action<string?>? ActionHovered;

    public event Action<string>? ActionPressed;

    public CombatUiNavigationSnapshot NavigationSnapshot => _navigation.Snapshot;

    public string? LastFocusPushedActionId { get; private set; }

    public bool LastFocusPopRequested { get; private set; }

    public static IReadOnlyList<CombatUiFocusNeighborBinding> BuildFocusNeighborBindings(CombatUiNavigationSnapshot navigation)
    {
        return navigation.Nodes
            .Select(node => new CombatUiFocusNeighborBinding(node.ActionId, node.UpNeighborActionId, node.DownNeighborActionId))
            .ToArray();
    }

    public override void _Ready()
    {
        base._Ready();
        GrabPendingInitialFocus();
    }

    /// <summary>
    /// 应用面板快照。打开时记录焦点推入目标并请求首个可用行动聚焦。
    /// </summary>
    public void ApplySnapshot(CombatUiMoveSelectionSnapshot snapshot)
    {
        _snapshot = snapshot;
        if (!snapshot.IsOpen)
        {
            ApplySlots(snapshot.Entries);
            Preview.Configure(null);
            ClosePanel();
            return;
        }

        Visible = snapshot.IsOpen;
        LastFocusPopRequested = false;
        ApplySlots(snapshot.Entries);
        _navigation.ApplySnapshot(snapshot);
        BindFocusNeighbors(_navigation.Snapshot);
        Preview.Configure(snapshot.PreviewCard);
        if (snapshot.IsOpen && snapshot.DefaultFocusActionId is not null)
            PushInitialFocus(snapshot.DefaultFocusActionId);

        MarkDirty();
    }

    /// <summary>
    /// 手柄/键盘导航到上一项。
    /// </summary>
    public CombatUiNavigationSnapshot NavigateUp()
    {
        var navigation = _navigation.Move(CombatUiNavigationDirection.Up);
        GrabFocusedSlot(navigation.FocusedActionId);
        MarkDirty();
        return navigation;
    }

    /// <summary>
    /// 手柄/键盘导航到下一项。
    /// </summary>
    public CombatUiNavigationSnapshot NavigateDown()
    {
        var navigation = _navigation.Move(CombatUiNavigationDirection.Down);
        GrabFocusedSlot(navigation.FocusedActionId);
        MarkDirty();
        return navigation;
    }

    /// <summary>
    /// 记录鼠标 hover 目标，不覆盖手柄焦点。
    /// </summary>
    public CombatUiNavigationSnapshot HoverAction(string? actionId)
    {
        var navigation = _navigation.Hover(actionId);
        MarkDirty();
        return navigation;
    }

    /// <summary>
    /// 切换输入模式并保留当前焦点状态。
    /// </summary>
    public CombatUiNavigationSnapshot ChangeInputMode(InputMode inputMode)
    {
        var navigation = _navigation.ChangeInputMode(inputMode);
        MarkDirty();
        return navigation;
    }

    /// <summary>
    /// 确认当前聚焦行动 ID。真实战斗命令由上层 Presenter/Adapter 提交。
    /// </summary>
    public string? ConfirmFocusedAction()
    {
        return _navigation.ConfirmFocusedAction();
    }

    /// <summary>
    /// 关闭面板并通过 FocusManager 恢复焦点栈。
    /// </summary>
    public void ClosePanel()
    {
        Visible = false;
        LastFocusPopRequested = true;
        LastFocusPushedActionId = null;
        _pendingInitialFocusActionId = null;
        foreach (var slot in _slots.Values)
            slot.ReleaseFocus();
        if (_focusLifecycle.TryEnd())
        {
            _focusManager.PopFocus();
        }

        MarkDirty();
    }

    private void ApplySlots(IReadOnlyList<CombatUiMoveSelectionEntry> entries)
    {
        var activeActionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            activeActionIds.Add(entry.ActionId);
            if (!_slots.TryGetValue(entry.ActionId, out var slot))
            {
                slot = new CombatMoveActionSlot();
                _slots.Add(entry.ActionId, slot);
                _orderedSlots.Add(slot);
                _slotContainer.AddChild(slot);

                // cu-008 dual-focus: slot hover -> navigation.Hover (不抢 keyboard focus)
                slot.SlotMouseEntered += OnSlotMouseEntered;
                slot.SlotMouseExited += OnSlotMouseExited;
                slot.SlotPressed += OnSlotPressed;
            }

            slot.Configure(entry);
        }

        foreach (var slot in _slots)
        {
            if (!activeActionIds.Contains(slot.Key))
                slot.Value.HideForAbsentSnapshot();
        }

        _orderedSlots.Sort((left, right) => left.StableOrder.CompareTo(right.StableOrder));
    }

    private void OnSlotMouseEntered(string actionId)
    {
        var navigation = _navigation.Hover(actionId);
        ApplyNavigationVisual(navigation);
        ActionHovered?.Invoke(actionId);
    }

    private void OnSlotMouseExited(string actionId)
    {
        // 只有当离开的恰好是当前 hover 时才清空，避免不同 slot 互踩
        if (string.Equals(_navigation.Snapshot.HoveredActionId, actionId, StringComparison.Ordinal))
        {
            var navigation = _navigation.Hover(null);
            ApplyNavigationVisual(navigation);
            ActionHovered?.Invoke(null);
        }
    }

    private void OnSlotPressed(string actionId)
    {
        var navigation = _navigation.Hover(actionId);
        ApplyNavigationVisual(navigation);
        ActionPressed?.Invoke(actionId);
    }

    private void ApplyNavigationVisual(CombatUiNavigationSnapshot navigation)
    {
        // navigation snapshot 已经包含 dual focus visual state，
        // 由 slot.OnFocusEntered/Exited + hover signal 各自维护视觉态，
        // 这里仅 reserved for future input mode handling。
        _ = navigation;
        MarkDirty();
    }

    private void BindFocusNeighbors(CombatUiNavigationSnapshot navigation)
    {
        // 不设置 Godot 原生 FocusNeighborTop/Bottom —— 导航完全由
        // CombatUiNavigationController 代码管理（NavigateUp/Down + GrabFocus）。
        // 设置原生路径会导致方向键被引擎内建焦点系统拦截，不传到 _UnhandledInput。
        foreach (var binding in BuildFocusNeighborBindings(navigation))
        {
            if (!_slots.TryGetValue(binding.ActionId, out var slot))
                continue;

            slot.ConfigureFocusNavigation(binding.UpNeighborActionId, binding.DownNeighborActionId);
        }
    }

    private void PushInitialFocus(string actionId)
    {
        if (!_slots.TryGetValue(actionId, out var slot))
            return;

        if (!_focusLifecycle.TryBegin(actionId))
            return;

        LastFocusPushedActionId = actionId;
        _pendingInitialFocusActionId = actionId;
        PushPendingInitialFocus();
    }

    private void GrabPendingInitialFocus()
    {
        PushPendingInitialFocus();
    }

    private void PushPendingInitialFocus()
    {
        if (_pendingInitialFocusActionId is null)
            return;

        if (!_slots.TryGetValue(_pendingInitialFocusActionId, out var slot) || !slot.IsInsideTree())
            return;

        _focusManager.PushFocus(slot);
        _pendingInitialFocusActionId = null;
    }

    private bool GrabFocusedSlot(string? actionId)
    {
        if (actionId is null || !_slots.TryGetValue(actionId, out var slot))
            return false;

        if (!slot.IsInsideTree() || !slot.IsEnabledForSelection)
            return false;

        slot.GrabFocus();
        return true;
    }

    private sealed class LocalFocusManager : IFocusManager
    {
        private readonly Stack<WeakReference<Control>> _stack = new();

        public InputMode CurrentMode => InputMode.Keyboard;

        public void PushFocus(Control target)
        {
            ArgumentNullException.ThrowIfNull(target);
            var current = target.GetViewport()?.GuiGetFocusOwner();
            if (current is not null && current != target)
                _stack.Push(new WeakReference<Control>(current));

            target.GrabFocus();
        }

        public void PopFocus()
        {
            while (_stack.TryPop(out var previous))
            {
                if (previous.TryGetTarget(out var control) && GodotObject.IsInstanceValid(control) && control.IsInsideTree())
                {
                    control.GrabFocus();
                    return;
                }
            }
        }

        public void ClearStack()
        {
            _stack.Clear();
        }
    }
}

/// <summary>
/// 可交互行动槽位。键鼠与手柄导航都必须能聚焦。
///
/// 视觉结构（cu-004 + cu-005 集成时通过 EnsureVisualChildren 懒构造）：
///   VBoxContainer
///   ├─ HBoxContainer  (TopRow)
///   │   ├─ Label TypeGlyph        (体系字形 拳/水/风)
///   │   ├─ Label DisplayName      (招式名)
///   │   ├─ Label NeixiCost        (内息 N)
///   │   ├─ Label XinfaBadge       (心法, 隐藏除非 IsXinfaExclusive)
///   │   └─ Label CounterTag       (反制 / 反制·内息不足, 隐藏除非 CounterPrompt.IsVisible)
///   ├─ Label EffectSummary        (效果摘要 + 触发条件)
///   └─ Label DisabledReason       (置灰原因, 隐藏除非 DisabledReason != null)
///   + ColorRect DecisiveHighlight (顶层 z-index = -1, IsDecisiveStrike 时显示金色描边)
///
/// 单元测试不调 _Ready，但 Configure 会触发 EnsureVisualChildren；调 AddChild 不需要 SceneTree。
/// </summary>
public partial class CombatMoveActionSlot : Control
{
    private HBoxContainer? _layout;
    private HBoxContainer? _topRow;
    private Label? _typeGlyphLabel;
    private Label? _nameLabel;
    private Label? _neixiLabel;
    private Label? _xinfaBadgeLabel;
    private Label? _counterTagLabel;
    private Label? _effectLabel;
    private Label? _disabledReasonLabel;
    private ColorRect? _decisiveHighlight;

    private static readonly Color CounterEnabledColor = new(1f, 0.83f, 0.20f, 1f); // counter_gold
    private static readonly Color CounterDisabledColor = new(0.70f, 0.70f, 0.70f, 1f); // counter_disabled_gray
    private static readonly Color XinfaBadgeColor = new(0.45f, 0.85f, 1.0f, 1f);
    private static readonly Color DisabledReasonColor = new(0.85f, 0.55f, 0.45f, 1f);
    private static readonly Color DecisiveHighlightColor = new(0.95f, 0.78f, 0.20f, 0.20f);

    // cu-008 dual-focus: hover 与 focus 独立视觉态
    private static readonly Color HoverBgColor = new(0.30f, 0.40f, 0.55f, 0.35f);     // mouse_hover semi-transparent blue
    private static readonly Color FocusBgColor = new(0.55f, 0.40f, 0.15f, 0.45f);     // keyboard/gamepad focus amber
    private static readonly Color FocusAndHoverBgColor = new(0.50f, 0.45f, 0.25f, 0.55f); // 同时被聚焦 + 悬停

    private ColorRect? _hoverBackground;
    private ColorRect? _focusBackground;
    private bool _isHovered;
    private bool _isFocused;

    /// <summary>
    /// 鼠标进入槽位 hover 区域（cu-008 dual-focus 路径，宿主用于通知 binder 调 panel.HoverAction）。
    /// </summary>
    [Signal]
    public delegate void SlotMouseEnteredEventHandler(string actionId);

    /// <summary>
    /// 鼠标离开槽位 hover 区域。
    /// </summary>
    [Signal]
    public delegate void SlotMouseExitedEventHandler(string actionId);

    [Signal]
    public delegate void SlotPressedEventHandler(string actionId);

    public CombatMoveActionSlot()
    {
        Name = "CombatMoveActionSlot";
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(0, 28);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        FocusEntered += OnFocusEntered;
        FocusExited += OnFocusExited;
        GuiInput += OnGuiInput;
    }

    /// <summary>当前 slot 是否被鼠标 hover（仅 UI 视觉，不影响 focus）。</summary>
    public bool IsMouseHovered => _isHovered;

    /// <summary>当前 slot 是否被键盘/手柄聚焦。</summary>
    public bool IsKeyboardFocused => _isFocused;

    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.All;

    public static FocusModeEnum ResolveFocusMode(bool isEnabled)
    {
        return isEnabled ? FocusModeEnum.All : FocusModeEnum.None;
    }

    public string ActionId { get; private set; } = string.Empty;

    public string? MoveId { get; private set; }

    public CombatUiMoveActionKind ActionKind { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public CombatUiIntentIconKind TypeIconKind { get; private set; } = CombatUiIntentIconKind.Unknown;

    public string TypeGlyph { get; private set; } = "?";

    public int NeixiCost { get; private set; }

    public string TriggerConditionIcon { get; private set; } = string.Empty;

    public string EffectSummary { get; private set; } = string.Empty;

    public bool IsXinfaExclusive { get; private set; }

    public bool IsEnabledForSelection { get; private set; }

    public string? DisabledReason { get; private set; }

    public CombatUiCounterPrompt? CounterPrompt { get; private set; }

    public string? TargetId { get; private set; }

    public bool IsDecisiveStrike { get; private set; }

    public int StableOrder { get; private set; }

    public string UpNeighborActionId { get; private set; } = string.Empty;

    public string DownNeighborActionId { get; private set; } = string.Empty;

    public void Configure(CombatUiMoveSelectionEntry entry)
    {
        ActionId = entry.ActionId;
        Name = ToNodeName(entry.ActionId);
        MoveId = entry.MoveId;
        ActionKind = entry.ActionKind;
        DisplayName = entry.DisplayName;
        TypeIconKind = entry.TypeIconKind;
        TypeGlyph = entry.TypeGlyph;
        NeixiCost = entry.NeixiCost;
        TriggerConditionIcon = entry.TriggerConditionIcon;
        EffectSummary = entry.EffectSummary;
        IsXinfaExclusive = entry.IsXinfaExclusive;
        IsEnabledForSelection = entry.IsEnabled;
        DisabledReason = entry.DisabledReason;
        CounterPrompt = entry.CounterPrompt;
        TargetId = entry.TargetId;
        IsDecisiveStrike = entry.IsDecisiveStrike;
        StableOrder = entry.StableOrder;
        FocusMode = ResolveFocusMode(entry.IsEnabled);
        if (!entry.IsEnabled)
            ReleaseFocus();
        Visible = true;
        Modulate = entry.IsEnabled ? Colors.White : new Color(1f, 1f, 1f, 0.4f);

        EnsureVisualChildren();
        RefreshVisualLabels();
    }

    public void ConfigureFocusNavigation(string upNeighborActionId, string downNeighborActionId)
    {
        UpNeighborActionId = upNeighborActionId;
        DownNeighborActionId = downNeighborActionId;
    }

    public void HideForAbsentSnapshot()
    {
        ReleaseFocus();
        Visible = false;
        CounterPrompt = null;
        TargetId = null;
        IsDecisiveStrike = false;
        UpNeighborActionId = string.Empty;
        DownNeighborActionId = string.Empty;
        FocusMode = FocusModeEnum.None;
    }

    /// <summary>
    /// 懒构造视觉子节点。idempotent — 第一次 Configure 时建立，后续刷新只更新 Label 文字。
    /// 不依赖 _Ready 调用时序，单元测试也可触发（AddChild 不需要 SceneTree）。
    /// </summary>
    private void EnsureVisualChildren()
    {
        if (_layout != null)
            return;

        // cu-008 dual-focus: focus 与 hover 各一个独立背景层，互不抢
        _focusBackground = new ColorRect
        {
            Name = "FocusBackground",
            Color = FocusBgColor,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = -2,
        };
        _focusBackground.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_focusBackground);

        _hoverBackground = new ColorRect
        {
            Name = "HoverBackground",
            Color = HoverBgColor,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = -2,
        };
        _hoverBackground.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_hoverBackground);

        _decisiveHighlight = new ColorRect
        {
            Name = "DecisiveHighlight",
            Color = DecisiveHighlightColor,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = -1,
        };
        _decisiveHighlight.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_decisiveHighlight);

        _layout = new HBoxContainer
        {
            Name = "Layout",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _layout.AddThemeConstantOverride("separation", 12);
        AddChild(_layout);

        _topRow = _layout;

        _typeGlyphLabel = new Label { Name = "TypeGlyph", CustomMinimumSize = new Vector2(20, 0) };
        _layout.AddChild(_typeGlyphLabel);

        _nameLabel = new Label { Name = "DisplayName", CustomMinimumSize = new Vector2(120, 0) };
        _layout.AddChild(_nameLabel);

        _neixiLabel = new Label { Name = "NeixiCost", CustomMinimumSize = new Vector2(60, 0) };
        _layout.AddChild(_neixiLabel);

        _xinfaBadgeLabel = new Label
        {
            Name = "XinfaBadge",
            Text = "[心法]",
            Visible = false,
            Modulate = XinfaBadgeColor,
            CustomMinimumSize = new Vector2(40, 0),
        };
        _layout.AddChild(_xinfaBadgeLabel);

        _counterTagLabel = new Label
        {
            Name = "CounterTag",
            Visible = false,
            CustomMinimumSize = new Vector2(50, 0),
        };
        _layout.AddChild(_counterTagLabel);

        _effectLabel = new Label
        {
            Name = "EffectSummary",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _layout.AddChild(_effectLabel);

        _disabledReasonLabel = new Label
        {
            Name = "DisabledReason",
            Visible = false,
            Modulate = DisabledReasonColor,
        };
        _layout.AddChild(_disabledReasonLabel);
    }

    private void RefreshVisualLabels()
    {
        if (_topRow == null) return;

        _typeGlyphLabel!.Text = TypeGlyph;
        _nameLabel!.Text = DisplayName;
        _neixiLabel!.Text = NeixiCost > 0 ? $"内息 {NeixiCost}" : "—";
        _xinfaBadgeLabel!.Visible = IsXinfaExclusive;
        _effectLabel!.Text = string.IsNullOrEmpty(TriggerConditionIcon)
            ? EffectSummary
            : $"({TriggerConditionIcon}) {EffectSummary}";

        if (CounterPrompt is { IsVisible: true } counter)
        {
            _counterTagLabel!.Visible = true;
            _counterTagLabel.Text = counter.IsEnabled
                ? counter.Label
                : $"{counter.Label}";
            _counterTagLabel.Modulate = counter.IsEnabled ? CounterEnabledColor : CounterDisabledColor;
        }
        else
        {
            _counterTagLabel!.Visible = false;
        }

        _disabledReasonLabel!.Visible = !string.IsNullOrEmpty(DisabledReason);
        _disabledReasonLabel.Text = DisabledReason ?? string.Empty;

        _decisiveHighlight!.Visible = IsDecisiveStrike;
        RefreshDualFocusVisuals();
    }

    /// <summary>
    /// cu-008: hover + focus 两个状态独立计算视觉态。
    /// 同时存在时混合色（焦点 dominant），仅 focus 时琥珀，仅 hover 时蓝。
    /// </summary>
    private void RefreshDualFocusVisuals()
    {
        if (_focusBackground == null || _hoverBackground == null)
            return;

        // disabled 槽位不允许任何 hover/focus 视觉
        if (!IsEnabledForSelection)
        {
            _focusBackground.Visible = false;
            _hoverBackground.Visible = false;
            return;
        }

        _focusBackground.Visible = _isFocused;
        _hoverBackground.Visible = _isHovered;

        if (_isFocused && _isHovered)
            _focusBackground.Color = FocusAndHoverBgColor;
        else
            _focusBackground.Color = FocusBgColor;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        RefreshDualFocusVisuals();
        if (IsEnabledForSelection && !string.IsNullOrEmpty(ActionId))
            EmitSignal(SignalName.SlotMouseEntered, ActionId);
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        RefreshDualFocusVisuals();
        if (!string.IsNullOrEmpty(ActionId))
            EmitSignal(SignalName.SlotMouseExited, ActionId);
    }

    private void OnFocusEntered()
    {
        _isFocused = true;
        RefreshDualFocusVisuals();
    }

    private void OnFocusExited()
    {
        _isFocused = false;
        RefreshDualFocusVisuals();
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (!IsEnabledForSelection || string.IsNullOrEmpty(ActionId))
            return;
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;

        EmitSignal(SignalName.SlotPressed, ActionId);
    }

    private static string ToNodeName(string actionId)
    {
        return $"MoveAction_{actionId.Replace(':', '_')}";
    }
}

/// <summary>
/// 招式预览卡 Control。只展示关系和反制提示，不展示预测数值。
///
/// 视觉结构：
///   VBoxContainer
///   ├─ Label TitleLabel      ("预览：[招式名]")
///   ├─ Label RelationshipLabel ("关系：克制/中性/被克")
///   └─ Label CounterHintLabel  ("可反制 / 谨防被反制 / 等待目标意图" 等)
/// </summary>
public partial class CombatMovePreviewCardControl : Control
{
    private VBoxContainer? _layout;
    private Label? _titleLabel;
    private Label? _relationshipLabel;
    private Label? _counterHintLabel;

    private static readonly Color AdvantageColor = new(1f, 0.83f, 0.20f, 1f);
    private static readonly Color DisadvantageColor = new(0.95f, 0.45f, 0.40f, 1f);
    private static readonly Color NeutralColor = new(0.80f, 0.80f, 0.80f, 1f);
    private static readonly Color UnknownColor = new(0.60f, 0.60f, 0.65f, 1f);

    public CombatMovePreviewCardControl()
    {
        Name = "CombatMovePreviewCard";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(440, 70);
    }

    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.None;

    public string ActionId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public CombatUiMoveTypeRelationship TypeRelationship { get; private set; } = CombatUiMoveTypeRelationship.Unknown;

    public string RelationshipText { get; private set; } = string.Empty;

    public string CounterHint { get; private set; } = string.Empty;

    public void Configure(CombatUiMovePreviewCard? card)
    {
        if (card is null)
        {
            Visible = false;
            ActionId = string.Empty;
            DisplayName = string.Empty;
            TypeRelationship = CombatUiMoveTypeRelationship.Unknown;
            RelationshipText = string.Empty;
            CounterHint = string.Empty;
            return;
        }

        Visible = true;
        ActionId = card.ActionId;
        DisplayName = card.DisplayName;
        TypeRelationship = card.TypeRelationship;
        RelationshipText = card.RelationshipText;
        CounterHint = card.CounterHint;

        EnsureVisualChildren();
        RefreshVisualLabels();
    }

    private void EnsureVisualChildren()
    {
        if (_layout != null) return;

        _layout = new VBoxContainer { Name = "Layout", MouseFilter = MouseFilterEnum.Ignore };
        _layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _layout.AddThemeConstantOverride("separation", 2);
        AddChild(_layout);

        _titleLabel = new Label { Name = "Title" };
        _layout.AddChild(_titleLabel);

        _relationshipLabel = new Label { Name = "Relationship" };
        _layout.AddChild(_relationshipLabel);

        _counterHintLabel = new Label { Name = "CounterHint" };
        _layout.AddChild(_counterHintLabel);
    }

    private void RefreshVisualLabels()
    {
        if (_titleLabel == null) return;

        _titleLabel.Text = $"预览：{DisplayName}";
        _relationshipLabel!.Text = $"关系：{RelationshipText}";
        _relationshipLabel.Modulate = TypeRelationship switch
        {
            CombatUiMoveTypeRelationship.Advantage => AdvantageColor,
            CombatUiMoveTypeRelationship.Disadvantage => DisadvantageColor,
            CombatUiMoveTypeRelationship.Neutral => NeutralColor,
            _ => UnknownColor,
        };
        _counterHintLabel!.Text = CounterHint;
    }
}
