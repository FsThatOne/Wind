using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeEndingSaveContractTest
{
    [Fact]
    public void ResolveEnding_UsesMindsetAndCompanionToBuildScriptKey()
    {
        var service = new NarrativeProgressionService(CreateEndingGraph(), new EventBus());
        var ending = MindsetService.DetermineEnding(resolve: 20, worldly: 20, morality: 0);

        var result = service.ResolveEnding(ending, new[]
        {
            new NarrativeCompanionBond("heroine_a", NarrativeCompanionEndingState.Companion)
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Selection);
        Assert.Equal(BaseEnding.DaYinYuShi, result.Selection!.BaseEnding);
        Assert.Equal("ending.da_yin_yu_shi.heroine_a", result.Selection.ScriptKey);
    }

    [Fact]
    public void ResolveEnding_RejectsMultipleCompanionBonds()
    {
        var service = new NarrativeProgressionService(CreateEndingGraph(), new EventBus());
        var ending = MindsetService.DetermineEnding(resolve: 20, worldly: 20, morality: 0);

        var result = service.ResolveEnding(ending, new[]
        {
            new NarrativeCompanionBond("heroine_a", NarrativeCompanionEndingState.Companion),
            new NarrativeCompanionBond("heroine_b", NarrativeCompanionEndingState.Companion)
        });

        Assert.False(result.Success);
        Assert.Contains("不互斥", result.Error);
    }

    [Fact]
    public void ResolveEnding_ChoiceLogAffectsEndingDialogueKeys()
    {
        var service = new NarrativeProgressionService(CreateEndingGraph(), new EventBus());
        service.Initialize();
        service.SelectBranch("choice", "route_a");
        var ending = MindsetService.DetermineEnding(resolve: -20, worldly: -20, morality: 0);

        var result = service.ResolveEnding(ending, Array.Empty<NarrativeCompanionBond>());

        Assert.True(result.Success);
        Assert.Contains("ending.choice.choice.route_a", result.Selection!.DialogueKeys);
    }

    [Fact]
    public void SerializeDeserialize_RestoresNodeProgressChoiceLogAndBreathingPeriod()
    {
        var service = new NarrativeProgressionService(CreateSaveGraph(), new EventBus());
        service.Initialize(currentDay: 1);
        service.SelectBranch("choice", "route_a", currentDay: 1);
        service.CompleteNode("key_node", currentDay: 3);

        var snapshot = service.Serialize();
        var restored = new NarrativeProgressionService(CreateSaveGraph(), new EventBus());
        restored.Deserialize(snapshot, version: 1);

        Assert.True(restored.State.IsCompleted("choice"));
        Assert.True(restored.State.IsCompleted("key_node"));
        Assert.Single(restored.State.ChoiceLog);
        Assert.True(restored.State.IsInBreathingPeriod);
        Assert.Equal("key_node", restored.State.BreathingPeriod!.SourceNodeId);
        Assert.Equal(3, restored.State.BreathingPeriod.StartedDay);
    }

    [Fact]
    public void SerializeDeserialize_RestoresTimeLimitCountdownFromSavedStartDay()
    {
        var service = new NarrativeProgressionService(CreateSaveGraph(), new EventBus());
        service.Initialize(currentDay: 1);
        service.SelectBranch("choice", "route_a", currentDay: 1);
        service.CompleteNode("key_node", currentDay: 3);
        service.TriggerNextFromBreathing(currentDay: 10);

        var snapshot = service.Serialize();
        var restored = new NarrativeProgressionService(CreateSaveGraph(), new EventBus());
        restored.Deserialize(snapshot, version: 1);

        Assert.True(restored.State.IsActive("timed_node"));
        Assert.True(restored.State.HasActiveTimeLimit("timed_node"));
        Assert.Equal(3, restored.GetRemainingDays("timed_node", currentDay: 12));
    }

    private static NarrativeGraph CreateEndingGraph()
    {
        return new NarrativeGraph
        {
            Id = "ending",
            EntryNode = "choice",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "choice",
                    Chapter = 1,
                    Type = NarrativeNodeType.Choice,
                    Branches = new[]
                    {
                        new NarrativeBranch { Id = "route_a", Title = "公开真相", Next = "after" }
                    }
                },
                new NarrativeNode { Id = "after", Chapter = 1, Type = NarrativeNodeType.Dialogue }
            }
        };
    }

    private static NarrativeGraph CreateSaveGraph()
    {
        return new NarrativeGraph
        {
            Id = "save_contract",
            EntryNode = "choice",
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "choice",
                    Chapter = 1,
                    Type = NarrativeNodeType.Choice,
                    Branches = new[]
                    {
                        new NarrativeBranch { Id = "route_a", Title = "追查旧信使", Next = "key_node" }
                    }
                },
                new NarrativeNode
                {
                    Id = "key_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Next = "timed_node",
                    OnComplete = new[]
                    {
                        new NarrativeEventSpec { Type = "enter_breathing", Delta = 7 }
                    }
                },
                new NarrativeNode
                {
                    Id = "timed_node",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "key_node" }
                    },
                    TimeLimit = new NarrativeTimeLimit { Days = 5 }
                }
            }
        };
    }
}
