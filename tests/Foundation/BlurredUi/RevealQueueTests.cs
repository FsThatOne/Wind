using FengZhi.Foundation.BlurredUi;
using Xunit;

namespace Foundation.Tests.BlurredUi;

public class RevealQueueTests
{
    [Fact]
    public void Enqueue_And_Dequeue_Fifo()
    {
        var queue = new RevealQueue();
        var r1 = new PendingReveal { Channel = RevealChannel.Realm, Message = "first", Timestamp = 1 };
        var r2 = new PendingReveal { Channel = RevealChannel.Mindset, Message = "second", Timestamp = 2 };

        queue.Enqueue(r1);
        queue.Enqueue(r2);

        Assert.Equal(2, queue.Count);
        Assert.Same(r1, queue.Dequeue());
        Assert.Same(r2, queue.Dequeue());
        Assert.Null(queue.Dequeue());
    }

    [Fact]
    public void DequeueByChannel_SkipsOtherChannels()
    {
        var queue = new RevealQueue();
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Message = "realm" });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Mindset, Message = "mindset" });

        var result = queue.DequeueByChannel(RevealChannel.Mindset);
        Assert.Equal("mindset", result!.Message);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void NeedsMerge_TrueWhenExceedsThreshold()
    {
        var queue = new RevealQueue(mergeThreshold: 2);
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Timestamp = 1 });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Timestamp = 2 });

        Assert.False(queue.NeedsMerge(RevealChannel.Realm));

        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Timestamp = 3 });
        Assert.True(queue.NeedsMerge(RevealChannel.Realm));
    }

    [Fact]
    public void MergeChannel_ReplacesAllWithMerged()
    {
        var queue = new RevealQueue(mergeThreshold: 2);
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Mindset, Message = "a" });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Mindset, Message = "b" });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Mindset, Message = "c" });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Message = "keep" });

        var merged = new PendingReveal { Channel = RevealChannel.Mindset, Message = "merged" };
        queue.MergeChannel(RevealChannel.Mindset, merged);

        Assert.Equal(2, queue.Count);
        Assert.Equal("keep", queue.PeekByChannel(RevealChannel.Realm)!.Message);
        Assert.Equal("merged", queue.PeekByChannel(RevealChannel.Mindset)!.Message);
    }

    [Fact]
    public void ExportAll_And_ImportAll_RoundTrips()
    {
        var queue = new RevealQueue();
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Realm, Message = "r1" });
        queue.Enqueue(new PendingReveal { Channel = RevealChannel.Relationship, Message = "r2" });

        var exported = queue.ExportAll();
        var newQueue = new RevealQueue();
        newQueue.ImportAll(exported);

        Assert.Equal(2, newQueue.Count);
        Assert.Equal("r1", newQueue.Dequeue()!.Message);
        Assert.Equal("r2", newQueue.Dequeue()!.Message);
    }
}
