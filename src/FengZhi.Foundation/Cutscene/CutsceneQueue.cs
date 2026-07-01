namespace FengZhi.Foundation.Cutscene;

public sealed class CutsceneQueue
{
    private readonly Queue<ICutscenePlayable> _queue = new();

    public bool IsEmpty => _queue.Count == 0;
    public int Count => _queue.Count;

    public void Enqueue(CutsceneScript script)
    {
        _queue.Enqueue(new SingleCutsceneRequest(script));
    }

    public void EnqueueChain(CutsceneScript[] scripts, int transitionGapMs = 500)
    {
        _queue.Enqueue(new CutsceneChainRequest(scripts, transitionGapMs));
    }

    public void EnqueuePlayable(ICutscenePlayable playable)
    {
        _queue.Enqueue(playable);
    }

    public ICutscenePlayable Dequeue() => _queue.Dequeue();

    public ICutscenePlayable Peek() => _queue.Peek();

    public void PruneStaleTier4()
    {
        if (_queue.Count <= 1) return;

        var items = _queue.ToArray();
        _queue.Clear();

        bool hasHighTierAhead = false;
        foreach (var item in items)
        {
            var tier = item.CurrentScript.Tier;
            if (tier <= CutsceneTier.FullScreenPixel)
            {
                hasHighTierAhead = true;
                _queue.Enqueue(item);
            }
            else if (tier == CutsceneTier.InlineMicro && hasHighTierAhead)
            {
                // Tier4 排在高 Tier 后面，丢弃（E6）
                continue;
            }
            else
            {
                _queue.Enqueue(item);
            }
        }
    }

    public void Clear() => _queue.Clear();
}
