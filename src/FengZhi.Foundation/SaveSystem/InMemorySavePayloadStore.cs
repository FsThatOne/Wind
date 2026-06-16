namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 测试与早期集成使用的内存 payload 存储。真实磁盘、加密与迁移由后续 story 替换。
/// </summary>
public sealed partial class InMemorySavePayloadStore : ISavePayloadStore
{
    private readonly Dictionary<SaveSlotId, SavePayload> _payloads = new();

    /// <inheritdoc />
    public Task<SaveResult> WriteAsync(SaveSlotId slotId, SavePayload payload, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(SaveResult.Fail("保存已取消。", slotId));

        _payloads[slotId] = payload with
        {
            Metadata = payload.Metadata with
            {
                SlotId = slotId,
                IsOccupied = true,
                Timestamp = payload.Metadata.Timestamp ?? DateTimeOffset.UtcNow
            }
        };
        return Task.FromResult(SaveResult.Ok(slotId));
    }

    /// <inheritdoc />
    public Task<(SaveResult Result, SavePayload? Payload)> ReadAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(((SaveResult Result, SavePayload? Payload))(SaveResult.Fail("读取已取消。", slotId), null));

        if (!_payloads.TryGetValue(slotId, out var payload))
            return Task.FromResult(((SaveResult Result, SavePayload? Payload))(SaveResult.Fail("槽位为空。", slotId), null));

        return Task.FromResult(((SaveResult Result, SavePayload? Payload))(SaveResult.Ok(slotId), payload));
    }

    /// <inheritdoc />
    public IReadOnlyList<SlotMetadata> ListSlots()
    {
        return SaveSlotId.AllSlots()
            .Select(GetMetadata)
            .ToList();
    }

    /// <inheritdoc />
    public SaveResult Delete(SaveSlotId slotId)
    {
        _payloads.Remove(slotId);
        return SaveResult.Ok(slotId);
    }

    /// <inheritdoc />
    public SlotMetadata GetMetadata(SaveSlotId slotId)
    {
        return _payloads.TryGetValue(slotId, out var payload)
            ? payload.Metadata
            : SlotMetadata.Empty(slotId);
    }

    /// <inheritdoc />
    public bool Exists(SaveSlotId slotId) => _payloads.ContainsKey(slotId);
}
