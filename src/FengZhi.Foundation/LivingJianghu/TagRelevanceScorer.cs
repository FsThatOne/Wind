namespace FengZhi.Foundation.LivingJianghu;

public static class TagRelevanceScorer
{
    public const int RegionWeight = 2;
    public const int ChapterWeight = 1;

    public static int ComputeScore(
        IReadOnlyList<string> eventTags,
        string playerRegion,
        int currentChapter)
    {
        int regionMatches = 0;
        int chapterMatches = 0;
        string chapterTag = $"ch{currentChapter}";

        for (int i = 0; i < eventTags.Count; i++)
        {
            string tag = eventTags[i];
            if (string.Equals(tag, playerRegion, StringComparison.OrdinalIgnoreCase))
                regionMatches++;
            if (string.Equals(tag, chapterTag, StringComparison.OrdinalIgnoreCase))
                chapterMatches++;
        }

        return regionMatches * RegionWeight + chapterMatches * ChapterWeight;
    }
}
