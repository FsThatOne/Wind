using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class DialogueEventDispatchTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void NodeEvents_DoNotDispatchBeforeDialogueExitsAndWorldRestores()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var runtime = new DialogueRuntime(eventQueue: eventQueue) { CharactersPerTick = 100 };
        var received = new List<GameEvent>();
        eventBus.Subscribe<DialogueMindsetShiftEvent>(received.Add);
        eventBus.Subscribe<DialogueQuestFlagEvent>(received.Add);
        var sequence = Load("""
id: delayed_dispatch
version: 1
entry_node: a
nodes:
  - id: a
    type: narration
    text: "风起。"
    next: END
    events:
      - { type: mindset_shift, axis: firmness, delta: 1 }
      - { type: quest_flag, key: narrative_seen, value: true }
""");

        runtime.Start(sequence);
        runtime.Tick();
        runtime.Confirm(frame: 1);

        Assert.Empty(received);
        Assert.Equal(2, eventQueue.PendingCount);
        runtime.CompleteExit();
        Assert.Equal(2, runtime.RestoreWorldAndDispatchEvents());
        Assert.Equal(new[] { nameof(DialogueMindsetShiftEvent), nameof(DialogueQuestFlagEvent) }, received.Select(e => e.GetType().Name));
    }

    [Fact]
    public void DispatchPending_PublishesEachEventExactlyOnceInAuthoringOrder()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var received = new List<string>();
        eventBus.Subscribe<DialogueQuestFlagEvent>(e => received.Add($"flag:{e.Key}"));
        eventBus.Subscribe<DialogueItemGrantEvent>(e => received.Add($"item:{e.ItemId}:{e.Quantity}"));

        eventQueue.EnqueueRange(new[]
        {
            new DialogueEventSpec { Type = "quest_flag", Key = "a", Value = "true" },
            new DialogueEventSpec { Type = "item_grant", Key = "herb", Delta = 2 },
            new DialogueEventSpec { Type = "quest_flag", Key = "a", Value = "true" }
        });

        Assert.Equal(3, eventQueue.DispatchPending());
        Assert.Equal(new[] { "flag:a", "item:herb:2", "flag:a" }, received);
        Assert.Equal(0, eventQueue.DispatchPending());
    }

    [Fact]
    public void ChoiceSelection_EnqueuesNodeAndOptionEventsInStableOrder()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var runtime = new DialogueRuntime(eventQueue: eventQueue);
        var received = new List<string>();
        eventBus.Subscribe<DialogueQuestFlagEvent>(e => received.Add($"flag:{e.Key}"));
        eventBus.Subscribe<DialogueMindsetShiftEvent>(e => received.Add($"mindset:{e.Axis}:{e.Delta}"));
        var sequence = Load("""
id: choice_events
version: 1
entry_node: choose
nodes:
  - id: choose
    type: choice
    prompt: "如何回应？"
    next: END
    events:
      - { type: quest_flag, key: node_seen, value: true }
    options:
      - text: "出言"
        next: END
        events:
          - { type: mindset_shift, axis: kindness, delta: 2 }
""");

        runtime.Start(sequence);
        runtime.SelectOption(0);

        Assert.Empty(received);
        runtime.CompleteExit();
        runtime.RestoreWorldAndDispatchEvents();
        Assert.Equal(new[] { "flag:node_seen", "mindset:kindness:2" }, received);
    }

    [Fact]
    public void CombatTrigger_ExitsDialogueAndDispatchesAfterEarlierNodeEvents()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var runtime = new DialogueRuntime(eventQueue: eventQueue) { CharactersPerTick = 100 };
        var received = new List<string>();
        eventBus.Subscribe<DialogueQuestFlagEvent>(e => received.Add($"flag:{e.Key}"));
        eventBus.Subscribe<DialogueCombatTriggerEvent>(e => received.Add($"combat:{e.CombatId}"));
        var sequence = Load("""
id: combat_trigger
version: 1
entry_node: a
nodes:
  - id: a
    type: narration
    text: "拔剑。"
    next: b
    events:
      - { type: quest_flag, key: duel_started, value: true }
      - { type: combat_trigger, key: duel_001 }
  - id: b
    type: narration
    text: "战后再说。"
    next: END
""");

        runtime.Start(sequence);
        runtime.Tick();
        runtime.Confirm(frame: 1);

        Assert.Equal(DialogueRuntimeState.Exiting, runtime.State);
        Assert.Equal(2, eventQueue.PendingCount);
        runtime.CompleteExit();
        runtime.RestoreWorldAndDispatchEvents();
        Assert.Equal(new[] { "flag:duel_started", "combat:duel_001" }, received);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/events.yaml");
    }
}
