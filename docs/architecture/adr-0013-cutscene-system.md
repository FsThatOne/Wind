# ADR-0013: Cutscene System — Playback Pipeline & Global State Locking

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | Cutscene Playback, Game State Management, Resource Loading |
| **Knowledge Risk** | **MEDIUM** — ResourceLoader 异步加载 + SceneTreeTween 程序化动画为成熟 API；需验证多 CanvasLayer 叠加 Z-order 行为 |
| **References Consulted** | `design/gdd/cutscene-system.md`, `docs/engine-reference/godot/modules/ui.md`, ADR-0001 (EventBus), ADR-0002 (UI Framework), ADR-0009 (Audio), ADR-0011 (Combat UI Animation) |
| **Post-Cutoff APIs Used** | ResourceLoader.LoadThreadedRequest (4.x async), SceneTreeTween process mode |
| **Verification Required** | 1) 验证 ResourceLoader.LoadThreadedRequest 在 FULL 锁定下的行为; 2) 验证 CanvasLayer(100) 遮罩下方 UI 的输入屏蔽效果; 3) 验证 SceneTreeTween 在 Engine.TimeScale=0 时 SetProcessMode(ALWAYS) 的跳过恢复动画 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 发布/订阅 cutscene_completed 等事件), ADR-0002 (UI Framework — CanvasLayer 层级, HUD 隐藏), ADR-0009 (Audio — BGM 切换/SFX 播放), ADR-0011 (TimeScaleController — 演出慢动作集成) |
| **Enables** | 11+ 系统的演出触发能力 (主线叙事/战斗/顿悟/感情/心境/探索/NPC/日历/朦胧化 UI), 跳过机制的 GameplayEffect 保障 |
| **Blocks** | Sprint 5+ (所有包含 Tier 1-2 演出的 Feature 实现) |
| **Ordering Note** | ADR-0009 (Audio) 必须就绪以支持演出期间 BGM override；ADR-0011 (TimeScaleController) 必须就绪以支持 SLOW_MOTION 步骤 |

## Context

### Problem Statement

