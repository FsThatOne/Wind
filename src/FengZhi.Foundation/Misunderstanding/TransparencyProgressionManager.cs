using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>透明度升级管理器 — HIDDEN→HINTED→PERCEIVED→URGENT 单向递进。</summary>
public sealed class TransparencyProgressionManager
{
    private readonly IMisunderstandingRegistry _registry;
    private readonly ITransparencySignalEmitter _emitter;
    private readonly MisunderstandingConfig _config;
    private readonly HashSet<string> _hintEmitted = new();
    private readonly HashSet<string> _perceivedEmitted = new();
    private readonly HashSet<string> _urgentEmitted = new();

    public TransparencyProgressionManager(
        IMisunderstandingRegistry registry,
        ITransparencySignalEmitter emitter,
        MisunderstandingConfig config)
    {
        _registry = registry;
        _emitter = emitter;
        _config = config;
    }

    /// <summary>每日 tick 中检查所有活跃误会的透明度升级。</summary>
    public void CheckAll(IEnumerable<string> npcIds, int currentDay)
    {
        foreach (var npcId in npcIds)
        {
            var all = _registry.GetAll(npcId);
            foreach (var inst in all)
                CheckInstance(inst, npcId, currentDay);
        }
    }

    /// <summary>玩家与 NPC 交互时，HIDDEN 可提前升级为 HINTED。</summary>
    public void OnPlayerInteraction(string npcId)
    {
        var all = _registry.GetAll(npcId);
        foreach (var inst in all)
        {
            if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated))
                continue;
            if (inst.Transparency == Transparency.Hidden)
            {
                inst.Transparency = Transparency.Hinted;
                EmitHintOnce(npcId, inst.Id);
            }
        }
    }

    private void CheckInstance(MisunderstandingInstance inst, string npcId, int currentDay)
    {
        if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated))
            return;

        int daysElapsed = currentDay - inst.CreatedDay;

        if (inst.Transparency == Transparency.Hidden && daysElapsed >= _config.HiddenDurationDays)
        {
            inst.Transparency = Transparency.Hinted;
            EmitHintOnce(npcId, inst.Id);
        }

        if (inst.Transparency == Transparency.Hinted)
        {
            int halfWindow = (int)(inst.InitialWindow * _config.PerceivedThresholdRatio);
            if (daysElapsed >= halfWindow)
            {
                inst.Transparency = Transparency.Perceived;
                EmitPerceivedOnce(npcId, inst.Id);
            }
        }

        if (inst.Transparency == Transparency.Perceived && inst.WindowRemaining <= 3)
        {
            inst.Transparency = Transparency.Urgent;
            EmitUrgentOnce(npcId, inst.Id);
        }
    }

    private void EmitHintOnce(string npcId, string instId)
    {
        if (_hintEmitted.Add(instId))
            _emitter.EmitHint(npcId, instId);
    }

    private void EmitPerceivedOnce(string npcId, string instId)
    {
        if (_perceivedEmitted.Add(instId))
            _emitter.EmitPerceived(npcId, instId);
    }

    private void EmitUrgentOnce(string npcId, string instId)
    {
        if (_urgentEmitted.Add(instId))
            _emitter.EmitUrgent(npcId, instId);
    }
}
