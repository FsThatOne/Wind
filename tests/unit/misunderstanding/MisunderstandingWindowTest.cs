using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingWindowTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 5);
    private readonly MisunderstandingStateMachine _sm = new();
    private readonly MockNpcStateWriter _writer = new();
    private readonly MockForceBreakHandler _breakHandler = new();
    private readonly MisunderstandingWindowManager _windowMgr;

    public MisunderstandingWindowTest()
    {
        var calc = new MisunderstandingModCalculator(_registry, _writer);
        _windowMgr = new MisunderstandingWindowManager(_registry, _sm, calc, _breakHandler);
    }

    private MisunderstandingInstance Register(string id, string npc, Severity sev, int window)
    {
        var inst = new MisunderstandingInstance
        {
            Id = id,
            TargetNpc = npc,
            SourceType = SourceType.JianghuEvent,
            Severity = sev,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        inst.State = MisunderstandingState.Active;
        _registry.TryRegister(inst);
        return inst;
    }

    private void Tick(string npc, int days = 1)
    {
        for (int i = 0; i < days; i++)
            _windowMgr.OnDayAdvanced(new[] { npc });
    }

    [Fact]
    public void MinorWindow3_After3Days_Permanent()
    {
        var inst = Register("a", "npc1", Severity.Minor, 3);
        Tick("npc1", 3);

        Assert.Equal(MisunderstandingState.Permanent, inst.State);
        Assert.Equal(0, inst.WindowRemaining);
    }

    [Fact]
    public void ModerateWindow5_After5Days_Permanent()
    {
        var inst = Register("a", "npc1", Severity.Moderate, 5);
        Tick("npc1", 5);

        Assert.Equal(MisunderstandingState.Permanent, inst.State);
    }

    [Fact]
    public void SevereWindow1_After1Day_BrokenAndForceBreakCalled()
    {
        var inst = Register("a", "npc1", Severity.Severe, 1);
        Tick("npc1", 1);

        Assert.Equal(MisunderstandingState.Broken, inst.State);
        Assert.Contains("npc1", _breakHandler.BrokenNpcs);
    }

    [Fact]
    public void SevereWindow3_After3Days_Broken()
    {
        var inst = Register("a", "npc1", Severity.Severe, 3);
        Tick("npc1", 3);

        Assert.Equal(MisunderstandingState.Broken, inst.State);
        Assert.Single(_breakHandler.BrokenNpcs);
    }

    [Fact]
    public void Window5_After2Days_StillActive()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        Tick("npc1", 2);

        Assert.Equal(MisunderstandingState.Active, inst.State);
        Assert.Equal(3, inst.WindowRemaining);
    }

    [Fact]
    public void CatchUpDays_SimulatesSaveLoad()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        _windowMgr.CatchUpDays(new[] { "npc1" }, 3);

        Assert.Equal(MisunderstandingState.Active, inst.State);
        Assert.Equal(2, inst.WindowRemaining);
    }

    [Fact]
    public void CatchUpDays_SevereWindow2_BreaksOnDay2NotDay3()
    {
        var inst = Register("a", "npc1", Severity.Severe, 2);
        _windowMgr.CatchUpDays(new[] { "npc1" }, 5);

        Assert.Equal(MisunderstandingState.Broken, inst.State);
        Assert.Single(_breakHandler.BrokenNpcs);
    }

    [Fact]
    public void CatchUpDays_MultipleInstances_IndependentWindows()
    {
        var minor = Register("a", "npc1", Severity.Minor, 2);
        var severe = Register("b", "npc1", Severity.Severe, 4);

        _windowMgr.CatchUpDays(new[] { "npc1" }, 3);

        Assert.Equal(MisunderstandingState.Permanent, minor.State);
        Assert.Equal(MisunderstandingState.Active, severe.State);
        Assert.Equal(1, severe.WindowRemaining);
    }

    [Fact]
    public void EdgeCaseE2_EscalateOnLastDay_ThenTickNextDay()
    {
        var inst = Register("a", "npc1", Severity.Minor, 1);
        _sm.Escalate(inst);
        Assert.Equal(Severity.Moderate, inst.Severity);
        Assert.Equal(1, inst.WindowRemaining);

        Tick("npc1", 1);

        Assert.Equal(MisunderstandingState.Permanent, inst.State);
    }

    [Fact]
    public void ResolvedInstance_NotDecremented()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        inst.State = MisunderstandingState.Resolved;

        Tick("npc1", 3);

        Assert.Equal(5, inst.WindowRemaining);
    }

    [Fact]
    public void PermanentInstance_NotDecremented()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        inst.State = MisunderstandingState.Permanent;

        Tick("npc1", 3);

        Assert.Equal(5, inst.WindowRemaining);
    }

    [Fact]
    public void BrokenInstance_NotDecremented()
    {
        var inst = Register("a", "npc1", Severity.Severe, 5);
        inst.State = MisunderstandingState.Broken;

        Tick("npc1", 3);

        Assert.Equal(5, inst.WindowRemaining);
    }

    [Fact]
    public void ModRecomputedAfterPermanent()
    {
        Register("a", "npc1", Severity.Minor, 1);
        Tick("npc1", 1);

        // PERMANENT 仍参与 mod 计算（GDD: 定型后 mod 永久保留）
        Assert.Equal(-1, _writer.LastValue("npc1"));
    }

    private sealed class MockNpcStateWriter : INpcStateWriter
    {
        private readonly Dictionary<string, int> _values = new();
        public void SetMisunderstandingMod(string npcId, int value) => _values[npcId] = value;
        public void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays) { }
        public int LastValue(string npcId) => _values.GetValueOrDefault(npcId);
    }

    private sealed class MockForceBreakHandler : IForceBreakHandler
    {
        public List<string> BrokenNpcs { get; } = new();
        public void ForceBreak(string npcId) => BrokenNpcs.Add(npcId);
    }
}
