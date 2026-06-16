# ADR-0011: Combat UI Animation Pipeline

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | UI Animation, TimeScale, Camera |
| **Knowledge Risk** | **HIGH** — 依赖 ADR-0002 的 dual-focus 体系；TimeScale 与 Tween 交互行为需验证 |
| **References Consulted** | `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/breaking-changes.md`, `design/gdd/combat-ui.md` |
| **Post-Cutoff APIs Used** | Dual-focus system (4.6), SceneTreeTween process_mode 行为 |
| **Verification Required** | 1) 验证 `Engine.TimeScale = 0.2` 时 SceneTreeTween 的 `SetProcessMode(ALWAYS)` 是否正确忽略 TimeScale; 2) 验证 Camera2D smoothing 在低 TimeScale 下的平滑行为; 3) 验证对象池 Control 节点 reparent 时焦点不泄漏 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 订阅战斗事件), ADR-0002 (UI Framework — BaseUiPanel, CanvasLayer 层级, FocusManager) |
| **Enables** | Sprint 3 CombatUi 实现, Presentation/CombatUi 模块全部动画功能 |
| **Blocks** | Sprint 3+ (战斗 UI 实现不可在本 ADR 验证前开始) |
| **Ordering Note** | 必须在 ADR-0002 spike 通过后开始本 ADR 的 spike 验证 |

## Context

### Problem Statement

