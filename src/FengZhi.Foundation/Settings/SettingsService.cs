namespace FengZhi.Foundation.Settings;

public sealed class SettingsService
{
    private readonly ISettingsPersistence _persistence;
    private readonly ISettingsApplier _applier;

    private SettingsData _current;
    private SettingsData? _pendingResolution;
    private float _resolutionConfirmTimer;
    private bool _awaitingResolutionConfirm;

    public float ResolutionConfirmTimeoutSec { get; set; } = 10f;

    public SettingsData Current => _current;
    public bool IsAwaitingResolutionConfirm => _awaitingResolutionConfirm;
    public float ResolutionConfirmTimeRemaining => _resolutionConfirmTimer;

    public event Action<string, int>? VolumeChanged;
    public event Action<FontScale>? FontScaleChanged;
    public event Action<TextSpeed>? TextSpeedChanged;

    public SettingsService(ISettingsPersistence persistence, ISettingsApplier applier)
    {
        _persistence = persistence;
        _applier = applier;
        _current = new SettingsData();
    }

    public void Load()
    {
        var loaded = _persistence.Load();
        if (loaded != null)
        {
            _current = loaded;
            Validate();
        }
        else
        {
            _current = new SettingsData();
        }
        ApplyAll();
    }

    public void SetMasterVolume(int value)
    {
        _current.MasterVolume = Clamp(value);
        _applier.ApplyVolume("master", _current.MasterVolume);
        VolumeChanged?.Invoke("master", _current.MasterVolume);
        Save();
    }

    public void SetBgmVolume(int value)
    {
        _current.BgmVolume = Clamp(value);
        _applier.ApplyVolume("bgm", _current.BgmVolume);
        VolumeChanged?.Invoke("bgm", _current.BgmVolume);
        Save();
    }

    public void SetAmbientVolume(int value)
    {
        _current.AmbientVolume = Clamp(value);
        _applier.ApplyVolume("ambient", _current.AmbientVolume);
        VolumeChanged?.Invoke("ambient", _current.AmbientVolume);
        Save();
    }

    public void SetSfxVolume(int value)
    {
        _current.SfxVolume = Clamp(value);
        _applier.ApplyVolume("sfx", _current.SfxVolume);
        VolumeChanged?.Invoke("sfx", _current.SfxVolume);
        Save();
    }

    public void SetResolution(string resolution)
    {
        _pendingResolution = _current.Clone();
        _current.Resolution = resolution;
        _applier.ApplyResolution(resolution);
        _awaitingResolutionConfirm = true;
        _resolutionConfirmTimer = ResolutionConfirmTimeoutSec;
    }

    public void ConfirmResolution()
    {
        _awaitingResolutionConfirm = false;
        _pendingResolution = null;
        Save();
    }

    public void RevertResolution()
    {
        if (_pendingResolution == null) return;
        _current.Resolution = _pendingResolution.Resolution;
        _applier.RevertResolution();
        _awaitingResolutionConfirm = false;
        _pendingResolution = null;
    }

    public void SetWindowMode(WindowMode mode)
    {
        _current.WindowMode = mode;
        _applier.ApplyWindowMode(mode);
        Save();
    }

    public void SetFontScale(FontScale scale)
    {
        _current.FontScale = scale;
        _applier.ApplyFontScale(scale);
        FontScaleChanged?.Invoke(scale);
        Save();
    }

    public void SetTextSpeed(TextSpeed speed)
    {
        _current.TextSpeed = speed;
        _applier.ApplyTextSpeed(speed);
        TextSpeedChanged?.Invoke(speed);
        Save();
    }

    public void Tick(float deltaSeconds)
    {
        if (!_awaitingResolutionConfirm) return;

        _resolutionConfirmTimer -= deltaSeconds;
        if (_resolutionConfirmTimer <= 0f)
        {
            RevertResolution();
        }
    }

    private void Validate()
    {
        _current.MasterVolume = Clamp(_current.MasterVolume);
        _current.BgmVolume = Clamp(_current.BgmVolume);
        _current.AmbientVolume = Clamp(_current.AmbientVolume);
        _current.SfxVolume = Clamp(_current.SfxVolume);

        if (!Enum.IsDefined(_current.WindowMode))
            _current.WindowMode = WindowMode.Fullscreen;
        if (!Enum.IsDefined(_current.FontScale))
            _current.FontScale = FontScale.Normal;
        if (!Enum.IsDefined(_current.TextSpeed))
            _current.TextSpeed = TextSpeed.Normal;
    }

    private void ApplyAll()
    {
        _applier.ApplyVolume("master", _current.MasterVolume);
        _applier.ApplyVolume("bgm", _current.BgmVolume);
        _applier.ApplyVolume("ambient", _current.AmbientVolume);
        _applier.ApplyVolume("sfx", _current.SfxVolume);
        if (!string.IsNullOrEmpty(_current.Resolution))
            _applier.ApplyResolution(_current.Resolution);
        _applier.ApplyWindowMode(_current.WindowMode);
        _applier.ApplyFontScale(_current.FontScale);
        _applier.ApplyTextSpeed(_current.TextSpeed);
    }

    private void Save() => _persistence.Save(_current);

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);
}
