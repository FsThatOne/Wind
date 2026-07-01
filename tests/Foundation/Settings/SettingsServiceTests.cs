using Xunit;
using FengZhi.Foundation.Settings;

namespace Foundation.Tests.Settings;

public class SettingsServiceTests
{
    private readonly MockPersistence _persistence = new();
    private readonly MockApplier _applier = new();

    private SettingsService CreateService() => new(_persistence, _applier);

    [Fact]
    public void Load_UsesDefaults_WhenPersistenceReturnsNull()
    {
        _persistence.Data = null;
        var svc = CreateService();
        svc.Load();

        Assert.Equal(80, svc.Current.MasterVolume);
        Assert.Equal(TextSpeed.Normal, svc.Current.TextSpeed);
    }

    [Fact]
    public void Load_RestoresPersistedValues()
    {
        _persistence.Data = new SettingsData { MasterVolume = 50, TextSpeed = TextSpeed.Fast };
        var svc = CreateService();
        svc.Load();

        Assert.Equal(50, svc.Current.MasterVolume);
        Assert.Equal(TextSpeed.Fast, svc.Current.TextSpeed);
    }

    [Fact]
    public void Load_ClampsInvalidVolume()
    {
        _persistence.Data = new SettingsData { MasterVolume = 150, BgmVolume = -20 };
        var svc = CreateService();
        svc.Load();

        Assert.Equal(100, svc.Current.MasterVolume);
        Assert.Equal(0, svc.Current.BgmVolume);
    }

    [Fact]
    public void Load_ResetsInvalidEnum()
    {
        _persistence.Data = new SettingsData { WindowMode = (WindowMode)99 };
        var svc = CreateService();
        svc.Load();

        Assert.Equal(WindowMode.Fullscreen, svc.Current.WindowMode);
    }

    [Fact]
    public void SetMasterVolume_ClampsAndApplies()
    {
        var svc = CreateService();
        svc.Load();

        svc.SetMasterVolume(120);
        Assert.Equal(100, svc.Current.MasterVolume);
        Assert.Equal(("master", 100), _applier.LastVolume);
        Assert.NotNull(_persistence.LastSaved);
    }

    [Fact]
    public void SetBgmVolume_FiresEvent()
    {
        var svc = CreateService();
        svc.Load();

        string? track = null;
        int? vol = null;
        svc.VolumeChanged += (t, v) => { track = t; vol = v; };

        svc.SetBgmVolume(60);
        Assert.Equal("bgm", track);
        Assert.Equal(60, vol);
    }

    [Fact]
    public void SetResolution_StartsConfirmationTimer()
    {
        var svc = CreateService();
        svc.Load();

        svc.SetResolution("1920x1080");
        Assert.True(svc.IsAwaitingResolutionConfirm);
        Assert.Equal(10f, svc.ResolutionConfirmTimeRemaining);
    }

    [Fact]
    public void ConfirmResolution_SavesAndStopsTimer()
    {
        var svc = CreateService();
        svc.Load();

        svc.SetResolution("1920x1080");
        svc.ConfirmResolution();

        Assert.False(svc.IsAwaitingResolutionConfirm);
        Assert.Equal("1920x1080", svc.Current.Resolution);
        Assert.NotNull(_persistence.LastSaved);
    }

    [Fact]
    public void Resolution_AutoReverts_AfterTimeout()
    {
        var svc = CreateService();
        svc.Load();
        svc.Current.Resolution = "1280x720";

        svc.SetResolution("3840x2160");
        svc.Tick(5f);
        Assert.True(svc.IsAwaitingResolutionConfirm);

        svc.Tick(6f); // 超过 10s
        Assert.False(svc.IsAwaitingResolutionConfirm);
        Assert.Equal("1280x720", svc.Current.Resolution);
        Assert.True(_applier.ResolutionReverted);
    }

    [Fact]
    public void SetFontScale_AppliesAndFiresEvent()
    {
        var svc = CreateService();
        svc.Load();

        FontScale? received = null;
        svc.FontScaleChanged += s => received = s;

        svc.SetFontScale(FontScale.Large);
        Assert.Equal(FontScale.Large, svc.Current.FontScale);
        Assert.Equal(FontScale.Large, received);
        Assert.Equal(FontScale.Large, _applier.LastFontScale);
    }

    [Fact]
    public void SetTextSpeed_AppliesAndFiresEvent()
    {
        var svc = CreateService();
        svc.Load();

        TextSpeed? received = null;
        svc.TextSpeedChanged += s => received = s;

        svc.SetTextSpeed(TextSpeed.Instant);
        Assert.Equal(TextSpeed.Instant, svc.Current.TextSpeed);
        Assert.Equal(TextSpeed.Instant, received);
    }

    [Fact]
    public void SetWindowMode_AppliesAndSaves()
    {
        var svc = CreateService();
        svc.Load();

        svc.SetWindowMode(WindowMode.Windowed);
        Assert.Equal(WindowMode.Windowed, svc.Current.WindowMode);
        Assert.Equal(WindowMode.Windowed, _applier.LastWindowMode);
    }

    [Fact]
    public void Load_AppliesAllSettings()
    {
        _persistence.Data = new SettingsData
        {
            MasterVolume = 70,
            BgmVolume = 60,
            AmbientVolume = 50,
            SfxVolume = 40,
            Resolution = "1920x1080",
            WindowMode = WindowMode.BorderlessWindow,
            FontScale = FontScale.ExtraLarge,
            TextSpeed = TextSpeed.Slow
        };

        var svc = CreateService();
        svc.Load();

        Assert.Equal(4, _applier.VolumeApplied.Count);
        Assert.Equal("1920x1080", _applier.LastResolution);
        Assert.Equal(WindowMode.BorderlessWindow, _applier.LastWindowMode);
        Assert.Equal(FontScale.ExtraLarge, _applier.LastFontScale);
        Assert.Equal(TextSpeed.Slow, _applier.LastTextSpeed);
    }

    // --- Mocks ---

    private class MockPersistence : ISettingsPersistence
    {
        public SettingsData? Data { get; set; }
        public SettingsData? LastSaved { get; private set; }
        public SettingsData? Load() => Data?.Clone();
        public void Save(SettingsData data) => LastSaved = data.Clone();
    }

    private class MockApplier : ISettingsApplier
    {
        public (string track, int value) LastVolume { get; private set; }
        public List<(string, int)> VolumeApplied { get; } = new();
        public string? LastResolution { get; private set; }
        public WindowMode? LastWindowMode { get; private set; }
        public FontScale? LastFontScale { get; private set; }
        public TextSpeed? LastTextSpeed { get; private set; }
        public bool ResolutionReverted { get; private set; }

        public void ApplyVolume(string track, int value)
        {
            LastVolume = (track, value);
            VolumeApplied.Add((track, value));
        }
        public void ApplyResolution(string resolution) => LastResolution = resolution;
        public void ApplyWindowMode(WindowMode mode) => LastWindowMode = mode;
        public void ApplyFontScale(FontScale scale) => LastFontScale = scale;
        public void ApplyTextSpeed(TextSpeed speed) => LastTextSpeed = speed;
        public void RevertResolution() => ResolutionReverted = true;
    }
}
