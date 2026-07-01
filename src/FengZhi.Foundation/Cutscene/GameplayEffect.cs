namespace FengZhi.Foundation.Cutscene;

public enum EffectType
{
    SetFlag,
    ApplyAttribute,
    UnlockMove,
    UnlockNarrativeOption,
    ResumeCombat,
    TriggerEvent,
    Custom
}

public sealed class GameplayEffect
{
    public EffectType Type { get; init; }
    public string Key { get; init; } = string.Empty;
    public string? Value { get; init; }
    public Dictionary<string, int>? AttributeChanges { get; init; }
}
