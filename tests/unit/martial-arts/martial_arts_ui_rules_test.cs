using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace Foundation.Tests.Unit.MartialArts;

#region Test Helpers

internal sealed class UITestDataTable<T> : IDataTable<T> where T : class
{
    private readonly Dictionary<string, T> _items = new();
    private readonly Func<T, string> _idSelector;

    public UITestDataTable(Func<T, string> idSelector) => _idSelector = idSelector;
    public void Add(T item) => _items[_idSelector(item)] = item;
    public T? Get(string id) => _items.TryGetValue(id, out var v) ? v : null;
    public IReadOnlyList<T> GetAll() => _items.Values.ToList();
    public int Count => _items.Count;
}

internal sealed class UITestStatProvider : ICharacterStatProvider
{
    public int GetStat(string characterId, string statName) => 99; // 总是满足门槛
}

#endregion

public class MartialArtsUIRulesTest
{
    private const string CharId = "player";

    private readonly UITestDataTable<MoveDefinition> _moveTable;
    private readonly UITestDataTable<XinfaDefinition> _xinfaTable;
    private readonly MoveProgressionService _progressionService;
    private readonly XinfaService _xinfaService;
    private readonly MartialArtsUIService _uiService;
    private readonly CharacterLoadout _loadout;

    public MartialArtsUIRulesTest()
    {
        _moveTable = new UITestDataTable<MoveDefinition>(m => m.Id);
        _xinfaTable = new UITestDataTable<XinfaDefinition>(x => x.Id);
        _progressionService = new MoveProgressionService(_moveTable);
        _xinfaService = new XinfaService(_xinfaTable, _moveTable, new UITestStatProvider());
        _uiService = new MartialArtsUIService(_moveTable, _progressionService, _xinfaService);
        _loadout = new CharacterLoadout(CharId);

        // 普通武学 - 刚系
        _moveTable.Add(new MoveDefinition
        {
            Id = "basic_gang",
            Name = "铁臂功",
            Type = MoveType.Gang,
            Category = MoveCategory.Basic,
            NeixiCost = 2,
            BaseMultiplier = 0.8f,
            TriggerConditions = new List<string> { "always" },
            SpecialEffects = new List<string> { "flaw_expose" },
            Annotatable = false
        });

        // 高级武学 - 柔系
        _moveTable.Add(new MoveDefinition
        {
            Id = "advanced_rou",
            Name = "千叶掌",
            Type = MoveType.Rou,
            Category = MoveCategory.Advanced,
            NeixiCost = 5,
            BaseMultiplier = 1.1f,
            TriggerConditions = new List<string> { "second_counter_hit" },
            SpecialEffects = new List<string> { "multi_strike" },
            Annotatable = false
        });

        // 绝学 - 巧系（可批注）
        _moveTable.Add(new MoveDefinition
        {
            Id = "ultimate_qiao",
            Name = "听风辨形",
            Type = MoveType.Qiao,
            Category = MoveCategory.Ultimate,
            NeixiCost = 7,
            BaseMultiplier = 1.4f,
            TriggerConditions = new List<string> { "no_condition" },
            SpecialEffects = new List<string> { "devastate" },
            Annotatable = true,
            Annotation = new AnnotationDefinition
            {
                ConditionOverride = "no_condition",
                EffectOverride = "devastate_enhanced",
                AnnotationMult = 1.5f
            }
        });

        // 心法专属招式
        _moveTable.Add(new MoveDefinition
        {
            Id = "xinfa_move_1",
            Name = "心法招式一",
            Type = MoveType.Rou,
            Category = MoveCategory.Basic,
            NeixiCost = 3,
            BaseMultiplier = 0.9f,
            TriggerConditions = new List<string> { "always" },
            SpecialEffects = new List<string> { "guard_up" }
        });

        _moveTable.Add(new MoveDefinition
        {
            Id = "xinfa_move_2",
            Name = "心法招式二",
            Type = MoveType.Rou,
            Category = MoveCategory.Basic,
            NeixiCost = 4,
            BaseMultiplier = 1.0f,
            TriggerConditions = new List<string> { "target_not_immobilized" },
            SpecialEffects = new List<string> { "immobilize_chance" }
        });

        // 心法
        _xinfaTable.Add(new XinfaDefinition
        {
            Id = "test_xinfa",
            Name = "测试心法",
            Requirements = new List<XinfaRequirement>(),
            PassiveBonuses = new List<XinfaPassiveBonus> { new() { Stat = "attack", Value = 5f } },
            ExclusiveMoves = new List<string> { "xinfa_move_1", "xinfa_move_2" }
        });
    }

