namespace FengZhi.Foundation.Combat.Xingqi;

/// <summary>
/// 行气增长纯函数（GDD combat-system.md F1）。
/// </summary>
public static class XingqiFormulaEngine
{
    /// <summary>
    /// 计算单角色单脉冲的行气增长量。
    /// </summary>
    public static int ComputeGain(
        int baseGain,
        int agility,
        int chapterBaselineAgility,
        float agilityFactorMin,
        float agilityFactorMax,
        float qinggongXingqiBonus = 0f,
        float qinggongMultMin = 0.85f,
        float qinggongMultMax = 1.30f,
        float statusMultiplier = 1.0f)
    {
        float agilityFactor = Math.Clamp(
            (float)agility / chapterBaselineAgility,
            agilityFactorMin,
            agilityFactorMax);

        float qinggongMult = Math.Clamp(
            1.0f + qinggongXingqiBonus,
            qinggongMultMin,
            qinggongMultMax);

        float raw = baseGain * agilityFactor * qinggongMult * statusMultiplier;
        return Math.Max(1, (int)Math.Round(raw));
    }

    /// <summary>
    /// 使用 XingqiConfig 的便捷重载。
    /// </summary>
    public static int ComputeGain(XingqiConfig config, int agility,
        float qinggongBonus = 0f, float statusMult = 1.0f)
    {
        return ComputeGain(
            config.BaseXingqiGain,
            agility,
            config.ChapterBaselineAgility,
            config.AgilityFactorMin,
            config.AgilityFactorMax,
            qinggongBonus,
            config.QinggongMultiplierMin,
            config.QinggongMultiplierMax,
            statusMult);
    }
}
