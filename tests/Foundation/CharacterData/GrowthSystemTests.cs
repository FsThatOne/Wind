using Xunit;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Tests.Foundation.CharacterData;

public class GrowthSystemTests
{
    private static CharacterAttributes CreateDefaultAttrs() => new()
    {
        Strength = 8, Agility = 8, InnerPower = 8, Insight = 8, Constitution = 8
    }; // TotalPower = 40

    private static GrowthNode CreateChapterNode(string id, int chapter,
        int str = 0, int agi = 0, int inn = 0, int ins = 0, int con = 0) => new()
    {
        Id = id, Chapter = chapter,
        StrengthGain = str, AgilityGain = agi, InnerPowerGain = inn,
        InsightGain = ins, ConstitutionGain = con
    };

    // ─── AC1: 顿悟触发境界突破 ──────────────────────────────

    [Fact]
    public void ApplyEpiphanyReward_CrossesThreshold_TriggersBreakthrough()
    {
        // 当前功力=114 (炉火纯青 index=5), 顿悟+2 → 116 → 出神入化
        var attrs = new CharacterAttributes
        {
            Strength = 23, Agility = 23, InnerPower = 23, Insight = 23, Constitution = 22
        }; // TotalPower = 114
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var reward = CreateChapterNode("epiphany_ch4", 4, str: 1, inn: 1);
        var result = growth.ApplyEpiphanyReward(attrs, modifiers, reward,
            currentRealmIndex: 5, narrativeConditionMet: true);

        Assert.True(result.Success);
        Assert.Equal(BreakthroughResult.Triggered, result.Breakthrough);
        Assert.Equal(116, attrs.TotalPower);
    }

    // ─── AC2: 叙事未满足 → PendingNarrative ────────────────

    [Fact]
    public void ApplyEpiphanyReward_CrossesThreshold_NarrativeNotMet_Pending()
    {
        var attrs = new CharacterAttributes
        {
            Strength = 23, Agility = 23, InnerPower = 23, Insight = 23, Constitution = 22
        }; // 114
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var reward = CreateChapterNode("epiphany_ch4_2", 4, str: 1, inn: 1);
        var result = growth.ApplyEpiphanyReward(attrs, modifiers, reward,
            currentRealmIndex: 5, narrativeConditionMet: false);

        Assert.True(result.Success);
        Assert.Equal(BreakthroughResult.PendingNarrative, result.Breakthrough);
    }

    // ─── AC3: ApplyGrowthNode 写入永久修改器 ────────────────

    [Fact]
    public void ApplyGrowthNode_IncreasesAttributes_AddsPermanentModifier()
    {
        var attrs = CreateDefaultAttrs(); // TotalPower=40
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var node = CreateChapterNode("ch3_node2", 3, str: 2, agi: 1, inn: 2);
        var result = growth.ApplyGrowthNode(attrs, modifiers, node);

        Assert.True(result.Success);
        Assert.Equal(10, attrs.Strength);  // 8+2
        Assert.Equal(9, attrs.Agility);    // 8+1
        Assert.Equal(10, attrs.InnerPower); // 8+2
        Assert.Equal(45, attrs.TotalPower); // 40+5
        Assert.True(modifiers.HasSource("growth:ch3_node2"));
    }

    [Fact]
    public void ApplyGrowthNode_DuplicateNode_ReturnsFalse()
    {
        var attrs = CreateDefaultAttrs();
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var node = CreateChapterNode("ch1_node1", 1, str: 2, con: 1);
        growth.ApplyGrowthNode(attrs, modifiers, node);

        // 重复施加
        var result = growth.ApplyGrowthNode(attrs, modifiers, node);
        Assert.False(result.Success);
    }

    // ─── AC4: 追赶成长受上限约束 ────────────────────────────

