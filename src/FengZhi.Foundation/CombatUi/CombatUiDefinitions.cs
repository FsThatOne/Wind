using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 战斗 UI 的展示状态流。
/// </summary>
public enum CombatUiState
{
    Inactive,
    RoundStart,
    PlayerDecision,
    Resolving,
    RoundEnd,
    BattleEnd
}

/// <summary>
/// 战斗 UI CanvasLayer 的语义类型。
/// </summary>
public enum CombatUiLayerKind
{
    WorldIntent,
    Hud
}

/// <summary>
/// 战斗 UI 意图图标类型。Unknown 只能展示问号迷雾，不携带真实体系。
/// </summary>
public enum CombatUiIntentIconKind
{
    Unknown,
    Gang,
    Rou,
    Qiao
}

/// <summary>
/// 战斗 UI 意图槽位状态。
/// </summary>
public enum CombatUiIntentSlotState
{
    Visible,
    Dimmed,
    Hidden
}

/// <summary>
/// 战斗 HUD 资源条类型。
/// </summary>
public enum CombatUiResourceKind
{
    Health,
    Neixi,
    Stagger
}

/// <summary>
/// 伤害浮字样式类型。只表达视觉，不参与伤害计算。
/// </summary>
public enum CombatUiDamageNumberStyleKind
{
    Advantage,
    Neutral,
    Disadvantage,
    Critical,
    Decisive
}

/// <summary>
/// 战斗 UI 层描述。后续 Godot CanvasLayer 节点按此契约创建。
/// </summary>
public sealed record CombatUiLayerDescriptor(
    string Name,
    CombatUiLayerKind Kind,
    int CanvasLayerOrder,
    bool IsScreenSpace)
{
    public static CombatUiLayerDescriptor WorldIntentLayer { get; } =
        new("WorldIntentLayer", CombatUiLayerKind.WorldIntent, 10, false);

    public static CombatUiLayerDescriptor HudLayer { get; } =
        new("HUDLayer", CombatUiLayerKind.Hud, 20, true);
}

/// <summary>
/// 敌方意图展示条目。字段来自战斗事件，不在 UI 层重新识破。
/// </summary>
public sealed record CombatUiIntentEntry(
    string EnemyId,
    IntentVisibility Visibility,
    string? MoveTypeName,
    MoveType? MoveType);

/// <summary>
/// 意图图标展示条目。WorldIntentLayer 与 HUDLayer 共享该数据，避免两边各自判定。
/// </summary>
public sealed record CombatUiIntentDisplayEntry(
    string EnemyId,
    CombatUiIntentIconKind IconKind,
    string Glyph,
    string ColorKey,
    IntentVisibility Visibility,
    string? RevealedMoveName,
    bool IsUnknown,
    bool ShouldPlayRevealTransition,
    CombatUiIntentSlotState SlotState,
    int StableOrder)
{
    public bool IsDimmed => SlotState == CombatUiIntentSlotState.Dimmed;

    public bool IsHidden => SlotState == CombatUiIntentSlotState.Hidden;
}

/// <summary>
/// Combat UI 调参常量，来自 GDD 与 ADR-0011。
/// </summary>
public static class CombatUiFeedbackTuning
{
    public const int StaggerExposureThreshold = 5;
    public const int DamageNumberPoolSize = 12;
    public const int MaxConcurrentDamageNumbers = 6;
    public const int BaseDamageFontSize = 24;
    public const double ResourceBarUpdateDurationSeconds = 0.25;
    public const double DamageFloatDurationSeconds = 1.2;
    public const double StaggerPulseIntervalSeconds = 0.5;
}

/// <summary>
/// 单条资源条展示数据。数值来自战斗快照或事件，UI 仅做填充比例与动效标记。
/// </summary>
public sealed record CombatUiResourceDisplayEntry(
    string CombatantId,
    CombatUiResourceKind Kind,
    int Current,
    int Maximum,
    double FillRatio,
    string ColorKey,
    string VisualKey,
    bool ShouldFlashWhite,
    bool ShouldTweenValue,
    bool IsImmediateValueVisible,
    bool IsExposed,
    double UpdateDurationSeconds);

