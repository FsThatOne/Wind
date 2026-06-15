using FengZhi.Foundation.Presentation.Shared;
using Godot;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 战场空间意图层。用于挂载跟随战场目标的意图提示。
/// </summary>
public partial class WorldIntentLayer : CanvasLayer
{
    public WorldIntentLayer()
    {
        Name = CombatUiLayerDescriptor.WorldIntentLayer.Name;
        Layer = CombatUiLayerDescriptor.WorldIntentLayer.CanvasLayerOrder;
        Visible = true;

        Panel = new WorldIntentPanel();
        AddChild(Panel);
    }

    public WorldIntentPanel Panel { get; }
}

/// <summary>
/// 屏幕空间战斗 HUD 层。用于挂载资源条、行动提示和后续战斗面板。
/// </summary>
public partial class CombatHudLayer : CanvasLayer
{
    public CombatHudLayer()
    {
        Name = CombatUiLayerDescriptor.HudLayer.Name;
        Layer = CombatUiLayerDescriptor.HudLayer.CanvasLayerOrder;
        Visible = true;

        Panel = new CombatHudPanel();
        AddChild(Panel);
    }

    public CombatHudPanel Panel { get; }
}

/// <summary>
/// 世界意图基础面板。具体图标与动画由后续 story 扩展。
/// </summary>
public partial class WorldIntentPanel : BaseUiPanel
{
    private readonly Dictionary<string, CombatIntentIconSlot> _slots = new(StringComparer.Ordinal);

    public WorldIntentPanel()
    {
        Name = "WorldIntentPanel";
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// 当前世界空间意图槽位。槽位按敌人首次出现顺序复用。
    /// </summary>
    public IReadOnlyList<CombatIntentIconSlot> Slots => _slots.Values
        .OrderBy(slot => slot.StableOrder)
        .ToArray();

    /// <summary>
    /// 应用世界空间意图快照。不会重建已有槽位。
    /// </summary>
    public void ApplyIntentSnapshot(IReadOnlyList<CombatUiIntentDisplayEntry> entries)
    {
        ApplyEntries(entries);
    }

    private void ApplyEntries(IReadOnlyList<CombatUiIntentDisplayEntry> entries)
    {
        var activeEnemyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            activeEnemyIds.Add(entry.EnemyId);
            if (!_slots.TryGetValue(entry.EnemyId, out var slot))
            {
                slot = new CombatIntentIconSlot();
                _slots.Add(entry.EnemyId, slot);
                AddChild(slot);
            }

            slot.Configure(entry);
        }

        HideAbsentSlots(_slots, activeEnemyIds);
        MarkDirty();
    }

    private static void HideAbsentSlots(
        Dictionary<string, CombatIntentIconSlot> slots,
        HashSet<string> activeEnemyIds)
    {
        foreach (var slot in slots)
        {
            if (!activeEnemyIds.Contains(slot.Key))
                slot.Value.HideForAbsentSnapshot();
        }
    }
}

/// <summary>
/// 战斗 HUD 基础面板。当前只承载快照绑定入口。
/// </summary>
public partial class CombatHudPanel : BaseUiPanel
{
    private readonly Dictionary<string, CombatIntentIconSlot> _intentSummarySlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CombatResourceBarSlot> _resourceSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StaggerCueLabel> _staggerCueLabels = new(StringComparer.Ordinal);
    private readonly HashSet<int> _playedDamageNumberSequences = new();

    public CombatHudPanel()
    {
        Name = "CombatHudPanel";
        MouseFilter = MouseFilterEnum.Pass;
        DamageNumbers = new DamageNumberPool();
        AddChild(DamageNumbers);
    }

    /// <summary>
    /// 伤害数字池。HUD 创建时预分配，战斗中复用。
    /// </summary>
    public DamageNumberPool DamageNumbers { get; }

