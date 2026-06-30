using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class FloorProtectionTest
{
    private readonly MockRomanceService _romance = new();
    private readonly MockDialogueEffectWriter _effectWriter = new();
    private readonly FloorProtectionService _service;

    public FloorProtectionTest()
    {
        _service = new FloorProtectionService(_romance, _effectWriter);
    }

    [Fact]
    public void FloorEquals_CurrentScore_ModClamped()
    {
        _romance.SetState("npc1", floor: 5, score: 5);

        int effective = _service.ComputeEffectiveMod("npc1", rawMod: -2);

        Assert.Equal(0, effective);
    }

    [Fact]
    public void FloorBelow_CurrentScore_ModAppliedNormally()
    {
        _romance.SetState("npc1", floor: 3, score: 5);

        int effective = _service.ComputeEffectiveMod("npc1", rawMod: -2);

        Assert.Equal(-2, effective);
    }

    [Fact]
    public void FloorPartialClamp_ModReducedButNotZero()
    {
        _romance.SetState("npc1", floor: 4, score: 5);

        int effective = _service.ComputeEffectiveMod("npc1", rawMod: -2);

        Assert.Equal(-1, effective);
    }

    [Fact]
    public void FloorClamped_DialogueEffectStillWritten()
    {
        _romance.SetState("npc1", floor: 5, score: 5);

        _service.ComputeEffectiveMod("npc1", rawMod: -2);

        Assert.Equal(2, _effectWriter.LastEffectLevel("npc1"));
    }

    [Fact]
    public void NoClamping_DialogueEffectNotWritten()
    {
        _romance.SetState("npc1", floor: 0, score: 5);

        _service.ComputeEffectiveMod("npc1", rawMod: -2);

        Assert.Equal(0, _effectWriter.LastEffectLevel("npc1"));
    }

    private sealed class MockRomanceService : IRomanceService
    {
        private readonly Dictionary<string, (int floor, int score)> _states = new();

        public void SetState(string npcId, int floor, int score) => _states[npcId] = (floor, score);
        public int GetMilestoneFloor(string npcId) => _states.TryGetValue(npcId, out var s) ? s.floor : 0;
        public int GetAttitudeScore(string npcId) => _states.TryGetValue(npcId, out var s) ? s.score : 0;
        public void ForceBreak(string npcId) { }
        public bool IsBrokenState(string npcId) => false;
    }

    private sealed class MockDialogueEffectWriter : IDialogueEffectWriter
    {
        private readonly Dictionary<string, int> _effects = new();

        public void SetMisunderstandingEffect(string npcId, int effectLevel) => _effects[npcId] = effectLevel;
        public int LastEffectLevel(string npcId) => _effects.GetValueOrDefault(npcId);
    }
}

public class ForceBreakIntegrationTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 5);
    private readonly MisunderstandingStateMachine _sm = new();
    private readonly MockRomanceService _romance = new();
    private readonly ForceBreakIntegration _forceBreak;

    public ForceBreakIntegrationTest()
    {
        _forceBreak = new ForceBreakIntegration(_romance, _registry, _sm);
    }

    private MisunderstandingInstance Register(string id, string npc, Severity sev)
    {
        var inst = new MisunderstandingInstance
        {
            Id = id,
            TargetNpc = npc,
            SourceType = SourceType.JianghuEvent,
            Severity = sev,
            InitialWindow = 3,
            WindowRemaining = 0,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        inst.State = MisunderstandingState.Active;
        _registry.TryRegister(inst);
        return inst;
    }

    [Fact]
    public void ForceBreak_CallsRomanceAndBreaksAllActive()
    {
        var inst1 = Register("a", "npc1", Severity.Severe);
        var inst2 = Register("b", "npc1", Severity.Minor);

        _forceBreak.ForceBreak("npc1");

        Assert.True(_romance.WasForceBreakCalled("npc1"));
        Assert.Equal(MisunderstandingState.Broken, inst1.State);
        Assert.Equal(MisunderstandingState.Broken, inst2.State);
    }

    [Fact]
    public void ForceBreak_AlreadyBroken_NotCalledAgain()
    {
        _romance.SetBroken("npc1");

        _forceBreak.ForceBreak("npc1");

        Assert.False(_romance.WasForceBreakCalled("npc1"));
    }

    [Fact]
    public void ForceBreak_WithPositiveMilestone_ForceBreakWins()
    {
        Register("a", "npc1", Severity.Severe);

        _forceBreak.ForceBreak("npc1");

        Assert.True(_romance.WasForceBreakCalled("npc1"));
    }

    private sealed class MockRomanceService : IRomanceService
    {
        private readonly HashSet<string> _broken = new();
        private readonly HashSet<string> _forceBreakCalled = new();

        public void SetBroken(string npcId) => _broken.Add(npcId);
        public int GetMilestoneFloor(string npcId) => 0;
        public int GetAttitudeScore(string npcId) => 5;
        public void ForceBreak(string npcId) => _forceBreakCalled.Add(npcId);
        public bool IsBrokenState(string npcId) => _broken.Contains(npcId);
        public bool WasForceBreakCalled(string npcId) => _forceBreakCalled.Contains(npcId);
    }
}
