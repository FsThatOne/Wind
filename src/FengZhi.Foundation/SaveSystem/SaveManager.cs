using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// SaveManager 核心分发器，负责汇总 ISaveable、读回 payload 并发布完成事件。
/// </summary>
public sealed partial class SaveManager : ISaveManager
{
    private readonly ISavePayloadStore _store;
    private readonly IEventBus _eventBus;
    private readonly Func<SaveSlotId, SlotMetadata> _metadataFactory;
    private readonly Dictionary<string, ISaveable> _saveables = new(StringComparer.Ordinal);

    /// <summary>创建 SaveManager。</summary>
    public SaveManager(
        ISavePayloadStore store,
        IEventBus eventBus,
        Func<SaveSlotId, SlotMetadata>? metadataFactory = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _metadataFactory = metadataFactory ?? (slotId => SlotMetadata.Empty(slotId));
    }

    /// <inheritdoc />
    public SaveResult RegisterSerializer(ISaveable saveable)
    {
        if (saveable == null)
            throw new ArgumentNullException(nameof(saveable));

        if (string.IsNullOrWhiteSpace(saveable.SaveKey))
            return SaveResult.Fail("SaveKey 不能为空。");

        if (_saveables.ContainsKey(saveable.SaveKey))
            return SaveResult.Fail($"重复的 SaveKey：{saveable.SaveKey}");

        _saveables.Add(saveable.SaveKey, saveable);
        return SaveResult.Ok();
    }

    /// <inheritdoc />
    public async Task<SaveResult> SaveGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = _metadataFactory(slotId) with { SlotId = slotId, IsOccupied = true };
            var payload = SavePayload.Create(metadata);

            foreach (var (key, saveable) in _saveables)
                payload.Systems[key] = saveable.Serialize();

            var result = await _store.WriteAsync(slotId, payload, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                return result;

            var writtenMetadata = _store.GetMetadata(slotId);
            _eventBus.Publish(new SaveCompletedEvent(slotId, writtenMetadata));
            return SaveResult.Ok(slotId);
        }
        catch (Exception ex)
        {
            return SaveResult.Fail(ex.Message, slotId);
        }
    }

    /// <inheritdoc />
    public async Task<SaveResult> LoadGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var (result, payload) = await _store.ReadAsync(slotId, cancellationToken).ConfigureAwait(false);
            if (!result.Success || payload == null)
                return result;

            foreach (var (key, snapshot) in payload.Systems)
            {
                if (_saveables.TryGetValue(key, out var saveable))
                    saveable.Deserialize(snapshot, payload.SchemaVersion);
            }

            _eventBus.Publish(new SaveLoadedEvent(slotId, payload.Metadata));
            return SaveResult.Ok(slotId);
        }
        catch (Exception ex)
        {
            return SaveResult.Fail(ex.Message, slotId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SlotMetadata> ListSlots() => _store.ListSlots();

    /// <inheritdoc />
    public SaveResult DeleteSlot(SaveSlotId slotId) => _store.Delete(slotId);

    /// <inheritdoc />
    public SlotMetadata GetMetadata(SaveSlotId slotId) => _store.GetMetadata(slotId);

    /// <inheritdoc />
    public bool IsSlotOccupied(SaveSlotId slotId) => _store.Exists(slotId);
}
