namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// TimeScale 优先级栈控制器。任何系统通过 Request 取得 IDisposable 句柄，
/// 释放后栈顶切回下一个。CurrentScale 始终反映栈顶请求；空栈回到 1.0。
/// 修改 Engine.TimeScale 的 Tween 必须 SetProcessMode(TweenProcessMode.Always) — Godot 集成层职责。
/// </summary>
public sealed class TimeScaleController
{
    private readonly List<TimeScaleRequest> _stack = new();

    public event Action<double>? ScaleChanged;

    public double CurrentScale { get; private set; } = DecisiveTuning.TimeScaleNormal;

    public int CurrentPriority => _stack.Count == 0 ? DecisiveTuning.DefaultPriority : _stack[^1].Priority;

    public string? CurrentReason => _stack.Count == 0 ? null : _stack[^1].Reason;

    public int ActiveRequestCount => _stack.Count;

    public IDisposable Request(double scale, int priority, string reason)
    {
        if (scale < 0.0) throw new ArgumentOutOfRangeException(nameof(scale), "scale must be >= 0");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var request = new TimeScaleRequest(this, scale, priority, reason);
        InsertByPriority(request);
        UpdateCurrentScale();
        return request;
    }

    private void InsertByPriority(TimeScaleRequest request)
    {
        int i = _stack.Count;
        while (i > 0 && _stack[i - 1].Priority > request.Priority) i--;
        _stack.Insert(i, request);
    }

    private void Release(TimeScaleRequest request)
    {
        if (_stack.Remove(request))
            UpdateCurrentScale();
    }

    private void UpdateCurrentScale()
    {
        var next = _stack.Count == 0 ? DecisiveTuning.TimeScaleNormal : _stack[^1].Scale;
        if (Math.Abs(CurrentScale - next) < double.Epsilon) return;
        CurrentScale = next;
        ScaleChanged?.Invoke(next);
    }

    private sealed class TimeScaleRequest : IDisposable
    {
        private readonly TimeScaleController _owner;
        private bool _released;

        public TimeScaleRequest(TimeScaleController owner, double scale, int priority, string reason)
        {
            _owner = owner;
            Scale = scale;
            Priority = priority;
            Reason = reason;
        }

        public double Scale { get; }
        public int Priority { get; }
        public string Reason { get; }

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _owner.Release(this);
        }
    }
}
