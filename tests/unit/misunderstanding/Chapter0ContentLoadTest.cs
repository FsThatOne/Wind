using System.IO;
using System.Linq;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class Chapter0ContentLoadTest
{
    private static readonly string DataRoot = FindDataRoot();

    private static string FindDataRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "feng-zhi", "assets", "data", "misunderstanding");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Path.Combine(Directory.GetCurrentDirectory(), "feng-zhi", "assets", "data", "misunderstanding");
    }

    [Fact]
    public void LoadTriggers_ParsesValidTriggerConfigs()
    {
        var json = File.ReadAllText(Path.Combine(DataRoot, "chapter00", "shixiong_triggers.json"));
        var triggers = MisunderstandingContentLoader.LoadTriggers(json);

        Assert.Equal(3, triggers.Count);
        Assert.Equal("ch0_rumor_survivor", triggers[0].TriggerId);
        Assert.Equal("bai_ling", triggers[0].TargetNpc);
        Assert.Equal(SourceType.JianghuEvent, triggers[0].SourceType);
        Assert.Equal(Severity.Moderate, triggers[0].Severity);
        Assert.Equal("evt_juejiangdu_survivor_rumor", triggers[0].MatchEventId);
    }

    [Fact]
    public void LoadResolutions_ParsesValidConditions()
    {
        var json = File.ReadAllText(Path.Combine(DataRoot, "chapter00", "shixiong_resolution.json"));
        var resolutions = MisunderstandingContentLoader.LoadResolutions(json);

        Assert.Equal(3, resolutions.Count);
        Assert.Equal("ch0_rumor_survivor", resolutions[0].InstancePattern);
        Assert.Equal(2, resolutions[0].ResolutionConditions.Count);
        Assert.Contains("cond_explain_juejiangdu_truth", resolutions[0].ResolutionConditions);
    }

    [Fact]
    public void TriggerConfig_CanDriveService()
    {
        var json = File.ReadAllText(Path.Combine(DataRoot, "chapter00", "shixiong_triggers.json"));
        var triggers = MisunderstandingContentLoader.LoadTriggers(json);

        var registry = new MisunderstandingRegistry(maxActivePerNpc: 5);
        var sm = new MisunderstandingStateMachine();
        var writer = new StubNpcStateWriter();
        var modCalc = new MisunderstandingModCalculator(registry, writer);
        var config = new MisunderstandingConfig();
        var absenceDetector = new AbsenceDetector(new StubPresence(), config);
        var service = new MisunderstandingTriggerService(registry, sm, modCalc, absenceDetector, config);

        var worldTrigger = triggers.First(t => t.SourceType == SourceType.JianghuEvent);
        service.RegisterWorldEventTrigger(MisunderstandingContentLoader.ToTriggerConfig(worldTrigger));

        var inst = service.OnWorldEvent("evt_juejiangdu_survivor_rumor", 0, 1);

        Assert.NotNull(inst);
        Assert.Equal(Severity.Moderate, inst.Severity);
        Assert.Equal(5, inst.InitialWindow);
    }

    [Fact]
    public void AddressTable_HasIntimateAndDistantAddress()
    {
        var json = File.ReadAllText(Path.Combine(DataRoot, "address_tables", "shixiong.json"));
        var table = MisunderstandingContentLoader.LoadAddressTable(json);

        Assert.Equal("bai_ling", table.NpcId);
        Assert.Equal("停云", table.Intimate);
        Assert.Equal("阁下", table.Distant);
        Assert.Equal("……", table.Broken);
    }

    private sealed class StubNpcStateWriter : INpcStateWriter
    {
        public void SetMisunderstandingMod(string npcId, int value) { }
        public void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays) { }
    }

    private sealed class StubPresence : IScenePresenceQuery
    {
        public bool IsPlayerColocatedWith(string npcId) => false;
    }
}
