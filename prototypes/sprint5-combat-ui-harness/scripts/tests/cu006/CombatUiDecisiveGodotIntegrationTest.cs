using System;
using System.Collections.Generic;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using FengZhi.Foundation.CombatUi.GodotIntegration;
using Godot;

namespace Sprint5CombatUiHarness.Tests.Cu006;

/// <summary>
/// cu-006 BUILD AC 1..6 implementation tests against Godot 4.7-stable real APIs.
/// Each [GodotTest] reproduces one acceptance criterion end-to-end and asserts
/// both the active state and the cleanup pathway.
///
/// Phase timing reference (Foundation contract):
///   Phase1 instant + Phase2 0.3 + Phase3 0.3 + Phase4 1.0 + Phase5 0.6
///   + Phase6 0.3 + Phase7 0.3 = 2.8s total
/// Single Tick(5.0) is sufficient to drive Idle → Completed.
/// </summary>
public partial class CombatUiDecisiveGodotIntegrationTest : GodotTestRunner
{
    private const string ActorId = "hero";
    private const string TargetId = "boss_xuan_ming";

    [GodotTest("cu006.ac1.time_scale_bridge_writes_engine_time_scale_and_dispose_restores")]
    public void Ac1_TimeScaleBridge_WritesEngineTimeScale_AndDisposeRestores()
    {
        var controller = new TimeScaleController();
        Engine.TimeScale = 1.0d;

        using (var bridge = new TimeScaleEngineBridge(controller))
        {
            TestAssert.ApproxEqual(1.0d, Engine.TimeScale, 0.001d,
                "AC1: bridge construction must mirror controller default scale onto Engine.TimeScale");

            using var slow = controller.Request(0.2, DecisiveTuning.DecisivePriority, "decisive_slow_motion");
            TestAssert.ApproxEqual(0.2d, Engine.TimeScale, 0.001d,
                "AC1: pushing 0.2 priority-50 should propagate to Engine.TimeScale");

            using (var pause = controller.Request(0.0, DecisiveTuning.PausePriority, "ui_pause"))
            {
                TestAssert.ApproxEqual(0.0d, Engine.TimeScale, 0.001d,
                    "AC1: priority-100 pause must override the slow request");
            }

            TestAssert.ApproxEqual(0.2d, Engine.TimeScale, 0.001d,
                "AC1: releasing pause must fall back to the slow request");
        }

        TestAssert.ApproxEqual(1.0d, Engine.TimeScale, 0.001d,
            "AC1: bridge dispose must restore Engine.TimeScale to 1.0");
    }

    [GodotTest("cu006.ac2.director_tick_publishes_seven_phase_advanced_events")]
    public void Ac2_Director_Tick_PublishesSevenPhaseAdvancedEvents()
    {
        var (bus, ts, camera, cinematicLock, director) = BuildDirector();
        var phases = new List<DecisiveStrikePhaseAdvancedEvent>();
        bus.Subscribe<DecisiveStrikePhaseAdvancedEvent>(phases.Add);

        director.RequestDecisiveStrike(NewRequest());
        DriveDirectorToCompletion(director);

        TestAssert.Equal(7, phases.Count,
            "AC2: PhaseAdvanced must fire exactly seven times (Phase1..Phase7)");
        for (int i = 0; i < 7; i++)
        {
            TestAssert.Equal(i + 1, phases[i].PhaseIndex,
                $"AC2: PhaseAdvanced[{i}] PhaseIndex must equal {i + 1}");
        }
        TestAssert.False(director.IsBusy, "AC2: director must be idle after sequence completion");

        director.Dispose();
    }

