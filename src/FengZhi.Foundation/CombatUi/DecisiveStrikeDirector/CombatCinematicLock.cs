namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// 战斗 cinematic 输入锁。演出期间屏蔽玩家输入，仅放行白名单系统操作。
/// 不直接修改 InputMap；Godot 集成层通过 IsAllowedDuringLock 过滤事件。
/// 本类只是数值契约。
/// </summary>
public sealed class CombatCinematicLock
{
    private static readonly IReadOnlySet<string> DefaultAllowedActions =
        new HashSet<string>(StringComparer.Ordinal) { "ui_pause", "ui_system_back" };

    private readonly Stack<LockHandle> _activeHandles = new();
    private readonly IReadOnlySet<string> _allowedActions;

    public CombatCinematicLock()
        : this(DefaultAllowedActions)
    {
    }

    public CombatCinematicLock(IReadOnlySet<string> allowedActions)
    {
        ArgumentNullException.ThrowIfNull(allowedActions);
        _allowedActions = allowedActions;
    }

    public bool IsLocked => _activeHandles.Count > 0;

    public string? CurrentReason => _activeHandles.Count > 0 ? _activeHandles.Peek().Reason : null;

    public IReadOnlySet<string> AllowedActionsDuringLock => _allowedActions;

    public IDisposable Acquire(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var handle = new LockHandle(this, reason);
        _activeHandles.Push(handle);
        return handle;
    }

    public bool IsAllowedDuringLock(string action)
    {
        if (string.IsNullOrEmpty(action)) return false;
        if (!IsLocked) return true;
        return _allowedActions.Contains(action);
    }

    private void Release(LockHandle handle)
    {
        if (_activeHandles.Count == 0) return;
        if (ReferenceEquals(_activeHandles.Peek(), handle))
        {
            _activeHandles.Pop();
            return;
        }

        var buffer = new List<LockHandle>(_activeHandles.Count);
        while (_activeHandles.Count > 0)
        {
            var top = _activeHandles.Pop();
            if (ReferenceEquals(top, handle)) break;
            buffer.Add(top);
        }
        for (int i = buffer.Count - 1; i >= 0; i--)
            _activeHandles.Push(buffer[i]);
    }

    private sealed class LockHandle : IDisposable
    {
        private readonly CombatCinematicLock _owner;
        private bool _released;

        public LockHandle(CombatCinematicLock owner, string reason)
        {
            _owner = owner;
            Reason = reason;
        }

        public string Reason { get; }

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _owner.Release(this);
        }
    }
}
