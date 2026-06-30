namespace FengZhi.Foundation.Party;

public sealed class PartyRoster
{
    public const int MaxDeployedParty = 5;

    private readonly Dictionary<string, PartyMember> _members = new();
    private readonly List<string> _deployedOrder = [];
    private readonly HashSet<string> _lockedDeployment = [];

    public IReadOnlyList<string> DeployedOrder => _deployedOrder;
    public int DeployedCount => _deployedOrder.Count;

    public void AddMember(PartyMember member)
    {
        _members[member.CharacterId] = member;
        if (member.IsProtagonist)
        {
            member.State = PartyMemberState.Deployed;
            if (!_deployedOrder.Contains(member.CharacterId))
                _deployedOrder.Insert(0, member.CharacterId);
        }
        else if (member.State == PartyMemberState.NotJoined)
        {
            member.State = PartyMemberState.Available;
        }
    }

    public PartyMember? GetMember(string characterId) =>
        _members.TryGetValue(characterId, out var m) ? m : null;

    public IReadOnlyCollection<PartyMember> GetAllMembers() => _members.Values;

    public IReadOnlyList<PartyMember> GetDeployed() =>
        _deployedOrder.Select(id => _members[id]).ToList();

    public IReadOnlyList<PartyMember> GetBenchObservers() =>
        _members.Values
            .Where(m => m.IsInPartyRange && !_deployedOrder.Contains(m.CharacterId))
            .ToList();

    public DeployResult TryDeploy(string characterId)
    {
        if (!_members.TryGetValue(characterId, out var member))
            return DeployResult.NotFound;

        if (_deployedOrder.Contains(characterId))
            return DeployResult.AlreadyDeployed;

        if (!member.CanDeploy)
            return DeployResult.Unavailable;

        if (_deployedOrder.Count >= MaxDeployedParty)
            return DeployResult.PartyFull;

        if (_lockedDeployment.Count > 0 && !_lockedDeployment.Contains(characterId))
            return DeployResult.Locked;

        member.State = PartyMemberState.Deployed;
        _deployedOrder.Add(characterId);
        return DeployResult.Success;
    }

    public DeployResult TryUndeploy(string characterId)
    {
        if (!_members.TryGetValue(characterId, out var member))
            return DeployResult.NotFound;

        if (!member.CanUndeploy)
            return member.IsProtagonist ? DeployResult.ProtagonistLocked : DeployResult.Unavailable;

        if (_lockedDeployment.Contains(characterId))
            return DeployResult.Locked;

        member.State = PartyMemberState.BenchObserver;
        _deployedOrder.Remove(characterId);
        return DeployResult.Success;
    }

    public void SetDeploymentLock(IEnumerable<string>? lockedIds)
    {
        _lockedDeployment.Clear();
        if (lockedIds != null)
        {
            foreach (var id in lockedIds)
                _lockedDeployment.Add(id);
        }
    }

    public void ClearDeploymentLock() => _lockedDeployment.Clear();

    public bool IsDeploymentLocked => _lockedDeployment.Count > 0;

    public void SetMemberAway(string characterId, string reason)
    {
        if (!_members.TryGetValue(characterId, out var member)) return;
        if (member.IsProtagonist) return;

        _deployedOrder.Remove(characterId);
        member.State = PartyMemberState.AwayTraining;
        member.AwayReason = reason;
    }

    public void SetMemberDelegating(string characterId, string delegationId)
    {
        if (!_members.TryGetValue(characterId, out var member)) return;
        if (member.IsProtagonist) return;

        _deployedOrder.Remove(characterId);
        member.State = PartyMemberState.Delegating;
        member.CurrentDelegationId = delegationId;
    }

    public void ReturnMember(string characterId)
    {
        if (!_members.TryGetValue(characterId, out var member)) return;
        member.State = PartyMemberState.Available;
        member.AwayReason = null;
        member.CurrentDelegationId = null;
    }

    public void OnChapterChanged()
    {
        foreach (var member in _members.Values)
        {
            member.CatchupUsedThisChapter = 0;
            member.DelegationGrowthThisChapter = 0;
        }
    }
}

public enum DeployResult
{
    Success,
    NotFound,
    AlreadyDeployed,
    Unavailable,
    PartyFull,
    ProtagonistLocked,
    Locked
}
