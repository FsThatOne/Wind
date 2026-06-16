namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 成长系统结果。
/// </summary>
public readonly record struct GrowthResult(
    bool Success,
    string Message,
    BreakthroughResult? Breakthrough = null);

/// <summary>
/// 成长节点定义（章节/队伍）。
/// </summary>
public sealed class GrowthNode
{
    public required string Id { get; init; }
    public required int Chapter { get; init; }
    public int StrengthGain { get; init; }
    public int AgilityGain { get; init; }
    public int InnerPowerGain { get; init; }
    public int InsightGain { get; init; }
    public int ConstitutionGain { get; init; }

    /// <summary>该节点的总属性增量</summary>
    public int TotalGain => StrengthGain + AgilityGain + InnerPowerGain + InsightGain + ConstitutionGain;
}

/// <summary>
/// 角色成长系统。管理四种叙事驱动的属性成长路径。
/// GDD: character-attributes.md §成长系统
/// </summary>
public sealed class GrowthSystem
{
    /// <summary>五维属性理论总上限</summary>
    public const int MaxTotalAttributes = 250;

    /// <summary>每章末尾追赶上限</summary>
    public const int CatchupCapPerChapter = 8;

    /// <summary>初始功力（五维初始各 8 = 40）</summary>
    public const int InitialTotalPower = 40;

    // 章节基线功力 (GDD 表格)
    private static readonly int[] ChapterBaselines = { 43, 56, 72, 94, 113, 132, 147 };

    private readonly HashSet<string> _appliedNodes = new();

    // ─── 章节成长 ───────────────────────────────────────────

    /// <summary>
    /// 施加章节成长节点。通过 Permanent 层修改器写入属性。
    /// </summary>
    public GrowthResult ApplyGrowthNode(CharacterAttributes attrs, ModifierStack modifiers, GrowthNode node)
    {
        if (_appliedNodes.Contains(node.Id))
            return new GrowthResult(false, $"成长节点 {node.Id} 已施加过");

        ApplyNodeModifiers(attrs, modifiers, node, $"growth:{node.Id}");
        _appliedNodes.Add(node.Id);

        return new GrowthResult(true, $"章节成长: {node.Id} (+{node.TotalGain})");
    }

    // ─── 顿悟突破 ───────────────────────────────────────────

    /// <summary>
    /// 施加顿悟奖励。写入属性后检查境界突破。
    /// 若五维总和已达 250，不增加属性，返回 content unlock flag。
    /// </summary>
    public GrowthResult ApplyEpiphanyReward(
        CharacterAttributes attrs,
        ModifierStack modifiers,
        GrowthNode reward,
        int currentRealmIndex,
        bool narrativeConditionMet)
    {
        // 五维上限检查
        if (attrs.TotalPower >= MaxTotalAttributes)
        {
            return new GrowthResult(true,
                "五维已达极限，顿悟转化为特殊武学/叙事解锁",
                BreakthroughResult.NoChange);
        }

        ApplyNodeModifiers(attrs, modifiers, reward, $"epiphany:{reward.Id}");

        // 境界突破检查
        var breakthrough = RealmSystem.CheckBreakthrough(
            currentRealmIndex, attrs.TotalPower, narrativeConditionMet);

        return new GrowthResult(true,
            $"顿悟: {reward.Id} (+{reward.TotalGain})",
            breakthrough);
    }

    // ─── 队伍成长 ───────────────────────────────────────────

    /// <summary>
    /// 施加队伍/同伴成长节点。
    /// </summary>
    public GrowthResult ApplyPartyGrowthNode(CharacterAttributes attrs, ModifierStack modifiers, GrowthNode node)
    {
        if (_appliedNodes.Contains(node.Id))
            return new GrowthResult(false, $"队伍成长节点 {node.Id} 已施加过");

        ApplyNodeModifiers(attrs, modifiers, node, $"party_growth:{node.Id}");
        _appliedNodes.Add(node.Id);

        return new GrowthResult(true, $"队伍成长: {node.Id} (+{node.TotalGain})");
    }

    // ─── 末尾追赶 ───────────────────────────────────────────

