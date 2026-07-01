using Xunit;
using FengZhi.Foundation.Cutscene;

namespace Foundation.Tests.Cutscene;

public class CutsceneQueueTests
{
    [Fact]
    public void Enqueue_Dequeue_FIFO_Order()
    {
        var queue = new CutsceneQueue();
        var s1 = new CutsceneScript { Id = "a", Tier = CutsceneTier.FullScreenPixel };
        var s2 = new CutsceneScript { Id = "b", Tier = CutsceneTier.FullScreenPixel };

        queue.Enqueue(s1);
        queue.Enqueue(s2);

        Assert.Equal(2, queue.Count);
        Assert.Equal("a", queue.Dequeue().CurrentScript.Id);
        Assert.Equal("b", queue.Dequeue().CurrentScript.Id);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void EnqueueChain_DequeuesAsSinglePlayable()
    {
        var queue = new CutsceneQueue();
        var scripts = new[]
        {
            new CutsceneScript { Id = "c1" },
            new CutsceneScript { Id = "c2" },
        };

        queue.EnqueueChain(scripts, 300);

        var playable = queue.Dequeue();
        Assert.Equal("c1", playable.CurrentScript.Id);
        Assert.True(playable.HasNext);
        playable.Advance();
        Assert.Equal("c2", playable.CurrentScript.Id);
        Assert.False(playable.HasNext);
    }

    [Fact]
    public void PruneStaleTier4_RemovesTier4BehindHighTier()
    {
        var queue = new CutsceneQueue();
        queue.Enqueue(new CutsceneScript { Id = "high", Tier = CutsceneTier.FullScreenPixel });
        queue.Enqueue(new CutsceneScript { Id = "micro", Tier = CutsceneTier.InlineMicro });

        queue.PruneStaleTier4();

        Assert.Equal(1, queue.Count);
        Assert.Equal("high", queue.Dequeue().CurrentScript.Id);
    }

    [Fact]
    public void PruneStaleTier4_KeepsTier4WithNoHighTierAhead()
    {
        var queue = new CutsceneQueue();
        queue.Enqueue(new CutsceneScript { Id = "micro1", Tier = CutsceneTier.InlineMicro });
        queue.Enqueue(new CutsceneScript { Id = "micro2", Tier = CutsceneTier.InlineMicro });

        queue.PruneStaleTier4();

        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void PruneStaleTier4_KeepsTier3()
    {
        var queue = new CutsceneQueue();
        queue.Enqueue(new CutsceneScript { Id = "high", Tier = CutsceneTier.FullScreenCg });
        queue.Enqueue(new CutsceneScript { Id = "half", Tier = CutsceneTier.HalfScreen });

        queue.PruneStaleTier4();

        Assert.Equal(2, queue.Count);
    }
}
