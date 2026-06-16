using FengZhi.Foundation.StateMachine;

namespace FengZhi.Foundation.Exploration;

/// <summary>
/// Bridges shared game-state locks into exploration cue pause and resume behavior.
/// </summary>
public sealed class ExplorationLockGuard
{
    private readonly ProximityDetector _detector;
    private int _blockingLockDepth;

    public ExplorationLockGuard(ProximityDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>True when at least one Partial-or-stronger lock is active.</summary>
    public bool IsLocked => _blockingLockDepth > 0;

    /// <summary>Applies a newly acquired shared game-state lock.</summary>
    public IReadOnlyList<InsightCueHiddenEvent> OnGameStateLockAcquired(LockMode mode)
    {
        if (mode < LockMode.Partial)
        {
            return Array.Empty<InsightCueHiddenEvent>();
        }

        _blockingLockDepth++;
        return _detector.PauseDetection();
    }

    /// <summary>Releases a shared game-state lock and resumes detection after all blocking locks clear.</summary>
    public void OnGameStateLockReleased(LockMode mode)
    {
        if (mode < LockMode.Partial || _blockingLockDepth == 0)
        {
            return;
        }

        _blockingLockDepth--;
        if (_blockingLockDepth == 0)
        {
            _detector.ResumeDetection();
        }
    }
}
