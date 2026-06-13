using FengZhi.Foundation.NpcState;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Ordered romance milestones. Break is terminal and can only be set by force break.
/// </summary>
public enum RomanceMilestone
{
    Acquainted,
    Trust,
    Crisis,
    Heart,
    Bond,
    Break
}

/// <summary>
/// Calculates romance milestone floors from NPC-owned milestone flags.
/// </summary>
public sealed class MilestoneRegistry
{
    public const int BreakFloor = -4;
    public const int BondFloor = 3;
    public const int HeartFloor = 2;
    public const int CrisisFloor = 2;
    public const int TrustFloor = 1;
    public const int AcquaintedFloor = 0;
    public const int NoMilestoneFloor = -4;

    private readonly IRomanceNpcStatePort _npcState;

    public MilestoneRegistry(IRomanceNpcStatePort npcState)
    {
        _npcState = npcState;
    }

    /// <summary>
    /// Returns the attitude floor from the highest milestone currently stored in NPC State.
    /// </summary>
    public int? GetFloor(string npcId)
    {
        var milestones = _npcState.GetMilestones(npcId);
        if (milestones == null) return null;

        if (milestones.Broken) return BreakFloor;
        if (milestones.Bond) return BondFloor;
        if (milestones.Heart) return HeartFloor;
        if (milestones.Crisis) return CrisisFloor;
        if (milestones.Trust) return TrustFloor;
        if (milestones.Acquainted) return AcquaintedFloor;
        return NoMilestoneFloor;
    }

    /// <summary>
    /// Checks milestone predecessor and attitude gates without mutating NPC State.
    /// </summary>
    public bool CanUnlock(string npcId, RomanceMilestone milestone)
    {
        var milestones = _npcState.GetMilestones(npcId);
        var attitude = _npcState.GetAttitude(npcId);
        if (milestones == null || attitude == null) return false;
        if (milestones.Broken) return false;

        var attitudeValue = (int)attitude.Value;
        return milestone switch
        {
            RomanceMilestone.Acquainted => true,
            RomanceMilestone.Trust => milestones.Acquainted && attitudeValue >= TrustFloor,
            RomanceMilestone.Crisis => milestones.Trust && attitudeValue >= TrustFloor,
            RomanceMilestone.Heart => milestones.Crisis && attitudeValue >= HeartFloor,
            RomanceMilestone.Bond => milestones.Heart && attitudeValue >= HeartFloor,
            RomanceMilestone.Break => false,
            _ => false
        };
    }

    /// <summary>
    /// Unlocks one normal milestone when its predecessor and attitude gates pass.
    /// </summary>
    public bool Unlock(string npcId, RomanceMilestone milestone, string source)
    {
        if (milestone == RomanceMilestone.Break) return false;
        return CanUnlock(npcId, milestone)
            && _npcState.SetMilestone(npcId, milestone, true, source);
    }

    /// <summary>
    /// Applies the terminal break milestone. This bypasses normal milestone gates.
    /// </summary>
    public bool ForceBreak(string npcId, string source)
    {
        return _npcState.ForceBreak(npcId, AttitudeLevel.DrawnSword, source);
    }
}
