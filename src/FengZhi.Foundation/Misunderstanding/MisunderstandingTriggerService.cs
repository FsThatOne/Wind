using System;
using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会触发服务 — 统一入口：世界事件、对话选择、缺席检测。</summary>
public sealed class MisunderstandingTriggerService
{
    private readonly IMisunderstandingRegistry _registry;
    private readonly MisunderstandingStateMachine _sm;
    private readonly MisunderstandingModCalculator _modCalc;
    private readonly AbsenceDetector _absenceDetector;
    private readonly MisunderstandingConfig _config;
    private readonly Dictionary<string, TriggerConfig> _worldEventTriggers = new();
    private int _nextInstanceId;

    public MisunderstandingTriggerService(
        IMisunderstandingRegistry registry,
        MisunderstandingStateMachine sm,
        MisunderstandingModCalculator modCalc,
        AbsenceDetector absenceDetector,
        MisunderstandingConfig config)
    {
        _registry = registry;
        _sm = sm;
        _modCalc = modCalc;
        _absenceDetector = absenceDetector;
        _config = config;
    }

    /// <summary>注册世界事件触发配置。</summary>
    public void RegisterWorldEventTrigger(TriggerConfig triggerConfig)
    {
        if (string.IsNullOrEmpty(triggerConfig.MatchEventId))
            throw new ArgumentException("世界事件 trigger 必须指定 MatchEventId");
        _worldEventTriggers[triggerConfig.MatchEventId] = triggerConfig;
    }

    /// <summary>世界事件发生时调用 — 匹配则创建误会实例。</summary>
    public MisunderstandingInstance? OnWorldEvent(string eventId, int currentChapter, int currentDay)
    {
        if (!_worldEventTriggers.TryGetValue(eventId, out var trigger))
            return null;

        return CreateInstance(trigger, currentChapter, currentDay);
    }

    /// <summary>对话选择触发 — 可同时对多个 NPC 创建误会。</summary>
    public List<MisunderstandingInstance> OnDialogueChoice(
        List<DialogueMisTrigger> triggers, int currentChapter, int currentDay)
    {
        var created = new List<MisunderstandingInstance>();
        foreach (var t in triggers)
        {
            var config = new TriggerConfig
            {
                TriggerId = $"dialogue_{t.NpcId}_{currentDay}",
                TargetNpc = t.NpcId,
                SourceType = SourceType.DialogueChoice,
                Severity = t.Severity,
            };
            var inst = CreateInstance(config, currentChapter, currentDay);
            if (inst != null)
                created.Add(inst);
        }
        return created;
    }

    /// <summary>每日 tick — 执行缺席检测并创建缺席误解。</summary>
    public List<MisunderstandingInstance> OnDayAdvancedAbsence(
        IEnumerable<string> trackedNpcs, int currentChapter, int currentDay)
    {
        var absentNpcs = _absenceDetector.OnDayAdvanced(trackedNpcs);
        var created = new List<MisunderstandingInstance>();

        foreach (var npcId in absentNpcs)
        {
            var config = new TriggerConfig
            {
                TriggerId = $"absence_{npcId}_{currentDay}",
                TargetNpc = npcId,
                SourceType = SourceType.Absence,
                Severity = Severity.Minor,
            };
            var inst = CreateInstance(config, currentChapter, currentDay);
            if (inst != null)
                created.Add(inst);
        }

        return created;
    }

    private MisunderstandingInstance? CreateInstance(TriggerConfig trigger, int chapter, int day)
    {
        int window = trigger.WindowOverride ?? GetDefaultWindow(trigger.Severity);
        var inst = new MisunderstandingInstance
        {
            Id = $"mis_{_nextInstanceId++:D4}",
            TargetNpc = trigger.TargetNpc,
            SourceType = trigger.SourceType,
            Severity = trigger.Severity,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = chapter,
            CreatedDay = day,
            UnlockFlag = trigger.UnlockFlag,
        };

        inst.State = MisunderstandingState.Active;
        if (!_registry.TryRegister(inst))
            return null;

        _modCalc.RecomputeAndWrite(trigger.TargetNpc);
        return inst;
    }

    private int GetDefaultWindow(Severity severity) => severity switch
    {
        Severity.Minor => _config.MinorWindowDays,
        Severity.Moderate => _config.ModerateWindowDays,
        Severity.Severe => _config.SevereCountdownDays,
        _ => _config.MinorWindowDays,
    };
}

/// <summary>对话误会触发标记。</summary>
public sealed class DialogueMisTrigger
{
    public required string NpcId { get; init; }
    public required Severity Severity { get; init; }
}
