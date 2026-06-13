using FengZhi.Foundation.Events;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public class BondFlowExclusivityAndDeclineTest
{
    [Fact]
    public void TryBond_WhenEligibleOnlyAwaitsChoiceWithoutMutatingBondState()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");

        var result = service.TryBond("heroine_a");

        Assert.Equal(RomanceBondStatus.AwaitingChoice, result.Status);
        Assert.Equal("awaiting_choice", result.VariantKey);
        Assert.Equal("None", service.BondedHeroine);
        Assert.False(npcState.GetMilestones("heroine_a")!.Bond);
    }

    [Fact]
    public void ConfirmBond_WhenEligibleSetsBondMilestoneAndGlobalBondedHeroine()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");

        var result = service.ConfirmBond("heroine_a", "bond_node");

        Assert.Equal(RomanceBondStatus.Confirmed, result.Status);
        Assert.Equal("bond_confirmed", result.VariantKey);
        Assert.Equal("heroine_a", service.BondedHeroine);
        Assert.True(npcState.GetMilestones("heroine_a")!.Bond);
        Assert.Equal(("heroine_a", RomanceMilestone.Bond, true, "bond_node"), Assert.Single(npcState.MilestoneWrites));
    }

    [Fact]
    public void ConfirmBond_WhenGlobalBondWriteFailsDoesNotPartiallySetBondMilestone()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");
        npcState.FailGlobalBondWrite = true;

        var result = service.ConfirmBond("heroine_a", "bond_node");

        Assert.Equal(RomanceBondStatus.ConditionsNotMet, result.Status);
        Assert.Equal("None", service.BondedHeroine);
        Assert.False(npcState.GetMilestones("heroine_a")!.Bond);
    }

    [Fact]
    public void BondedHeroine_WhenServiceRecreatedIsReadFromNpcStateFlag()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        manager.RegisterNpc("heroine_b");
        var port = new NpcStateRomancePort(manager);

        var confirmed = port.ConfirmBond("heroine_a", "bond_node");
        var reloadedPort = new NpcStateRomancePort(manager);
        var reloadedService = new RomanceService(reloadedPort, new MilestoneRegistry(reloadedPort));

        Assert.True(confirmed);
        Assert.Equal("heroine_a", reloadedService.BondedHeroine);
        Assert.True(manager.GetState("heroine_a")!.Flags.ContainsKey(NpcStateRomancePort.BondedHeroineFlag));
        Assert.Equal(RomanceBondStatus.AlreadyBonded, reloadedService.TryBond("heroine_b").Status);
    }

    [Fact]
    public void TryBond_WhenAnotherHeroineAlreadyBondedReturnsAlreadyBondedVariant()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");
        npcState.RegisterEligible("heroine_b");
        service.ConfirmBond("heroine_a", "bond_node");

        var result = service.TryBond("heroine_b");

        Assert.Equal(RomanceBondStatus.AlreadyBonded, result.Status);
        Assert.Equal("already_bonded", result.VariantKey);
        Assert.Equal("heroine_a", result.BondedHeroine);
        Assert.False(npcState.GetMilestones("heroine_b")!.Bond);
    }

    [Fact]
    public void DeclineBond_WhenEnteredAgainReturnsDeclinedPreviouslyAndKeepsBondUnset()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");

        var declined = service.DeclineBond("heroine_a", "choice");
        var repeated = service.TryBond("heroine_a");

        Assert.Equal(RomanceBondStatus.DeclinedPreviously, declined.Status);
        Assert.Equal(RomanceBondStatus.DeclinedPreviously, repeated.Status);
        Assert.Equal("declined_previously", repeated.VariantKey);
        Assert.True(npcState.HasRomanceFlag("heroine_a", RomanceService.GetBondDeclinedFlag("heroine_a")));
        Assert.False(npcState.GetMilestones("heroine_a")!.Bond);
    }

    [Fact]
    public void DeclineBond_WhenPrerequisitesMissingDoesNotWriteDeclineLock()
    {
        var (service, npcState) = CreateService();
        npcState.Register(
            "heroine_a",
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true));

        var result = service.DeclineBond("heroine_a", "choice");

        Assert.Equal(RomanceBondStatus.ConditionsNotMet, result.Status);
        Assert.False(npcState.HasRomanceFlag("heroine_a", RomanceService.GetBondDeclinedFlag("heroine_a")));
    }

    [Fact]
    public void ConfirmBond_WhenPrerequisitesMissingReturnsConditionsNotMet()
    {
        var (service, npcState) = CreateService();
        npcState.Register(
            "heroine_a",
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true));

        var result = service.ConfirmBond("heroine_a", "bond_node");

        Assert.Equal(RomanceBondStatus.ConditionsNotMet, result.Status);
        Assert.Equal("conditions_not_met", result.VariantKey);
        Assert.Equal("None", service.BondedHeroine);
        Assert.False(npcState.GetMilestones("heroine_a")!.Bond);
    }

    [Fact]
    public void DeclineBond_UsesRomancePrefixedStableFlag()
    {
        var (service, npcState) = CreateService();
        npcState.RegisterEligible("heroine_a");

        service.DeclineBond("heroine_a", "choice");

        Assert.Contains(RomanceService.GetBondDeclinedFlag("heroine_a"), npcState.Flags["heroine_a"].Keys);
        Assert.StartsWith("romance_", RomanceService.GetBondDeclinedFlag("heroine_a"));
    }

    [Fact]
    public void ConfirmBondAndDeclineBond_PublishTypedEvents()
    {
        var bus = new EventBus();
        var (service, npcState) = CreateService(bus);
        npcState.RegisterEligible("heroine_a");
        RomanceBondConfirmedEvent? confirmed = null;
        bus.Subscribe<RomanceBondConfirmedEvent>(e => confirmed = e);

        service.ConfirmBond("heroine_a", "bond_node");

        Assert.NotNull(confirmed);
        Assert.Equal("heroine_a", confirmed!.NpcId);
        Assert.Equal("bond_node", confirmed.Source);

        var declineBus = new EventBus();
        var (declineService, declineNpcState) = CreateService(declineBus);
        declineNpcState.RegisterEligible("heroine_b");
        RomanceBondDeclinedEvent? declined = null;
        declineBus.Subscribe<RomanceBondDeclinedEvent>(e => declined = e);

        declineService.DeclineBond("heroine_b", "choice");

        Assert.NotNull(declined);
        Assert.Equal("heroine_b", declined!.NpcId);
        Assert.Equal(RomanceService.GetBondDeclinedFlag("heroine_b"), declined.DeclineFlag);
        Assert.Equal("choice", declined.Source);
    }

    private static (RomanceService Service, RecordingRomanceNpcStatePort NpcState) CreateService(IEventBus? bus = null)
    {
        var npcState = new RecordingRomanceNpcStatePort();
        var registry = new MilestoneRegistry(npcState);
        return (new RomanceService(npcState, registry, bus ?? new EventBus()), npcState);
    }

    private sealed class RecordingRomanceNpcStatePort : IRomanceNpcStatePort
    {
        private readonly Dictionary<string, AttitudeLevel> _attitudes = new();
        private readonly Dictionary<string, RomanceMilestoneState> _milestones = new();

        public Dictionary<string, Dictionary<string, string>> Flags { get; } = new();
        public List<(string NpcId, RomanceMilestone Milestone, bool Value, string Source)> MilestoneWrites { get; } = new();
        public bool FailGlobalBondWrite { get; set; }

        public void RegisterEligible(string npcId)
        {
            Register(
                npcId,
                AttitudeLevel.LifeDeath,
                new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true));
        }

        public void Register(string npcId, AttitudeLevel attitude, RomanceMilestoneState milestones)
        {
            _attitudes[npcId] = attitude;
            _milestones[npcId] = milestones;
            Flags[npcId] = new Dictionary<string, string>();
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
            if (!_attitudes.ContainsKey(npcId) || !_milestones.TryGetValue(npcId, out var current))
                return false;

            _milestones[npcId] = current with { Broken = true };
            _attitudes[npcId] = terminalAttitude;
            return true;
        }

        public string? GetBondedHeroine()
        {
            return Flags.FirstOrDefault(pair => pair.Value.ContainsKey(NpcStateRomancePort.BondedHeroineFlag)).Key;
        }

        public bool ConfirmBond(string npcId, string source)
        {
            if (GetBondedHeroine() != null) return false;
            if (!_milestones.TryGetValue(npcId, out var current)) return false;

            _milestones[npcId] = current with { Bond = true };
            MilestoneWrites.Add((npcId, RomanceMilestone.Bond, true, source));
            if (FailGlobalBondWrite)
            {
                _milestones[npcId] = current;
                return false;
            }

            return SetRomanceFlag(npcId, NpcStateRomancePort.BondedHeroineFlag, "true", source);
        }

        public bool HasRomanceFlag(string npcId, string key)
        {
            return Flags.TryGetValue(npcId, out var flags) && flags.ContainsKey(key);
        }

        public bool SetRomanceFlag(string npcId, string key, string value, string source)
        {
            if (!Flags.TryGetValue(npcId, out var flags)) return false;
            if (!key.StartsWith("romance_", StringComparison.Ordinal))
                throw new ArgumentException("Romance flags must use the romance_ prefix.", nameof(key));

            flags[key] = value;
            return true;
        }
    }
}
