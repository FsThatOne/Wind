namespace FengZhi.Foundation.LivingJianghu;

public static class EventSelector
{
    public const int BacklogBonusPerDay = 5;
    public const int BacklogBonusCap = 25;

    public static int ComputeSelectionScore(
        WorldEventConfig config,
        EventInstance instance,
        string playerRegion,
        int currentChapter)
    {
        int tagScore = TagRelevanceScorer.ComputeScore(config.Tags, playerRegion, currentChapter);
        int backlogBonus = Math.Min(instance.BacklogDays * BacklogBonusPerDay, BacklogBonusCap);
        return config.Priority + tagScore + backlogBonus;
    }

    public static List<EventInstance> SelectDailyEvents(
        IReadOnlyList<(WorldEventConfig Config, EventInstance Instance)> candidates,
        int effectiveCap,
        int maxTypePerDay,
        string playerRegion,
        int currentChapter)
    {
        var scored = candidates
            .Select(c => (
                c.Config,
                c.Instance,
                Score: ComputeSelectionScore(c.Config, c.Instance, playerRegion, currentChapter)
            ))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => StableHash(x.Config.Id))
            .ToList();

        var selected = new List<EventInstance>();
        var typeCounts = new Dictionary<WorldEventType, int>();

        foreach (var (config, instance, _) in scored)
        {
            if (selected.Count >= effectiveCap)
                break;

            typeCounts.TryGetValue(config.Type, out int count);
            if (count >= maxTypePerDay)
                continue;

            selected.Add(instance);
            typeCounts[config.Type] = count + 1;
        }

        return selected;
    }

    public static int ComputeEffectiveCap(int dailyEventCap, int breathingMultiplier, bool isBreathing)
    {
        return dailyEventCap * (isBreathing ? breathingMultiplier : 1);
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in s)
                hash = hash * 31 + c;
            return hash;
        }
    }
}
