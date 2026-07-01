namespace FengZhi.Foundation.Cutscene;

public enum CutsceneState
{
    Idle,
    Loading,
    Playing,
    Skipping,
    CompletingEffects,
    Completed
}

public enum CutsceneTier
{
    FullScreenCg = 1,
    FullScreenPixel = 2,
    HalfScreen = 3,
    InlineMicro = 4
}

public enum LockMode
{
    Full,
    Partial,
    None
}

public enum StepType
{
    ShowImage,
    ShowText,
    PlayAnimation,
    CameraMove,
    SlowMotion,
    ScreenEffect,
    PlaySfx,
    PlayBgm,
    Wait,
    WaitInput,
    Parallel
}

public enum TextStyle
{
    Narration,
    Dialogue,
    Title
}

public enum ScreenEffectType
{
    Fade,
    Flash,
    Shake,
    InkSpread
}

public enum TransitionType
{
    Fade,
    Slide,
    Dissolve,
    Cut
}

public enum EasingType
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut
}
