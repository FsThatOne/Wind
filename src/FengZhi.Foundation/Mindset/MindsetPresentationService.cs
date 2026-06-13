namespace FengZhi.Foundation.Mindset;

public sealed record MindsetVisualParams(
    string Tone,
    float Warmth,
    float Saturation,
    float InkDensity);

public static class MindsetPresentationService
{
    public static string GetResolveDescription(MindsetState state)
    {
        return state.CurrentResolveZone switch
        {
            AxisPolarity.Negative => "旧恨如铁，尚未松手。",
            AxisPolarity.Positive => "心头霜雪渐化，往事已有归处。",
            _ => "心意未定，旧事仍在风中。"
        };
    }

    public static string GetWorldlyDescription(MindsetState state)
    {
        return state.CurrentWorldlyZone switch
        {
            AxisPolarity.Negative => "剑仍向人间喧嚣处去。",
            AxisPolarity.Positive => "脚步渐远，山水有归意。",
            _ => "去留之间，尚未择路。"
        };
    }

    public static MindsetVisualParams GetVisualParams(MindsetZone zone)
    {
        return zone switch
        {
            MindsetZone.GuJianRuShi => new MindsetVisualParams("cold-saturated", -0.35f, 0.25f, 0.8f),
            MindsetZone.ZhiNianWeiDing => new MindsetVisualParams("cold-balanced", -0.3f, 0.0f, 0.75f),
            MindsetZone.FengZhiChenYan => new MindsetVisualParams("cold-muted", -0.3f, -0.25f, 0.7f),
            MindsetZone.RuShiWeiDing => new MindsetVisualParams("neutral-saturated", 0.0f, 0.25f, 0.55f),
            MindsetZone.ZhongYong => new MindsetVisualParams("neutral", 0.0f, 0.0f, 0.5f),
            MindsetZone.ChuShiWeiDing => new MindsetVisualParams("neutral-muted", 0.0f, -0.25f, 0.45f),
            MindsetZone.BaiYiRuShi => new MindsetVisualParams("warm-saturated", 0.35f, 0.25f, 0.35f),
            MindsetZone.ShiHuaiWeiDing => new MindsetVisualParams("warm-balanced", 0.3f, 0.0f, 0.3f),
            MindsetZone.DaYinYuShi => new MindsetVisualParams("warm-muted", 0.35f, -0.25f, 0.25f),
            _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, null)
        };
    }
}
