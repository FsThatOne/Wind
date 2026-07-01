namespace FengZhi.Foundation.Cutscene;

public sealed class CutsceneScript
{
    public string Id { get; init; } = string.Empty;
    public CutsceneTier Tier { get; init; }
    public bool Skippable { get; init; } = true;
    public bool FirstViewUnskippable { get; init; }
    public LockMode LockMode { get; init; }
    public string? BgmOverride { get; init; }
    public IReadOnlyList<CutsceneStep> Steps { get; init; } = Array.Empty<CutsceneStep>();
    public IReadOnlyList<GameplayEffect> OnComplete { get; init; } = Array.Empty<GameplayEffect>();
}
