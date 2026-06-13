using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Foundation.Combat;

/// <summary>
/// cb-010: 战斗入口与配置接口集成测试。
/// </summary>
public class BattleEntryConfigTest
{
    private static readonly FixedDamageRandom _fixedRandom = new() { Variance = 1.0f, CritRoll = 1.0f };

    private static CombatantConfig MakePlayerConfig(string id, int attack = 30, int defense = 15)
        => new()
        {
            Id = id,
            Name = $"Player_{id}",
            MaxHP = 100,
            MaxNeixi = 30,
            AttackGang = attack,
            AttackRou = attack,
            AttackQiao = attack,
            Defense = defense,
            Speed = 10,
            CritRate = 0.1f,
            InsightStat = 5,
            NeixiRecovery = 3,
            StaggerThreshold = 5,
            EquippedMoveIds = new[] { "move_a", "move_b" }
        };

    private static CombatantConfig MakeEnemyConfig(string id, int hp = 50, int attack = 20, int defense = 10)
        => new()
        {
            Id = id,
            Name = $"Enemy_{id}",
            MaxHP = hp,
            MaxNeixi = 20,
            AttackGang = attack,
            AttackRou = attack,
            AttackQiao = attack,
            Defense = defense,
            Speed = 8,
            CritRate = 0.05f,
            InsightStat = 3,
            NeixiRecovery = 2,
            StaggerThreshold = 5,
            EquippedMoveIds = new[] { "enemy_move" }
        };

    // --- AC1: BattleConfig 包含 GDD 定义的所有字段 ---

    [Fact]
    public void BattleConfig_ContainsAllRequiredFields()
    {
        var config = new BattleConfig
        {
            BattleType = "boss",
            PlayerParty = new[] { MakePlayerConfig("p1") },
            EnemyGroup = new[] { MakeEnemyConfig("e1") },
            MaxRounds = 10
        };

        Assert.Equal("boss", config.BattleType);
        Assert.Single(config.PlayerParty);
        Assert.Single(config.EnemyGroup);
        Assert.Equal(10, config.MaxRounds);
    }

    [Fact]
    public void CombatantConfig_ContainsEquippedMoves()
    {
        var cfg = MakePlayerConfig("p1");
        Assert.Equal(2, cfg.EquippedMoveIds.Count);
        Assert.Equal("move_a", cfg.EquippedMoveIds[0]);
    }

    // --- AC2: InitiateBattle 正确初始化所有参战角色 ---

