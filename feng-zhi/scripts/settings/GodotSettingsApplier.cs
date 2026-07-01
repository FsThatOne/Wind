using Godot;
using FengZhi.Foundation.Audio;
using FengZhi.Foundation.Settings;

namespace FengZhi.Settings;

public sealed class GodotSettingsApplier : ISettingsApplier
{
    private readonly SceneTree _tree;
    private Vector2I _previousResolution;

    public GodotSettingsApplier(SceneTree tree)
    {
        _tree = tree;
        _previousResolution = DisplayServer.WindowGetSize();
    }

    public void ApplyVolume(string track, int value)
    {
        var audioDir = _tree.Root.GetNodeOrNull<Node>("/root/AudioDirector");
        if (audioDir == null) return;

        var volumeTrack = track switch
        {
            "master" => AudioVolumeTrack.Master,
            "bgm" => AudioVolumeTrack.Bgm,
            "ambient" => AudioVolumeTrack.Ambient,
            "sfx" => AudioVolumeTrack.Sfx,
            _ => AudioVolumeTrack.Master
        };

        audioDir.Call("SetVolume", (int)volumeTrack, value / 100f);
    }

    public void ApplyResolution(string resolution)
    {
        _previousResolution = DisplayServer.WindowGetSize();

        var parts = resolution.Split('x');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int w) &&
            int.TryParse(parts[1], out int h))
        {
            DisplayServer.WindowSetSize(new Vector2I(w, h));
            var screenSize = DisplayServer.ScreenGetSize();
            var pos = new Vector2I((screenSize.X - w) / 2, (screenSize.Y - h) / 2);
            DisplayServer.WindowSetPosition(pos);
        }
    }

    public void ApplyWindowMode(WindowMode mode)
    {
        var gdMode = mode switch
        {
            WindowMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
            WindowMode.Windowed => DisplayServer.WindowMode.Windowed,
            WindowMode.BorderlessWindow => DisplayServer.WindowMode.Fullscreen,
            _ => DisplayServer.WindowMode.ExclusiveFullscreen
        };
        DisplayServer.WindowSetMode(gdMode);
    }

    public void ApplyFontScale(FontScale scale)
    {
        int baseSize = 16;
        int newSize = baseSize * (int)scale / 100;
        if (_tree.Root != null)
        {
            var theme = ThemeDB.GetDefaultTheme();
            if (theme != null)
                theme.DefaultFontSize = newSize;
        }
    }

    public void ApplyTextSpeed(TextSpeed speed)
    {
        var gameFlow = _tree.Root.GetNodeOrNull<Node>("/root/GameFlow");
        gameFlow?.Set("TextCharsPerSecond", (int)speed);
    }

    public void RevertResolution()
    {
        DisplayServer.WindowSetSize(_previousResolution);
        var screenSize = DisplayServer.ScreenGetSize();
        var pos = new Vector2I(
            (screenSize.X - _previousResolution.X) / 2,
            (screenSize.Y - _previousResolution.Y) / 2);
        DisplayServer.WindowSetPosition(pos);
    }
}
