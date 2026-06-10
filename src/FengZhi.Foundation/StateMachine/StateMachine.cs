namespace FengZhi.Foundation.StateMachine;

/// <summary>
/// 状态转换记录，用于 History 追踪。
/// </summary>
public sealed record TransitionRecord<TState>(
    TState From,
    TState To,
    string? Trigger,
    DateTime Timestamp
) where TState : notnull;

/// <summary>
/// 泛型有限状态机基类。ADR-0008。
/// 纯 C# 实现，不依赖 Godot Node。
/// </summary>
public sealed class StateMachine<TState> where TState : notnull
{
    private readonly Dictionary<TState, Dictionary<string, TransitionRule<TState>>> _transitions = new();
    private readonly HashSet<TState> _registeredStates = new();
    private readonly List<TransitionRecord<TState>> _history = new();

    /// <summary>历史记录最大长度</summary>
    public int MaxHistoryLength { get; init; } = 20;

    /// <summary>当前状态</summary>
    public TState CurrentState { get; private set; }

    /// <summary>最近转换历史（只读）</summary>
    public IReadOnlyList<TransitionRecord<TState>> History => _history;

    /// <summary>所有已注册状态</summary>
    public IReadOnlyCollection<TState> RegisteredStates => _registeredStates;

    /// <summary>进入状态回调</summary>
    public event Action<TState, TState>? OnStateEnter;

    /// <summary>离开状态回调</summary>
    public event Action<TState, TState>? OnStateExit;

    public StateMachine(TState initialState)
    {
        CurrentState = initialState;
        _registeredStates.Add(initialState);
    }

    /// <summary>注册一个合法状态</summary>
    public void RegisterState(TState state)
    {
        _registeredStates.Add(state);
    }

    /// <summary>
    /// 注册状态转换规则。
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="trigger">触发器名称</param>
    /// <param name="to">目标状态</param>
    /// <param name="guard">守卫条件，返回 false 时拒绝转换</param>
    public void AddTransition(TState from, string trigger, TState to, Func<bool>? guard = null)
    {
        _registeredStates.Add(from);
        _registeredStates.Add(to);

        if (!_transitions.ContainsKey(from))
            _transitions[from] = new Dictionary<string, TransitionRule<TState>>();

        _transitions[from][trigger] = new TransitionRule<TState>(to, guard);
    }

    /// <summary>
    /// 尝试触发转换。
    /// </summary>
    /// <returns>true 转换成功，false 被拒绝或无对应规则</returns>
    public bool TryTransition(string trigger)
    {
        if (!_transitions.TryGetValue(CurrentState, out var rules))
            return false;

        if (!rules.TryGetValue(trigger, out var rule))
            return false;

        if (rule.Guard != null && !rule.Guard())
            return false;

        ExecuteTransition(rule.Target, trigger);
        return true;
    }

    /// <summary>
    /// 强制转换到指定状态，跳过 guard 检查。用于主线强制推进。
    /// </summary>
    /// <returns>true 成功（目标是已注册状态），false 目标未注册</returns>
    public bool ForceTransition(TState target, string? reason = null)
    {
        if (!_registeredStates.Contains(target))
            return false;

        ExecuteTransition(target, reason);
        return true;
    }

    /// <summary>
    /// 检查某个触发器在当前状态是否可用（有规则且 guard 通过）。
    /// </summary>
    public bool CanTransition(string trigger)
    {
        if (!_transitions.TryGetValue(CurrentState, out var rules))
            return false;

        if (!rules.TryGetValue(trigger, out var rule))
            return false;

        return rule.Guard == null || rule.Guard();
    }

    private void ExecuteTransition(TState target, string? trigger)
    {
        var from = CurrentState;
        OnStateExit?.Invoke(from, target);
        CurrentState = target;
        OnStateEnter?.Invoke(from, target);

        _history.Add(new TransitionRecord<TState>(from, target, trigger, DateTime.UtcNow));
        if (_history.Count > MaxHistoryLength)
            _history.RemoveAt(0);
    }
}

/// <summary>转换规则内部类</summary>
internal sealed record TransitionRule<TState>(TState Target, Func<bool>? Guard) where TState : notnull;
