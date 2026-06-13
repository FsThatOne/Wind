using System.Text.Json;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Integration.SaveSystem;

public sealed class SaveManagerRoundtripTests
{
    [Fact]
    public async Task SaveGame_CollectsRegisteredSaveablesAndWritesSingleSlot()
    {
        var store = new InMemorySavePayloadStore();
        var manager = CreateManager(store);
        manager.RegisterSerializer(new FakeSaveable("dialogue", Snapshot(("node", "start"))));
        manager.RegisterSerializer(new FakeSaveable("party", Snapshot(("count", 2))));

        var result = await manager.SaveGameAsync(SaveSlotId.Manual(1));
        var (_, payload) = await store.ReadAsync(SaveSlotId.Manual(1));

        Assert.True(result.Success);
        Assert.NotNull(payload);
        Assert.Equal("slot_01", payload.Metadata.SlotId.Name);
        Assert.True(payload.Metadata.IsOccupied);
        Assert.Equal(2, payload.Systems.Count);
        Assert.Equal("start", payload.Systems["dialogue"].Values["node"].GetString());
        Assert.Equal(2, payload.Systems["party"].Values["count"].GetInt32());
    }

    [Fact]
    public async Task LoadGame_DispatchesKnownKeysAndIgnoresUnknownKeys()
    {
        var store = new InMemorySavePayloadStore();
        var manager = CreateManager(store);
        var dialogue = new FakeSaveable("dialogue", SaveSnapshot.Empty);
        manager.RegisterSerializer(dialogue);
        var payload = SavePayload.Create(new SlotMetadata { SlotId = SaveSlotId.Manual(2), IsOccupied = true });
        payload.Systems["dialogue"] = Snapshot(("node", "loaded"));
        payload.Systems["unknown_future_system"] = Snapshot(("value", 99));
        await store.WriteAsync(SaveSlotId.Manual(2), payload);

        var result = await manager.LoadGameAsync(SaveSlotId.Manual(2));

        Assert.True(result.Success);
        Assert.Single(dialogue.DeserializedSnapshots);
        Assert.Equal("loaded", dialogue.DeserializedSnapshots[0].Values["node"].GetString());
        Assert.Equal(1, dialogue.DeserializedVersions[0]);
    }

    [Fact]
    public async Task SaveAndLoad_Success_PublishesCompletionEventsOnce()
    {
        var eventBus = new EventBus();
        var store = new InMemorySavePayloadStore();
        var manager = CreateManager(store, eventBus);
        manager.RegisterSerializer(new FakeSaveable("dialogue", Snapshot(("node", "start"))));
        var completed = new List<SaveCompletedEvent>();
        var loaded = new List<SaveLoadedEvent>();
        eventBus.Subscribe<SaveCompletedEvent>(completed.Add);
        eventBus.Subscribe<SaveLoadedEvent>(loaded.Add);

        await manager.SaveGameAsync(SaveSlotId.Manual(3));
        await manager.LoadGameAsync(SaveSlotId.Manual(3));

        Assert.Single(completed);
        Assert.Single(loaded);
        Assert.Equal("slot_03", completed[0].SlotId.Name);
        Assert.Equal("slot_03", loaded[0].SlotId.Name);
    }

    [Fact]
    public async Task LoadGame_Failure_DoesNotPublishLoadedEvent()
    {
        var eventBus = new EventBus();
        var manager = CreateManager(new InMemorySavePayloadStore(), eventBus);
        var loaded = new List<SaveLoadedEvent>();
        eventBus.Subscribe<SaveLoadedEvent>(loaded.Add);

        var result = await manager.LoadGameAsync(SaveSlotId.Manual(4));

        Assert.False(result.Success);
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task ListSlotsAndGetMetadata_ReturnUiReadySlotData()
    {
        var manager = CreateManager(new InMemorySavePayloadStore());

        await manager.SaveGameAsync(SaveSlotId.AutoSave);
        var slots = manager.ListSlots();
        var autosave = manager.GetMetadata(SaveSlotId.AutoSave);

        Assert.Equal(11, slots.Count);
        Assert.Equal("slot_01", slots[0].SlotId.Name);
        Assert.Equal("autosave", slots[10].SlotId.Name);
        Assert.True(manager.IsSlotOccupied(SaveSlotId.AutoSave));
        Assert.True(autosave.IsOccupied);
        Assert.Equal("第一章", autosave.ChapterName);
        Assert.Equal("竹林道", autosave.SceneName);
        Assert.Equal(12, autosave.GameDay);
        Assert.Equal(TimeSpan.FromMinutes(42), autosave.Playtime);
    }

    [Fact]
    public void RegisterSerializer_DuplicateKey_ReturnsFailure()
    {
        var manager = CreateManager(new InMemorySavePayloadStore());

        var first = manager.RegisterSerializer(new FakeSaveable("dialogue", SaveSnapshot.Empty));
        var second = manager.RegisterSerializer(new FakeSaveable("dialogue", SaveSnapshot.Empty));

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Contains("重复", second.Error);
    }

    private static SaveManager CreateManager(ISavePayloadStore store, IEventBus? eventBus = null)
    {
        return new SaveManager(
            store,
            eventBus ?? new EventBus(),
            slot => new SlotMetadata
            {
                SlotId = slot,
                ChapterName = "第一章",
                SceneName = "竹林道",
                GameDay = 12,
                Playtime = TimeSpan.FromMinutes(42),
                ThumbnailPng = new byte[] { 1, 2, 3 }
            });
    }

    private static SaveSnapshot Snapshot(params (string Key, object Value)[] values)
    {
        var snapshot = new SaveSnapshot();
        foreach (var (key, value) in values)
            snapshot.Values[key] = JsonSerializer.SerializeToElement(value);
        return snapshot;
    }

    private sealed class FakeSaveable : ISaveable
    {
        private readonly SaveSnapshot _snapshot;

        public FakeSaveable(string saveKey, SaveSnapshot snapshot)
        {
            SaveKey = saveKey;
            _snapshot = snapshot;
        }

        public string SaveKey { get; }

        public List<SaveSnapshot> DeserializedSnapshots { get; } = new();

        public List<int> DeserializedVersions { get; } = new();

        public SaveSnapshot Serialize() => _snapshot;

        public void Deserialize(SaveSnapshot snapshot, int version)
        {
            DeserializedSnapshots.Add(snapshot);
            DeserializedVersions.Add(version);
        }
    }
}
