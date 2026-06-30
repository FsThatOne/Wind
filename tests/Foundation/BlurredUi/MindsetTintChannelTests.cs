using FengZhi.Foundation.BlurredUi;
using Xunit;

namespace Foundation.Tests.BlurredUi;

public class MindsetTintChannelTests
{
    private class FakeMindsetProvider : IMindsetDataProvider
    {
        public string Zone { get; set; } = "中庸";
        public float Extremity { get; set; } = 0f;
        public List<string> Echoes { get; set; } = new();
        public int ConsumedCount { get; set; }

        public string GetMindsetZone() => Zone;
        public float GetZoneExtremity() => Extremity;
        public IReadOnlyList<string> GetEchoQueue() => Echoes;
        public void ConsumeEcho(int count) => ConsumedCount += count;
    }

    [Fact]
    public void CheckForZoneChange_NoChange_ReturnsNull()
    {
        var provider = new FakeMindsetProvider { Zone = "中庸" };
        var channel = new MindsetTintChannel(provider);

        Assert.Null(channel.CheckForZoneChange(100));
    }

    [Fact]
    public void CheckForZoneChange_ZoneChanged_ReturnsReveal()
    {
        var provider = new FakeMindsetProvider { Zone = "中庸" };
        var channel = new MindsetTintChannel(provider);

        provider.Zone = "孤剑入世";
        var reveal = channel.CheckForZoneChange(200);

        Assert.NotNull(reveal);
        Assert.Equal(RevealChannel.Mindset, reveal!.Channel);
        Assert.Contains("孤剑入世", reveal.Message);

        var payload = reveal.Payload as MindsetRevealPayload;
        Assert.Equal("中庸", payload!.PreviousZone);
        Assert.Equal("孤剑入世", payload.NewZone);
    }

    [Fact]
    public void GetCurrentTint_ReturnsZoneTint()
    {
        var provider = new FakeMindsetProvider { Zone = "白衣入世" };
        var channel = new MindsetTintChannel(provider);
        channel.CheckForZoneChange(1);

        var tint = channel.GetCurrentTint();
        Assert.Equal(0.04f, tint.R, 3);
        Assert.Equal(0.03f, tint.G, 3);
        Assert.Equal(-0.01f, tint.B, 3);
    }

    [Fact]
    public void GetCurrentIntensity_ScalesWithExtremity()
    {
        var provider = new FakeMindsetProvider { Extremity = 0f };
        var channel = new MindsetTintChannel(provider, baseIntensity: 0.10f, extremityScale: 0.3f);

        Assert.Equal(0.10f, channel.GetCurrentIntensity(), 3);

        provider.Extremity = 1.0f;
        Assert.Equal(0.13f, channel.GetCurrentIntensity(), 3);
    }

    [Fact]
    public void GetCurrentIntensity_MidExtremity()
    {
        var provider = new FakeMindsetProvider { Extremity = 0.5f };
        var channel = new MindsetTintChannel(provider, baseIntensity: 0.10f, extremityScale: 0.3f);

        float expected = 0.10f * (1.0f + 0.5f * 0.3f);
        Assert.Equal(expected, channel.GetCurrentIntensity(), 3);
    }

    [Fact]
    public void GetTintForZone_UnknownZone_ReturnsZero()
    {
        var tint = MindsetTintChannel.GetTintForZone("未知区域");
        Assert.Equal(0f, tint.R);
        Assert.Equal(0f, tint.G);
        Assert.Equal(0f, tint.B);
    }

    [Fact]
    public void EchoConsumption_DelegatesToProvider()
    {
        var provider = new FakeMindsetProvider { Echoes = new List<string> { "echo1", "echo2" } };
        var channel = new MindsetTintChannel(provider);

        var echoes = channel.GetPendingEchoes();
        Assert.Equal(2, echoes.Count);

        channel.ConsumeEchoes(1);
        Assert.Equal(1, provider.ConsumedCount);
    }
}
