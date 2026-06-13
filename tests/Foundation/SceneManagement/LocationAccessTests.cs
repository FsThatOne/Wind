using Xunit;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.SceneManagement;

namespace FengZhi.Tests.Foundation.SceneManagement;

public class LocationAccessTests
{
    private readonly EventBus _eventBus = new();

    private LocationAccessManager CreateManager()
    {
        var mgr = new LocationAccessManager(_eventBus);
        mgr.RegisterLocation("liujia_town");
        mgr.RegisterLocation("luoyang");
        mgr.RegisterLocation("desert_city");
        return mgr;
    }

    // ─── AC1: LocationState 枚举 ────────────────────────────

    [Fact]
    public void LocationState_HasThreeValues()
    {
        Assert.Equal(3, Enum.GetValues<LocationState>().Length);
        Assert.Equal(0, (int)LocationState.Locked);
        Assert.Equal(1, (int)LocationState.Known);
        Assert.Equal(2, (int)LocationState.Unlocked);
    }

    // ─── AC2: RevealLocation (Locked→Known) ─────────────────

    [Fact]
    public void RevealLocation_FromLocked_BecomesKnown()
    {
        var mgr = CreateManager();
        bool result = mgr.RevealLocation("liujia_town");
        Assert.True(result);
        Assert.Equal(LocationState.Known, mgr.GetAccessState("liujia_town"));
    }

    [Fact]
    public void RevealLocation_PublishesEvent()
    {
        var mgr = CreateManager();
        LocationRevealedEvent? received = null;
        _eventBus.Subscribe<LocationRevealedEvent>(e => received = e);

        mgr.RevealLocation("liujia_town");
        Assert.NotNull(received);
        Assert.Equal("liujia_town", received!.LocationId);
    }

    [Fact]
    public void RevealLocation_AlreadyKnown_ReturnsFalse()
    {
        var mgr = CreateManager();
        mgr.RevealLocation("liujia_town");
        bool result = mgr.RevealLocation("liujia_town"); // 已 Known
        Assert.False(result);
    }

    // ─── AC3: UnlockLocation (Known→Unlocked) ───────────────

    [Fact]
    public void UnlockLocation_FromKnown_BecomesUnlocked()
    {
        var mgr = CreateManager();
        mgr.RevealLocation("luoyang"); // Known
        bool result = mgr.UnlockLocation("luoyang");
        Assert.True(result);
        Assert.Equal(LocationState.Unlocked, mgr.GetAccessState("luoyang"));
    }

    [Fact]
    public void UnlockLocation_PublishesEvent()
    {
        var mgr = CreateManager();
        LocationUnlockedEvent? received = null;
        _eventBus.Subscribe<LocationUnlockedEvent>(e => received = e);

        mgr.UnlockLocation("luoyang");
        Assert.NotNull(received);
        Assert.Equal("luoyang", received!.LocationId);
    }

    // ─── AC4: 直接解锁 (Locked→Unlocked) ───────────────────

    [Fact]
    public void UnlockLocation_FromLocked_DirectlyUnlocked()
    {
        var mgr = CreateManager();
        bool result = mgr.UnlockLocation("desert_city");
        Assert.True(result);
        Assert.Equal(LocationState.Unlocked, mgr.GetAccessState("desert_city"));
    }

    // ─── AC5: 逆向变化被拒绝 ────────────────────────────────

    [Fact]
    public void UnlockLocation_AlreadyUnlocked_ReturnsFalse()
    {
        var mgr = CreateManager();
        mgr.UnlockLocation("liujia_town");
        bool result = mgr.UnlockLocation("liujia_town");
        Assert.False(result);
    }

    [Fact]
    public void RevealLocation_WhenUnlocked_ReturnsFalse()
    {
        var mgr = CreateManager();
        mgr.UnlockLocation("liujia_town"); // Unlocked
        bool result = mgr.RevealLocation("liujia_town"); // 不能降级
        Assert.False(result);
        Assert.Equal(LocationState.Unlocked, mgr.GetAccessState("liujia_town"));
    }

    // ─── AC6: 查询接口 ─────────────────────────────────────

    [Fact]
    public void GetAccessState_UnregisteredLocation_ReturnsLocked()
    {
        var mgr = CreateManager();
        Assert.Equal(LocationState.Locked, mgr.GetAccessState("unknown_place"));
    }

    [Fact]
    public void GetUnlockedLocations_ReturnsCorrect()
    {
        var mgr = CreateManager();
        mgr.UnlockLocation("liujia_town");
        mgr.UnlockLocation("luoyang");

        var unlocked = mgr.GetUnlockedLocations();
        Assert.Equal(2, unlocked.Count);
        Assert.Contains("liujia_town", unlocked);
        Assert.Contains("luoyang", unlocked);
    }

    [Fact]
    public void GetKnownLocations_IncludesUnlocked()
    {
        var mgr = CreateManager();
        mgr.RevealLocation("liujia_town"); // Known
        mgr.UnlockLocation("luoyang"); // Unlocked

        var known = mgr.GetKnownLocations();
        Assert.Equal(2, known.Count);
    }

    // ─── 未注册地点操作自动注册 ─────────────────────────────

    [Fact]
    public void RevealLocation_Unregistered_AutoRegisters()
    {
        var mgr = new LocationAccessManager(_eventBus);
        mgr.RevealLocation("new_place");
        Assert.Equal(LocationState.Known, mgr.GetAccessState("new_place"));
    }
}
