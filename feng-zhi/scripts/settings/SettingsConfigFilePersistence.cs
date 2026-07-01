using Godot;
using FengZhi.Foundation.Settings;

namespace FengZhi.Settings;

public sealed class SettingsConfigFilePersistence : ISettingsPersistence
{
    private const string FilePath = "user://settings.cfg";

    public SettingsData? Load()
    {
        var cfg = new ConfigFile();
        var err = cfg.Load(FilePath);
        if (err != Error.Ok) return null;

        return new SettingsData
        {
            MasterVolume = (int)cfg.GetValue("audio", "master_volume", 80),
            BgmVolume = (int)cfg.GetValue("audio", "bgm_volume", 80),
            AmbientVolume = (int)cfg.GetValue("audio", "ambient_volume", 80),
            SfxVolume = (int)cfg.GetValue("audio", "sfx_volume", 80),
            Resolution = (string)cfg.GetValue("display", "resolution", ""),
            WindowMode = (WindowMode)(int)cfg.GetValue("display", "window_mode", 0),
            FontScale = (FontScale)(int)cfg.GetValue("display", "font_scale", 100),
            TextSpeed = (TextSpeed)(int)cfg.GetValue("game", "text_speed", 30),
        };
    }

    public void Save(SettingsData data)
    {
        var cfg = new ConfigFile();

        cfg.SetValue("audio", "master_volume", data.MasterVolume);
        cfg.SetValue("audio", "bgm_volume", data.BgmVolume);
        cfg.SetValue("audio", "ambient_volume", data.AmbientVolume);
        cfg.SetValue("audio", "sfx_volume", data.SfxVolume);

        cfg.SetValue("display", "resolution", data.Resolution);
        cfg.SetValue("display", "window_mode", (int)data.WindowMode);
        cfg.SetValue("display", "font_scale", (int)data.FontScale);

        cfg.SetValue("game", "text_speed", (int)data.TextSpeed);

        cfg.Save(FilePath);
    }
}
