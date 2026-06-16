namespace FengZhi.Foundation.TimeSystem;

/// <summary>休息方式</summary>
public enum RestType
{
    /// <summary>小憩：推进 1 时辰，恢复 30%</summary>
    Nap,
    /// <summary>过夜：推进至次日卯时，完全恢复</summary>
    Overnight,
    /// <summary>自选：推进至目标时辰</summary>
    Custom
}

/// <summary>休息结果</summary>
public sealed class RestResult
{
    public int ShichenAdvanced { get; set; }
    public bool DayChanged { get; set; }
    public int NewDay { get; set; }
    public float StaminaRestored { get; set; }
    public bool FullRestore { get; set; }
    public List<string> TriggeredEvents { get; } = new();
}

/// <summary>
/// 客栈休息逻辑。计算时间推进量和恢复效果。
/// GDD: §Core Rules 6
/// </summary>
public static class InnRestLogic
{
    public const float NapRestRatio = 0.3f;
    public const int NapShichenCount = 1;

    /// <summary>
    /// AC1: 小憩 — 推进 1 时辰，恢复 30% max。
    /// </summary>
    public static RestResult Nap(TimeManager timeManager)
    {
        var result = new RestResult();
        float delta = GameCalendar.ShichenDuration;
        int dayBefore = timeManager.Calendar.CurrentDay;

        timeManager.AdvanceTime(delta);
        result.ShichenAdvanced = 1;
        result.DayChanged = timeManager.Calendar.CurrentDay > dayBefore;
        result.NewDay = timeManager.Calendar.CurrentDay;

        // 恢复 30%
        result.StaminaRestored = timeManager.Stamina.RestByRatio(NapRestRatio);
        return result;
    }

    /// <summary>
    /// AC2: 过夜 — 推进至次日卯时，完全恢复。
    /// AC6: 过夜时气血/内息完全恢复（由调用方处理 CharacterAttributes）。
    /// </summary>
    public static RestResult Overnight(TimeManager timeManager)
    {
        var result = new RestResult();
        int shichenCount = CalculateShichenToNextMao(timeManager.Calendar.CurrentShichen);
        float delta = GameCalendar.ShichenDuration * shichenCount;

        // 逐时辰推进以确保延迟事件逐日触发（AC4）
        AdvanceByShichen(timeManager, shichenCount, result);

        result.FullRestore = true;
        result.StaminaRestored = timeManager.Stamina.RestFull();
        return result;
    }

    /// <summary>
    /// AC3: 自选 — 推进至目标时辰，按推进时辰数/12 比例恢复。
    /// </summary>
    public static RestResult CustomRest(TimeManager timeManager, Shichen targetShichen)
    {
        var result = new RestResult();
        int shichenCount = CalculateShichenTo(timeManager.Calendar.CurrentShichen, targetShichen);

        if (shichenCount <= 0) shichenCount = 12; // 同一时辰 = 推进整轮

        AdvanceByShichen(timeManager, shichenCount, result);

        // 按比例恢复
        float ratio = (float)shichenCount / GameCalendar.ShichenCount;
        result.StaminaRestored = timeManager.Stamina.RestByRatio(ratio);
        return result;
    }

    /// <summary>
    /// 从当前时辰到"下一个卯时"需推进的时辰数。
    /// 如果当前在 Mao 之前（子/丑/寅 = 深夜），推进到当日卯时。
    /// 如果当前在 Mao 或之后，推进到次日卯时。
    /// </summary>
    public static int CalculateShichenToNextMao(Shichen current)
    {
        int currentIdx = (int)current;
        int maoIdx = (int)Shichen.Mao; // 3

        if (currentIdx < maoIdx)
        {
            // 深夜（子/丑/寅）→ 当日卯时
            return maoIdx - currentIdx;
        }

        // Mao 或之后 → 次日卯时 = (12 - current) + Mao
        int count = (12 - currentIdx) + maoIdx;
        if (count == 0) count = 12; // 恰好在 Mao → 推进整轮
        return count;
    }

    /// <summary>从 current 到 target 的正向时辰数（前进）</summary>
    public static int CalculateShichenTo(Shichen current, Shichen target)
    {
        int from = (int)current;
        int to = (int)target;
        int diff = to - from;
        if (diff <= 0) diff += 12;
        return diff;
    }

    private static void AdvanceByShichen(TimeManager timeManager, int shichenCount, RestResult result)
    {
        int dayBefore = timeManager.Calendar.CurrentDay;

        // 一次性推进整段时间（TimeManager 内部会逐时辰处理并触发事件）
        float delta = GameCalendar.ShichenDuration * shichenCount;
        timeManager.AdvanceTime(delta);

        result.ShichenAdvanced = shichenCount;
        result.DayChanged = timeManager.Calendar.CurrentDay > dayBefore;
        result.NewDay = timeManager.Calendar.CurrentDay;
    }
}