CG/演出系统 (GDD #20) 是被 11+ 系统调用的**全局服务层**——任何系统在任何时刻都可能请求播放演出（章节过渡、一击决胜、顿悟+境界突破串联、结缘/诀别……）。这带来以下架构挑战：

1. **并发冲突**：多系统可能同时请求演出，必须有排队/优先级机制
2. **全局状态锁定**：不同 Tier 的演出需要不同粒度的游戏状态冻结（禁止输入/系统 tick/存档）
3. **跳过安全性**：跳过不是"什么都不做"，而是"跳过视觉但执行所有 gameplay 副作用"
4. **资源预加载**：Tier 1 全屏 CG 插画体积大（单张 ~2MB），需要异步预加载+超时降级
5. **串联播放**：顿悟+境界突破等场景要求多段演出无缝串联，且各段独立可跳过
6. **PARALLEL 步骤**：单个步骤内部需要并行执行多个子操作（动画+音效同时）

需要决定：如何组织演出系统的播放管线、全局锁定策略、队列管理，使其对所有调用方透明且安全。

### Constraints

- 不同时播放两段 Tier 1-2 演出（互斥约束）
- 跳过时 `on_complete` 的 GameplayEffect **必须完整执行**，无论跳过发生在哪个步骤
- FULL 锁定期间不允许存档、不推进自然日、不消耗体力
- Tier 4 微演出不锁定任何操作（场景内原位播放）
- 串联中跳过当前段只前进到下一段，不跳过整个链
- 素材缺失时回退到占位画面而非崩溃
- 所有演出播放期间隐藏全部 HUD

### Requirements (from GDD #20)

- 4 层 Tier 分级 (全屏CG → 全屏像素 → 半屏局部 → 内嵌微演出)
- 11 种步骤类型 (SHOW_IMAGE, SHOW_TEXT, PLAY_ANIMATION, CAMERA_MOVE, SLOW_MOTION, SCREEN_EFFECT, PLAY_SFX, PLAY_BGM, WAIT, WAIT_INPUT, PARALLEL)
- 3 种 LockMode (FULL / PARTIAL / NONE)
- 6 种播放器状态 (IDLE → LOADING → PLAYING → SKIPPING → COMPLETING_EFFECTS → COMPLETED)
- FIFO 排队策略
- play_cutscene_chain 串联播放
- 长按 1.0s 跳过 + 进度条
- first_view_unskippable 首次观看保护
- 加载超时 3.0s 降级为占位画面

## Decision

采用 **CutsceneDirector 状态机 + GameStateLock 全局锁 + CutsceneQueue 优先排队 + StepExecutor 步骤引擎** 四层管线方案。

### 核心架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    CutsceneService (Autoload)                     │
│              全局入口 — play_cutscene / play_cutscene_chain       │
│                                                                   │
│  职责：验证请求 → 入队 → 通知 Director                          │
└──────────────────────────┬────────────────────────────────────────┘
                           │
            ┌──────────────┼──────────────────┐
            ▼              ▼                  ▼
┌────────────────┐  ┌──────────────┐  ┌────────────────────┐
│ CutsceneQueue  │  │ CutsceneDir  │  │ GameStateLock      │
│ (FIFO + Chain) │  │ ector (状态机)│  │ (锁定管理器)       │
│                │  │              │  │                    │
│ Enqueue()      │  │ IDLE→LOAD→   │  │ Acquire(mode)      │
│ PeekNext()     │  │ PLAY→SKIP→   │  │ Release()          │
│ Dequeue()      │  │ COMPLETE     │  │ IsLocked(scope)    │
└────────────────┘  └───────┬──────┘  └────────────────────┘
                            │ 委托
               ┌────────────┼────────────────┐
               ▼            ▼                ▼
    ┌───────────────┐ ┌──────────────┐ ┌──────────────────┐
    │ StepExecutor  │ │ ResourcePre  │ │ SkipHandler      │
    │ (步骤引擎)    │ │ loader       │ │ (跳过处理器)     │
    │               │ │ (异步加载)   │ │                  │
    │ Run(step)     │ │ Preload()    │ │ OnSkipRequested()│
    │ RunParallel() │ │ GetOrFallback│ │ ExecuteEffects() │
    └───────────────┘ └──────────────┘ └──────────────────┘
```

### 1. CutsceneService (全局入口 — Autoload)

```csharp
// Core/Cutscene/CutsceneService.cs
public partial class CutsceneService : Node
{
    private CutsceneQueue _queue;
    private CutsceneDirector _director;

    /// <summary>
    /// 播放单段演出。调用方无需关心排队逻辑。
    /// </summary>
    public void PlayCutscene(string scriptId, Dictionary<string, Variant>? context = null)
    {
        var script = CutsceneRegistry.Load(scriptId);
        _queue.Enqueue(new CutsceneRequest(script, context));
        TryPlayNext();
    }

    /// <summary>
    /// 串联播放多段演出。各段独立可跳过，on_complete 分段执行。
    /// </summary>
    public void PlayCutsceneChain(string[] scriptIds, int transitionGapMs = 500)
    {
        var chain = new CutsceneChainRequest(
            scriptIds.Select(id => CutsceneRegistry.Load(id)).ToArray(),
            transitionGapMs
        );
        _queue.EnqueueChain(chain);
        TryPlayNext();
    }

    private void TryPlayNext()
    {
        if (_director.State != CutsceneState.Idle) return;
        if (_queue.IsEmpty) return;
        _director.Begin(_queue.Dequeue());
    }

    // Director 完成后回调
    private void OnDirectorCompleted()
    {
        TryPlayNext(); // 自动消费队列中下一个
    }
}
```

### 2. CutsceneQueue (FIFO 排队 + 串联管理)

```csharp
// Core/Cutscene/CutsceneQueue.cs
public class CutsceneQueue
{
    private readonly Queue<ICutscenePlayable> _queue = new();

    public bool IsEmpty => _queue.Count == 0;

    public void Enqueue(CutsceneRequest request) => _queue.Enqueue(request);

    /// <summary>
    /// 串联请求作为一个 ChainPlayable 入队，内部管理子段推进。
    /// </summary>
    public void EnqueueChain(CutsceneChainRequest chain) => _queue.Enqueue(chain);

    public ICutscenePlayable Dequeue() => _queue.Dequeue();

    /// <summary>
    /// 丢弃过时的 Tier 4 微演出（当高 Tier 演出在队列前方时）。
    /// 规则：Tier 4 请求在队列中且前方有 Tier 1-2，直接丢弃。
    /// </summary>
    public void PruneStale()
    {
        // E6 边界条件：Tier 4 排在 Tier 2 后面可能已过时
    }
}

public interface ICutscenePlayable
{
    CutsceneScript CurrentScript { get; }
    bool HasNext { get; }
    void Advance(); // 串联时推进到下一段
    int TransitionGapMs { get; }
}
```

### 3. CutsceneDirector (播放器状态机)

```csharp
// Core/Cutscene/CutsceneDirector.cs
public partial class CutsceneDirector : Node
{
    public CutsceneState State { get; private set; } = CutsceneState.Idle;

    private ICutscenePlayable _current;
    private GameStateLock _lock;
    private StepExecutor _stepExecutor;
    private SkipHandler _skipHandler;
    private ResourcePreloader _preloader;

    public async void Begin(ICutscenePlayable playable)
    {
        _current = playable;
        await PlayCurrentScript();
    }

    private async Task PlayCurrentScript()
    {
        var script = _current.CurrentScript;

        // LOADING — 异步预加载资源
        SetState(CutsceneState.Loading);
        bool loaded = await _preloader.PreloadAsync(script, TimeoutMs: 3000);
        if (!loaded) GD.PushWarning($"Cutscene {script.Id}: partial load, using fallbacks");

        // 获取全局锁
        _lock.Acquire(script.LockMode);
        HideHud();
        if (script.BgmOverride != null) AudioService.OverrideBgm(script.BgmOverride);

        // PLAYING — 逐步执行
        SetState(CutsceneState.Playing);
        foreach (var step in script.Steps)
        {
            if (State == CutsceneState.Skipping) break;
            await _stepExecutor.Execute(step);
        }

        // COMPLETING_EFFECTS — 保障 on_complete
        SetState(CutsceneState.CompletingEffects);
        foreach (var effect in script.OnComplete)
        {
            effect.Apply();
        }

        // 解锁 + 恢复
        _lock.Release();
        RestoreHud();
        if (script.BgmOverride != null) AudioService.RestoreBgm();

        // 串联 — 推进到下一段或完成
        if (_current.HasNext)
        {
            _current.Advance();
            await TransitionGap(_current.TransitionGapMs);
            await PlayCurrentScript(); // 递归播放下一段
        }
        else
        {
            SetState(CutsceneState.Completed);
            Services.EventBus.Publish(new CutsceneCompletedEvent(script.Id));
            SetState(CutsceneState.Idle);
        }
    }

    private async Task TransitionGap(int ms)
    {
        // 短暂黑屏过渡（fade out → wait → fade in）
        await ScreenFade.FadeOut(150);
        await Task.Delay(ms - 300);
        await ScreenFade.FadeIn(150);
    }
}

public enum CutsceneState
{
    Idle, Loading, Playing, Skipping, CompletingEffects, Completed
}
```

### 4. GameStateLock (全局锁定管理器)

```csharp
// Foundation/GameState/GameStateLock.cs
public partial class GameStateLock : Node
{
    private LockMode _currentMode = LockMode.None;
    private readonly Stack<LockMode> _lockStack = new();

    /// <summary>
    /// 获取指定粒度的全局锁。支持嵌套（罕见场景：Tier 3 播放中触发 Tier 2）。
    /// </summary>
    public void Acquire(LockMode mode)
    {
        _lockStack.Push(mode);
        _currentMode = ResolveHighest();
        Services.EventBus.Publish(new GameStateLockChangedEvent(_currentMode));
    }

    public void Release()
    {
        if (_lockStack.Count > 0) _lockStack.Pop();
        _currentMode = _lockStack.Count > 0 ? ResolveHighest() : LockMode.None;
        Services.EventBus.Publish(new GameStateLockChangedEvent(_currentMode));
    }

    /// <summary>
    /// 各系统查询当前是否被锁定。
    /// </summary>
    public bool IsInputLocked => _currentMode >= LockMode.Partial;
    public bool IsSystemTickLocked => _currentMode == LockMode.Full;
    public bool IsSaveLocked => _currentMode == LockMode.Full;

    private LockMode ResolveHighest() =>
        _lockStack.Count > 0 ? _lockStack.Max() : LockMode.None;
}

public enum LockMode
{
    // 🔗 Shared Foundation enum — 由 ADR-0013 定义，被 ADR-0014, ADR-0017, ADR-0018 引用
    // 命名空间：Game.Foundation.GameState.LockMode
    // 任何 ADR 修改本 enum 必须同步更新所有引用方
    None = 0,     // Tier 4 — 无锁定
    Partial = 1,  // Tier 3 — 禁止输入，允许系统 tick
    Full = 2      // Tier 1-2 — 禁止输入 + 系统 tick + 存档
}
```

### 5. StepExecutor (步骤引擎)

```csharp
// Core/Cutscene/StepExecutor.cs
public class StepExecutor
{
    private readonly Dictionary<StepType, IStepHandler> _handlers = new();

    public StepExecutor()
    {
        _handlers[StepType.ShowImage] = new ShowImageHandler();
        _handlers[StepType.ShowText] = new ShowTextHandler();
        _handlers[StepType.PlayAnimation] = new PlayAnimationHandler();
        _handlers[StepType.CameraMove] = new CameraMoveHandler();
        _handlers[StepType.SlowMotion] = new SlowMotionHandler();   // → TimeScaleController
        _handlers[StepType.ScreenEffect] = new ScreenEffectHandler();
        _handlers[StepType.PlaySfx] = new PlaySfxHandler();         // → AudioService
        _handlers[StepType.PlayBgm] = new PlayBgmHandler();         // → AudioService
        _handlers[StepType.Wait] = new WaitHandler();
        _handlers[StepType.WaitInput] = new WaitInputHandler();
        _handlers[StepType.Parallel] = new ParallelHandler(this);   // 递归
    }

    public async Task Execute(CutsceneStep step)
    {
        if (_handlers.TryGetValue(step.Type, out var handler))
            await handler.Execute(step);
        else
            GD.PushWarning($"Unknown step type: {step.Type}");
    }
}

/// <summary>
/// PARALLEL 步骤处理器 — Task.WhenAll 并行所有子步骤。
/// </summary>
public class ParallelHandler : IStepHandler
{
    private readonly StepExecutor _executor;
    public ParallelHandler(StepExecutor executor) => _executor = executor;

    public async Task Execute(CutsceneStep step)
    {
        var tasks = step.SubSteps.Select(sub => _executor.Execute(sub));
        await Task.WhenAll(tasks);
    }
}
```

### 6. SkipHandler (跳过处理器)

```csharp
// Core/Cutscene/SkipHandler.cs
public class SkipHandler
{
    private float _holdDuration;
    private const float SkipThreshold = 1.0f; // Tuning Knob

    /// <summary>
    /// 每帧检测跳过输入。长按确认键累积时间，达到阈值时触发跳过。
    /// </summary>
    public bool ProcessInput(double delta, bool confirmHeld)
    {
        if (!confirmHeld) { _holdDuration = 0f; return false; }
        _holdDuration += (float)delta;
        UpdateSkipProgressBar(_holdDuration / SkipThreshold);
        return _holdDuration >= SkipThreshold;
    }

    /// <summary>
    /// 跳过后的恢复序列：快速黑屏淡出 → Director 切到 CompletingEffects。
    /// </summary>
    public async Task ExecuteSkipTransition()
    {
        HideSkipProgressBar();
        await ScreenFade.FadeOut(300); // 0.3s 淡出
    }
}
```

### 7. ResourcePreloader (异步预加载 + 超时降级)

```csharp
// Core/Cutscene/ResourcePreloader.cs
public class ResourcePreloader
{
    /// <summary>
    /// 异步预加载演出脚本引用的所有资源。超时后用占位资源替代。
    /// </summary>
    public async Task<bool> PreloadAsync(CutsceneScript script, int TimeoutMs = 3000)
    {
        var paths = ExtractResourcePaths(script);
        var cts = new CancellationTokenSource(TimeoutMs);
        bool allLoaded = true;

        foreach (var path in paths)
        {
            ResourceLoader.LoadThreadedRequest(path);
        }

        // 轮询加载状态直到全部完成或超时
        while (!cts.Token.IsCancellationRequested)
        {
            bool done = true;
            foreach (var path in paths)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(path);
                if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                    { done = false; break; }
                if (status == ResourceLoader.ThreadLoadStatus.Failed)
                    { RegisterFallback(path); allLoaded = false; }
            }
            if (done) break;
            await Task.Delay(16); // ~1 frame
        }

        return allLoaded;
    }

    /// <summary>
    /// 获取资源，缺失时返回占位（纯色 + 脚本 ID 文字）。
    /// </summary>
    public Resource GetOrFallback(string path) { /* ... */ }
}
```

### CanvasLayer 层级分配

| Layer | 用途 | 说明 |
|-------|------|------|
| 90 | Cutscene 底层 | 全屏遮罩（黑屏/淡入淡出） |
| 91 | Cutscene 画面层 | CG 插画 / 像素演出画面 |
| 92 | Cutscene 文字层 | 旁白/对话/章节题字 |
| 93 | Cutscene 特效层 | 水墨扩散/闪白/震动叠加 |
| 95 | Skip UI | 跳过进度条 + 提示文字 |

> ADR-0002 定义 HUD = Layer 10-30, Menu = Layer 50-70。演出层 90+ 确保覆盖所有游戏 UI。

### 与 ADR-0011 TimeScaleController 的集成

SLOW_MOTION 步骤通过 TimeScaleController 请求优先级：

```csharp
// SlowMotionHandler.cs
public class SlowMotionHandler : IStepHandler
{
    public async Task Execute(CutsceneStep step)
    {
        var handle = Services.TimeScaleController.Request(
            targetScale: step.Params.TimeScale,  // 0.1-1.0
            priority: 50,                         // 演出优先级 = 50
            owner: "CutsceneDirector"
        );

        await Task.Delay((int)(step.Params.Duration * 1000));
        handle.Dispose(); // 自动恢复
    }
}
```

### 演出期间 HUD 管理

```csharp
// CutsceneDirector 内部
private void HideHud()
{
    Services.EventBus.Publish(new HudVisibilityRequestEvent(visible: false, requester: "Cutscene"));
}

