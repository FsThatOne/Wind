namespace FengZhi.Foundation.LivingJianghu;

public sealed class WorldEventConfig
{
    public required string Id { get; init; }
    public WorldEventType Type { get; init; }
    public int ChapterMin { get; init; }
    public int ChapterMax { get; init; } = int.MaxValue;
    public int Priority { get; init; } = 50;
    public int Cooldown { get; init; }
    public bool Repeatable { get; init; }
    public bool BreathingOnly { get; init; }
    public DeliveryMethod DeliveryMethod { get; init; } = DeliveryMethod.Storyteller;
    public string ContentKey { get; init; } = "";
    public string? SourceRegion { get; init; }
    public int? ExpireDay { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<Precondition> Preconditions { get; init; } = [];
    public IReadOnlyList<TriggerAction> OnTrigger { get; init; } = [];
    public IReadOnlyList<TriggerAction> OnExpire { get; init; } = [];
}

public sealed class TriggerAction
{
    public TriggerActionType ActionType { get; init; }
    public string? FlagToSet { get; init; }
    public NpcStateChange? NpcChange { get; init; }
    public string? RegisterEventId { get; init; }
}

public enum TriggerActionType
{
    SetFlag,
    NpcStateChange,
    RegisterDelayedEvent
}

public sealed class NpcStateChange
{
    public required string NpcId { get; init; }
    public required string Field { get; init; }
    public required string Value { get; init; }
    public string? SourceEvent { get; init; }
}
