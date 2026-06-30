namespace FengZhi.Foundation.LivingJianghu;

public interface ILivingJianghuContext
{
    bool HasFlag(string flag);
    void SetFlag(string flag);
    int GetDaysSinceFlag(string flag);
    int GetCurrentChapter();
    bool IsBreathing();
    int GetCurrentDay();
    Season GetCurrentSeason();
    string GetPlayerRegion();
    int GetRegionDistance(string fromRegion, string toRegion);
    string? GetNpcAxisValue(string npcId, string axis);
    string GetMindsetZone();
}

public interface ILivingJianghuNpcHandler
{
    void ApplyNpcStateChange(NpcStateChange change);
}

public interface ILivingJianghuPresenter
{
    void DeliverEvent(WorldEventConfig config, EventInstance instance);
    void PlayWorldPulse();
}
