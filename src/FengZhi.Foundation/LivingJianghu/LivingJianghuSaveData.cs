namespace FengZhi.Foundation.LivingJianghu;

public sealed class LivingJianghuSaveData
{
    public List<EventInstanceData> EventStates { get; set; } = [];
    public List<string> DeliveredQueue { get; set; } = [];
}

public sealed class EventInstanceData
{
    public required string ConfigId { get; set; }
    public WorldEventState State { get; set; }
    public int BacklogDays { get; set; }
    public int TriggerDay { get; set; }
    public int? PropagationReadyDay { get; set; }
    public int CooldownUntilDay { get; set; }
}
