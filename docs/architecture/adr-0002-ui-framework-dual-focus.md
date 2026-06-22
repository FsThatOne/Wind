# ADR-0002: UI Framework & Dual-Focus Adaptation

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | UI |
| **Knowledge Risk** | **HIGH** — dual-focus system 是 4.6 新增，行为模型 post-cutoff |
| **References Consulted** | `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/current-best-practices.md`, `docs/engine-reference/godot/breaking-changes.md` |
| **Post-Cutoff APIs Used** | Dual-focus system (4.6), FoldableContainer (4.5), Recursive Control disable (4.5), Screen reader / AccessKit (4.5) |
| **Verification Required** | 1) 验证 grab_focus() 不影响鼠标悬停高亮; 2) 验证键盘焦点和鼠标焦点可同时存在于不同 Control; 3) 验证朦胧化 shader 在 dual-focus 下不干扰输入响应 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. **[2026-06-20 verified for cu-006 scope: Engine.time_scale / Tween ALWAYS / Camera2D.position_smoothing_enabled / InputMap.get_actions — see `production/notes/spike-cu-006-godot-4.7-api-verify-2026-06-20.md`]** |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — UI 层订阅状态变化事件) |
| **Enables** | 所有 Presentation 层模块实现（CombatUi, BlurredUi, Tutorial） |
| **Blocks** | Sprint 3+ (CombatUi 实现), Sprint 4+ (BlurredUi 实现) |
| **Ordering Note** | 必须在任何 UI 实现开始前完成 dual-focus spike 验证 |

## Context

### Problem Statement

《风止》有 5 个 UI 系统（战斗 HUD、朦胧化 UI、CG 演出、教学覆盖、设置面板），全部需要同时支持键鼠和手柄操作（Steam Deck 强制）。Godot 4.6 引入了 dual-focus 机制（鼠标/触摸焦点与键盘/手柄焦点分离），这改变了 UI 输入处理的基础模型。需要决定：是直接使用原生 Control 节点 + dual-focus，还是引入 UI 框架/抽象层。

### Constraints

- 必须支持完整手柄导航（Steam Deck v1.0 目标）
- 禁止 hover-only 交互
- 2D 像素艺术风格 — 无复杂 3D UI 需求
- 朦胧化 UI 需要 shader 效果叠加在标准 Control 上
- 中文文字渲染需清晰（像素字体 + 矢量字体混用）
- Godot 4.6 dual-focus 行为未经 LLM 训练验证（HIGH RISK）

### Requirements

- 键盘/手柄可完整导航所有 UI（无鼠标也能操作）
- 鼠标和手柄同时接入时 UI 反馈正确（不冲突）
- 朦胧化 shader 不阻断 UI 输入事件
- 战斗 HUD 实时刷新 ≤ 1 帧延迟
- 教学覆盖层可叠加在任何场景 UI 之上
- 可测试：UI 逻辑与视觉表现分离

## Decision

采用 **原生 Godot Control 节点 + 薄抽象层** 方案，不引入第三方 UI 框架。

### 核心策略

1. **直接使用 Godot Control 体系**：所有 UI 基于 Control 节点树构建
2. **FocusManager 薄层**：封装 dual-focus 差异，统一处理输入模式切换
3. **InputModeDetector**：自动检测当前输入设备（键鼠 vs 手柄），通知 UI 切换视觉反馈
4. **UI 逻辑/视图分离**：逻辑层通过 EventBus 订阅状态，视图层只负责渲染

### Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│              UI Scene Tree                        │
│                                                  │
│  CanvasLayer (overlay)                           │
│  ├── TutorialOverlay (教学层, 最高 z-index)     │
│  ├── DialoguePanel                               │
│  └── NotificationToast                           │
│                                                  │
│  CanvasLayer (hud)                               │
│  ├── CombatHud                                   │
│  ├── BlurredUiLayer (shader + 信息面板)         │
│  └── MiniMap                                     │
│                                                  │
│  CanvasLayer (world_ui)                          │
│  └── FloatingLabels (NPC 头顶信息)              │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│         FocusManager (Autoload)                   │
│                                                  │
│  InputModeDetector                               │
│  ├── 检测 InputEvent 类型                        │
│  ├── 切换 currentMode: Keyboard | Mouse | Gamepad│
│  └── 发布 InputModeChangedEvent                  │
│                                                  │
│  FocusStack                                      │
│  ├── PushFocus(control) — 记录焦点历史          │
│  ├── PopFocus() — 恢复上一焦点                   │
│  └── 处理 UI 层级切换时的焦点恢复               │
└─────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
// Presentation/Shared/IFocusManager.cs
public interface IFocusManager
{
    InputMode CurrentMode { get; }
    void PushFocus(Control target);
    void PopFocus();
    void ClearStack();
}

