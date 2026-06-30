namespace FengZhi.Foundation.BlurredUi;

public sealed class BlurredUiSaveData
{
    public List<PendingReveal> PendingReveals { get; init; } = new();
    public int LastKnownRealmTier { get; init; }
    public string LastKnownMindsetZone { get; init; } = "中庸";
}
