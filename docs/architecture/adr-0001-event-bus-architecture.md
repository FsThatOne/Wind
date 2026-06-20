# ADR-0001: Event Bus Architecture

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Core / Scripting |
| **Knowledge Risk** | HIGH — 4.5+ Signal delegate patterns post-cutoff |
| **References Consulted** | `docs/engine-reference/godot/current-best-practices.md`, `docs/engine-reference/godot/breaking-changes.md` |
| **Post-Cutoff APIs Used** | `[Signal] delegate` C# event syntax (4.5+ recommended pattern) |
| **Verification Required** | Verify `[Signal] delegate` works correctly with generic EventBus Autoload; verify GC behavior of event subscriptions on scene unload |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0002 (UI needs EventBus for state notification), all Feature/Core/Foundation modules |
| **Blocks** | All Sprint 1+ implementation — EventBus is first infrastructure to build |
| **Ordering Note** | Must be implemented and verified before any cross-layer system can be built |

## Context

### Problem Statement

《风止》的 5 层架构（Platform → Foundation → Core → Feature → Presentation）需要一个层间通信机制。下层系统不得直接引用上层，但需要通知上层状态变化（如战斗结算通知 UI 刷新、自然日推进通知所有 Feature 系统更新）。需要决定全局事件路由的实现方案。

### Constraints

- Godot 4.7-stable C# (.NET 8+) 环境
- 25 个系统、7 条关键数据流路径需要事件驱动
- 必须支持类型安全（避免 string-based 连接）
- 必须处理节点生命周期（场景切换时自动清理订阅）
- 性能：DayAdvancedEvent 最多触发 ~10 个订阅者，CombatResultEvent ~5 个
- 可测试性：单元测试中不依赖 Godot SceneTree

### Requirements

- 跨层通信无需持有目标节点引用
- 类型安全的事件发布和订阅
- 场景卸载时自动解除订阅（防内存泄漏）
- 支持事件优先级或有序分发（存档系统需最后处理）
- 单元测试中可 mock

## Decision

采用 **混合方案**：自定义 C# EventBus Autoload（跨层）+ Godot Signals（本地）。

### 通信规则

| 场景 | 机制 | 示例 |
|------|------|------|
| 跨层通知（下→上） | EventBus.Publish\<T\>() | Combat → CombatUi (TurnResolvedEvent) |
| 同场景父子/兄弟 | Godot [Signal] delegate | Button → Panel (pressed) |
| 同层同模块内部 | 直接方法调用 | CombatSystem → TurnResolver |

### Architecture Diagram

```
┌──────────────────────────────────────────────────┐
│             EventBus (Autoload)                   │
│                                                  │
│  Dictionary<Type, List<Delegate>> _subscribers   │
│                                                  │
│  Publish<T>(T evt)                               │
│  Subscribe<T>(Action<T> handler, int priority)   │
│  Unsubscribe<T>(Action<T> handler)               │
│  ClearSubscriptionsFor(Node node)                │
└──────────┬───────────────────────┬───────────────┘
           │ Subscribe             │ Publish
           ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│ Presentation    │     │ Foundation      │
│ (subscribers)   │     │ (publishers)    │
│ CombatUi        │     │ TimeSystem      │
│ Audio           │     │ NpcState        │
│ BlurredUi       │     │ SceneManagement │
└─────────────────┘     └─────────────────┘
```

### Key Interfaces

```csharp
// Shared/EventBus/GameEvent.cs
public abstract record GameEvent(double Timestamp)
{
    public double Timestamp { get; init; } = Godot.Time.GetTicksMsec() / 1000.0;
}

// Shared/EventBus/IEventBus.cs
public interface IEventBus
{
    void Publish<T>(T evt) where T : GameEvent;
    void Subscribe<T>(Action<T> handler, int priority = 0) where T : GameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : GameEvent;
    void ClearAllFor(GodotObject owner);
}

// Shared/EventBus/EventBus.cs (Autoload)
public partial class EventBus : Node, IEventBus
{
    private readonly Dictionary<Type, SortedList<int, List<Delegate>>> _subs = new();

    public void Publish<T>(T evt) where T : GameEvent
    {
        if (!_subs.TryGetValue(typeof(T), out var priorityMap)) return;
        foreach (var (_, handlers) in priorityMap)
            foreach (var handler in handlers)
                ((Action<T>)handler).Invoke(evt);
    }

    public void Subscribe<T>(Action<T> handler, int priority = 0) where T : GameEvent
    {
        var type = typeof(T);
        if (!_subs.ContainsKey(type))
            _subs[type] = new SortedList<int, List<Delegate>>();
        if (!_subs[type].ContainsKey(priority))
            _subs[type][priority] = new List<Delegate>();
        _subs[type][priority].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent { /* remove */ }

    public void ClearAllFor(GodotObject owner) { /* remove all handlers targeting owner */ }
}

// 使用示例 — 订阅者（Presentation 层）
public partial class CombatHud : Control
{
    public override void _Ready()
    {
        Services.EventBus.Subscribe<TurnResolvedEvent>(OnTurnResolved, priority: 0);
    }

    public override void _ExitTree()
    {
        Services.EventBus.ClearAllFor(this);
    }

    private void OnTurnResolved(TurnResolvedEvent evt) { /* 刷新 HUD */ }
}

// 使用示例 — 发布者（Core 层）
public partial class CombatSystem : Node
{
    private void ResolveTurn()
    {
        var result = /* ... calculate ... */;
        Services.EventBus.Publish(new TurnResolvedEvent(result));
    }
}
```

