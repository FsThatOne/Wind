namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会系统可调参数。</summary>
public sealed class MisunderstandingConfig
{
    public int MaxActivePerNpc { get; init; } = 3;
    public int MinorWindowDays { get; init; } = 7;
    public int ModerateWindowDays { get; init; } = 5;
    public int SevereCountdownDays { get; init; } = 3;
    public int HiddenDurationDays { get; init; } = 1;
    public float PerceivedThresholdRatio { get; init; } = 0.5f;
    public int ResolveAttitudeBonus { get; init; } = 1;
    public int ResolveBonusDurationDays { get; init; } = 3;
    public int AbsenceTriggerThresholdDays { get; init; } = 3;
}