    [Fact]
    public void ApplyCatchupGrowth_BehindBaseline_AppliesCappedGrowth()
    {
        // 章节3基线=94, 当前功力=40 → deficit=54 → 追赶 min(8, 54) = 8
        var attrs = CreateDefaultAttrs(); // TotalPower=40
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var result = growth.ApplyCatchupGrowth(attrs, modifiers, chapter: 3, reason: "rejoin");

        Assert.True(result.Success);
        Assert.Equal(48, attrs.TotalPower); // 40+8
    }

    [Fact]
    public void ApplyCatchupGrowth_SmallDeficit_AppliesExactDeficit()
    {
        // 章节0基线=43, 当前功力=40 → deficit=3 → 追赶 min(8, 3) = 3
        var attrs = CreateDefaultAttrs(); // TotalPower=40
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var result = growth.ApplyCatchupGrowth(attrs, modifiers, chapter: 0, reason: "observation");

        Assert.True(result.Success);
        Assert.Equal(43, attrs.TotalPower); // 40+3
    }

    // ─── AC5: 不落后则不追赶 ────────────────────────────────

    [Fact]
    public void ApplyCatchupGrowth_NotBehind_NoGrowth()
    {
        // 章节0基线=43, 当前功力=50 → 不落后
        var attrs = new CharacterAttributes
        {
            Strength = 10, Agility = 10, InnerPower = 10, Insight = 10, Constitution = 10
        }; // TotalPower=50
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var result = growth.ApplyCatchupGrowth(attrs, modifiers, chapter: 0, reason: "test");

        Assert.False(result.Success);
        Assert.Equal(50, attrs.TotalPower); // 不变
    }

    // ─── AC6: GetChapterBaselinePower ────────────────────────

    [Theory]
    [InlineData(0, 43)]   // 序章
    [InlineData(1, 56)]   // 第一章
    [InlineData(2, 72)]   // 第二章
    [InlineData(3, 94)]   // 第三章
    [InlineData(4, 113)]  // 第四章
    [InlineData(5, 132)]  // 第五章
    [InlineData(6, 147)]  // 终章
    public void GetChapterBaselinePower_ReturnsCorrectValue(int chapter, int expected)
    {
        Assert.Equal(expected, GrowthSystem.GetChapterBaselinePower(chapter));
    }

    [Fact]
    public void GetChapterBaselinePower_BeyondLastChapter_ReturnsLast()
    {
        Assert.Equal(147, GrowthSystem.GetChapterBaselinePower(10));
    }

    [Fact]
    public void GetChapterBaselinePower_Negative_ReturnsInitial()
    {
        Assert.Equal(40, GrowthSystem.GetChapterBaselinePower(-1));
    }

    // ─── AC7: 五维上限 250 → 不增属性 ──────────────────────

    [Fact]
    public void ApplyEpiphanyReward_AtMaxAttributes_NoGrowth_ContentUnlock()
    {
        var attrs = new CharacterAttributes
        {
            Strength = 50, Agility = 50, InnerPower = 50, Insight = 50, Constitution = 50
        }; // TotalPower=250
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var reward = CreateChapterNode("epiphany_max", 6, str: 2);
        var result = growth.ApplyEpiphanyReward(attrs, modifiers, reward,
            currentRealmIndex: 8, narrativeConditionMet: true);

        Assert.True(result.Success);
        Assert.Contains("特殊武学", result.Message);
        Assert.Equal(250, attrs.TotalPower); // 不变
    }

    // ─── 队伍成长 ───────────────────────────────────────────

    [Fact]
    public void ApplyPartyGrowthNode_Works()
    {
        var attrs = CreateDefaultAttrs();
        var modifiers = new ModifierStack();
        var growth = new GrowthSystem();

        var node = CreateChapterNode("companion_combat_insight", 2, ins: 2, agi: 1);
        var result = growth.ApplyPartyGrowthNode(attrs, modifiers, node);

        Assert.True(result.Success);
        Assert.Equal(10, attrs.Insight);
        Assert.Equal(9, attrs.Agility);
        Assert.True(modifiers.HasSource("party_growth:companion_combat_insight"));
    }
}
