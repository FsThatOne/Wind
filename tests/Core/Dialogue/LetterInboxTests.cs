using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using Xunit;

namespace FengZhi.Tests.Core.Dialogue;

public sealed class LetterInboxTests
{
    private readonly DialogueConfigLoader _loader = new();

    [Fact]
    public void LetterNode_EntersLetterReadingAndStoresLetterInInbox()
    {
        var inbox = new DialogueLetterInbox();
        var runtime = new DialogueRuntime(letterInbox: inbox);
        var sequence = Load("""
id: letter_flow
version: 1
entry_node: letter_01
nodes:
  - id: letter_01
    type: letter
    sender: bai_ling
    recipient: player
    text: "见字如面。"
    next: END
""");

        runtime.Start(sequence);

        Assert.Equal(DialogueRuntimeState.LetterReading, runtime.State);
        Assert.Equal("letter_01", runtime.CurrentLetter?.LetterId);
        Assert.Equal("bai_ling", runtime.CurrentLetter?.Sender);
        Assert.Equal("见字如面。", runtime.CurrentLetter?.Text);
        Assert.Equal(1, inbox.Count);
    }

    [Fact]
    public void LetterNode_WhenSameLetterArrivesAgain_DoesNotInsertDuplicate()
    {
        var inbox = new DialogueLetterInbox();
        var node = new DialogueNode
        {
            Id = "letter_01",
            Type = "letter",
            Sender = "bai_ling",
            Recipient = "player",
            Text = "见字如面。"
        };

        inbox.Receive(node);
        inbox.Receive(node);

        Assert.Equal(1, inbox.Count);
        Assert.Single(inbox.Letters);
    }

    [Fact]
    public void LetterNode_WithCodePhrase_LearnsPhraseOnlyOnFirstView()
    {
        var inbox = new DialogueLetterInbox();
        var phraseBook = new DialogueCodePhraseBook();
        var node = new DialogueNode
        {
            Id = "letter_01",
            Type = "letter",
            Text = "白蘋渡口，芦花如雪。"
        };
        node.CodePhraseIds.Add("white_reed");

        inbox.Receive(node, phraseBook);
        var learnedAgain = phraseBook.Learn("white_reed");
        inbox.Reread("letter_01");

        Assert.True(phraseBook.HasLearned("white_reed"));
        Assert.False(learnedAgain);
    }

    [Fact]
    public void Reread_ReturnsFullLetterWithoutEnqueuingEventsAgain()
    {
        var eventBus = new EventBus();
        var eventQueue = new DialogueEventQueue(eventBus);
        var inbox = new DialogueLetterInbox();
        var runtime = new DialogueRuntime(eventQueue: eventQueue, letterInbox: inbox);
        var sequence = Load("""
id: letter_events
version: 1
entry_node: letter_01
nodes:
  - id: letter_01
    type: letter
    sender: bai_ling
    recipient: player
    text: "见字如面。"
    next: END
    events:
      - { type: mindset_shift, axis: kindness, delta: 1 }
      - { type: quest_flag, key: letter_read, value: true }
""");

        runtime.Start(sequence);
        runtime.Confirm(frame: 1);
        Assert.Equal(2, eventQueue.PendingCount);

        var reread = inbox.Reread("letter_01");

        Assert.Equal("见字如面。", reread.Text);
        Assert.Equal(2, eventQueue.PendingCount);
    }

    [Fact]
    public void QueueArrival_DuringDialogue_DoesNotInterruptAndNotifiesAfterwardInOrder()
    {
        var inbox = new DialogueLetterInbox();
        var first = new DialogueNode { Id = "letter_01", Type = "letter", Text = "第一封。" };
        var second = new DialogueNode { Id = "letter_02", Type = "letter", Text = "第二封。" };

        inbox.QueueArrival(first);
        inbox.QueueArrival(second);

        Assert.Equal(2, inbox.PendingNotificationCount);
        Assert.Equal("letter_01", inbox.DequeuePendingNotification()?.Letter.LetterId);
        Assert.Equal("letter_02", inbox.DequeuePendingNotification()?.Letter.LetterId);
        Assert.Null(inbox.DequeuePendingNotification());
    }

    [Fact]
    public void LetterInbox_RoundTripsSerializableStateIncludingPendingNotifications()
    {
        var inbox = new DialogueLetterInbox();
        inbox.QueueArrival(new DialogueNode
        {
            Id = "letter_01",
            Type = "letter",
            Sender = "bai_ling",
            Recipient = "player",
            Text = "见字如面。",
            EmotionalClues = { "concern" },
            CodePhraseIds = { "white_reed" }
        });

        var restored = DialogueLetterInbox.FromState(inbox.ToState());
        var letter = restored.Reread("letter_01");

        Assert.Equal("见字如面。", letter.Text);
        Assert.Contains("white_reed", letter.CodePhraseIds);
        Assert.Contains("concern", letter.EmotionalClues);
        Assert.Equal("letter_01", restored.DequeuePendingNotification()?.Letter.LetterId);
    }

    private DialogueSequence Load(string yaml)
    {
        return _loader.LoadSequence(yaml, "assets/data/dialogues/test/letters.yaml");
    }
}
