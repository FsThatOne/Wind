using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;

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

    // ---------- cu-004-vs-integration demo seed (harness spec subtask A) ----------
    //
    // cu-004 demo 目的: 展示招式选择面板 8 行（6 装备 + 调息 + 使用道具），覆盖：
    //   - 体系角标 (Gang/Rou/Qiao 3 种)
    //   - 心法专属角标
    //   - 置灰原因 (内息不足 / 心法封印 / 无道具)
    //   - 预览卡 3 种关系 (克制/中性/被克)
    //
    // 展示与结算解耦（owner 2026-06-24 Q2=a 决策）：
    //   - 4 个 demo_* 招式仅作 UI 呈现，提交时由 JiangnanBattleGame 翻译回 luo_han_quan/tie_bi_heng_lan
    //   - 心法专属招式恒置灰（IsXinfaSealed=true），不会被 ConfirmSelected 选中
    //   - cu-004 录屏只验 UI 状态，不验结算精确性

    /// <summary>cu-004 demo move id：柔系展示招（克制敌人 Gang）。</summary>
    public const string Cu004DemoRouMoveId = "demo_rou_yun_palm";

    /// <summary>cu-004 demo move id：巧系展示招（被敌人 Gang 克制）。</summary>
    public const string Cu004DemoQiaoMoveId = "demo_qiao_yun_step";

    /// <summary>cu-004 demo move id：高内息消耗招式（恒置灰：差 N 内息）。</summary>
    public const string Cu004DemoHighCostMoveId = "demo_qian_ye_palm";

    /// <summary>cu-004 demo move id：心法专属招式（IsXinfaSealed=true 时置灰）。</summary>
    public const string Cu004DemoXinfaMoveId = "demo_ming_jing_zhi_shui";

    /// <summary>
    /// cu-004 demo seed 完整契约：战斗 config + 招式面板 demo 数据 + 面板 context 字段。
    /// </summary>
    /// <param name="BattleConfig">实际战斗 config（复用 CreateBattleConfig，结算走 2 真招）。</param>
    /// <param name="PanelDisplay">招式面板展示数据：6 招（2 真 + 3 demo + 1 心法专属）。</param>
    /// <param name="IsXinfaSealed">面板 context: 心法封印（demo 恒 true，让心法专属置灰）。</param>
    /// <param name="UsableCombatItemCount">面板 context: 可用战斗道具（demo 恒 0，让"使用道具"置灰）。</param>
    public sealed record Cu004DemoSeed(
        BattleConfig BattleConfig,
        BattlePanelDisplayData PanelDisplay,
        bool IsXinfaSealed,
        int UsableCombatItemCount);

    /// <summary>
    /// 构建 cu-004 demo seed。面板覆盖 6 装备槽 + 心法专属 + 置灰原因 + 体系关系预览。
    /// </summary>
    public static Cu004DemoSeed CreateDemoConfig_Cu004Showcase() => new(
        BattleConfig: CreateBattleConfig(),
        PanelDisplay: BuildCu004PanelDisplay(),
        IsXinfaSealed: true,
        UsableCombatItemCount: 0);

    // ---------- cu-005-vs-integration demo seed (harness spec subtask B) ----------
    //
    // cu-005 demo 目的: 在 cu-004 面板基础上覆盖反制 + 决胜一击提示：
    //   - 敌方公开柔意图（玩家 Gang 招式 = 克制 → 反制标签可见）
    //   - 内息 ≥3 → 反制标签金色启用 / <3 → 灰色置灰 + "内息不足"
    //   - 敌方破绽 ≥5 → 顶部插入"决胜一击"高亮行
    //   - 提交反制 → BattleAction.Type = Counter；提交决胜 → BattleAction.Type = Decisive
    //
    // 与 cu-004 demo seed 差异（与 cu-004 共用 6 招 panel display）：
    //   - PreloadedDecisiveTargetIds = [BanditId]（demo 直接 UI 注入决胜目标，不依赖真破绽 ≥5；
    //     owner Q3=a 决策一致：纯 UI demo 字段不连战斗结算）
    //   - InitialPlayerNeixi=3（反制启用边界；录屏者可用 hotkey 切到 <3 演置灰）
    //   - 敌方 ScriptedAI commit Rou 意图（玩家 Gang 招式 = 克制 → 反制标签可见）

    /// <summary>
    /// cu-005 demo seed 完整契约。继承 cu-004 panel 数据（保持 6 招覆盖），
    /// 额外预置决胜目标 + 初始内息让反制/决胜立刻可演示。
    /// </summary>
    /// <param name="BattleConfig">战斗 config（复用 cu-004，结算走 2 真招）。</param>
    /// <param name="PanelDisplay">招式面板展示数据（复用 cu-004 6 招）。</param>
    /// <param name="IsXinfaSealed">心法封印（demo 恒 true）。</param>
    /// <param name="UsableCombatItemCount">可用道具（demo 恒 0）。</param>
    /// <param name="PreloadedDecisiveTargetIds">demo 启动时直接注入 UI 决胜目标 list。</param>
    /// <param name="InitialPlayerNeixi">demo 启动时玩家 UI 内息显示值（≥3 反制启用 / &lt;3 反制置灰）。</param>
    public sealed record Cu005DemoSeed(
        BattleConfig BattleConfig,
        BattlePanelDisplayData PanelDisplay,
        bool IsXinfaSealed,
        int UsableCombatItemCount,
        IReadOnlyList<string> PreloadedDecisiveTargetIds,
        int InitialPlayerNeixi);

    /// <summary>
    /// 构建 cu-005 demo seed。
    /// </summary>
    public static Cu005DemoSeed CreateDemoConfig_Cu005Showcase() => new(
        BattleConfig: CreateBattleConfig(),
        PanelDisplay: BuildCu004PanelDisplay(),
        IsXinfaSealed: true,
        UsableCombatItemCount: 0,
        PreloadedDecisiveTargetIds: new[] { BanditId },
        InitialPlayerNeixi: 3); // 反制启用边界，录屏者用 hotkey 切到 <3 演置灰

    // ---------- cu-006-vs-integration demo seed (harness spec subtask C) ----------
    //
    // cu-006 demo 目的：在 battle scene 接入 Foundation DecisiveStrikeDirector 完整 7-phase 演出。
    //   - 敌方破绽 = StaggerThreshold(5) 直接可触发决胜 → 玩家第一回合按 Enter 即可触发
    //   - 玩家初始 Neixi >= 3 走决胜 (Foundation Sequence 内部不扣 Neixi, 但 UI panel 显示要求)
    //   - PanelDisplay 复用 cu-004 face card 让玩家有"决胜一击"行可选 (cu-005 已落 panel 顶部决胜行)
    //   - EnemyInitialStaggerOverride = 5 由 JiangnanBattleGame._Ready 在 facade.InitiateBattle 之后调
    //     bandit.AddStagger(5) 直接预置
    //   - 演出触发链路: 玩家选决胜行 → BattleAction.ActionType=Decisive → ResolutionService.ExecuteDecisive
    //     → DamageDealtEvent(VisualRelation=Decisive) → JiangnanBattleGame.OnDamageDealtForDecisive
    //     → adapter.RequestDecisive → 7-phase
    //   - 演出期 Engine.TimeScale = 0.2; Camera 推/锁/恢复; 输入屏蔽 (whitelist ui_pause/ui_system_back)
    //   - Phase5 显示 max-size deep-gold 伤害数字 (deep-gold 0.95,0.78,0.20)
    //
    // 与 cu-005 共用: 决胜目标预置 + 内息覆盖
    // 与 cu-005 差异: 决胜触发后演 7-phase, 不需要 hotkey [/] 调内息

    /// <summary>
    /// cu-006 demo seed: cu-004 panel + 敌方破绽预置到阈值（直接触发决胜）。
    /// </summary>
    public sealed record Cu006DemoSeed(
        BattleConfig BattleConfig,
        BattlePanelDisplayData PanelDisplay,
        bool IsXinfaSealed,
        int UsableCombatItemCount,
        IReadOnlyList<string> PreloadedDecisiveTargetIds,
        int InitialPlayerNeixi,
        int EnemyInitialStaggerOverride);

    /// <summary>
    /// 构建 cu-006 demo seed。
    /// EnemyInitialStaggerOverride=5 = bandit.StaggerThreshold，让第一回合就能演决胜。
    /// </summary>
    public static Cu006DemoSeed CreateDemoConfig_Cu006Showcase() => new(
        BattleConfig: CreateBattleConfig(),
        PanelDisplay: BuildCu004PanelDisplay(),
        IsXinfaSealed: true,
        UsableCombatItemCount: 0,
        PreloadedDecisiveTargetIds: new[] { BanditId },
        InitialPlayerNeixi: 8, // 8 内息保证决胜内息成本 + 备选招式都可见
        EnemyInitialStaggerOverride: 5);

    // ---------- cu-008-vs-integration demo seed (harness spec subtask D) ----------
    //
    // cu-008 demo 目的: 验证招式选择面板的 dual-focus + D-pad/方向键循环导航：
    //   - 默认聚焦第一个可用招式
    //   - D-pad/方向键 上/下循环穿过 6 装备 + 调息 + 使用道具 (置灰行也参与循环？否)
    //   - 决胜行（如有）可被手柄触达
    //   - 鼠标 hover + 手柄 focus 视觉态独立（cu-005 已落 Slot hover/focus 双背景层）
    //   - 输入模式切换（鼠标 ↔ 键盘 ↔ 手柄）不丢 focus
    //
    // 与 cu-004 共用: panel display 6 招 + 心法封印 + 无道具 + 决胜目标预置（同 cu-005）
    // 与 cu-004/005 差异: PrefersGamepadOnlyHint=true 告知录屏者建议拔鼠标 / 用方向键模拟 D-pad

    /// <summary>
    /// cu-008 demo seed: 与 cu-004 panel 一致 + 决胜目标预置 + gamepad-only 录制提示。
    /// </summary>
    public sealed record Cu008DemoSeed(
        BattleConfig BattleConfig,
        BattlePanelDisplayData PanelDisplay,
        bool IsXinfaSealed,
        int UsableCombatItemCount,
        IReadOnlyList<string> PreloadedDecisiveTargetIds,
        int InitialPlayerNeixi,
        bool PrefersGamepadOnlyHint);

    /// <summary>
    /// 构建 cu-008 demo seed。复用 cu-004 panel display + cu-005 决胜目标预置。
    /// </summary>
    public static Cu008DemoSeed CreateDemoConfig_Cu008Showcase() => new(
        BattleConfig: CreateBattleConfig(),
        PanelDisplay: BuildCu004PanelDisplay(),
        IsXinfaSealed: true,
        UsableCombatItemCount: 0,
        PreloadedDecisiveTargetIds: new[] { BanditId },
        InitialPlayerNeixi: 8, // 8 内息让 demo 招式大都可用，循环穿过更多行
        PrefersGamepadOnlyHint: true);

    /// <summary>
    /// cu-005 demo 小贼 ScriptedAI：所有回合 commit Rou 意图（让玩家 Gang 招式始终克制）。
    /// </summary>
    public static ScriptedAI CreateBanditAICu005(int rounds = 8)
    {
        var script = new List<BattleAction>();
        for (int round = 0; round < rounds; round++)
        {
            // demo: 敌方 commit Rou 意图（与 protagonist Gang 招式互克）
            // moveId 沿用 LightStrike 让 ResolutionService 能查表，但 MoveType=Rou 让 IntentRevealed 公开柔
            script.Add(new BattleAction
            {
                ActorId = BanditId,
                Type = ActionType.Move,
                TargetId = ProtagonistId,
                MoveId = LightStrikeMoveId,
                MoveType = MoveType.Rou,
                NeixiCost = 2,
            });
        }
        return new ScriptedAI(script);
    }

    private static BattlePanelDisplayData BuildCu004PanelDisplay() => new()
    {
        Entries = new[]
        {
            new BattlePanelMoveEntry
            {
                MoveId = LightStrikeMoveId,
                Name = "罗汉拳 · 轻击",
                Source = MoveSource.BaseSlot,
                ColorTheme = TypeColorTheme.WarmGold,
                NeixiCost = 2,
                EffectiveMultiplier = 1.0f,
                TriggerConditions = new[] { "always" },
                SpecialEffects = new[] { "稳定输出" },
            },
            new BattlePanelMoveEntry
            {
                MoveId = HeavyStrikeMoveId,
                Name = "铁臂横拦 · 重击",
                Source = MoveSource.BaseSlot,
                ColorTheme = TypeColorTheme.WarmGold,
                NeixiCost = 4,
                EffectiveMultiplier = 1.4f,
                TriggerConditions = new[] { "always" },
                SpecialEffects = new[] { "破绽 +1" },
            },
            new BattlePanelMoveEntry
            {
                MoveId = Cu004DemoRouMoveId,
                Name = "柔云掌 · 引力",
                Source = MoveSource.BaseSlot,
                ColorTheme = TypeColorTheme.CoolCyan,
                NeixiCost = 3,
                EffectiveMultiplier = 1.1f,
                TriggerConditions = new[] { "after_intent_revealed" },
                SpecialEffects = new[] { "化刚为柔" },
            },
            new BattlePanelMoveEntry
            {
                MoveId = Cu004DemoQiaoMoveId,
                Name = "巧云步 · 错身",
                Source = MoveSource.BaseSlot,
                ColorTheme = TypeColorTheme.NeutralGray,
                NeixiCost = 3,
                EffectiveMultiplier = 1.0f,
                TriggerConditions = new[] { "always" },
                SpecialEffects = new[] { "走位 +1" },
            },
            new BattlePanelMoveEntry
            {
                MoveId = Cu004DemoHighCostMoveId,
                Name = "千叶千手",
                Source = MoveSource.BaseSlot,
                ColorTheme = TypeColorTheme.CoolCyan,
                NeixiCost = 99, // 恒置灰: PlayerNeixi 不可能 ≥ 99
                EffectiveMultiplier = 2.5f,
                TriggerConditions = new[] { "second_hit" },
                SpecialEffects = new[] { "范围 / 多段" },
            },
            new BattlePanelMoveEntry
            {
                MoveId = Cu004DemoXinfaMoveId,
                Name = "明镜止水（心法）",
                Source = MoveSource.XinfaExclusive,
                ColorTheme = TypeColorTheme.CoolCyan,
                NeixiCost = 2,
                EffectiveMultiplier = 1.2f,
                TriggerConditions = new[] { "when_guarding" },
                SpecialEffects = new[] { "封印破除前不可用" },
            },
        }
    };
}
