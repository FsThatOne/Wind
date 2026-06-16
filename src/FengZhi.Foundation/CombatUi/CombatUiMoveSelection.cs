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
    BasicAttack,
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
/// 将 MartialArts 战斗面板数据和战斗资源快照组装为 Combat UI 招式选择快照。
/// </summary>
public sealed class CombatUiMoveSelectionPresenter
{
    public const int MaxEquippedMoveSlots = 6;
    public const int BaseActionCount = 3;
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
        result.Add(BuildBaseAction("basic_attack", CombatUiMoveActionKind.BasicAttack, "普通攻击", order++));
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
    private readonly IFocusManager? _focusManager;
    private readonly CombatUiFocusLifecycle _focusLifecycle = new();
    private CombatUiMoveSelectionSnapshot _snapshot = new(false, Array.Empty<CombatUiMoveSelectionEntry>(), null, null, null, false, 0);

    public CombatMoveSelectionPanel()
        : this(null)
    {
    }

    public CombatMoveSelectionPanel(IFocusManager? focusManager)
    {
        _focusManager = focusManager;
        Name = "CombatMoveSelectionPanel";
        MouseFilter = MouseFilterEnum.Pass;
        Visible = false;
        Preview = new CombatMovePreviewCardControl();
        AddChild(Preview);
    }

    public CombatMovePreviewCardControl Preview { get; }

    public IReadOnlyList<CombatMoveActionSlot> Slots => _orderedSlots;

    public string? LastFocusPushedActionId { get; private set; }

    public bool LastFocusPopRequested { get; private set; }

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
        Preview.Configure(snapshot.PreviewCard);
        if (snapshot.IsOpen && snapshot.DefaultFocusActionId is not null)
            PushInitialFocus(snapshot.DefaultFocusActionId);

        MarkDirty();
    }

    /// <summary>
    /// 关闭面板并通过 FocusManager 恢复焦点栈。
    /// </summary>
    public void ClosePanel()
    {
        Visible = false;
        LastFocusPopRequested = true;
        LastFocusPushedActionId = null;
        foreach (var slot in _slots.Values)
            slot.ReleaseFocus();
        if (_focusLifecycle.TryEnd())
        {
            _focusManager?.PopFocus();
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
                AddChild(slot);
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

    private void PushInitialFocus(string actionId)
    {
        if (!_slots.TryGetValue(actionId, out var slot))
            return;

        if (!_focusLifecycle.TryBegin(actionId))
            return;

        LastFocusPushedActionId = actionId;
        if (_focusManager is not null)
        {
            _focusManager.PushFocus(slot);
            return;
        }

        if (slot.IsInsideTree())
            slot.GrabFocus();
    }
}

/// <summary>
/// 可交互行动槽位。键鼠与手柄导航都必须能聚焦。
/// </summary>
public partial class CombatMoveActionSlot : Control
{
    public CombatMoveActionSlot()
    {
        Name = "CombatMoveActionSlot";
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Pass;
    }

    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.All;

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

    public void Configure(CombatUiMoveSelectionEntry entry)
    {
        ActionId = entry.ActionId;
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
        Visible = true;
        Modulate = entry.IsEnabled ? Colors.White : new Color(1f, 1f, 1f, 0.4f);
    }

    public void HideForAbsentSnapshot()
    {
        ReleaseFocus();
        Visible = false;
        CounterPrompt = null;
        TargetId = null;
        IsDecisiveStrike = false;
    }
}

/// <summary>
/// 招式预览卡 Control。只展示关系和反制提示，不展示预测数值。
/// </summary>
public partial class CombatMovePreviewCardControl : Control
{
    public CombatMovePreviewCardControl()
    {
        Name = "CombatMovePreviewCard";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
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
    }
}
