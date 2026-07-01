using Xunit;
using FengZhi.Foundation.Settings;

namespace Foundation.Tests.Settings;

public class PauseMenuServiceTests
{
    [Fact]
    public void TryPause_Succeeds_WhenNotPausedAndNoCinematicLock()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        Assert.True(svc.TryPause());
        Assert.True(svc.IsPaused);
    }

    [Fact]
    public void TryPause_Fails_WhenAlreadyPaused()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        svc.TryPause();
        Assert.False(svc.TryPause());
    }

    [Fact]
    public void TryPause_Fails_DuringCinematicLock()
    {
        var svc = new PauseMenuService(new MockCinematicLock(true));
        Assert.False(svc.TryPause());
        Assert.False(svc.IsPaused);
    }

    [Fact]
    public void Resume_UnpausesAndFiresEvent()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        bool resumed = false;
        svc.Resumed += () => resumed = true;

        svc.TryPause();
        svc.Resume();

        Assert.False(svc.IsPaused);
        Assert.True(resumed);
    }

    [Fact]
    public void GetMenuItems_Exploration_Has6Items_IncludingSave()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        var items = svc.GetMenuItems(PauseMenuContext.Exploration);

        Assert.Equal(6, items.Count);
        Assert.Contains(PauseMenuItem.Save, items);
        Assert.Contains(PauseMenuItem.Load, items);
    }

    [Fact]
    public void GetMenuItems_Combat_Has5Items_NoSave()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        var items = svc.GetMenuItems(PauseMenuContext.Combat);

        Assert.Equal(5, items.Count);
        Assert.DoesNotContain(PauseMenuItem.Save, items);
        Assert.Contains(PauseMenuItem.Load, items);
    }

    [Fact]
    public void Paused_Event_Fires()
    {
        var svc = new PauseMenuService(new MockCinematicLock(false));
        bool fired = false;
        svc.Paused += () => fired = true;

        svc.TryPause();
        Assert.True(fired);
    }

    private class MockCinematicLock : ICinematicLockQuery
    {
        private readonly bool _locked;
        public MockCinematicLock(bool locked) => _locked = locked;
        public bool IsInCinematicLock() => _locked;
    }
}
