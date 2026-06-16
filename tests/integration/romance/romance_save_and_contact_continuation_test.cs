using System.Text.Json;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Integration.Romance;

public sealed class RomanceSaveAndContactContinuationTest
{
    [Fact]
    public async Task SaveLoad_RestoresLastContactAndContinuesDaysSinceContact()
    {
        var store = new InMemorySavePayloadStore();
        var source = CreateRomancePersistence();
        var sourceTracker = new CometPresenceTracker(source.Port, new FixedWorldDay(10), new CometPresenceTuning());
        Assert.True(sourceTracker.RecordSignDiscovered("heroine_a", "sign"));
        var saveManager = CreateSaveManager(store);
        saveManager.RegisterSerializer(source.Persistence);
        var restored = CreateRomancePersistence();
        var loadManager = CreateSaveManager(store);
        loadManager.RegisterSerializer(restored.Persistence);

        await saveManager.SaveGameAsync(SaveSlotId.Manual(1));
        var result = await loadManager.LoadGameAsync(SaveSlotId.Manual(1));
        var restoredTracker = new CometPresenceTracker(restored.Port, new FixedWorldDay(14), new CometPresenceTuning());

        Assert.True(result.Success);
        Assert.Equal(10, restored.Port.GetLastContactDay("heroine_a"));
        Assert.Equal(4, restoredTracker.GetDaysSinceLastContact("heroine_a"));
    }

    [Fact]
    public void GetDaysSinceLastContact_WhenStoredDayIsFuture_ClampsToZero()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag(
            "heroine_a",
            CometPresenceTracker.GetLastContactFlag("heroine_a"),
            "15",
            "corrupt_save");
        var tracker = new CometPresenceTracker(services.Port, new FixedWorldDay(12), new CometPresenceTuning());

        var days = tracker.GetDaysSinceLastContact("heroine_a");