    [GodotTest("cu006.ac3.camera_bridge_applies_and_restores_smoothing_zoom_position")]
    public void Ac3_CameraBridge_AppliesAndRestoresSmoothingZoomPosition()
    {
        var bus = new CameraRequestBus();
        var camera = new Camera2D
        {
            Name = "Cu006TestCamera",
            PositionSmoothingEnabled = true,
            Zoom = new Vector2(1.0f, 1.0f),
            GlobalPosition = new Vector2(0, 0)
        };
        AddChild(camera);

        try
        {
            var defaultZoom = camera.Zoom;
            var defaultSmoothing = camera.PositionSmoothingEnabled;

            CameraRequestBusBridge.TargetResolver resolver = id =>
                id == TargetId ? new Vector2(123, 456) : null;

            using (var bridge = new CameraRequestBusBridge(bus, camera, resolver))
            {
                TestAssert.True(camera.PositionSmoothingEnabled == defaultSmoothing,
                    "AC3: empty bus state must keep default smoothing");
                TestAssert.ApproxEqual(defaultZoom.X, camera.Zoom.X, 0.001f,
                    "AC3: empty bus state must keep default zoom.x");

                using (var handle = bus.Request(TargetId, DecisiveTuning.DecisiveCameraZoom,
                    disableSmoothing: true, DecisiveTuning.DecisivePriority, "decisive_push_in"))
                {
                    TestAssert.False(camera.PositionSmoothingEnabled,
                        "AC3: active request with disableSmoothing=true must turn smoothing off");
                    TestAssert.ApproxEqual(DecisiveTuning.DecisiveCameraZoom, camera.Zoom.X, 0.001f,
                        "AC3: zoom.x must equal request zoom");
                    TestAssert.ApproxEqual(123.0d, camera.GlobalPosition.X, 0.001d,
                        "AC3: GlobalPosition.x must follow resolver-provided target position");
                }

                TestAssert.True(camera.PositionSmoothingEnabled == defaultSmoothing,
                    "AC3: releasing request must restore smoothing");
                TestAssert.ApproxEqual(defaultZoom.X, camera.Zoom.X, 0.001f,
                    "AC3: releasing request must restore zoom");
            }
        }
        finally
        {
            camera.QueueFree();
        }
    }

    [GodotTest("cu006.ac4.lock_filter_blocks_combat_actions_allows_ui_pause")]
    public void Ac4_LockFilter_BlocksCombatActions_AllowsUiPause()
    {
        const string CombatAction = "cu006_test_combat_select_move";
        const string PauseAction = "cu006_test_ui_pause";
        const Key CombatKey = Key.Space;
        const Key PauseKey = Key.Escape;

        bool combatActionRegistered = false;
        bool pauseActionRegistered = false;

        try
        {
            if (!InputMap.HasAction(CombatAction))
            {
                InputMap.AddAction(CombatAction);
                combatActionRegistered = true;
                var ev = new InputEventKey { Keycode = CombatKey };
                InputMap.ActionAddEvent(CombatAction, ev);
            }
            if (!InputMap.HasAction(PauseAction))
            {
                InputMap.AddAction(PauseAction);
                pauseActionRegistered = true;
                var ev = new InputEventKey { Keycode = PauseKey };
                InputMap.ActionAddEvent(PauseAction, ev);
            }

            var allowed = new HashSet<string>(StringComparer.Ordinal) { PauseAction };
            var cinematicLock = new CombatCinematicLock(allowed);

            var combatEvent = new InputEventKey { Keycode = CombatKey, Pressed = true };
            var pauseEvent = new InputEventKey { Keycode = PauseKey, Pressed = true };

            // Unlocked — never consumes.
            TestAssert.False(CombatCinematicLockInputFilter.ShouldConsume(combatEvent, cinematicLock),
                "AC4: when unlocked the filter must never consume");
            TestAssert.False(CombatCinematicLockInputFilter.ShouldConsume(pauseEvent, cinematicLock),
                "AC4: when unlocked the pause event must pass through");

            using var handle = cinematicLock.Acquire("decisive_strike");

            TestAssert.True(CombatCinematicLockInputFilter.ShouldConsume(combatEvent, cinematicLock),
                "AC4: locked state must consume combat actions outside whitelist");
            TestAssert.False(CombatCinematicLockInputFilter.ShouldConsume(pauseEvent, cinematicLock),
                "AC4: locked state must allow whitelisted pause action through");
        }
        finally
        {
            if (combatActionRegistered && InputMap.HasAction(CombatAction)) InputMap.EraseAction(CombatAction);
            if (pauseActionRegistered && InputMap.HasAction(PauseAction)) InputMap.EraseAction(PauseAction);
        }
    }