    #region AC-1: 卡片颜色只体现体系，不体现稀有度

    [Theory]
    [InlineData(MoveType.Gang, TypeColorTheme.WarmGold)]
    [InlineData(MoveType.Rou, TypeColorTheme.CoolCyan)]
    [InlineData(MoveType.Qiao, TypeColorTheme.NeutralGray)]
    public void ColorTheme_DependsOnlyOnType_NotCategory(MoveType type, TypeColorTheme expected)
    {
        Assert.Equal(expected, MartialArtsUIService.GetColorTheme(type));
    }

    [Fact]
    public void CardColor_BasicGang_SameAsUltimateGang()
    {
        // 普通/高级/绝学同体系 → 同颜色
        _progressionService.LearnBasicMove("basic_gang");
        var basicCard = _uiService.GetMoveCardDisplay("basic_gang");

        // 添加一个刚系绝学用于对比
        _moveTable.Add(new MoveDefinition
        {
            Id = "ultimate_gang",
            Name = "天罡裂魄",
            Type = MoveType.Gang,
            Category = MoveCategory.Ultimate,
            NeixiCost = 8,
            BaseMultiplier = 1.4f,
            TriggerConditions = new List<string> { "no_condition" },
            SpecialEffects = new List<string> { "shatter_guard" }
        });
        _progressionService.AddFragment("ultimate_gang");
        var ultimateCard = _uiService.GetMoveCardDisplay("ultimate_gang");

        Assert.Equal(basicCard!.ColorTheme, ultimateCard!.ColorTheme);
        Assert.Equal(TypeColorTheme.WarmGold, basicCard.ColorTheme);
    }

    [Fact]
    public void CardColor_DifferentTypes_DifferentColors()
    {
        _progressionService.LearnBasicMove("basic_gang");
        _progressionService.AddFragment("advanced_rou");

        var gangCard = _uiService.GetMoveCardDisplay("basic_gang");
        var rouCard = _uiService.GetMoveCardDisplay("advanced_rou");

        Assert.NotEqual(gangCard!.ColorTheme, rouCard!.ColorTheme);
    }

    #endregion

    #region AC-2: 进度显示规则

    [Fact]
    public void BasicMove_ShowsLearnedMode_NoProgressBar()
    {
        // 普通武学显示"已习得"
        _progressionService.LearnBasicMove("basic_gang");

        var card = _uiService.GetMoveCardDisplay("basic_gang");

        Assert.Equal(ProgressDisplayMode.Learned, card!.DisplayMode);
        Assert.Equal(ProgressTierLabel.None, card.TierLabel);
        Assert.False(card.IsAnnotated);
    }

    [Fact]
    public void AdvancedMove_Fragment_ShowsProgressBar()
    {
        // 高级武学残卷阶段显示进度条
        _progressionService.AddFragment("advanced_rou");

        var card = _uiService.GetMoveCardDisplay("advanced_rou");

        Assert.Equal(ProgressDisplayMode.ProgressBar, card!.DisplayMode);
        Assert.Equal(ProgressTierLabel.Fragment, card.TierLabel);
        Assert.Equal(0.55f, card.Completion, precision: 2);
    }

    [Fact]
    public void UltimateMove_Mastered_ShowsMasteredTier()
    {
        // 绝学真传阶段
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.SynthesizeToManuscript("ultimate_qiao");
        _progressionService.UpgradeToComplete("ultimate_qiao");
        _progressionService.UpgradeToMastered("ultimate_qiao");

        var card = _uiService.GetMoveCardDisplay("ultimate_qiao");

        Assert.Equal(ProgressDisplayMode.ProgressBar, card!.DisplayMode);
        Assert.Equal(ProgressTierLabel.Mastered, card.TierLabel);
        Assert.Equal(1.0f, card.Completion, precision: 2);
    }

