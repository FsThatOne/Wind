// VERTICAL SLICE - Boss Battle Prototype
// Bridges Foundation EnemyBrain AI into playable Godot combat
using System.Collections.Generic;

namespace FengzhiSlice;

/// <summary>
/// Boss character data and move sets for the vertical slice.
/// </summary>
public static class BossData
{
    public static CharacterData CreateBoss()
    {
        return new CharacterData
        {
            Name = "铁冠道人",
            Title = "玄霜峰主",
            MaxHp = 200,
            CurrentHp = 200,
            MaxNeili = 8,
            CurrentNeili = 8,
            StaggerCap = 5,
            GongliRank = 5,
            GongliTitle = "内力深厚"
        };
    }

    public static CharacterData CreateBossPlayer()
    {
        return new CharacterData
        {
            Name = "无名",
            Title = "风止山庄末代弟子",
            MaxHp = 150,
            CurrentHp = 150,
            MaxNeili = 6,
            CurrentNeili = 6,
            StaggerCap = 4,
            GongliRank = 3,
            GongliTitle = "登堂入室"
        };
    }

    /// <summary>
    /// Player moves for Boss fight (slightly stronger than bandit fight).
    /// </summary>
    public static MartialMove[] GetPlayerBossFightMoves() => new[]
    {
        new MartialMove
        {
            Id = "fz_zhengfeng", Name = "正锋", Description = "尺中藏剑意，正面破敌",
            Type = MoveType.Gang, Category = MoveCategory.Attack,
            BaseDamage = 22, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 0
        },
        new MartialMove
        {
            Id = "fz_huiwan", Name = "回腕", Description = "尺尾旋转，以柔克刚",
            Type = MoveType.Rou, Category = MoveCategory.Defend,
            BaseDamage = 8, NeiliCost = 0, StaggerValue = 0, GrantsNeili = true, CooldownTurns = 0
        },
        new MartialMove
        {
            Id = "fz_cunjin", Name = "寸劲", Description = "贴身短打，巧妙寻隙",
            Type = MoveType.Qiao, Category = MoveCategory.Attack,
            BaseDamage = 18, NeiliCost = 1, StaggerValue = 2, CooldownTurns = 1
        },
        new MartialMove
        {
            Id = "fz_pojun", Name = "破军", Description = "全力一击，尺破苍穹",
            Type = MoveType.Gang, Category = MoveCategory.Special,
            BaseDamage = 40, NeiliCost = 3, StaggerValue = 0, CooldownTurns = 2
        },
        new MartialMove
        {
            Id = "fz_liuyun", Name = "流云", Description = "如云流转，四两拨千斤",
            Type = MoveType.Rou, Category = MoveCategory.Special,
            BaseDamage = 12, NeiliCost = 2, StaggerValue = 0, GrantsNeili = true, CooldownTurns = 2
        },
        new MartialMove
        {
            Id = "fz_dianxue", Name = "点穴", Description = "精准一指，封住经脉",
            Type = MoveType.Qiao, Category = MoveCategory.Special,
            BaseDamage = 30, NeiliCost = 2, StaggerValue = 3, CooldownTurns = 2
        }
    };

    /// <summary>
    /// Boss moves mapped by MoveType. AI selects type, then we pick a move.
    /// </summary>
    public static Dictionary<MoveType, MartialMove[]> GetBossMovesByType() => new()
    {
        [MoveType.Gang] = new[]
        {
            new MartialMove
            {
                Id = "boss_tiejin", Name = "铁襟式", Description = "浑厚内力凝于拳锋",
                Type = MoveType.Gang, Category = MoveCategory.Attack,
                BaseDamage = 28, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 0
            },
            new MartialMove
            {
                Id = "boss_kuanglei", Name = "狂雷击", Description = "势如雷霆，不可阻挡",
                Type = MoveType.Gang, Category = MoveCategory.Special,
                BaseDamage = 45, NeiliCost = 3, StaggerValue = 2, CooldownTurns = 2
            }
        },
        [MoveType.Rou] = new[]
        {
            new MartialMove
            {
                Id = "boss_xuanshuang", Name = "玄霜护体", Description = "寒气护身，削弱来敌",
                Type = MoveType.Rou, Category = MoveCategory.Defend,
                BaseDamage = 10, NeiliCost = 0, StaggerValue = 0, GrantsNeili = true, CooldownTurns = 0
            },
            new MartialMove
            {
                Id = "boss_hanbingzhang", Name = "寒冰掌", Description = "至寒之力灌注掌心",
                Type = MoveType.Rou, Category = MoveCategory.Attack,
                BaseDamage = 20, NeiliCost = 2, StaggerValue = 1, CooldownTurns = 1
            }
        },
        [MoveType.Qiao] = new[]
        {
            new MartialMove
            {
                Id = "boss_lingxu", Name = "凌虚步", Description = "身形飘渺，伺机而动",
                Type = MoveType.Qiao, Category = MoveCategory.Attack,
                BaseDamage = 22, NeiliCost = 1, StaggerValue = 1, CooldownTurns = 0
            },
            new MartialMove
            {
                Id = "boss_fengxue", Name = "封穴指", Description = "直取要害，一指封经",
                Type = MoveType.Qiao, Category = MoveCategory.Special,
                BaseDamage = 35, NeiliCost = 2, StaggerValue = 2, CooldownTurns = 2
            }
        }
    };
}