战斗 UI (GDD #7) 定义了 7 阶段一击决胜演出、意图图标动画、伤害数字弹出、资源条渐变、协同浮字等大量动画需求。这些动画需要：
- 与 `Engine.TimeScale` 协作（决胜演出主动降低 TimeScale）
- 在一帧内响应多个战斗事件（如同回合多人受伤）
- 管理 Camera2D 控制权（决胜演出需推进特写）
- 高频创建/销毁浮动 UI 元素（伤害数字）而不产生 GC 压力

需要决定：如何组织动画系统，使其可维护、可测试、性能可控。

### Constraints

- 所有动画必须在 `Engine.TimeScale` 变化时行为正确
- 决胜演出不可跳过（默认）；可通过 Tuning Knob 开启跳过
- 暂停菜单（TimeScale = 0）期间动画必须冻结
- 伤害数字峰值并发：单回合最多 6 个（3v3 战斗）
- Camera2D 在非演出期间必须恢复默认跟随行为
- 不引入第三方动画库，使用 Godot 原生 Tween + AnimationPlayer

### Requirements (from GDD #7)

- 意图图标淡入动画（`intent_icon_fadein_duration`）
- 资源条平滑更新（`resource_bar_update_speed`）
- 伤害数字弹出 + 浮升 + 消散（`damage_float_duration`）
- 破绽爆满脉冲动效（`stagger_pulse_speed`）
- 一击决胜 7 阶段演出（TimeScale 插值 + Camera 推进 + 招式动画 + 恢复）
- 协同浮字提示
- 克制/被克命中画面闪光/微震

## Decision

采用 **CombatAnimationDirector 状态机 + Tween 程序化动画 + 对象池** 方案。

### 核心架构

```
┌─────────────────────────────────────────────────────────┐
│              CombatAnimationDirector                      │
│              (状态机 — 编排所有战斗演出)                  │
│                                                          │
│  States: Idle → IntentReveal → PlayerDecision            │
│          → ActionResolve → DecisiveStrike → RoundEnd     │
│                                                          │
│  职责：接收战斗事件 → 排队动画 → 按优先级/顺序播放      │
└────────────────────┬────────────────────────────────────┘
                     │ 委托
    ┌────────────────┼────────────────────┐
    ▼                ▼                    ▼
┌──────────┐  ┌──────────────┐  ┌────────────────┐
│DamageNum │  │TimeScale     │  │CameraRequest   │
│Pool      │  │Controller    │  │Bus             │
│(对象池)  │  │(优先级栈)    │  │(优先级请求)    │
└──────────┘  └──────────────┘  └────────────────┘
```

### 1. CombatAnimationDirector (状态机)

```csharp
// Presentation/CombatUi/CombatAnimationDirector.cs
public partial class CombatAnimationDirector : Node
{
    private readonly Queue<IAnimationCommand> _commandQueue = new();
    private bool _isPlaying;

    public override void _Ready()
    {
        var bus = Services.EventBus;
        bus.Subscribe<TurnResolvedEvent>(OnTurnResolved);
        bus.Subscribe<IntentDeclaredEvent>(OnIntentDeclared);
        bus.Subscribe<BattleEndedEvent>(OnBattleEnded);
    }

    /// <summary>
    /// 入队一组动画命令，按顺序执行。
    /// 并行命令通过 ParallelCommand 包装。
    /// </summary>
    public void Enqueue(IAnimationCommand command)
    {
        _commandQueue.Enqueue(command);
        if (!_isPlaying) PlayNext();
    }

    private async void PlayNext()
    {
        if (_commandQueue.Count == 0) { _isPlaying = false; return; }
        _isPlaying = true;
        var cmd = _commandQueue.Dequeue();
        await cmd.Execute();
        PlayNext();
    }
}

// Presentation/CombatUi/Commands/IAnimationCommand.cs
public interface IAnimationCommand
{
    Task Execute();
}
```

### 2. TimeScale Controller (优先级栈)

解决多系统争抢 TimeScale 的冲突（暂停菜单 vs 决胜演出）：

```csharp
// Presentation/Shared/TimeScaleController.cs
public partial class TimeScaleController : Node
{
    private readonly SortedList<int, TimeScaleRequest> _stack = new();

    /// <summary>
    /// priority 越高越优先。暂停菜单 = 100，决胜演出 = 50，默认 = 0。
    /// </summary>
    public TimeScaleHandle Request(float targetScale, int priority, string owner)
    {
        var request = new TimeScaleRequest(targetScale, priority, owner);
        _stack.Add(priority, request);
        Apply();
        return new TimeScaleHandle(() => Release(request));
    }

    private void Release(TimeScaleRequest request)
    {
        _stack.RemoveAt(_stack.IndexOfValue(request));
        Apply();
    }

    private void Apply()
    {
        Engine.TimeScale = _stack.Count > 0
            ? _stack.Values[^1].TargetScale  // 最高优先级生效
            : 1.0;
    }
}

public record TimeScaleRequest(float TargetScale, int Priority, string Owner);

public class TimeScaleHandle : IDisposable
{
    private readonly Action _release;
    private bool _disposed;
    public TimeScaleHandle(Action release) => _release = release;
    public void Dispose() { if (!_disposed) { _release(); _disposed = true; } }
}
```

### 3. DamageNumberPool (对象池)

```csharp
// Presentation/CombatUi/DamageNumberPool.cs
public partial class DamageNumberPool : Node
{
    private const int PoolSize = 12;
    private readonly Stack<DamageNumberLabel> _available = new();

    public override void _Ready()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var label = new DamageNumberLabel();
            label.Visible = false;
            AddChild(label);
            _available.Push(label);
        }
    }

    public DamageNumberLabel Acquire(Vector2 worldPos, int amount, DamageType type)
    {
        var label = _available.Count > 0 ? _available.Pop() : CreateOverflow();
        label.Setup(worldPos, amount, type);
        label.Visible = true;
        return label;
    }

    public void Release(DamageNumberLabel label)
    {
        label.Visible = false;
        label.Reset();
        _available.Push(label);
    }

    private DamageNumberLabel CreateOverflow()
    {
        // 超出池大小时动态创建，但记录警告
        GD.PushWarning("DamageNumberPool overflow — consider increasing PoolSize");
        var label = new DamageNumberLabel();
        AddChild(label);
        return label;
    }
}
```

### 4. 动画实现策略分类

| 动画类型 | 方案 | 理由 |
|----------|------|------|
| 意图图标淡入 | `SceneTreeTween` | 单属性插值（modulate.a + position.y），参数化 |
| 资源条渐变 | `SceneTreeTween` | value 属性插值，需 TimeScale 感知 |
| 伤害数字弹出 | `SceneTreeTween` | position + scale + modulate.a 组合，程序化参数 |
| 破绽脉冲 | `AnimationPlayer` (循环) | 持续循环动效，适合 authored animation |
| 决胜招式特效 | `AnimationPlayer` | 体系专属动画 (刚/柔/巧)，美术制作后导入 |
| 决胜 TimeScale 曲线 | `SceneTreeTween` + `SetProcessMode(ALWAYS)` | 必须不受自身 TimeScale 修改影响 |
| Camera 推进/归位 | `SceneTreeTween` + `SetProcessMode(ALWAYS)` | 必须不受 TimeScale 影响 |
| 画面闪光/微震 | `SceneTreeTween` (CanvasModulate + Camera offset) | 短暂一次性效果 |

**关键规则**：修改 TimeScale 本身的 Tween 必须设置 `SetProcessMode(Tween.TweenProcessMode.Always)`，否则会自锁。

### 5. CameraRequestBus (优先级请求)

```csharp
// Presentation/Shared/CameraRequestBus.cs
public partial class CameraRequestBus : Node
{
    private readonly PriorityQueue<CameraRequest, int> _requests = new();
    private CameraRequest? _active;

    public CameraHandle RequestCamera(CameraRequest request)
    {
        _requests.Enqueue(request, -request.Priority); // 负数 = 高优先先出
        EvaluateTop();
        return new CameraHandle(() => Release(request));
    }

    private void EvaluateTop()
    {
        if (_requests.Count == 0) { RestoreDefault(); return; }
        _requests.TryPeek(out var top, out _);
        if (top != _active) { _active = top; ApplyCamera(top); }
    }

    private void ApplyCamera(CameraRequest req)
    {
        var cam = GetViewport().GetCamera2D();
        // 委托给当前请求控制 Camera
        req.Apply(cam);
    }

    private void RestoreDefault()
    {
        _active = null;
        var cam = GetViewport().GetCamera2D();
        cam.PositionSmoothingEnabled = true;
        // 恢复默认跟随目标
    }
}

// 优先级定义
public static class CameraPriority
{
    public const int Default = 0;
    public const int DecisiveStrike = 50;
    public const int Cutscene = 80;
}
```

### 6. 一击决胜演出完整流程 (7 Phase)

```csharp
// Presentation/CombatUi/Commands/DecisiveStrikeSequence.cs
public class DecisiveStrikeSequence : IAnimationCommand
{
    private readonly DecisiveStrikeData _data;
    private readonly TimeScaleController _tsController;
    private readonly CameraRequestBus _cameraBus;

    public async Task Execute()
    {
        // Phase 1: 逻辑预结算（已由 Core/Combat 完成，此处仅接收结果）

        // Phase 2: TimeScale slow-in
        using var tsHandle = _tsController.Request(0.2f, 50, "DecisiveStrike");
        var tween = GetTree().CreateTween().SetProcessMode(Tween.TweenProcessMode.Always);
        tween.TweenProperty(Engine.Singleton, "time_scale", 0.2f, _data.SlowInDuration);
        await ToSignal(tween, Tween.SignalName.Finished);

        // Phase 3: Camera 推进
        var camHandle = _cameraBus.RequestCamera(new DecisiveCameraRequest(_data.Target));
        await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        // Phase 4: 播放体系专属招式动画
        _data.AnimPlayer.Play(_data.GetAnimationName());
        await ToSignal(_data.AnimPlayer, AnimationPlayer.SignalName.AnimationFinished);

        // Phase 5: 伤害数字弹出（最大尺寸）
        var dmgLabel = _data.Pool.Acquire(_data.Target.GlobalPosition, _data.Damage, DamageType.Decisive);
        await ToSignal(GetTree().CreateTimer(_data.HoldDuration), SceneTreeTimer.SignalName.Timeout);

        // Phase 6: TimeScale slow-out
        var tweenOut = GetTree().CreateTween().SetProcessMode(Tween.TweenProcessMode.Always);
        tweenOut.TweenProperty(Engine.Singleton, "time_scale", 1.0f, _data.SlowOutDuration);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        // Phase 7: Camera 归位
        camHandle.Dispose();
        tsHandle.Dispose(); // 释放 TimeScale 请求
    }
}
```

### 7. 画面反馈效果 (闪光/微震)

```csharp
// Presentation/CombatUi/ScreenEffects.cs
public partial class ScreenEffects : Node
{
    [Export] private CanvasModulate _flashLayer;
    [Export] private Camera2D _camera;

    public void FlashOnHit(HitType type)
    {
        var color = type switch
        {
            HitType.Counter => new Color(1, 0.9f, 0.6f, 0.3f),  // 金色闪光
            HitType.Countered => new Color(0.5f, 0.5f, 0.5f, 0.2f),  // 灰色
            _ => Colors.Transparent
        };

        var tween = CreateTween();
        tween.TweenProperty(_flashLayer, "color", color, 0.05f);
        tween.TweenProperty(_flashLayer, "color", Colors.White, 0.15f);
    }

    public void ShakeOnHit(float intensity = 4f, float duration = 0.2f)
    {
        var tween = CreateTween();
        var originalOffset = _camera.Offset;
        for (int i = 0; i < 4; i++)
        {
            var offset = new Vector2(
                (float)GD.RandRange(-intensity, intensity),
                (float)GD.RandRange(-intensity, intensity)
            );
            tween.TweenProperty(_camera, "offset", offset, duration / 8);
        }
        tween.TweenProperty(_camera, "offset", originalOffset, duration / 8);
    }
}
```

## Alternatives Considered

### Alternative 1: AnimationPlayer 统一方案

- **Description**: 所有动画均通过 AnimationPlayer 轨道编排
- **Pros**: 编辑器可视化；美术直接编辑；方便调时间线
- **Cons**: 动态参数化困难（伤害数字大小、位置依赖运行时数据）；需要大量 AnimationPlayer 节点；程序化效果无法在编辑器预览
- **Rejection Reason**: 战斗 UI 的动画 70% 是程序化参数驱动（数字大小、位置、颜色查表），AnimationPlayer 不适合

### Alternative 2: 自研 Coroutine 动画系统

- **Description**: 基于 C# async/await 自研轻量动画协程
- **Pros**: 完全控制；可精确管理 TimeScale 交互
- **Cons**: 重新造轮子；失去 Tween 的 easing 曲线库；增加维护负担；新人学习成本
- **Rejection Reason**: Godot SceneTreeTween 已经支持 `SetProcessMode(ALWAYS)` 解决 TimeScale 自锁问题，无需自研

### Alternative 3: Tween 无状态机（直接在事件回调中创建 Tween）

- **Description**: 每个事件处理器直接创建 Tween，无统一编排
- **Pros**: 简单直接；无额外架构
- **Cons**: 并发动画冲突无法管理（两个决胜同时触发？）；Camera 控制权无仲裁；测试困难
- **Rejection Reason**: GDD 明确定义了 7 阶段顺序演出，需要状态机保证序列正确性

## Consequences

### Positive

- 命令队列保证动画顺序一致性，无竞态
- TimeScaleController 优先级栈彻底解决暂停/演出/默认三方冲突
- 对象池消除伤害数字 GC 压力（预分配 12 个，峰值 6 并发）
- Tween + AnimationPlayer 混用，各取所长
- CameraRequestBus 解耦 Camera 控制，未来 Cutscene 系统可直接复用

### Negative

- CombatAnimationDirector 是中心化编排器 — 复杂度集中
- IAnimationCommand 接口需为每种演出写具体实现类
- TimeScaleController 是全局可变状态 — 需严格管理 Handle 生命周期

### Risks

| 风险 | 缓解 |
|------|------|
| `SetProcessMode(ALWAYS)` 在 4.6 行为变更 | Sprint 2 spike：创建 TimeScale 从 1→0.2→1 的最小 Tween 测试 |
| 对象池 Control 节点 reparent 导致焦点泄漏 | DamageNumberLabel 设置 `focus_mode = NONE`；池回收时调用 `ReleaseFocus()` |
| 决胜演出被暂停菜单中断后恢复状态异常 | TimeScaleController 优先级栈确保暂停释放后演出值恢复 |
| Camera Tween 与默认 smoothing 冲突 | 演出期间禁用 `position_smoothing_enabled`，归位时重新启用 |
| 命令队列堆积（极端：回合结束时 6 个伤害同时入队） | 并行伤害用 `ParallelCommand` 包装，实际只占 1 个队列槽 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| combat-ui.md | 意图图标淡入 + 位移动画 | SceneTreeTween 参数化 `intent_icon_fadein_duration` |
| combat-ui.md | 资源条平滑更新 | SceneTreeTween 驱动 value 属性，时长 `resource_bar_update_speed` |
| combat-ui.md | 伤害数字弹出 (4 种样式) | DamageNumberPool + DamageType enum 查表决定 scale/color |
| combat-ui.md | 破绽 ≥5 脉冲动效 | AnimationPlayer 循环动画，`stagger_pulse_speed` 控制 |
| combat-ui.md | 一击决胜 7 Phase 演出 | DecisiveStrikeSequence 命令 + TimeScaleController + CameraRequestBus |
| combat-ui.md | 克制闪光 / 被克微震 | ScreenEffects.FlashOnHit() / ShakeOnHit() |
| combat-ui.md | 协同浮字 | DamageNumberPool 复用 + DamageType.Coop 样式 |
| combat-ui.md | 决胜不可跳过 (默认) | DecisiveStrikeSequence 内不监听跳过输入；Tuning Knob 控制 |
| combat-ui.md | 暂停不影响演出恢复 | TimeScaleController 优先级栈 (暂停=100 > 演出=50) |

## Performance Implications

- **CPU**: 命令队列 `Dequeue` O(1)；Tween 每帧 1 次 lerp 计算 — 可忽略
- **Memory**: 对象池预分配 12 个 Label 节点 (~2KB each) ≈ 24KB 常驻
- **GC Pressure**: 零 — 对象池避免战斗中 alloc/free
- **Frame Budget**: 决胜演出期间 TimeScale=0.2，物理帧率不变但逻辑处理减少
- **Load Time**: 无影响（池在战斗场景 _Ready 时创建）

## Migration Plan

首次实现，无迁移需求。依赖 ADR-0002 spike 通过后，在 Sprint 3 开始实现。

## Validation Criteria

1. **TimeScale Spike** (Sprint 2)：验证 Tween + `SetProcessMode(ALWAYS)` 在 `Engine.TimeScale = 0.2` 时正常插值
2. **对象池测试**：连续 Acquire 12 次后 Release 12 次，无内存增长
3. **决胜演出完整流程**：Phase 1-7 顺序执行无跳帧，暂停后恢复位置正确
4. **Camera 归位**：演出结束后 Camera 在 0.3s 内恢复默认跟随
5. **并发伤害**：同帧 6 个 `OnDamageDealt` 事件触发，所有伤害数字正确显示不重叠
6. **暂停恢复**：决胜演出 Phase 4 途中暂停 → 恢复 → 演出从中断处继续

## ICombatService 接口契约 (Combat 模块对外 Facade)

> **MI-1 补齐**：本节明确战斗模块 (Core/Combat) 对外暴露的接口契约，作为 Presentation/CombatUi、Cutscene、Epiphany 等系统的稳定调用边界。

### 接口定义

```csharp
// Core/Combat/ICombatService.cs
namespace Game.Core.Combat;

/// <summary>
/// 战斗系统 Facade — Presentation 层与跨模块调用统一入口。
/// 实现位于 Core/Combat/CombatService.cs（Autoload 单例，挂在 Services.Combat）。
/// </summary>
public interface ICombatService
{
    // ─── 战斗生命周期 ───
    /// <summary>开始战斗。返回当前战斗会话句柄。</summary>
    BattleHandle BeginBattle(BattleContext ctx);

    /// <summary>主动结束战斗（如剧情打断）。</summary>
    void EndBattle(BattleEndReason reason);

    /// <summary>当前是否在战斗中。</summary>
    bool IsCombatActive { get; }

    /// <summary>当前战斗回合（1-based）；非战斗中返回 0。</summary>
    int CurrentTurn { get; }

    // ─── 状态查询（只读，供 UI / Cutscene / Epiphany 等订阅方使用）───
    /// <summary>获取当前战斗参与单位的只读快照。</summary>
    IReadOnlyList<CombatantSnapshot> GetCombatants();

    /// <summary>查询指定单位的最新意图（用于 IntentReveal 阶段 UI 渲染）。</summary>
    IntentSnapshot? GetIntent(string combatantId);

    /// <summary>当前可执行的一击决胜目标列表（无可决胜时返回空）。</summary>
    IReadOnlyList<DecisiveStrikeOpportunity> GetDecisiveOpportunities();

    // ─── 演出协作（CombatAnimationDirector 与 Cutscene 复用）───
    /// <summary>请求执行一击决胜（由 UI 玩家选择后调用）。返回演出脚本数据供 Director 排队。</summary>
    DecisiveStrikeData RequestDecisiveStrike(string targetId);

    /// <summary>暂停战斗逻辑推进（不暂停 UI 演出）。Cutscene/Epiphany 用于穿插剧情时调用。</summary>
    IDisposable SuspendLogic(string reason);

    // ─── 事件契约（详见 ADR-0001 EventBus）───
    // 发布事件（订阅方使用 Services.EventBus.Subscribe<T>）：
    //   - BattleStartedEvent
    //   - TurnAdvancedEvent
    //   - IntentDeclaredEvent
    //   - TurnResolvedEvent
    //   - DamageDealtEvent
    //   - DecisiveStrikeAvailableEvent
    //   - BattleEndedEvent
}
```

### 关键调用方

| 调用方 | 调用方法 | 用途 |
|---|---|---|
| `CombatAnimationDirector` (本 ADR) | `GetIntent()`, `GetCombatants()`, `RequestDecisiveStrike()` | 渲染战斗 UI、播放决胜演出 |
| `CutsceneService` (ADR-0013) | `SuspendLogic()`, `IsCombatActive` | 战斗中插入剧情时暂停回合推进 |
| `EpiphanyEvaluator` (ADR-0017) | `IsCombatActive`, `CurrentTurn`, `GetCombatants()` | 战斗触发型顿悟的概率评估 |
| `Presentation/CombatUi/CombatHud` | `GetIntent()`, `GetCombatants()` | HUD 状态渲染 |
| `MainNarrativeService` (#9) | `BeginBattle(BattleContext)`, `EndBattle()` | 剧情触发战斗 |

### 接口稳定性约定

- **稳定接口**：上述方法签名一旦 ADR Accepted，不可在 Sprint 3 实现期内修改；新增能力通过新方法或可选参数扩展
- **事件契约**：事件类名、字段、语义对外稳定；新增事件不算破坏性变更
- **实现细节隔离**：`CombatAnimationDirector`、`TimeScaleController` 等本 ADR 内部组件**不**暴露给 Core 以外模块；外部仅依赖 `ICombatService`

## Related Decisions

- [ADR-0001](adr-0001-event-bus-architecture.md) — 战斗事件通过 EventBus 触发动画
- [ADR-0002](adr-0002-ui-framework-dual-focus.md) — UI 框架、CanvasLayer 层级、BaseUiPanel 基类
- [ADR-0008](adr-0008-finite-state-machine.md) — CombatAnimationDirector 内部状态机可复用泛型 FSM
- [ADR-0009](adr-0009-dynamic-audio.md) — 决胜演出音效与动画同步触发点
- [ADR-0013](adr-0013-cutscene-system.md) — Cutscene 通过 `SuspendLogic()` 在战斗中插剧情
- [ADR-0017](adr-0017-epiphany-breakthrough.md) — Epiphany 战斗触发路径依赖本 facade 状态查询
