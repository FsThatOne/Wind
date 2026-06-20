using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using Xunit;

namespace FengZhi.Tests.Foundation.CombatUi;

public class CombatUiDecisiveAnimationDirectorTest
{
    [Fact]
    public void Director_RequestDecisiveStrike_QueuesAllSevenPhasesInOrder()
    {
        var director = CreateDirector(
            out var bus,
            out _,
            out _,
            out _);
        var phases = new List<int>();
        bus.Subscribe<DecisiveStrikePhaseAdvancedEvent>(evt => phases.Add(evt.PhaseIndex));

        director.RequestDecisiveStrike(MakeRequest());
        TickToCompletion(director);

        Assert.Equal(
            new[]
            {
                (int)DecisiveStrikePhase.Phase1_PrecomputeReceived,
                (int)DecisiveStrikePhase.Phase2_TimeScaleSlowIn,
                (int)DecisiveStrikePhase.Phase3_CameraPushIn,
                (int)DecisiveStrikePhase.Phase4_StyleAnimation,
                (int)DecisiveStrikePhase.Phase5_DamageNumber,
                (int)DecisiveStrikePhase.Phase6_TimeScaleSlowOut,
                (int)DecisiveStrikePhase.Phase7_CameraRestore
            },
            phases);
        Assert.False(director.IsBusy);
    }

    [Fact]
    public void TimeScaleController_DecisiveRequest_UsesPriority50AndReachesPoint2()
    {
        var director = CreateDirector(
            out _,
            out var timeScale,
            out _,
            out _);

        director.RequestDecisiveStrike(MakeRequest());
        // Phase1 -> Phase2 in Start; TimeScale slow-motion is now active.
        Assert.Equal(DecisiveStrikePhase.Phase2_TimeScaleSlowIn, director.CurrentDecisivePhase);
        Assert.Equal(DecisiveTuning.TimeScaleMin, timeScale.CurrentScale, 6);
        Assert.Equal(DecisiveTuning.DecisivePriority, timeScale.CurrentPriority);
        Assert.Equal("decisive_slow_motion", timeScale.CurrentReason);
    }

    [Fact]
    public void TimeScaleController_PauseStackPreemptsDecisiveAndRestoresOnRelease()
    {
        var director = CreateDirector(
            out _,
            out var timeScale,
            out _,
            out _);

        director.RequestDecisiveStrike(MakeRequest());
        Assert.Equal(DecisiveTuning.TimeScaleMin, timeScale.CurrentScale, 6);

        var pauseHandle = timeScale.Request(0.0, DecisiveTuning.PausePriority, "ui_pause");
        Assert.Equal(0.0, timeScale.CurrentScale, 6);
        Assert.Equal(DecisiveTuning.PausePriority, timeScale.CurrentPriority);

        pauseHandle.Dispose();
        Assert.Equal(DecisiveTuning.TimeScaleMin, timeScale.CurrentScale, 6);
        Assert.Equal(DecisiveTuning.DecisivePriority, timeScale.CurrentPriority);
        Assert.Equal("decisive_slow_motion", timeScale.CurrentReason);
    }

    [Fact]
    public void CameraRequestBus_DecisiveRequest_LocksTargetAndDisablesSmoothing()
    {
        var director = CreateDirector(
            out _,
            out _,
            out var camera,
            out _);

        director.RequestDecisiveStrike(MakeRequest());
        director.Tick(DecisiveTuning.SlowInDurationSeconds);
        // Phase2 -> Phase3 transitions camera push-in
        Assert.Equal(DecisiveStrikePhase.Phase3_CameraPushIn, director.CurrentDecisivePhase);
        var active = camera.ActiveRequest;
        Assert.NotNull(active);
        Assert.Equal("bandit", active!.TargetId);
        Assert.Equal(DecisiveTuning.DecisiveCameraZoom, active.Zoom);
        Assert.True(active.DisableSmoothing);
        Assert.Equal(DecisiveTuning.DecisivePriority, active.Priority);
        Assert.Equal("decisive_push_in", active.Reason);
    }

    [Fact]
    public void CinematicLock_AcquireDuringDecisive_BlocksCombatActionWhitelistsPause()
    {
        var director = CreateDirector(
            out _,
            out _,
            out _,
            out var cinematicLock);

        director.RequestDecisiveStrike(MakeRequest());

        Assert.True(cinematicLock.IsLocked);
        Assert.Equal("decisive_strike", cinematicLock.CurrentReason);
        Assert.False(cinematicLock.IsAllowedDuringLock("combat_select_move"));
        Assert.True(cinematicLock.IsAllowedDuringLock("ui_pause"));
        Assert.True(cinematicLock.IsAllowedDuringLock("ui_system_back"));
    }

