namespace FengZhi.Foundation.Party;

public sealed class PartyMember
{
    public string CharacterId { get; }
    public bool IsProtagonist { get; init; }
    public PartyMemberState State { get; set; } = PartyMemberState.NotJoined;
    public int CatchupUsedThisChapter { get; set; }
    public int DelegationGrowthThisChapter { get; set; }
    public string? CurrentDelegationId { get; set; }
    public string? AwayReason { get; set; }

    public PartyMember(string characterId)
    {
        CharacterId = characterId;
    }

    public bool CanDeploy =>
        State == PartyMemberState.Available || State == PartyMemberState.BenchObserver;

    public bool CanUndeploy =>
        State == PartyMemberState.Deployed && !IsProtagonist;

    public bool IsInPartyRange =>
        State is PartyMemberState.Deployed
            or PartyMemberState.Available
            or PartyMemberState.BenchObserver;
}
