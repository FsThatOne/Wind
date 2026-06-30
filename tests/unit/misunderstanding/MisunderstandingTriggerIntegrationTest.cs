using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingTriggerIntegrationTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 5);
    private readonly MisunderstandingStateMachine _sm = new();
    private readonly MockNpcStateWriter _writer = new();
    private readonly MockScenePresence _presence = new();
    private readonly MisunderstandingConfig _config = new();
    private readonly AbsenceDetector _absenceDetector;
    private readonly MisunderstandingTriggerService _service;

    public MisunderstandingTriggerIntegrationTest()
    {
        var modCalc = new MisunderstandingModCalculator(_registry, _writer);
        _absenceDetector = new AbsenceDetector(_presence, _config);
        _service = new MisunderstandingTriggerService(_registry, _sm, modCalc, _absenceDetector, _config);
    }

    [Fact]
    public void WorldEvent_MatchingTrigger_CreatesActiveInstance()
    {
        _service.RegisterWorldEventTrigger(new TriggerConfig
        {
            TriggerId = "t1",
            TargetNpc = "npc1",
            SourceType = SourceType.JianghuEvent,
            Severity = Severity.Moderate,
            MatchEventId = "evt_rumor_spread",
        });

        var inst = _service.OnWorldEvent("evt_rumor_spread", currentChapter: 0, currentDay: 5);

        Assert.NotNull(inst);
        Assert.Equal(MisunderstandingState.Active, inst.State);
        Assert.Equal(Severity.Moderate, inst.Severity);
        Assert.Equal(SourceType.JianghuEvent, inst.SourceType);
        Assert.Equal("npc1", inst.TargetNpc);
    }

    [Fact]
    public void WorldEvent_NoMatch_ReturnsNull()
    {
        _service.RegisterWorldEventTrigger(new TriggerConfig
        {
            TriggerId = "t1",
            TargetNpc = "npc1",
            SourceType = SourceType.JianghuEvent,
            Severity = Severity.Minor,
            MatchEventId = "evt_a",
        });

        var inst = _service.OnWorldEvent("evt_b", currentChapter: 0, currentDay: 1);

        Assert.Null(inst);
        Assert.Empty(_registry.GetAll("npc1"));
    }

    [Fact]
    public void DialogueChoice_CreatesInstanceWithCorrectSourceType()
    {
        var triggers = new List<DialogueMisTrigger>
        {
            new() { NpcId = "npc1", Severity = Severity.Minor },
        };

        var created = _service.OnDialogueChoice(triggers, currentChapter: 0, currentDay: 3);

        Assert.Single(created);
        Assert.Equal(SourceType.DialogueChoice, created[0].SourceType);
        Assert.Equal("npc1", created[0].TargetNpc);
    }

    [Fact]
    public void Absence_After3Days_TriggersMisunderstanding()
    {
        _presence.SetColocated("npc1", false);

        // tick 3 天
        List<MisunderstandingInstance> result = new();
        for (int day = 1; day <= 3; day++)
            result = _service.OnDayAdvancedAbsence(new[] { "npc1" }, currentChapter: 0, currentDay: day);

        Assert.Single(result);
        Assert.Equal(SourceType.Absence, result[0].SourceType);
        Assert.Equal(Severity.Minor, result[0].Severity);
    }

    [Fact]
    public void Absence_PlayerColocated_DoesNotTrigger()
    {
        _presence.SetColocated("npc1", true);

        List<MisunderstandingInstance> result = new();
        for (int day = 1; day <= 5; day++)
            result = _service.OnDayAdvancedAbsence(new[] { "npc1" }, currentChapter: 0, currentDay: day);

        Assert.Empty(result);
    }

    [Fact]
    public void DialogueChoice_TwoNpcs_CreatesIndependentInstances()
    {
        var triggers = new List<DialogueMisTrigger>
        {
            new() { NpcId = "npc1", Severity = Severity.Minor },
            new() { NpcId = "npc2", Severity = Severity.Moderate },
        };

        var created = _service.OnDialogueChoice(triggers, currentChapter: 0, currentDay: 1);

        Assert.Equal(2, created.Count);
        Assert.Equal("npc1", created[0].TargetNpc);
        Assert.Equal("npc2", created[1].TargetNpc);
        Assert.Equal(Severity.Minor, created[0].Severity);
        Assert.Equal(Severity.Moderate, created[1].Severity);
    }

    [Fact]
    public void DefeatImmunity_SkipsAbsenceTrigger()
    {
        _presence.SetColocated("npc1", false);
        _absenceDetector.ApplyDefeatImmunity("npc1", days: 2);

        // 保护期第 1 天
        var r1 = _service.OnDayAdvancedAbsence(new[] { "npc1" }, currentChapter: 0, currentDay: 1);
        Assert.Empty(r1);

        // 保护期第 2 天（immunity 过期，开始计缺席 day=1）
        var r2 = _service.OnDayAdvancedAbsence(new[] { "npc1" }, currentChapter: 0, currentDay: 2);
        Assert.Empty(r2);

        // 保护期结束后继续积累缺席直到触发
        var allTriggered = new List<MisunderstandingInstance>();
        for (int day = 3; day <= 6; day++)
        {
            var result = _service.OnDayAdvancedAbsence(new[] { "npc1" }, currentChapter: 0, currentDay: day);
            allTriggered.AddRange(result);
        }

        Assert.Single(allTriggered);
    }

    private sealed class MockNpcStateWriter : INpcStateWriter
    {
        private readonly Dictionary<string, int> _mods = new();
        public void SetMisunderstandingMod(string npcId, int value) => _mods[npcId] = value;
        public void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays) { }
    }

    private sealed class MockScenePresence : IScenePresenceQuery
    {
        private readonly Dictionary<string, bool> _colocated = new();

        public void SetColocated(string npcId, bool value) => _colocated[npcId] = value;
        public bool IsPlayerColocatedWith(string npcId) => _colocated.GetValueOrDefault(npcId);
    }
}
