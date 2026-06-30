namespace FengZhi.Foundation.BlurredUi;

public interface IRealmDataProvider
{
    int GetTotalPower(string characterId);
    int GetRealmTier(string characterId);
    string GetRealmName(string characterId);
    int GetRelativeStrength(string selfId, string targetId);
}

public interface IMindsetDataProvider
{
    string GetMindsetZone();
    float GetZoneExtremity();
    IReadOnlyList<string> GetEchoQueue();
    void ConsumeEcho(int count);
}

public interface IRomanceDataProvider
{
    int GetAttitudeTier(string npcId);
    string GetAttitudeTierName(string npcId);
    IReadOnlyList<CometEvent> GetPendingCometEvents();
    void ConsumeCometEvent(string eventId);
}

public sealed class CometEvent
{
    public string Id { get; init; } = string.Empty;
    public string NpcId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
