namespace FengZhi.Foundation.Cutscene;

public sealed class CutsceneStep
{
    public StepType Type { get; init; }
    public float Duration { get; init; }

    // ShowImage
    public string? ImageId { get; init; }
    public TransitionType ImageTransition { get; init; }

    // ShowText
    public string? Text { get; init; }
    public TextStyle TextStyle { get; init; }

    // PlayAnimation
    public string? AnimationId { get; init; }
    public string? AnimationTarget { get; init; }
    public bool Loop { get; init; }

    // CameraMove
    public float CameraZoom { get; init; } = 1f;
    public EasingType CameraEasing { get; init; }

    // SlowMotion
    public float TimeScale { get; init; } = 1f;

    // ScreenEffect
    public ScreenEffectType ScreenEffect { get; init; }

    // PlaySfx / PlayBgm
    public string? AudioId { get; init; }
    public float Volume { get; init; } = 1f;
    public int FadeInMs { get; init; }

    // WaitInput
    public string? PromptText { get; init; }

    // Parallel
    public IReadOnlyList<CutsceneStep>? ParallelSteps { get; init; }

    public float GetEffectiveDuration()
    {
        if (Type == StepType.Parallel && ParallelSteps is { Count: > 0 })
            return ParallelSteps.Max(s => s.Duration);
        if (Type == StepType.WaitInput)
            return float.MaxValue;
        return Duration;
    }
}
