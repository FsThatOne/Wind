namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanySaveData
{
    public Dictionary<string, EpiphanyEventSaveEntry> EventStates { get; init; } = new();
    public Dictionary<int, int> ChapterUsed { get; init; } = new();
    public int MeditationCount { get; init; }
}

public sealed class EpiphanyEventSaveEntry
{
    public EpiphanyEventState State { get; init; }
    public int SkipCount { get; init; }
}
