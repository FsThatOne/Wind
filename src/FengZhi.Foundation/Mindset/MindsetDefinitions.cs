namespace FengZhi.Foundation.Mindset;

public enum MindsetAxis
{
    Resolve,
    Worldly,
    Morality
}

public enum AxisPolarity
{
    Negative,
    Neutral,
    Positive
}

public enum MindsetZone
{
    GuJianRuShi,
    ZhiNianWeiDing,
    FengZhiChenYan,
    RuShiWeiDing,
    ZhongYong,
    ChuShiWeiDing,
    BaiYiRuShi,
    ShiHuaiWeiDing,
    DaYinYuShi
}

public enum MoralityTier
{
    ExtremeEvil,
    Evil,
    Neutral,
    Good,
    ExtremeGood
}

public enum BaseEnding
{
    GuJianZhanShi,
    FengZhiJianMing,
    BaiYiXingTian,
    DaYinYuShi,
    Undecided,
    MoDao
}

public sealed record MindsetState(
    int Resolve = MindsetConstants.InitialResolve,
    int Worldly = MindsetConstants.InitialWorldly,
    int Morality = MindsetConstants.InitialMorality,
    int Reputation = MindsetConstants.InitialReputation,
    AxisPolarity? CurrentResolveZone = null,
    AxisPolarity? CurrentWorldlyZone = null)
{
    public static MindsetState CreateDefault()
    {
        return FromValues(
            MindsetConstants.InitialResolve,
            MindsetConstants.InitialWorldly,
            MindsetConstants.InitialMorality,
            MindsetConstants.InitialReputation);
    }

    public static MindsetState FromValues(
        int resolve,
        int worldly,
        int morality,
        int reputation,
        AxisPolarity? currentResolveZone = null,
        AxisPolarity? currentWorldlyZone = null)
    {
        var clampedResolve = MindsetMath.ClampAxis(resolve);
        var clampedWorldly = MindsetMath.ClampAxis(worldly);

        return new MindsetState(
            clampedResolve,
            clampedWorldly,
            MindsetMath.ClampAxis(morality),
            MindsetMath.ClampAxis(reputation),
            currentResolveZone ?? MindsetMath.GetInitialPolarity(clampedResolve),
            currentWorldlyZone ?? MindsetMath.GetInitialPolarity(clampedWorldly));
    }
}

public sealed record EndingResult(BaseEnding BaseEnding, MoralityTier MoralityTier);

public static class MindsetConstants
{
    public const int MinAxisValue = -50;
    public const int MaxAxisValue = 50;
    public const int InitialResolve = -5;
    public const int InitialWorldly = 0;
    public const int InitialMorality = 0;
    public const int InitialReputation = 0;
    public const int ZoneThreshold = 15;
    public const int Hysteresis = 2;
    public const float DefaultReputationCatchUpRate = 0.2f;
}

public static class MindsetMath
{
    public static int ClampAxis(int value)
    {
        return Math.Clamp(value, MindsetConstants.MinAxisValue, MindsetConstants.MaxAxisValue);
    }

    public static AxisPolarity GetInitialPolarity(int value)
    {
        if (value >= MindsetConstants.ZoneThreshold)
            return AxisPolarity.Positive;
        if (value <= -MindsetConstants.ZoneThreshold)
            return AxisPolarity.Negative;
        return AxisPolarity.Neutral;
    }

    public static AxisPolarity ApplyHysteresis(int value, AxisPolarity current)
    {
        return current switch
        {
            AxisPolarity.Positive when value <= MindsetConstants.ZoneThreshold - MindsetConstants.Hysteresis => AxisPolarity.Neutral,
            AxisPolarity.Negative when value >= -MindsetConstants.ZoneThreshold + MindsetConstants.Hysteresis => AxisPolarity.Neutral,
            AxisPolarity.Neutral when value >= MindsetConstants.ZoneThreshold => AxisPolarity.Positive,
            AxisPolarity.Neutral when value <= -MindsetConstants.ZoneThreshold => AxisPolarity.Negative,
            _ => current
        };
    }

    public static MindsetZone CombineZone(AxisPolarity resolve, AxisPolarity worldly)
    {
        return (resolve, worldly) switch
        {
            (AxisPolarity.Negative, AxisPolarity.Negative) => MindsetZone.GuJianRuShi,
            (AxisPolarity.Negative, AxisPolarity.Neutral) => MindsetZone.ZhiNianWeiDing,
            (AxisPolarity.Negative, AxisPolarity.Positive) => MindsetZone.FengZhiChenYan,
            (AxisPolarity.Neutral, AxisPolarity.Negative) => MindsetZone.RuShiWeiDing,
            (AxisPolarity.Neutral, AxisPolarity.Neutral) => MindsetZone.ZhongYong,
            (AxisPolarity.Neutral, AxisPolarity.Positive) => MindsetZone.ChuShiWeiDing,
            (AxisPolarity.Positive, AxisPolarity.Negative) => MindsetZone.BaiYiRuShi,
            (AxisPolarity.Positive, AxisPolarity.Neutral) => MindsetZone.ShiHuaiWeiDing,
            (AxisPolarity.Positive, AxisPolarity.Positive) => MindsetZone.DaYinYuShi,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static MoralityTier GetMoralityTier(int morality)
    {
        if (morality <= -30)
            return MoralityTier.ExtremeEvil;
        if (morality <= -15)
            return MoralityTier.Evil;
        if (morality <= 14)
            return MoralityTier.Neutral;
        if (morality <= 29)
            return MoralityTier.Good;
        return MoralityTier.ExtremeGood;
    }
}
