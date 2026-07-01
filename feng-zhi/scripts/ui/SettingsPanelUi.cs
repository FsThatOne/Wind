using System;
using Godot;
using FengZhi.Foundation.Settings;

namespace FengZhi.Ui;

public partial class SettingsPanelUi : Control
{
    private HSlider _masterSlider = null!;
    private HSlider _bgmSlider = null!;
    private HSlider _ambientSlider = null!;
    private HSlider _sfxSlider = null!;
    private Label _masterLabel = null!;
    private Label _bgmLabel = null!;
    private Label _ambientLabel = null!;
    private Label _sfxLabel = null!;

    private OptionButton _resolutionDropdown = null!;
    private OptionButton _windowModeDropdown = null!;
    private OptionButton _fontScaleDropdown = null!;
    private OptionButton _textSpeedDropdown = null!;

    private Button _backButton = null!;
    private Label? _countdownLabel;

    private Settings.SettingsManager? _mgr;
    private SettingsService? _svc;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;

        _mgr = GetNodeOrNull<Settings.SettingsManager>("/root/SettingsManager");
        _svc = _mgr?.Settings;

        _masterSlider = GetNode<HSlider>("TabContainer/音频/MasterSlider/Slider");
        _bgmSlider = GetNode<HSlider>("TabContainer/音频/BgmSlider/Slider");
        _ambientSlider = GetNode<HSlider>("TabContainer/音频/AmbientSlider/Slider");
        _sfxSlider = GetNode<HSlider>("TabContainer/音频/SfxSlider/Slider");
        _masterLabel = GetNode<Label>("TabContainer/音频/MasterSlider/Value");
        _bgmLabel = GetNode<Label>("TabContainer/音频/BgmSlider/Value");
        _ambientLabel = GetNode<Label>("TabContainer/音频/AmbientSlider/Value");
        _sfxLabel = GetNode<Label>("TabContainer/音频/SfxSlider/Value");

        _resolutionDropdown = GetNode<OptionButton>("TabContainer/显示/ResolutionRow/Dropdown");
        _windowModeDropdown = GetNode<OptionButton>("TabContainer/显示/WindowModeRow/Dropdown");
        _fontScaleDropdown = GetNode<OptionButton>("TabContainer/显示/FontScaleRow/Dropdown");
        _textSpeedDropdown = GetNode<OptionButton>("TabContainer/游戏/TextSpeedRow/Dropdown");

        _backButton = GetNode<Button>("BackButton");
        _countdownLabel = GetNodeOrNull<Label>("CountdownLabel");

