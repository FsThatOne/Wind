namespace FengZhi.Foundation.Party;

public sealed class PartySaveData
{
    public List<PartyMemberData> Members { get; set; } = [];
    public List<string> DeployedOrder { get; set; } = [];
}

public sealed class PartyMemberData
{
    public required string CharacterId { get; set; }
    public bool IsProtagonist { get; set; }
    public PartyMemberState State { get; set; }
    public int CatchupUsedThisChapter { get; set; }
    public int DelegationGrowthThisChapter { get; set; }
    public string? CurrentDelegationId { get; set; }
    public string? AwayReason { get; set; }
}
