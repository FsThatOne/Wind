using FengZhi.Foundation.LivingJianghu;
using Xunit;

namespace Foundation.Tests.LivingJianghu;

public class PreconditionTests
{
    private readonly FakeContext _ctx = new();

    [Fact]
    public void Flag_ReturnsTrueWhenFlagExists()
    {
        _ctx.Flags.Add("quest_done");
        var p = new Precondition { Type = PreconditionType.Flag, StringValue = "quest_done" };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void Flag_ReturnsFalseWhenFlagMissing()
    {
        var p = new Precondition { Type = PreconditionType.Flag, StringValue = "quest_done" };
        Assert.False(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void NotFlag_ReturnsTrueWhenFlagMissing()
    {
        var p = new Precondition { Type = PreconditionType.NotFlag, StringValue = "quest_done" };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void Chapter_MatchesExactChapter()
    {
        _ctx.CurrentChapter = 2;
        var p = new Precondition { Type = PreconditionType.Chapter, IntValue = 2 };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void ChapterRange_MatchesWithinRange()
    {
        _ctx.CurrentChapter = 2;
        var p = new Precondition { Type = PreconditionType.ChapterRange, IntValue = 1, IntValue2 = 3 };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void ChapterRange_FailsOutsideRange()
    {
        _ctx.CurrentChapter = 4;
        var p = new Precondition { Type = PreconditionType.ChapterRange, IntValue = 1, IntValue2 = 3 };
        Assert.False(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void DayElapsedSince_CorrectlyComputes()
    {
        _ctx.Flags.Add("event_start");
        _ctx.FlagDays["event_start"] = 5;
        var p = new Precondition { Type = PreconditionType.DayElapsedSince, StringValue = "event_start", IntValue = 3 };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void DayElapsedSince_FailsWhenNotEnoughDays()
    {
        _ctx.Flags.Add("event_start");
        _ctx.FlagDays["event_start"] = 2;
        var p = new Precondition { Type = PreconditionType.DayElapsedSince, StringValue = "event_start", IntValue = 3 };
        Assert.False(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void Season_Matches()
    {
        _ctx.CurrentSeason = Season.Autumn;
        var p = new Precondition { Type = PreconditionType.Season, StringValue = "autumn" };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void InBreathing_MatchesTrue()
    {
        _ctx.Breathing = true;
        var p = new Precondition { Type = PreconditionType.InBreathing, BoolValue = true };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void PlayerRegion_Matches()
    {
        _ctx.Region = "jiangnan";
        var p = new Precondition { Type = PreconditionType.PlayerRegion, StringValue = "jiangnan" };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void MindsetZone_Matches()
    {
        _ctx.Mindset = "lone_sword";
        var p = new Precondition { Type = PreconditionType.MindsetZone, StringValue = "lone_sword" };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }

    [Fact]
    public void EvaluateAll_ANDLogic_AllMustPass()
    {
        _ctx.Flags.Add("flag_a");
        _ctx.CurrentChapter = 1;
        var preconditions = new List<Precondition>
        {
            new() { Type = PreconditionType.Flag, StringValue = "flag_a" },
            new() { Type = PreconditionType.Chapter, IntValue = 1 }
        };
        Assert.True(PreconditionEvaluator.EvaluateAll(preconditions, _ctx));
    }

    [Fact]
    public void EvaluateAll_ANDLogic_OneFailsMeansAllFail()
    {
        _ctx.CurrentChapter = 2;
        var preconditions = new List<Precondition>
        {
            new() { Type = PreconditionType.Flag, StringValue = "missing_flag" },
            new() { Type = PreconditionType.Chapter, IntValue = 2 }
        };
        Assert.False(PreconditionEvaluator.EvaluateAll(preconditions, _ctx));
    }

    [Fact]
    public void NpcState_Matches()
    {
        _ctx.NpcStates[("companion_a", "presence")] = "away";
        var p = new Precondition
        {
            Type = PreconditionType.NpcState,
            NpcId = "companion_a",
            Axis = "presence",
            StringValue = "away"
        };
        Assert.True(PreconditionEvaluator.Evaluate(p, _ctx));
    }
}

internal class FakeContext : ILivingJianghuContext
{
    public HashSet<string> Flags { get; } = [];
    public Dictionary<string, int> FlagDays { get; } = new();
    public int CurrentChapter { get; set; } = 1;
    public bool Breathing { get; set; }
    public int Day { get; set; } = 1;
    public Season CurrentSeason { get; set; } = Season.Spring;
    public string Region { get; set; } = "jiangnan";
    public string Mindset { get; set; } = "neutral";
    public Dictionary<(string, string), string> NpcStates { get; } = new();
    public Dictionary<(string, string), int> RegionDistances { get; } = new();

    public bool HasFlag(string flag) => Flags.Contains(flag);
    public void SetFlag(string flag) => Flags.Add(flag);
    public int GetDaysSinceFlag(string flag) => FlagDays.TryGetValue(flag, out int d) ? d : 0;
    public int GetCurrentChapter() => CurrentChapter;
    public bool IsBreathing() => Breathing;
    public int GetCurrentDay() => Day;
    public Season GetCurrentSeason() => CurrentSeason;
    public string GetPlayerRegion() => Region;
    public int GetRegionDistance(string from, string to)
    {
        if (from == to) return 0;
        return RegionDistances.TryGetValue((from, to), out int dist) ? dist : 1;
    }
    public string? GetNpcAxisValue(string npcId, string axis) =>
        NpcStates.TryGetValue((npcId, axis), out string? v) ? v : null;
    public string GetMindsetZone() => Mindset;
}
