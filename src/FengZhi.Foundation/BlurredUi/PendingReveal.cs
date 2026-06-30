namespace FengZhi.Foundation.BlurredUi;

public sealed class PendingReveal
{
    public RevealChannel Channel { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Payload { get; init; }
    public long Timestamp { get; init; }
}

public sealed class RealmRevealPayload
{
    public int PreviousTier { get; init; }
    public int NewTier { get; init; }
    public string RealmName { get; init; } = string.Empty;
    public bool IsMultiBreakthrough { get; init; }
}

public sealed class MindsetRevealPayload
{
    public string PreviousZone { get; init; } = string.Empty;
    public string NewZone { get; init; } = string.Empty;
    public TintColor TargetTint { get; init; }
}

public sealed class RelationshipRevealPayload
{
    public string NpcId { get; init; } = string.Empty;
    public string PreviousTier { get; init; } = string.Empty;
    public string NewTier { get; init; } = string.Empty;
    public bool IsForceBreak { get; init; }
}

public readonly struct TintColor
{
    public float R { get; init; }
    public float G { get; init; }
    public float B { get; init; }

    public TintColor(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
    }

    public static TintColor Zero => new(0f, 0f, 0f);
}
