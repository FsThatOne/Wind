using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Unit.SaveSystem;

public sealed class SaveLoadMenuPresenterTests
{
    [Fact]
    public void GetSnapshot_ShowsTenManualSlotsAndOneAutosaveWithMetadata()
    {
        var manager = new FakeSaveManager();
        manager.SetOccupied(SaveSlotId.Manual(1), Metadata(SaveSlotId.Manual(1), "序章", "青石镇"));
        manager.SetOccupied(SaveSlotId.AutoSave, Metadata(SaveSlotId.AutoSave, "第一章", "竹林道"));
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Save);

        var snapshot = presenter.GetSnapshot();

        Assert.Equal(11, snapshot.Slots.Count);
        Assert.Equal("存档 01", snapshot.Slots[0].Title);
        Assert.Equal("已有存档", snapshot.Slots[0].StatusText);
        Assert.Equal("序章", snapshot.Slots[0].ChapterName);
        Assert.True(snapshot.Slots[0].HasThumbnail);
        Assert.Equal("空（可存档）", snapshot.Slots[1].StatusText);
        Assert.Equal("自动存档", snapshot.Slots[10].Title);
        Assert.True(snapshot.Slots[10].IsAutosave);
        Assert.Equal("自动槽", snapshot.Slots[10].StatusText);
    }

    [Fact]
    public void GetSnapshot_AllManualSlotsOccupied_ShowsFullSlotText()
    {
        var manager = new FakeSaveManager();
        for (var i = 1; i <= SaveSlotId.ManualSlotCount; i++)
            manager.SetOccupied(SaveSlotId.Manual(i), Metadata(SaveSlotId.Manual(i), "序章", $"场景{i}"));
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Save);

        var snapshot = presenter.GetSnapshot();

        Assert.True(snapshot.IsManualSlotFull);
        Assert.Equal("槽已满（10/10）", snapshot.FullSlotText);
    }

    [Fact]
    public async Task ConfirmSaveOnOccupiedSlot_RequiresOverwriteConfirmationBeforeWriting()
    {
        var manager = new FakeSaveManager();
        manager.SetOccupied(SaveSlotId.Manual(1), Metadata(SaveSlotId.Manual(1), "序章", "青石镇"));
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Save);

        var first = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);
        var confirming = presenter.GetSnapshot();

        Assert.True(first.Success);
        Assert.Equal(SaveLoadConfirmationKind.Overwrite, confirming.ConfirmationKind);
        Assert.Contains("覆盖", confirming.ConfirmationText);
        Assert.Empty(manager.SavedSlots);

        var second = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);

        Assert.True(second.Success, second.Error);
        Assert.Equal(new[] { SaveSlotId.Manual(1) }, manager.SavedSlots);
        Assert.Equal(SaveLoadConfirmationKind.None, presenter.GetSnapshot().ConfirmationKind);
    }

    [Fact]
    public async Task DeleteOnOccupiedSlot_RequiresDeleteConfirmationBeforeDeleting()
    {
        var manager = new FakeSaveManager();
        manager.SetOccupied(SaveSlotId.Manual(1), Metadata(SaveSlotId.Manual(1), "序章", "青石镇"));
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Load);

        var first = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Delete, SaveLoadMenuInputSource.Gamepad);
        var confirming = presenter.GetSnapshot();

        Assert.True(first.Success);
        Assert.Equal(SaveLoadConfirmationKind.Delete, confirming.ConfirmationKind);
        Assert.Contains("删除", confirming.ConfirmationText);
        Assert.True(manager.IsSlotOccupied(SaveSlotId.Manual(1)));

        var second = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.Gamepad);

        Assert.True(second.Success, second.Error);
        Assert.False(manager.IsSlotOccupied(SaveSlotId.Manual(1)));
        Assert.Equal(new[] { SaveSlotId.Manual(1) }, manager.DeletedSlots);
    }

    [Fact]
    public async Task SaveBlocked_DisablesSaveAndReturnsReasonWithoutWriting()
    {
        var manager = new FakeSaveManager();
        var blocker = new SaveMenuBlocker(true, "战斗中无法存档");
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Save, blocker);

        var snapshot = presenter.GetSnapshot();
        var result = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);

        Assert.True(snapshot.IsSaveBlocked);
        Assert.Equal("战斗中无法存档", snapshot.BlockReason);
        Assert.All(snapshot.Slots.Where(slot => slot.SlotId.Kind == SaveSlotKind.Manual), slot => Assert.False(slot.CanSave));
        Assert.False(result.Success);
        Assert.Contains("战斗中", result.Error);
        Assert.Empty(manager.SavedSlots);
    }

    [Fact]
    public async Task KeyboardMouseAndGamepadInputs_NavigateAndConfirmEquivalently()
    {
        var keyboardManager = new FakeSaveManager();
        var gamepadManager = new FakeSaveManager();
        var keyboard = new SaveLoadMenuPresenter(keyboardManager, SaveLoadMenuMode.Save);
        var gamepad = new SaveLoadMenuPresenter(gamepadManager, SaveLoadMenuMode.Save);

        await keyboard.HandleInputAsync(SaveLoadMenuInputIntent.MoveDown, SaveLoadMenuInputSource.KeyboardMouse);
        var keyboardResult = await keyboard.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);

        await gamepad.HandleInputAsync(SaveLoadMenuInputIntent.MoveDown, SaveLoadMenuInputSource.Gamepad);
        var gamepadResult = await gamepad.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.Gamepad);

        Assert.True(keyboardResult.Success);
        Assert.True(gamepadResult.Success);
        Assert.Equal(keyboard.GetSnapshot().SelectedIndex, gamepad.GetSnapshot().SelectedIndex);
        Assert.Equal(keyboardManager.SavedSlots, gamepadManager.SavedSlots);
        Assert.Equal(new[] { SaveSlotId.Manual(2) }, keyboardManager.SavedSlots);
    }

    [Fact]
    public async Task LoadMode_EmptySlotDoesNotLoadOccupiedSlotLoads()
    {
        var manager = new FakeSaveManager();
        manager.SetOccupied(SaveSlotId.Manual(2), Metadata(SaveSlotId.Manual(2), "序章", "青石镇"));
        var presenter = new SaveLoadMenuPresenter(manager, SaveLoadMenuMode.Load);

        var empty = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);
        presenter.SelectIndex(1);
        var occupied = await presenter.HandleInputAsync(SaveLoadMenuInputIntent.Confirm, SaveLoadMenuInputSource.KeyboardMouse);

        Assert.False(empty.Success);
        Assert.True(occupied.Success, occupied.Error);
        Assert.Equal(new[] { SaveSlotId.Manual(2) }, manager.LoadedSlots);
    }

    private static SlotMetadata Metadata(SaveSlotId slotId, string chapter, string scene)
    {
        return new SlotMetadata
        {
            SlotId = slotId,
            IsOccupied = true,
            Timestamp = new DateTimeOffset(2026, 6, 11, 12, 30, 0, TimeSpan.Zero),
            ChapterName = chapter,
            SceneName = scene,
            GameDay = 8,
            Playtime = TimeSpan.FromMinutes(95),
            ThumbnailPng = new byte[] { 1, 2, 3 }
        };
    }

    private sealed class FakeSaveManager : ISaveManager
    {
        private readonly Dictionary<SaveSlotId, SlotMetadata> _metadata = SaveSlotId.AllSlots()
            .ToDictionary(slot => slot, SlotMetadata.Empty);

        public List<SaveSlotId> SavedSlots { get; } = new();

        public List<SaveSlotId> LoadedSlots { get; } = new();

        public List<SaveSlotId> DeletedSlots { get; } = new();

        public SaveResult RegisterSerializer(ISaveable saveable) => SaveResult.Ok();

        public Task<SaveResult> SaveGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            SavedSlots.Add(slotId);
            _metadata[slotId] = _metadata[slotId] with { IsOccupied = true };
            return Task.FromResult(SaveResult.Ok(slotId));
        }

        public Task<SaveResult> LoadGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            LoadedSlots.Add(slotId);
            return Task.FromResult(SaveResult.Ok(slotId));
        }

        public IReadOnlyList<SlotMetadata> ListSlots()
        {
            return SaveSlotId.AllSlots().Select(slot => _metadata[slot]).ToList();
        }

        public SaveResult DeleteSlot(SaveSlotId slotId)
        {
            DeletedSlots.Add(slotId);
            _metadata[slotId] = SlotMetadata.Empty(slotId);
            return SaveResult.Ok(slotId);
        }

        public SlotMetadata GetMetadata(SaveSlotId slotId) => _metadata[slotId];

        public bool IsSlotOccupied(SaveSlotId slotId) => _metadata[slotId].IsOccupied;

        public void SetOccupied(SaveSlotId slotId, SlotMetadata metadata)
        {
            _metadata[slotId] = metadata with { SlotId = slotId, IsOccupied = true };
        }
    }
}
