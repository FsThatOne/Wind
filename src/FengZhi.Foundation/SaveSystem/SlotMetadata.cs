namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 槽位列表 UI 直接读取的存档摘要。
/// </summary>
public sealed partial record SlotMetadata
{
    /// <summary>槽位标识。</summary>
    public SaveSlotId SlotId { get; init; }

    /// <summary>该槽是否已有存档。</summary>
    public bool IsOccupied { get; init; }

    /// <summary>保存发生的 UTC 时间。空槽为 null。</summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>当前章节名。</summary>
    public string ChapterName { get; init; } = string.Empty;

    /// <summary>当前场景名。</summary>
    public string SceneName { get; init; } = string.Empty;

    /// <summary>游戏内天数。</summary>
    public int GameDay { get; init; }

    /// <summary>累计游戏时长。</summary>
    public TimeSpan Playtime { get; init; } = TimeSpan.Zero;

    /// <summary>缩略图 PNG 字节；空槽或无图时为空数组。</summary>
    public byte[] ThumbnailPng { get; init; } = Array.Empty<byte>();

    /// <summary>是否为二周目存档。</summary>
    public bool IsNewGamePlus { get; init; }

    /// <summary>创建一个空槽元数据。</summary>
    public static SlotMetadata Empty(SaveSlotId slotId)
    {
        return new SlotMetadata { SlotId = slotId };
    }
}
