using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 体系颜色主题。刚系暖赤金、柔系玄青冷色、巧系烟灰中性。
/// 颜色只体现体系，不体现稀有度。
/// </summary>
public enum TypeColorTheme
{
    /// <summary>刚系：暖赤金</summary>
    WarmGold,
    /// <summary>柔系：玄青冷色</summary>
    CoolCyan,
    /// <summary>巧系：烟灰中性</summary>
    NeutralGray
}

/// <summary>
/// 招式进度显示模式。
/// </summary>
public enum ProgressDisplayMode
{
    /// <summary>普通武学：只显示"已习得"</summary>
    Learned,
    /// <summary>高级/绝学：显示层级进度条</summary>
    ProgressBar,
    /// <summary>批注版：独特质感标记</summary>
    Annotated
}

/// <summary>
/// 招式层级显示标签（仅 ProgressBar 模式使用）。
/// </summary>
public enum ProgressTierLabel
{
    None,
    Fragment,     // 残卷
    Manuscript,   // 拓本
    Complete,     // 完本
    Mastered      // 真传
}

/// <summary>
/// 招式卡片呈现数据。UI 层直接消费此 DTO。
/// </summary>
public sealed class MoveCardDisplayData
{
    public string MoveId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MoveType Type { get; init; }
    public TypeColorTheme ColorTheme { get; init; }
    public int NeixiCost { get; init; }
    public float BaseMultiplier { get; init; }
    public float EffectiveMultiplier { get; init; }
    public ProgressDisplayMode DisplayMode { get; init; }
    public ProgressTierLabel TierLabel { get; init; }
    public float Completion { get; init; }
    public bool IsAnnotated { get; init; }

    /// <summary>触发条件（直接可见，不折叠）。</summary>
    public IReadOnlyList<string> TriggerConditions { get; init; } = Array.Empty<string>();