public enum InputMode { Keyboard, Mouse, Gamepad }

// Presentation/Shared/InputModeDetector.cs
public partial class InputModeDetector : Node
{
    public InputMode CurrentMode { get; private set; } = InputMode.Keyboard;

    public override void _Input(InputEvent @event)
    {
        var newMode = @event switch
        {
            InputEventMouseMotion or InputEventMouseButton => InputMode.Mouse,
            InputEventJoypadButton or InputEventJoypadMotion => InputMode.Gamepad,
            InputEventKey => InputMode.Keyboard,
            _ => CurrentMode
        };

        if (newMode != CurrentMode)
        {
            CurrentMode = newMode;
            Services.EventBus.Publish(new InputModeChangedEvent(newMode));
        }
    }
}

// Presentation/Shared/FocusStack.cs
public partial class FocusStack : Node
{
    private readonly Stack<NodePath> _stack = new();

    public void PushFocus(Control target)
    {
        var current = GetViewport().GuiGetFocusOwner();
        if (current != null) _stack.Push(current.GetPath());
        target.GrabFocus();
    }

    public void PopFocus()
    {
        if (_stack.TryPop(out var path))
        {
            var node = GetNode<Control>(path);
            node?.GrabFocus();
        }
    }
}

// Presentation/Shared/BaseUiPanel.cs — 所有 UI 面板的基类
public abstract partial class BaseUiPanel : Control
{
    protected IEventBus EventBus => Services.EventBus;
    protected IFocusManager FocusManager => Services.FocusManager;

    public override void _Ready()
    {
        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        EventBus.ClearAllFor(this);
    }

    protected abstract void SubscribeEvents();

    // 脏标记模式 — 子类设置 _dirty = true，统一在 _Process 刷新
    private bool _dirty;
    protected void MarkDirty() => _dirty = true;

    public override void _Process(double delta)
    {
        if (_dirty)
        {
            Refresh();
            _dirty = false;
        }
    }

