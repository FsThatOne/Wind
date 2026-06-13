using FengZhi.Foundation.Events;
using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public class NarrativeChapterManagerTest
{
    [Fact]
    public void CompletingChapterGate_ActivatesNextChapterAndPublishesChapterChanged()
    {
        var bus = new EventBus();
        var events = new List<NarrativeChapterChangedEvent>();
        bus.Subscribe<NarrativeChapterChangedEvent>(events.Add);
        var service = new NarrativeProgressionService(CreateChapterGraph(), bus);
        service.Initialize();

        var completed = service.CompleteNode("ch1_gate");

        Assert.True(completed);
        Assert.True(service.State.IsActive("ch2_start"));
        Assert.Equal(2, service.State.CurrentChapter);
        Assert.Single(events);
        Assert.Equal(1, events[0].OldChapter);
        Assert.Equal(2, events[0].NewChapter);
        Assert.Equal("江南", events[0].ChapterTitle);
    }

    [Fact]
    public void ChapterChange_PublishesToneAndLocationUnlockEvents()
    {
        var bus = new EventBus();
        var tones = new List<NarrativeToneChangedEvent>();
        var locations = new List<NarrativeLocationUnlockedEvent>();
        bus.Subscribe<NarrativeToneChangedEvent>(tones.Add);
        bus.Subscribe<NarrativeLocationUnlockedEvent>(locations.Add);
        var service = new NarrativeProgressionService(CreateChapterGraph(), bus);
        service.Initialize();

        service.CompleteNode("ch1_gate");

        Assert.Single(tones);
        Assert.Equal("jiangnan-warm-rain", tones[0].Tone);
        Assert.Equal(new[] { "jingchuan", "gusu" }, locations.Select(e => e.LocationId));
    }

    [Fact]
    public void ChapterChange_RequestsTransitionByEventOnly()
    {
        var bus = new EventBus();
        var transitions = new List<NarrativeChapterTransitionRequestedEvent>();
        bus.Subscribe<NarrativeChapterTransitionRequestedEvent>(transitions.Add);
        var service = new NarrativeProgressionService(CreateChapterGraph(), bus);
        service.Initialize();

        service.CompleteNode("ch1_gate");

        Assert.Single(transitions);
        Assert.Equal("chapter-card-jiangnan", transitions[0].TransitionKey);
        Assert.Equal(1, transitions[0].OldChapter);
        Assert.Equal(2, transitions[0].NewChapter);
    }

    [Fact]
    public void TryActivateNode_RejectsNodeFromCompletedPreviousChapter()
    {
        var service = new NarrativeProgressionService(CreateChapterGraph(), new EventBus());
        service.Initialize();
        service.CompleteNode("ch1_gate");

        var result = service.TryActivateNode("ch1_optional");

        Assert.False(result);
        Assert.False(service.State.IsActive("ch1_optional"));
    }

    [Fact]
    public void TryActivateNode_CanMoveForwardOnlyOnce()
    {
        var bus = new EventBus();
        var events = new List<NarrativeChapterChangedEvent>();
        bus.Subscribe<NarrativeChapterChangedEvent>(events.Add);
        var service = new NarrativeProgressionService(CreateChapterGraph(), bus);
        service.Initialize();

        service.CompleteNode("ch1_gate");
        var repeated = service.TryActivateNode("ch2_start");

        Assert.False(repeated);
        Assert.Single(events);
    }

    private static NarrativeGraph CreateChapterGraph()
    {
        return new NarrativeGraph
        {
            Id = "chapters",
            EntryNode = "ch1_gate",
            Chapters = new[]
            {
                new NarrativeChapter
                {
                    Chapter = 1,
                    Title = "风止",
                    Tone = "valley-peace",
                    TransitionKey = "chapter-card-prologue",
                    UnlockedLocations = new[] { "luochen_valley" }
                },
                new NarrativeChapter
                {
                    Chapter = 2,
                    Title = "江南",
                    Tone = "jiangnan-warm-rain",
                    TransitionKey = "chapter-card-jiangnan",
                    UnlockedLocations = new[] { "jingchuan", "gusu" }
                }
            },
            Nodes = new[]
            {
                new NarrativeNode
                {
                    Id = "ch1_gate",
                    Chapter = 1,
                    Type = NarrativeNodeType.Gate,
                    Next = "ch2_start",
                    Gate = new GateRequirement { RequiredBranches = 0, BranchNodeIds = Array.Empty<string>() }
                },
                new NarrativeNode
                {
                    Id = "ch1_optional",
                    Chapter = 1,
                    Type = NarrativeNodeType.Dialogue
                },
                new NarrativeNode
                {
                    Id = "ch2_start",
                    Chapter = 2,
                    Type = NarrativeNodeType.Dialogue,
                    Preconditions = new[]
                    {
                        new NarrativeCondition { Kind = NarrativeConditionKind.NodeCompleted, Key = "ch1_gate" }
                    }
                }
            }
        };
    }
}
