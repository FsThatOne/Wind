using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Combat.AI;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Combat.Xingqi;

namespace FengZhi.Foundation.Combat.Fixtures;

/// <summary>
/// 战棋移动 Demo Fixture。8x8 棋盘 + 2 障碍，玩家(1,1) vs 敌人(6,6)。
/// </summary>
public static class MovementDemoFixture
{
    public const string ProtagonistId = "player_protagonist";
    public const string BanditId = "enemy_bandit";

    public static BattleGrid CreateDemoGrid() => new(8, 8, obstacles: new[]
    {
        new GridPosition(3, 3),
        new GridPosition(4, 5),
    });

    public static BattleConfig CreateBattleConfig() => new()
    {
        BattleType = "movement_demo",
        PlayerParty = new[] { CreateProtagonistConfig() },
        EnemyGroup = new[] { CreateBanditConfig() },
        MaxRounds = 99,
    };

    public static CombatantConfig CreateProtagonistConfig() => new()
    {
        Id = ProtagonistId,
        Name = "风止·主角",
        MaxHP = 80,
        MaxNeixi = 40,
        AttackGang = 25,
        AttackRou = 12,
        AttackQiao = 12,
        Defense = 5,
        Speed = 8,
        Agility = 12,
        CritRate = 0.05f,
        InsightStat = 8,
        NeixiRecovery = 3,
        StaggerThreshold = 5,
        InitialPosition = new GridPosition(1, 1),
        MoveRange = 3,
        InitialFacing = Iso4Direction.SE,
        EquippedMoveIds = new[] { "luo_han_quan", "tie_bi_heng_lan" },
    };

    public static CombatantConfig CreateBanditConfig() => new()
    {
        Id = BanditId,
        Name = "江湖小贼",
        MaxHP = 60,
        MaxNeixi = 25,
        AttackGang = 20,
        AttackRou = 6,
        AttackQiao = 6,
        Defense = 5,
        Speed = 6,
        Agility = 8,
        CritRate = 0.03f,
        InsightStat = 4,
        NeixiRecovery = 2,
        StaggerThreshold = 5,
        InitialPosition = new GridPosition(6, 6),
        MoveRange = 2,
        InitialFacing = Iso4Direction.NW,
        EquippedMoveIds = new[] { "luo_han_quan", "tie_bi_heng_lan" },
    };

    public static ChasingAI CreateBanditAI(int maxTurns = 20)
    {
        var script = new List<BattleAction>();
        for (int i = 0; i < maxTurns; i++)
        {
            script.Add(new BattleAction
            {
                ActorId = BanditId,
                Type = ActionType.Move,
                TargetId = ProtagonistId,
                MoveId = "luo_han_quan",
                MoveType = CharacterData.MoveType.Gang,
                NeixiCost = 2,
            });
        }
        return new ChasingAI(ProtagonistId, script);
    }

    public static XingqiConfig CreateXingqiConfig() => new()
    {
        BaseXingqiGain = 20,
        XingqiThreshold = 100,
        ChapterBaselineAgility = 10,
    };
}
