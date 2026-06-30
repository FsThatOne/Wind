using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class TransparencyProgressionTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 5);
    private readonly MockSignalEmitter _emitter = new();
    private readonly MisunderstandingConfig _config = new();
    private readonly TransparencyProgressionManager _mgr;

    public TransparencyProgressionTest()
    {
        _mgr = new TransparencyProgressionManager(_registry, _emitter, _config);
    }

    private MisunderstandingInstance Register(string id, string npc, int window, int createdDay = 1)
    {
        var inst = new MisunderstandingInstance
        {
            Id = id,
            TargetNpc = npc,
            SourceType = SourceType.JianghuEvent,
            Severity = Severity.Minor,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = 0,
            CreatedDay = createdDay,
        };
        inst.State = MisunderstandingState.Active;
        _registry.TryRegister(inst);
        return inst;
    }

    [Fact]
    public void InitialTransparency_IsHidden()
    {
        var inst = Register("a", "npc1", 7, createdDay: 1);
        Assert.Equal(Transparency.Hidden, inst.Transparency);
    }

    [Fact]
    public void After1Day_HiddenToHinted()
    {
        var inst = Register("a", "npc1", 7, createdDay: 1);
        _mgr.CheckAll(new[] { "npc1" }, currentDay: 2);

        Assert.Equal(Transparency.Hinted, inst.Transparency);
        Assert.Single(_emitter.HintCalls);
        Assert.Equal("npc1", _emitter.HintCalls[0].NpcId);
    }

    [Fact]
    public void HalfWindow_HintedToPerceived()
    {
        var inst = Register("a", "npc1", 6, createdDay: 1);
        // hidden_duration=1, perceived_threshold=0.5 → halfWindow = 6*0.5 = 3
        _mgr.CheckAll(new[] { "npc1" }, currentDay: 2); // day 2: elapsed=1 ≥ hidden(1) → HINTED
        _mgr.CheckAll(new[] { "npc1" }, currentDay: 4); // day 4: elapsed=3 ≥ half(3) → PERCEIVED

        Assert.Equal(Transparency.Perceived, inst.Transparency);
        Assert.Single(_emitter.PerceivedCalls);
    }

    [Fact]
    public void WindowRemaining3_PerceivedToUrgent()
    {
        var inst = Register("a", "npc1", 6, createdDay: 1);
        inst.Transparency = Transparency.Perceived;
        inst.WindowRemaining = 3;

        _mgr.CheckAll(new[] { "npc1" }, currentDay: 5);

        Assert.Equal(Transparency.Urgent, inst.Transparency);
        Assert.Single(_emitter.UrgentCalls);
    }

    [Fact]
    public void UrgentStage_NoRepeatedEmit()
    {
        var inst = Register("a", "npc1", 6, createdDay: 1);
        inst.Transparency = Transparency.Perceived;
        inst.WindowRemaining = 3;

        _mgr.CheckAll(new[] { "npc1" }, currentDay: 5);
        _mgr.CheckAll(new[] { "npc1" }, currentDay: 6);

        Assert.Single(_emitter.UrgentCalls);
    }

    [Fact]
    public void PlayerInteraction_HiddenToHinted()
    {
        var inst = Register("a", "npc1", 7, createdDay: 1);
        _mgr.OnPlayerInteraction("npc1");

        Assert.Equal(Transparency.Hinted, inst.Transparency);
        Assert.Single(_emitter.HintCalls);
    }

    [Fact]
    public void PlayerInteraction_AlreadyHinted_NoChange()
    {
        var inst = Register("a", "npc1", 7, createdDay: 1);
        inst.Transparency = Transparency.Hinted;

        _mgr.OnPlayerInteraction("npc1");

        Assert.Equal(Transparency.Hinted, inst.Transparency);
        Assert.Empty(_emitter.HintCalls);
    }

    [Fact]
    public void HiddenDurationZero_ImmediateHinted()
    {
        var config = new MisunderstandingConfig { HiddenDurationDays = 0 };
        var mgr = new TransparencyProgressionManager(_registry, _emitter, config);

        var inst = Register("b", "npc2", 7, createdDay: 1);
        mgr.CheckAll(new[] { "npc2" }, currentDay: 1); // elapsed=0 ≥ hidden(0)

        Assert.Equal(Transparency.Hinted, inst.Transparency);
    }

    [Fact]
    public void FullProgression_InSequence()
    {
        var inst = Register("a", "npc1", 8, createdDay: 0);
        // hidden_duration=1 → day 1: HINTED
        // halfWindow = 8*0.5 = 4 → day 4: PERCEIVED
        // window remains ≤ 3: need window to decrement externally

        _mgr.CheckAll(new[] { "npc1" }, currentDay: 1);
        Assert.Equal(Transparency.Hinted, inst.Transparency);

        _mgr.CheckAll(new[] { "npc1" }, currentDay: 4);
        Assert.Equal(Transparency.Perceived, inst.Transparency);

        inst.WindowRemaining = 2;
        _mgr.CheckAll(new[] { "npc1" }, currentDay: 6);
        Assert.Equal(Transparency.Urgent, inst.Transparency);

        Assert.Single(_emitter.HintCalls);
        Assert.Single(_emitter.PerceivedCalls);
        Assert.Single(_emitter.UrgentCalls);
    }

    [Fact]
    public void ResolvedInstance_NotProcessed()
    {
        var inst = Register("a", "npc1", 7, createdDay: 1);
        inst.State = MisunderstandingState.Resolved;

        _mgr.CheckAll(new[] { "npc1" }, currentDay: 10);
        Assert.Equal(Transparency.Hidden, inst.Transparency);
        Assert.Empty(_emitter.HintCalls);
    }

    private sealed class MockSignalEmitter : ITransparencySignalEmitter
    {
        public List<(string NpcId, string InstId)> HintCalls { get; } = new();
        public List<(string NpcId, string InstId)> PerceivedCalls { get; } = new();
        public List<(string NpcId, string InstId)> UrgentCalls { get; } = new();

        public void EmitHint(string npcId, string instanceId) => HintCalls.Add((npcId, instanceId));
        public void EmitPerceived(string npcId, string instanceId) => PerceivedCalls.Add((npcId, instanceId));
        public void EmitUrgent(string npcId, string instanceId) => UrgentCalls.Add((npcId, instanceId));
    }
}
