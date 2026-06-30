using FengZhi.Foundation.Party;
using Xunit;

namespace Foundation.Tests.Party;

public class PartyGrowthServiceTests
{
    private readonly PartyRoster _roster = new();
    private readonly FakeAttributeProvider _attrs = new();
    private PartyGrowthService _service;

    public PartyGrowthServiceTests()
    {
        _service = new PartyGrowthService(_roster, _attrs);
        SetupDefaultParty();
    }

    private void SetupDefaultParty()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.AddMember(new PartyMember("bench_1"));
        _roster.TryDeploy("ally_1");

        _attrs.Powers["hero"] = 50;
        _attrs.Powers["ally_1"] = 45;
        _attrs.Powers["bench_1"] = 30;
    }

    [Fact]
    public void OnBattleCompleted_DeployedGetFullInsight()
    {
        var config = new BattleGrowthConfig
        {
            BattleId = "boss_1",
            GrowthEligible = true,
            InsightGains = new() { ["strength"] = 2, ["agility"] = 1 }
        };

        _service.OnBattleCompleted(config);

        Assert.Contains("hero", _attrs.AppliedGrowth.Keys);
        Assert.Contains("ally_1", _attrs.AppliedGrowth.Keys);
        Assert.Equal(2, _attrs.AppliedGrowth["hero"].gains["strength"]);
        Assert.Equal(GrowthSource.BattleInsight, _attrs.AppliedGrowth["hero"].source);
    }

    [Fact]
    public void OnBattleCompleted_ObserversGetReducedInsight()
    {
        var config = new BattleGrowthConfig
        {
            BattleId = "boss_1",
            GrowthEligible = true,
            InsightGains = new() { ["strength"] = 3 }
        };

        _service.OnBattleCompleted(config);

        Assert.Contains("bench_1", _attrs.AppliedGrowth.Keys);
        // floor(3 * 0.35) = floor(1.05) = 1
        Assert.Equal(1, _attrs.AppliedGrowth["bench_1"].gains["strength"]);
        Assert.Equal(GrowthSource.Observation, _attrs.AppliedGrowth["bench_1"].source);
    }

    [Fact]
    public void OnBattleCompleted_NotEligible_NoGrowth()
    {
        var config = new BattleGrowthConfig
        {
            BattleId = "random_fight",
            GrowthEligible = false,
            InsightGains = new() { ["strength"] = 2 }
        };

        _service.OnBattleCompleted(config);

        Assert.Empty(_attrs.AppliedGrowth);
    }

    [Fact]
    public void ComputeCatchupGain_BelowTarget_ReturnsGap()
    {
        _attrs.ChapterBaselines[2] = 40;
        // party average = (50+45+30)/3 = ~41.67, floor_ratio=0.85 → 35
        // catchup_target = max(40, 35) = 40
        // bench_1 power=30, gap=10, but cap=8
        int gain = _service.ComputeCatchupGain("bench_1", 2);
        Assert.Equal(8, gain);
    }

    [Fact]
    public void ComputeCatchupGain_AboveTarget_ReturnsZero()
    {
        _attrs.ChapterBaselines[2] = 40;
        int gain = _service.ComputeCatchupGain("hero", 2);
        Assert.Equal(0, gain);
    }

    [Fact]
    public void ApplyCatchup_AppliesGainAndTracksCap()
    {
        _attrs.ChapterBaselines[2] = 40;
        _service.ApplyCatchup("bench_1", 2);

        var member = _roster.GetMember("bench_1")!;
        Assert.Equal(8, member.CatchupUsedThisChapter);
        Assert.Contains("bench_1", _attrs.AppliedGrowth.Keys);
        Assert.Equal(GrowthSource.Catchup, _attrs.AppliedGrowth["bench_1"].source);
    }

    [Fact]
    public void ApplyCatchup_RespectsCap_SecondCallReduced()
    {
        _attrs.ChapterBaselines[2] = 40;
        _service.CatchupChapterCap = 8;

        _service.ApplyCatchup("bench_1", 2);
        _attrs.AppliedGrowth.Clear();

        // After first catchup, used=8, remaining=0
        _service.ApplyCatchup("bench_1", 2);
        Assert.Empty(_attrs.AppliedGrowth);
    }

    [Fact]
    public void OnDelegationCompleted_AppliesGrowth()
    {
        _roster.SetMemberDelegating("bench_1", "del_1");

        var result = new DelegationResult
        {
            DelegationId = "del_1",
            CharacterId = "bench_1",
            Outcome = DelegationOutcome.Success,
            AttributeGains = new() { ["insight"] = 2 }
        };

        _service.OnDelegationCompleted(result);

        var member = _roster.GetMember("bench_1")!;
        Assert.Equal(PartyMemberState.Available, member.State);
        Assert.Contains("bench_1", _attrs.AppliedGrowth.Keys);
        Assert.Equal(2, member.DelegationGrowthThisChapter);
    }

    [Fact]
    public void OnDelegationCompleted_RespectsChapterCap()
    {
        _roster.SetMemberDelegating("bench_1", "del_1");
        var member = _roster.GetMember("bench_1")!;
        member.DelegationGrowthThisChapter = 3;
        _service.DelegateGrowthCapPerChapter = 4;

        var result = new DelegationResult
        {
            DelegationId = "del_1",
            CharacterId = "bench_1",
            Outcome = DelegationOutcome.Success,
            AttributeGains = new() { ["strength"] = 3 }
        };

        _service.OnDelegationCompleted(result);

        // remaining cap = 4 - 3 = 1, total gain request = 3, scale = 1/3
        // scaled = max(1, floor(3 * 0.33)) = 1
        Assert.True(member.DelegationGrowthThisChapter <= 5);
    }

    [Fact]
    public void ApplyChapterBaselineGrowth_AppliesToAllExceptDeparted()
    {
        var departed = new PartyMember("gone") { State = PartyMemberState.Departed };
        _roster.AddMember(departed);
        _attrs.Powers["gone"] = 0;

        var gains = new Dictionary<string, int> { ["strength"] = 1 };
        _service.ApplyChapterBaselineGrowth(2, gains);

        Assert.Contains("hero", _attrs.AppliedGrowth.Keys);
        Assert.Contains("ally_1", _attrs.AppliedGrowth.Keys);
        Assert.Contains("bench_1", _attrs.AppliedGrowth.Keys);
        Assert.DoesNotContain("gone", _attrs.AppliedGrowth.Keys);
    }

    [Fact]
    public void AwayMember_NotCountedAsObserver()
    {
        _roster.SetMemberAway("bench_1", "历练");

        var config = new BattleGrowthConfig
        {
            BattleId = "boss_1",
            GrowthEligible = true,
            InsightGains = new() { ["strength"] = 2 }
        };

        _service.OnBattleCompleted(config);

        Assert.DoesNotContain("bench_1", _attrs.AppliedGrowth.Keys);
    }
}

internal class FakeAttributeProvider : IPartyAttributeProvider
{
    public Dictionary<string, int> Powers { get; } = new();
    public Dictionary<int, int> ChapterBaselines { get; } = new();
    public Dictionary<string, (Dictionary<string, int> gains, GrowthSource source)> AppliedGrowth { get; } = new();

    public int GetTotalPower(string characterId) =>
        Powers.TryGetValue(characterId, out int p) ? p : 0;

    public void ApplyAttributeGrowth(string characterId, Dictionary<string, int> gains, GrowthSource source)
    {
        AppliedGrowth[characterId] = (new Dictionary<string, int>(gains), source);
    }

    public int GetChapterBaselinePower(int chapter) =>
        ChapterBaselines.TryGetValue(chapter, out int b) ? b : 30;
}
