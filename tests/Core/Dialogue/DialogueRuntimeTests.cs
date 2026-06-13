using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueRuntimeTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void SpeechNarrationAndInnerMonologue_AdvanceThroughDisplayingAndWaiting()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 100 };
        var sequence = Load("""
id: normal_flow
version: 1
entry_node: speech_01
nodes:
  - { id: speech_01, type: speech, speaker: master_li, text: "一。", next: narration_01 }
  - { id: narration_01, type: narration, text: "二。", next: mind_01 }
  - { id: mind_01, type: inner_monologue, text: "三。", next: END }
""");

        runtime.Start(sequence);

        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);
        runtime.Tick();
        Assert.Equal(DialogueRuntimeState.WaitingForInput, runtime.State);

        runtime.Confirm(frame: 1);
        Assert.Equal("narration_01", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);

        runtime.Tick();
        runtime.Confirm(frame: 2);
        Assert.Equal("mind_01", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);
    }

    [Fact]
    public void ConfirmDuringTypewriter_RevealsFullTextWithoutAdvancingNode()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 1 };
        var sequence = Load("""
id: typewriter_skip
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "尚未说完的话", next: b }
  - { id: b, type: speech, text: "下一句。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();

        var snapshot = runtime.Confirm(frame: 10);

        Assert.Equal("a", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.WaitingForInput, runtime.State);
        Assert.Equal("尚未说完的话", snapshot.VisibleText);
        Assert.True(snapshot.IsTextComplete);
    }

    [Fact]
    public void ConfirmAfterTextComplete_AdvancesToNextNodeOnlyOnSecondConfirm()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 1 };
        var sequence = Load("""
id: second_confirm
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "第一句。", next: b }
  - { id: b, type: speech, text: "第二句。", next: END }
""");

        runtime.Start(sequence);

        runtime.Confirm(frame: 1);
        Assert.Equal("a", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.WaitingForInput, runtime.State);

        runtime.Confirm(frame: 2);
        Assert.Equal("b", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.Displaying, runtime.State);
    }

    [Fact]
    public void MultipleConfirmsInSameFrame_DoNotSkipAcrossNodes()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 1 };
        var sequence = Load("""
id: input_throttle
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "第一句。", next: b }
  - { id: b, type: speech, text: "第二句。", next: END }
""");

        runtime.Start(sequence);

        runtime.Confirm(frame: 7);
        runtime.Confirm(frame: 7);

        Assert.Equal("a", runtime.CurrentNode?.Id);
        Assert.Equal(DialogueRuntimeState.WaitingForInput, runtime.State);
    }

    [Fact]
    public void EndOrMissingNext_ExitsAndReturnsToIdle()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 100 };
        var sequence = Load("""
id: end_flow
version: 1
entry_node: a
nodes:
  - { id: a, type: narration, text: "终。", next: END }
""");

        runtime.Start(sequence);
        runtime.Tick();
        runtime.Confirm(frame: 1);

        Assert.Equal(DialogueRuntimeState.Exiting, runtime.State);
        runtime.CompleteExit();
        Assert.Equal(DialogueRuntimeState.Idle, runtime.State);
    }

    [Fact]
    public void VisitingFiveHundredthNode_ForcesExitAndRecordsError()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 100 };
        var sequence = Load("""
id: cyclic_runtime
version: 1
entry_node: a
nodes:
  - { id: a, type: narration, text: "转。", next: a }
""");

        runtime.Start(sequence);
        while (runtime.State != DialogueRuntimeState.Exiting)
        {
            runtime.Tick();
            runtime.Confirm(runtime.VisitedNodeCount);
        }

        Assert.Equal(DialogueConfigLoader.MaxNodeVisitsPerRun, runtime.VisitedNodeCount);
        Assert.Contains(runtime.Errors, e => e.Contains("达到上限", StringComparison.Ordinal));
    }

    [Fact]
    public void InstantTypewriter_AllowsConfirmToAdvanceAfterNodeIsAlreadyComplete()
    {
        var runtime = new DialogueRuntime { CharactersPerTick = 0 };
        var sequence = Load("""
id: instant_typewriter
version: 1
entry_node: a
nodes:
  - { id: a, type: speech, text: "瞬间。", next: b }
  - { id: b, type: speech, text: "下一句。", next: END }
""");

        runtime.Start(sequence);

        Assert.Equal(DialogueRuntimeState.WaitingForInput, runtime.State);
        runtime.Confirm(frame: 1);
        Assert.Equal("b", runtime.CurrentNode?.Id);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/runtime.yaml");
    }
}
