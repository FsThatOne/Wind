using FengZhi.Foundation.SceneManagement;
using FengZhi.Foundation.TimeSystem;
using Xunit;

namespace Foundation.Tests.SceneManagement;

public class EncounterTriggerTests
{
    private EncounterTriggerSystem CreateSystem(params EncounterDefinition[] encounters)
    {
        var system = new EncounterTriggerSystem();
        system.RegisterRange(encounters);
        return system;
    }

    private EncounterContext DefaultContext() => new()
    {
        Season = Season.Spring,
        Weather = WeatherType.Clear,
        Shichen = Shichen.Wu,
        Progress = 0.5f,
        Mindset = "release"
    };

    // --- AC1: Condition fields ---

    [Fact]
    public void Condition_Season_Matches()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredSeason = Season.Spring }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
        Assert.Equal("e1", result!.Id);
    }

    [Fact]
    public void Condition_Season_Mismatch_ReturnsNull()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredSeason = Season.Winter }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.Null(result);
    }

    [Fact]
    public void Condition_Weather_Matches()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredWeather = WeatherType.Clear }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
    }

    [Fact]
    public void Condition_Shichen_Matches()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredShichen = Shichen.Wu }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
    }

    [Fact]
    public void Condition_Progress_InRange()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { MinProgress = 0.3f, MaxProgress = 0.7f }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
    }

    [Fact]
    public void Condition_Progress_OutOfRange()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { MinProgress = 0.8f }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.Null(result);
    }

    [Fact]
    public void Condition_Mindset_Matches()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredMindset = "release" }
        };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
    }

    // --- AC2: Priority ordering ---

    [Fact]
    public void Evaluate_HigherPriority_First()
    {
        var low = new EncounterDefinition { Id = "low", Priority = 1 };
        var high = new EncounterDefinition { Id = "high", Priority = 10 };
        var system = CreateSystem(low, high);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.Equal("high", result!.Id);
    }

    // --- AC3: Probability check ---

    [Fact]
    public void Evaluate_TriggerChance_Pass()
    {
        var enc = new EncounterDefinition { Id = "e1", Priority = 1, TriggerChance = 0.5f };
        var system = CreateSystem(enc);
        // randomValue (0.3) < triggerChance (0.5) → triggers
        var result = system.Evaluate(DefaultContext(), 0.3f);
        Assert.NotNull(result);
    }

    [Fact]
    public void Evaluate_TriggerChance_Fail()
    {
        var enc = new EncounterDefinition { Id = "e1", Priority = 1, TriggerChance = 0.5f };
        var system = CreateSystem(enc);
        // randomValue (0.7) >= triggerChance (0.5) → does not trigger
        var result = system.Evaluate(DefaultContext(), 0.7f);
        Assert.Null(result);
    }

    // --- AC4: Consumed (non-repeatable) ---

    [Fact]
    public void NonRepeatable_ConsumedAfterTrigger()
    {
        var enc = new EncounterDefinition { Id = "e1", Priority = 1, Repeatable = false };
        var system = CreateSystem(enc);

        var first = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(first);
        Assert.True(system.IsConsumed("e1"));

        var second = system.Evaluate(DefaultContext(), 0f);
        Assert.Null(second);
    }

    [Fact]
    public void Repeatable_NotConsumed()
    {
        var enc = new EncounterDefinition { Id = "e1", Priority = 1, Repeatable = true };
        var system = CreateSystem(enc);

        system.Evaluate(DefaultContext(), 0f);
        Assert.False(system.IsConsumed("e1"));

        var second = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(second);
    }

    // --- AC5: Empty condition = always matches ---

    [Fact]
    public void EmptyCondition_AlwaysMatches()
    {
        var enc = new EncounterDefinition { Id = "e1", Priority = 1 };
        var system = CreateSystem(enc);
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.NotNull(result);
    }

    // --- AC6: No match returns null ---

    [Fact]
    public void NoEncounters_ReturnsNull()
    {
        var system = new EncounterTriggerSystem();
        var result = system.Evaluate(DefaultContext(), 0f);
        Assert.Null(result);
    }

    [Fact]
    public void AllConditionsFail_ReturnsNull()
    {
        var enc = new EncounterDefinition
        {
            Id = "e1", Priority = 1,
            Condition = new EncounterCondition { RequiredSeason = Season.Winter }
        };
        var system = CreateSystem(enc);
        var ctx = DefaultContext(); // Season.Spring
        var result = system.Evaluate(ctx, 0f);
        Assert.Null(result);
    }
}
