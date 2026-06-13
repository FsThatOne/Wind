using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeCombatOutcomeTest
{
    [Fact]
    public void LethalCombat_DefeatRequestsDeathEndingAndDoesNotAdvance()
    {
        var bus = new EventBus();
        var deaths = new List<NarrativeDeathEndingRequestedEvent>();
        bus.Subscribe<NarrativeDeathEndingRequestedEvent>(deaths.Add);
        var service = new NarrativeProgressionService(
            CreateCombatGraph(NarrativeCombatOutcomeType.Lethal),
            bus);
        service.Initialize();

        var result = service.HandleCombatOutcome("combat", BattleResult.Defeat);

        Assert.True(result);
        Assert.True(service.State.IsActive("combat"));
        Assert.False(service.State.IsCompleted("combat"));
        Assert.False(service.State.IsActive("after"));
        Assert.Single(deaths);
        Assert.Equal(BattleResult.Defeat, deaths[0].Result);
    }

    [Fact]
    public void ScriptedCombat_AdvancesOnDefeat()
    {
        var service = new NarrativeProgressionService(
            CreateCombatGraph(NarrativeCombatOutcomeType.Scripted),
            new EventBus());
        service.Initialize();

        var result = service.HandleCombatOutcome("combat", BattleResult.Defeat);

        Assert.True(result);
        Assert.True(service.State.IsCompleted("combat"));
        Assert.True(service.State.IsActive("after"));
    }

    [Fact]
    public void NonLethalCombat_WritesOutcomeToChoiceLog()
    {
        var bus = new EventBus();
        var choices = new List<NarrativeChoiceLoggedEvent>();
        bus.Subscribe<NarrativeChoiceLoggedEvent>(choices.Add);
        var service = new NarrativeProgressionService(
            CreateCombatGraph(NarrativeCombatOutcomeType.NonLethal),
            bus);
        service.Initialize();

        var result = service.HandleCombatOutcome("combat", BattleResult.NarrowDefeat);

        Assert.True(result);
        Assert.Single(service.State.ChoiceLog);
        Assert.Equal("combat", service.State.ChoiceLog[0].NodeId);
        Assert.Equal("combat_narrow_defeat", service.State.ChoiceLog[0].BranchId);
        Assert.True(service.State.IsActive("after"));
        Assert.Single(choices);
    }

    [Fact]
    public void ScriptedCombat_VictoryPublishesConfiguredRewardEvent()
    {
        var bus = new EventBus();
        var rewards = new List<NarrativeScriptedCombatRewardEvent>();
        bus.Subscribe<NarrativeScriptedCombatRewardEvent>(rewards.Add);
        var service = new NarrativeProgressionService(
            CreateCombatGraph(NarrativeCombatOutcomeType.Scripted, rewardKey: "reward.hidden_manual"),
            bus);
        service.Initialize();

        var result = service.HandleCombatOutcome("combat", BattleResult.Victory);

        Assert.True(result);
        Assert.True(service.State.IsCompleted("combat"));
        Assert.Single(rewards);
        Assert.Equal("reward.hidden_manual", rewards[0].RewardKey);
    }

    [Fact]
    public void LethalCombat_VictoryCompletesAndAdvances()
    {
        var service = new NarrativeProgressionService(
            CreateCombatGraph(NarrativeCombatOutcomeType.Lethal),
            new EventBus());
        service.Initialize();

        var result = service.HandleCombatOutcome("combat", BattleResult.Victory);

        Assert.True(result);
        Assert.True(service.State.IsCompleted("combat"));
        Assert.True(service.State.IsActive("after"));
    }

    private static NarrativeGraph CreateCombatGraph(
        NarrativeCombatOutcomeType outcomeType,
        string? rewardKey = null)
    {
        return new NarrativeGraph
        {
            Id = "combat_outcome",
            EntryNode = "combat",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "combat",
                    Chapter = 1,
                    Type = NarrativeNodeType.Combat,
                    Next = "after",
                    CombatOutcome = new NarrativeCombatOutcome
                    {
                        Type = outcomeType,
                        ScriptedVictoryRewardKey = rewardKey
                    }
                },
                new NarrativeNode
                {
                    Id = "after",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "combat" }
                    }
                }
            }
        };
    }
}
