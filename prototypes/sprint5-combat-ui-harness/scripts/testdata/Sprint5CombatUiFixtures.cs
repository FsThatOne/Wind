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
    string InitialHoverActionId);

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
            InitialHoverActionId: "move:tingyu_palm")
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