    /// <summary>
    /// HUD 汇总意图槽位。顺序稳定，供测试和后续场景绑定使用。
    /// </summary>
    public IReadOnlyList<CombatIntentIconSlot> IntentSummarySlots => _intentSummarySlots.Values
        .OrderBy(slot => slot.StableOrder)
        .ToArray();

    /// <summary>
    /// 当前 HUD 资源条槽位。
    /// </summary>
    public IReadOnlyList<CombatResourceBarSlot> ResourceSlots => _resourceSlots.Values
        .OrderBy(slot => slot.CombatantId, StringComparer.Ordinal)
        .ThenBy(slot => slot.Kind)
        .ToArray();

    /// <summary>
    /// 当前破绽爆满提示。
    /// </summary>
    public IReadOnlyList<StaggerCueLabel> StaggerCueLabels => _staggerCueLabels.Values
        .OrderBy(label => label.TargetId, StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// 应用 HUD 意图汇总快照。仅更新变化槽位，避免每帧全量重建。
    /// </summary>
    public void ApplyIntentSummary(IReadOnlyList<CombatUiIntentDisplayEntry> entries)
    {
        var activeEnemyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            activeEnemyIds.Add(entry.EnemyId);
            if (!_intentSummarySlots.TryGetValue(entry.EnemyId, out var slot))
            {
                slot = new CombatIntentIconSlot();
                _intentSummarySlots.Add(entry.EnemyId, slot);
                AddChild(slot);
            }

            slot.Configure(entry);
        }

        HideAbsentSlots(activeEnemyIds);
        MarkDirty();
    }

    /// <summary>
    /// 应用资源条快照。槽位按角色和资源类型稳定复用。
    /// </summary>
    public void ApplyResourceSnapshot(IReadOnlyList<CombatUiResourceDisplayEntry> entries)
    {
        var activeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var key = ResourceSlotKey(entry.CombatantId, entry.Kind);
            activeKeys.Add(key);
            if (!_resourceSlots.TryGetValue(key, out var slot))
            {
                slot = new CombatResourceBarSlot();
                _resourceSlots.Add(key, slot);
                AddChild(slot);
            }

            slot.Configure(entry);
        }

        foreach (var slot in _resourceSlots)
        {
            if (!activeKeys.Contains(slot.Key))
                slot.Value.HideForAbsentSnapshot();
        }

        MarkDirty();
    }

    /// <summary>
    /// 应用伤害浮字快照。同帧多个事件由 DamageNumberPool 依次分配。
    /// </summary>
    public void ApplyDamageNumbers(IReadOnlyList<CombatUiDamageNumberDisplayEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (_playedDamageNumberSequences.Add(entry.SequenceId))
                DamageNumbers.Acquire(entry);
        }

        MarkDirty();
    }

    /// <summary>
    /// 应用破绽爆满提示。
    /// </summary>
    public void ApplyStaggerCues(IReadOnlyList<CombatUiStaggerCueEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!_staggerCueLabels.TryGetValue(entry.TargetId, out var label))
            {
                label = new StaggerCueLabel();
                _staggerCueLabels.Add(entry.TargetId, label);
                AddChild(label);
            }

            label.Configure(entry);
        }

        MarkDirty();
    }

    private void HideAbsentSlots(HashSet<string> activeEnemyIds)
    {
        foreach (var slot in _intentSummarySlots)
        {
            if (!activeEnemyIds.Contains(slot.Key))
                slot.Value.HideForAbsentSnapshot();
        }
    }

    private static string ResourceSlotKey(string combatantId, CombatUiResourceKind kind) => $"{combatantId}:{kind}";
}

/// <summary>
/// 非交互意图图标槽位。真实符文资产后续替换 Glyph 绑定，不改变数据契约。
/// </summary>
public partial class CombatIntentIconSlot : Control
{
    public const double IntentIconFadeInDurationSeconds = 0.18;
    public const float IntentIconRisePixels = 6f;

    private Tween? _revealTween;

