namespace FengZhi.Foundation.SaveSystem;

/// <summary>存读档菜单模式。</summary>
public enum SaveLoadMenuMode
{
    /// <summary>存档模式。</summary>
    Save,

    /// <summary>读档模式。</summary>
    Load
}

/// <summary>存读档菜单输入来源。</summary>
public enum SaveLoadMenuInputSource
{
    /// <summary>键鼠输入。</summary>
    KeyboardMouse,

    /// <summary>手柄输入。</summary>
    Gamepad
}

/// <summary>存读档菜单输入意图。</summary>
public enum SaveLoadMenuInputIntent
{
    /// <summary>向上移动焦点。</summary>
    MoveUp,

    /// <summary>向下移动焦点。</summary>
    MoveDown,

    /// <summary>确认当前焦点。</summary>
    Confirm,

    /// <summary>取消当前确认弹窗。</summary>
    Cancel,

    /// <summary>删除当前槽位。</summary>
    Delete
}

/// <summary>存读档菜单确认类型。</summary>
public enum SaveLoadConfirmationKind
{
    /// <summary>无确认。</summary>
    None,

    /// <summary>覆盖已有存档。</summary>
    Overwrite,

    /// <summary>删除已有存档。</summary>
    Delete
}

/// <summary>存读档菜单槽位 UI 快照。</summary>
public sealed record SaveSlotUiSnapshot(
    SaveSlotId SlotId,
    int VisibleIndex,
    string Title,
    string StatusText,
    string ChapterName,
    string SceneName,
    string TimestampText,
    string PlaytimeText,
    bool IsOccupied,
    bool IsAutosave,
    bool IsSelected,
    bool CanSave,
    bool CanLoad,
    bool CanDelete,
    bool HasThumbnail,
    byte[] ThumbnailPng);

/// <summary>存读档菜单 UI 快照。</summary>
public sealed record SaveLoadMenuSnapshot(
    SaveLoadMenuMode Mode,
    IReadOnlyList<SaveSlotUiSnapshot> Slots,
    int SelectedIndex,
    bool IsManualSlotFull,
    string? FullSlotText,
    bool IsSaveBlocked,
    string? BlockReason,
    SaveLoadConfirmationKind ConfirmationKind,
    string? ConfirmationText,
    bool ShowContinueEntry,
    string? LastMessage);

/// <summary>存读档菜单存档阻断状态。</summary>
public interface ISaveMenuBlocker
{
    /// <summary>当前是否禁止玩家主动存档。</summary>
    bool IsSaveBlocked { get; }

    /// <summary>阻断原因。</summary>
    string BlockReason { get; }
}

/// <summary>无阻断的存档菜单状态。</summary>
public sealed partial class SaveMenuBlocker : ISaveMenuBlocker
{
    /// <summary>永不阻断的默认实例。</summary>
    public static SaveMenuBlocker None { get; } = new(false, string.Empty);

    /// <summary>创建阻断状态。</summary>
    public SaveMenuBlocker(bool isSaveBlocked, string blockReason)
    {
        IsSaveBlocked = isSaveBlocked;
        BlockReason = blockReason;
    }

    /// <inheritdoc />
    public bool IsSaveBlocked { get; }

    /// <inheritdoc />
    public string BlockReason { get; }
}

/// <summary>
/// 存读档菜单 Presenter。将 SaveManager 的槽位元数据翻译为 UI 快照，并处理基础导航与确认。
/// </summary>
public sealed partial class SaveLoadMenuPresenter
{
    private readonly ISaveManager _saveManager;
    private readonly ISaveMenuBlocker _blocker;
    private readonly bool _showContinueEntry;
    private int _selectedIndex;
    private SaveLoadConfirmationKind _confirmationKind;
    private SaveSlotId? _confirmationSlot;
    private string? _lastMessage;

    /// <summary>创建存读档菜单 Presenter。</summary>
    public SaveLoadMenuPresenter(
        ISaveManager saveManager,
        SaveLoadMenuMode mode,
        ISaveMenuBlocker? blocker = null,
        bool showContinueEntry = true)
    {
        _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
        Mode = mode;
        _blocker = blocker ?? SaveMenuBlocker.None;
        _showContinueEntry = showContinueEntry;
    }

    /// <summary>当前菜单模式。</summary>
    public SaveLoadMenuMode Mode { get; }

