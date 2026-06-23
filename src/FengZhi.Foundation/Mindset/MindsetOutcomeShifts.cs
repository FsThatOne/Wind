namespace FengZhi.Foundation.Mindset;

/// <summary>
/// 战后玩家对败者的处置选择。
///
/// 取自 dev-story spec `2026-06-23-s7-vs-outcome-feedback.md` §3，
/// 是 outcome scene UI 与 Foundation Mindset 之间的 narrative-level 契约枚举。
/// </summary>
public enum MindsetOutcomeChoice
{
    None,
    /// <summary>放过：仁慈对待，主动收手。</summary>
    Spare,
    /// <summary>重伤：杀气未消，下死手。</summary>
    Defeat,
}

/// <summary>
/// 战后心境位移映射表（spec §3 数值的唯一权威实现）。
///
/// 数值规约（owner 2026-06-23 sign-off，对应 GDD §来源 A 关键位移 ±5-10 + 日常位移 ±2 联动）：
/// - Spare  → Morality +5（关键善行）+ Resolve +2（放下一寸）
/// - Defeat → Morality -5（关键恶行）+ Resolve -2（执念加深）
/// - Worldly：江南章节单次小贼遭遇不足以影响"入世↔出世"，本表不动
///
/// 抽到 Foundation 是为了让 integration tests 直接验证 spec §3 数值表，
/// 不依赖 Godot Autoload。Sprint 8 扩到更多 outcome 类型时（如"擒下→自首"
/// 等）在本表追加 case 即可。
/// </summary>
public static class MindsetOutcomeShifts
{
    public static IReadOnlyList<MindsetShift> ResolveShifts(MindsetOutcomeChoice choice)
    {
        return choice switch
        {
            MindsetOutcomeChoice.Spare => new[]
            {
                new MindsetShift(MindsetAxis.Morality, +5),
                new MindsetShift(MindsetAxis.Resolve, +2),
            },
            MindsetOutcomeChoice.Defeat => new[]
            {
                new MindsetShift(MindsetAxis.Morality, -5),
                new MindsetShift(MindsetAxis.Resolve, -2),
            },
            _ => Array.Empty<MindsetShift>(),
        };
    }
}
