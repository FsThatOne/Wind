# ADR-0008: Generic Finite State Machine

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Core / Shared Infrastructure |
| **Knowledge Risk** | LOW — 纯 C# 实现，不依赖引擎特定 API |
| **References Consulted** | None needed (pure C# pattern) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | NPC 状态管理, 误会系统, 战斗阶段, 动态音乐, 场景过渡 |
| **Blocks** | None (可与其他 Sprint 1 基础设施并行实现) |
| **Ordering Note** | Sprint 1 Shared 基础设施之一 |

## Context

### Problem Statement

《风止》至少 5 个系统需要状态机：NPC 态度 FSM（6 状态）、误会系统（6 状态）、战斗阶段管理、动态音乐（6 状态）、场景过渡（4 状态）。需要决定：使用 Godot 的 AnimationTree/StateMachine 节点，还是自研泛型 C# FSM。

## Decision

采用 **泛型 C# FSM 基类**（`FiniteStateMachine<TState, TTrigger>`），不使用 Godot 节点型状态机。

### 核心实现

```csharp
// Shared/StateMachine/FiniteStateMachine.cs
public class FiniteStateMachine<TState, TTrigger>
    where TState : Enum
    where TTrigger : Enum
{
    public TState CurrentState { get; private set; }
    public event Action<TState, TState> StateChanged;

    private readonly Dictionary<(TState, TTrigger), TState> _transitions = new();
    private readonly Dictionary<TState, Action> _onEnter = new();
    private readonly Dictionary<TState, Action> _onExit = new();

    public FiniteStateMachine(TState initial) => CurrentState = initial;

    public void AddTransition(TState from, TTrigger trigger, TState to)
        => _transitions[(from, trigger)] = to;

    public void OnEnter(TState state, Action action) => _onEnter[state] = action;
    public void OnExit(TState state, Action action) => _onExit[state] = action;

    public bool Fire(TTrigger trigger)
    {
        if (!_transitions.TryGetValue((CurrentState, trigger), out var next))
            return false;

        var prev = CurrentState;
        if (_onExit.TryGetValue(prev, out var exit)) exit();
        CurrentState = next;
        if (_onEnter.TryGetValue(next, out var enter)) enter();
        StateChanged?.Invoke(prev, next);
        return true;
    }

    public bool CanFire(TTrigger trigger)
        => _transitions.ContainsKey((CurrentState, trigger));
}
```

### 使用示例

```csharp
// 误会系统 6 状态
enum MisState { Dormant, Brewing, Active, Escalated, Resolving, Resolved }
enum MisTrigger { SeedPlanted, ThresholdReached, Escalate, AttemptResolve, Success, Timeout }

var fsm = new FiniteStateMachine<MisState, MisTrigger>(MisState.Dormant);
fsm.AddTransition(MisState.Dormant, MisTrigger.SeedPlanted, MisState.Brewing);
fsm.AddTransition(MisState.Brewing, MisTrigger.ThresholdReached, MisState.Active);
// ...
fsm.OnEnter(MisState.Active, () => Services.EventBus.Publish(new MisunderstandingActiveEvent(id)));
```

### 为何不用 Godot StateMachine 节点

| Godot 节点 FSM | 自研泛型 FSM |
|----------------|-------------|
| 需要场景树中挂节点 | 纯 C# 对象，不依赖节点 |
| 主要服务于动画状态 | 服务于任意逻辑状态 |
| 不支持强类型 Trigger | enum 约束，编译期检查 |
| 难以单元测试 | 无 Godot 依赖，直接测试 |

## Consequences

### Positive
- 单一实现复用 5+ 系统 — DRY
- 强类型 enum — 无拼写错误风险
- 无 Godot 依赖 — 纯单元测试
- StateChanged 事件可接入 EventBus

### Negative
- 不支持 hierarchical FSM（如需要，后续可扩展）
- 无可视化调试工具（需自建 DEBUG 面板）

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| npc-state.md | 6 状态态度 FSM | FiniteStateMachine<NpcAttitude, NpcTrigger> |
| misunderstanding-system.md | 6 状态误会 FSM | FiniteStateMachine<MisState, MisTrigger> |
| combat-system.md | 战斗阶段管理 | FiniteStateMachine<BattlePhase, BattleTrigger> |
| audio-system.md | 6 状态音频 FSM | FiniteStateMachine<AudioState, AudioTrigger> |
| map-scene-management.md | 4 状态过渡 FSM | FiniteStateMachine<TransitionState, TransitionTrigger> |

## Validation Criteria
1. 单元测试：状态转换正确
2. 单元测试：无效 trigger 返回 false，状态不变
3. 单元测试：OnEnter/OnExit 回调按序执行
4. 单元测试：StateChanged 事件正确触发

## Related Decisions
- [ADR-0001](adr-0001-event-bus-architecture.md) — FSM StateChanged 可桥接到 EventBus
- [ADR-0009](adr-0009-dynamic-audio.md) — 音乐 FSM 使用此基类