    /// <summary>读取当前 UI 快照。</summary>
    public SaveLoadMenuSnapshot GetSnapshot()
    {
        var metadata = _saveManager.ListSlots();
        var slots = metadata
            .Select((slot, index) => BuildSlotSnapshot(slot, index))
            .ToList();

        if (slots.Count == 0)
            _selectedIndex = -1;
        else if (_selectedIndex >= slots.Count)
            _selectedIndex = slots.Count - 1;

        var manualFull = metadata
            .Where(slot => slot.SlotId.Kind == SaveSlotKind.Manual)
            .Count(slot => slot.IsOccupied) == SaveSlotId.ManualSlotCount;

        return new SaveLoadMenuSnapshot(
            Mode,
            slots,
            _selectedIndex,
            manualFull,
            manualFull ? $"槽已满（{SaveSlotId.ManualSlotCount}/{SaveSlotId.ManualSlotCount}）" : null,
            _blocker.IsSaveBlocked,
            _blocker.IsSaveBlocked ? _blocker.BlockReason : null,
            _confirmationKind,
            BuildConfirmationText(),
            _showContinueEntry,
            _lastMessage);
    }

    /// <summary>处理键鼠或手柄输入意图。</summary>
    public Task<SaveResult> HandleInputAsync(
        SaveLoadMenuInputIntent intent,
        SaveLoadMenuInputSource source,
        CancellationToken cancellationToken = default)
    {
        _ = source;

        return intent switch
        {
            SaveLoadMenuInputIntent.MoveUp => Task.FromResult(MoveSelection(-1)),
            SaveLoadMenuInputIntent.MoveDown => Task.FromResult(MoveSelection(1)),
            SaveLoadMenuInputIntent.Confirm => ConfirmAsync(cancellationToken),
            SaveLoadMenuInputIntent.Cancel => Task.FromResult(CancelConfirmation()),
            SaveLoadMenuInputIntent.Delete => RequestDeleteAsync(cancellationToken),
            _ => Task.FromResult(SaveResult.Fail($"未知输入：{intent}"))
        };
    }

    /// <summary>直接选择指定可见索引。</summary>
    public SaveResult SelectIndex(int index)
    {
        var slots = _saveManager.ListSlots();
        if (index < 0 || index >= slots.Count)
            return SaveResult.Fail($"槽位索引越界：{index}");

        _selectedIndex = index;
        ClearConfirmation();
        return SaveResult.Ok(slots[index].SlotId);
    }

    private SaveSlotUiSnapshot BuildSlotSnapshot(SlotMetadata metadata, int index)
    {
        var isAutosave = metadata.SlotId.Kind == SaveSlotKind.Auto;
        var canSave = Mode == SaveLoadMenuMode.Save
            && metadata.SlotId.Kind == SaveSlotKind.Manual
            && !_blocker.IsSaveBlocked;
        var canLoad = Mode == SaveLoadMenuMode.Load && metadata.IsOccupied;
        var canDelete = metadata.IsOccupied;

        return new SaveSlotUiSnapshot(
            metadata.SlotId,
            index,
            GetSlotTitle(metadata.SlotId),
            GetStatusText(metadata),
            metadata.ChapterName,
            metadata.SceneName,
            FormatTimestamp(metadata.Timestamp),
            FormatPlaytime(metadata.Playtime),
            metadata.IsOccupied,
            isAutosave,
            index == _selectedIndex,
            canSave,
            canLoad,
            canDelete,
            metadata.ThumbnailPng.Length > 0,
            metadata.ThumbnailPng);
    }