        Assert.Equal(0, days);
    }

    [Fact]
    public void GetDaysSinceLastContact_WhenNeverContacted_ReturnsNull()
    {
        var services = CreateRomancePersistence();
        var tracker = new CometPresenceTracker(services.Port, new FixedWorldDay(12), new CometPresenceTuning());

        var days = tracker.GetDaysSinceLastContact("heroine_a");

        Assert.Null(days);
    }

    [Fact]
    public async Task SaveLoad_RestoresBondedHeroineFromNpcStateFlag()
    {
        var store = new InMemorySavePayloadStore();
        var source = CreateRomancePersistence();
        Assert.True(source.Port.ConfirmBond("heroine_a", "bond_node"));
        var saveManager = CreateSaveManager(store);
        saveManager.RegisterSerializer(source.Persistence);
        var restored = CreateRomancePersistence(registerHeroineB: true);
        var loadManager = CreateSaveManager(store);
        loadManager.RegisterSerializer(restored.Persistence);

        await saveManager.SaveGameAsync(SaveSlotId.Manual(2));
        var result = await loadManager.LoadGameAsync(SaveSlotId.Manual(2));
        var service = new RomanceService(restored.Port, new MilestoneRegistry(restored.Port));

        Assert.True(result.Success);
        Assert.Equal("heroine_a", service.BondedHeroine);
        Assert.Equal(RomanceBondStatus.AlreadyBonded, service.TryBond("heroine_b").Status);
        Assert.True(restored.Manager.GetState("heroine_a")!.Flags.ContainsKey(NpcStateRomancePort.BondedHeroineFlag));
    }

    [Fact]
    public void ResetForNewRun_ClearsAllRomanceFlagsAndKeepsNonRomanceNpcState()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", "narrative_chapter_marker", "chapter_2", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.AcquaintedFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.TrustFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.CrisisFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.BondFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.BrokenFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.BondedHeroineFlag, "true", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.SignsDiscoveredFlag, "3", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.LettersReceivedFlag, "2", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.RumorsHeardFlag, "4", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.EncountersHadFlag, "1", "test");
        services.Manager.UpdateFlag(
            "heroine_a",
            CometPresenceTracker.GetLastContactFlag("heroine_a"),
            "10",
            "test");

        services.Persistence.ResetForNewRun();

        var flags = services.Manager.GetState("heroine_a")!.Flags;
        Assert.DoesNotContain(flags.Keys, key => key.StartsWith("romance_", StringComparison.Ordinal));
        Assert.Equal("chapter_2", flags["narrative_chapter_marker"]);
    }

    [Fact]
    public void Deserialize_WhenSnapshotContainsNonRomanceFlag_FailsBeforeMutatingExistingState()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "existing");
        var snapshot = Snapshot(new Dictionary<string, Dictionary<string, string>>
        {
            ["heroine_a"] = new() { ["narrative_bad_flag"] = "true" }
        });

        Assert.Throws<ArgumentException>(() => services.Persistence.Deserialize(snapshot, version: 1));

        Assert.Equal("true", services.Manager.GetState("heroine_a")!.Flags[NpcStateRomancePort.HeartFlag]);
    }

    [Fact]
    public void Deserialize_WhenSnapshotContainsMultipleBondedHeroines_FailsBeforeMutatingExistingState()
    {
        var services = CreateRomancePersistence(registerHeroineB: true);
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "existing");
        var snapshot = Snapshot(new Dictionary<string, Dictionary<string, string>>
        {
            ["heroine_a"] = new() { [NpcStateRomancePort.BondedHeroineFlag] = "true" },
            ["heroine_b"] = new() { [NpcStateRomancePort.BondedHeroineFlag] = "true" }
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.Persistence.Deserialize(snapshot, version: 1));

        Assert.Contains("multiple bonded heroines", ex.Message);
        Assert.Equal("true", services.Manager.GetState("heroine_a")!.Flags[NpcStateRomancePort.HeartFlag]);
        Assert.False(services.Manager.GetState("heroine_b")!.Flags.ContainsKey(NpcStateRomancePort.BondedHeroineFlag));
    }

    [Fact]
    public void Deserialize_WhenBondedHeroineValueIsInvalid_FailsBeforeMutatingExistingState()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "existing");
        var snapshot = Snapshot(new Dictionary<string, Dictionary<string, string>>
        {
            ["heroine_a"] = new() { [NpcStateRomancePort.BondedHeroineFlag] = "not_bool" }
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.Persistence.Deserialize(snapshot, version: 1));

        Assert.Contains("must be a boolean", ex.Message);
        Assert.Equal("true", services.Manager.GetState("heroine_a")!.Flags[NpcStateRomancePort.HeartFlag]);
    }

    [Fact]
    public void Deserialize_WhenNpcIsDead_RestoresRomanceFlagsImmediately()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "old_state");
        services.Manager.UpdateLife("heroine_a", LifeStatus.Dead, "test");
        var snapshot = Snapshot(new Dictionary<string, Dictionary<string, string>>
        {
            ["heroine_a"] = new()
            {
                [NpcStateRomancePort.BondFlag] = "true",
                [NpcStateRomancePort.BondedHeroineFlag] = "true"
            }
        });

        services.Persistence.Deserialize(snapshot, version: 1);

        var flags = services.Manager.GetState("heroine_a")!.Flags;
        Assert.False(flags.ContainsKey(NpcStateRomancePort.HeartFlag));
        Assert.Equal("true", flags[NpcStateRomancePort.BondFlag]);
        Assert.Equal("true", flags[NpcStateRomancePort.BondedHeroineFlag]);
    }

    [Fact]
    public void ResetForNewRun_WhenNpcIsDead_ClearsRomanceFlagsImmediately()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", "narrative_chapter_marker", "chapter_2", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "test");
        services.Manager.UpdateLife("heroine_a", LifeStatus.Dead, "test");

        services.Persistence.ResetForNewRun();

        var flags = services.Manager.GetState("heroine_a")!.Flags;
        Assert.False(flags.ContainsKey(NpcStateRomancePort.HeartFlag));
        Assert.Equal("chapter_2", flags["narrative_chapter_marker"]);
    }

    [Fact]
    public void ResetForNewRun_WhenNpcIsDialogueLocked_ClearsCurrentAndPendingRomanceFlagsImmediately()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", "narrative_chapter_marker", "chapter_2", "test");
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "test");
        services.Manager.SetDialogueLocked("heroine_a", locked: true);
        Assert.True(services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.BondFlag, "true", "queued"));

        services.Persistence.ResetForNewRun();
        services.Manager.SetDialogueLocked("heroine_a", locked: false);

        var flags = services.Manager.GetState("heroine_a")!.Flags;
        Assert.DoesNotContain(flags.Keys, key => key.StartsWith("romance_", StringComparison.Ordinal));
        Assert.Equal("chapter_2", flags["narrative_chapter_marker"]);
    }

    [Fact]
    public void Deserialize_WhenNpcIsDialogueLocked_RestoresSnapshotImmediatelyAndDropsQueuedRomanceWrites()
    {
        var services = CreateRomancePersistence();
        services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.HeartFlag, "true", "old_state");
        services.Manager.SetDialogueLocked("heroine_a", locked: true);
        Assert.True(services.Manager.UpdateFlag("heroine_a", NpcStateRomancePort.BondFlag, "true", "queued"));
        var snapshot = Snapshot(new Dictionary<string, Dictionary<string, string>>
        {
            ["heroine_a"] = new() { [NpcStateRomancePort.TrustFlag] = "true" }
        });

        services.Persistence.Deserialize(snapshot, version: 1);
        services.Manager.SetDialogueLocked("heroine_a", locked: false);

        var flags = services.Manager.GetState("heroine_a")!.Flags;
        Assert.False(flags.ContainsKey(NpcStateRomancePort.HeartFlag));
        Assert.False(flags.ContainsKey(NpcStateRomancePort.BondFlag));
        Assert.Equal("true", flags[NpcStateRomancePort.TrustFlag]);
    }

    private static SaveManager CreateSaveManager(InMemorySavePayloadStore store)
    {
        return new SaveManager(
            store,
            new EventBus(),
            slot => new SlotMetadata
            {
                SlotId = slot,
                ChapterName = "第一章",
                SceneName = "竹林道",
                GameDay = 12,
                Playtime = TimeSpan.FromMinutes(42)
            });
    }

    private static RomancePersistenceServices CreateRomancePersistence(bool registerHeroineB = false)
    {
        var manager = new NpcStateManager(new EventBus());
        manager.RegisterNpc("heroine_a");
        if (registerHeroineB)
            manager.RegisterNpc("heroine_b");
        var port = new NpcStateRomancePort(manager);
        var persistence = new RomancePersistenceAdapter(port);
        return new RomancePersistenceServices(manager, port, persistence);
    }

    private static SaveSnapshot Snapshot(Dictionary<string, Dictionary<string, string>> npcRomanceFlags)
    {
        var snapshot = new SaveSnapshot();
        snapshot.Values[RomancePersistenceAdapter.RomanceFlagsField] =
            JsonSerializer.SerializeToElement(npcRomanceFlags);
        return snapshot;
    }

    private sealed record FixedWorldDay(int CurrentDay) : IWorldDayProvider;

    private sealed record RomancePersistenceServices(
        NpcStateManager Manager,
        NpcStateRomancePort Port,
        RomancePersistenceAdapter Persistence);
}
