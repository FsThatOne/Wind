using FengZhi.Foundation.Party;
using Xunit;

namespace Foundation.Tests.Party;

public class PartyRosterTests
{
    private readonly PartyRoster _roster = new();

    [Fact]
    public void AddProtagonist_AutoDeployed()
    {
        var protagonist = new PartyMember("hero") { IsProtagonist = true };
        _roster.AddMember(protagonist);

        Assert.Equal(PartyMemberState.Deployed, protagonist.State);
        Assert.Equal(1, _roster.DeployedCount);
        Assert.Equal("hero", _roster.DeployedOrder[0]);
    }

    [Fact]
    public void AddCompanion_BecomesAvailable()
    {
        var companion = new PartyMember("ally_1");
        _roster.AddMember(companion);

        Assert.Equal(PartyMemberState.Available, companion.State);
        Assert.Equal(0, _roster.DeployedCount);
    }

    [Fact]
    public void Deploy_Success()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("ally_1"));

        var result = _roster.TryDeploy("ally_1");

        Assert.Equal(DeployResult.Success, result);
        Assert.Equal(2, _roster.DeployedCount);
    }

    [Fact]
    public void Deploy_PartyFull_AtFive()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        for (int i = 1; i <= 4; i++)
        {
            _roster.AddMember(new PartyMember($"ally_{i}"));
            _roster.TryDeploy($"ally_{i}");
        }

        _roster.AddMember(new PartyMember("ally_5"));
        var result = _roster.TryDeploy("ally_5");

        Assert.Equal(DeployResult.PartyFull, result);
        Assert.Equal(5, _roster.DeployedCount);
    }

    [Fact]
    public void Undeploy_Protagonist_Blocked()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });

        var result = _roster.TryUndeploy("hero");

        Assert.Equal(DeployResult.ProtagonistLocked, result);
        Assert.Equal(PartyMemberState.Deployed, _roster.GetMember("hero")!.State);
    }

    [Fact]
    public void Undeploy_Companion_Success()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.TryDeploy("ally_1");

        var result = _roster.TryUndeploy("ally_1");

        Assert.Equal(DeployResult.Success, result);
        Assert.Equal(PartyMemberState.BenchObserver, _roster.GetMember("ally_1")!.State);
        Assert.Equal(1, _roster.DeployedCount);
    }

    [Fact]
    public void DeploymentLock_PreventsUnlistedDeploy()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.AddMember(new PartyMember("ally_2"));
        _roster.SetDeploymentLock(["hero", "ally_1"]);

        var result = _roster.TryDeploy("ally_2");

        Assert.Equal(DeployResult.Locked, result);
    }

    [Fact]
    public void SetMemberAway_RemovesFromDeployedAndSetsState()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.TryDeploy("ally_1");

        _roster.SetMemberAway("ally_1", "离队调查");

        var member = _roster.GetMember("ally_1")!;
        Assert.Equal(PartyMemberState.AwayTraining, member.State);
        Assert.Equal("离队调查", member.AwayReason);
        Assert.Equal(1, _roster.DeployedCount);
    }

    [Fact]
    public void SetMemberDelegating_SetsStateAndDelegationId()
    {
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.SetMemberDelegating("ally_1", "delegation_001");

        var member = _roster.GetMember("ally_1")!;
        Assert.Equal(PartyMemberState.Delegating, member.State);
        Assert.Equal("delegation_001", member.CurrentDelegationId);
    }

    [Fact]
    public void ReturnMember_RestoresAvailableState()
    {
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.SetMemberAway("ally_1", "历练");

        _roster.ReturnMember("ally_1");

        var member = _roster.GetMember("ally_1")!;
        Assert.Equal(PartyMemberState.Available, member.State);
        Assert.Null(member.AwayReason);
    }

    [Fact]
    public void GetBenchObservers_ExcludesDeployedAndAway()
    {
        _roster.AddMember(new PartyMember("hero") { IsProtagonist = true });
        _roster.AddMember(new PartyMember("deployed"));
        _roster.AddMember(new PartyMember("bench"));
        _roster.AddMember(new PartyMember("away"));
        _roster.TryDeploy("deployed");
        _roster.SetMemberAway("away", "远行");

        var observers = _roster.GetBenchObservers();

        Assert.Single(observers);
        Assert.Equal("bench", observers[0].CharacterId);
    }

    [Fact]
    public void OnChapterChanged_ResetsCaps()
    {
        _roster.AddMember(new PartyMember("ally_1"));
        var member = _roster.GetMember("ally_1")!;
        member.CatchupUsedThisChapter = 5;
        member.DelegationGrowthThisChapter = 3;

        _roster.OnChapterChanged();

        Assert.Equal(0, member.CatchupUsedThisChapter);
        Assert.Equal(0, member.DelegationGrowthThisChapter);
    }

    [Fact]
    public void AwayMember_CannotDeploy()
    {
        _roster.AddMember(new PartyMember("ally_1"));
        _roster.SetMemberAway("ally_1", "历练");

        var result = _roster.TryDeploy("ally_1");

        Assert.Equal(DeployResult.Unavailable, result);
    }
}
