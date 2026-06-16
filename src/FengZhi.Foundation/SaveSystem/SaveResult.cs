namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档系统对外返回的统一结果。
/// </summary>
public sealed partial record SaveResult
{
    /// <summary>是否成功。</summary>
    public bool Success { get; init; }

    /// <summary>失败原因；成功时为空。</summary>
    public string? Error { get; init; }

    /// <summary>本次操作关联的槽位。</summary>
    public SaveSlotId? SlotId { get; init; }

    /// <summary>成功结果。</summary>
    public static SaveResult Ok(SaveSlotId? slotId = null) => new() { Success = true, SlotId = slotId };

    /// <summary>失败结果。</summary>
    public static SaveResult Fail(string error, SaveSlotId? slotId = null) => new() { Success = false, Error = error, SlotId = slotId };
}
