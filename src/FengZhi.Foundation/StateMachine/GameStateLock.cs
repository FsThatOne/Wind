namespace FengZhi.Foundation.StateMachine;

/// <summary>
/// Shared game-state lock severity used by systems that must pause during combat, dialogue, or transitions.
/// </summary>
public enum LockMode
{
    None = 0,
    Partial = 1,
    Full = 2
}
