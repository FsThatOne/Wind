using FengZhi.Foundation.Combat;

namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// 战斗 UI 演出导演。串行执行命令队列，每帧外部注入 Tick(deltaSeconds)。
/// 当前 sprint 内只接受单条决胜请求（同时只允许一条决胜在跑），保证 phase 顺序不被并发打断。
/// IDisposable 释放任何尚未完成的 sequence 与残留 handle。
/// </summary>
public sealed class CombatAnimationDirector : IDisposable
{
    private readonly AnimationCommandContext _context;
    private IAnimationCommand? _current;
    private bool _disposed;

    public CombatAnimationDirector(
        BattleEventBus eventBus,
        TimeScaleController timeScale,
        CameraRequestBus camera,
        CombatCinematicLock cinematicLock)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(timeScale);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(cinematicLock);
        _context = new AnimationCommandContext(eventBus, timeScale, camera, cinematicLock);
    }

    public bool IsBusy => _current is not null && !_current.IsCompleted;

    public DecisiveStrikePhase CurrentDecisivePhase =>
        _current is DecisiveStrikeSequence seq ? seq.CurrentPhase : DecisiveStrikePhase.Idle;

    public DecisiveStrikeSequence? CurrentSequence => _current as DecisiveStrikeSequence;

    public void RequestDecisiveStrike(DecisiveStrikeRequest request)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        if (IsBusy)
            throw new InvalidOperationException("Director is already running a decisive sequence");
        var seq = new DecisiveStrikeSequence(request);
        _current = seq;
        seq.Start(_context);
        if (seq.IsCompleted) _current = null;
    }

    public void Tick(double deltaSeconds)
    {
        ThrowIfDisposed();
        if (deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (_current is null) return;
        if (_current.Tick(deltaSeconds))
            _current = null;
    }

    public void CancelCurrent()
    {
        ThrowIfDisposed();
        if (_current is null) return;
        _current.Stop(true);
        _current = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_current is { IsCompleted: false })
            _current.Stop(true);
        _current = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