private void RestoreHud()
{
    Services.EventBus.Publish(new HudVisibilityRequestEvent(visible: true, requester: "Cutscene"));
}
```

> HUD 系统（ADR-0002 管辖）响应此事件，统一控制 CanvasLayer 10-30 的 visible 属性。

## Consequences

### Positive

- **调用方零负担**：任何系统只需 `CutsceneService.PlayCutscene(id)` 一行调用，排队/锁定/跳过全部透明
- **跳过安全**：状态机保障 SKIPPING → COMPLETING_EFFECTS 必经路径，on_complete 不可能被跳过
- **串联灵活**：ChainPlayable 封装使串联逻辑不污染 Director 状态机
- **资源弹性**：3s 超时 + 占位回退确保开发期无崩溃
- **锁定可查询**：GameStateLock 提供 `IsInputLocked` / `IsSystemTickLocked` / `IsSaveLocked` 属性，各系统自行检查

### Negative

- **单例瓶颈**：CutsceneService 是全局 Autoload，热重载时需注意状态残留
- **递归串联**：PlayCurrentScript 递归调用深度 = 串联长度（实际最多 2-3 段，可控）
- **Step 类型扩展**：新增步骤类型需同时添加 Handler + StepType enum，有一定样板代码

### Risks

| 风险 | 影响 | 缓解 |
|------|------|------|
| ResourceLoader 在 FULL 锁定下的行为未验证 | LOADING 阶段可能阻塞 | Spike: 测试 FULL 锁定是否影响后台线程加载 |
| CanvasLayer 90+ 叠加可能干扰 ADR-0002 菜单层 | 菜单无法在演出上层弹出 | 设计约束：FULL 锁定期间禁止打开菜单 |
| 串联中段 on_complete 修改游戏状态后影响下一段演出 | 状态不一致 | 要求各段 on_complete 幂等，且不修改演出上下文 |

## Alternatives Considered

### A. Godot AnimationPlayer Timeline 方案

直接用 AnimationPlayer 的 Track 编排整个演出序列。

**优点**：编辑器可视化预览，设计师友好。
**拒绝原因**：AnimationPlayer 不支持条件分支、不支持 WAIT_INPUT 等待玩家输入、不支持动态加载资源。演出脚本需要程序化控制（if 条件、await 输入、动态 context 替换），AnimationPlayer 无法满足。

### B. 纯事件驱动（无状态机）

每个步骤完成后发布事件，下一个步骤订阅前一个步骤的完成事件。

**优点**：极度解耦。
**拒绝原因**：跳过时需要"跳到末尾"的全局控制，纯事件链无法安全中断中间步骤。状态机提供明确的 SKIPPING 状态来处理这种全局跳转。

### C. GameStateLock 用 InputMap 覆盖而非锁定查询

直接禁用 InputMap 中的 action 来实现输入锁定。

**优点**：物理层面阻止输入。
**拒绝原因**：跳过操作本身需要输入（长按确认键），直接禁用 InputMap 会连跳过也禁掉。查询式锁定让跳过处理器绕过锁定检查。

## Compliance

- **GDD #20 Tier 1-4 分级**：通过 `CutsceneScript.Tier` 映射到 `LockMode`，Step 类型支持所有层级需求
- **跳过保障 (Rule 5, AC2)**：状态机 SKIPPING → COMPLETING_EFFECTS 路径确保 on_complete 执行
- **FIFO 排队 (E2, AC6)**：CutsceneQueue FIFO + PruneStale 处理 Tier 4 过时丢弃
- **串联播放 (Rule 6, AC5)**：CutsceneChainRequest / ICutscenePlayable 接口封装
- **全局锁定 (Rule 7, AC7, AC8)**：GameStateLock 三级 LockMode 精确映射 GDD 锁定表
- **素材降级 (E5, AC9)**：ResourcePreloader 超时回退 + GetOrFallback
- **首次不可跳 (Rule 5, AC4)**：SkipHandler 查询 `cutscene_viewed` 记录
- **HUD 隐藏 (UI Requirements)**：通过 EventBus 通知 HUD 系统统一隐藏/恢复
