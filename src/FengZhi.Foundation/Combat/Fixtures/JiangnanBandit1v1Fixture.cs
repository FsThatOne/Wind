using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.Fixtures;

/// <summary>
/// Sprint 7 VS 江南小贼 1v1 战斗 fixture（MVP-A 范围）。
///
/// 数据来源与对齐：
/// - 主角 base stats 对齐 `assets/data/characters/player.yaml`（base_hp=100, base_neixi=20）
/// - 招式 id 对齐 `assets/data/martial-arts/moves.yaml`（luo_han_quan + tie_bi_heng_lan）
/// - 江湖小贼 personality: 刚劲外露（spike §4.1）→ 与主角同体系，AI 优先刚招
///
/// 详见 `docs/superpowers/specs/2026-06-23-s7-vs-combat-loop-mvp-a.md` §3。
///
/// 注意：本 fixture 是 **C# 静态常量**（spec §10 Decision B1），不读 yaml；
/// 与 yaml 数据出现偏差时优先以本文件为准（VS 范围内的固化 fixture）。
/// </summary>
public static class JiangnanBandit1v1Fixture
{
    public const string ProtagonistId = "player_protagonist";
    public const string BanditId = "enemy_jiangnan_bandit";

    public const string LightStrikeMoveId = "luo_han_quan";    // 罗汉拳 — 轻击
    public const string HeavyStrikeMoveId = "tie_bi_heng_lan"; // 铁臂横拦 — 重击

    /// <summary>
    /// 构建 MVP-A 战斗配置。
    /// </summary>
    public static BattleConfig CreateBattleConfig() => new()
    {
        BattleType = "vs_jiangnan_lite",
        PlayerParty = new[] { CreateProtagonistConfig() },
        EnemyGroup = new[] { CreateBanditConfig() },
        MaxRounds = 8,
    };

    /// <summary>
    /// 主角配置。
    ///
    /// VS-only 战斗数值（不直接读 player.yaml）：
    /// - ResolutionService 当前硬编码 BaseMultiplier=1.0（不读 moves.yaml mult），
    ///   所以伤害公式 = max(1, attack - defense) × counter × crit。
    /// - 为让 1v1 在 2-5 回合内分胜负（spec §3 设计），把 Attack 提高到 25 以脱离
    ///   "防御吃满后 1 伤害" 的下限区。这是 VS 范围内的固化数值，不污染主线 player.yaml。
    /// </summary>
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
        CritRate = 0.05f,
        InsightStat = 8,
        NeixiRecovery = 3,
        StaggerThreshold = 5,
        EquippedMoveIds = new[] { LightStrikeMoveId, HeavyStrikeMoveId },
    };

    /// <summary>
    /// 江湖小贼配置（刚体系 personality "刚劲外露"，比主角弱以确保主角能赢）。
    /// </summary>
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
        CritRate = 0.03f,
        InsightStat = 4,
        NeixiRecovery = 2,
        StaggerThreshold = 5,
        EquippedMoveIds = new[] { LightStrikeMoveId, HeavyStrikeMoveId },
    };

    /// <summary>
    /// 构建小贼 ScriptedAI（轮流轻重攻击，最多 MaxRounds 次）。
    /// 内息不足时退化为 BasicAttack（ScriptedAI 兜底）。
    /// </summary>
    public static ScriptedAI CreateBanditAI(int rounds = 8)
    {
        var script = new List<BattleAction>();
        for (int round = 0; round < rounds; round++)
        {
            bool useHeavy = round % 2 == 1;
            script.Add(new BattleAction
            {
                ActorId = BanditId,
                Type = ActionType.Move,
                TargetId = ProtagonistId,
                MoveId = useHeavy ? HeavyStrikeMoveId : LightStrikeMoveId,
                MoveType = MoveType.Gang,
                NeixiCost = useHeavy ? 4 : 2,
            });
        }
        return new ScriptedAI(script);
    }

    /// <summary>
    /// 主角 ScriptedAI（自动化测试用，模拟玩家也轮流轻重攻击）。
    /// 实际 VS 场景下由 player input 替代此队列。
    /// </summary>
    public static IReadOnlyList<BattleAction> CreateProtagonistAutoScript(int rounds = 8)
    {
        var script = new List<BattleAction>();
        for (int round = 0; round < rounds; round++)
        {
            bool useHeavy = round % 2 == 1;
            script.Add(new BattleAction
            {
                ActorId = ProtagonistId,
                Type = ActionType.Move,
                TargetId = BanditId,
                MoveId = useHeavy ? HeavyStrikeMoveId : LightStrikeMoveId,
                MoveType = MoveType.Gang,
                NeixiCost = useHeavy ? 4 : 2,
            });
        }
        return script;
    }
}
