namespace FengZhi.Foundation.LivingJianghu;

public enum PreconditionType
{
    Flag,
    NotFlag,
    Chapter,
    ChapterRange,
    DayElapsedSince,
    NpcState,
    Season,
    InBreathing,
    PlayerRegion,
    MindsetZone
}

public sealed class Precondition
{
    public PreconditionType Type { get; init; }
    public string? StringValue { get; init; }
    public int? IntValue { get; init; }
    public int? IntValue2 { get; init; }
    public string? NpcId { get; init; }
    public string? Axis { get; init; }
    public bool? BoolValue { get; init; }
}

public static class PreconditionEvaluator
{
    public static bool EvaluateAll(
        IReadOnlyList<Precondition> preconditions,
        ILivingJianghuContext context)
    {
        for (int i = 0; i < preconditions.Count; i++)
        {
            if (!Evaluate(preconditions[i], context))
                return false;
        }
        return true;
    }

    public static bool Evaluate(Precondition precondition, ILivingJianghuContext context)
    {
        return precondition.Type switch
        {
            PreconditionType.Flag =>
                context.HasFlag(precondition.StringValue!),

            PreconditionType.NotFlag =>
                !context.HasFlag(precondition.StringValue!),

            PreconditionType.Chapter =>
                context.GetCurrentChapter() == precondition.IntValue!.Value,

            PreconditionType.ChapterRange =>
                context.GetCurrentChapter() >= precondition.IntValue!.Value
                && context.GetCurrentChapter() <= precondition.IntValue2!.Value,

            PreconditionType.DayElapsedSince =>
                context.HasFlag(precondition.StringValue!)
                && context.GetDaysSinceFlag(precondition.StringValue!) >= precondition.IntValue!.Value,

            PreconditionType.NpcState =>
                context.GetNpcAxisValue(precondition.NpcId!, precondition.Axis!) == precondition.StringValue,

            PreconditionType.Season =>
                context.GetCurrentSeason().ToString().ToLowerInvariant() == precondition.StringValue?.ToLowerInvariant(),

            PreconditionType.InBreathing =>
                context.IsBreathing() == precondition.BoolValue!.Value,

            PreconditionType.PlayerRegion =>
                context.GetPlayerRegion() == precondition.StringValue,

            PreconditionType.MindsetZone =>
                context.GetMindsetZone() == precondition.StringValue,

            _ => false
        };
    }
}