    /// <summary>特殊效果（直接可见，不折叠）。</summary>
    public IReadOnlyList<string> SpecialEffects { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 战斗面板招式来源标记。
/// </summary>
public enum MoveSource
{
    /// <summary>基础装备槽</summary>
    BaseSlot,
    /// <summary>心法专属招式</summary>
    XinfaExclusive
}

/// <summary>
/// 战斗面板中的招式条目。
/// </summary>
public sealed class BattlePanelMoveEntry
{
    public string MoveId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MoveSource Source { get; init; }
    public TypeColorTheme ColorTheme { get; init; }
    public int NeixiCost { get; init; }
    public float EffectiveMultiplier { get; init; }
    public IReadOnlyList<string> TriggerConditions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SpecialEffects { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 战斗招式面板呈现数据。
/// </summary>
public sealed class BattlePanelDisplayData
{
    public IReadOnlyList<BattlePanelMoveEntry> Entries { get; init; } = Array.Empty<BattlePanelMoveEntry>();
    public int BaseSlotCount => Entries.Count(e => e.Source == MoveSource.BaseSlot);
    public int XinfaExclusiveCount => Entries.Count(e => e.Source == MoveSource.XinfaExclusive);
}

/// <summary>
/// 武学 UI 数据组装服务。将底层数据转化为符合呈现规则的 DTO。
/// </summary>
public sealed class MartialArtsUIService
{
    private readonly IDataTable<MoveDefinition> _moveTable;
    private readonly MoveProgressionService _progressionService;
    private readonly XinfaService _xinfaService;

    public MartialArtsUIService(
        IDataTable<MoveDefinition> moveTable,
        MoveProgressionService progressionService,
        XinfaService xinfaService)
    {
        _moveTable = moveTable ?? throw new ArgumentNullException(nameof(moveTable));
        _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        _xinfaService = xinfaService ?? throw new ArgumentNullException(nameof(xinfaService));
    }

    /// <summary>
    /// 获取招式卡片呈现数据。
    /// </summary>
    public MoveCardDisplayData? GetMoveCardDisplay(string moveId)
    {
        var def = _moveTable.Get(moveId);
        if (def == null) return null;

        var entry = _progressionService.GetEntry(moveId);
        var (displayMode, tierLabel, completion, isAnnotated) = ResolveProgressDisplay(def, entry);
        var effectiveMult = def.BaseMultiplier;
        if (isAnnotated && def.Annotation != null)
            effectiveMult *= def.Annotation.AnnotationMult;

        return new MoveCardDisplayData
        {
            MoveId = def.Id,
            Name = def.Name,
            Type = def.Type,
            ColorTheme = GetColorTheme(def.Type),
            NeixiCost = def.NeixiCost,
            BaseMultiplier = def.BaseMultiplier,
            EffectiveMultiplier = effectiveMult,
            DisplayMode = displayMode,
            TierLabel = tierLabel,
            Completion = completion,
            IsAnnotated = isAnnotated,
            TriggerConditions = def.TriggerConditions,
            SpecialEffects = def.SpecialEffects
        };
    }

    /// <summary>
    /// 组装战斗招式面板数据。6 基础 + 心法专属。
    /// </summary>
    public BattlePanelDisplayData GetBattlePanelDisplay(CharacterLoadout loadout)
    {
        var entries = new List<BattlePanelMoveEntry>();

        // 基础槽位招式
        foreach (var moveId in loadout.GetEquippedMoves())
        {
            var def = _moveTable.Get(moveId);
            if (def == null) continue;
            entries.Add(BuildPanelEntry(def, MoveSource.BaseSlot));
        }

        // 心法专属招式
        var combatSet = _xinfaService.GetCombatMoveSet(loadout);
        foreach (var moveId in combatSet.XinfaExclusiveMoves)
        {
            var def = _moveTable.Get(moveId);
            if (def == null) continue;
            entries.Add(BuildPanelEntry(def, MoveSource.XinfaExclusive));
        }

        return new BattlePanelDisplayData { Entries = entries };
    }

    /// <summary>
    /// 体系 → 颜色主题映射。颜色只体现体系，不体现稀有度。
    /// </summary>
    public static TypeColorTheme GetColorTheme(MoveType type) => type switch
    {
        MoveType.Gang => TypeColorTheme.WarmGold,
        MoveType.Rou => TypeColorTheme.CoolCyan,
        MoveType.Qiao => TypeColorTheme.NeutralGray,
        _ => TypeColorTheme.NeutralGray
    };

    private static (ProgressDisplayMode mode, ProgressTierLabel tier, float completion, bool isAnnotated)
        ResolveProgressDisplay(MoveDefinition def, MoveProgressionEntry? entry)
    {
        if (entry == null)
            return (ProgressDisplayMode.Learned, ProgressTierLabel.None, 0f, false);

        if (entry.Stage == MoveProgressionStage.Annotated)
            return (ProgressDisplayMode.Annotated, ProgressTierLabel.None, 1.0f, true);

        if (def.Category == MoveCategory.Basic)
            return (ProgressDisplayMode.Learned, ProgressTierLabel.None, entry.Completion, false);

        // 高级/绝学：显示进度条
        var tierLabel = entry.Stage switch
        {
            MoveProgressionStage.Fragment => ProgressTierLabel.Fragment,
            MoveProgressionStage.Manuscript => ProgressTierLabel.Manuscript,
            MoveProgressionStage.Complete => ProgressTierLabel.Complete,
            MoveProgressionStage.Mastered => ProgressTierLabel.Mastered,
            _ => ProgressTierLabel.None
        };

        return (ProgressDisplayMode.ProgressBar, tierLabel, entry.Completion, false);
    }

    private BattlePanelMoveEntry BuildPanelEntry(MoveDefinition def, MoveSource source)
    {
        var entry = _progressionService.GetEntry(def.Id);
        var effectiveMult = def.BaseMultiplier;
        if (entry?.Stage == MoveProgressionStage.Annotated && def.Annotation != null)
            effectiveMult *= def.Annotation.AnnotationMult;

        return new BattlePanelMoveEntry
        {
            MoveId = def.Id,
            Name = def.Name,
            Source = source,
            ColorTheme = GetColorTheme(def.Type),
            NeixiCost = def.NeixiCost,
            EffectiveMultiplier = effectiveMult,
            TriggerConditions = def.TriggerConditions,
            SpecialEffects = def.SpecialEffects
        };
    }
}
