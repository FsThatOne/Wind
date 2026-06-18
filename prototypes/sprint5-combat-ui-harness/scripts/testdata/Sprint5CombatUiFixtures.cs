using System.Collections.Generic;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;

namespace Sprint5CombatUiHarness.TestData;

public sealed record Sprint5CombatUiFixture(
    string Id,
    string DisplayName,
    BattlePanelDisplayData DisplayData,
    CombatUiMoveSelectionContext Context,
    MoveType? ExpectedEnemyCurrentQi,
    string ExpectedContractSummary,
    bool ExpectsCurrentQiContract,
    string InitialHoverActionId,
    bool IsPlaytestLoop = false,
    CombatDecisionLoopExpectation? DecisionExpectation = null);

public sealed record CombatDecisionLoopExpectation(
    string EnemyId,
    string EnemyDisplayName,
    MoveType EnemyCurrentQi,
    int PlayerFlaw,
    int EnemyFlaw,
    int ExpectedNeixiAfterMeditate,
    IReadOnlyDictionary<string, string> ActionOutcomeSummaries);

public static class Sprint5CombatUiFixtures
{
    public static IReadOnlyList<Sprint5CombatUiFixture> All { get; } = new[]
    {
        new Sprint5CombatUiFixture(
            "default_available",
            "DefaultAvailable / 默认可用",
            SixMovePanel(),
            Context(playerNeixi: 9, itemCount: 2, revealedEnemyMoveType: MoveType.Qiao, decisiveTargets: new[] { "iron_crown" }),
            ExpectedEnemyCurrentQi: null,
            ExpectedContractSummary: "期望：6 招式 + 调息 + 使用道具 + 决胜入口均可见；hover/focus 后出现预览卡。普通攻击不属于当前 combat_action_types。",
            ExpectsCurrentQiContract: false,
            InitialHoverActionId: "move:duanyue_fist"),

        new Sprint5CombatUiFixture(
            "insufficient_neixi",
            "InsufficientNeixi / 内息不足",
            SixMovePanel(expensiveFirstMove: true),
            Context(playerNeixi: 2, itemCount: 1, revealedEnemyMoveType: null),
            ExpectedEnemyCurrentQi: null,
            ExpectedContractSummary: "期望：高消耗招式禁用并显示“差 X 内息”；调息仍可聚焦。",
            ExpectsCurrentQiContract: false,
            InitialHoverActionId: "move:duanyue_fist"),

        new Sprint5CombatUiFixture(
            "no_combat_item",
            "NoCombatItem / 无战斗道具",
            SixMovePanel(),
            Context(playerNeixi: 9, itemCount: 0, revealedEnemyMoveType: null),
            ExpectedEnemyCurrentQi: null,
            ExpectedContractSummary: "期望：使用道具可见但禁用，原因显示“无可用战斗道具”。",
            ExpectsCurrentQiContract: false,
            InitialHoverActionId: "use_item"),

        new Sprint5CombatUiFixture(
            "enemy_current_qi_gang",
            "EnemyCurrentQiGang / 敌方当前刚气机",
            SixMovePanel(),
            Context(playerNeixi: 9, itemCount: 1, revealedEnemyMoveType: MoveType.Gang),
            ExpectedEnemyCurrentQi: MoveType.Gang,
            ExpectedContractSummary: "期望：cu-005 按敌方当前内功气机 Gang 判断可读信息与反制关系，而不是按敌方下一招属性。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "move:tingyu_palm"),

        new Sprint5CombatUiFixture(
            "enemy_current_qi_rou",
            "EnemyCurrentQiRou / 敌方当前柔气机",
            SixMovePanel(),
            Context(playerNeixi: 9, itemCount: 1, revealedEnemyMoveType: MoveType.Rou),
            ExpectedEnemyCurrentQi: MoveType.Rou,
            ExpectedContractSummary: "期望：敌方当前内功气机切换为 Rou 后，提示语随当前气机切换。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "move:liuyun_step"),

        new Sprint5CombatUiFixture(
            "stagger_and_decisive",
            "StaggerAndDecisive / 破绽与决胜",
            SixMovePanel(),
            Context(playerNeixi: 9, itemCount: 1, revealedEnemyMoveType: MoveType.Qiao, decisiveTargets: new[] { "iron_crown" }),
            ExpectedEnemyCurrentQi: MoveType.Qiao,
            ExpectedContractSummary: "期望：目标破绽暴露时出现决胜一击，且决胜入口与反制提示分离。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "decisive_strike"),

        new Sprint5CombatUiFixture(
            "dual_focus",
            "DualFocus / 双焦点",
            SixMovePanel(expensiveFirstMove: true),
            Context(playerNeixi: 3, itemCount: 1, revealedEnemyMoveType: MoveType.Qiao),
            ExpectedEnemyCurrentQi: null,
            ExpectedContractSummary: "期望：键盘/手柄 focus 与鼠标 hover 可同时存在；禁用行动不可进入导航图。",
            ExpectsCurrentQiContract: false,
            InitialHoverActionId: "move:tingyu_palm"),

        new Sprint5CombatUiFixture(
            "decision_loop_balanced",
            "DecisionLoopBalanced / 标准决策闭环",
            SixMovePanel(),
            Context(playerNeixi: 8, itemCount: 1, revealedEnemyMoveType: MoveType.Qiao, decisiveTargets: new[] { "iron_crown" }),
            ExpectedEnemyCurrentQi: MoveType.Qiao,
            ExpectedContractSummary: "playtest：观察敌方当前巧气机，在招式、调息、道具、决胜之间做一次合法战斗决策。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "move:duanyue_fist",
            IsPlaytestLoop: true,
            DecisionExpectation: Decision(
                enemyQi: MoveType.Qiao,
                playerFlaw: 1,
                enemyFlaw: 3,
                expectedNeixiAfterMeditate: 11,
                summaries: new Dictionary<string, string>
                {
                    ["move:duanyue_fist"] = "合法：断岳拳消耗 2 内息，刚克敌方当前巧气机；预期可压迫破绽，但不显示最终伤害。",
                    ["move:tingyu_palm"] = "合法：听雨掌消耗 2 内息，柔对巧为不利关系；适合观察风险，不应提示反制收益。",
                    ["rest_meditate"] = "合法：调息不攻击，预期内息从 8 恢复到 11，用于续航。",
                    ["use_item"] = "合法：使用道具入口可提交；具体道具效果由后续物品 UI 决定。",
                    ["decisive_strike"] = "合法：敌方破绽已暴露，决胜入口可提交；与克制提示分离。"
                })),

        new Sprint5CombatUiFixture(
            "decision_loop_resource_pressure",
            "DecisionLoopResourcePressure / 内息压力",
            SixMovePanel(expensiveFirstMove: true),
            Context(playerNeixi: 2, itemCount: 0, revealedEnemyMoveType: MoveType.Gang),
            ExpectedEnemyCurrentQi: MoveType.Gang,
            ExpectedContractSummary: "playtest：玩家内息紧张时，应能看懂高消耗招式和道具不可用原因，并转向调息。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "move:duanyue_fist",
            IsPlaytestLoop: true,
            DecisionExpectation: Decision(
                enemyQi: MoveType.Gang,
                playerFlaw: 2,
                enemyFlaw: 1,
                expectedNeixiAfterMeditate: 5,
                summaries: new Dictionary<string, string>
                {
                    ["move:duanyue_fist"] = "不可用：断岳拳需要 7 内息，当前只有 2；玩家应看到缺少内息原因。",
                    ["move:liuyun_step"] = "合法：流云步消耗 1 内息，巧对刚为不利关系；这是低消耗但非克制选择。",
                    ["rest_meditate"] = "合法：调息恢复内息，预期从 2 恢复到 5，是本状态的主要续航选择。",
                    ["use_item"] = "不可用：无可用战斗道具；入口可见但应禁用。"
                })),

        new Sprint5CombatUiFixture(
            "decision_loop_decisive_window",
            "DecisionLoopDecisiveWindow / 破绽决胜窗口",
            SixMovePanel(),
            Context(playerNeixi: 6, itemCount: 1, revealedEnemyMoveType: MoveType.Rou, decisiveTargets: new[] { "iron_crown" }),
            ExpectedEnemyCurrentQi: MoveType.Rou,
            ExpectedContractSummary: "playtest：敌方破绽暴露时，决胜入口必须清楚出现，且不与克制按钮混淆。",
            ExpectsCurrentQiContract: true,
            InitialHoverActionId: "decisive_strike",
            IsPlaytestLoop: true,
            DecisionExpectation: Decision(
                enemyQi: MoveType.Rou,
                playerFlaw: 0,
                enemyFlaw: 5,
                expectedNeixiAfterMeditate: 9,
                summaries: new Dictionary<string, string>
                {
                    ["decisive_strike"] = "合法：敌方破绽 5，决胜窗口开启；提交的是决胜选择，不是基础反制按钮。",
                    ["move:tingyu_palm"] = "合法：听雨掌消耗 2 内息，柔对柔为中性；可打稳定选择但不是决胜。",
                    ["move:fengzhi_ruler"] = "合法：风止尺法消耗 2 内息，巧克敌方当前柔气机；可利用克制但仍不是最终结算。",
                    ["rest_meditate"] = "合法：调息恢复内息，预期从 6 恢复到 9；会错过当前决胜节奏风险。",
                    ["use_item"] = "合法：使用道具入口可提交，但本 fixture 重点是确认决胜入口可读。"
                }))
    };

    private static CombatUiMoveSelectionContext Context(
        int playerNeixi,
        int itemCount,
        MoveType? revealedEnemyMoveType,
        IReadOnlyList<string>? decisiveTargets = null)
    {
        return new CombatUiMoveSelectionContext(
            PlayerNeixi: playerNeixi,
            IsXinfaSealed: false,
            UsableCombatItemCount: itemCount,
            RevealedEnemyMoveType: revealedEnemyMoveType,
            CurrentTargetId: "iron_crown",
            DecisiveStrikeTargetIds: decisiveTargets);
    }

    private static CombatDecisionLoopExpectation Decision(
        MoveType enemyQi,
        int playerFlaw,
        int enemyFlaw,
        int expectedNeixiAfterMeditate,
        IReadOnlyDictionary<string, string> summaries)
    {
        return new CombatDecisionLoopExpectation(
            EnemyId: "iron_crown",
            EnemyDisplayName: "铁冠道人",
            EnemyCurrentQi: enemyQi,
            PlayerFlaw: playerFlaw,
            EnemyFlaw: enemyFlaw,
            ExpectedNeixiAfterMeditate: expectedNeixiAfterMeditate,
            ActionOutcomeSummaries: summaries);
    }

    private static BattlePanelDisplayData SixMovePanel(bool expensiveFirstMove = false)
    {
        return new BattlePanelDisplayData
        {
            Entries = new[]
            {
                Move("duanyue_fist", "断岳拳", TypeColorTheme.WarmGold, expensiveFirstMove ? 7 : 2, "贴身", "破防"),
                Move("tingyu_palm", "听雨掌", TypeColorTheme.CoolCyan, 2, "近距", "牵引"),
                Move("liuyun_step", "流云步", TypeColorTheme.NeutralGray, 1, "短距", "换位"),
                Move("cunjin", "寸劲", TypeColorTheme.WarmGold, 1, "贴身", "打断"),
                Move("hanbing_palm", "寒冰掌", TypeColorTheme.CoolCyan, 3, "直线", "迟滞"),
                Move("fengzhi_ruler", "风止尺法", TypeColorTheme.NeutralGray, 2, "侧身", "封穴")
            }
        };
    }

    private static BattlePanelMoveEntry Move(
        string id,
        string name,
        TypeColorTheme theme,
        int neixiCost,
        string trigger,
        string effect)
    {
        return new BattlePanelMoveEntry
        {
            MoveId = id,
            Name = name,
            Source = MoveSource.BaseSlot,
            ColorTheme = theme,
            NeixiCost = neixiCost,
            EffectiveMultiplier = 1.0f,
            TriggerConditions = new[] { trigger },
            SpecialEffects = new[] { effect }
        };
    }
}
