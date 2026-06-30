namespace FengZhi.Foundation.Misunderstanding;

/// <summary>场景共存查询接口 — 判断玩家与 NPC 是否在同一场景。</summary>
public interface IScenePresenceQuery
{
    bool IsPlayerColocatedWith(string npcId);
}