    [Fact]
    public void InitiateBattle_CreatesCorrectCombatants()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1"), MakePlayerConfig("p2") },
            EnemyGroup = new[] { MakeEnemyConfig("e1"), MakeEnemyConfig("e2") }
        };

        var battle = facade.InitiateBattle(config);

        Assert.Equal(2, battle.PlayerParty.Count);
        Assert.Equal(2, battle.EnemyGroup.Count);
        Assert.Equal("p1", battle.PlayerParty[0].Id);
        Assert.Equal("Player_p1", battle.PlayerParty[0].Name);
        Assert.Equal(100, battle.PlayerParty[0].MaxHP);
        Assert.Equal(100, battle.PlayerParty[0].HP);
        Assert.Equal(30, battle.PlayerParty[0].MaxNeixi);
        Assert.Equal(30, battle.PlayerParty[0].Neixi);
        Assert.Equal(0, battle.PlayerParty[0].Stagger);
        Assert.Equal(BattlePhase.Initializing, battle.CurrentPhase);
    }

    [Fact]
    public void InitiateBattle_EnemyStatsMatchConfig()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1") },
            EnemyGroup = new[] { MakeEnemyConfig("e1", hp: 80, attack: 25, defense: 12) }
        };

        var battle = facade.InitiateBattle(config);

        var enemy = battle.EnemyGroup[0];
        Assert.Equal(80, enemy.MaxHP);
        Assert.Equal(25, enemy.AttackGang);
        Assert.Equal(12, enemy.Defense);
    }

    // --- AC3: 完整 3 回合战斗可从创建到结束走通 ---

    [Fact]
    public void RunFullBattle_ThreeRounds_CompletesSuccessfully()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1", attack: 40, defense: 20) },
            EnemyGroup = new[] { MakeEnemyConfig("e1", hp: 30, attack: 10, defense: 5) },
            MaxRounds = 15
        };

        // Player 连续用刚系攻击（每回合一个 action，因为一个 player 活着）
        var decisions = new List<BattleAction>
        {
            new() { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" },
            new() { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" },
            new() { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" }
        };

        var enemyAI = new ScriptedAI(new[]
        {
            new BattleAction { ActorId = "e1", Type = ActionType.Breathe },
            new BattleAction { ActorId = "e1", Type = ActionType.Breathe },
            new BattleAction { ActorId = "e1", Type = ActionType.Breathe }
        });

        var stats = facade.RunFullBattle(config, decisions, enemyAI);

        // BasicAttack = max_attack × 0.3 = 40 × 0.3 = 12, enemy defense = 5
        // 实际：直接用 ActionExecutor.ExecuteBasicAttack → max(1, round(40×0.3)) = 12 每次
        // 3 次 = 36 > 30 HP → 应该在第3回合杀死敌人
        Assert.Equal(BattleResult.Victory, stats.Result);
        Assert.True(stats.TotalRounds <= 3);
        Assert.True(stats.TotalDamageDealt > 0);
    }

    // --- AC4: 战斗结果正确返回 Victory/Defeat/Draw/NearDefeat ---

    [Fact]
    public void RunFullBattle_PlayerLoses_ReturnsDefeat()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1", attack: 5, defense: 5) },
            EnemyGroup = new[] { MakeEnemyConfig("e1", hp: 200, attack: 60, defense: 50) },
            MaxRounds = 15
        };

        // Player does nothing useful
        var decisions = Enumerable.Range(0, 15).Select(_ =>
            new BattleAction { ActorId = "p1", Type = ActionType.Breathe }).ToList();

        // Enemy repeatedly attacks
        var enemyActions = Enumerable.Range(0, 15).Select(_ =>
            new BattleAction { ActorId = "e1", Type = ActionType.BasicAttack, TargetId = "p1" });
        var enemyAI = new ScriptedAI(enemyActions);

        var stats = facade.RunFullBattle(config, decisions, enemyAI);

        Assert.Equal(BattleResult.Defeat, stats.Result);
        Assert.True(stats.TotalDamageReceived > 0);
    }

    [Fact]
    public void RunFullBattle_MaxRoundsReached_ReturnsDraw()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1", attack: 1, defense: 100) },
            EnemyGroup = new[] { MakeEnemyConfig("e1", hp: 9999, attack: 1, defense: 100) },
            MaxRounds = 3
        };

        // Both do nothing useful, battle runs to max rounds
        var decisions = Enumerable.Range(0, 3).Select(_ =>
            new BattleAction { ActorId = "p1", Type = ActionType.Breathe }).ToList();
        var enemyAI = new ScriptedAI(Enumerable.Range(0, 3).Select(_ =>
            new BattleAction { ActorId = "e1", Type = ActionType.Breathe }));

        var stats = facade.RunFullBattle(config, decisions, enemyAI);

        Assert.Equal(BattleResult.Draw, stats.Result);
        Assert.Equal(3, stats.TotalRounds);
    }

    // --- AC5: 从 CharacterLoadout 读取装备招式并用于战斗 ---

    [Fact]
    public void CombatantConfig_EquippedMoveIds_Preserved()
    {
        var config = new CombatantConfig
        {
            Id = "hero",
            Name = "主角",
            MaxHP = 100,
            MaxNeixi = 50,
            AttackGang = 40,
            AttackRou = 35,
            AttackQiao = 30,
            Defense = 20,
            Speed = 12,
            CritRate = 0.15f,
            InsightStat = 8,
            NeixiRecovery = 5,
            StaggerThreshold = 5,
            EquippedMoveIds = new[] { "broken_sword_1", "iron_palm_2", "cloud_step_3", "wind_blade_4", "river_flow_5", "thunder_fist_6" }
        };

        Assert.Equal(6, config.EquippedMoveIds.Count);
        Assert.Equal("broken_sword_1", config.EquippedMoveIds[0]);
        Assert.Equal("thunder_fist_6", config.EquippedMoveIds[5]);
    }

    // --- 补充：ScriptedAI 兜底行为 ---

    [Fact]
    public void ScriptedAI_FallsBackToBreathe_WhenScriptExhausted()
    {
        var ai = new ScriptedAI(new[]
        {
            new BattleAction { ActorId = "e1", Type = ActionType.BasicAttack, TargetId = "p1" }
        });

        var dummy = new BattleCombatant("e1", "enemy", 50, 20, 20, 20, 20, 10, 8, 0.05f, 3, 2, 5);
        var battle = new BattleInstance(
            new[] { new BattleCombatant("p1", "p", 100, 30, 30, 30, 30, 15, 10, 0.1f, 5, 3, 5) },
            new[] { dummy });

        var first = ai.DecideAction(dummy, battle);
        Assert.Equal(ActionType.BasicAttack, first.Type);

        var second = ai.DecideAction(dummy, battle);
        Assert.Equal(ActionType.Breathe, second.Type);
    }

    // --- BattleStats 属性验证 ---

    [Fact]
    public void BattleStats_TracksFullBattle()
    {
        var facade = new BattleFacade(_fixedRandom);
        var config = new BattleConfig
        {
            PlayerParty = new[] { MakePlayerConfig("p1", attack: 30, defense: 20) },
            EnemyGroup = new[] { MakeEnemyConfig("e1", hp: 60, attack: 10, defense: 5) },
            MaxRounds = 10
        };

        // BasicAttack = round(30 × 0.3) = 9 damage per hit, 7 hits = 63 > 60
        var decisions = new List<BattleAction>();
        for (int i = 0; i < 10; i++)
            decisions.Add(new BattleAction { ActorId = "p1", Type = ActionType.BasicAttack, TargetId = "e1" });

        var enemyAI = new ScriptedAI(Enumerable.Range(0, 10).Select(_ =>
            new BattleAction { ActorId = "e1", Type = ActionType.Breathe }));

        var stats = facade.RunFullBattle(config, decisions, enemyAI);

        Assert.Equal(BattleResult.Victory, stats.Result);
        Assert.True(stats.TotalRounds <= 7);
        Assert.True(stats.TotalDamageDealt >= 60);
    }
}
