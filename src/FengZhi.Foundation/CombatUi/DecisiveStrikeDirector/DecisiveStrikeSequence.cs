using FengZhi.Foundation.Combat;

namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// 一击决胜 7 阶段演出命令。状态机按 GDD §Detailed Design 推进；
/// 暂停（更高优先级 TimeScale 压栈到 0）期间不累计 phase 计时。
/// 反向 LIFO 释放：Phase6 释放 TimeScale → Phase7 释放 Camera → Completed 释放 Lock；
/// Stop(cancelled) 走相同顺序。
/// </summary>
public sealed class DecisiveStrikeSequence : IAnimationCommand
{
    private readonly DecisiveStrikeRequest _request;
    private AnimationCommandContext _context = null!;

    private DecisiveStrikePhase _phase = DecisiveStrikePhase.Idle;
    private double _phaseElapsed;
    private bool _paused;
    private bool _started;
    private Action<double>? _onScaleChanged;

    private IDisposable? _lockHandle;
    private IDisposable? _timeScaleHandle;
    private IDisposable? _cameraHandle;

    public DecisiveStrikeSequence(DecisiveStrikeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _request = request;
    }

    public bool IsCompleted => _phase == DecisiveStrikePhase.Completed;

    public DecisiveStrikePhase CurrentPhase => _phase;

    public bool IsPausedByExternalTimeScale => _paused;

    public DecisiveStrikeRequest Request => _request;

    public void Start(AnimationCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_started) return;
        _started = true;
        _context = context;

        _onScaleChanged = OnExternalScaleChanged;
        _context.TimeScale.ScaleChanged += _onScaleChanged;

        _lockHandle = _context.CinematicLock.Acquire("decisive_strike");
        EnterPhase(DecisiveStrikePhase.Phase1_PrecomputeReceived);
        _context.EventBus.Publish(new DecisiveStrikeStartedEvent(
            _request.SourceId, _request.TargetId, _request.PrecomputedDamage, _request.MoveType));
        PublishPhaseAdvanced(DecisiveStrikePhase.Phase1_PrecomputeReceived);
        AdvanceFromInstantPhase();
    }

    public bool Tick(double deltaSeconds)
    {
        if (deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (!_started) throw new InvalidOperationException("Sequence not started");
        if (IsCompleted) return true;

        if (!_paused) _phaseElapsed += deltaSeconds;

        var duration = DurationFor(_phase);
        while (!IsCompleted && !_paused && _phaseElapsed >= duration && duration > 0)
        {
            _phaseElapsed -= duration;
            AdvanceToNextPhase();
            duration = DurationFor(_phase);
        }
        return IsCompleted;
    }

    public void Stop(bool cancelled)
    {
        if (!_started || IsCompleted)
        {
            DetachScaleSubscription();
            return;
        }

        DetachScaleSubscription();
        _timeScaleHandle?.Dispose();
        _timeScaleHandle = null;
        _cameraHandle?.Dispose();
        _cameraHandle = null;
        _lockHandle?.Dispose();
        _lockHandle = null;
        _phase = DecisiveStrikePhase.Completed;
        _context.EventBus.Publish(new DecisiveStrikeCompletedEvent(
            _request.SourceId, _request.TargetId, cancelled));
    }

    private void AdvanceFromInstantPhase()
    {
        // Phase1 持续 0s — 立刻推进。
        if (_phase == DecisiveStrikePhase.Phase1_PrecomputeReceived)
            AdvanceToNextPhase();
    }

    private void AdvanceToNextPhase()
    {
        switch (_phase)
        {
            case DecisiveStrikePhase.Phase1_PrecomputeReceived:
                EnterPhase(DecisiveStrikePhase.Phase2_TimeScaleSlowIn);
                _timeScaleHandle = _context.TimeScale.Request(
                    DecisiveTuning.TimeScaleMin,
                    DecisiveTuning.DecisivePriority,
                    "decisive_slow_motion");
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase2_TimeScaleSlowIn:
                EnterPhase(DecisiveStrikePhase.Phase3_CameraPushIn);
                _cameraHandle = _context.Camera.Request(
                    _request.TargetId,
                    DecisiveTuning.DecisiveCameraZoom,
                    disableSmoothing: true,
                    DecisiveTuning.DecisivePriority,
                    "decisive_push_in");
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase3_CameraPushIn:
                EnterPhase(DecisiveStrikePhase.Phase4_StyleAnimation);
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase4_StyleAnimation:
                EnterPhase(DecisiveStrikePhase.Phase5_DamageNumber);
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase5_DamageNumber:
                EnterPhase(DecisiveStrikePhase.Phase6_TimeScaleSlowOut);
                _timeScaleHandle?.Dispose();
                _timeScaleHandle = null;
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase6_TimeScaleSlowOut:
                EnterPhase(DecisiveStrikePhase.Phase7_CameraRestore);
                _cameraHandle?.Dispose();
                _cameraHandle = null;
                PublishPhaseAdvanced(_phase);
                break;
            case DecisiveStrikePhase.Phase7_CameraRestore:
                EnterPhase(DecisiveStrikePhase.Completed);
                _lockHandle?.Dispose();
                _lockHandle = null;
                DetachScaleSubscription();
                _context.EventBus.Publish(new DecisiveStrikeCompletedEvent(
                    _request.SourceId, _request.TargetId, false));
                break;
        }
    }

    private void EnterPhase(DecisiveStrikePhase phase)
    {
        _phase = phase;
        _phaseElapsed = 0;
    }

    private void OnExternalScaleChanged(double scale)
    {
        if (IsCompleted) return;
        if (_phase == DecisiveStrikePhase.Idle) return;
        var paused = scale <= DecisiveTuning.PauseDetectionThreshold;
        _paused = paused;
    }

    private void DetachScaleSubscription()
    {
        if (_onScaleChanged is null) return;
        _context.TimeScale.ScaleChanged -= _onScaleChanged;
        _onScaleChanged = null;
    }

    private void PublishPhaseAdvanced(DecisiveStrikePhase phase)
    {
        _context.EventBus.Publish(new DecisiveStrikePhaseAdvancedEvent(
            _request.SourceId,
            _request.TargetId,
            (int)phase,
            phase.ToString(),
            _request.MoveType));
    }

    private static double DurationFor(DecisiveStrikePhase phase) => phase switch
    {
        DecisiveStrikePhase.Phase1_PrecomputeReceived => 0,
        DecisiveStrikePhase.Phase2_TimeScaleSlowIn => DecisiveTuning.SlowInDurationSeconds,
        DecisiveStrikePhase.Phase3_CameraPushIn => DecisiveTuning.CameraPushInDurationSeconds,
        DecisiveStrikePhase.Phase4_StyleAnimation => DecisiveTuning.StyleAnimationDurationSeconds,
        DecisiveStrikePhase.Phase5_DamageNumber => DecisiveTuning.DamageNumberHoldDurationSeconds,
        DecisiveStrikePhase.Phase6_TimeScaleSlowOut => DecisiveTuning.SlowOutDurationSeconds,
        DecisiveStrikePhase.Phase7_CameraRestore => DecisiveTuning.CameraRestoreDurationSeconds,
        _ => 0
    };
}