    public CombatIntentIconSlot()
    {
        Name = "CombatIntentIconSlot";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// 池化/复用意图槽位必须使用的焦点模式。
    /// </summary>
    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.None;

    public string EnemyId { get; private set; } = string.Empty;

    public CombatUiIntentIconKind IconKind { get; private set; } = CombatUiIntentIconKind.Unknown;

    public string Glyph { get; private set; } = "?";

    public string ColorKey { get; private set; } = "intent_unknown_fog";

    public string? RevealedMoveName { get; private set; }

    public bool ShouldPlayRevealTransition { get; private set; }

    public CombatUiIntentSlotState SlotState { get; private set; } = CombatUiIntentSlotState.Visible;

    public int StableOrder { get; private set; }

    /// <summary>
    /// 最近一次 Configure 是否请求了真实 reveal 动画，供自动测试验证动画入口。
    /// </summary>
    public bool LastRevealAnimationRequested { get; private set; }

    /// <summary>
    /// 将只读展示条目绑定到槽位。已落败目标跳过过渡动画。
    /// </summary>
    public void Configure(CombatUiIntentDisplayEntry entry)
    {
        EnemyId = entry.EnemyId;
        IconKind = entry.IconKind;
        Glyph = entry.Glyph;
        ColorKey = entry.ColorKey;
        RevealedMoveName = entry.RevealedMoveName;
        SlotState = entry.SlotState;
        StableOrder = entry.StableOrder;
        ShouldPlayRevealTransition = entry.ShouldPlayRevealTransition
            && entry.SlotState == CombatUiIntentSlotState.Visible;
        Visible = entry.SlotState != CombatUiIntentSlotState.Hidden;
        LastRevealAnimationRequested = ShouldPlayRevealTransition;
        ApplyStaticVisibility(entry);

        if (ShouldPlayRevealTransition)
            PlayRevealTransition();
    }

    /// <summary>
    /// 本次战斗意图快照不再包含该目标时隐藏槽位并释放焦点。
    /// </summary>
    public void HideForAbsentSnapshot()
    {
        StopRevealTransition();
        ReleaseFocus();
        Visible = false;
        SlotState = CombatUiIntentSlotState.Hidden;
        ShouldPlayRevealTransition = false;
        LastRevealAnimationRequested = false;
    }

    private void ApplyStaticVisibility(CombatUiIntentDisplayEntry entry)
    {
        if (entry.SlotState != CombatUiIntentSlotState.Visible)
            StopRevealTransition();

        Modulate = entry.SlotState == CombatUiIntentSlotState.Dimmed
            ? new Color(1f, 1f, 1f, 0.35f)
            : Colors.White;
    }

    private void PlayRevealTransition()
    {
        StopRevealTransition();
        if (!IsInsideTree())
            return;

        var finalPosition = Position;
        Position = finalPosition + new Vector2(0f, IntentIconRisePixels);
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0f);

        _revealTween = CreateTween();
        _revealTween.SetParallel();
        _revealTween.TweenProperty(this, "modulate:a", 1f, IntentIconFadeInDurationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _revealTween.TweenProperty(this, "position", finalPosition, IntentIconFadeInDurationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void StopRevealTransition()
    {
        _revealTween?.Kill();
        _revealTween = null;
    }
}

/// <summary>
/// 非交互资源条槽位。真实美术材质后续替换 ColorKey / VisualKey 绑定。
/// </summary>
public partial class CombatResourceBarSlot : Control
{
    private Tween? _valueTween;
    private AnimationPlayer? _pulseAnimation;

    public CombatResourceBarSlot()
    {
        Name = "CombatResourceBarSlot";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.None;

    public string CombatantId { get; private set; } = string.Empty;

    public CombatUiResourceKind Kind { get; private set; }

    public int Current { get; private set; }

    public int Maximum { get; private set; } = 1;

    public double FillRatio { get; private set; }

    public string ColorKey { get; private set; } = string.Empty;

    public string VisualKey { get; private set; } = string.Empty;

    public bool LastFlashWhiteRequested { get; private set; }

    public bool LastTweenRequested { get; private set; }

    public bool LastPulseRequested { get; private set; }

    public double UpdateDurationSeconds { get; private set; }

    /// <summary>
    /// 绑定资源条展示数据，并按契约请求闪白、Tween 或破绽脉冲入口。
    /// </summary>
    public void Configure(CombatUiResourceDisplayEntry entry)
    {
        CombatantId = entry.CombatantId;
        Kind = entry.Kind;
        Current = entry.Current;
        Maximum = entry.Maximum;
        FillRatio = entry.FillRatio;
        ColorKey = entry.ColorKey;
        VisualKey = entry.VisualKey;
        LastFlashWhiteRequested = entry.ShouldFlashWhite;
        LastTweenRequested = entry.ShouldTweenValue;
        LastPulseRequested = entry.IsExposed && entry.Kind == CombatUiResourceKind.Stagger;
        UpdateDurationSeconds = entry.UpdateDurationSeconds;
        Visible = true;

        if (entry.ShouldTweenValue)
            PlayValueTween(entry.UpdateDurationSeconds);
        if (LastPulseRequested)
            EnsurePulseAnimation();
    }

    public void HideForAbsentSnapshot()
    {
        StopAnimations();
        ReleaseFocus();
        Visible = false;
    }

    private void PlayValueTween(double durationSeconds)
    {
        _valueTween?.Kill();
        _valueTween = null;
        if (!IsInsideTree())
            return;

        _valueTween = CreateTween();
        _valueTween.TweenProperty(this, "modulate:a", 1f, durationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void EnsurePulseAnimation()
    {
        if (_pulseAnimation is not null)
            return;

        _pulseAnimation = new AnimationPlayer();
        AddChild(_pulseAnimation);
    }

    private void StopAnimations()
    {
        _valueTween?.Kill();
        _valueTween = null;
        _pulseAnimation?.Stop();
    }
}

/// <summary>
/// 伤害数字对象池。预分配 12 个非交互 Label，战斗中循环复用。
/// </summary>
public partial class DamageNumberPool : Control
{
    private readonly Queue<DamageNumberLabel> _available = new();
    private readonly List<DamageNumberLabel> _labels = new();

    public const int RequiredPoolSize = CombatUiFeedbackTuning.DamageNumberPoolSize;

    public DamageNumberPool()
    {
        Name = "DamageNumberPool";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;

        for (var i = 0; i < RequiredPoolSize; i++)
        {
            var label = new DamageNumberLabel(Release);
            label.Visible = false;
            _labels.Add(label);
            _available.Enqueue(label);
            AddChild(label);
        }
    }

    public IReadOnlyList<DamageNumberLabel> Labels => _labels;

    public int AvailableCount => _available.Count;

    public int ActiveCount => _labels.Count(label => label.Visible);

    public DamageNumberLabel Acquire(CombatUiDamageNumberDisplayEntry entry)
    {
        var label = _available.Count > 0
            ? _available.Dequeue()
            : _labels.OrderBy(item => item.LastAcquiredSequence).First();
        label.Configure(entry);
        return label;
    }

    public void Release(DamageNumberLabel label)
    {
        if (!_labels.Contains(label))
            return;

        label.ResetForPool();
        if (!_available.Contains(label))
            _available.Enqueue(label);
    }
}

/// <summary>
/// 单个伤害浮字 Label。
/// </summary>
public partial class DamageNumberLabel : Label
{
    private readonly Action<DamageNumberLabel>? _releaseToPool;
    private Vector2 _restingPosition;
    private Tween? _floatTween;
    private bool _isReleased = true;
    private int _lastAcquiredSequence;

    public DamageNumberLabel()
        : this(null)
    {
    }

    internal DamageNumberLabel(Action<DamageNumberLabel>? releaseToPool)
    {
        _releaseToPool = releaseToPool;
        Name = "DamageNumberLabel";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public static FocusModeEnum RequiredFocusMode => FocusModeEnum.None;

    public int Amount { get; private set; }

    public CombatUiDamageNumberStyleKind StyleKind { get; private set; } = CombatUiDamageNumberStyleKind.Neutral;

    public double FontScale { get; private set; } = 1.0;

    public string ColorHex { get; private set; } = "#FFFFFF";

    public bool HasBlackOutline { get; private set; }

    public bool UsesMicroBurst { get; private set; }

    public int LastLaneIndex { get; private set; }

    public int LastAcquiredSequence => _lastAcquiredSequence;

    public float VerticalOffsetPixels { get; private set; }

    public double DurationSeconds { get; private set; }

    public void Configure(CombatUiDamageNumberDisplayEntry entry)
    {
        _floatTween?.Kill();
        _floatTween = null;
        _isReleased = false;
        _lastAcquiredSequence++;
        Position = _restingPosition;
        Modulate = Colors.White;
        RemoveThemeColorOverride("font_outline_color");
        RemoveThemeConstantOverride("outline_size");

        Amount = entry.Amount;
        StyleKind = entry.StyleKind;
        FontScale = entry.FontScale;
        ColorHex = entry.ColorHex;
        HasBlackOutline = entry.HasBlackOutline;
        UsesMicroBurst = entry.UsesMicroBurst;
        LastLaneIndex = entry.LaneIndex;
        VerticalOffsetPixels = entry.VerticalOffsetPixels;
        DurationSeconds = entry.DurationSeconds;
        Text = entry.Amount.ToString();
        Visible = true;

        if (Color.FromHtml(entry.ColorHex) is { } color)
            Modulate = color;

        AddThemeFontSizeOverride("font_size", (int)Math.Round(CombatUiFeedbackTuning.BaseDamageFontSize * entry.FontScale));
        if (entry.HasBlackOutline)
        {
            AddThemeColorOverride("font_outline_color", Colors.Black);
            AddThemeConstantOverride("outline_size", 2);
        }

        PlayFloatTween(entry.DurationSeconds);
    }

    public void ResetForPool()
    {
        _floatTween?.Kill();
        _floatTween = null;
        _isReleased = true;
        Position = _restingPosition;
        Modulate = Colors.White;
        RemoveThemeFontSizeOverride("font_size");
        RemoveThemeColorOverride("font_outline_color");
        RemoveThemeConstantOverride("outline_size");
        ReleaseFocus();
        Visible = false;
        Text = string.Empty;
    }

    private void PlayFloatTween(double durationSeconds)
    {
        _floatTween?.Kill();
        _floatTween = null;
        if (!IsInsideTree())
            return;

        var finalPosition = Position + new Vector2(0f, -24f - VerticalOffsetPixels);
        _floatTween = CreateTween();
        _floatTween.SetParallel();
        _floatTween.TweenProperty(this, "position", finalPosition, durationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _floatTween.TweenProperty(this, "modulate:a", 0f, durationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _floatTween.Finished += ReleaseAfterTween;
    }

    private void ReleaseAfterTween()
    {
        if (_isReleased)
            return;

        if (_releaseToPool is null)
        {
            ResetForPool();
            return;
        }

        _releaseToPool(this);
    }
}

/// <summary>
/// 破绽爆满浮字。后续可替换为篆刻印章风素材。
/// </summary>
public partial class StaggerCueLabel : Label
{
    public StaggerCueLabel()
    {
        Name = "StaggerCueLabel";
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public string TargetId { get; private set; } = string.Empty;

    public bool ShouldPulse { get; private set; }

    public double PulseIntervalSeconds { get; private set; }

    public string ColorKey { get; private set; } = string.Empty;

    public void Configure(CombatUiStaggerCueEntry entry)
    {
        TargetId = entry.TargetId;
        Text = entry.Glyph;
        ColorKey = entry.ColorKey;
        ShouldPulse = entry.ShouldPulse;
        PulseIntervalSeconds = entry.PulseIntervalSeconds;
        Visible = entry.IsExposed;
    }
}