### 生命周期管理

- 每个订阅 Node 在 `_ExitTree()` 中调用 `ClearAllFor(this)`
- EventBus 提供 `SceneTree.NodeRemoved` 信号的自动监听作为安全网
- record 类型的事件不可变，发布后无竞态风险

### 事件命名规范

- 事件类名：`{Domain}{Action}Event`（如 `TurnResolvedEvent`, `DayAdvancedEvent`）
- 文件位置：`src/FengZhi.Shared/EventBus/Events/{Layer}/`
- 一个文件一个事件类型

## Alternatives Considered

### Alternative 1: Pure Godot Signals on Single Autoload

- **Description**: 在一个 EventBus Autoload 上声明所有 `[Signal] delegate`，系统通过 `+=` 连接
- **Pros**: 完全引擎原生；编辑器可见；内置 type-safe
- **Cons**: 所有事件集中在一个节点 → 类爆炸；信号参数受 Variant 限制（不支持 record）；无优先级控制；25 系统 × ~3 事件 = 75+ signals 在一个类上不可维护
- **Rejection Reason**: 不具备可扩展性，75+ signals 集中声明违反单一职责

### Alternative 2: Pure C# Static Events

- **Description**: `static class GameEvents { public static event Action<T> EventName; }`
- **Pros**: 零 Godot 依赖；纯 .NET 模式；极简
- **Cons**: 静态事件无生命周期管理 → 内存泄漏风险高；无法在 Godot 编辑器调试；无法利用 SceneTree 节点卸载自动清理
- **Rejection Reason**: 内存泄漏风险对长时间运行的 RPG 不可接受

### Alternative 3: Third-party Message Bus (e.g., MediatR)

- **Description**: 引入 NuGet 包如 MediatR 做中介者模式
- **Pros**: 成熟的 .NET 生态方案；支持 pipeline behaviors
- **Cons**: 额外依赖；与 Godot 节点生命周期无集成；过度工程化（项目无微服务需求）
- **Rejection Reason**: 引入外部依赖违反最小依赖原则，且不感知 Godot 生命周期

## Consequences

### Positive

- 层间完全解耦：Foundation 层永远不知道 Presentation 层的存在
- 类型安全：编译期检查事件类型，避免 string typo
- 可测试：mock IEventBus 即可单元测试任何系统
- 自动清理：_ExitTree + 安全网双保险

### Negative

- 两种通信模式（EventBus + Godot Signal）增加认知负荷
- 事件发布顺序依赖 priority 参数，调试需注意
- 间接调用使调用链追踪稍难（需 IDE 搜索 Subscribe 调用）

### Risks

| 风险 | 缓解 |
|------|------|
| 事件风暴（DayAdvanced 触发 10+ 处理器） | priority 排序 + 可选延迟队列（分帧处理） |
| 忘记 Unsubscribe 导致泄漏 | _ExitTree 强制 ClearAllFor + 运行时泄漏检测（DEBUG 模式） |
| EventBus 成为上帝对象 | 仅路由职责，无业务逻辑；事件定义分散在各层 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| combat-system.md | 战斗结算后通知 UI 更新、顿悟判定、AI 决策 | TurnResolvedEvent + BattleEndedEvent 通过 EventBus 广播 |
| natural-day-stamina.md | 日推进时所有系统响应 | DayAdvancedEvent 广播到 7 个订阅者 |
| npc-state.md | NPC 态度变化通知对话/感情/误会/UI | NpcAttitudeChangedEvent 跨层传播 |
| mindset-dual-axis.md | 心境位移通知 UI 色调和结局路径 | MindsetShiftedEvent 广播 |
| save-system.md | 存档事件通知全系统准备序列化 | SaveLoadedEvent 触发各系统状态恢复 |
| map-scene-management.md | 场景切换通知音乐/教学/存档 | SceneTransitionEvent 跨层通知 |
| misunderstanding-system.md | 误会状态变化通知 UI 模糊度 | MisunderstandingStateChangedEvent |

## Performance Implications

- **CPU**: Dictionary 查找 O(1) + 线性遍历订阅者列表（最大 ~10），可忽略
- **Memory**: 每个事件类型一个 SortedList，25 系统 × 3 事件 ≈ 75 entries，< 10KB
- **Load Time**: 无影响（订阅在 _Ready 时注册）
- **Network**: N/A（纯本地）

## Migration Plan

首次实现，无迁移需求。

## Validation Criteria

1. 单元测试：Publish → Subscribe 接收正确事件
2. 单元测试：ClearAllFor 后不再收到事件
3. 单元测试：priority 排序正确
4. 集成测试：场景切换后无残留订阅（DEBUG 模式计数器归零）
5. 性能测试：1000 次 Publish 调用 < 1ms（10 订阅者场景）

## Related Decisions

- [architecture.md](architecture.md) — Section 5.1 定义了 EventBus 作为跨切基础设施
- ADR-0002 (UI Framework) — UI 层是 EventBus 的主要消费者
