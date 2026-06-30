using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会解除器 — 条件检查 + RESOLVED 转换 + 弹回加成。</summary>
public sealed class MisunderstandingResolver
{
    private readonly IMisunderstandingRegistry _registry;
    private readonly MisunderstandingStateMachine _sm;
    private readonly MisunderstandingModCalculator _modCalc;
    private readonly IConditionEvaluator _evaluator;
    private readonly ResolutionBonusTracker _bonusTracker;
    private readonly HashSet<string> _permanentUnlockFlags = new();

    public MisunderstandingResolver(
        IMisunderstandingRegistry registry,
        MisunderstandingStateMachine sm,
        MisunderstandingModCalculator modCalc,
        IConditionEvaluator evaluator,
        ResolutionBonusTracker bonusTracker)
    {
        _registry = registry;
        _sm = sm;
        _modCalc = modCalc;
        _evaluator = evaluator;
        _bonusTracker = bonusTracker;
    }

    /// <summary>注册剧情解锁标记，允许 PERMANENT 误会被解除。</summary>
    public void RegisterPermanentUnlock(string flagId) => _permanentUnlockFlags.Add(flagId);

    /// <summary>尝试解除指定 NPC 的所有可解除误会。返回被解除的实例列表。</summary>
    public List<MisunderstandingInstance> TryResolveAll(string npcId)
    {
        var resolved = new List<MisunderstandingInstance>();
        var all = _registry.GetAll(npcId);

        foreach (var inst in all)
        {
            if (TryResolveInstance(inst))
                resolved.Add(inst);
        }

        if (resolved.Count > 0)
        {
            _modCalc.RecomputeAndWrite(npcId);
            _bonusTracker.ApplyBonus(npcId);
        }

        return resolved;
    }

    /// <summary>尝试解除单个误会实例。</summary>
    public bool TryResolveSingle(string npcId, string instanceId)
    {
        var all = _registry.GetAll(npcId);
        foreach (var inst in all)
        {
            if (inst.Id != instanceId) continue;
            if (!TryResolveInstance(inst)) return false;
            _modCalc.RecomputeAndWrite(npcId);
            _bonusTracker.ApplyBonus(npcId);
            return true;
        }
        return false;
    }

    private bool TryResolveInstance(MisunderstandingInstance inst)
    {
        switch (inst.State)
        {
            case MisunderstandingState.Active:
            case MisunderstandingState.Escalated:
                break;
            case MisunderstandingState.Permanent:
                if (!_permanentUnlockFlags.Contains(inst.UnlockFlag ?? ""))
                    return false;
                break;
            default:
                return false;
        }

        if (inst.ResolutionConditions.Count == 0)
            return false;

        if (!_evaluator.AreConditionsMet(inst.ResolutionConditions))
            return false;

        _sm.Resolve(inst);
        return true;
    }
}
