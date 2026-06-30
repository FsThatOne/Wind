namespace FengZhi.Foundation.BlurredUi;

public enum BlurredUiState
{
    Blurred,
    Clear,
    TransitionToClear,
    TransitionToBlurred
}

public enum UiContext
{
    Panel,
    Dialogue,
    Exploration,
    Comparison,
    CombatResult
}

public enum RevealChannel
{
    Realm = 1,
    Mindset = 2,
    Relationship = 3
}
