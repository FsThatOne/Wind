using FengZhi.Foundation.Events;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public class MilestoneUnlockAndForceBreakTest
{
    [Fact]
    public void CanUnlock_WhenTrustMissing_RejectsCrisis()
    {
        var (_, _, milestones) = CreateService(
            AttitudeLevel.Friendly,
            new RomanceMilestoneState(Acquainted: true));

        Assert.False(milestones.CanUnlock("heroine_a", RomanceMilestone.Crisis));
    }

    [Theory]
    [InlineData(RomanceMilestone.Trust, 0)]
    [InlineData(RomanceMilestone.Crisis, 0)]
    [InlineData(RomanceMilestone.Heart, 1)]
    [InlineData(RomanceMilestone.Bond, 1)]
    public void CanUnlock_WhenAttitudeBelowThreshold_RejectsMilestone(
        RomanceMilestone milestone,
        int attitude)
    {
        var (_, _, milestones) = CreateService(
            (AttitudeLevel)attitude,
            new RomanceMilestoneState(
                Acquainted: true,
                Trust: true,
                Crisis: true,
                Heart: true));

        Assert.False(milestones.CanUnlock("heroine_a", milestone));
    }

    [Theory]
    [InlineData(RomanceMilestone.Acquainted, -4)]
    [InlineData(RomanceMilestone.Trust, 1)]
    [InlineData(RomanceMilestone.Crisis, 1)]
    [InlineData(RomanceMilestone.Heart, 2)]
    [InlineData(RomanceMilestone.Bond, 2)]
    public void CanUnlock_WhenPredecessorAndThresholdMet_AllowsMilestone(
        RomanceMilestone milestone,
        int attitude)
    {
        var (_, _, milestones) = CreateService(
            (AttitudeLevel)attitude,
            new RomanceMilestoneState(
                Acquainted: milestone != RomanceMilestone.Acquainted,
                Trust: milestone is RomanceMilestone.Crisis or RomanceMilestone.Heart or RomanceMilestone.Bond,
                Crisis: milestone is RomanceMilestone.Heart or RomanceMilestone.Bond,
                Heart: milestone == RomanceMilestone.Bond));

        Assert.True(milestones.CanUnlock("heroine_a", milestone));
    }

    [Fact]
    public void TryUnlockMilestone_WhenGatePasses_WritesNpcOwnedFlag()
    {
        var (service, npcState, _) = CreateService(
            AttitudeLevel.Friendly,
            new RomanceMilestoneState(Acquainted: true));

        var result = service.TryUnlockMilestone("heroine_a", RomanceMilestone.Trust, "story");

        Assert.True(result.Success);
        Assert.True(npcState.GetMilestones("heroine_a")!.Trust);
        Assert.Equal(("heroine_a", RomanceMilestone.Trust, true, "story"), Assert.Single(npcState.MilestoneWrites));
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, true, true, true, false)]
    [InlineData(false, true, true, true, true)]
    public void ForceBreak_FromAnyMilestoneState_SetsBreakAndDrawnSword(
        bool broken,
        bool acquainted,
        bool trust,
        bool crisis,
        bool heart)
    {
        var (service, npcState, _) = CreateService(
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(broken, acquainted, trust, crisis, heart, Bond: heart));

        var result = service.ForceBreak("heroine_a", "plot");

        Assert.True(result.Success);
        Assert.True(npcState.GetMilestones("heroine_a")!.Broken);
        Assert.Equal(AttitudeLevel.DrawnSword, npcState.GetAttitude("heroine_a"));
    }

    [Fact]
    public void TryUnlockMilestone_WhenRequestedForBreak_RejectsNormalPath()
    {
        var (service, npcState, _) = CreateService(
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true, Bond: true));

        var result = service.TryUnlockMilestone("heroine_a", RomanceMilestone.Break, "story");

        Assert.False(result.Success);
        Assert.Equal("break_requires_force_break", result.ErrorCode);
        Assert.False(npcState.GetMilestones("heroine_a")!.Broken);
    }

    [Fact]
    public void BrokenNpc_RejectsLaterNormalRomanceJudgement()
    {
        var (service, npcState, milestones) = CreateService(
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true));

        var forced = service.ForceBreak("heroine_a", "plot");
        var laterUnlock = service.TryUnlockMilestone("heroine_a", RomanceMilestone.Bond, "story");
        var laterAttitude = service.ApplyAttitudeChange("heroine_a", 3, "dialogue");

        Assert.True(forced.Success);
        Assert.False(laterUnlock.Success);
        Assert.False(laterAttitude.Success);
        Assert.Equal("romance_broken", laterAttitude.ErrorCode);
        Assert.False(milestones.CanUnlock("heroine_a", RomanceMilestone.Bond));
        Assert.Equal(AttitudeLevel.DrawnSword, npcState.GetAttitude("heroine_a"));
        Assert.True(npcState.GetMilestones("heroine_a")!.Broken);
    }

    [Fact]
    public void ForceBreak_WhenAfterPositiveUnlock_RemainsTerminalWinner()
    {
        var (service, npcState, milestones) = CreateService(
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true));

        var unlocked = service.TryUnlockMilestone("heroine_a", RomanceMilestone.Heart, "story");
        var forced = service.ForceBreak("heroine_a", "plot");

        Assert.True(unlocked.Success);
        Assert.True(forced.Success);
        Assert.True(npcState.GetMilestones("heroine_a")!.Broken);
        Assert.False(milestones.CanUnlock("heroine_a", RomanceMilestone.Bond));
        Assert.Equal(AttitudeLevel.DrawnSword, npcState.GetAttitude("heroine_a"));
    }

    [Fact]
    public void ForceBreak_WhenTerminalWriteFails_DoesNotPartiallySetBreak()
    {
        var npcState = new RecordingRomanceNpcStatePort { FailForceBreak = true };
        npcState.Register(
            "heroine_a",
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true, Bond: true));
        var milestones = new MilestoneRegistry(npcState);
        var service = new RomanceService(npcState, milestones);

        var result = service.ForceBreak("heroine_a", "plot");

        Assert.False(result.Success);
        Assert.Equal("npc_state_missing", result.ErrorCode);
        Assert.False(npcState.GetMilestones("heroine_a")!.Broken);
        Assert.Equal(AttitudeLevel.LifeDeath, npcState.GetAttitude("heroine_a"));
    }

    [Fact]
    public void NpcStateRomancePort_ForceBreakWritesBreakAndAttitudeTogether()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        var port = new NpcStateRomancePort(manager);

        var result = port.ForceBreak("heroine_a", AttitudeLevel.DrawnSword, "plot");

        Assert.True(result);
        var state = manager.GetState("heroine_a");
        Assert.NotNull(state);
        Assert.Equal(AttitudeLevel.DrawnSword, state!.Attitude);
        Assert.True(port.GetMilestones("heroine_a")!.Broken);
    }

    private static (
        RomanceService Service,
        RecordingRomanceNpcStatePort NpcState,
        MilestoneRegistry Milestones) CreateService(
            AttitudeLevel attitude,
            RomanceMilestoneState milestones)
    {
        var npcState = new RecordingRomanceNpcStatePort();
        npcState.Register("heroine_a", attitude, milestones);
        var registry = new MilestoneRegistry(npcState);
        return (new RomanceService(npcState, registry, new EventBus()), npcState, registry);
    }

    private sealed class RecordingRomanceNpcStatePort : IRomanceNpcStatePort
    {
        private readonly Dictionary<string, AttitudeLevel> _attitudes = new();
        private readonly Dictionary<string, RomanceMilestoneState> _milestones = new();

        public bool FailForceBreak { get; init; }
        public List<(string NpcId, RomanceMilestone Milestone, bool Value, string Source)> MilestoneWrites { get; } = new();

        public void Register(string npcId, AttitudeLevel attitude, RomanceMilestoneState milestones)
        {
            _attitudes[npcId] = attitude;
            _milestones[npcId] = milestones;
        }

        public AttitudeLevel? GetAttitude(string npcId)
        {
            return _attitudes.TryGetValue(npcId, out var attitude) ? attitude : null;
        }

        public RomanceMilestoneState? GetMilestones(string npcId)
        {
            return _milestones.TryGetValue(npcId, out var milestones) ? milestones : null;
        }

        public bool SetAttitude(string npcId, AttitudeLevel attitude, string source)
        {
            if (!_attitudes.ContainsKey(npcId)) return false;
            _attitudes[npcId] = attitude;
            return true;
        }

        public bool SetMilestone(string npcId, RomanceMilestone milestone, bool value, string source)
        {
            if (!_milestones.TryGetValue(npcId, out var current)) return false;

            _milestones[npcId] = milestone switch
            {
                RomanceMilestone.Break => current with { Broken = value },
                RomanceMilestone.Acquainted => current with { Acquainted = value },
                RomanceMilestone.Trust => current with { Trust = value },
                RomanceMilestone.Crisis => current with { Crisis = value },
                RomanceMilestone.Heart => current with { Heart = value },
                RomanceMilestone.Bond => current with { Bond = value },
                _ => current
            };
            MilestoneWrites.Add((npcId, milestone, value, source));
            return true;
        }

        public bool ForceBreak(string npcId, AttitudeLevel terminalAttitude, string source)
        {
            if (FailForceBreak) return false;
            if (!_attitudes.ContainsKey(npcId) || !_milestones.TryGetValue(npcId, out var current))
                return false;

            _milestones[npcId] = current with { Broken = true };
            _attitudes[npcId] = terminalAttitude;
            MilestoneWrites.Add((npcId, RomanceMilestone.Break, true, source));
            return true;
        }
    }
}
