namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanyConfig
{
    public string Id { get; init; } = string.Empty;
    public EpiphanyType Type { get; init; }
    public int ChapterMin { get; init; }
    public int ChapterMax { get; init; }
    public int Priority { get; init; } = 50;
    public int ChapterCapCost { get; init; } = 1;
    public int SkipLimit { get; init; } = 3;
    public bool DeathProtection { get; init; }
    public bool CombatOnly { get; init; }

    public CombatTriggerCondition? CombatCondition { get; init; }
    public string? NarrativeNodeId { get; init; }
    public MeditationCondition? MeditationCondition { get; init; }

    public List<string> Preconditions { get; init; } = new();
    public EpiphanyReward Reward { get; init; } = new();
    public SkipReward SkipRewardConfig { get; init; } = new();
    public string CutsceneKey { get; init; } = string.Empty;
}

public sealed class CombatTriggerCondition
{
    public float HpThreshold { get; init; } = 0.2f;
    public int MinActorActions { get; init; } = 5;
    public int ConsecutiveQiDisadvantageHits { get; init; }
}

public sealed class MeditationCondition
{
    public List<string> RequiredFlags { get; init; } = new();
    public int MinMeditationDays { get; init; } = 3;
}

public sealed class EpiphanyReward
{
    public Dictionary<string, int> Attributes { get; init; } = new();
    public string? UnlockMoveId { get; init; }
    public string? UnlockNarrativeOption { get; init; }
    public string? UnlockAbility { get; init; }
}

public sealed class SkipReward
{
    public float HpRestorePercent { get; init; } = 0.5f;
    public float NeiXiRestorePercent { get; init; } = 0.5f;
    public bool ClearStagger { get; init; } = true;
    public bool NextCrit { get; init; } = true;
    public float DamageReduction { get; init; } = 0.3f;
}
