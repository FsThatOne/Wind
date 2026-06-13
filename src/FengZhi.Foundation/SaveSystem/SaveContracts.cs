namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 可被存档系统收集和恢复的运行时系统。
/// </summary>
public interface ISaveable
{
    /// <summary>系统存档 key，进入 payload 的 systems 字典后不可重命名。</summary>
    string SaveKey { get; }

    /// <summary>序列化当前系统状态。</summary>
    SaveSnapshot Serialize();

    /// <summary>按指定 schema 版本恢复系统状态。</summary>
    void Deserialize(SaveSnapshot snapshot, int version);
}

/// <summary>
/// 单步 schema 迁移。迁移必须链式执行，不跳版本。
/// </summary>
public interface IMigration
{
    /// <summary>源版本。</summary>
    int FromVersion { get; }

    /// <summary>目标版本，必须等于 FromVersion + 1。</summary>
    int ToVersion { get; }

    /// <summary>迁移 payload。</summary>
    SavePayload Migrate(SavePayload payload);
}

/// <summary>
/// 存档管理器对游戏系统与 UI 暴露的最小契约。
/// </summary>
public interface ISaveManager
{
    /// <summary>注册一个可存档系统。重复 key 会被拒绝。</summary>
    SaveResult RegisterSerializer(ISaveable saveable);

    /// <summary>保存到指定槽位。</summary>
    Task<SaveResult> SaveGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default);

    /// <summary>从指定槽位加载。</summary>
    Task<SaveResult> LoadGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default);

    /// <summary>列出全部槽位元数据。</summary>
    IReadOnlyList<SlotMetadata> ListSlots();

    /// <summary>删除指定槽位。</summary>
    SaveResult DeleteSlot(SaveSlotId slotId);

    /// <summary>读取单个槽位元数据。</summary>
    SlotMetadata GetMetadata(SaveSlotId slotId);

    /// <summary>槽位是否已有存档。</summary>
    bool IsSlotOccupied(SaveSlotId slotId);
}

/// <summary>
/// SaveManager 底层 payload 存储抽象。ss-003/ss-004 会替换为加密文件实现。
/// </summary>
public interface ISavePayloadStore
{
    /// <summary>写入 payload。</summary>
    Task<SaveResult> WriteAsync(SaveSlotId slotId, SavePayload payload, CancellationToken cancellationToken = default);

    /// <summary>读取 payload；空槽或失败时返回 null 并通过 result 描述错误。</summary>
    Task<(SaveResult Result, SavePayload? Payload)> ReadAsync(SaveSlotId slotId, CancellationToken cancellationToken = default);

    /// <summary>列出全部槽位元数据。</summary>
    IReadOnlyList<SlotMetadata> ListSlots();

    /// <summary>删除指定槽位。</summary>
    SaveResult Delete(SaveSlotId slotId);

    /// <summary>读取槽位元数据。</summary>
    SlotMetadata GetMetadata(SaveSlotId slotId);

    /// <summary>槽位是否已有 payload。</summary>
    bool Exists(SaveSlotId slotId);
}
