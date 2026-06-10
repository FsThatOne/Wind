// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using System;

namespace FengzhiSlice;

/// <summary>
/// Typed event bus for cross-layer communication. 
/// Production version would be a Godot Autoload; this is simplified for the slice.
/// </summary>
public static class EventBus
{
    // Combat events
    public static event Action? CombatStarted;
    public static event Action<CombatResult>? CombatEnded;
    public static event Action<TurnResult>? TurnResolved;
    
    // Narrative events
    public static event Action<string>? DialogueStarted;
    public static event Action? DialogueEnded;
    public static event Action<MindsetChoice>? MindsetChoiceMade;
    
    // UI events
    public static event Action<string>? ShowBlurredFeedback;
    
    public static void PublishCombatStarted() => CombatStarted?.Invoke();
    public static void PublishCombatEnded(CombatResult result) => CombatEnded?.Invoke(result);
    public static void PublishTurnResolved(TurnResult result) => TurnResolved?.Invoke(result);
    public static void PublishDialogueStarted(string dialogueId) => DialogueStarted?.Invoke(dialogueId);
    public static void PublishDialogueEnded() => DialogueEnded?.Invoke();
    public static void PublishMindsetChoice(MindsetChoice choice) => MindsetChoiceMade?.Invoke(choice);
    public static void PublishBlurredFeedback(string text) => ShowBlurredFeedback?.Invoke(text);
    
    public static void Reset()
    {
        CombatStarted = null;
        CombatEnded = null;
        TurnResolved = null;
        DialogueStarted = null;
        DialogueEnded = null;
        MindsetChoiceMade = null;
        ShowBlurredFeedback = null;
    }
}

public record CombatResult(bool PlayerWon, int TurnsElapsed, bool UsedBurst);
public record TurnResult(string PlayerMove, string EnemyMove, int PlayerDamage, int EnemyDamage, bool WasCounter);
public record MindsetChoice(string ChoiceId, float ObsessionDelta, float EngagementDelta);
