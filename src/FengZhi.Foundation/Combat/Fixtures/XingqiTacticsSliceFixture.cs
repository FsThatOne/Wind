using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Combat.AI;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Combat.Xingqi;

namespace FengZhi.Foundation.Combat.Fixtures;

/// <summary>
/// 行气战棋可玩薄片 Fixture。5x5 棋盘，主角 1 人对江湖小贼 1 人。
/// </summary>
public static class XingqiTacticsSliceFixture
{
    public const string ProtagonistId = "player_protagonist";
    public const string BanditId = "enemy_demo_bandit";

    public static BattleGrid CreateGrid() => new(5, 5);

    public static BattleConfig CreateBattleConfig() => new()
    {
        BattleType = "tactics_slice",
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
        AttackGang = 18,
        AttackRou = 22,
        AttackQiao = 24,
        Defense = 6,
        Speed = 8,
        Agility = 12,
        CritRate = 0.05f,
        InsightStat = 8,
        NeixiRecovery = 3,
        StaggerThreshold = 5,
        InitialPosition = new GridPosition(1, 2),
        MoveRange = 3,
        InitialFacing = Iso4Direction.NE,
        EquippedMoveIds = new[] { "fengzhi_ruler_probe", "fengzhi_ruler_sweep" },
    };

    public static CombatantConfig CreateBanditConfig() => new()
    {
        Id = BanditId,
        Name = "江湖小贼",
        MaxHP = 60,
        MaxNeixi = 25,
        AttackGang = 20,
        AttackRou = 8,
        AttackQiao = 10,
        Defense = 5,
        Speed = 6,
        Agility = 8,
        CritRate = 0.03f,
        InsightStat = 4,
        NeixiRecovery = 2,
        StaggerThreshold = 5,
        InitialPosition = new GridPosition(3, 2),
        MoveRange = 2,
        InitialFacing = Iso4Direction.SW,
        EquippedMoveIds = new[] { "luo_han_quan", "tie_bi_heng_lan" },
    };

    public static ChasingAI CreateBanditAI(int maxTurns = 20)
    {
        var script = new List<BattleAction>();
        for (int i = 0; i < maxTurns; i++)
        {
            bool useHeavy = i % 2 == 1;
            script.Add(new BattleAction
            {
                ActorId = BanditId,
                Type = ActionType.Move,
                TargetId = ProtagonistId,
                MoveId = useHeavy ? "tie_bi_heng_lan" : "luo_han_quan",
                MoveType = CharacterData.MoveType.Gang,
                NeixiCost = useHeavy ? 4 : 2,
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
