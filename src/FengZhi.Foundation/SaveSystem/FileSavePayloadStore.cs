using System.Text.Json;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// ISavePayloadStore 的加密文件实现。将 payload 序列化为 JSON，加密后写入磁盘。
/// </summary>
public sealed partial class FileSavePayloadStore : ISavePayloadStore
{
    private readonly string _basePath;
    private readonly SaveCryptoService _crypto;
    private readonly MigrationChain _migrationChain;
    private readonly int _targetSchemaVersion;
    private readonly SaveDebugFileService? _debugFileService;
    private readonly Dictionary<SaveSlotId, SlotMetadata> _metadataCache = new();

    public FileSavePayloadStore(
        string basePath,
        SaveCryptoService crypto,
        MigrationChain migrationChain,
        int targetSchemaVersion = SaveFileHeader.InitialSchemaVersion,
        SaveDebugFileService? debugFileService = null)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
        _migrationChain = migrationChain ?? throw new ArgumentNullException(nameof(migrationChain));
        _targetSchemaVersion = targetSchemaVersion;
        _debugFileService = debugFileService;
    }

    /// <summary>扫描存档目录，填充元数据缓存。应在首次使用前调用。</summary>
    public void Initialize()
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        foreach (var slotId in SaveSlotId.AllSlots())
        {
            var filePath = GetFilePath(slotId);
            if (File.Exists(filePath))
            {
                var metadata = TryReadMetadataFromFile(filePath, slotId);
                _metadataCache[slotId] = metadata ?? SlotMetadata.Empty(slotId);
            }
            else
            {
                _metadataCache[slotId] = SlotMetadata.Empty(slotId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<SaveResult> WriteAsync(SaveSlotId slotId, SavePayload payload, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return SaveResult.Fail("保存已取消。", slotId);

        try
        {
            var finalPayload = payload with
            {
                Metadata = payload.Metadata with
                {
                    SlotId = slotId,
                    IsOccupied = true,
                    Timestamp = payload.Metadata.Timestamp ?? DateTimeOffset.UtcNow
                }
            };

            var json = JsonSerializer.Serialize(finalPayload, SerializerOptions);
            var encrypted = _crypto.EncryptJson(json, finalPayload.SchemaVersion);
            var filePath = GetFilePath(slotId);

            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);

            await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken).ConfigureAwait(false);

            _metadataCache[slotId] = finalPayload.Metadata;

            if (_debugFileService != null)
                await _debugFileService.WriteDevJsonAsync(filePath, json, cancellationToken).ConfigureAwait(false);

            return SaveResult.Ok(slotId);
        }
        catch (OperationCanceledException)
        {
            return SaveResult.Fail("保存已取消。", slotId);
        }
        catch (Exception ex)
        {
            return SaveResult.Fail($"写入存档失败：{ex.Message}", slotId);
        }
    }

    /// <inheritdoc />
    public async Task<(SaveResult Result, SavePayload? Payload)> ReadAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return (SaveResult.Fail("读取已取消。", slotId), null);

        var filePath = GetFilePath(slotId);
        if (!File.Exists(filePath))
            return (SaveResult.Fail("槽位为空。", slotId), null);

        try
        {
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            var decryptResult = _crypto.DecryptJson(fileBytes);
            if (!decryptResult.Success || decryptResult.Json == null)
                return (SaveResult.Fail(decryptResult.Error ?? "解密失败。", slotId), null);

            var payload = JsonSerializer.Deserialize<SavePayload>(decryptResult.Json, SerializerOptions);
            if (payload == null)
                return (SaveResult.Fail("反序列化存档失败。", slotId), null);

            if (payload.SchemaVersion < _targetSchemaVersion)
            {
                if (_debugFileService != null)
                    await _debugFileService.CreateMigrationBackupAsync(filePath, cancellationToken).ConfigureAwait(false);

                var migrationResult = await _migrationChain.ApplyAsync(payload, _targetSchemaVersion, cancellationToken).ConfigureAwait(false);
                if (!migrationResult.Success || migrationResult.Payload == null)
                    return (SaveResult.Fail(migrationResult.Error ?? "迁移失败。", slotId), null);

                payload = migrationResult.Payload;

                var reJson = JsonSerializer.Serialize(payload, SerializerOptions);
                var reEncrypted = _crypto.EncryptJson(reJson, payload.SchemaVersion);
                await File.WriteAllBytesAsync(filePath, reEncrypted, cancellationToken).ConfigureAwait(false);
            }

            _metadataCache[slotId] = payload.Metadata;
            return (SaveResult.Ok(slotId), payload);
        }
        catch (OperationCanceledException)
        {
            return (SaveResult.Fail("读取已取消。", slotId), null);
        }
        catch (Exception ex)
        {
            return (SaveResult.Fail($"读取存档失败：{ex.Message}", slotId), null);
        }
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
        var filePath = GetFilePath(slotId);
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            var devJsonPath = SaveDebugFileService.GetDevJsonPath(filePath);
            if (File.Exists(devJsonPath))
                File.Delete(devJsonPath);

            _metadataCache[slotId] = SlotMetadata.Empty(slotId);
            return SaveResult.Ok(slotId);
        }
        catch (Exception ex)
        {
            return SaveResult.Fail($"删除存档失败：{ex.Message}", slotId);
        }
    }

    /// <inheritdoc />
    public SlotMetadata GetMetadata(SaveSlotId slotId)
    {
        return _metadataCache.TryGetValue(slotId, out var metadata)
            ? metadata
            : SlotMetadata.Empty(slotId);
    }

    /// <inheritdoc />
    public bool Exists(SaveSlotId slotId)
    {
        return _metadataCache.TryGetValue(slotId, out var metadata) && metadata.IsOccupied;
    }

    private string GetFilePath(SaveSlotId slotId) => Path.Combine(_basePath, $"{slotId.Name}.sav");

    private SlotMetadata? TryReadMetadataFromFile(string filePath, SaveSlotId slotId)
    {
        try
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var decryptResult = _crypto.DecryptJson(fileBytes);
            if (!decryptResult.Success || decryptResult.Json == null)
                return null;

            var payload = JsonSerializer.Deserialize<SavePayload>(decryptResult.Json, SerializerOptions);
            if (payload == null) return null;
            return payload.Metadata with { SlotId = slotId, IsOccupied = true };
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
