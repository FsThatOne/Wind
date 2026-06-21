using System;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using Godot;

namespace FengZhi.Foundation.CombatUi.GodotIntegration;

/// <summary>
/// Stateless filter that decides whether an <see cref="InputEvent"/> should be
/// consumed by the cinematic lock. Caller (typically a Godot input filter Node)
/// invokes <see cref="ShouldConsume"/> from <c>_UnhandledInput</c> and, when it
/// returns true, calls <c>GetViewport().SetInputAsHandled()</c>.
///
/// Behaviour:
///  - When the lock is not locked, never consumes (returns false).
///  - When locked, iterates registered input actions and collects the set of
///    actions the event matches. The decision is "any-allowed":
///      * If the event matches no registered action, do NOT consume (passthrough).
///      * If ANY matched action is in the cinematic-lock whitelist, do NOT consume.
///      * Otherwise (event matches at least one action and none are whitelisted),
///        consume.
///    Rationale: a single key (e.g. Escape) frequently maps to multiple actions
///    in Godot's InputMap (built-in <c>ui_cancel</c> + a project-specific
///    <c>ui_pause</c>). Treating any whitelisted hit as "allowed" preserves
///    pause/system-back even when other non-whitelisted actions also bind it.
///
/// AC-4 binding: never mutates <see cref="InputMap"/>; only reads
/// <see cref="InputMap.GetActions"/> + <see cref="InputMap.EventIsAction"/>.
/// </summary>
public static class CombatCinematicLockInputFilter
{
    public static bool ShouldConsume(InputEvent evt, CombatCinematicLock cinematicLock)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(cinematicLock);
        if (!cinematicLock.IsLocked) return false;

        bool anyMatched = false;
        bool anyAllowed = false;
        foreach (var actionName in InputMap.GetActions())
        {
            string action = actionName.ToString();
            if (!InputMap.EventIsAction(evt, action)) continue;
            anyMatched = true;
            if (cinematicLock.IsAllowedDuringLock(action))
            {
                anyAllowed = true;
                break;
            }
        }
        if (!anyMatched) return false;
        return !anyAllowed;
    }
}
