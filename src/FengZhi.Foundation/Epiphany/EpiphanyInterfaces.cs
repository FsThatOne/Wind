namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanyEventInstance
{
    public string ConfigId { get; init; } = string.Empty;
    public EpiphanyEventState State { get; set; } = EpiphanyEventState.Locked;
    public int SkipCount { get; set; }
    public bool IsDone => State == EpiphanyEventState.Completed || State == EpiphanyEventState.Exhausted;
}

public interface IEpiphanyFlagProvider
{
    bool HasFlag(string flag);
    void SetFlag(string flag);
    int GetCurrentChapter();
    int GetMeditationDays();
}

public interface IEpiphanyCombatContext
{
    float GetCurrentHpRatio(string characterId);
    int GetActorActionsCompleted(string characterId);
    int GetConsecutiveQiDisadvantageHits(string characterId);
    bool HasTriggeredEpiphanyThisBattle();
    void MarkEpiphanyTriggered();
    void PauseCombat();
    void ResumeCombat();
}

public interface IEpiphanyRewardTarget
{
    void ApplyPermanentAttributeGrowth(string characterId, Dictionary<string, int> attributes);
    void UnlockMove(string characterId, string moveId);
    void UnlockNarrativeOption(string optionId);
    void UnlockAbility(string characterId, string abilityId);
    void ApplyPostEpiphanyBuff(string characterId, float multiplier);
    void RefreshCooldowns(string characterId);
    void ApplySkipReward(string characterId, SkipReward reward);
    void CheckRealmBreakthrough(string characterId);
}
