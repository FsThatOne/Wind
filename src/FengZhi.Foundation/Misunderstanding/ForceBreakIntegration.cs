using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>诀别集成 — SEVERE 归零时调用 ForceBreak，无视地板保护。</summary>
public sealed class ForceBreakIntegration : IForceBreakHandler
{
    private readonly IRomanceService _romance;
    private readonly IMisunderstandingRegistry _registry;
    private readonly MisunderstandingStateMachine _sm;

    public ForceBreakIntegration(
        IRomanceService romance,
        IMisunderstandingRegistry registry,
        MisunderstandingStateMachine sm)
    {
        _romance = romance;
        _registry = registry;
        _sm = sm;
    }

    /// <summary>执行诀别 — 幂等，已处于 M_BREAK 时不重复调用。</summary>
    public void ForceBreak(string npcId)
    {
        if (_romance.IsBrokenState(npcId))
            return;

        _romance.ForceBreak(npcId);

        var all = _registry.GetAll(npcId);
        foreach (var inst in all)
        {
            if (inst.State is MisunderstandingState.Active or MisunderstandingState.Escalated)
                _sm.Break(inst);
        }
    }
}
