namespace FengZhi.Foundation.Combat.Xingqi;

/// <summary>
/// 行气系统参数配置（GDD combat-system.md F1/F2）。
/// </summary>
public sealed class XingqiConfig
{
    public int BaseXingqiGain { get; init; } = 20;
    public int XingqiThreshold { get; init; } = 100;
    public int ChapterBaselineAgility { get; init; } = 10;
    public float AgilityFactorMin { get; init; } = 0.75f;
    public float AgilityFactorMax { get; init; } = 1.35f;
    public float QinggongMultiplierMin { get; init; } = 0.85f;
    public float QinggongMultiplierMax { get; init; } = 1.30f;
    public float RetainedXingqiCap { get; init; } = 0.30f;
}