        InitializeValues();
        ConnectSignals();
    }

    public override void _Process(double delta)
    {
        if (_svc != null && _svc.IsAwaitingResolutionConfirm && _countdownLabel != null)
        {
            int remaining = (int)MathF.Ceiling(_svc.ResolutionConfirmTimeRemaining);
            _countdownLabel.Text = $"确认新分辨率？ ({remaining}s)";
            _countdownLabel.Visible = true;
        }
        else if (_countdownLabel != null)
        {
            _countdownLabel.Visible = false;
        }
    }

    private void InitializeValues()
    {
        if (_svc == null) return;
        var data = _svc.Current;

        _masterSlider.Value = data.MasterVolume;
        _bgmSlider.Value = data.BgmVolume;
        _ambientSlider.Value = data.AmbientVolume;
        _sfxSlider.Value = data.SfxVolume;
        UpdateVolumeLabels();

        PopulateResolutions();
        PopulateWindowModes(data.WindowMode);
        PopulateFontScales(data.FontScale);
        PopulateTextSpeeds(data.TextSpeed);
    }

    private void ConnectSignals()
    {
        _masterSlider.ValueChanged += v => { _svc?.SetMasterVolume((int)v); _masterLabel.Text = $"{(int)v}"; };
        _bgmSlider.ValueChanged += v => { _svc?.SetBgmVolume((int)v); _bgmLabel.Text = $"{(int)v}"; };
        _ambientSlider.ValueChanged += v => { _svc?.SetAmbientVolume((int)v); _ambientLabel.Text = $"{(int)v}"; };
        _sfxSlider.ValueChanged += v => { _svc?.SetSfxVolume((int)v); _sfxLabel.Text = $"{(int)v}"; };

        _resolutionDropdown.ItemSelected += OnResolutionSelected;
        _windowModeDropdown.ItemSelected += OnWindowModeSelected;
        _fontScaleDropdown.ItemSelected += OnFontScaleSelected;
        _textSpeedDropdown.ItemSelected += OnTextSpeedSelected;

        _backButton.Pressed += OnBackPressed;
    }

    private void UpdateVolumeLabels()
    {
        if (_svc == null) return;
        _masterLabel.Text = $"{_svc.Current.MasterVolume}";
        _bgmLabel.Text = $"{_svc.Current.BgmVolume}";
        _ambientLabel.Text = $"{_svc.Current.AmbientVolume}";
        _sfxLabel.Text = $"{_svc.Current.SfxVolume}";
    }

    private void PopulateResolutions()
    {
        _resolutionDropdown.Clear();
        string[] resolutions = { "1280x720", "1600x900", "1920x1080", "2560x1440", "3840x2160" };
        int selected = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            _resolutionDropdown.AddItem(resolutions[i]);
            if (_svc?.Current.Resolution == resolutions[i]) selected = i;
        }
        _resolutionDropdown.Selected = selected;
    }

    private void PopulateWindowModes(WindowMode current)
    {
        _windowModeDropdown.Clear();
        _windowModeDropdown.AddItem("全屏");
        _windowModeDropdown.AddItem("窗口化");
        _windowModeDropdown.AddItem("无边框窗口");
        _windowModeDropdown.Selected = (int)current;
    }

    private void PopulateFontScales(FontScale current)
    {
        _fontScaleDropdown.Clear();
        _fontScaleDropdown.AddItem("80%");
        _fontScaleDropdown.AddItem("100%");
        _fontScaleDropdown.AddItem("120%");
        _fontScaleDropdown.AddItem("150%");

        int idx = current switch
        {
            FontScale.Small => 0,
            FontScale.Normal => 1,
            FontScale.Large => 2,
            FontScale.ExtraLarge => 3,
            _ => 1
        };
        _fontScaleDropdown.Selected = idx;
    }

    private void PopulateTextSpeeds(TextSpeed current)
    {
        _textSpeedDropdown.Clear();
        _textSpeedDropdown.AddItem("慢");
        _textSpeedDropdown.AddItem("标准");
        _textSpeedDropdown.AddItem("快");
        _textSpeedDropdown.AddItem("瞬间");

        int idx = current switch
        {
            TextSpeed.Slow => 0,
            TextSpeed.Normal => 1,
            TextSpeed.Fast => 2,
            TextSpeed.Instant => 3,
            _ => 1
        };
        _textSpeedDropdown.Selected = idx;
    }

    private void OnResolutionSelected(long index)
    {
        string res = _resolutionDropdown.GetItemText((int)index);
        _svc?.SetResolution(res);
    }

    private void OnWindowModeSelected(long index)
    {
        _svc?.SetWindowMode((WindowMode)(int)index);
    }

    private void OnFontScaleSelected(long index)
    {
        FontScale scale = index switch
        {
            0 => FontScale.Small,
            1 => FontScale.Normal,
            2 => FontScale.Large,
            3 => FontScale.ExtraLarge,
            _ => FontScale.Normal
        };
        _svc?.SetFontScale(scale);
    }

    private void OnTextSpeedSelected(long index)
    {
        TextSpeed speed = index switch
        {
            0 => TextSpeed.Slow,
            1 => TextSpeed.Normal,
            2 => TextSpeed.Fast,
            3 => TextSpeed.Instant,
            _ => TextSpeed.Normal
        };
        _svc?.SetTextSpeed(speed);
    }

    private void OnBackPressed()
    {
        if (_svc != null && _svc.IsAwaitingResolutionConfirm)
            _svc.ConfirmResolution();

        QueueFree();
    }
}
