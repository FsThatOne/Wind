using FengZhi.Foundation.BlurredUi;
using Xunit;

namespace Foundation.Tests.BlurredUi;

public class RealmChannelTests
{
    private class FakeRealmProvider : IRealmDataProvider
    {
        public int TotalPower { get; set; } = 40;
        public int RealmTier { get; set; } = 1;
        public string RealmName { get; set; } = "初窥门径";
        public int RelativeStrength { get; set; } = 0;

        public int GetTotalPower(string characterId) => TotalPower;
        public int GetRealmTier(string characterId) => RealmTier;
        public string GetRealmName(string characterId) => RealmName;
        public int GetRelativeStrength(string selfId, string targetId) => RelativeStrength;
    }

    [Fact]
    public void Initialize_SetsBaseline_NoReveal()
    {
        var provider = new FakeRealmProvider { RealmTier = 2 };
        var channel = new RealmChannel(provider);

        channel.Initialize("player");
        var reveal = channel.CheckForBreakthrough("player", 100);

        Assert.Null(reveal);
        Assert.Equal(2, channel.LastKnownTier);
    }

    [Fact]
    public void CheckForBreakthrough_TierIncrease_ReturnsReveal()
    {
        var provider = new FakeRealmProvider { RealmTier = 2, RealmName = "初窥门径" };
        var channel = new RealmChannel(provider);
        channel.Initialize("player");

        provider.RealmTier = 3;
        provider.RealmName = "登堂入室";
        var reveal = channel.CheckForBreakthrough("player", 200);

        Assert.NotNull(reveal);
        Assert.Equal(RevealChannel.Realm, reveal!.Channel);
        Assert.Contains("登堂入室", reveal.Message);

        var payload = reveal.Payload as RealmRevealPayload;
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.PreviousTier);
        Assert.Equal(3, payload.NewTier);
        Assert.False(payload.IsMultiBreakthrough);
    }

    [Fact]
    public void CheckForBreakthrough_MultiTierJump_IsMulti()
    {
        var provider = new FakeRealmProvider { RealmTier = 2 };
        var channel = new RealmChannel(provider);
        channel.Initialize("player");

        provider.RealmTier = 5;
        provider.RealmName = "驾轻就熟";
        var reveal = channel.CheckForBreakthrough("player", 300);

        var payload = reveal!.Payload as RealmRevealPayload;
        Assert.True(payload!.IsMultiBreakthrough);
        Assert.Contains("连破数境", reveal.Message);
    }

    [Fact]
    public void CheckForBreakthrough_NoChange_ReturnsNull()
    {
        var provider = new FakeRealmProvider { RealmTier = 3 };
        var channel = new RealmChannel(provider);
        channel.Initialize("player");

        Assert.Null(channel.CheckForBreakthrough("player", 100));
    }

    [Fact]
    public void GetRelativeStrengthText_FarWeaker()
    {
        var provider = new FakeRealmProvider { RelativeStrength = -3 };
        var channel = new RealmChannel(provider);

        string text = channel.GetRelativeStrengthText("player", "boss");
        Assert.Equal("此人深不可测，远非你所能窥", text);
    }

    [Fact]
    public void GetRelativeStrengthText_Equal()
    {
        var provider = new FakeRealmProvider { RelativeStrength = 0 };
        var channel = new RealmChannel(provider);

        Assert.Equal("此人修为与你相当", channel.GetRelativeStrengthText("player", "npc"));
    }

    [Fact]
    public void GetRelativeStrengthText_FarStronger()
    {
        var provider = new FakeRealmProvider { RelativeStrength = 3 };
        var channel = new RealmChannel(provider);

        Assert.Equal("此人不过尔尔", channel.GetRelativeStrengthText("player", "weak"));
    }

    [Fact]
    public void GetRelativeStrengthText_ClampsBeyondRange()
    {
        var provider = new FakeRealmProvider { RelativeStrength = -10 };
        var channel = new RealmChannel(provider);

        Assert.Equal("此人深不可测，远非你所能窥", channel.GetRelativeStrengthText("player", "god"));
    }
}
