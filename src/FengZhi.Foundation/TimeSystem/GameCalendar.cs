namespace FengZhi.Foundation.TimeSystem;

/// <summary>12 时辰枚举（子丑寅卯辰巳午未申酉戌亥）</summary>
public enum Shichen
{
    Zi = 0,   // 子 23:00-01:00
    Chou = 1, // 丑 01:00-03:00
    Yin = 2,  // 寅 03:00-05:00
    Mao = 3,  // 卯 05:00-07:00
    Chen = 4, // 辰 07:00-09:00
    Si = 5,   // 巳 09:00-11:00
    Wu = 6,   // 午 11:00-13:00
    Wei = 7,  // 未 13:00-15:00
    Shen = 8, // 申 15:00-17:00
    You = 9,  // 酉 17:00-19:00
    Xu = 10,  // 戌 19:00-21:00
    Hai = 11  // 亥 21:00-23:00
}

/// <summary>光照分类</summary>
public enum LightPhase { Dawn, Day, Dusk, Night }

/// <summary>四季</summary>
public enum Season { Spring = 0, Summer = 1, Autumn = 2, Winter = 3 }

/// <summary>
/// 12 时辰日历核心。纯状态+纯函数，无 EventBus 依赖。
/// </summary>
public sealed class GameCalendar
{
    public const int ShichenCount = 12;
    public const float ShichenDuration = 1f / ShichenCount; // ≈0.0833 天

    /// <summary>当前游戏日（从 1 开始）</summary>
    public int CurrentDay { get; private set; } = 1;

    /// <summary>当前时辰</summary>
    public Shichen CurrentShichen { get; private set; } = Shichen.Mao;

    /// <summary>当前时辰内的进度（0 ~ ShichenDuration）</summary>
    public float DayProgress { get; private set; } = 0f;

    /// <summary>当前季节</summary>
    public Season CurrentSeason { get; private set; } = Season.Spring;

    /// <summary>当前季节内的第几天</summary>
    public int SeasonDay { get; private set; } = 1;

    /// <summary>每季天数（可配置）</summary>
    public int DaysPerSeason { get; set; } = 30;

    /// <summary>当前光照分类</summary>
    public LightPhase CurrentLightPhase => GetLightPhase(CurrentShichen);

    /// <summary>
    /// 推进日历时间。返回推进过程中触发的事件记录。
    /// </summary>
    private const float Epsilon = 1e-6f;

    public CalendarAdvanceResult AdvanceProgress(float timeDelta)
    {
        var result = new CalendarAdvanceResult();
        DayProgress += timeDelta;

        while (DayProgress >= ShichenDuration - Epsilon)
        {
            DayProgress -= ShichenDuration;
            AdvanceShichen(result);
        }

        // 浮点精度修正
        if (DayProgress < 0) DayProgress = 0;

        return result;
    }

    /// <summary>F4: 根据游戏日计算季节</summary>
    public static Season GetSeasonForDay(int day, int daysPerSeason)
    {
        if (day < 1) return Season.Spring;
        int index = ((day - 1) / daysPerSeason) % 4;
        return (Season)index;
    }

    /// <summary>时辰 → 光照分类映射</summary>
    public static LightPhase GetLightPhase(Shichen shichen) => shichen switch
    {
        Shichen.Yin or Shichen.Mao => LightPhase.Dawn,
        Shichen.Chen or Shichen.Si or Shichen.Wu or Shichen.Wei or Shichen.Shen => LightPhase.Day,
        Shichen.You or Shichen.Xu => LightPhase.Dusk,
        _ => LightPhase.Night // 子丑亥
    };

    /// <summary>重置为安全默认值</summary>
    public void ResetToDefaults()
    {
        CurrentDay = 1;
        CurrentShichen = Shichen.Mao;
        DayProgress = 0f;
        CurrentSeason = Season.Spring;
        SeasonDay = 1;
    }

    /// <summary>设置状态（存档加载用）</summary>
    public void SetState(int day, Shichen shichen, float dayProgress, int daysPerSeason)
    {
        if (day < 1 || (int)shichen < 0 || (int)shichen > 11)
        {
            ResetToDefaults();
            return;
        }

        CurrentDay = day;
        CurrentShichen = shichen;
        DayProgress = Math.Clamp(dayProgress, 0f, ShichenDuration);
        DaysPerSeason = daysPerSeason;
        CurrentSeason = GetSeasonForDay(day, daysPerSeason);
        SeasonDay = ((day - 1) % daysPerSeason) + 1;
    }

    // ─── 私有 ───────────────────────────────────────────────

    private void AdvanceShichen(CalendarAdvanceResult result)
    {
        var oldShichen = CurrentShichen;
        int next = ((int)CurrentShichen + 1) % ShichenCount;
        CurrentShichen = (Shichen)next;
        result.ShichenChanges.Add((oldShichen, CurrentShichen));

        // 从亥(11)回到子(0)意味着日切换
        if (next == 0)
        {
            AdvanceDay(result);
        }
    }

    private void AdvanceDay(CalendarAdvanceResult result)
    {
        CurrentDay++;
        SeasonDay++;
        result.DayAdvanced = true;
        result.NewDay = CurrentDay;

        if (SeasonDay > DaysPerSeason)
        {
            SeasonDay = 1;
            var oldSeason = CurrentSeason;
            CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
            result.SeasonChanged = true;
            result.OldSeason = oldSeason;
            result.NewSeason = CurrentSeason;
        }
    }
}

/// <summary>日历推进结果（记录过程中发生的事件）</summary>
public sealed class CalendarAdvanceResult
{
    public List<(Shichen From, Shichen To)> ShichenChanges { get; } = new();
    public bool DayAdvanced { get; set; }
    public int NewDay { get; set; }
    public bool SeasonChanged { get; set; }
    public Season OldSeason { get; set; }
    public Season NewSeason { get; set; }
}