    [Fact]
    public void DecisiveDamageNumber_AtPhase5_PublishesPhaseAdvancedSoStyleDecisiveCanRender()
    {
        var director = CreateDirector(
            out var bus,
            out _,
            out _,
            out _);
        DecisiveStrikePhaseAdvancedEvent? phase5Event = null;
        bus.Subscribe<DecisiveStrikePhaseAdvancedEvent>(evt =>
        {
            if (evt.PhaseIndex == (int)DecisiveStrikePhase.Phase5_DamageNumber)
                phase5Event = evt;
        });

        director.RequestDecisiveStrike(MakeRequest());
        director.Tick(DecisiveTuning.SlowInDurationSeconds);
        director.Tick(DecisiveTuning.CameraPushInDurationSeconds);
        director.Tick(DecisiveTuning.StyleAnimationDurationSeconds);

        Assert.Equal(DecisiveStrikePhase.Phase5_DamageNumber, director.CurrentDecisivePhase);
        Assert.NotNull(phase5Event);
        Assert.Equal("hero_a", phase5Event!.Value.SourceId);
        Assert.Equal("bandit", phase5Event.Value.TargetId);
        Assert.Equal(MoveType.Gang, phase5Event.Value.MoveType);
        // Style table for Decisive must use ColorHex deep gold and font scale 2.0.
        var style = CombatUiDamageNumberStyle.ForKind(CombatUiDamageNumberStyleKind.Decisive);
        Assert.Equal("#FF8C00", style.ColorHex);
        Assert.Equal(2.0, style.FontScale, 6);
        Assert.True(style.HasBlackOutline);
    }

    [Fact]
    public void DecisiveSequence_PauseDuringSlowMotion_ContinuesFromInterruptedPhase()
    {
        var director = CreateDirector(
            out _,
            out var timeScale,
            out _,
            out _);

        director.RequestDecisiveStrike(MakeRequest());
        // Advance halfway through Phase4 (style animation) so we have measurable elapsed time.
        director.Tick(DecisiveTuning.SlowInDurationSeconds);
        director.Tick(DecisiveTuning.CameraPushInDurationSeconds);
        director.Tick(DecisiveTuning.StyleAnimationDurationSeconds * 0.5);
        Assert.Equal(DecisiveStrikePhase.Phase4_StyleAnimation, director.CurrentDecisivePhase);
        Assert.False(director.CurrentSequence!.IsPausedByExternalTimeScale);

        var pauseHandle = timeScale.Request(0.0, DecisiveTuning.PausePriority, "ui_pause");
        Assert.True(director.CurrentSequence.IsPausedByExternalTimeScale);

        // Tick during pause: phase elapsed must NOT advance.
        director.Tick(DecisiveTuning.StyleAnimationDurationSeconds);
        Assert.Equal(DecisiveStrikePhase.Phase4_StyleAnimation, director.CurrentDecisivePhase);

        pauseHandle.Dispose();
        Assert.False(director.CurrentSequence.IsPausedByExternalTimeScale);

        // Resume — cover the remaining half of Phase4 to advance into Phase5.
        director.Tick(DecisiveTuning.StyleAnimationDurationSeconds * 0.5);
        Assert.Equal(DecisiveStrikePhase.Phase5_DamageNumber, director.CurrentDecisivePhase);
    }

    [Fact]
    public void Director_DisposeReleasesAllHandlesEvenIfSequenceUncompleted()
    {
        var director = CreateDirector(
            out var bus,
            out var timeScale,
            out var camera,
            out var cinematicLock);
        var completedEvents = new List<DecisiveStrikeCompletedEvent>();
        bus.Subscribe<DecisiveStrikeCompletedEvent>(evt => completedEvents.Add(evt));

        director.RequestDecisiveStrike(MakeRequest());
        director.Tick(DecisiveTuning.SlowInDurationSeconds);
        // We are now in Phase3 (camera push-in); both TimeScale and Camera handles are held.
        Assert.Equal(DecisiveStrikePhase.Phase3_CameraPushIn, director.CurrentDecisivePhase);
        Assert.True(timeScale.ActiveRequestCount > 0);
        Assert.NotNull(camera.ActiveRequest);
        Assert.True(cinematicLock.IsLocked);

        director.Dispose();

        Assert.Equal(0, timeScale.ActiveRequestCount);
        Assert.Equal(DecisiveTuning.TimeScaleNormal, timeScale.CurrentScale, 6);
        Assert.Null(camera.ActiveRequest);
        Assert.False(cinematicLock.IsLocked);
        var completed = Assert.Single(completedEvents);
        Assert.True(completed.WasCancelled);
    }

    private static CombatAnimationDirector CreateDirector(
        out BattleEventBus bus,
        out TimeScaleController timeScale,
        out CameraRequestBus camera,
        out CombatCinematicLock cinematicLock)
    {
        bus = new BattleEventBus();
        timeScale = new TimeScaleController();
        camera = new CameraRequestBus();
        cinematicLock = new CombatCinematicLock();
        return new CombatAnimationDirector(bus, timeScale, camera, cinematicLock);
    }

    private static DecisiveStrikeRequest MakeRequest(MoveType moveType = MoveType.Gang) =>
        new("hero_a", "bandit", PrecomputedDamage: 88, moveType);

    private static void TickToCompletion(CombatAnimationDirector director)
    {
        director.Tick(DecisiveTuning.SlowInDurationSeconds);
        director.Tick(DecisiveTuning.CameraPushInDurationSeconds);
        director.Tick(DecisiveTuning.StyleAnimationDurationSeconds);
        director.Tick(DecisiveTuning.DamageNumberHoldDurationSeconds);
        director.Tick(DecisiveTuning.SlowOutDurationSeconds);
        director.Tick(DecisiveTuning.CameraRestoreDurationSeconds);
    }
}