    protected virtual void Refresh() { }
}
```

### Dual-Focus 适配规则

| 规则 | 实现 |
|------|------|
| 手柄模式下显示焦点高亮 | `InputModeChangedEvent → Gamepad` 时为当前焦点 Control 添加 StyleBox 边框 |
| 鼠标模式下隐藏焦点框 | `InputModeChangedEvent → Mouse` 时移除焦点高亮（hover 自带反馈） |
| 切换到手柄时自动 grab_focus | InputModeDetector 发现手柄输入 → FocusManager 自动 grab 最近一个合理目标 |
| UI 层级切换 | PushFocus/PopFocus 管理焦点栈（如打开背包 → 关闭背包恢复原焦点） |
| 禁止 hover-only | 所有可交互 Control 必须设置 focus_mode = FOCUS_ALL |

### 朦胧化 UI (BlurredUi) 技术方案

- 使用 `ShaderMaterial` 附加到 Control 节点
- Shader 控制模糊度/色调/透明度
- **不阻断输入**：shader 只影响视觉，Control 的 mouse_filter 正常工作
- 信息差模糊：通过 `visible` / `modulate.a` 控制信息元素可见度

### CanvasLayer 层级

| Layer | Z-Order | 用途 |
|-------|---------|------|
| WorldUi | 10 | NPC 头顶标签、交互提示 |
| Hud | 20 | 战斗 HUD、朦胧化面板 |
| Menu | 30 | 暂停菜单、背包、设置 |
| Dialogue | 40 | 对话框 |
| Tutorial | 50 | 教学覆盖 |
| Notification | 60 | Toast、成就弹窗 |

## Alternatives Considered

### Alternative 1: 第三方 UI 框架 (GodotUIFramework / ImGui)

- **Description**: 引入社区 UI 框架替代原生 Control
- **Pros**: 可能有更好的数据绑定、MVVM 模式
- **Cons**: 额外依赖；与 Godot dual-focus 集成未知；社区维护不确定；像素风不需要复杂布局
- **Rejection Reason**: 项目是 2D 像素 UI，复杂度不需要框架；引入第三方增加 HIGH RISK 域的不确定性

### Alternative 2: 完全自研 UI 系统 (绕过 Control)

- **Description**: 用纯 CanvasItem draw_* 调用构建自定义 UI
- **Pros**: 完全控制渲染和输入处理
- **Cons**: 重新造轮子；失去 Control 的布局、主题、focus 管理、Accessibility 支持；开发量巨大
- **Rejection Reason**: 成本不可接受，且放弃了 4.5+ 的 AccessKit 集成

### Alternative 3: 纯 Godot 原生无抽象

- **Description**: 直接使用 Control 节点，不加 FocusManager/BaseUiPanel
- **Pros**: 最简单，无学习成本
- **Cons**: dual-focus 适配逻辑分散在每个 UI 场景中 → 重复代码；焦点管理无统一栈 → 层级切换 bug；无脏标记 → 每帧刷新浪费
- **Rejection Reason**: 5 个 UI 系统的重复代码不可维护；手柄适配需要统一策略

## Consequences

### Positive

- 利用 Godot 原生 Control 体系 — 编辑器可视化编辑、主题系统、AccessKit
- 薄抽象层（~200 行代码）解决 dual-focus 差异
- 焦点栈统一管理所有 UI 层级切换
- 脏标记模式避免不必要的 UI 刷新
- 输入模式自动检测，玩家无需手动切换

### Negative

- FocusManager 是全局状态 — 需注意线程安全（单线程 Godot 环境下无问题）
- 开发者必须继承 BaseUiPanel — 约束性强但统一
- dual-focus 行为在 spike 验证前存在不确定性

### Risks

| 风险 | 缓解 |
|------|------|
| dual-focus 行为与预期不符 | Sprint 1 spike：创建最小 UI 场景，验证键鼠+手柄并行焦点行为 |
| 朦胧化 shader 阻断输入 | 确保 shader 所在 Control 的 mouse_filter = PASS |
| 焦点栈溢出（打开太多层） | 设置最大深度（8），超出时强制清栈 |
| 中文像素字体渲染模糊 | 使用 Godot 的 pixel snap + nearest-neighbor 渲染；矢量字体备选 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| combat-ui.md | 意图指示器 + Burst 动画 + 实时 HUD | BaseUiPanel + 脏标记实现 ≤1 帧刷新 |
| blurred-ui.md | 动态模糊 + 信息差 + 色调联动 | ShaderMaterial + modulate.a 控制 |
| blurred-ui.md | 心境区域驱动 UI 色调变化 | EventBus 订阅 MindsetShiftedEvent → shader uniform 更新 |
| tutorial-onboarding.md | 覆盖层叠加在任何 UI 上 | CanvasLayer z-order 50（最高非 notification） |
| settings-options.md | 输入重映射 + 手柄全导航 | FocusManager + InputModeDetector |
| combat-ui.md | 手柄操作战斗菜单 | focus_mode = FOCUS_ALL + grab_focus on mode switch |
| achievement-steam.md | 成就弹窗通知 | Notification CanvasLayer (z-60) |

## Performance Implications

- **CPU**: InputModeDetector 在每次 InputEvent 时检查类型 — O(1)，可忽略
- **Memory**: FocusStack 最大 8 层 — 可忽略
- **Load Time**: 无影响
- **Network**: N/A

## Migration Plan

首次实现，无迁移需求。Sprint 1 spike 验证 dual-focus 后直接进入实现。

## Validation Criteria

1. **Spike 验证**（Sprint 1）：最小场景含 3 按钮 + 手柄导航 + 鼠标点击并存
2. **手柄测试**：拔掉鼠标后能完整导航所有 UI 面板
3. **焦点栈**：打开背包→打开物品详情→关闭详情→焦点回到背包原位
4. **模式切换**：手柄操作时焦点高亮可见；鼠标操作时高亮隐藏
5. **朦胧化**：shader 开启后 Control 仍能接收 mouse click
6. **性能**：战斗 HUD 刷新不超过 1ms/frame

## Related Decisions

- [ADR-0001](adr-0001-event-bus-architecture.md) — UI 通过 EventBus 订阅状态变化
- [architecture.md](architecture.md) — Section 3.5 定义了 Presentation 层 5 个 UI 模块
