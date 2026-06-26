namespace FengZhi.Foundation.Audio;

/// <summary>
/// AudioServer 总线名称常量。ADR-0009 总线架构。
/// Master → BGM(BGM_Main + BGM_Crossfade) → Ambient(Terrain/Weather/TimeOfDay) → SFX(Pool×8)
/// </summary>
public static class AudioBusLayout
{
    public const string Master = "Master";

    public const string Bgm = "BGM";
    public const string BgmMain = "BGM_Main";
    public const string BgmCrossfade = "BGM_Crossfade";

    public const string Ambient = "Ambient";
    public const string AmbientTerrain = "Ambient_Terrain";
    public const string AmbientWeather = "Ambient_Weather";
    public const string AmbientTimeOfDay = "Ambient_TimeOfDay";

    public const string Sfx = "SFX";
    public const string SfxPool = "SFX_Pool";

    public const int SfxPoolSize = 8;
    public const int BgmOverrideStackMaxDepth = 3;
}
