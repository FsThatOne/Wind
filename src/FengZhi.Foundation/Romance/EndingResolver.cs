using FengZhi.Foundation.Mindset;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Relationship branch used by the final romance ending variant.
/// </summary>
public enum RomanceEndingBondVariant
{
    Demonic,
    Solo,
    Companion,
    Farewell
}

/// <summary>
/// Narrator tone derived from morality; it must not change the ending branch identity.
/// </summary>
public enum RomanceEndingNarratorTone
{
    Dark,
    Neutral,
    Light
}

/// <summary>
/// Deterministic ending selection result consumed by narrative playback.
/// </summary>
public sealed record RomanceEndingVariant(
    string ScriptKey,
    BaseEnding BaseEnding,
    RomanceEndingBondVariant BondVariant,
    RomanceEndingNarratorTone NarratorTone,
    MindsetZone? MindsetZone,
    string? BondedHeroine
);

/// <summary>
/// Resolves the final romance ending variant from mindset zone, morality, and global bond state.
/// </summary>
public sealed class EndingResolver
{
    public const int DemonicMoralityThreshold = -30;
    public const string DemonicEndingKey = "ENDING_6_DEMONIC";
    public const string NoBondedHeroine = "None";

    private readonly IReadOnlyDictionary<string, IReadOnlySet<MindsetZone>> _compatibleZonesByHeroine;

    public EndingResolver(IReadOnlyDictionary<string, IReadOnlySet<MindsetZone>>? compatibleZonesByHeroine = null)
    {
        _compatibleZonesByHeroine = compatibleZonesByHeroine
            ?? new Dictionary<string, IReadOnlySet<MindsetZone>>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Selects the stable ending variant. Demonic override is always evaluated before bond logic.
    /// </summary>
    public RomanceEndingVariant Resolve(MindsetZone mindsetZone, int moralityTier, string? bondedHeroine)
    {
        var narratorTone = ResolveNarratorTone(moralityTier);
        if (moralityTier <= DemonicMoralityThreshold)
        {
            return new RomanceEndingVariant(
                DemonicEndingKey,
                BaseEnding.MoDao,
                RomanceEndingBondVariant.Demonic,
                narratorTone,
                null,
                NormalizeBondedHeroine(bondedHeroine));
        }

        var baseEnding = ToBaseEnding(mindsetZone);
        var normalizedHeroine = NormalizeBondedHeroine(bondedHeroine);
        if (normalizedHeroine == null)
        {
            return new RomanceEndingVariant(
                BuildScriptKey(baseEnding, RomanceEndingBondVariant.Solo),
                baseEnding,
                RomanceEndingBondVariant.Solo,
                narratorTone,
                mindsetZone,
                null);
        }

        var bondVariant = IsZoneCompatible(normalizedHeroine, mindsetZone)
            ? RomanceEndingBondVariant.Companion
            : RomanceEndingBondVariant.Farewell;

        return new RomanceEndingVariant(
            BuildScriptKey(baseEnding, bondVariant),
            baseEnding,
            bondVariant,
            narratorTone,
            mindsetZone,
            normalizedHeroine);
    }

    /// <summary>
    /// Returns whether a heroine has explicit compatibility with the final mindset zone.
    /// Missing compatibility config falls back to false so companion variants are never silent.
    /// </summary>
    public bool IsZoneCompatible(string heroineId, MindsetZone mindsetZone)
    {
        return _compatibleZonesByHeroine.TryGetValue(heroineId, out var zones)
            && zones.Contains(mindsetZone);
    }

    /// <summary>
    /// Collapses the 3x3 mindset grid into the five core ending script identities.
    /// </summary>
    public static BaseEnding ToBaseEnding(MindsetZone mindsetZone)
    {
        return mindsetZone switch
        {
            MindsetZone.GuJianRuShi => BaseEnding.GuJianZhanShi,
            MindsetZone.ZhiNianWeiDing => BaseEnding.GuJianZhanShi,
            MindsetZone.RuShiWeiDing => BaseEnding.GuJianZhanShi,
            MindsetZone.FengZhiChenYan => BaseEnding.FengZhiJianMing,
            MindsetZone.ChuShiWeiDing => BaseEnding.FengZhiJianMing,
            MindsetZone.BaiYiRuShi => BaseEnding.BaiYiXingTian,
            MindsetZone.ShiHuaiWeiDing => BaseEnding.DaYinYuShi,
            MindsetZone.DaYinYuShi => BaseEnding.DaYinYuShi,
            MindsetZone.ZhongYong => BaseEnding.Undecided,
            _ => throw new ArgumentOutOfRangeException(nameof(mindsetZone), mindsetZone, null)
        };
    }

    private static RomanceEndingNarratorTone ResolveNarratorTone(int moralityTier)
    {
        if (moralityTier <= -15)
            return RomanceEndingNarratorTone.Dark;
        if (moralityTier >= 15)
            return RomanceEndingNarratorTone.Light;
        return RomanceEndingNarratorTone.Neutral;
    }

    private static string? NormalizeBondedHeroine(string? bondedHeroine)
    {
        if (string.IsNullOrWhiteSpace(bondedHeroine))
            return null;
        return string.Equals(bondedHeroine, NoBondedHeroine, StringComparison.Ordinal)
            ? null
            : bondedHeroine;
    }

    private static string BuildScriptKey(BaseEnding baseEnding, RomanceEndingBondVariant bondVariant)
    {
        return $"ending.{ToSnakeCase(baseEnding.ToString())}.{bondVariant.ToString().ToLowerInvariant()}";
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('_');
            chars.Add(char.ToLowerInvariant(c));
        }

        return new string(chars.ToArray());
    }
}
