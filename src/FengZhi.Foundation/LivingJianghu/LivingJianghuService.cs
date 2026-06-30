namespace FengZhi.Foundation.LivingJianghu;

public sealed class LivingJianghuService
{
    private readonly ILivingJianghuContext _context;
    private readonly ILivingJianghuNpcHandler _npcHandler;
    private readonly ILivingJianghuPresenter _presenter;

    private readonly Dictionary<string, WorldEventConfig> _configs = new();
    private readonly Dictionary<string, EventInstance> _instances = new();
    private readonly List<string> _pendingDelivery = [];

    public int DailyEventCap { get; set; } = 3;
    public int BreathingMultiplier { get; set; } = 2;
    public int MaxTypePerDay { get; set; } = 2;
    public int RumorBaseDelay { get; set; } = 1;
    public float RumorDistanceFactor { get; set; } = 1.0f;
    public bool BreathingBacklogRelease { get; set; } = true;
    public bool ChapterTransitionCleanup { get; set; } = true;

    public LivingJianghuService(
        ILivingJianghuContext context,
        ILivingJianghuNpcHandler npcHandler,
        ILivingJianghuPresenter presenter)
    {
        _context = context;
        _npcHandler = npcHandler;
        _presenter = presenter;
    }

    public void RegisterEvent(WorldEventConfig config)
    {
        _configs[config.Id] = config;
        if (!_instances.ContainsKey(config.Id))
            _instances[config.Id] = new EventInstance(config.Id);
    }

    public void OnDayAdvanced()
    {
        int currentDay = _context.GetCurrentDay();
        bool isBreathing = _context.IsBreathing();

        UpdateEventStates(currentDay);
        ExpireEvents(currentDay);

        var candidates = CollectCandidates(currentDay, isBreathing);
        int effectiveCap = EventSelector.ComputeEffectiveCap(DailyEventCap, BreathingMultiplier, isBreathing);
        string playerRegion = _context.GetPlayerRegion();
        int chapter = _context.GetCurrentChapter();

        var selected = EventSelector.SelectDailyEvents(
            candidates, effectiveCap, MaxTypePerDay, playerRegion, chapter);

        foreach (var instance in selected)
        {
            TriggerEvent(instance, currentDay);
        }

        IncrementBacklogForUnselected(candidates, selected);
    }

    public void OnChapterChanged()
    {
        if (!ChapterTransitionCleanup) return;

        int chapter = _context.GetCurrentChapter();
        foreach (var (id, instance) in _instances)
        {
            if (instance.State != WorldEventState.Pending) continue;
            var config = _configs[id];
            if (chapter < config.ChapterMin || chapter > config.ChapterMax)
            {
                instance.State = WorldEventState.Expired;
                ExecuteActions(config.OnExpire);
            }
        }
    }

    public void OnBreathingStarted()
    {
        if (!BreathingBacklogRelease) return;
        // 积压释放：将高 backlog 的 pending 事件提升优先级（已由 backlog_bonus 自动处理）
    }

    public void DeliverPendingEvents()
    {
        while (_pendingDelivery.Count > 0)
        {
            string id = _pendingDelivery[0];
            _pendingDelivery.RemoveAt(0);

            if (!_instances.TryGetValue(id, out var instance)) continue;
            if (instance.State != WorldEventState.Triggered) continue;
            if (!_configs.TryGetValue(id, out var config)) continue;

            instance.State = WorldEventState.Delivered;
            _presenter.DeliverEvent(config, instance);
            _presenter.PlayWorldPulse();
        }
    }

    public IReadOnlyList<string> GetPendingDeliveryQueue() => _pendingDelivery;

    public EventInstance? GetInstance(string eventId) =>
        _instances.TryGetValue(eventId, out var inst) ? inst : null;

    public WorldEventConfig? GetConfig(string eventId) =>
        _configs.TryGetValue(eventId, out var cfg) ? cfg : null;

    public int ComputeRumorDelay(string? sourceRegion)
    {
        if (sourceRegion == null) return RumorBaseDelay;
        string playerRegion = _context.GetPlayerRegion();
        int distance = _context.GetRegionDistance(sourceRegion, playerRegion);
        return RumorBaseDelay + (int)(distance * RumorDistanceFactor);
    }

    public LivingJianghuSaveData ExportSaveData()
    {
        var data = new LivingJianghuSaveData
        {
            DeliveredQueue = [.._pendingDelivery]
        };

        foreach (var (id, instance) in _instances)
        {
            data.EventStates.Add(new EventInstanceData
            {
                ConfigId = id,
                State = instance.State,
                BacklogDays = instance.BacklogDays,
                TriggerDay = instance.TriggerDay,
                PropagationReadyDay = instance.PropagationReadyDay,
                CooldownUntilDay = instance.CooldownUntilDay
            });
        }

        return data;
    }

