using System.Text.Json;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 加密前的 JSON payload，包含槽位元数据与各系统的持久化快照。
/// </summary>
public sealed partial record SavePayload
{
    /// <summary>当前存档 schema 版本。</summary>
    public int SchemaVersion { get; init; } = SaveFileHeader.InitialSchemaVersion;

    /// <summary>槽位列表 UI 可直接读取的元数据。</summary>
    public SlotMetadata Metadata { get; init; } = SlotMetadata.Empty(SaveSlotId.AutoSave);

    /// <summary>按 ISaveable.SaveKey 索引的系统快照。</summary>
    public Dictionary<string, SaveSnapshot> Systems { get; init; } = new(StringComparer.Ordinal);

    /// <summary>创建空 payload，供 SaveManager 汇总系统快照时使用。</summary>
    public static SavePayload Create(SlotMetadata metadata)
    {
        return new SavePayload { Metadata = metadata };
    }
}

/// <summary>
/// 单个系统的 JSON 对象快照。值使用 JsonElement，避免反序列化后 object 类型不稳定。
/// </summary>
public sealed partial record SaveSnapshot
{
    /// <summary>系统数据字段。</summary>
    public Dictionary<string, JsonElement> Values { get; init; } = new(StringComparer.Ordinal);

    /// <summary>空快照。</summary>
    public static SaveSnapshot Empty { get; } = new();
}
