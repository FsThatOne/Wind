namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// Camera2D 请求。锁定目标、缩放与 smoothing 启用状态由请求字段定义；
/// Godot 集成层订阅 ActiveRequest 把数值映射到实际 Camera2D。
/// </summary>
public sealed record CameraRequest(
    string TargetId,
    float Zoom,
    bool DisableSmoothing,
    int Priority,
    string Reason);

/// <summary>
/// Camera 请求优先级栈。决胜推进、过场镜头都通过此栈协调；
/// 空栈时 ActiveRequest 为 null，集成层应恢复默认跟随。
/// </summary>
public sealed class CameraRequestBus
{
    private readonly List<RequestHandle> _stack = new();

    public event Action<CameraRequest?>? ActiveRequestChanged;

    public CameraRequest? ActiveRequest => _stack.Count == 0 ? null : _stack[^1].Request;

    public int ActiveRequestCount => _stack.Count;

    public IDisposable Request(string targetId, float zoom, bool disableSmoothing, int priority, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var handle = new RequestHandle(
            this,
            new CameraRequest(targetId, zoom, disableSmoothing, priority, reason));
        InsertByPriority(handle);
        NotifyChange();
        return handle;
    }

    private void InsertByPriority(RequestHandle handle)
    {
        int i = _stack.Count;
        while (i > 0 && _stack[i - 1].Request.Priority > handle.Request.Priority) i--;
        _stack.Insert(i, handle);
    }

    private void Release(RequestHandle handle)
    {
        if (_stack.Remove(handle))
            NotifyChange();
    }

    private void NotifyChange()
    {
        ActiveRequestChanged?.Invoke(ActiveRequest);
    }

    private sealed class RequestHandle : IDisposable
    {
        private readonly CameraRequestBus _owner;
        private bool _released;

        public RequestHandle(CameraRequestBus owner, CameraRequest request)
        {
            _owner = owner;
            Request = request;
        }

        public CameraRequest Request { get; }

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _owner.Release(this);
        }
    }
}