    public void LoadSaveData(LivingJianghuSaveData data)
    {
        _pendingDelivery.Clear();
        _pendingDelivery.AddRange(data.DeliveredQueue);

        foreach (var eventData in data.EventStates)
        {
            if (_instances.TryGetValue(eventData.ConfigId, out var instance))
            {
                instance.State = eventData.State;
                instance.BacklogDays = eventData.BacklogDays;
                instance.TriggerDay = eventData.TriggerDay;
                instance.PropagationReadyDay = eventData.PropagationReadyDay;
                instance.CooldownUntilDay = eventData.CooldownUntilDay;
            }
        }
    }

    private void UpdateEventStates(int currentDay)
    {
        foreach (var (id, instance) in _instances)
        {
            if (instance.IsDone) continue;
            var config = _configs[id];

            if (instance.State == WorldEventState.Inactive)
            {
                bool chapterValid = _context.GetCurrentChapter() >= config.ChapterMin
                                    && _context.GetCurrentChapter() <= config.ChapterMax;
                if (!chapterValid) continue;

                if (config.BreathingOnly && !_context.IsBreathing()) continue;

                if (PreconditionEvaluator.EvaluateAll(config.Preconditions, _context))
                {
                    instance.State = WorldEventState.Pending;

                    if (config.Type == WorldEventType.Rumor && config.SourceRegion != null)
                    {
                        int delay = ComputeRumorDelay(config.SourceRegion);
                        instance.PropagationReadyDay = currentDay + delay;
                    }
                }
            }
        }
    }

    private void ExpireEvents(int currentDay)
    {
        foreach (var (id, instance) in _instances)
        {
            if (instance.State != WorldEventState.Pending) continue;
            var config = _configs[id];
            if (config.ExpireDay.HasValue && currentDay >= config.ExpireDay.Value)
            {
                instance.State = WorldEventState.Expired;
                ExecuteActions(config.OnExpire);
            }
        }
    }

    private List<(WorldEventConfig Config, EventInstance Instance)> CollectCandidates(
        int currentDay, bool isBreathing)
    {
        var candidates = new List<(WorldEventConfig, EventInstance)>();

        foreach (var (id, instance) in _instances)
        {
            if (instance.State != WorldEventState.Pending) continue;

            var config = _configs[id];

            if (config.BreathingOnly && !isBreathing) continue;

            if (instance.PropagationReadyDay.HasValue && currentDay < instance.PropagationReadyDay.Value)
                continue;

            if (instance.CooldownUntilDay > currentDay) continue;

            candidates.Add((config, instance));
        }

        return candidates;
    }

    private void TriggerEvent(EventInstance instance, int currentDay)
    {
        instance.State = WorldEventState.Triggered;
        instance.TriggerDay = currentDay;

        var config = _configs[instance.ConfigId];
        ExecuteActions(config.OnTrigger);

        _pendingDelivery.Add(instance.ConfigId);

        if (config.Repeatable && config.Cooldown > 0)
        {
            instance.State = WorldEventState.Inactive;
            instance.CooldownUntilDay = currentDay + config.Cooldown;
            instance.BacklogDays = 0;
        }
    }

    private void IncrementBacklogForUnselected(
        List<(WorldEventConfig Config, EventInstance Instance)> candidates,
        List<EventInstance> selected)
    {
        var selectedSet = new HashSet<string>(selected.Select(s => s.ConfigId));
        foreach (var (_, instance) in candidates)
        {
            if (!selectedSet.Contains(instance.ConfigId))
                instance.BacklogDays = Math.Min(instance.BacklogDays + 1, 5);
        }
    }

    private void ExecuteActions(IReadOnlyList<TriggerAction> actions)
    {
        foreach (var action in actions)
        {
            switch (action.ActionType)
            {
                case TriggerActionType.SetFlag:
                    if (action.FlagToSet != null)
                        _context.SetFlag(action.FlagToSet);
                    break;

                case TriggerActionType.NpcStateChange:
                    if (action.NpcChange != null)
                        _npcHandler.ApplyNpcStateChange(action.NpcChange);
                    break;

                case TriggerActionType.RegisterDelayedEvent:
                    // 注册的新事件在下一日 tick 才生效（无同日连锁）
                    break;
            }
        }
    }
}
