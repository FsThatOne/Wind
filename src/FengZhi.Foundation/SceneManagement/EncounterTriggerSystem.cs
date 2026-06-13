using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Foundation.SceneManagement;

public sealed class EncounterCondition
{
    public Season? RequiredSeason { get; set; }
    public WeatherType? RequiredWeather { get; set; }
    public Shichen? RequiredShichen { get; set; }
    public float? MinProgress { get; set; }
    public float? MaxProgress { get; set; }
    public string? RequiredMindset { get; set; }
}

public sealed class EncounterDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Priority { get; set; }
    public float TriggerChance { get; set; } = 1f;
    public bool Repeatable { get; set; } = true;
    public EncounterCondition Condition { get; set; } = new();
}

public sealed class EncounterContext
{
    public Season Season { get; set; }
    public WeatherType Weather { get; set; }
    public Shichen Shichen { get; set; }
    public float Progress { get; set; }
    public string? Mindset { get; set; }
}

public sealed class EncounterTriggerSystem
{
    private readonly List<EncounterDefinition> _encounters = new();
    private readonly HashSet<string> _consumed = new();

    public void Register(EncounterDefinition encounter) => _encounters.Add(encounter);

    public void RegisterRange(IEnumerable<EncounterDefinition> encounters)
    {
        _encounters.AddRange(encounters);
    }

    public bool IsConsumed(string id) => _consumed.Contains(id);

    /// <summary>
    /// Evaluate encounters and return the triggered one (or null).
    /// Uses provided randomValue [0,1) for probability check.
    /// </summary>
    public EncounterDefinition? Evaluate(EncounterContext context, float randomValue)
    {
        var candidates = _encounters
            .Where(e => !_consumed.Contains(e.Id))
            .Where(e => CheckCondition(e.Condition, context))
            .OrderByDescending(e => e.Priority)
            .ToList();

        foreach (var candidate in candidates)
        {
            if (randomValue < candidate.TriggerChance)
            {
                if (!candidate.Repeatable)
                    _consumed.Add(candidate.Id);
                return candidate;
            }
        }

        return null;
    }

    private static bool CheckCondition(EncounterCondition cond, EncounterContext ctx)
    {
        if (cond.RequiredSeason.HasValue && cond.RequiredSeason.Value != ctx.Season)
            return false;
        if (cond.RequiredWeather.HasValue && cond.RequiredWeather.Value != ctx.Weather)
            return false;
        if (cond.RequiredShichen.HasValue && cond.RequiredShichen.Value != ctx.Shichen)
            return false;
        if (cond.MinProgress.HasValue && ctx.Progress < cond.MinProgress.Value)
            return false;
        if (cond.MaxProgress.HasValue && ctx.Progress > cond.MaxProgress.Value)
            return false;
        if (cond.RequiredMindset != null && cond.RequiredMindset != ctx.Mindset)
            return false;
        return true;
    }
}
