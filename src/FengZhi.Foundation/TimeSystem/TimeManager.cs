using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.TimeSystem;

// ─── 时间事件定义 ─────────────────────────────────────────────

public sealed record ShichenChangedEvent(Shichen OldShichen, Shichen NewShichen) : GameEvent;
public sealed record DayAdvancedEvent(int NewDay) : GameEvent;
public sealed record SeasonChangedEvent(Season OldSeason, Season NewSeason) : GameEvent;
public sealed record StaminaChangedEvent(float OldStamina, float NewStamina, StaminaState NewState) : GameEvent;

/// <summary>
/// 时间系统统一入口。组装 GameCalendar + StaminaSystem + DelayedEventScheduler。
/// 通过 EventBus 发布时间事件。
/// </summary>
public sealed class TimeManager
{
    private readonly IEventBus _eventBus;
    private bool _isAdvancing = false;

    public GameCalendar Calendar { get; }
    public StaminaSystem Stamina { get; }
    public DelayedEventScheduler Scheduler { get; }

    public TimeManager(IEventBus eventBus, int constitution = 10, int daysPerSeason = 30)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Calendar = new GameCalendar { DaysPerSeason = daysPerSeason };
        Stamina = new StaminaSystem(constitution);
        Scheduler = new DelayedEventScheduler();
    }

    public TimeManager(IEventBus eventBus, GameCalendar calendar, StaminaSystem stamina, DelayedEventScheduler scheduler)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        Stamina = stamina ?? throw new ArgumentNullException(nameof(stamina));
        Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    /// <summary>
    /// AC1: 推进时间。返回是否成功。
    /// AC6: 推进中再调用返回 false（防递归）。
    /// </summary>
    public bool AdvanceTime(float timeDelta)
    {
        if (_isAdvancing) return false; // AC6
        if (timeDelta <= 0) return false;

        _isAdvancing = true;
        try
        {
            var result = Calendar.AdvanceProgress(timeDelta);

            // AC2: 时辰切换事件
            foreach (var (from, to) in result.ShichenChanges)
            {
                _eventBus.Publish(new ShichenChangedEvent(from, to));
            }

            // AC3: 日切换 → 发布事件 + 触发延迟事件
            if (result.DayAdvanced)
            {
                _eventBus.Publish(new DayAdvancedEvent(result.NewDay));
                Scheduler.ProcessDay(result.NewDay);
            }

            // AC4: 季节切换事件
            if (result.SeasonChanged)
            {
                _eventBus.Publish(new SeasonChangedEvent(result.OldSeason, result.NewSeason));
            }

            return true;
        }
        finally
        {
            _isAdvancing = false;
        }
    }

    /// <summary>
    /// AC5: 消耗体力并发布状态变更事件。
    /// 返回实际消耗量。
    /// </summary>
    public float ConsumeStamina(float amount)
    {
        float oldStamina = Stamina.CurrentStamina;
        float consumed = Stamina.ConsumeStamina(amount);

        if (consumed > 0)
        {
            _eventBus.Publish(new StaminaChangedEvent(oldStamina, Stamina.CurrentStamina, Stamina.CurrentState));
        }

        return consumed;
    }

    /// <summary>恢复体力并发布事件</summary>
    public float RestoreStamina(float amount)
    {
        float oldStamina = Stamina.CurrentStamina;
        float restored = Stamina.RestoreStamina(amount);

        if (restored > 0)
        {
            _eventBus.Publish(new StaminaChangedEvent(oldStamina, Stamina.CurrentStamina, Stamina.CurrentState));
        }

        return restored;
    }

    /// <summary>是否正在推进（防递归检查用）</summary>
    public bool IsAdvancing => _isAdvancing;
}
