namespace FengZhi.Foundation.Settings;

public sealed class PauseMenuService
{
    private readonly ICinematicLockQuery _cinematicLock;

    private bool _isPaused;

    public bool IsPaused => _isPaused;

    public event Action? Paused;
    public event Action? Resumed;

    public PauseMenuService(ICinematicLockQuery cinematicLock)
    {
        _cinematicLock = cinematicLock;
    }

    public bool TryPause()
    {
        if (_isPaused) return false;
        if (_cinematicLock.IsInCinematicLock()) return false;

        _isPaused = true;
        Paused?.Invoke();
        return true;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Resumed?.Invoke();
    }

    public IReadOnlyList<PauseMenuItem> GetMenuItems(PauseMenuContext context)
    {
        if (context == PauseMenuContext.Combat)
        {
            return new[]
            {
                PauseMenuItem.Resume,
                PauseMenuItem.Load,
                PauseMenuItem.Settings,
                PauseMenuItem.ReturnToTitle,
                PauseMenuItem.QuitGame
            };
        }

        return new[]
        {
            PauseMenuItem.Resume,
            PauseMenuItem.Save,
            PauseMenuItem.Load,
            PauseMenuItem.Settings,
            PauseMenuItem.ReturnToTitle,
            PauseMenuItem.QuitGame
        };
    }
}
