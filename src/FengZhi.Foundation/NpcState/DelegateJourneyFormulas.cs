namespace FengZhi.Foundation.NpcState;

/// <summary>
/// F2 代办资格判定。全部 5 条件 AND 逻辑。
/// </summary>
public static class DelegateFormula
{
    /// <summary>
    /// 判断 NPC 是否有代办资格。
    /// 条件: alive AND (attitude >= Friendly) AND (relationship >= Acquaintance)
    ///        AND (presence != Unreachable) AND (delegateAllowed flag)
    /// </summary>
    public static bool IsEligible(
        LifeStatus life,
        AttitudeLevel attitude,
        RelationshipStage relationship,
        PresenceStatus presence,
        bool delegateAllowed)
    {
        if (life != LifeStatus.Alive && life != LifeStatus.Injured) return false;
        if (attitude < AttitudeLevel.Friendly) return false;
        if (relationship < RelationshipStage.Acquaintance) return false;
        if (presence == PresenceStatus.Unreachable) return false;
        if (!delegateAllowed) return false;
        return true;
    }
}

/// <summary>
/// F3 旅程触发判定。
/// </summary>
public static class JourneyFormula
{
    /// <summary>
    /// 判断是否触发 NPC 独立旅程。
    /// 条件: (chapter >= requiredChapter OR dayCount >= requiredDays) AND NOT alreadyTriggered
    /// </summary>
    public static bool IsTriggered(
        int currentChapter,
        int requiredChapter,
        int dayCount,
        int requiredDays,
        bool alreadyTriggered)
    {
        if (alreadyTriggered) return false;
        bool chapterMet = currentChapter >= requiredChapter;
        bool daysMet = dayCount >= requiredDays;
        return chapterMet || daysMet;
    }
}

/// <summary>
/// F4 求援概率计算。
/// help_chance = clamp(base + ability_gap + attitude_mod, 0, 100)
/// </summary>
public static class HelpRequestFormula
{
    /// <summary>
    /// 计算求援概率百分比。
    /// </summary>
    /// <param name="baseChance">基础概率（help_request_allowed=false 时为 0）</param>
    /// <param name="abilityGap">能力差距加成</param>
    /// <param name="attitudeMod">态度修正（高好感度加成）</param>
    /// <returns>0-100 概率值</returns>
    public static int Calculate(int baseChance, int abilityGap, int attitudeMod)
    {
        int raw = baseChance + abilityGap + attitudeMod;
        return Math.Clamp(raw, 0, 100);
    }
}

/// <summary>
/// F5 回信指点加成。
/// guided_power = delegate_power + guidance_bonus
/// </summary>
public static class GuidedDelegateFormula
{
    /// <summary>
    /// 计算指导后的代办效力。
    /// </summary>
    public static int Calculate(int delegatePower, int guidanceBonus)
    {
        return delegatePower + guidanceBonus;
    }
}
