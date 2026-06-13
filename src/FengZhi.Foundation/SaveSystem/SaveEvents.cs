using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档成功完成事件。
/// </summary>
public sealed partial record SaveCompletedEvent(SaveSlotId SlotId, SlotMetadata Metadata) : GameEvent;

/// <summary>
/// 读档成功完成事件。
/// </summary>
public sealed partial record SaveLoadedEvent(SaveSlotId SlotId, SlotMetadata Metadata) : GameEvent;
