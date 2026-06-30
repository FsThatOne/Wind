using System;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会状态机 — 管理状态转换和 severity 升级。</summary>
public sealed class MisunderstandingStateMachine
{
    /// <summary>激活一个 DORMANT 误会。</summary>
    public void Activate(MisunderstandingInstance inst)
    {
        AssertState(inst, MisunderstandingState.Dormant);
        inst.State = MisunderstandingState.Active;
    }

    /// <summary>执行恶化（severity 递升 + escalation_count++）。</summary>
    public void Escalate(MisunderstandingInstance inst)
    {
        if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated))
            throw new InvalidStateTransitionException(inst.State, MisunderstandingState.Escalated,
                "只能从 Active 或 Escalated 恶化");

        inst.EscalationCount++;

        inst.Severity = inst.Severity switch
        {
            Severity.Minor => Severity.Moderate,
            Severity.Moderate => Severity.Severe,
            _ => inst.Severity,
        };

        if (inst.Severity == Severity.Severe)
            inst.WindowRemaining = Math.Min(inst.WindowRemaining, 3);

        inst.State = MisunderstandingState.Escalated;
    }

    /// <summary>澄清成功 → RESOLVED。</summary>
    public void Resolve(MisunderstandingInstance inst)
    {
        if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated or MisunderstandingState.Permanent))
            throw new InvalidStateTransitionException(inst.State, MisunderstandingState.Resolved,
                "只能从 Active/Escalated/Permanent 澄清");

        inst.State = MisunderstandingState.Resolved;
    }

    /// <summary>窗口关闭 → PERMANENT（非 SEVERE）。</summary>
    public void MakePermanent(MisunderstandingInstance inst)
    {
        if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated))
            throw new InvalidStateTransitionException(inst.State, MisunderstandingState.Permanent,
                "只能从 Active/Escalated 定型");

        if (inst.Severity == Severity.Severe)
            throw new InvalidStateTransitionException(inst.State, MisunderstandingState.Permanent,
                "SEVERE 应走 Break 路径而非 Permanent");

        inst.State = MisunderstandingState.Permanent;
    }

    /// <summary>SEVERE 窗口归零 → BROKEN（诀别）。</summary>
    public void Break(MisunderstandingInstance inst)
    {
        if (inst.State is not (MisunderstandingState.Active or MisunderstandingState.Escalated or MisunderstandingState.Permanent))
            throw new InvalidStateTransitionException(inst.State, MisunderstandingState.Broken,
                "只能从 Active/Escalated/Permanent 诀别");

        inst.State = MisunderstandingState.Broken;
    }

    private static void AssertState(MisunderstandingInstance inst, MisunderstandingState expected)
    {
        if (inst.State != expected)
            throw new InvalidStateTransitionException(inst.State, expected,
                $"预期状态 {expected}，实际 {inst.State}");
    }
}

/// <summary>非法状态转换异常。</summary>
public sealed class InvalidStateTransitionException : InvalidOperationException
{
    public MisunderstandingState FromState { get; }
    public MisunderstandingState AttemptedTarget { get; }

    public InvalidStateTransitionException(MisunderstandingState from, MisunderstandingState target, string reason)
        : base($"非法状态转换: {from} → {target} — {reason}")
    {
        FromState = from;
        AttemptedTarget = target;
    }
}
