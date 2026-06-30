namespace FengZhi.Foundation.Epiphany;

public sealed class CombatEpiphanyChecker
{
    public float BaseChance { get; init; } = 0.30f;
    public float DesperationScaling { get; init; } = 0.40f;
    public float ChanceCap { get; init; } = 0.70f;
    public float SkipChanceDecay { get; init; } = 0.15f;

    public float ComputeChance(float currentHpRatio, int skipCount)
    {
        float adjustedBase = BaseChance * (1f - skipCount * SkipChanceDecay);
        if (adjustedBase <= 0f) return 0f;

        float desperationBonus = (1f - currentHpRatio) * DesperationScaling;
        return MathF.Min(adjustedBase + desperationBonus, ChanceCap);
    }

    public bool MeetsCombatConditions(
        CombatTriggerCondition condition,
        IEpiphanyCombatContext context,
        string characterId)
    {
        if (context.HasTriggeredEpiphanyThisBattle()) return false;

        float hpRatio = context.GetCurrentHpRatio(characterId);
        if (hpRatio > condition.HpThreshold) return false;

        int actions = context.GetActorActionsCompleted(characterId);
        if (actions < condition.MinActorActions) return false;

        if (condition.ConsecutiveQiDisadvantageHits > 0)
        {
            int hits = context.GetConsecutiveQiDisadvantageHits(characterId);
            if (hits < condition.ConsecutiveQiDisadvantageHits) return false;
        }

        return true;
    }

    public bool Roll(float chance, Random? rng = null)
    {
        float roll = (rng ?? Random.Shared).NextSingle();
        return roll < chance;
    }
}
