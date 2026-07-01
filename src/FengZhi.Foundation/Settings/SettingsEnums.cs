namespace FengZhi.Foundation.Settings;

public enum WindowMode
{
    Fullscreen,
    Windowed,
    BorderlessWindow
}

public enum TextSpeed
{
    Slow = 15,
    Normal = 30,
    Fast = 60,
    Instant = 9999
}

public enum FontScale
{
    Small = 80,
    Normal = 100,
    Large = 120,
    ExtraLarge = 150
}

public enum PauseMenuContext
{
    Exploration,
    Combat
}

public enum PauseMenuItem
{
    Resume,
    Save,
    Load,
    Settings,
    ReturnToTitle,
    QuitGame
}
