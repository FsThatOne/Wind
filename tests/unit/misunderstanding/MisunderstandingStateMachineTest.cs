using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingStateMachineTest
{
    private readonly MisunderstandingStateMachine _sm = new();

    private static MisunderstandingInstance CreateInstance(
        Severity severity = Severity.Minor,
        int window = 7,
        MisunderstandingState state = MisunderstandingState.Dormant)
    {
        var inst = new MisunderstandingInstance
        {
            Id = $"test_{Guid.NewGuid():N}",
            TargetNpc = "bailing",
            SourceType = SourceType.JianghuEvent,
            Severity = severity,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        inst.State = state;
        return inst;
    }

    [Fact]
    public void Activate_FromDormant_Succeeds()
    {
        var inst = CreateInstance();
        _sm.Activate(inst);
        Assert.Equal(MisunderstandingState.Active, inst.State);
    }

    [Fact]
    public void Activate_FromNonDormant_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Active);
        var ex = Assert.Throws<InvalidStateTransitionException>(() => _sm.Activate(inst));
        Assert.Equal(MisunderstandingState.Active, ex.FromState);
    }

    [Fact]
    public void Escalate_FromActive_MinorToModerate()
    {
        var inst = CreateInstance(severity: Severity.Minor, state: MisunderstandingState.Active);
        _sm.Escalate(inst);

        Assert.Equal(Severity.Moderate, inst.Severity);
        Assert.Equal(MisunderstandingState.Escalated, inst.State);
        Assert.Equal(1, inst.EscalationCount);
    }

    [Fact]
    public void Escalate_ModerateToSevere_ClampsWindow()
    {
        var inst = CreateInstance(severity: Severity.Moderate, window: 5, state: MisunderstandingState.Active);
        _sm.Escalate(inst);

        Assert.Equal(Severity.Severe, inst.Severity);
        Assert.Equal(3, inst.WindowRemaining);
        Assert.Equal(MisunderstandingState.Escalated, inst.State);
    }

    [Fact]
    public void Escalate_ModerateToSevere_WindowAlreadyBelow3_KeepsLower()
    {
        var inst = CreateInstance(severity: Severity.Moderate, window: 2, state: MisunderstandingState.Active);
        inst.WindowRemaining = 2;
        _sm.Escalate(inst);

        Assert.Equal(Severity.Severe, inst.Severity);
        Assert.Equal(2, inst.WindowRemaining);
    }

    [Fact]
    public void Escalate_SevereStaysSevere()
    {
        var inst = CreateInstance(severity: Severity.Severe, window: 3, state: MisunderstandingState.Escalated);
        inst.EscalationCount = 1;
        _sm.Escalate(inst);

        Assert.Equal(Severity.Severe, inst.Severity);
        Assert.Equal(2, inst.EscalationCount);
    }

    [Fact]
    public void Escalate_FromResolved_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Resolved);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.Escalate(inst));
    }

    [Fact]
    public void Resolve_FromActive_Succeeds()
    {
        var inst = CreateInstance(state: MisunderstandingState.Active);
        _sm.Resolve(inst);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
    }

    [Fact]
    public void Resolve_FromEscalated_Succeeds()
    {
        var inst = CreateInstance(state: MisunderstandingState.Escalated);
        _sm.Resolve(inst);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
    }

    [Fact]
    public void Resolve_FromPermanent_Succeeds()
    {
        var inst = CreateInstance(state: MisunderstandingState.Permanent);
        _sm.Resolve(inst);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
    }

    [Fact]
    public void Resolve_FromDormant_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Dormant);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.Resolve(inst));
    }

    [Fact]
    public void Resolve_FromBroken_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Broken);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.Resolve(inst));
    }

    [Fact]
    public void MakePermanent_FromActive_NonSevere_Succeeds()
    {
        var inst = CreateInstance(severity: Severity.Minor, state: MisunderstandingState.Active);
        _sm.MakePermanent(inst);
        Assert.Equal(MisunderstandingState.Permanent, inst.State);
    }

    [Fact]
    public void MakePermanent_Severe_Throws()
    {
        var inst = CreateInstance(severity: Severity.Severe, state: MisunderstandingState.Active);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.MakePermanent(inst));
    }

    [Fact]
    public void Break_FromActive_Succeeds()
    {
        var inst = CreateInstance(state: MisunderstandingState.Active);
        _sm.Break(inst);
        Assert.Equal(MisunderstandingState.Broken, inst.State);
    }

    [Fact]
    public void Break_FromPermanent_Succeeds()
    {
        var inst = CreateInstance(state: MisunderstandingState.Permanent);
        _sm.Break(inst);
        Assert.Equal(MisunderstandingState.Broken, inst.State);
    }

    [Fact]
    public void Break_FromResolved_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Resolved);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.Break(inst));
    }

    [Fact]
    public void Break_FromBroken_Throws()
    {
        var inst = CreateInstance(state: MisunderstandingState.Broken);
        Assert.Throws<InvalidStateTransitionException>(() => _sm.Break(inst));
    }

    // --- Registry tests ---

    [Fact]
    public void Registry_Register_Succeeds()
    {
        var registry = new MisunderstandingRegistry(maxActivePerNpc: 3);
        var inst = CreateInstance(state: MisunderstandingState.Active);
        Assert.True(registry.TryRegister(inst));
        Assert.True(registry.TryGet(inst.Id, out var found));
        Assert.Same(inst, found);
    }

    [Fact]
    public void Registry_BrokenNpc_RejectsNew()
    {
        var registry = new MisunderstandingRegistry(maxActivePerNpc: 3);
        var broken = CreateInstance(state: MisunderstandingState.Broken);
        broken = new MisunderstandingInstance
        {
            Id = "broken_1",
            TargetNpc = "bailing",
            SourceType = SourceType.Absence,
            Severity = Severity.Severe,
            InitialWindow = 3,
            WindowRemaining = 0,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        broken.State = MisunderstandingState.Broken;
        registry.TryRegister(broken);

        var newInst = CreateInstance();
        Assert.False(registry.TryRegister(newInst));
    }

    [Fact]
    public void Registry_MaxActive_RejectsExcess()
    {
        var registry = new MisunderstandingRegistry(maxActivePerNpc: 2);

        var a = new MisunderstandingInstance { Id = "a", TargetNpc = "npc1", SourceType = SourceType.JianghuEvent, Severity = Severity.Minor, InitialWindow = 7, WindowRemaining = 7, CreatedChapter = 0, CreatedDay = 1 };
        a.State = MisunderstandingState.Active;
        var b = new MisunderstandingInstance { Id = "b", TargetNpc = "npc1", SourceType = SourceType.DialogueChoice, Severity = Severity.Moderate, InitialWindow = 5, WindowRemaining = 5, CreatedChapter = 0, CreatedDay = 2 };
        b.State = MisunderstandingState.Active;
        var c = new MisunderstandingInstance { Id = "c", TargetNpc = "npc1", SourceType = SourceType.Absence, Severity = Severity.Minor, InitialWindow = 7, WindowRemaining = 7, CreatedChapter = 0, CreatedDay = 3 };
        c.State = MisunderstandingState.Active;

        Assert.True(registry.TryRegister(a));
        Assert.True(registry.TryRegister(b));
        Assert.False(registry.TryRegister(c));
    }

    [Fact]
    public void Registry_GetActive_FiltersCorrectly()
    {
        var registry = new MisunderstandingRegistry();

        var active = new MisunderstandingInstance { Id = "x1", TargetNpc = "npc1", SourceType = SourceType.JianghuEvent, Severity = Severity.Minor, InitialWindow = 7, WindowRemaining = 7, CreatedChapter = 0, CreatedDay = 1 };
        active.State = MisunderstandingState.Active;
        var resolved = new MisunderstandingInstance { Id = "x2", TargetNpc = "npc1", SourceType = SourceType.DialogueChoice, Severity = Severity.Minor, InitialWindow = 7, WindowRemaining = 0, CreatedChapter = 0, CreatedDay = 2 };
        resolved.State = MisunderstandingState.Resolved;

        registry.TryRegister(active);
        registry.TryRegister(resolved);

        var result = registry.GetActive("npc1");
        Assert.Single(result);
        Assert.Equal("x1", result[0].Id);
    }
}