    [GodotTest("cu006.ac5.director_two_sequential_decisive_strikes_run_serialized")]
    public void Ac5_Director_TwoSequentialDecisiveStrikes_RunSerialized()
    {
        var (bus, ts, camera, cinematicLock, director) = BuildDirector();

        director.RequestDecisiveStrike(NewRequest());
        TestAssert.True(director.IsBusy,
            "AC5: director must be busy after first request");

        bool threw = false;
        try
        {
            director.RequestDecisiveStrike(NewRequest());
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        TestAssert.True(threw,
            "AC5: concurrent decisive request must throw InvalidOperationException");

        DriveDirectorToCompletion(director);
        TestAssert.False(director.IsBusy,
            "AC5: first sequence must complete before retrying");

        // Second request should now succeed.
        director.RequestDecisiveStrike(NewRequest("hero2", "boss_xuan_ming"));
        DriveDirectorToCompletion(director);
        TestAssert.False(director.IsBusy,
            "AC5: second sequence must also complete cleanly");

        director.Dispose();
    }

    [GodotTest("cu006.ac6.combat_service_request_decisive_strike_end_to_end_releases_all_side_effects")]
    public void Ac6_CombatService_RequestDecisiveStrike_EndToEnd_ReleasesAllSideEffects()
    {
        var (bus, ts, cameraBus, cinematicLock, director) = BuildDirector();
        var camera = new Camera2D
        {
            Name = "Cu006Ac6Camera",
            PositionSmoothingEnabled = true,
            Zoom = new Vector2(1.0f, 1.0f),
            GlobalPosition = new Vector2(0, 0)
        };
        AddChild(camera);
        var defaultZoom = camera.Zoom;
        var defaultSmoothing = camera.PositionSmoothingEnabled;
        Engine.TimeScale = 1.0d;

        using var tsBridge = new TimeScaleEngineBridge(ts);
        using var camBridge = new CameraRequestBusBridge(cameraBus, camera,
            id => id == TargetId ? new Vector2(50, 50) : null);

        var provider = new InMemoryDecisiveContextProvider();
        provider.Register(ActorId, TargetId, NewRequest());
        ICombatService svc = new CombatService(director, provider);

        bool completedFired = false;
        bus.Subscribe<DecisiveStrikeCompletedEvent>(_ => completedFired = true);

        try
        {
            svc.RequestDecisiveStrike(ActorId, TargetId);

            DriveDirectorToCompletion(director);

            TestAssert.True(completedFired,
                "AC6: end-to-end run must publish DecisiveStrikeCompletedEvent");
            TestAssert.False(director.IsBusy,
                "AC6: director must be idle after end-to-end run");
            TestAssert.False(cinematicLock.IsLocked,
                "AC6: cinematic lock must be released after Phase7");
            TestAssert.True(cameraBus.ActiveRequest is null,
                "AC6: camera bus must have no active request after Phase7");
            TestAssert.True(camera.PositionSmoothingEnabled == defaultSmoothing,
                "AC6: camera smoothing must restore");
            TestAssert.ApproxEqual(defaultZoom.X, camera.Zoom.X, 0.001f,
                "AC6: camera zoom must restore");
            TestAssert.ApproxEqual(1.0d, ts.CurrentScale, 0.001d,
                "AC6: TimeScaleController must reset to 1.0 after Phase6");
            TestAssert.ApproxEqual(1.0d, Engine.TimeScale, 0.001d,
                "AC6: Engine.TimeScale must read 1.0 via bridge after sequence");
        }
        finally
        {
            director.Dispose();
            camera.QueueFree();
        }
    }

    private static DecisiveStrikeRequest NewRequest(string sourceId = ActorId, string targetId = TargetId) =>
        new(sourceId, targetId, PrecomputedDamage: 350, MoveType: MoveType.Gang);

    private static void DriveDirectorToCompletion(
        CombatAnimationDirector director,
        double stepSeconds = 0.05,
        int maxIterations = 200)
    {
        int i = 0;
        while (director.IsBusy && i++ < maxIterations)
        {
            director.Tick(stepSeconds);
        }
        if (director.IsBusy)
            throw new InvalidOperationException(
                $"DriveDirectorToCompletion: director still busy after {maxIterations} iterations of {stepSeconds}s");
    }

    private (BattleEventBus bus, TimeScaleController ts, CameraRequestBus camera,
        CombatCinematicLock cinematicLock, CombatAnimationDirector director) BuildDirector()
    {
        var bus = new BattleEventBus();
        var ts = new TimeScaleController();
        var camera = new CameraRequestBus();
        var cinematicLock = new CombatCinematicLock();
        var director = new CombatAnimationDirector(bus, ts, camera, cinematicLock);
        return (bus, ts, camera, cinematicLock, director);
    }
}
