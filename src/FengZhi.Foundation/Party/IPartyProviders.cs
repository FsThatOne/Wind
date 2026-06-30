namespace FengZhi.Foundation.Party;

public interface IPartyAttributeProvider
{
    int GetTotalPower(string characterId);
    void ApplyAttributeGrowth(string characterId, Dictionary<string, int> gains, GrowthSource source);
    int GetChapterBaselinePower(int chapter);
}

public interface IPartyNpcStateProvider
{
    bool IsCompanionAvailable(string characterId);
    bool IsDelegateAllowed(string characterId, string delegationId);
}

public sealed class BattleGrowthConfig
{
    public required string BattleId { get; init; }
    public bool GrowthEligible { get; init; } = true;
    public Dictionary<string, int> InsightGains { get; init; } = new();
    public IReadOnlyList<string>? LockedDeployment { get; init; }
}

public sealed class DelegationResult
{
    public required string DelegationId { get; init; }
    public required string CharacterId { get; init; }
    public DelegationOutcome Outcome { get; init; }
    public Dictionary<string, int> AttributeGains { get; init; } = new();
}

public enum DelegationOutcome
{
    Success,
    PartialSuccess,
    Failure
}
