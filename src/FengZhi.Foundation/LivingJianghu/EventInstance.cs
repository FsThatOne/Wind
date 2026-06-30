namespace FengZhi.Foundation.LivingJianghu;

public sealed class EventInstance
{
    public string ConfigId { get; }
    public WorldEventState State { get; set; } = WorldEventState.Inactive;
    public int BacklogDays { get; set; }
    public int TriggerDay { get; set; }
    public int? PropagationReadyDay { get; set; }
    public int CooldownUntilDay { get; set; }

    public EventInstance(string configId)
    {
        ConfigId = configId;
    }

    public bool IsDone =>
        State == WorldEventState.Delivered || State == WorldEventState.Expired;
}
