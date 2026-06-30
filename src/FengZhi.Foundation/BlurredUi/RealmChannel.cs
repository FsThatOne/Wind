using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.BlurredUi;

public sealed class RealmChannel
{
    private static readonly string[] RelativeStrengthTexts =
    {
        "此人深不可测，远非你所能窥",
        "此人功力远在你之上",
        "此人修为略胜于你",
        "此人修为与你相当",
        "此人修为不及你",
        "此人远非你的对手",
        "此人不过尔尔"
    };

    private readonly IRealmDataProvider _provider;
    private int _lastKnownTier = -1;

    public RealmChannel(IRealmDataProvider provider)
    {
        _provider = provider;
    }

    public void Initialize(string characterId)
    {
        _lastKnownTier = _provider.GetRealmTier(characterId);
    }

    public PendingReveal? CheckForBreakthrough(string characterId, long timestamp)
    {
        int currentTier = _provider.GetRealmTier(characterId);

        if (_lastKnownTier < 0)
        {
            _lastKnownTier = currentTier;
            return null;
        }

        if (currentTier <= _lastKnownTier) return null;

        bool isMulti = (currentTier - _lastKnownTier) > 1;
        string realmName = _provider.GetRealmName(characterId);
        int previousTier = _lastKnownTier;
        _lastKnownTier = currentTier;

        return new PendingReveal
        {
            Channel = RevealChannel.Realm,
            Message = isMulti ? $"连破数境，已入{realmName}之境" : $"突破至{realmName}",
            Payload = new RealmRevealPayload
            {
                PreviousTier = previousTier,
                NewTier = currentTier,
                RealmName = realmName,
                IsMultiBreakthrough = isMulti
            },
            Timestamp = timestamp
        };
    }

    public string GetCurrentRealmDisplay(string characterId)
    {
        return _provider.GetRealmName(characterId);
    }

    public string GetRelativeStrengthText(string selfId, string targetId)
    {
        int diff = _provider.GetRelativeStrength(selfId, targetId);
        int index = Math.Clamp(diff + 3, 0, RelativeStrengthTexts.Length - 1);
        return RelativeStrengthTexts[index];
    }

    public static PendingReveal CreateMergedReveal(PendingReveal latest)
    {
        return latest;
    }

    public int LastKnownTier => _lastKnownTier;

    public void SetLastKnownTier(int tier)
    {
        _lastKnownTier = tier;
    }
}
