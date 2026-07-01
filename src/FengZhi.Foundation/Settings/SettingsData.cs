namespace FengZhi.Foundation.Settings;

public sealed class SettingsData
{
    // 音频
    public int MasterVolume { get; set; } = 80;
    public int BgmVolume { get; set; } = 80;
    public int AmbientVolume { get; set; } = 80;
    public int SfxVolume { get; set; } = 80;

    // 显示
    public string Resolution { get; set; } = "";
    public WindowMode WindowMode { get; set; } = WindowMode.Fullscreen;
    public FontScale FontScale { get; set; } = FontScale.Normal;

    // 游戏
    public TextSpeed TextSpeed { get; set; } = TextSpeed.Normal;

    public SettingsData Clone() => new()
    {
        MasterVolume = MasterVolume,
        BgmVolume = BgmVolume,
        AmbientVolume = AmbientVolume,
        SfxVolume = SfxVolume,
        Resolution = Resolution,
        WindowMode = WindowMode,
        FontScale = FontScale,
        TextSpeed = TextSpeed
    };
}
