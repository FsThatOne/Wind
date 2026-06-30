namespace FengZhi.Foundation.Misunderstanding;

/// <summary>感情系统接口 — 提供里程碑地板查询和诀别触发。</summary>
public interface IRomanceService
{
    int GetMilestoneFloor(string npcId);
    int GetAttitudeScore(string npcId);
    void ForceBreak(string npcId);
    bool IsBrokenState(string npcId);
}
