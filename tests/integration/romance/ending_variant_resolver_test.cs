using FengZhi.Foundation.Mindset;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public class EndingVariantResolverTest
{
    [Theory]
    [InlineData(-30)]
    [InlineData(-50)]
    public void Resolve_WhenMoralityIsDemonicReturnsDemonicOverrideRegardlessOfBond(int moralityTier)
    {
        var resolver = new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>
        {
            ["heroine_a"] = new HashSet<MindsetZone> { MindsetZone.DaYinYuShi }
        });

        var bonded = resolver.Resolve(MindsetZone.DaYinYuShi, moralityTier, "heroine_a");
        var unbonded = resolver.Resolve(MindsetZone.ZhongYong, moralityTier, EndingResolver.NoBondedHeroine);

        Assert.Equal(EndingResolver.DemonicEndingKey, bonded.ScriptKey);
        Assert.Equal(RomanceEndingBondVariant.Demonic, bonded.BondVariant);
        Assert.Equal(BaseEnding.MoDao, bonded.BaseEnding);
        Assert.Equal("heroine_a", bonded.BondedHeroine);
        Assert.Equal(EndingResolver.DemonicEndingKey, unbonded.ScriptKey);
        Assert.Equal(RomanceEndingBondVariant.Demonic, unbonded.BondVariant);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("None")]
    public void Resolve_WhenNonDemonicAndUnbondedReturnsSoloVariant(string? bondedHeroine)
    {
        var resolver = new EndingResolver();

        var result = resolver.Resolve(MindsetZone.FengZhiChenYan, 20, bondedHeroine);

        Assert.Equal(RomanceEndingBondVariant.Solo, result.BondVariant);
        Assert.Equal(BaseEnding.FengZhiJianMing, result.BaseEnding);
        Assert.Equal(RomanceEndingNarratorTone.Light, result.NarratorTone);
        Assert.Equal(MindsetZone.FengZhiChenYan, result.MindsetZone);
        Assert.Null(result.BondedHeroine);
        Assert.Equal("ending.feng_zhi_jian_ming.solo", result.ScriptKey);
    }

    [Fact]
    public void Resolve_WhenNonDemonicBondedAndCompatibleReturnsCompanionVariant()
    {
        var resolver = new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>
        {
            ["heroine_a"] = new HashSet<MindsetZone>
            {
                MindsetZone.FengZhiChenYan,
                MindsetZone.DaYinYuShi
            }
        });

        var result = resolver.Resolve(MindsetZone.FengZhiChenYan, 0, "heroine_a");

        Assert.Equal(RomanceEndingBondVariant.Companion, result.BondVariant);
        Assert.Equal(BaseEnding.FengZhiJianMing, result.BaseEnding);
        Assert.Equal(RomanceEndingNarratorTone.Neutral, result.NarratorTone);
        Assert.Equal("heroine_a", result.BondedHeroine);
        Assert.Equal("ending.feng_zhi_jian_ming.companion", result.ScriptKey);
    }

    [Fact]
    public void Resolve_WhenNonDemonicBondedAndIncompatibleReturnsFarewellVariant()
    {
        var resolver = new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>
        {
            ["heroine_a"] = new HashSet<MindsetZone> { MindsetZone.DaYinYuShi }
        });

        var result = resolver.Resolve(MindsetZone.ZhongYong, -10, "heroine_a");

        Assert.Equal(RomanceEndingBondVariant.Farewell, result.BondVariant);
        Assert.Equal(BaseEnding.Undecided, result.BaseEnding);
        Assert.Equal(RomanceEndingNarratorTone.Neutral, result.NarratorTone);
        Assert.Equal("heroine_a", result.BondedHeroine);
        Assert.Equal("ending.undecided.farewell", result.ScriptKey);
    }

    [Fact]
    public void Resolve_WhenCompatibilityConfigMissingFallsBackToFarewell()
    {
        var resolver = new EndingResolver();

        var result = resolver.Resolve(MindsetZone.BaiYiRuShi, 10, "heroine_a");

        Assert.False(resolver.IsZoneCompatible("heroine_a", MindsetZone.BaiYiRuShi));
        Assert.Equal(RomanceEndingBondVariant.Farewell, result.BondVariant);
        Assert.Equal(BaseEnding.BaiYiXingTian, result.BaseEnding);
        Assert.Equal("ending.bai_yi_xing_tian.farewell", result.ScriptKey);
    }

    [Theory]
    [InlineData(-20, RomanceEndingNarratorTone.Dark)]
    [InlineData(0, RomanceEndingNarratorTone.Neutral)]
    [InlineData(20, RomanceEndingNarratorTone.Light)]
    public void Resolve_WhenNarratorToneChangesDoesNotAlterBranchIdentity(
        int moralityTier,
        RomanceEndingNarratorTone expectedTone)
    {
        var resolver = new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>
        {
            ["heroine_a"] = new HashSet<MindsetZone> { MindsetZone.ShiHuaiWeiDing }
        });

        var result = resolver.Resolve(MindsetZone.ShiHuaiWeiDing, moralityTier, "heroine_a");

        Assert.Equal(RomanceEndingBondVariant.Companion, result.BondVariant);
        Assert.Equal(expectedTone, result.NarratorTone);
        Assert.Equal("ending.da_yin_yu_shi.companion", result.ScriptKey);
    }

    [Fact]
    public void ToBaseEnding_WhenAllMindsetZonesAreEnumeratedCollapsesToFiveCoreScripts()
    {
        var expected = new Dictionary<MindsetZone, BaseEnding>
        {
            [MindsetZone.GuJianRuShi] = BaseEnding.GuJianZhanShi,
            [MindsetZone.ZhiNianWeiDing] = BaseEnding.GuJianZhanShi,
            [MindsetZone.FengZhiChenYan] = BaseEnding.FengZhiJianMing,
            [MindsetZone.RuShiWeiDing] = BaseEnding.GuJianZhanShi,
            [MindsetZone.ZhongYong] = BaseEnding.Undecided,
            [MindsetZone.ChuShiWeiDing] = BaseEnding.FengZhiJianMing,
            [MindsetZone.BaiYiRuShi] = BaseEnding.BaiYiXingTian,
            [MindsetZone.ShiHuaiWeiDing] = BaseEnding.DaYinYuShi,
            [MindsetZone.DaYinYuShi] = BaseEnding.DaYinYuShi
        };

        var actual = Enum.GetValues<MindsetZone>()
            .ToDictionary(zone => zone, EndingResolver.ToBaseEnding);

        Assert.Equal(expected, actual);
        Assert.Equal(5, actual.Values.Distinct().Count());
    }

    [Fact]
    public void Resolve_WhenAllZonesAndBondVariantsAreEnumeratedProducesSixteenTotalVariantKeys()
    {
        var allZones = Enum.GetValues<MindsetZone>();
        var resolver = new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>
        {
            ["heroine_a"] = new HashSet<MindsetZone>(allZones)
        });

        var nonDemonicKeys = allZones
            .SelectMany(zone => new[]
            {
                resolver.Resolve(zone, 0, EndingResolver.NoBondedHeroine).ScriptKey,
                resolver.Resolve(zone, 0, "heroine_a").ScriptKey,
                new EndingResolver(new Dictionary<string, IReadOnlySet<MindsetZone>>()).Resolve(zone, 0, "heroine_a").ScriptKey
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(15, nonDemonicKeys.Length);
        Assert.Equal(16, nonDemonicKeys.Append(EndingResolver.DemonicEndingKey).Distinct(StringComparer.Ordinal).Count());
    }
}
