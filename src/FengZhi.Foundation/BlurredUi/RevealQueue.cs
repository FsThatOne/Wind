namespace FengZhi.Foundation.BlurredUi;

public sealed class RevealQueue
{
    public const int DefaultMergeThreshold = 3;

    private readonly List<PendingReveal> _queue = new();
    private readonly int _mergeThreshold;

    public RevealQueue(int mergeThreshold = DefaultMergeThreshold)
    {
        _mergeThreshold = mergeThreshold;
    }

    public int Count => _queue.Count;
    public bool HasPending => _queue.Count > 0;
    public IReadOnlyList<PendingReveal> PendingReveals => _queue;

    public void Enqueue(PendingReveal reveal)
    {
        _queue.Add(reveal);
    }

    public PendingReveal? Dequeue()
    {
        if (_queue.Count == 0) return null;
        var item = _queue[0];
        _queue.RemoveAt(0);
        return item;
    }

    public bool NeedsMerge(RevealChannel channel)
    {
        int count = 0;
        foreach (var r in _queue)
        {
            if (r.Channel == channel) count++;
        }
        return count > _mergeThreshold;
    }

    public void MergeChannel(RevealChannel channel, PendingReveal merged)
    {
        _queue.RemoveAll(r => r.Channel == channel);
        _queue.Add(merged);
    }

    public PendingReveal? PeekByChannel(RevealChannel channel)
    {
        foreach (var r in _queue)
        {
            if (r.Channel == channel) return r;
        }
        return null;
    }

    public PendingReveal? DequeueByChannel(RevealChannel channel)
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            if (_queue[i].Channel == channel)
            {
                var item = _queue[i];
                _queue.RemoveAt(i);
                return item;
            }
        }
        return null;
    }

    public void Clear()
    {
        _queue.Clear();
    }

    public List<PendingReveal> ExportAll()
    {
        return new List<PendingReveal>(_queue);
    }

    public void ImportAll(IEnumerable<PendingReveal> reveals)
    {
        _queue.Clear();
        _queue.AddRange(reveals);
    }
}
