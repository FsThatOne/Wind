using FengZhi.Foundation.Events;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public class RomanceStateAndMilestoneFloorTest
{
    [Fact]
    public void ApplyAttitudeChange_WhenTrustFloorWouldBeCrossed_ClampsToFriendly()
    {
        var (service, npcState, auditLog, _) = CreateService();
        npcState.Register("heroine_a", AttitudeLevel.Friendly, new RomanceMilestoneState(Trust: true));

        var result = service.ApplyAttitudeChange("heroine_a", -3, "dialogue");

        Assert.True(result.Success);
        Assert.Equal(1, result.Clamped);
        Assert.Equal(AttitudeLevel.Friendly, npcState.GetAttitude("heroine_a"));
        var audit = Assert.Single(auditLog.Records);
        Assert.Equal("heroine_a", audit.NpcId);
        Assert.Equal(-3, audit.Delta);
        Assert.Equal(-2, audit.Proposed);
        Assert.Equal(1, audit.Clamped);
    }

    [Theory]
    [InlineData(false, false, false, false, false, false, -4)]
    [InlineData(false, true, false, false, false, false, 0)]
    [InlineData(false, true, true, false, false, false, 1)]
    [InlineData(false, true, true, true, false, false, 2)]
    [InlineData(false, true, true, true, true, false, 2)]
    [InlineData(false, true, true, true, true, true, 3)]
    [InlineData(true, true, true, true, true, true, -4)]
    public void GetFloor_UsesHighestNpcStateMilestone(
        bool broken,
        bool acquainted,
        bool trust,
        bool crisis,
        bool heart,
        bool bond,
        int expectedFloor)
    {
        var npcState = new RecordingRomanceNpcStatePort();
        npcState.Register(
            "heroine_a",
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(broken, acquainted, trust, crisis, heart, bond));
        var registry = new MilestoneRegistry(npcState);

        Assert.Equal(expectedFloor, registry.GetFloor("heroine_a"));
    }

    [Fact]
    public void ApplyAttitudeChange_WhenDeltaStaysAboveFloor_DoesNotAudit()
    {
        var (service, npcState, auditLog, _) = CreateService();
        npcState.Register("heroine_a", AttitudeLevel.Trusted, new RomanceMilestoneState(Trust: true));

        var result = service.ApplyAttitudeChange("heroine_a", -1, "dialogue");

        Assert.True(result.Success);
        Assert.Equal(1, result.Clamped);
        Assert.Equal(AttitudeLevel.Friendly, npcState.GetAttitude("heroine_a"));
        Assert.Empty(auditLog.Records);
    }

    [Fact]
    public void EventBusRequest_AppliesClampAndPublishesTypedResultEvent()
    {
        var bus = new EventBus();
        var (service, npcState, _, _) = CreateService(bus);
        npcState.Register("heroine_a", AttitudeLevel.Friendly, new RomanceMilestoneState(Trust: true));
        RomanceAttitudeChangedEvent? changed = null;
        bus.Subscribe<RomanceAttitudeChangedEvent>(e => changed = e);

        bus.Publish(new AttitudeChangeRequestEvent("heroine_a", -3, "dialogue"));

        Assert.Equal(AttitudeLevel.Friendly, npcState.GetAttitude("heroine_a"));
        Assert.NotNull(changed);
        Assert.Equal("heroine_a", changed!.NpcId);
        Assert.Equal(-2, changed.Proposed);
        Assert.Equal(1, changed.Clamped);
        service.Dispose();
    }

    [Fact]
    public void ApplyAttitudeChange_UsesNpcStateContractForReadsAndWrites()
    {
        var (service, npcState, _, _) = CreateService();
        npcState.Register("heroine_a", AttitudeLevel.Friendly, new RomanceMilestoneState(Trust: true));

        var result = service.ApplyAttitudeChange("heroine_a", -3, "dialogue");

        Assert.True(result.Success);
        Assert.Equal(1, npcState.GetAttitudeReadCount);
        Assert.Equal(2, npcState.GetMilestoneReadCount);
        Assert.Single(npcState.AttitudeWrites);
        Assert.Equal(("heroine_a", AttitudeLevel.Friendly, "dialogue"), npcState.AttitudeWrites[0]);
    }

    [Fact]
    public void ApplyAttitudeChange_WhenNpcMissing_FailsExplicitlyWithoutWriting()
    {
        var (service, npcState, auditLog, _) = CreateService();

        var result = service.ApplyAttitudeChange("missing", -1, "dialogue");

        Assert.False(result.Success);
        Assert.Equal("npc_state_missing", result.ErrorCode);
        Assert.Empty(npcState.AttitudeWrites);
        Assert.Empty(auditLog.Records);
    }

    [Fact]
    public void NpcStateManagerContract_DoesNotExposeRawAttitudeWriter()
    {
        Assert.Null(typeof(INpcStateManager).GetMethod("UpdateAttitude"));
    }

    [Fact]
    public void NpcStateRomancePort_ReadsNpcOwnedMilestonesAndWritesThroughNpcState()
    {
        var bus = new EventBus();
        var manager = new NpcStateManager(bus);
        manager.RegisterNpc("heroine_a");
        manager.UpdateFlag("heroine_a", NpcStateRomancePort.TrustFlag, "true", "story");
        var port = new NpcStateRomancePort(manager);
        var service = new RomanceService(port, new MilestoneRegistry(port));

        var result = service.ApplyAttitudeChange("heroine_a", -3, "dialogue");

        Assert.True(result.Success);
        var state = manager.GetState("heroine_a");
        Assert.NotNull(state);
        Assert.Equal(AttitudeLevel.Friendly, state!.Attitude);
        Assert.True(port.GetMilestones("heroine_a")!.Trust);
    }

    [Fact]
    public void NpcStateRomancePort_RejectsNonRomanceMilestoneFlagPrefix()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        var port = new NpcStateRomancePort(manager);

        var ex = Assert.Throws<ArgumentException>(
            () => port.SetMilestoneFlag("heroine_a", "narrative_milestone_trust", true, "test"));

        Assert.Contains("romance_ prefix", ex.Message);
    }

    private static (
        RomanceService Service,
        RecordingRomanceNpcStatePort NpcState,
        RecordingRomanceAuditLog AuditLog,
        MilestoneRegistry Milestones) CreateService(IEventBus? bus = null)
    {
        var npcState = new RecordingRomanceNpcStatePort();
        var milestones = new MilestoneRegistry(npcState);
        var auditLog = new RecordingRomanceAuditLog();
        var service = new RomanceService(npcState, milestones, bus, auditLog);
        return (service, npcState, auditLog, milestones);
    }

    private sealed class RecordingRomanceAuditLog : IRomanceAuditLog
    {
        public List<RomanceFloorClampAudit> Records { get; } = new();

        public void RecordFloorClamp(RomanceFloorClampAudit record)
        {
            Records.Add(record);
        }
    }

    private sealed class RecordingRomanceNpcStatePort : IRomanceNpcStatePort
    {
        private readonly Dictionary<string, AttitudeLevel> _attitudes = new();
        private readonly Dictionary<string, RomanceMilestoneState> _milestones = new();

        public int GetAttitudeReadCount { get; private set; }
        public int GetMilestoneReadCount { get; private set; }
        public List<(string NpcId, AttitudeLevel Attitude, string Source)> AttitudeWrites { get; } = new();

        public void Register(string npcId, AttitudeLevel attitude, RomanceMilestoneState milestones)
        {
            _attitudes[npcId] = attitude;
            _milestones[npcId] = milestones;
        }

        public AttitudeLevel? GetAttitude(string npcId)
        {
            GetAttitudeReadCount++;
            return _attitudes.TryGetValue(npcId, out var attitude) ? attitude : null;
        }

        public RomanceMilestoneState? GetMilestones(string npcId)
        {
            GetMilestoneReadCount++;
            return _milestones.TryGetValue(npcId, out var milestones) ? milestones : null;
        }

        public bool SetAttitude(string npcId, AttitudeLevel attitude, string source)
        {
            if (!_attitudes.ContainsKey(npcId)) return false;
            _attitudes[npcId] = attitude;
            AttitudeWrites.Add((npcId, attitude, source));
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
            return true;
        }

        public bool ForceBreak(string npcId, AttitudeLevel terminalAttitude, string source)
        {
            if (!_attitudes.ContainsKey(npcId) || !_milestones.TryGetValue(npcId, out var current))
                return false;

            _milestones[npcId] = current with { Broken = true };
            _attitudes[npcId] = terminalAttitude;
            return true;
        }
    }
}
