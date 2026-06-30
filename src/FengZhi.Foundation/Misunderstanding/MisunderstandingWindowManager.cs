using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会窗口管理器 — 每日递减 + PERMANENT/BROKEN 转换 + 存档补算。</summary>
public sealed class MisunderstandingWindowManager
{
    private readonly IMisunderstandingRegistry _registry;
    private readonly MisunderstandingStateMachine _sm;
    private readonly MisunderstandingModCalculator _modCalc;
    private readonly IForceBreakHandler _forceBreakHandler;

    public MisunderstandingWindowManager(
        IMisunderstandingRegistry registry,
        MisunderstandingStateMachine sm,
        MisunderstandingModCalculator modCalc,
        IForceBreakHandler forceBreakHandler)
    {
        _registry = registry;
        _sm = sm;
        _modCalc = modCalc;
        _forceBreakHandler = forceBreakHandler;
    }

    /// <summary>每日 tick — 递减所有活跃误会窗口并处理归零逻辑。</summary>
    public void OnDayAdvanced(IEnumerable<string> npcIds)
    {
        foreach (var npcId in npcIds)
            TickNpc(npcId);
    }

    /// <summary>存档补算 — 逐天执行，确保中间天的状态转换不被跳过。</summary>
    public void CatchUpDays(IEnumerable<string> npcIds, int daysMissed)
    {
        for (int i = 0; i < daysMissed; i++)
            OnDayAdvanced(npcIds);
    }

    private void TickNpc(string npcId)
    {
        var all = _registry.GetAll(npcId);
        bool modDirty = false;

        foreach (var inst in all)
        {
            if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated))
                continue;
            if (inst.WindowRemaining <= 0)
                continue;

            inst.WindowRemaining--;

            if (inst.WindowRemaining == 0)
            {
                if (inst.Severity == Severity.Severe)
                {
                    _sm.Break(inst);
                    _forceBreakHandler.ForceBreak(npcId);
                }
                else
                {
                    _sm.MakePermanent(inst);
                }
                modDirty = true;
            }
        }

        if (modDirty)
            _modCalc.RecomputeAndWrite(npcId);
    }
}
