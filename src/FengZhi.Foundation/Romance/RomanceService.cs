using FengZhi.Foundation.Events;
using FengZhi.Foundation.NpcState;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Records audit information when a milestone floor swallows part of an attitude delta.
/// </summary>
public interface IRomanceAuditLog
{
    /// <summary>Stores one floor-clamp audit record.</summary>
    void RecordFloorClamp(RomanceFloorClampAudit record);
}

/// <summary>
/// No-op audit sink for tests or callers that do not need persisted debug records.
/// </summary>
public sealed class NullRomanceAuditLog : IRomanceAuditLog
{
    public void RecordFloorClamp(RomanceFloorClampAudit record)
    {
    }
}

/// <summary>
/// Audit record produced when a requested attitude value is clamped by milestone floor.
/// </summary>
public sealed record RomanceFloorClampAudit(
    string NpcId,
    int Delta,
    int Current,
    int Proposed,
    int Clamped,
    int Floor,
    string Source
);

/// <summary>
/// Result of applying one romance attitude change request.
/// </summary>
public sealed record RomanceAttitudeChangeResult(
    bool Success,
    string? ErrorCode,
    int Current,
    int Proposed,
    int Clamped
)
{
    public static RomanceAttitudeChangeResult Ok(int current, int proposed, int clamped)
    {
        return new RomanceAttitudeChangeResult(true, null, current, proposed, clamped);
    }

    public static RomanceAttitudeChangeResult Fail(string errorCode)
    {
        return new RomanceAttitudeChangeResult(false, errorCode, 0, 0, 0);
    }
}

/// <summary>
/// Result of a romance milestone write request.
/// </summary>
public sealed record RomanceMilestoneResult(
    bool Success,
    string? ErrorCode,
    RomanceMilestone Milestone
)
{
    public static RomanceMilestoneResult Ok(RomanceMilestone milestone)
    {
        return new RomanceMilestoneResult(true, null, milestone);
    }

    public static RomanceMilestoneResult Fail(string errorCode, RomanceMilestone milestone)
    {
        return new RomanceMilestoneResult(false, errorCode, milestone);
    }
}

/// <summary>
/// Romance rules entry point. Data remains owned by NPC State; this service only applies rules.
/// </summary>
public sealed class RomanceService : IDisposable
{
    private const string DefaultSource = "romance";
    private readonly IRomanceNpcStatePort _npcState;
    private readonly MilestoneRegistry _milestones;
    private readonly IEventBus? _eventBus;
    private readonly IRomanceAuditLog _auditLog;
    private readonly Action? _unsubscribe;

    public RomanceService(
        IRomanceNpcStatePort npcState,
        MilestoneRegistry milestones,
        IEventBus? eventBus = null,
        IRomanceAuditLog? auditLog = null)
    {
        _npcState = npcState;
        _milestones = milestones;
        _eventBus = eventBus;
        _auditLog = auditLog ?? new NullRomanceAuditLog();
        _unsubscribe = _eventBus?.Subscribe<AttitudeChangeRequestEvent>(OnAttitudeChangeRequest);
    }

    /// <summary>
    /// Applies the milestone floor to a requested attitude delta and writes the result through NPC State.
    /// </summary>
    public RomanceAttitudeChangeResult ApplyAttitudeChange(string npcId, int delta, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return RomanceAttitudeChangeResult.Fail("npc_id_required");

        var currentAttitude = _npcState.GetAttitude(npcId);
        var milestones = _npcState.GetMilestones(npcId);
        var floor = _milestones.GetFloor(npcId);
        if (currentAttitude == null || milestones == null || floor == null)
            return RomanceAttitudeChangeResult.Fail("npc_state_missing");
        if (milestones.Broken)
            return RomanceAttitudeChangeResult.Fail("romance_broken");

        var current = (int)currentAttitude.Value;
        var proposed = current + delta;
        var clamped = Math.Max(proposed, floor.Value);
        var attitude = ToAttitudeLevel(clamped);
        if (attitude == null)
            return RomanceAttitudeChangeResult.Fail("attitude_out_of_range");

        var changeSource = string.IsNullOrWhiteSpace(source) ? DefaultSource : source;
        if (!_npcState.SetAttitude(npcId, attitude.Value, changeSource))
            return RomanceAttitudeChangeResult.Fail("npc_state_write_failed");

        if (clamped != proposed)
        {
            _auditLog.RecordFloorClamp(new RomanceFloorClampAudit(
                npcId,
                delta,
                current,
                proposed,
                clamped,
                floor.Value,
                changeSource));
        }

        _eventBus?.Publish(new RomanceAttitudeChangedEvent(
            npcId,
            delta,
            current,
            proposed,
            clamped,
            changeSource));

        return RomanceAttitudeChangeResult.Ok(current, proposed, clamped);
    }

    /// <summary>
    /// Attempts to unlock a normal milestone in strict sequence.
    /// </summary>
    public RomanceMilestoneResult TryUnlockMilestone(
        string npcId,
        RomanceMilestone milestone,
        string? source = null)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return RomanceMilestoneResult.Fail("npc_id_required", milestone);
        if (milestone == RomanceMilestone.Break)
            return RomanceMilestoneResult.Fail("break_requires_force_break", milestone);

        var changeSource = string.IsNullOrWhiteSpace(source) ? DefaultSource : source;
        if (!_milestones.CanUnlock(npcId, milestone))
            return RomanceMilestoneResult.Fail("milestone_gate_not_met", milestone);
        if (!_milestones.Unlock(npcId, milestone, changeSource))
            return RomanceMilestoneResult.Fail("npc_state_write_failed", milestone);

        _eventBus?.Publish(new RomanceMilestoneUnlockedEvent(npcId, milestone, changeSource));
        return RomanceMilestoneResult.Ok(milestone);
    }

    /// <summary>
    /// Applies the terminal break override, ignoring milestone floor and progression gates.
    /// </summary>
    public RomanceMilestoneResult ForceBreak(string npcId, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return RomanceMilestoneResult.Fail("npc_id_required", RomanceMilestone.Break);

        var changeSource = string.IsNullOrWhiteSpace(source) ? DefaultSource : source;
        if (!_milestones.ForceBreak(npcId, changeSource))
            return RomanceMilestoneResult.Fail("npc_state_missing", RomanceMilestone.Break);

        _eventBus?.Publish(new RomanceForceBreakEvent(npcId, changeSource));
        return RomanceMilestoneResult.Ok(RomanceMilestone.Break);
    }

    public void Dispose()
    {
        _unsubscribe?.Invoke();
    }

    private void OnAttitudeChangeRequest(AttitudeChangeRequestEvent request)
    {
        ApplyAttitudeChange(request.NpcId, request.Delta, request.Source);
    }

    private static AttitudeLevel? ToAttitudeLevel(int value)
    {
        return Enum.IsDefined(typeof(AttitudeLevel), value)
            ? (AttitudeLevel)value
            : null;
    }
}