/// <summary>
/// 伤害浮字样式表。按 GDD F1 查表，不改变伤害金额。
/// </summary>
public sealed record CombatUiDamageNumberStyle(
    CombatUiDamageNumberStyleKind Kind,
    double FontScale,
    string ColorHex,
    bool HasBlackOutline,
    bool UsesMicroBurst)
{
    public static CombatUiDamageNumberStyle ForKind(CombatUiDamageNumberStyleKind kind) => kind switch
    {
        CombatUiDamageNumberStyleKind.Advantage => new(kind, 1.4, "#FFD700", false, true),
        CombatUiDamageNumberStyleKind.Neutral => new(kind, 1.0, "#FFFFFF", false, false),
        CombatUiDamageNumberStyleKind.Disadvantage => new(kind, 0.75, "#AAAAAA", false, false),
        CombatUiDamageNumberStyleKind.Critical => new(kind, 1.3, "#FF6600", false, true),
        CombatUiDamageNumberStyleKind.Decisive => new(kind, 2.0, "#FF8C00", true, true),
        _ => new(CombatUiDamageNumberStyleKind.Neutral, 1.0, "#FFFFFF", false, false)
    };
}

/// <summary>
/// 单个伤害浮字展示数据。Amount 为 Core Combat 已结算结果。
/// </summary>
public sealed record CombatUiDamageNumberDisplayEntry(
    int SequenceId,
    string SourceId,
    string TargetId,
    int Amount,
    CombatUiDamageNumberStyleKind StyleKind,
    double FontScale,
    string ColorHex,
    bool HasBlackOutline,
    bool UsesMicroBurst,
    int LaneIndex,
    float VerticalOffsetPixels,
    double DurationSeconds);

/// <summary>
/// 破绽爆满提示。后续可替换为篆刻印章资产，契约不变。
/// </summary>
public sealed record CombatUiStaggerCueEntry(
    string TargetId,
    int NewStagger,
    bool IsExposed,
    string Glyph,
    string ColorKey,
    bool ShouldPulse,
    double PulseIntervalSeconds);

/// <summary>
/// 伤害展示条目。Amount 为 Core Combat 已结算结果。
/// </summary>
public sealed record CombatUiDamageEntry(
    string SourceId,
    string TargetId,
    int Amount,
    bool IsCrit,
    bool IsCounter);

/// <summary>
/// 破绽展示条目。
/// </summary>
public sealed record CombatUiStaggerEntry(string TargetId, int NewStagger);

/// <summary>
/// 内息展示条目。
/// </summary>
public sealed record CombatUiNeixiEntry(string ActorId, int NewValue);

/// <summary>
/// 战斗 UI 只读快照。Godot Control 层读取它进行渲染，不通过它修改战斗状态。
/// </summary>
public sealed record CombatUiSnapshot(
    CombatUiState State,
    int RoundNumber,
    BattleResult BattleResult,
    IReadOnlyList<CombatUiIntentEntry> IntentEntries,
    IReadOnlyList<CombatUiIntentDisplayEntry> WorldIntentEntries,
    IReadOnlyList<CombatUiIntentDisplayEntry> HudIntentEntries,
    IReadOnlyList<CombatUiDamageEntry> DamageEntries,
    IReadOnlyList<CombatUiStaggerEntry> StaggerEntries,
    IReadOnlyList<CombatUiNeixiEntry> NeixiEntries,
    IReadOnlyList<CombatUiResourceDisplayEntry> ResourceEntries,
    IReadOnlyList<CombatUiDamageNumberDisplayEntry> DamageNumberEntries,
    IReadOnlyList<CombatUiStaggerCueEntry> StaggerCueEntries,
    IReadOnlyList<string> DecisiveStrikeTargets,
    bool IsDirty,
    int RefreshCount);