    /// <summary>
    /// 为落后的同伴施加追赶成长。
    /// catchup = min(cap, baseline - currentPower)，仅对落后者生效。
    /// </summary>
    public GrowthResult ApplyCatchupGrowth(
        CharacterAttributes attrs,
        ModifierStack modifiers,
        int chapter,
        string reason)
    {
        int baseline = GetChapterBaselinePower(chapter);
        int currentPower = attrs.TotalPower;

        if (currentPower >= baseline)
        {
            return new GrowthResult(false, "角色未落后于章节基线，无需追赶");
        }

        int deficit = baseline - currentPower;
        int catchup = Math.Min(CatchupCapPerChapter, deficit);

        // 均匀分配到五维（优先补足最低属性）
        var distribution = DistributeCatchup(attrs, catchup);
        string source = $"catchup:ch{chapter}_{reason}";

        if (distribution.StrengthGain > 0)
            AddPermanentModifier(modifiers, source, AttributeType.Strength, distribution.StrengthGain);
        if (distribution.AgilityGain > 0)
            AddPermanentModifier(modifiers, source, AttributeType.Agility, distribution.AgilityGain);
        if (distribution.InnerPowerGain > 0)
            AddPermanentModifier(modifiers, source, AttributeType.InnerPower, distribution.InnerPowerGain);
        if (distribution.InsightGain > 0)
            AddPermanentModifier(modifiers, source, AttributeType.Insight, distribution.InsightGain);
        if (distribution.ConstitutionGain > 0)
            AddPermanentModifier(modifiers, source, AttributeType.Constitution, distribution.ConstitutionGain);

        // 直接写入属性
        attrs.Strength += distribution.StrengthGain;
        attrs.Agility += distribution.AgilityGain;
        attrs.InnerPower += distribution.InnerPowerGain;
        attrs.Insight += distribution.InsightGain;
        attrs.Constitution += distribution.ConstitutionGain;

        return new GrowthResult(true, $"追赶成长: +{catchup} (章节{chapter}, {reason})");
    }

    // ─── 章节基线查询 ───────────────────────────────────────

    /// <summary>
    /// 返回指定章节的推荐功力基线。
    /// chapter: 0=序章, 1=第一章, ..., 6=终章
    /// </summary>
    public static int GetChapterBaselinePower(int chapter)
    {
        if (chapter < 0) return InitialTotalPower;
        if (chapter >= ChapterBaselines.Length) return ChapterBaselines[^1];
        return ChapterBaselines[chapter];
    }

    /// <summary>
    /// 检查某成长节点是否已施加。
    /// </summary>
    public bool HasApplied(string nodeId) => _appliedNodes.Contains(nodeId);

    // ─── 私有方法 ───────────────────────────────────────────

    private void ApplyNodeModifiers(CharacterAttributes attrs, ModifierStack modifiers, GrowthNode node, string source)
    {
        if (node.StrengthGain != 0)
        {
            AddPermanentModifier(modifiers, source, AttributeType.Strength, node.StrengthGain);
            attrs.Strength += node.StrengthGain;
        }
        if (node.AgilityGain != 0)
        {
            AddPermanentModifier(modifiers, source, AttributeType.Agility, node.AgilityGain);
            attrs.Agility += node.AgilityGain;
        }
        if (node.InnerPowerGain != 0)
        {
            AddPermanentModifier(modifiers, source, AttributeType.InnerPower, node.InnerPowerGain);
            attrs.InnerPower += node.InnerPowerGain;
        }
        if (node.InsightGain != 0)
        {
            AddPermanentModifier(modifiers, source, AttributeType.Insight, node.InsightGain);
            attrs.Insight += node.InsightGain;
        }
        if (node.ConstitutionGain != 0)
        {
            AddPermanentModifier(modifiers, source, AttributeType.Constitution, node.ConstitutionGain);
            attrs.Constitution += node.ConstitutionGain;
        }
    }

    private static void AddPermanentModifier(ModifierStack modifiers, string source, AttributeType attr, int value)
    {
        modifiers.Add(new AttributeModifier
        {
            Source = source,
            Layer = ModifierLayer.Permanent,
            Attribute = attr,
            Value = value
        });
    }

    /// <summary>
    /// 将追赶点数分配到五维（优先补最低属性）。
    /// </summary>
    private static GrowthNode DistributeCatchup(CharacterAttributes attrs, int points)
    {
        int str = 0, agi = 0, inn = 0, ins = 0, con = 0;

        // 简单策略：按当前值从低到高轮流分配
        var ranked = new (int current, int index)[]
        {
            (attrs.Strength, 0),
            (attrs.Agility, 1),
            (attrs.InnerPower, 2),
            (attrs.Insight, 3),
            (attrs.Constitution, 4)
        };

        Array.Sort(ranked, (a, b) => a.current.CompareTo(b.current));

        for (int i = 0; i < points; i++)
        {
            int target = ranked[i % ranked.Length].index;
            switch (target)
            {
                case 0: str++; break;
                case 1: agi++; break;
                case 2: inn++; break;
                case 3: ins++; break;
                case 4: con++; break;
            }
        }

        return new GrowthNode
        {
            Id = "catchup_distribution",
            Chapter = 0,
            StrengthGain = str,
            AgilityGain = agi,
            InnerPowerGain = inn,
            InsightGain = ins,
            ConstitutionGain = con
        };
    }
}
