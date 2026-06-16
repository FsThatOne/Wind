using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueInsightTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void SpeechNode_WhenInsightMeetsThreshold_ExposesCueAfterTextCompletes()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(18))
        {
            CharactersPerTick = 100
        };
        var sequence = Load("""
id: insight_success
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "他说话时避开了你的眼睛。"
    insight_level_required: 15
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "他在隐瞒夜巡一事。", next: normal }
  - { id: normal, type: narration, text: "风声渐止。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();

        Assert.True(runtime.HasInsightCue);
        Assert.True(runtime.CanInvestigateInsight);
        Assert.Equal(new DialogueInsightCue("line", "hidden", 15, 18, CanInvestigate: true), runtime.CurrentInsightCue);
    }

    [Fact]
    public void SpeechNode_WhenInsightEqualsThreshold_TriggersCue()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(15))
        {
            CharactersPerTick = 100
        };
        var sequence = Load("""
id: insight_equal_threshold
version: 1
entry_node: line
nodes:
  - id: line
    type: narration
    text: "更鼓忽然停了。"
    insight_level_required: 15
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "有人故意压下了更声。", next: normal }
  - { id: normal, type: narration, text: "你继续前行。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();

        Assert.True(runtime.HasInsightCue);
        Assert.Equal(15, runtime.CurrentInsightCue?.PlayerInsight);
    }

    [Fact]
    public void SpeechNode_WhenInsightBelowThreshold_HasNoCueAndConfirmsNormally()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(14))
        {
            CharactersPerTick = 100
        };
        var sequence = Load("""
id: insight_low
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "他说话时避开了你的眼睛。"
    insight_level_required: 15
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "隐藏信息。", next: normal }
  - { id: normal, type: narration, text: "普通推进。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();

        Assert.False(runtime.HasInsightCue);
        Assert.False(runtime.CanInvestigateInsight);
        runtime.Confirm(frame: 1);
        Assert.Equal("normal", runtime.CurrentNode?.Id);
    }

    [Fact]
    public void ConfirmWhileCueVisible_IgnoresInsightAndUsesNormalNext()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(20))
        {
            CharactersPerTick = 100
        };
        var sequence = Load("""
id: insight_ignore
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "话里有话。"
    insight_level_required: 10
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "隐藏信息。", next: END }
  - { id: normal, type: narration, text: "普通推进。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();
        runtime.Confirm(frame: 1);

        Assert.Equal("normal", runtime.CurrentNode?.Id);
        Assert.False(runtime.HasInsightCue);
    }

    [Fact]
    public void InvestigateInsight_EntersInsightPromptAndDispatchesDiscoveredOnce()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var runtime = new DialogueRuntime(
            eventQueue: eventQueue,
            insightValueProvider: new FixedInsightProvider(20))
        {
            CharactersPerTick = 100
        };
        var discovered = new List<string>();
        eventBus.Subscribe<DialogueInsightDiscoveredEvent>(e => discovered.Add(e.InsightId));
        var sequence = Load("""
id: insight_investigate
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "话里有话。"
    insight_level_required: 10
    insight_next: hidden
    next: normal
  - { id: hidden, type: insight_prompt, text: "隐藏信息。", next: END }
  - { id: normal, type: narration, text: "普通推进。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();

        runtime.InvestigateInsight();
        runtime.InvestigateInsight();

        Assert.Equal("hidden", runtime.CurrentNode?.Id);
        Assert.Equal(1, eventQueue.PendingCount);
        runtime.Tick();
        runtime.Confirm(frame: 1);
        runtime.CompleteExit();
        runtime.RestoreWorldAndDispatchEvents();
        Assert.Equal(new[] { "hidden" }, discovered);
    }

    [Fact]
    public void ChoiceNode_WithInsightThreshold_DoesNotExposeCue()
    {
        var runtime = new DialogueRuntime(insightValueProvider: new FixedInsightProvider(99));
        var sequence = Load("""
id: choice_no_insight_cue
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    insight_level_required: 1
    options:
      - text: "沉默"
        next: END
""");

        runtime.Start(sequence);

        Assert.Equal(DialogueRuntimeState.ProcessingChoice, runtime.State);
        Assert.False(runtime.HasInsightCue);
    }

    [Fact]
    public void Schema_InsightCueSourceRequiresInsightNextToPromptNode()
    {
        var ex = Assert.Throws<FengZhi.Foundation.Data.DataLoadException>(() => Load("""
id: invalid_insight_next
version: 1
entry_node: line
nodes:
  - id: line
    type: speech
    text: "话里有话。"
    insight_level_required: 10
    insight_next: normal
    next: normal
  - { id: normal, type: narration, text: "普通推进。", next: END }
"""));

        Assert.Contains("insight_next", ex.Message);
        Assert.Contains("insight_prompt", ex.Message);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/insight.yaml");
    }

    private sealed class FixedInsightProvider : IDialogueInsightValueProvider
    {
        private readonly int _insight;

        public FixedInsightProvider(int insight)
        {
            _insight = insight;
        }

        public int GetPlayerInsight()
        {
            return _insight;
        }
    }
}
