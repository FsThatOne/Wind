// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
namespace FengzhiSlice;

public enum MoveType
{
    Gang,  // 刚 — aggressive, breaks through defense
    Rou,   // 柔 — defensive, deflects and counters
    Qiao   // 巧 — tricky, exploits openings
}

public enum MoveCategory
{
    Attack,
    Defend,
    Special
}

public class MartialMove
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public MoveType Type { get; set; }
    public MoveCategory Category { get; set; }
    public int BaseDamage { get; set; }
    public int NeiliCost { get; set; }
    public int StaggerValue { get; set; } // Stagger inflicted on hit
    public bool GrantsNeili { get; set; } // Rou defense + deflect = +1 neili
    public int CooldownTurns { get; set; } // Cooldown after use (0 = always available)
    
    /// <summary>
    /// Counter multiplier: 1.3x when countering the weak type.
    /// Gang > Qiao > Rou > Gang (circular)
    /// </summary>
    public static float GetCounterMultiplier(MoveType attacker, MoveType defender)
    {
        // Gang beats Qiao, Qiao beats Rou, Rou beats Gang
        if (attacker == MoveType.Gang && defender == MoveType.Qiao) return 1.3f;
        if (attacker == MoveType.Qiao && defender == MoveType.Rou) return 1.3f;
        if (attacker == MoveType.Rou && defender == MoveType.Gang) return 1.3f;
        
        // Being countered
        if (defender == MoveType.Gang && attacker == MoveType.Qiao) return 0.7f;
        if (defender == MoveType.Qiao && attacker == MoveType.Rou) return 0.7f;
        if (defender == MoveType.Rou && attacker == MoveType.Gang) return 0.7f;
        
        return 1.0f; // Same type = neutral
    }
    
    // === 风止尺法 (Player martial art) ===
    public static MartialMove[] GetFengzhiRulerSet() => new[]
    {
        new MartialMove
        {
            Id = "fz_zhengfeng", Name = "正锋", Description = "尺中藏剑意，正面破敌",
            Type = MoveType.Gang, Category = MoveCategory.Attack,
            BaseDamage = 25, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 0
        },
        new MartialMove
        {
            Id = "fz_huiwan", Name = "回腕", Description = "尺尾旋转，以柔克刚",
            Type = MoveType.Rou, Category = MoveCategory.Defend,
            BaseDamage = 10, NeiliCost = 0, StaggerValue = 0, GrantsNeili = true, CooldownTurns = 1
        },
        new MartialMove
        {
            Id = "fz_cunjin", Name = "寸劲", Description = "贴身短打，巧妙寻隙",
            Type = MoveType.Qiao, Category = MoveCategory.Attack,
            BaseDamage = 20, NeiliCost = 1, StaggerValue = 2, CooldownTurns = 1
        },
        new MartialMove
        {
            Id = "fz_pojun", Name = "破军", Description = "全力一击，尺破苍穹",
            Type = MoveType.Gang, Category = MoveCategory.Special,
            BaseDamage = 45, NeiliCost = 3, StaggerValue = 0, CooldownTurns = 2
        }
    };
    
    // === 山贼刀法 (Enemy martial art) ===
    public static MartialMove[] GetBanditSaberSet() => new[]
    {
        new MartialMove
        {
            Id = "bd_pikan", Name = "劈砍", Description = "蛮力一刀",
            Type = MoveType.Gang, Category = MoveCategory.Attack,
            BaseDamage = 20, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 0
        },
        new MartialMove
        {
            Id = "bd_gedang", Name = "格挡", Description = "刀背横架",
            Type = MoveType.Rou, Category = MoveCategory.Defend,
            BaseDamage = 5, NeiliCost = 0, StaggerValue = 0, GrantsNeili = true, CooldownTurns = 1
        },
        new MartialMove
        {
            Id = "bd_cisha", Name = "刺杀", Description = "阴狠一刺",
            Type = MoveType.Qiao, Category = MoveCategory.Attack,
            BaseDamage = 18, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 1
        }
    };
}