    private async Task<SaveResult> ConfirmAsync(CancellationToken cancellationToken)
    {
        var slot = GetSelectedSlot();
        if (slot == null)
            return SaveResult.Fail("没有可选择的槽位。");

        if (_confirmationKind == SaveLoadConfirmationKind.Overwrite && _confirmationSlot == slot.Value)
            return await ExecuteSaveAsync(slot.Value, cancellationToken).ConfigureAwait(false);

        if (_confirmationKind == SaveLoadConfirmationKind.Delete && _confirmationSlot == slot.Value)
            return ExecuteDelete(slot.Value);

        ClearConfirmation();

        if (Mode == SaveLoadMenuMode.Save)
            return await RequestSaveAsync(slot.Value, cancellationToken).ConfigureAwait(false);

        return await RequestLoadAsync(slot.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SaveResult> RequestSaveAsync(SaveSlotId slotId, CancellationToken cancellationToken)
    {
        if (_blocker.IsSaveBlocked)
        {
            _lastMessage = _blocker.BlockReason;
            return SaveResult.Fail(_blocker.BlockReason, slotId);
        }

        if (slotId.Kind == SaveSlotKind.Auto)
        {
            _lastMessage = "自动存档槽不可手动覆盖。";
            return SaveResult.Fail(_lastMessage, slotId);
        }

        if (_saveManager.IsSlotOccupied(slotId))
        {
            _confirmationKind = SaveLoadConfirmationKind.Overwrite;
            _confirmationSlot = slotId;
            _lastMessage = null;
            return SaveResult.Ok(slotId);
        }

        return await ExecuteSaveAsync(slotId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SaveResult> ExecuteSaveAsync(SaveSlotId slotId, CancellationToken cancellationToken)
    {
        ClearConfirmation();
        var result = await _saveManager.SaveGameAsync(slotId, cancellationToken).ConfigureAwait(false);
        _lastMessage = result.Success ? "存档完成。" : result.Error;
        return result;
    }

    private async Task<SaveResult> RequestLoadAsync(SaveSlotId slotId, CancellationToken cancellationToken)
    {
        if (!_saveManager.IsSlotOccupied(slotId))
        {
            _lastMessage = "该槽位为空，无法读档。";
            return SaveResult.Fail(_lastMessage, slotId);
        }

        var result = await _saveManager.LoadGameAsync(slotId, cancellationToken).ConfigureAwait(false);
        _lastMessage = result.Success ? "读档完成。" : result.Error;
        return result;
    }

    private Task<SaveResult> RequestDeleteAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var slot = GetSelectedSlot();
        if (slot == null)
            return Task.FromResult(SaveResult.Fail("没有可选择的槽位。"));

        if (!_saveManager.IsSlotOccupied(slot.Value))
        {
            _lastMessage = "空槽无需删除。";
            return Task.FromResult(SaveResult.Fail(_lastMessage, slot.Value));
        }

        _confirmationKind = SaveLoadConfirmationKind.Delete;
        _confirmationSlot = slot.Value;
        _lastMessage = null;
        return Task.FromResult(SaveResult.Ok(slot.Value));
    }

    private SaveResult ExecuteDelete(SaveSlotId slotId)
    {
        ClearConfirmation();
        var result = _saveManager.DeleteSlot(slotId);
        _lastMessage = result.Success ? "存档已删除。" : result.Error;
        return result;
    }

    private SaveResult MoveSelection(int delta)
    {
        var slots = _saveManager.ListSlots();
        if (slots.Count == 0)
            return SaveResult.Fail("没有可选择的槽位。");

        ClearConfirmation();
        _selectedIndex = ((_selectedIndex + delta) % slots.Count + slots.Count) % slots.Count;
        return SaveResult.Ok(slots[_selectedIndex].SlotId);
    }

    private SaveResult CancelConfirmation()
    {
        ClearConfirmation();
        _lastMessage = null;
        return SaveResult.Ok(GetSelectedSlot());
    }

    private void ClearConfirmation()
    {
        _confirmationKind = SaveLoadConfirmationKind.None;
        _confirmationSlot = null;
    }

    private SaveSlotId? GetSelectedSlot()
    {
        var slots = _saveManager.ListSlots();
        return _selectedIndex >= 0 && _selectedIndex < slots.Count
            ? slots[_selectedIndex].SlotId
            : null;
    }

    private string? BuildConfirmationText()
    {
        if (_confirmationSlot == null || _confirmationKind == SaveLoadConfirmationKind.None)
            return null;

        return _confirmationKind switch
        {
            SaveLoadConfirmationKind.Overwrite => $"覆盖 {_confirmationSlot.Value.Name} 的已有存档？",
            SaveLoadConfirmationKind.Delete => $"删除 {_confirmationSlot.Value.Name} 的已有存档？",
            _ => null
        };
    }

    private static string GetSlotTitle(SaveSlotId slotId)
    {
        return slotId.Kind == SaveSlotKind.Auto
            ? "自动存档"
            : $"存档 {slotId.ManualIndex:00}";
    }

    private static string GetStatusText(SlotMetadata metadata)
    {
        if (!metadata.IsOccupied)
            return "空（可存档）";

        return metadata.SlotId.Kind == SaveSlotKind.Auto ? "自动槽" : "已有存档";
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp)
    {
        return timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
    }

    private static string FormatPlaytime(TimeSpan playtime)
    {
        var totalHours = (int)playtime.TotalHours;
        return $"{totalHours:00}:{playtime.Minutes:00}";
    }
}
