using System.Text.Json;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Unit.SaveSystem;

public sealed class SavePayloadModelTests
{
    [Fact]
    public void SaveFileHeader_DefaultHeader_ContainsMagicVersionFlagsAndCryptoPlaceholders()
    {
        var header = SaveFileHeader.CreateDefault(SaveFileFlags.Encrypted | SaveFileFlags.HasHmac);

        Assert.Equal("FZHI", header.Magic);
        Assert.Equal(1, header.SchemaVersion);
        Assert.True(header.Flags.HasFlag(SaveFileFlags.Encrypted));
        Assert.True(header.Flags.HasFlag(SaveFileFlags.HasHmac));
        Assert.Equal(32, SaveFileHeader.HeaderByteLength);
        Assert.Equal(16, SaveFileHeader.IvByteLength);
        Assert.Equal(32, SaveFileHeader.HmacByteLength);
        Assert.Equal(20, header.Reserved.Length);
    }

    [Fact]
    public void SlotMetadata_ManualAndAutoSlots_ExposeUiReadableFields()
    {
        var manual = new SlotMetadata
        {
            SlotId = SaveSlotId.Manual(1),
            IsOccupied = true,
            Timestamp = new DateTimeOffset(2026, 6, 11, 8, 30, 0, TimeSpan.Zero),
            ChapterName = "序章",
            SceneName = "青石镇",
            GameDay = 7,
            Playtime = TimeSpan.FromSeconds(3605),
            ThumbnailPng = new byte[] { 0x89, 0x50, 0x4e, 0x47 },
            IsNewGamePlus = true
        };
        var auto = SlotMetadata.Empty(SaveSlotId.AutoSave);

        Assert.Equal("slot_01", manual.SlotId.Name);
        Assert.Equal(SaveSlotKind.Manual, manual.SlotId.Kind);
        Assert.Equal(1, manual.SlotId.ManualIndex);
        Assert.True(manual.IsOccupied);
        Assert.Equal("序章", manual.ChapterName);
        Assert.Equal("青石镇", manual.SceneName);
        Assert.Equal(7, manual.GameDay);
        Assert.Equal(TimeSpan.FromSeconds(3605), manual.Playtime);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, manual.ThumbnailPng);
        Assert.True(manual.IsNewGamePlus);

        Assert.Equal("autosave", auto.SlotId.Name);
        Assert.Equal(SaveSlotKind.Auto, auto.SlotId.Kind);
        Assert.Null(auto.SlotId.ManualIndex);
        Assert.False(auto.IsOccupied);
        Assert.Null(auto.Timestamp);
    }

    [Fact]
    public void SaveSlotId_AllSlots_ReturnsTenManualSlotsAndOneAutosave()
    {
        var slots = SaveSlotId.AllSlots();

        Assert.Equal(11, slots.Count);
        Assert.Equal("slot_01", slots[0].Name);
        Assert.Equal("slot_10", slots[9].Name);
        Assert.Equal("autosave", slots[10].Name);
        Assert.All(slots.Take(10), slot => Assert.Equal(SaveSlotKind.Manual, slot.Kind));
        Assert.Equal(SaveSlotKind.Auto, slots[10].Kind);
    }

    [Fact]
    public void SaveSlotId_ManualSlotOutsideOneToTen_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SaveSlotId.Manual(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SaveSlotId.Manual(11));
    }

    [Fact]
    public void SavePayload_CreatePayload_PreservesSchemaMetadataAndSystemSnapshots()
    {
        var payload = SavePayload.Create(new SlotMetadata
        {
            SlotId = SaveSlotId.Manual(2),
            IsOccupied = true,
            ChapterName = "第一章",
            SceneName = "竹林道"
        });
        payload.Systems["dialogue"] = Snapshot(("current_node", "node_03"), ("flag_met_master", true));

        Assert.Equal(1, payload.SchemaVersion);
        Assert.Equal("slot_02", payload.Metadata.SlotId.Name);
        Assert.Equal("第一章", payload.Metadata.ChapterName);
        Assert.True(payload.Systems.ContainsKey("dialogue"));
        Assert.Equal("node_03", payload.Systems["dialogue"].Values["current_node"].GetString());
        Assert.True(payload.Systems["dialogue"].Values["flag_met_master"].GetBoolean());
    }

    [Fact]
    public async Task SaveContracts_MinimalFakeManagerAndSaveable_CanRoundTripThroughInterfaces()
    {
        ISaveable saveable = new FakeSaveable("dialogue", Snapshot(("node", "start")));
        ISaveManager manager = new FakeSaveManager();

        var saveResult = await manager.SaveGameAsync(SaveSlotId.Manual(3));
        var loadResult = await manager.LoadGameAsync(SaveSlotId.Manual(3));
        var snapshot = saveable.Serialize();
        saveable.Deserialize(snapshot, version: 1);

        Assert.True(saveResult.Success);
        Assert.True(loadResult.Success);
        Assert.Equal("dialogue", saveable.SaveKey);
        Assert.True(manager.IsSlotOccupied(SaveSlotId.Manual(3)));
        Assert.Equal("slot_03", manager.GetMetadata(SaveSlotId.Manual(3)).SlotId.Name);
        Assert.Equal(11, manager.ListSlots().Count);
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
        private SaveSnapshot _snapshot;

        public FakeSaveable(string saveKey, SaveSnapshot snapshot)
        {
            SaveKey = saveKey;
            _snapshot = snapshot;
        }

        public string SaveKey { get; }

        public SaveSnapshot Serialize() => _snapshot;

        public void Deserialize(SaveSnapshot snapshot, int version)
        {
            Assert.Equal(1, version);
            _snapshot = snapshot;
        }
    }

    private sealed class FakeSaveManager : ISaveManager
    {
        private readonly HashSet<SaveSlotId> _occupiedSlots = new();

        public SaveResult RegisterSerializer(ISaveable saveable)
        {
            return SaveResult.Ok();
        }

        public Task<SaveResult> SaveGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            _occupiedSlots.Add(slotId);
            return Task.FromResult(SaveResult.Ok());
        }

        public Task<SaveResult> LoadGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SaveResult.Ok());
        }

        public IReadOnlyList<SlotMetadata> ListSlots()
        {
            return SaveSlotId.AllSlots()
                .Select(slot => new SlotMetadata { SlotId = slot, IsOccupied = _occupiedSlots.Contains(slot) })
                .ToList();
        }

        public SaveResult DeleteSlot(SaveSlotId slotId)
        {
            _occupiedSlots.Remove(slotId);
            return SaveResult.Ok();
        }

        public SlotMetadata GetMetadata(SaveSlotId slotId)
        {
            return new SlotMetadata { SlotId = slotId, IsOccupied = _occupiedSlots.Contains(slotId) };
        }

        public bool IsSlotOccupied(SaveSlotId slotId) => _occupiedSlots.Contains(slotId);
    }
}
