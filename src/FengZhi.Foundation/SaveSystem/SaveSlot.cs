namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档槽位标识。手动槽固定为 slot_01 到 slot_10，自动槽固定为 autosave。
/// </summary>
public readonly partial record struct SaveSlotId
{
    /// <summary>手动槽数量。</summary>
    public const int ManualSlotCount = 10;

    /// <summary>自动槽文件名。</summary>
    public const string AutoSaveName = "autosave";

    /// <summary>槽位文件名，如 slot_01 或 autosave。</summary>
    public string Name { get; }

    /// <summary>槽位种类。</summary>
    public SaveSlotKind Kind { get; }

    /// <summary>手动槽编号，自动槽为 null。</summary>
    public int? ManualIndex { get; }

    private SaveSlotId(string name, SaveSlotKind kind, int? manualIndex)
    {
        Name = name;
        Kind = kind;
        ManualIndex = manualIndex;
    }

    /// <summary>自动存档槽。</summary>
    public static SaveSlotId AutoSave { get; } = new(AutoSaveName, SaveSlotKind.Auto, null);

    /// <summary>创建手动槽，编号范围 1..10。</summary>
    public static SaveSlotId Manual(int index)
    {
        if (index is < 1 or > ManualSlotCount)
            throw new ArgumentOutOfRangeException(nameof(index), $"手动槽编号必须在 1..{ManualSlotCount} 之间。");

        return new SaveSlotId($"slot_{index:00}", SaveSlotKind.Manual, index);
    }

    /// <summary>返回 10 个手动槽加 1 个自动槽，顺序稳定供 UI 使用。</summary>
    public static IReadOnlyList<SaveSlotId> AllSlots()
    {
        var slots = new List<SaveSlotId>(ManualSlotCount + 1);
        for (var i = 1; i <= ManualSlotCount; i++)
            slots.Add(Manual(i));
        slots.Add(AutoSave);
        return slots;
    }

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// 槽位种类。
/// </summary>
public enum SaveSlotKind
{
    /// <summary>玩家手动保存的槽位。</summary>
    Manual,

    /// <summary>系统自动保存的槽位。</summary>
    Auto
}
