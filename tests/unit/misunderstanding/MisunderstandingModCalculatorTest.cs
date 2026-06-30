using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingModCalculatorTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 3);
    private readonly MockNpcStateWriter _writer = new();
    private readonly MisunderstandingModCalculator _calc;

    public MisunderstandingModCalculatorTest()
    {
        _calc = new MisunderstandingModCalculator(_registry, _writer);
    }

    private MisunderstandingInstance MakeActive(string id, string npc, Severity severity)
    {
        var inst = new MisunderstandingInstance
        {
            Id = id,
            TargetNpc = npc,
            SourceType = SourceType.JianghuEvent,
            Severity = severity,
            InitialWindow = 7,
            WindowRemaining = 7,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        inst.State = MisunderstandingState.Active;
        return inst;
    }

    [Fact]
    public void NoActive_ReturnsZero()
    {
        var mod = _calc.ComputeMod("npc1");
        Assert.Equal(0, mod);
    }

    [Fact]
    public void SingleMinor_ReturnsMinusOne()
    {
        var inst = MakeActive("a", "npc1", Severity.Minor);
        _registry.TryRegister(inst);

        Assert.Equal(-1, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void SingleModerate_ReturnsMinusTwo()
    {
        var inst = MakeActive("a", "npc1", Severity.Moderate);
        _registry.TryRegister(inst);

        Assert.Equal(-2, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void SingleSevere_ReturnsMinusTwo()
    {
        var inst = MakeActive("a", "npc1", Severity.Severe);
        _registry.TryRegister(inst);

        Assert.Equal(-2, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void MinorPlusModerate_TakesMax_ReturnsMinusTwo()
    {
        _registry.TryRegister(MakeActive("a", "npc1", Severity.Minor));
        _registry.TryRegister(MakeActive("b", "npc1", Severity.Moderate));

        Assert.Equal(-2, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void ResolvedInstance_NotIncluded()
    {
        var inst = MakeActive("a", "npc1", Severity.Moderate);
        inst.State = MisunderstandingState.Resolved;
        _registry.TryRegister(inst);

        Assert.Equal(0, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void DormantInstance_NotIncluded()
    {
        var inst = new MisunderstandingInstance
        {
            Id = "d",
            TargetNpc = "npc1",
            SourceType = SourceType.Absence,
            Severity = Severity.Moderate,
            InitialWindow = 5,
            WindowRemaining = 5,
            CreatedChapter = 0,
            CreatedDay = 1,
        };
        _registry.TryRegister(inst);

        Assert.Equal(0, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void PermanentInstance_Included()
    {
        var inst = MakeActive("p", "npc1", Severity.Minor);
        inst.State = MisunderstandingState.Permanent;
        _registry.TryRegister(inst);

        Assert.Equal(-1, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void RecomputeAndWrite_CallsWriter()
    {
        _registry.TryRegister(MakeActive("a", "npc1", Severity.Minor));

        var mod = _calc.RecomputeAndWrite("npc1");

        Assert.Equal(-1, mod);
        Assert.Equal(-1, _writer.LastValue("npc1"));
    }

    [Fact]
    public void RecomputeAndWrite_Idempotent()
    {
        _registry.TryRegister(MakeActive("a", "npc1", Severity.Moderate));

        _calc.RecomputeAndWrite("npc1");
        _calc.RecomputeAndWrite("npc1");

        Assert.Equal(-2, _writer.LastValue("npc1"));
        Assert.Equal(2, _writer.CallCount("npc1"));
    }

    [Fact]
    public void MaxActive_RejectsExcess()
    {
        _registry.TryRegister(MakeActive("a", "npc1", Severity.Minor));
        _registry.TryRegister(MakeActive("b", "npc1", Severity.Minor));
        _registry.TryRegister(MakeActive("c", "npc1", Severity.Minor));

        var excess = MakeActive("d", "npc1", Severity.Moderate);
        Assert.False(_registry.TryRegister(excess));
        Assert.Equal(-1, _calc.ComputeMod("npc1"));
    }

    [Fact]
    public void ModAlwaysClamped_EvenWithMultipleSevere()
    {
        _registry.TryRegister(MakeActive("a", "npc1", Severity.Severe));
        _registry.TryRegister(MakeActive("b", "npc1", Severity.Severe));
        _registry.TryRegister(MakeActive("c", "npc1", Severity.Severe));

        var mod = _calc.ComputeMod("npc1");
        Assert.True(mod >= -2 && mod <= 0);
        Assert.Equal(-2, mod);
    }

    private sealed class MockNpcStateWriter : INpcStateWriter
    {
        private readonly Dictionary<string, int> _values = new();
        private readonly Dictionary<string, int> _counts = new();

        public void SetMisunderstandingMod(string npcId, int value)
        {
            _values[npcId] = value;
            _counts[npcId] = _counts.GetValueOrDefault(npcId) + 1;
        }

        public void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays) { }

        public int LastValue(string npcId) => _values.GetValueOrDefault(npcId);
        public int CallCount(string npcId) => _counts.GetValueOrDefault(npcId);
    }
}
