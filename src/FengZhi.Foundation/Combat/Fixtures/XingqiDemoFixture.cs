using FengZhi.Foundation.Combat.Xingqi;

namespace FengZhi.Foundation.Combat.Fixtures;

/// <summary>
/// 行气系统 Demo Fixture。1v1 配置，玩家 Agility=12 vs 敌人 Agility=8 → 玩家先手。
/// </summary>
public static class XingqiDemoFixture
{
    public const string ProtagonistId = "player_protagonist";
    public const string BanditId = "enemy_jiangnan_bandit";

    public static BattleConfig CreateXingqiBattleConfig() => new()
    {
        BattleType = "xingqi_demo",
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
        EquippedMoveIds = new[] { "luo_han_quan", "tie_bi_heng_lan" },
    };

    public static ScriptedAI CreateBanditAI(int maxTurns = 20)
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
        return new ScriptedAI(script);
    }

    public static XingqiConfig CreateXingqiConfig() => new()
    {
        BaseXingqiGain = 20,
        XingqiThreshold = 100,
        ChapterBaselineAgility = 10,
    };
}
