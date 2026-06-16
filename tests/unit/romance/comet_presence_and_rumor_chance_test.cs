using FengZhi.Foundation.Events;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public class CometPresenceAndRumorChanceTest
{
    [Fact]
    public void CalcRumorChance_WhenAllBonusesApply_ClampsToCap()
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a", journeyRegion: "jiangnan", lastContactDay: 4);
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        var chance = tracker.CalcRumorChance("heroine_a", "jiangnan");

        Assert.Equal(0.8f, chance);
    }

    [Theory]
    [InlineData(null, null, "jiangnan", 0.3f)]
    [InlineData("jiangnan", 12, "jiangnan", 0.7f)]
    [InlineData("beimo", 4, "jiangnan", 0.5f)]
    [InlineData("jiangnan", 4, "jiangnan", 0.8f)]
    public void CalcRumorChance_AppliesSameRegionAndAbsenceRules(
        string? journeyRegion,
        int? lastContactDay,
        string currentRegion,
        float expectedChance)
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a", journeyRegion, lastContactDay);
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        var chance = tracker.CalcRumorChance("heroine_a", currentRegion);

        Assert.Equal(expectedChance, chance, precision: 6);
    }

    [Fact]
    public void CalcRumorChance_WhenAbsenceEqualsThreshold_DoesNotApplyAbsenceBonus()
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a", journeyRegion: null, lastContactDay: 5);
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        var chance = tracker.CalcRumorChance("heroine_a", "jiangnan");

        Assert.Equal(0.3f, chance);
    }

    [Fact]
    public void RecordCometPresence_IncrementsAllCountersExactlyOnce()
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a");
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        Assert.True(tracker.RecordSignDiscovered("heroine_a"));
        Assert.True(tracker.RecordLetterReceived("heroine_a"));
        Assert.True(tracker.RecordRumorHeard("heroine_a"));
        Assert.True(tracker.RecordEncounter("heroine_a"));

        Assert.Equal(new CometPresenceCounters(
            SignsDiscovered: 1,
            LettersReceived: 1,
            RumorsHeard: 1,
            EncountersHad: 1), npcState.GetCometPresenceCounters("heroine_a"));
    }

    [Fact]
    public void RecordSignAndLetter_UpdateLastContactToCurrentWorldDay()
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a");
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        tracker.RecordSignDiscovered("heroine_a", "sign");
        Assert.Equal(12, npcState.GetLastContactDay("heroine_a"));

        var laterTracker = new CometPresenceTracker(npcState, new FixedWorldDay(14), DefaultTuning);
        laterTracker.RecordLetterReceived("heroine_a", "letter");

        Assert.Equal(14, npcState.GetLastContactDay("heroine_a"));
    }

    [Fact]
    public void RecordRumor_DoesNotForceLastContactFlag()
    {
        var npcState = new RecordingCometPresencePort();
        npcState.Register("heroine_a");
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        tracker.RecordRumorHeard("heroine_a", "rumor");

        Assert.Null(npcState.GetLastContactDay("heroine_a"));
        Assert.Equal(1, npcState.GetCometPresenceCounters("heroine_a")!.RumorsHeard);
    }

    [Fact]
    public void RecordCometPresence_WhenNpcMissing_FailsExplicitlyWithoutWriting()
    {
        var npcState = new RecordingCometPresencePort();
        var tracker = new CometPresenceTracker(npcState, new FixedWorldDay(12), DefaultTuning);

        var result = tracker.RecordSignDiscovered("missing");

        Assert.False(result);
        Assert.Null(npcState.GetCometPresenceCounters("missing"));
        Assert.Null(npcState.GetLastContactDay("missing"));
    }

    [Fact]
    public void NpcStateRomancePort_PersistsCountersAndLastContactAsRomanceFlags()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        manager.UpdateFlag("heroine_a", NpcStateRomancePort.JourneyRegionFlag, "jiangnan", "test");
        var port = new NpcStateRomancePort(manager);
        var tracker = new CometPresenceTracker(port, new FixedWorldDay(12), DefaultTuning);

        Assert.True(tracker.RecordSignDiscovered("heroine_a", "sign"));
        Assert.True(tracker.RecordLetterReceived("heroine_a", "letter"));
        Assert.True(tracker.RecordRumorHeard("heroine_a", "rumor"));
        Assert.True(tracker.RecordEncounter("heroine_a", "encounter"));
        Assert.Equal(0.7f, tracker.CalcRumorChance("heroine_a", "jiangnan"), precision: 6);

        var state = manager.GetState("heroine_a");
        Assert.NotNull(state);
        Assert.Equal("1", state!.Flags[NpcStateRomancePort.SignsDiscoveredFlag]);
        Assert.Equal("1", state.Flags[NpcStateRomancePort.LettersReceivedFlag]);
        Assert.Equal("1", state.Flags[NpcStateRomancePort.RumorsHeardFlag]);
        Assert.Equal("1", state.Flags[NpcStateRomancePort.EncountersHadFlag]);
        Assert.Equal("12", state.Flags[CometPresenceTracker.GetLastContactFlag("heroine_a")]);
    }

    [Fact]
    public void NpcStateRomancePort_RepeatedQueuedCounters_IncrementLinearly()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        manager.SetDialogueLocked("heroine_a", true);
        var port = new NpcStateRomancePort(manager);
        var tracker = new CometPresenceTracker(port, new FixedWorldDay(12), DefaultTuning);

        Assert.True(tracker.RecordRumorHeard("heroine_a", "rumor"));
        Assert.True(tracker.RecordRumorHeard("heroine_a", "rumor"));
        Assert.Equal(0, port.GetCometPresenceCounters("heroine_a")!.RumorsHeard);

        manager.SetDialogueLocked("heroine_a", false);

        Assert.Equal(2, port.GetCometPresenceCounters("heroine_a")!.RumorsHeard);
    }

    [Fact]
    public void NpcStateRomancePort_RumorAndEncounter_DoNotAdvanceLastContact()
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        var port = new NpcStateRomancePort(manager);
        var tracker = new CometPresenceTracker(port, new FixedWorldDay(12), DefaultTuning);
        Assert.True(tracker.RecordSignDiscovered("heroine_a", "sign"));
        Assert.Equal(12, port.GetLastContactDay("heroine_a"));

        var laterTracker = new CometPresenceTracker(port, new FixedWorldDay(14), DefaultTuning);
        Assert.True(laterTracker.RecordRumorHeard("heroine_a", "rumor"));
        Assert.True(laterTracker.RecordEncounter("heroine_a", "encounter"));

        Assert.Equal(12, port.GetLastContactDay("heroine_a"));
        Assert.Equal(1, port.GetCometPresenceCounters("heroine_a")!.RumorsHeard);
        Assert.Equal(1, port.GetCometPresenceCounters("heroine_a")!.EncountersHad);
    }

    [Fact]
    public void CometPresenceTuningLoader_LoadsYamlTuning()
    {
        const string yaml = """
                            rumor_base_chance: 0.1
                            rumor_same_region_bonus: 0.2
                            rumor_absence_bonus: 0.3
                            rumor_absence_threshold_days: 9
                            rumor_cap: 0.6
                            """;

        var tuning = new CometPresenceTuningLoader()
            .Load(yaml, CometPresenceTuningLoader.DefaultPath);

        Assert.Equal(0.1f, tuning.RumorBaseChance);
        Assert.Equal(0.2f, tuning.RumorSameRegionBonus);
        Assert.Equal(0.3f, tuning.RumorAbsenceBonus);
        Assert.Equal(9, tuning.RumorAbsenceThresholdDays);
        Assert.Equal(0.6f, tuning.RumorCap);
    }

    [Fact]
    public void CometPresenceTuningLoader_RegistersRuntimeReadOnlyTable()
    {
        const string yaml = """
                            rumor_base_chance: 0.1
                            rumor_same_region_bonus: 0.2
                            rumor_absence_bonus: 0.3
                            rumor_absence_threshold_days: 9
                            rumor_cap: 0.6
                            """;
        var registry = new DataRegistry();

        new CometPresenceTuningLoader().LoadAll(yaml, registry);

        var table = registry.GetTable<CometPresenceTuning>();
        Assert.NotNull(table);
        Assert.Equal(1, table!.Count);
        Assert.Equal(0.6f, table.Get("default")!.RumorCap);
        Assert.Null(table.Get("missing"));
    }

    private sealed record FixedWorldDay(int CurrentDay) : IWorldDayProvider;

    private static readonly CometPresenceTuning DefaultTuning = new();

    private sealed class RecordingCometPresencePort : IRomanceCometPresencePort
    {
        private readonly Dictionary<string, CometPresenceCounters> _counters = new();
        private readonly Dictionary<string, string?> _journeyRegions = new();
        private readonly Dictionary<string, int?> _lastContactDays = new();

        public void Register(string npcId, string? journeyRegion = null, int? lastContactDay = null)
        {
            _counters[npcId] = new CometPresenceCounters();
            _journeyRegions[npcId] = journeyRegion;
            _lastContactDays[npcId] = lastContactDay;
        }

        public CometPresenceCounters? GetCometPresenceCounters(string npcId)
        {
            return _counters.TryGetValue(npcId, out var counters) ? counters : null;
        }

        public string? GetJourneyRegion(string npcId)
        {
            return _journeyRegions.TryGetValue(npcId, out var region) ? region : null;
        }

        public int? GetLastContactDay(string npcId)
        {
            return _lastContactDays.TryGetValue(npcId, out var day) ? day : null;
        }

        public bool RecordCometPresence(
            string npcId,
            CometPresenceKind kind,
            int? lastContactDay,
            string source)
        {
            if (!_counters.TryGetValue(npcId, out var counters)) return false;

            _counters[npcId] = kind switch
            {
                CometPresenceKind.Sign => counters with { SignsDiscovered = counters.SignsDiscovered + 1 },
                CometPresenceKind.Letter => counters with { LettersReceived = counters.LettersReceived + 1 },
                CometPresenceKind.Rumor => counters with { RumorsHeard = counters.RumorsHeard + 1 },
                CometPresenceKind.Encounter => counters with { EncountersHad = counters.EncountersHad + 1 },
                _ => counters
            };

            if (lastContactDay != null)
                _lastContactDays[npcId] = lastContactDay.Value;

            return true;
        }
    }
}
