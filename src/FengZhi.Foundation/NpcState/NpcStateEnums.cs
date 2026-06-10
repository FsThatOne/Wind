namespace FengZhi.Foundation.NpcState;

/// <summary>NPC 生命状态</summary>
public enum LifeStatus { Unknown, Alive, Injured, Dying, Dead }

/// <summary>NPC 在场状态</summary>
public enum PresenceStatus { Unreachable, InParty, NearbyVisible, SameRegion, FarRegion, Departed }

/// <summary>NPC 场所位置</summary>
public enum LocationStatus { Unknown, Town, Wilderness, Dungeon, Hideout, Traveling }

/// <summary>NPC 可交互状态</summary>
public enum InteractionStatus { NotInteractable, DialogueAvailable, QuestGiver, Merchant, Training, Busy }

/// <summary>NPC 旅程阶段</summary>
public enum JourneyStage { NotStarted, Preparing, InProgress, Returning, Completed, Failed }

/// <summary>NPC 关系阶段</summary>
public enum RelationshipStage { Stranger, Acquaintance, Friend, CloseFriend, SwornSibling, Rival, Enemy }

/// <summary>NPC 态度等级（-4 到 +3 共 8 档）</summary>
public enum AttitudeLevel
{
    DrawnSword = -4,      // 拔剑相向
    HostileGuard = -3,    // 怒目相视
    ColdShoulder = -2,    // 冷眼旁观
    Wary = -1,            // 心存芥蒂
    Stranger = 0,         // 萍水相逢
    Friendly = 1,         // 颇为投契
    Trusted = 2,          // 肝胆相照
    LifeDeath = 3         // 生死相托
}