    [Fact]
    public void AnnotatedMove_ShowsAnnotatedMode()
    {
        // 批注版独特标记
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.AddFragment("ultimate_qiao");
        _progressionService.SynthesizeToManuscript("ultimate_qiao");
        _progressionService.UpgradeToComplete("ultimate_qiao");
        _progressionService.UpgradeToMastered("ultimate_qiao");
        _progressionService.ApplyAnnotation("ultimate_qiao");

        var card = _uiService.GetMoveCardDisplay("ultimate_qiao");

        Assert.Equal(ProgressDisplayMode.Annotated, card!.DisplayMode);
        Assert.True(card.IsAnnotated);
        Assert.Equal(1.4f * 1.5f, card.EffectiveMultiplier, precision: 2);
    }

    #endregion

    #region AC-3: 条件效果直接可见

    [Fact]
    public void MoveCard_ConditionsAndEffects_AlwaysVisible()
    {
        _progressionService.LearnBasicMove("basic_gang");

        var card = _uiService.GetMoveCardDisplay("basic_gang");

        // 触发条件和特殊效果非空且直接暴露
        Assert.NotEmpty(card!.TriggerConditions);
        Assert.NotEmpty(card.SpecialEffects);
        Assert.Contains("always", card.TriggerConditions);
        Assert.Contains("flaw_expose", card.SpecialEffects);
    }

    [Fact]
    public void MoveCard_MultiplierAsNumber_Visible()
    {
        _progressionService.LearnBasicMove("basic_gang");

        var card = _uiService.GetMoveCardDisplay("basic_gang");

        Assert.Equal(0.8f, card!.BaseMultiplier, precision: 2);
        Assert.Equal(0.8f, card.EffectiveMultiplier, precision: 2);
    }

    #endregion

    #region AC-4: 战斗面板 6 基础 + 心法专属

    [Fact]
    public void BattlePanel_ShowsBasePlusXinfaExclusive()
    {
        // 装备基础招式
        _progressionService.LearnBasicMove("basic_gang");
        _progressionService.AddFragment("advanced_rou");

        var loadoutSvc = new LoadoutService(_moveTable, _progressionService);
        loadoutSvc.EquipMove(_loadout, 0, "basic_gang");
        loadoutSvc.EquipMove(_loadout, 1, "advanced_rou");

        // 装备心法（含 2 个专属招式）
        _xinfaService.EquipXinfa(_loadout, "test_xinfa");

        // When
        var panel = _uiService.GetBattlePanelDisplay(_loadout);

        // Then: 2 基础 + 2 心法专属 = 4
        Assert.Equal(2, panel.BaseSlotCount);
        Assert.Equal(2, panel.XinfaExclusiveCount);
        Assert.Equal(4, panel.Entries.Count);

        // 来源标记正确
        Assert.All(panel.Entries.Where(e => e.Source == MoveSource.BaseSlot),
            e => Assert.Contains(e.MoveId, new[] { "basic_gang", "advanced_rou" }));
        Assert.All(panel.Entries.Where(e => e.Source == MoveSource.XinfaExclusive),
            e => Assert.Contains(e.MoveId, new[] { "xinfa_move_1", "xinfa_move_2" }));
    }

    [Fact]
    public void BattlePanel_XinfaExclusive_HasCorrectColorAndData()
    {
        _progressionService.LearnBasicMove("basic_gang");
        var loadoutSvc = new LoadoutService(_moveTable, _progressionService);
        loadoutSvc.EquipMove(_loadout, 0, "basic_gang");
        _xinfaService.EquipXinfa(_loadout, "test_xinfa");

        var panel = _uiService.GetBattlePanelDisplay(_loadout);

        var xinfaEntry = panel.Entries.First(e => e.Source == MoveSource.XinfaExclusive);
        Assert.Equal(TypeColorTheme.CoolCyan, xinfaEntry.ColorTheme); // 柔系
        Assert.NotEmpty(xinfaEntry.TriggerConditions);
        Assert.NotEmpty(xinfaEntry.SpecialEffects);
    }

    [Fact]
    public void BattlePanel_NoXinfa_OnlyBaseSlots()
    {
        _progressionService.LearnBasicMove("basic_gang");
        var loadoutSvc = new LoadoutService(_moveTable, _progressionService);
        loadoutSvc.EquipMove(_loadout, 0, "basic_gang");

        var panel = _uiService.GetBattlePanelDisplay(_loadout);

        Assert.Equal(1, panel.BaseSlotCount);
        Assert.Equal(0, panel.XinfaExclusiveCount);
    }

    #endregion
}
