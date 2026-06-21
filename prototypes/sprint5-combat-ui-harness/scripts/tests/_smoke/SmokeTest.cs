using Godot;

namespace Sprint5CombatUiHarness.Tests.Smoke;

/// <summary>
/// Stage 1 smoke — confirms the GodotTestRunner reflection pipeline works under
/// Godot 4.7-stable: discovers [GodotTest] methods, executes them, prints
/// PASS/FAIL, and exits with code = failure count.
/// </summary>
public partial class SmokeTest : GodotTestRunner
{
    [GodotTest("smoke.engine_time_scale_writable_and_restorable")]
    public void EngineTimeScaleWritableAndRestorable()
    {
        Engine.TimeScale = 0.5d;
        TestAssert.ApproxEqual(0.5d, Engine.TimeScale, 0.001d, "Engine.TimeScale should accept 0.5 write");
        Engine.TimeScale = 1.0d;
        TestAssert.ApproxEqual(1.0d, Engine.TimeScale, 0.001d, "Engine.TimeScale should restore to 1.0");
    }

    [GodotTest("smoke.input_map_get_actions_non_empty")]
    public void InputMapGetActionsNonEmpty()
    {
        var actions = InputMap.GetActions();
        TestAssert.True(actions.Count > 0, "InputMap should have at least one registered action");
    }
}
