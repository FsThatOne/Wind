namespace FengZhi.Foundation.Party;

public sealed class PartyGrowthService
{
    private readonly PartyRoster _roster;
    private readonly IPartyAttributeProvider _attributes;

    public float ObservationGrowthRate { get; set; } = 0.35f;
    public float CatchupFloorRatio { get; set; } = 0.85f;
    public int CatchupChapterCap { get; set; } = 8;
    public int DelegateGrowthCapPerChapter { get; set; } = 4;

    public PartyGrowthService(PartyRoster roster, IPartyAttributeProvider attributes)
    {
        _roster = roster;
        _attributes = attributes;
    }

    public void OnBattleCompleted(BattleGrowthConfig config)
    {
        if (!config.GrowthEligible) return;

        var deployed = _roster.GetDeployed();
        var observers = _roster.GetBenchObservers();

        foreach (var member in deployed)
        {
            if (config.InsightGains.Count > 0)
            {
                _attributes.ApplyAttributeGrowth(
                    member.CharacterId, config.InsightGains, GrowthSource.BattleInsight);
            }
        }

        if (config.InsightGains.Count > 0)
        {
            var observerGains = ComputeObserverGains(config.InsightGains);
            foreach (var member in observers)
            {
                _attributes.ApplyAttributeGrowth(
                    member.CharacterId, observerGains, GrowthSource.Observation);
            }
        }
    }

    public int ComputeCatchupGain(string characterId, int currentChapter)
    {
        var member = _roster.GetMember(characterId);
        if (member == null) return 0;

        int totalPower = _attributes.GetTotalPower(characterId);
        int chapterBaseline = _attributes.GetChapterBaselinePower(currentChapter);
        float partyAverage = ComputePartyAveragePower();
        int catchupTarget = Math.Max(chapterBaseline, (int)(partyAverage * CatchupFloorRatio));

        if (totalPower >= catchupTarget) return 0;

        int gap = catchupTarget - totalPower;
        int remainingCap = CatchupChapterCap - member.CatchupUsedThisChapter;
        return Math.Max(0, Math.Min(gap, remainingCap));
    }

    public void ApplyCatchup(string characterId, int currentChapter)
    {
        int gain = ComputeCatchupGain(characterId, currentChapter);
        if (gain <= 0) return;

        var member = _roster.GetMember(characterId);
        if (member == null) return;

        var gains = new Dictionary<string, int> { ["total_power"] = gain };
        _attributes.ApplyAttributeGrowth(characterId, gains, GrowthSource.Catchup);
        member.CatchupUsedThisChapter += gain;
    }

    public void OnDelegationCompleted(DelegationResult result)
    {
        var member = _roster.GetMember(result.CharacterId);
        if (member == null) return;

        _roster.ReturnMember(result.CharacterId);

        if (result.AttributeGains.Count == 0) return;

        int totalGain = result.AttributeGains.Values.Sum();
        int remainingCap = DelegateGrowthCapPerChapter - member.DelegationGrowthThisChapter;
        if (remainingCap <= 0) return;

        if (totalGain <= remainingCap)
        {
            _attributes.ApplyAttributeGrowth(
                result.CharacterId, result.AttributeGains, GrowthSource.Delegation);
            member.DelegationGrowthThisChapter += totalGain;
        }
        else
        {
            float scale = (float)remainingCap / totalGain;
            var scaledGains = result.AttributeGains.ToDictionary(
                kv => kv.Key,
                kv => Math.Max(1, (int)(kv.Value * scale)));
            _attributes.ApplyAttributeGrowth(
                result.CharacterId, scaledGains, GrowthSource.Delegation);
            member.DelegationGrowthThisChapter += scaledGains.Values.Sum();
        }
    }

    public void ApplyChapterBaselineGrowth(int currentChapter, Dictionary<string, int> gains)
    {
        foreach (var member in _roster.GetAllMembers())
        {
            if (member.State == PartyMemberState.Departed) continue;
            _attributes.ApplyAttributeGrowth(
                member.CharacterId, gains, GrowthSource.ChapterBaseline);
        }
    }

    private Dictionary<string, int> ComputeObserverGains(Dictionary<string, int> deployedGains)
    {
        return deployedGains.ToDictionary(
            kv => kv.Key,
            kv => (int)MathF.Floor(kv.Value * ObservationGrowthRate));
    }

    private float ComputePartyAveragePower()
    {
        var members = _roster.GetAllMembers()
            .Where(m => m.IsInPartyRange)
            .ToList();

        if (members.Count == 0) return 0;

        float sum = members.Sum(m => _attributes.GetTotalPower(m.CharacterId));
        return sum / members.Count;
    }
}
