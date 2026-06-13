using FengZhi.Foundation.NpcState;
using NpcRuntimeState = FengZhi.Foundation.NpcState.NpcState;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Romance milestone flags owned by NPC State and interpreted by the Romance rules layer.
/// </summary>
public sealed record RomanceMilestoneState(
    bool Broken = false,
    bool Acquainted = false,
    bool Trust = false,
    bool Crisis = false,
    bool Heart = false,
    bool Bond = false
);

/// <summary>
/// NPC State boundary required by Romance. Implementations own the data; Romance owns only rules.
/// </summary>
public interface IRomanceNpcStatePort
{
    /// <summary>Reads the NPC's current attitude.</summary>
    AttitudeLevel? GetAttitude(string npcId);

    /// <summary>Reads the NPC's romance milestones.</summary>
    RomanceMilestoneState? GetMilestones(string npcId);

    /// <summary>Writes the clamped attitude through the owning NPC State system.</summary>
    bool SetAttitude(string npcId, AttitudeLevel attitude, string source);

    /// <summary>Writes a romance milestone flag through the owning NPC State system.</summary>
    bool SetMilestone(string npcId, RomanceMilestone milestone, bool value, string source);

    /// <summary>Writes the terminal break milestone and attitude as one contract operation.</summary>
    bool ForceBreak(string npcId, AttitudeLevel terminalAttitude, string source);

    /// <summary>Reads the globally bonded heroine, if one has been confirmed this playthrough.</summary>
    string? GetBondedHeroine();

    /// <summary>Writes M_BOND and the global bonded heroine flag as one contract operation.</summary>
    bool ConfirmBond(string npcId, string source);

    /// <summary>Returns whether a romance-owned flag is currently set.</summary>
    bool HasRomanceFlag(string npcId, string key);

    /// <summary>Writes a romance-owned flag through the NPC State owner.</summary>
    bool SetRomanceFlag(string npcId, string key, string value, string source);
}

/// <summary>
/// Adapter that stores romance milestone flags inside <see cref="NpcState"/> flags.
/// </summary>
internal sealed class NpcStateRomancePort : IRomanceNpcStatePort
{
    public const string BrokenFlag = "romance_milestone_break";
    public const string AcquaintedFlag = "romance_milestone_acquainted";
    public const string TrustFlag = "romance_milestone_trust";
    public const string CrisisFlag = "romance_milestone_crisis";
    public const string HeartFlag = "romance_milestone_heart";
    public const string BondFlag = "romance_milestone_bond";
    public const string BondedHeroineFlag = "romance_bonded_heroine";

    private readonly INpcStateManager _npcState;
    private readonly INpcStateAttitudeWriter _attitudeWriter;

    public NpcStateRomancePort(NpcStateManager npcState)
    {
        _npcState = npcState;
        _attitudeWriter = npcState;
    }

    public AttitudeLevel? GetAttitude(string npcId)
    {
        return _npcState.GetState(npcId)?.Attitude;
    }

    public RomanceMilestoneState? GetMilestones(string npcId)
    {
        var state = _npcState.GetState(npcId);
        if (state == null) return null;

        return new RomanceMilestoneState(
            Broken: HasFlag(state, BrokenFlag),
            Acquainted: HasFlag(state, AcquaintedFlag),
            Trust: HasFlag(state, TrustFlag),
            Crisis: HasFlag(state, CrisisFlag),
            Heart: HasFlag(state, HeartFlag),
            Bond: HasFlag(state, BondFlag)
        );
    }

    public bool SetAttitude(string npcId, AttitudeLevel attitude, string source)
    {
        return _attitudeWriter.UpdateAttitude(npcId, attitude, source);
    }

    public bool SetMilestone(string npcId, RomanceMilestone milestone, bool value, string source)
    {
        return SetMilestoneFlag(npcId, GetFlagKey(milestone), value, source);
    }

    public bool ForceBreak(string npcId, AttitudeLevel terminalAttitude, string source)
    {
        var state = _npcState.GetState(npcId);
        if (state == null || state.IsDead) return false;

        var hadBreakFlag = state.Flags.TryGetValue(BrokenFlag, out var previousBreakValue);
        if (!SetMilestone(npcId, RomanceMilestone.Break, true, source)) return false;
        if (_attitudeWriter.UpdateAttitude(npcId, terminalAttitude, source)) return true;

        // Best-effort rollback preserves the contract if an unexpected writer failure appears.
        if (hadBreakFlag)
            _npcState.UpdateFlag(npcId, BrokenFlag, previousBreakValue!, source);
        else
            _npcState.RemoveFlag(npcId, BrokenFlag, source);

        return false;
    }

    public string? GetBondedHeroine()
    {
        return _npcState.GetAll()
            .FirstOrDefault(state => HasFlag(state, BondedHeroineFlag))
            ?.TemplateId;
    }

    public bool ConfirmBond(string npcId, string source)
    {
        var state = _npcState.GetState(npcId);
        if (state == null || state.IsDead || GetBondedHeroine() != null) return false;

        var hadBondFlag = state.Flags.TryGetValue(BondFlag, out var previousBondValue);
        if (!SetMilestone(npcId, RomanceMilestone.Bond, true, source)) return false;
        if (SetRomanceFlag(npcId, BondedHeroineFlag, "true", source)) return true;

        if (hadBondFlag)
            _npcState.UpdateFlag(npcId, BondFlag, previousBondValue!, source);
        else
            _npcState.RemoveFlag(npcId, BondFlag, source);

        return false;
    }

    public bool HasRomanceFlag(string npcId, string key)
    {
        EnsureRomanceFlagKey(key);
        var state = _npcState.GetState(npcId);
        return state != null && state.Flags.ContainsKey(key);
    }

    public bool SetRomanceFlag(string npcId, string key, string value, string source)
    {
        EnsureRomanceFlagKey(key);
        return _npcState.UpdateFlag(npcId, key, value, source);
    }

    /// <summary>Writes a romance-prefixed milestone flag through the NPC State owner.</summary>
    internal bool SetMilestoneFlag(string npcId, string key, bool value, string source)
    {
        EnsureRomanceFlagKey(key);

        return value
            ? _npcState.UpdateFlag(npcId, key, "true", source)
            : _npcState.RemoveFlag(npcId, key, source);
    }

    private static void EnsureRomanceFlagKey(string key)
    {
        if (!key.StartsWith("romance_", StringComparison.Ordinal))
            throw new ArgumentException("Romance flags must use the romance_ prefix.", nameof(key));
    }

    private static string GetFlagKey(RomanceMilestone milestone)
    {
        return milestone switch
        {
            RomanceMilestone.Break => BrokenFlag,
            RomanceMilestone.Acquainted => AcquaintedFlag,
            RomanceMilestone.Trust => TrustFlag,
            RomanceMilestone.Crisis => CrisisFlag,
            RomanceMilestone.Heart => HeartFlag,
            RomanceMilestone.Bond => BondFlag,
            _ => throw new ArgumentOutOfRangeException(nameof(milestone), milestone, null)
        };
    }

    private static bool HasFlag(NpcRuntimeState state, string key)
    {
        return state.Flags.TryGetValue(key, out var value)
            && bool.TryParse(value, out var parsed)
            && parsed;
    }
}
