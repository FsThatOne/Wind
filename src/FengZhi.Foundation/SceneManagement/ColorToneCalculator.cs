using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Foundation.SceneManagement;

public enum SeasonSensitivity { None, Low, Medium, High }

public enum MindsetZone { Release_Social, Obsession_Social, Release_Hermit, Obsession_Hermit, Demonic }

public sealed class ChapterTone
{
    public float H { get; set; }
    public float S { get; set; }
    public float V { get; set; }

    public ChapterTone() { }

    public ChapterTone(float h, float s, float v)
    {
        H = h;
        S = s;
        V = v;
    }
}

public readonly record struct ToneResult(float H, float S, float V);

public readonly record struct SeasonOffset(float H, float S, float V);

public readonly record struct LightOffset(float S, float V);

public static class ColorToneCalculator
{
    // --- Season Offset Table (per sensitivity) ---
    // Values represent max offset; actual sign depends on season direction.
    // For simplicity, Spring/Summer are positive, Autumn/Winter are negative.

    public static SeasonOffset GetSeasonOffset(Season season, SeasonSensitivity sensitivity)
    {
        var (h, s, v) = GetSensitivityMagnitude(sensitivity);
        float sign = season switch
        {
            Season.Spring => 1f,
            Season.Summer => 1f,
            Season.Autumn => -1f,
            Season.Winter => -1f,
            _ => 0f
        };
        return new SeasonOffset(h * sign, s * sign, v * sign);
    }

    public static (float H, float S, float V) GetSensitivityMagnitude(SeasonSensitivity sensitivity) => sensitivity switch
    {
        SeasonSensitivity.None => (0f, 0f, 0f),
        SeasonSensitivity.Low => (5f / 360f, 0.03f, 0.02f),
        SeasonSensitivity.Medium => (15f / 360f, 0.08f, 0.05f),
        SeasonSensitivity.High => (30f / 360f, 0.15f, 0.10f),
        _ => (0f, 0f, 0f)
    };

    // --- Light Offset Table ---

    public static LightOffset GetLightOffset(LightPhase phase, float nightDarken = 0.20f) => phase switch
    {
        LightPhase.Day => new LightOffset(0f, 0f),
        LightPhase.Dawn => new LightOffset(-0.03f, -0.08f),
        LightPhase.Dusk => new LightOffset(-0.03f, -0.12f),
        LightPhase.Night => new LightOffset(-0.05f, -nightDarken),
        _ => new LightOffset(0f, 0f)
    };

    // --- Three-layer Composite ---

    public static ToneResult CalcFinalTone(
        ChapterTone chapter,
        Season season,
        SeasonSensitivity sensitivity,
        LightPhase lightPhase,
        float nightDarken = 0.20f)
    {
        var so = GetSeasonOffset(season, sensitivity);
        var lo = GetLightOffset(lightPhase, nightDarken);

        float h = chapter.H + so.H; // H is on color wheel, no clamp
        float s = Math.Clamp(chapter.S + so.S + lo.S, 0f, 1f);
        float v = Math.Clamp(chapter.V + so.V + lo.V, 0f, 1f);

        return new ToneResult(h, s, v);
    }

    // --- Finale Tone (5 mindset schemes) ---

    private static readonly Dictionary<MindsetZone, ChapterTone> FinaleTones = new()
    {
        // 白衣行天下 = 江南回暖色
        [MindsetZone.Release_Social] = new ChapterTone(30f / 360f, 0.35f, 0.85f),
        // 孤剑斩世 = 朱红压顶
        [MindsetZone.Obsession_Social] = new ChapterTone(0f / 360f, 0.80f, 0.50f),
        // 大隐于市 = 烟雨灰蓝
        [MindsetZone.Release_Hermit] = new ChapterTone(210f / 360f, 0.20f, 0.65f),
        // 风止剑鸣 = 雪白墨黑
        [MindsetZone.Obsession_Hermit] = new ChapterTone(0f / 360f, 0.05f, 0.90f),
        // 剑覆苍生 = 全黑 + 一点孤血红
        [MindsetZone.Demonic] = new ChapterTone(355f / 360f, 0.90f, 0.10f),
    };

    public static ChapterTone GetFinaleTone(MindsetZone zone)
    {
        return FinaleTones.TryGetValue(zone, out var tone) ? tone : new ChapterTone(0, 0, 0);
    }
}
