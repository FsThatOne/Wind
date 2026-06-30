namespace FengZhi.Foundation.BlurredUi;

public sealed class RelationshipChannel
{
    private readonly IRomanceDataProvider _provider;
    private readonly Dictionary<string, int> _lastKnownTiers = new();

    public RelationshipChannel(IRomanceDataProvider provider)
    {
        _provider = provider;
    }

    public PendingReveal? CheckForTierChange(string npcId, long timestamp)
    {
        int currentTier = _provider.GetAttitudeTier(npcId);

        if (!_lastKnownTiers.TryGetValue(npcId, out int lastTier))
        {
            _lastKnownTiers[npcId] = currentTier;
            return null;
        }

        if (currentTier == lastTier) return null;

        string previousName = TierToName(lastTier);
        string newName = _provider.GetAttitudeTierName(npcId);
        _lastKnownTiers[npcId] = currentTier;

        return new PendingReveal
        {
            Channel = RevealChannel.Relationship,
            Message = $"与{npcId}的关系发生了变化",
            Payload = new RelationshipRevealPayload
            {
                NpcId = npcId,
                PreviousTier = previousName,
                NewTier = newName,
                IsForceBreak = false
            },
            Timestamp = timestamp
        };
    }

    public PendingReveal CreateForceBreakReveal(string npcId, long timestamp)
    {
        _lastKnownTiers[npcId] = 0;

        return new PendingReveal
        {
            Channel = RevealChannel.Relationship,
            Message = $"与{npcId}的关系已决裂",
            Payload = new RelationshipRevealPayload
            {
                NpcId = npcId,
                PreviousTier = "",
                NewTier = "决裂",
                IsForceBreak = true
            },
            Timestamp = timestamp
        };
    }

    public IReadOnlyList<CometEvent> GetPendingCometEvents()
    {
        return _provider.GetPendingCometEvents();
    }

    public void ConsumeCometEvent(string eventId)
    {
        _provider.ConsumeCometEvent(eventId);
    }

    public static PendingReveal CreateMergedReveal(IReadOnlyList<PendingReveal> reveals)
    {
        var latest = reveals[^1];
        return new PendingReveal
        {
            Channel = RevealChannel.Relationship,
            Message = "最近发生了很多事…",
            Payload = latest.Payload,
            Timestamp = latest.Timestamp
        };
    }

    public void SetLastKnownTier(string npcId, int tier)
    {
        _lastKnownTiers[npcId] = tier;
    }

    private static string TierToName(int tier)
    {
        return tier switch
        {
            0 => "决裂",
            1 => "敌视",
            2 => "冷淡",
            3 => "疏远",
            4 => "中立",
            5 => "友善",
            6 => "亲近",
            7 => "信赖",
            _ => "未知"
        };
    }
}
