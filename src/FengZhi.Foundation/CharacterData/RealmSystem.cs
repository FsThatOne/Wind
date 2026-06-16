namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 境界突破检查结果。
/// </summary>
public enum BreakthroughResult
{
    /// <summary>无变化（功力未达下一阈值）</summary>
    NoChange,
    /// <summary>功力达标但叙事条件未满足（"蕴积感"提示）</summary>
    PendingNarrative,
    /// <summary>突破成功</summary>
    Triggered
}

/// <summary>
/// 功力境界系统。管理 9 境界映射、突破检查和相对强度比较。
/// GDD: character-attributes.md §功力境界映射, §States and Transitions
/// </summary>
public static class RealmSystem
{
    // ─── 境界定义 ───────────────────────────────────────────

    /// <summary>境界阈值数组（进入该境界所需的最低功力）</summary>
    public static readonly int[] Thresholds = { 25, 35, 50, 70, 90, 115, 140, 170, 200 };

    /// <summary>境界名称（四字）</summary>
    public static readonly string[] RealmNames =
    {
        "初学乍练", // index 0: < 25
        "初窥门径", // index 1: 25-34
        "登堂入室", // index 2: 35-49
        "融会贯通", // index 3: 50-69
        "驾轻就熟", // index 4: 70-89
        "炉火纯青", // index 5: 90-114
        "出神入化", // index 6: 115-139
        "登峰造极", // index 7: 140-169
        "返璞归真"  // index 8: 170-199
        // index 9 (200+): 仍显示"返璞归真"
    };

    // ─── 境界查询 ───────────────────────────────────────────

    /// <summary>
    /// 根据功力总值返回境界索引 (0-8)。
    /// </summary>
    public static int GetRealmIndex(int totalPower)
    {
        int index = 0;
        for (int i = 0; i < Thresholds.Length; i++)
        {
            if (totalPower >= Thresholds[i])
                index = i + 1;
            else
                break;
        }
        // 最大返回 RealmNames.Length - 1
        return Math.Min(index, RealmNames.Length - 1);
    }

    /// <summary>
    /// 根据功力总值返回四字境界名。
    /// </summary>
    public static string GetRealmName(int totalPower)
    {
        return RealmNames[GetRealmIndex(totalPower)];
    }

    // ─── 境界突破检查 ───────────────────────────────────────

    /// <summary>
    /// 检查是否触发境界突破。
    /// </summary>
    /// <param name="currentRealmIndex">当前已确认的境界索引</param>
    /// <param name="totalPower">当前永久属性总功力（不含临时buff）</param>
    /// <param name="narrativeConditionMet">对应叙事前置条件是否满足</param>
    /// <returns>突破结果</returns>
    public static BreakthroughResult CheckBreakthrough(int currentRealmIndex, int totalPower, bool narrativeConditionMet)
    {
        int potentialIndex = GetRealmIndex(totalPower);

        // 功力未达下一境界
        if (potentialIndex <= currentRealmIndex)
            return BreakthroughResult.NoChange;

        // 功力达标，检查叙事条件
        if (!narrativeConditionMet)
            return BreakthroughResult.PendingNarrative;

        return BreakthroughResult.Triggered;
    }

    // ─── 相对强度比较 ───────────────────────────────────────

    /// <summary>
    /// 比较两个角色的功力，返回 PowerLevel 和文学描述。
    /// </summary>
    public static PowerComparisonResult ComparePower(int selfPower, int targetPower)
    {
        var level = FormulaEngine.ComparePower(selfPower, targetPower);
        string description = level switch
        {
            PowerLevel.FarWeaker => "此人气势如渊，你感到难以抗衡",
            PowerLevel.Weaker => "此人内力深厚，不可小觑",
            PowerLevel.Comparable => "你与此人旗鼓相当",
            PowerLevel.Stronger => "此人功力尚浅",
            PowerLevel.FarStronger => "此人不足为惧",
            _ => ""
        };
        return new PowerComparisonResult(level, description);
    }
}

/// <summary>
/// 功力比较结果，包含等级和文学描述。
/// </summary>
public readonly record struct PowerComparisonResult(PowerLevel Level, string Description);
