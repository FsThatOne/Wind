---
title: 交互模式库 (Interaction Pattern Library)
project: 风止 (Wind Stops)
status: Draft
version: 1.0
owner: UX Lead
created: 2026-06-09
last_updated: 2026-06-09
related:
  - design/accessibility-requirements.md
  - design/gdd/blurred-ui.md
  - design/gdd/combat-ui.md
  - design/gdd/dialogue-system.md
  - design/gdd/settings-options.md
engine: Godot 4.7-stable
---

# 交互模式库 (Interaction Pattern Library) — v1.0

> 本文件定义《风止》v1.0 的可复用 UI 交互模式（约 25 个），作为 HUD 与各 screen UX spec 的基础参照。所有模式必须满足 `design/accessibility-requirements.md` 定义的 WCAG 2.1 AA Standard tier 要求。
>
> **使用约定**：
> - 任何 screen UX spec 引用模式时，使用 `[Pattern: 2.1 Button/Primary]` 形式标注。
> - 偏离模式必须在该 spec 的 "Pattern Deviations" 节列出原因。
> - 新增模式需通过本文件的 PR 流程，禁止 screen 文档内私造模式。

---

## 1. Pattern Catalog Index

| # | Pattern | Category | A11y 关键章节 | Godot 控件基类 |
|---|---------|----------|---------------|----------------|
| 2.1 | Button (Primary / Secondary / Destructive) | Standard Control | 2.3 / 3.2 | Button |
| 2.2 | Toggle | Standard Control | 2.3 / 3.2 | CheckButton |
| 2.3 | Slider | Standard Control | 3.2 / 6.2 | HSlider |
| 2.4 | Dropdown | Standard Control | 3.2 | OptionButton |
| 2.5 | List Item | Standard Control | 3.2 | ItemList / Container |
| 2.6 | Modal Dialog | Standard Control | 3.2 / 4.2 | AcceptDialog |
| 2.7 | Confirmation Dialog | Standard Control | 4.5 | ConfirmationDialog |
| 2.8 | Toast | Standard Control | 5.1 | Custom + Tween |
| 2.9 | Tooltip | Standard Control | 4.3 | Custom |
| 2.10 | Tab Bar | Standard Control | 3.2 | TabContainer |
| 3.1 | Dialogue Box | Game-Specific | 5.1 / 5.2 / 4.3 | Custom |
| 3.2 | Context Action Prompt | Game-Specific | 3.2 | Custom |
| 3.3 | Inventory Slot | Game-Specific | 2.3 / 3.2 | Custom |
| 3.4 | Ability/Skill Icon | Game-Specific | 2.3 / 4.3 | Custom |
| 3.5 | Health/Resource Bar | Game-Specific | 2.3 / 4.3 | ProgressBar + Label |
| 3.6 | Damage Number | Game-Specific | 4.3 / 2.5 | Label + Tween |
| 4.1 | Focus Management | Navigation | 3.2 | Control.focus_* |
| 4.2 | Escape/Cancel | Navigation | 3.2 | InputMap |
| 4.3 | Screen Push/Pop/Replace | Navigation | — | SceneTree / Custom Stack |
| 5.1 | Loading State | Feedback | 5.1 | Custom |
| 5.2 | Empty State | Feedback | — | Custom |
| 5.3 | Error State | Feedback | 5.1 | Custom |
| 6 | Animation Standards | Standards | 2.5 (Reduce Motion) | Tween / AnimationPlayer |
| 7 | Sound Standards | Standards | 5.4 (5 路音量) | AudioStreamPlayer |

---

## 2. Standard Control Patterns

### 2.1 Button (Primary / Secondary / Destructive)

**用途**：系统内所有可点击/可按下的按钮。按视觉权重分三级，Destructive 强制触发 [Pattern: 2.7 Confirmation Dialog]。

#### Visual Spec

| 属性 | Primary | Secondary | Destructive |
|------|---------|-----------|-------------|
| 背景色 | `token/btn-primary-bg` | `transparent` | `token/btn-destructive-bg` |
| 边框 | 无 | 1px `token/btn-secondary-border` | 无 |
| 文字色 | `token/btn-primary-text` (白/高对比) | `token/btn-secondary-text` | `token/btn-destructive-text` (白) |
| 最小尺寸 | 48×48 dp (touch target) | 同左 | 同左 |
| 圆角 | 4dp | 4dp | 4dp |
| 字号 | Body (跟随 4 档缩放) | Body | Body |
| 图标位 | 可选前置 16dp icon | 同左 | 前置 ⚠ icon (强制双轨) |

**状态矩阵**（5 态）：

| State | 视觉变化 | 附加信息 |
|-------|----------|----------|
| Default | 如上表 | — |
| Hover (鼠标) | 背景亮度 +10% | cursor: pointer |
| Focused (手柄/键盘) | 外框 3dp `token/focus-ring` (对比度 ≥3:1) | 焦点框不裁切，外扩 2dp |
| Pressed | 缩放 0.96 + 背景暗度 −10% | 持续 ≤100ms (Fast tier) |
| Disabled | 透明度 0.4 + 无交互 | aria-disabled / focus 跳过 |

> Hover 与 Focused 可同时生效（鼠标悬停后键盘切换焦点时叠加态）。

#### Interaction

| 输入方式 | 触发条件 | 备注 |
|----------|----------|------|
| 鼠标左键 | Click (release) | — |
| 键盘 | Enter / Space (release) | 非 repeat |
| 手柄 | A / Cross (release) | 通过 Focus Management 到达 |
| 触屏 (Steam Deck) | Tap release | Touch target ≥48dp |

**音效**：
- Hover: `ui_hover` (subtle tick)
- Pressed: `ui_confirm` (Primary/Secondary) / `ui_warning` (Destructive)
- Disabled 点击: `ui_deny` (soft buzz)

**动效** (参照 Section 6 Animation Standards)：
- Hover 过渡: Tween, 100ms, ease-out
- Press 缩放: Tween, 100ms, ease-in-out
- Reduce Motion 替代: 无缩放动画，仅即时颜色切换

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 焦点可见性 ≥3:1 | `a11y §3.2` | Focus ring token |
| 触摸目标 ≥48dp | `a11y §3.3` | min_size 属性 |
| 双轨编码 | `a11y §2.3` | Destructive: 颜色 + ⚠ 图标 + "危险操作"文字提示 |
| 字号跟随缩放 | `a11y §2.1` | Theme font_size binding |
| Reduce Motion | `a11y §2.5` | 取消 scale 动画 |
| 100% 键盘/手柄可达 | `a11y §3.2` | focus_mode = ALL |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: Button (继承 BaseButton)
# Theme Override:
#   - StyleBoxFlat: normal / hover / pressed / disabled / focus
#   - font_size: 绑定全局 accessibility_font_scale
#   - focus: StyleBoxFlat 外扩 2dp, border_width=3, border_color=token/focus-ring

# Destructive 子类:
#   - 点击时 emit signal("destructive_action_requested")
#   - 由父容器捕获并弹出 ConfirmationDialog (Pattern 2.7)
#   - 不在 Button 内部完成确认逻辑（关注点分离）

# Tween 动效:
# var tween = create_tween()
# tween.tween_property(self, "scale", Vector2(0.96, 0.96), 0.1)
# Reduce Motion check:
# if AccessibilitySettings.reduce_motion:
#     return  # skip animation

# focus_entered signal → play "ui_hover"
# pressed signal → play "ui_confirm" / "ui_warning"
```

#### Examples (出现位置)

| 场景 | 类型 | 示例文字 |
|------|------|----------|
| 主菜单 | Primary | "新游戏" / "继续" |
| 主菜单 | Secondary | "设置" / "退出" |
| 存档界面 | Destructive | "删除存档" → 触发 Confirmation |
| 战斗内 | Primary | "确认行动" |
| 商店 | Secondary | "购买" (非不可逆) |
| 设置 | Destructive | "重置所有设置" |

### 2.2 Toggle

**用途**：二态开关，用于设置项的 On/Off 切换（如"数值化模式""Reduce Motion"等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 尺寸 | 48×24 dp (滑轨) + 20dp 圆形滑块 |
| Off 态 | 滑轨 `token/toggle-off-bg` (灰)，滑块居左 |
| On 态 | 滑轨 `token/toggle-on-bg` (主题色)，滑块居右 |
| 标签 | 左侧 Body 字号文字说明 |
| 焦点框 | 外扩 2dp `token/focus-ring` |

**状态**：Default / Hover / Focused / Disabled（同 Button 5 态模式）

#### Interaction

| 输入 | 行为 |
|------|------|
| Click / A / Enter / Space | 切换状态 |
| 手柄 D-Pad 左右 | 同上（方向可切换） |

**音效**：`ui_toggle` (click)
**动效**：滑块位移 Fast (100ms) ease-out；Reduce Motion 下即时跳转

#### Accessibility

| 要求 | 实现 |
|------|------|
| 双轨 (`a11y §2.3`) | 颜色 + 位置 + 可选 "ON/OFF" 文字 |
| 焦点可见 (`a11y §3.2`) | focus ring |
| 48dp 触摸目标 (`a11y §3.3`) | min_size |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: CheckButton (内建 Toggle 样式)
# Theme: StyleBoxFlat for checked / unchecked
# 滑块动画: Tween position.x, 0.1s
```

### 2.3 Slider

**用途**：连续值调节（音量、打字机速度、亮度等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 滑轨 | 宽 200dp × 高 4dp，`token/slider-track` |
| 填充 | 左侧已填区 `token/slider-fill` |
| 滑块 | 20dp 圆形，`token/slider-thumb` |
| 数值标签 | 右侧显示当前值（如 "75%"） |
| 焦点框 | 滑块外扩 focus ring |
| 刻度 (可选) | 离散档位时显示刻度点 |

#### Interaction

| 输入 | 行为 |
|------|------|
| 鼠标拖拽 | 连续调节 |
| 键盘 ←/→ | 步进 ±5%（或 ±1 档） |
| 手柄 D-Pad ←/→ | 同上 |
| 手柄 LT/RT | 快步进 ±20% |

**音效**：步进时 `ui_slider_tick`（每档 tick）
**动效**：无（数值即时响应）

#### Accessibility

| 要求 | 实现 |
|------|------|
| 数值可见 (`a11y §4.3`) | 始终显示数值标签 |
| 键盘可操作 (`a11y §3.2`) | 方向键步进 |
| 焦点可见 (`a11y §3.2`) | 滑块 focus ring |
| 48dp 触摸目标 (`a11y §3.3`) | 滑块有效区域扩展至 48dp |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: HSlider
# Theme: grabber / grabber_highlight / slider (StyleBoxFlat)
# 数值标签: 子 Label 绑定 value_changed signal
# 离散模式: step = 0.25 (4 档)
```

### 2.4 Dropdown

**用途**：从预设列表选择单项（语言、分辨率、难度选择等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 触发器 | 48dp 高，显示当前选项 + ▼ 箭头 |
| 展开列表 | 向下展开，max 6 项可见后滚动 |
| 项高度 | 40dp，hover/focused 高亮 |
| 焦点框 | 触发器 + 列表项均有 focus ring |
| 圆角 | 4dp |

#### Interaction

| 操作 | 行为 |
|------|------|
| Click / A / Enter / Space | 展开/收起列表 |
| ↑/↓ (展开时) | 导航列表项 |
| A / Enter (列表项) | 选中并收起 |
| Esc / B | 取消并收起 |

**音效**：展开 `ui_dropdown_open`，选中 `ui_confirm`，取消 `ui_back`

#### Accessibility

| 要求 | 实现 |
|------|------|
| 键盘全可操作 (`a11y §3.2`) | 方向键导航 |
| 焦点可见 (`a11y §3.2`) | 每项 focus ring |
| Focus Trap | 展开时焦点限定在列表内 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: OptionButton
# PopupMenu: 自定义 Theme (项高 40dp, hover StyleBox)
# focus_mode = ALL
```

### 2.5 List Item

**用途**：垂直列表中的单行条目（存档列表、物品列表、设置项列表等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 行高 | 56dp (含 8dp 上下 padding) |
| 背景 | 偶数行 `token/list-item-alt`，奇数行 transparent |
| 选中态 | `token/list-item-selected` + 左侧 3dp 色条 |
| 焦点态 | focus ring (与选中可叠加) |
| Hover 态 | 背景 +5% 亮度 |
| 内容 | 左 icon (可选) + 主文本 + 副文本 (右对齐/下方) |

#### Interaction

| 输入 | 行为 |
|------|------|
| ↑/↓ | 导航相邻项 |
| A / Enter / Click | 选中/激活当前项 |
| 长按 / 右键 (可选) | 上下文菜单 |

**音效**：导航 `ui_hover`，选中 `ui_confirm`

#### Accessibility

| 要求 | 实现 |
|------|------|
| 焦点可见 (`a11y §3.2`) | focus ring |
| Hover = Focus 视觉一致 | 鼠标 hover 与手柄 focus 同样高亮 |
| 48dp 可触区域 (`a11y §3.3`) | 行高 56dp ≥ 48dp |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: ItemList / VBoxContainer + 自定义 ListItemControl
# focus_neighbor: 自动线性（同 Container 内）
# 斑马纹: modulate 交替
```

### 2.6 Modal Dialog

**用途**：全屏/半屏覆盖层，阻断背景交互，用于需要用户明确响应的操作。参照 [Pattern: 4.1 Focus Management] 的 Focus Trap 规则。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 背景遮罩 | `token/overlay-bg` (黑 60% alpha) |
| 面板宽度 | 固定 480dp (720p) / 640dp (1080p) |
| 面板圆角 | 8dp |
| 面板背景 | `token/surface-elevated` |
| 阴影 | drop-shadow 4dp blur |
| CanvasLayer | 92 (演出系统 90-95 范围内，低于 Cutscene 95) |
| 标题 | H3 字号，居左 |
| 内容区 | Body 字号，最大高度 60vh 后内滚动 |
| 操作区 | 底部右对齐，Primary + Secondary 按钮（参照 [Pattern: 2.1 Button]） |

**状态**：
- 打开：Slow tier (400ms) 遮罩淡入 + 面板 scale 0.95→1.0；Reduce Motion 下仅 alpha 淡入 200ms
- 关闭：反向动画 Normal tier (200ms)

#### Interaction

| 操作 | 行为 |
|------|------|
| 主操作按钮 (Primary) | 执行操作 + 关闭 Modal |
| 取消按钮 (Secondary) | 关闭 Modal，不执行 |
| Esc / 手柄 B | 等价取消按钮 |
| 点击遮罩 | 等价取消（可配置为禁止，如强制确认场景） |
| × 按钮 (右上角) | 等价取消 |

**Focus Trap**：
- 打开时首焦点 → 主操作按钮（非 Destructive 场景）
- Tab 循环限定在 Modal 内
- 关闭后焦点回归触发元素

**音效**：
- 打开: `ui_modal_open` (soft whoosh)
- 关闭: `ui_modal_close` (subtle click)

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| Focus Trap | `a11y §3.2` | 参照 [Pattern: 4.1] |
| 首焦点自动 | `a11y §3.2` | grab_focus on ready |
| Esc 可关闭 | `a11y §3.2` | ui_cancel action |
| Reduce Motion | `a11y §2.5` | 参照 Section 6 替代表 |
| 遮罩不完全遮盖背景 (认知) | `a11y §4.2` | 60% alpha 保留空间感 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: AcceptDialog / 自定义 Control
# CanvasLayer: 92
# 遮罩: ColorRect(Color(0, 0, 0, 0.6)), mouse_filter = STOP
# 面板: PanelContainer + StyleBoxFlat
# Focus Trap: 参照 4.1 实现
# 动画: UIAnimationHelper.animate_appear(panel, 0.4)
```

### 2.7 Confirmation Dialog

**用途**：Modal Dialog 的特化子类，用于不可逆/高风险操作的二次确认。由 [Pattern: 2.1 Button/Destructive] 强制触发。

#### Visual Spec（继承 2.6 + 以下差异）

| 差异属性 | 值 |
|----------|------|
| 标题前缀 | ⚠ icon (双轨编码) |
| 说明文字 | 明确描述后果 + 警告色 `token/text-warning` |
| 主操作按钮 | Destructive 样式（红底白字 + ⚠ icon） |
| 默认焦点 | **取消按钮**（非主操作！防误触） |
| 面板边框 | 左侧 4dp `token/border-warning` 色条 |

#### Interaction（继承 2.6 + 以下差异）

| 操作 | 行为 |
|------|------|
| 主操作 (Destructive) | 执行不可逆操作 + 关闭 |
| 取消 | 关闭，不执行 |
| 点击遮罩 | **禁止关闭**（强制明确选择） |
| Esc / 手柄 B | 等价取消 |

**特殊场景**（难度选择等锁定操作，参照 `a11y §4.5`）：
- 标题明确标注"此操作不可修改"
- 说明文字使用警告色
- 可选：要求键入确认文字（仅"删除存档"等极端场景）

**音效**：
- 打开: `ui_warning_open` (deeper tone than normal modal)
- 确认执行: `ui_destructive_confirm`
- 取消: `ui_modal_close`

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 默认焦点指向取消 | `a11y §4.5` | 防止方向键误触确认 |
| 警告双轨编码 | `a11y §2.3` | 颜色 + ⚠ 图标 + 文字 |
| 遮罩不可关闭 | — | 强制用户明确选择 |
| 难度锁定提示 | `a11y §4.5` | "无法修改"显式警告 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: ConfirmationDialog / 自定义继承 Modal
# 与 2.6 共享 CanvasLayer 92
# 默认焦点覆写:
#   func _ready():
#       $CancelButton.grab_focus()  # 非 OK 按钮
# 遮罩点击屏蔽:
#   overlay.mouse_filter = STOP  # 但不 emit close signal
# Destructive signal 链:
#   confirmed signal → emit("destructive_confirmed", action_id)
```

#### Examples

| 场景 | 标题 | 说明 |
|------|------|------|
| 删除存档 | "⚠ 删除存档" | "存档「{name}」将被永久删除，无法恢复。" |
| 重置设置 | "⚠ 重置所有设置" | "所有选项将恢复为默认值。" |
| 新建存档选难度 | "⚠ 确认难度选择" | "难度「{level}」将锁定到此存档，无法中途修改。" |
| 放弃未保存 | "⚠ 放弃更改" | "你有未保存的更改，退出后将丢失。" |

### 2.8 Toast

**用途**：非阻断式临时通知（保存成功、见闻录解锁、成就获得等），不抢焦点。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 位置 | 屏幕顶部居中，距顶 32dp |
| 尺寸 | 自适应文本宽度，max 60% 屏宽，高 48dp |
| 背景 | `token/surface-toast` (深色 80% alpha) |
| 圆角 | 24dp (胶囊形) |
| 字号 | Body |
| 图标 | 左侧 16dp icon (✓ / ⚠ / ℹ) |
| 生存时间 | 4s (默认) / 6s (长文本) |

#### Interaction

| 操作 | 行为 |
|------|------|
| 自动 | 4-6s 后自动消失 |
| 点击 Toast | 立即消失（可选） |
| 无焦点抢占 | Toast 不参与 Focus 系统 |

**动效**：滑入 Normal (200ms) ease-out → 停留 → 淡出 Normal (200ms)
**Reduce Motion**：即时出现，4s 后即时消失
**音效**：`ui_toast` (gentle chime)

**堆叠**：多 Toast 垂直排列，旧 Toast 上移

#### Accessibility

| 要求 | 实现 |
|------|------|
| 不抢焦点 (`a11y §4.2`) | focus_mode = NONE |
| 双轨 (`a11y §2.3`) | 图标 + 文字 + 颜色 |
| 生存时间充足 (`a11y §4.2`) | ≥4s |
| Reduce Motion (`a11y §2.5`) | 无滑入动画 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# CanvasLayer: 93 (高于 Modal 92，确保可见)
# focus_mode = NONE
# Timer: 4s → queue_free
# 动画: UIAnimationHelper.animate_appear / disappear
```

### 2.9 Tooltip

**用途**：Hover/Focus 时显示补充信息（物品描述、技能详情、设置说明等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 延迟 | Hover 后 500ms 出现 / Focus 后 800ms 出现 |
| 位置 | 触发元素上方 8dp（空间不足则翻转到下方） |
| 尺寸 | 自适应，max 宽 320dp |
| 背景 | `token/surface-tooltip` (深色 90% alpha) |
| 圆角 | 4dp |
| 字号 | Small (Body × 0.85) |
| 箭头 | 指向触发元素的 6dp 三角 |

#### Interaction

| 操作 | 行为 |
|------|------|
| Hover 离开 | 立即消失 |
| Focus 离开 | 立即消失 |
| 手柄 (无 Hover) | Focus 800ms 后自动显示 |
| 触屏 | 长按 500ms 触发 |

**动效**：alpha 淡入 Fast (100ms)；Reduce Motion 下即时出现
**音效**：无

#### Accessibility

| 要求 | 实现 |
|------|------|
| 手柄可触发 (`a11y §3.2`) | Focus 自动显示 |
| 对比度 (`a11y §2.2`) | 深色背景保证 ≥4.5:1 |
| 不遮挡触发元素 | 位置计算避开 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: Control.tooltip_text (内建) 或自定义 RichTooltip
# 自定义时: PopupPanel + Timer(0.5s) + 位置翻转逻辑
# 手柄模式: focus_entered → start timer → show tooltip
```

### 2.10 Tab Bar

**用途**：同层多页签切换（设置分类、角色面板分类、见闻录分类等）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 位置 | 内容区顶部水平排列 |
| Tab 高度 | 40dp |
| 活跃态 | 底部 3dp 下划线 `token/tab-active` + 文字加粗 |
| 非活跃态 | 无下划线，文字 `token/text-secondary` |
| 焦点态 | focus ring 包围整个 Tab |
| 指示器动画 | 下划线滑动 Normal (200ms) |

#### Interaction

| 输入 | 行为 |
|------|------|
| Click / A / Enter | 切换到该 Tab |
| ←/→ (Tab 间) | 导航相邻 Tab |
| LB/RB (手柄) | 快捷 Tab 切换（任何焦点位置） |

**音效**：切换 `ui_tab_switch`
**Reduce Motion**：下划线即时跳转，无滑动

#### Accessibility

| 要求 | 实现 |
|------|------|
| LB/RB 快捷切换 (`a11y §3.2`) | 全局监听 |
| 焦点可见 (`a11y §3.2`) | Tab focus ring |
| 当前 Tab 状态标识 | 视觉 + aria-selected |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: TabContainer / TabBar + 自定义内容切换
# LB/RB 快捷: _unhandled_input 监听 shoulder buttons
# 下划线: Line2D 或 ColorRect, Tween position.x
```

---

## 3. Game-Specific UI Patterns

### 3.1 Dialogue Box

**用途**：叙事/对话的核心载体。《风止》采用"朦胧化"文学笔触，Dialogue Box 同时承载：NPC 对话、内心独白、旁白描述、见闻录文本。需满足字幕全覆盖 + 打字机速度可调 + 说话人标识。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 位置 | 屏幕底部，占底 25%-30% 高度 |
| 宽度 | 满屏减左右 margin 32dp |
| 背景 | `token/surface-dialogue` (半透明深色，70% alpha) + 顶部 1px 分割线 |
| 圆角 | 顶部 8dp，底部 0 (贴底) |
| 说话人名牌 | 左上角外挂标签，`token/text-speaker-name`，H4 字号 |
| 正文字号 | Body (跟随 4 档缩放：100%/125%/150%/200%) |
| 行高 | 1.6em |
| 最大行数 | 4 行（超出后分页，显示"▼"翻页提示） |
| 立绘区 | 左侧 128dp (720p) / 192dp (1080p)，可选显示角色头像/半身像 |

**文本类型差异**：

| 类型 | 名牌 | 正文样式 | 备注 |
|------|------|----------|------|
| NPC 对话 | 显示说话人名 | 正常 | 标准模式 |
| 主角内心 | "（内心）" | 斜体 + `token/text-inner-thought` | 与对话区分 |
| 旁白/描述 | 无名牌 | 正常 + 居中 | 叙事段落 |
| 系统提示 | "【系统】" | `token/text-system` | 见闻录解锁等 |

#### Interaction

| 操作 | 行为 |
|------|------|
| 确认键 (A/Enter/Click) — 打字中 | 跳过打字机动画，全文即现 |
| 确认键 — 全文已显示 | 前进到下一段 / 关闭对话 |
| Cancel (B/Esc) | 无操作（对话中不可 Cancel 退出，除非在选项界面） |
| 长按确认键 (≥500ms) | 自动前进模式 (Auto) 切换 |
| LB/RB | 回顾历史对话 (Log) |

**打字机效果**：
- 默认速度: 30 字符/秒
- 可调范围: 15 / 30 / 60 / 即时（4 档，设置菜单可调）
- Reduce Motion: 强制即时显示（跳过打字机）
- 标点停顿: 逗号 +100ms，句号 +200ms，省略号 +400ms

**音效**：
- 打字: `dialogue_type` (每 N 字符 tick，频率可调)
- 翻页: `dialogue_advance`
- Auto 模式切换: `ui_toggle`

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 字幕全覆盖 | `a11y §5.1` | 所有语音/旁白均有文本 |
| 说话人标识 | `a11y §5.2` | 名牌 + 颜色标识（双轨：颜色 + 名字文本） |
| 字号 4 档 | `a11y §2.1` | Theme font_size binding |
| 打字机可调/可关 | `a11y §4.3` | 设置菜单速度档位 |
| Reduce Motion | `a11y §2.5` | 跳过打字机动画 |
| 对比度 ≥4.5:1 | `a11y §2.2` | token/surface-dialogue 深色保证 |
| 暂停 (认知) | `a11y §4.2` | 对话等待确认，不自动前进（除非 Auto 模式） |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: Control (自定义 DialogueBox.tscn)
# 子节点:
#   - SpeakerLabel: Label (名牌)
#   - DialogueText: RichTextLabel (支持 BBCode: 斜体/颜色)
#   - Portrait: TextureRect (立绘)
#   - PageIndicator: Label ("▼")
#   - AutoModeIcon: TextureRect
#
# 打字机实现:
#   RichTextLabel.visible_ratio (0.0 → 1.0)
#   var tween = create_tween()
#   var duration = text.length() / chars_per_second
#   tween.tween_property($DialogueText, "visible_ratio", 1.0, duration)
#
# Reduce Motion:
#   if AccessibilitySettings.reduce_motion:
#       $DialogueText.visible_ratio = 1.0  # 即时
#
# 标点停顿:
#   使用 tween.tween_callback() 在标点位置插入 delay
#
# 历史 Log:
#   维护 Array[DialogueEntry]，LB/RB 打开 Log 覆盖层
```

#### Examples

| 场景 | 类型 | 示例 |
|------|------|------|
| NPC 对话 | 名牌 "林师兄" | "此剑名曰「停云」，取自陶潜之诗。" |
| 主角内心 | 名牌 "（内心）" | *他话中似有深意，却不知是试探还是善意。* |
| 旁白 | 无名牌 | 落日余晖洒在枯叶上，远处传来断续的笛声。 |
| 系统 | 名牌 "【系统】" | 见闻录已更新：「停云剑」条目解锁。 |

### 3.2 Context Action Prompt

**用途**：探索中靠近可交互对象时显示的按键提示（"按 E 交谈""按 A 调查"）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 位置 | 可交互对象上方 / 屏幕底部中央 |
| 构成 | 按键 Glyph + 动作文字 (如 "[A] 交谈") |
| Glyph | 使用 Steam Input Action Glyph API 自动匹配当前设备 |
| 背景 | 轻量 pill 形，`token/surface-prompt` (50% alpha) |
| 字号 | Body |
| 出现条件 | 进入交互半径 (通常 1.5m) |

#### Interaction

| 输入 | 行为 |
|------|------|
| 对应按键 | 执行交互动作 |
| 离开范围 | 提示消失 |
| 多目标 | 最近目标优先，可用 ↑/↓ 切换 |

**动效**：淡入 Fast (100ms)；Reduce Motion 下即时
**音效**：出现时无；执行时由目标系统负责

#### Accessibility

| 要求 | 实现 |
|------|------|
| Glyph 自适应设备 (`a11y §6.1`) | Steam Input API |
| 文字说明 (`a11y §2.3`) | 图标 + 文字双轨 |
| 对比度 (`a11y §2.2`) | pill 背景保证可读 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# InteractionArea (Area2D/3D) → 进入时 show prompt
# Glyph: GodotSteam.getActionGlyphForOrigin() 获取当前设备图标
# 多目标: 按距离排序，显示最近 + "↕ 切换" 提示
```

### 3.3 Inventory Slot

**用途**：物品格子（背包、装备栏、商店）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 尺寸 | 64×64 dp |
| 背景 | `token/slot-bg` (深灰边框格) |
| 物品图标 | 48×48 dp 居中 |
| 数量标签 | 右下角小字 (如 "×3") |
| 品质边框 | 按稀有度颜色 (白/绿/蓝/紫/金) + 角标图标 (双轨) |
| 空格 | 虚线边框 |
| 选中态 | 外框高亮 `token/slot-selected` |
| 焦点态 | focus ring |

#### Interaction

| 输入 | 行为 |
|------|------|
| A / Click | 选中 / 打开物品详情 |
| Y / 右键 | 快捷使用/装备 |
| 长按 / X | 批量操作 |
| D-Pad | 网格导航 (上下左右) |

**音效**：导航 `ui_hover`，选中 `ui_confirm`，装备 `ui_equip`
**Tooltip**：Focus 800ms 后显示物品详情 (参照 [Pattern: 2.9])

#### Accessibility

| 要求 | 实现 |
|------|------|
| 双轨编码 (`a11y §2.3`) | 品质：颜色 + 角标图标 + Tooltip 文字 |
| 焦点可见 (`a11y §3.2`) | focus ring |
| 网格导航 (`a11y §3.2`) | focus_neighbor 四方向 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: TextureButton / Control
# 网格: GridContainer, focus_neighbor 自动推导
# 品质: enum ItemRarity → 颜色 + icon 映射
```

### 3.4 Ability/Skill Icon

**用途**：战斗中武学招式/技能的图标按钮（含冷却、内力消耗、快捷键绑定）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 尺寸 | 56×56 dp |
| 图标 | 48×48 dp 招式图标 |
| 冷却遮罩 | 顺时针扇形灰色遮罩 + 倒计时数字 |
| 内力消耗 | 底部小标签 "30" (蓝色 ◆ 前缀) |
| 快捷键 Glyph | 右上角 16dp (键盘: "Q"/"W"... / 手柄: glyph) |
| 不可用态 | 灰度 + 红色斜杠（内力不足） |
| 焦点态 | focus ring |

#### Interaction

| 输入 | 行为 |
|------|------|
| Click / 快捷键 | 选择该技能 |
| Focus + A | 同上 |
| Focus 800ms | 显示 Tooltip (技能详细描述) |

**音效**：选中 `combat_skill_select`，不可用 `ui_deny`
**动效**：选中时 scale 1.05 弹出 (Fast 100ms)；冷却扇形为连续动画

#### Accessibility

| 要求 | 实现 |
|------|------|
| 双轨 (`a11y §2.3`) | 不可用：灰度 + 斜杠 + "内力不足" Tooltip |
| 数值化 (`a11y §4.3`) | 冷却数字 + 内力数字始终可见 |
| Glyph 自适应 (`a11y §6.1`) | Steam Input |
| Reduce Motion (`a11y §2.5`) | 无 scale 弹出 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: TextureButton
# 冷却遮罩: 自定义 _draw() 扇形 (arc + polygon)
# Glyph: 子 TextureRect 绑定 Steam Input
# 不可用: modulate = Color(0.4, 0.4, 0.4) + overlay slash texture
```

### 3.5 Health/Resource Bar

**用途**：战斗中 HP/内力/气势等资源的实时可视化条。《风止》Burst+Read 回合制需要清晰传达当前资源占比及变化量。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 尺寸 | 宽 200dp × 高 16dp (可随 HUD 缩放) |
| 背景 | `token/bar-bg` (深灰) |
| 填充 | 渐变色 (左→右)，按资源类型区分 |
| 变化量指示 | 半透明延迟条 (damage ghost)，Normal tier (200ms) 后追赶 |
| 数值标签 | 可选叠加在条上（数值化模式下强制显示） |
| 边框 | 1px `token/bar-border` |
| 圆角 | 2dp |

**资源类型颜色 + 双轨**：

| 资源 | 填充色 | 图标标识 | 数值化模式 |
|------|--------|----------|------------|
| HP (生命) | `token/bar-hp` (红系) | ❤ 心形 | "245/300" |
| 内力 | `token/bar-qi` (蓝系) | ◆ 菱形 | "80/120" |
| 气势 (Burst) | `token/bar-burst` (金系) | ⚡ 闪电 | "3/5 段" |
| 护盾/减伤 | `token/bar-shield` (灰白系) | 🛡 盾 | "+50" |

> **双轨编码**（`a11y §2.3`）：颜色 + 图标 + 数值三路同时传达，不依赖单一通道。

#### Interaction

| 事件 | 行为 |
|------|------|
| 受伤 | 当前值即时降低 + ghost 条 200ms 后追赶 |
| 治疗 | 当前值即时增加（无 ghost） |
| Burst 蓄力 | 金条分段填充 + 满段闪烁 |
| 值归零 | 条变空 + 边框闪红 2 次 (Fast 100ms) |

**数值化模式**（`a11y §4.3`）：
- 开启时：条上叠加精确数值 "当前/最大"
- 关闭时（默认朦胧化）：仅显示条 + 图标，无精确数值

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 双轨编码 | `a11y §2.3` | 颜色 + 图标 + 可选数值 |
| 数值化模式 | `a11y §4.3` | 精确数值叠加 |
| Reduce Motion | `a11y §2.5` | ghost 条取消追赶动画，即时跳至目标 |
| 对比度 | `a11y §2.2` | 填充色与背景 ≥3:1 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: Control (自定义 ResourceBar.tscn)
# 子节点:
#   - Background: ColorRect (token/bar-bg)
#   - GhostFill: ColorRect (延迟条)
#   - CurrentFill: ColorRect (当前值)
#   - Icon: TextureRect (资源图标)
#   - ValueLabel: Label (数值化模式)
#
# 更新逻辑:
# func set_value(current: float, max_val: float):
#     var ratio = current / max_val
#     $CurrentFill.size.x = bar_width * ratio
#     if AccessibilitySettings.numerify_mode:
#         $ValueLabel.text = "%d/%d" % [current, max_val]
#         $ValueLabel.visible = true
#     # Ghost 追赶:
#     if not AccessibilitySettings.reduce_motion:
#         var tween = create_tween()
#         tween.tween_property($GhostFill, "size:x",
#             bar_width * ratio, 0.2).set_delay(0.2)
#     else:
#         $GhostFill.size.x = bar_width * ratio
```

### 3.6 Damage Number

**用途**：战斗中伤害/治疗/状态数值的飘字反馈。数值化模式核心载体之一。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 字号 | H2 (大伤害) / H3 (普通) / Body (小数值) |
| 字体 | 等宽/装饰体（战斗专用） |
| 阴影 | 2dp 黑色描边（保证任何背景可读） |
| 生存时间 | 1.5s |

**类型差异**：

| 类型 | 颜色 | 图标前缀 | 动画 |
|------|------|----------|------|
| 物理伤害 | `token/dmg-physical` (白/金) | ⚔ | 向上飘 + 淡出 |
| 内力伤害 | `token/dmg-qi` (蓝紫) | ◆ | 向上飘 + 淡出 |
| 治疗 | `token/dmg-heal` (绿) | ＋ | 向上飘 + 淡出 |
| 暴击 | 字号 H2 + 放大弹出 | 💥 | scale 1.3→1.0 + 飘 |
| 格挡/减免 | `token/dmg-block` (灰) | 🛡 | 无飘动，原地淡出 |
| 状态文字 | Body 字号 | 无 | "中毒""眩晕" 原地浮现 |

> **双轨编码**：颜色 + 图标前缀 + 数值文字三路。

#### Interaction

- 多数值堆叠：垂直偏移 24dp 防重叠
- 快速连击：合并显示（500ms 内同目标累加 → 显示总计）
- 数值化模式下：增加精确百分比 "(-12%HP)"

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 双轨编码 | `a11y §2.3` | 颜色 + 图标 + 数值 |
| Reduce Motion | `a11y §2.5` | 无飘动，固定位置 1.5s 后淡出 |
| 数值化模式 | `a11y §4.3` | 精确百分比附加信息 |
| 可读性 | `a11y §2.2` | 描边保证对比度 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 基类: Label / RichTextLabel (动态实例化)
# 父节点: CanvasLayer 91 (HUD 层)
#
# 飘字逻辑:
# func spawn_damage(value: int, type: DamageType, position: Vector2):
#     var label = damage_label_scene.instantiate()
#     label.text = _format(value, type)  # 含图标前缀
#     label.position = position + _stack_offset()
#     add_child(label)
#
#     if AccessibilitySettings.reduce_motion:
#         # 无飘动，原地显示后淡出
#         var tween = create_tween()
#         tween.tween_interval(1.5)
#         tween.tween_property(label, "modulate:a", 0.0, 0.1)
#         tween.tween_callback(label.queue_free)
#     else:
#         var tween = create_tween()
#         tween.tween_property(label, "position:y",
#             position.y - 40, 1.0).set_ease(Tween.EASE_OUT)
#         tween.parallel().tween_property(label, "modulate:a",
#             0.0, 0.3).set_delay(1.2)
#         tween.tween_callback(label.queue_free)
#
# 暴击放大:
#     label.scale = Vector2(1.3, 1.3)
#     tween.tween_property(label, "scale", Vector2.ONE, 0.15)
```

---

## 4. Navigation Patterns

### 4.1 Focus Management

**用途**：确保所有可交互元素可通过键盘/手柄/触屏导航到达，焦点状态始终可见。本模式是 accessibility §3.2 (100% 手柄可达) 的实现基础。

#### 核心规则

| 规则 | 说明 |
|------|------|
| **100% 可达** | 所有可交互 UI 必须 `focus_mode = ALL`，不可存在仅鼠标可达的交互 |
| **焦点可见** | 焦点框对比度 ≥3:1，使用统一 `token/focus-ring` (3dp 外扩 2dp) |
| **首焦点** | 每个 Screen/Modal 打开时自动设置首焦点（通常为主操作按钮） |
| **焦点回归** | Modal 关闭后焦点必须回到触发元素 |
| **焦点顺序** | 遵循视觉布局的自然阅读序（左→右、上→下） |
| **Focus Trap** | Modal Dialog 内焦点不可逃逸到背景层，Tab 循环在 Modal 内 |

#### 导航映射

| 输入 | 动作 | 备注 |
|------|------|------|
| 手柄 D-Pad / 左摇杆 | 空间方向导航 (上/下/左/右) | Godot 内建 focus_neighbor 系统 |
| 键盘 Tab | 下一焦点 | 线性序列 |
| 键盘 Shift+Tab | 上一焦点 | 反向 |
| 手柄 LB/RB | Tab Bar 切换 / Section 跳转 | 快捷跳过多个元素 |
| 手柄 A / 键盘 Enter | 激活当前焦点 | 等价鼠标 Click |
| 手柄 B / 键盘 Esc | Cancel / Back | 参照 [Pattern: 4.2 Escape/Cancel] |

#### Focus Neighbor 策略

```
空间导航优先级:
1. 显式 focus_neighbor_* 属性（手动指定）
2. Godot 自动空间最近匹配
3. Container 内线性排列 fallback

设计约束:
- 同一 Container 内子元素间 focus_neighbor 自动推导
- 跨 Container 必须手动设置 focus_neighbor 桥接
- 循环焦点：列表末尾 → 列表首项（wrap around, 可配置关闭）
```

#### Modal Focus Trap 实现

```
Modal 打开:
1. 记录 previous_focus_owner
2. 设置 Modal 首焦点（主操作 or 取消按钮，Destructive 场景焦点默认指向取消）
3. 劫持 Tab 循环范围至 Modal 子树

Modal 关闭:
1. 释放 Focus Trap
2. 恢复 previous_focus_owner.grab_focus()
3. 若 previous_focus_owner 已不可见，fallback 到 Screen 首焦点
```

#### 鼠标/手柄混合输入切换

| 检测条件 | 行为 |
|----------|------|
| 检测到鼠标移动 | 隐藏焦点框，显示 cursor |
| 检测到手柄/键盘输入 | 显示焦点框，隐藏 cursor，焦点跳到最近交互元素 |
| 切换延迟 | 50ms debounce 防抖 |

> Steam Deck 触摸板模拟鼠标时视为鼠标输入；物理按键视为手柄输入。

#### Accessibility

| 要求 | 关联章节 | 实现 |
|------|----------|------|
| 焦点可见性 ≥3:1 | `a11y §3.2` | token/focus-ring 统一样式 |
| 100% 手柄可达 | `a11y §3.2` | focus_mode = ALL 强制 |
| Focus Trap (Modal) | `a11y §3.2` | 防止焦点逃逸到遮罩后 |
| 首焦点自动设置 | `a11y §3.2` | 避免用户"盲导航" |
| 输入方式无缝切换 | `a11y §3.3 (Steam Deck)` | 50ms debounce |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 核心 API:
# Control.focus_mode = FOCUS_ALL
# Control.focus_neighbor_top / bottom / left / right
# Control.focus_next / focus_previous (Tab 序)
# Control.grab_focus()
# Control.release_focus()

# 焦点样式 (Theme):
# StyleBoxFlat "focus":
#   border_width_* = 3
#   expand_margin_* = 2
#   border_color = token/focus-ring

# 输入切换检测:
# func _input(event):
#     if event is InputEventMouseMotion:
#         switch_to_mouse_mode()
#     elif event is InputEventJoypadButton or event is InputEventKey:
#         switch_to_focus_mode()

# Focus Trap (Modal 场景):
# 在 Modal 的 _ready() 中:
#   _previous_focus = get_viewport().gui_get_focus_owner()
#   $FirstButton.grab_focus()
#   set_process_unhandled_key_input(true)  # 拦截 Tab 范围

# Modal _exit_tree():
#   if is_instance_valid(_previous_focus):
#       _previous_focus.grab_focus()
```

#### 验证清单

- [ ] 每个 Screen 有且仅有一个首焦点
- [ ] Modal 关闭后焦点回归正确
- [ ] 纯手柄模式可遍历所有交互元素（无死角）
- [ ] 鼠标↔手柄切换平滑无闪烁
- [ ] Focus ring 在所有背景色下对比度 ≥3:1

### 4.2 Escape/Cancel

**用途**：定义统一的"返回/取消"语义栈，确保任何场景下 Esc/B 按键行为可预测。

#### 核心规则

| 规则 | 说明 |
|------|------|
| **统一退出栈** | 每层 UI 入栈时注册退出回调，Esc/B 触发栈顶回调 |
| **优先级** | Confirmation Dialog > Modal > 子菜单 > 当前 Screen > 暂停菜单 |
| **空栈行为** | 在游戏世界中按 Esc/B → 打开暂停菜单 |
| **不可取消场景** | 演出锁定 (LockMode.Full) 期间屏蔽 Esc（参照 ADR-0013） |

#### Cancel Stack 语义

```
示例: 主菜单 → 设置 → 音频 Tab → Slider 调节中

Cancel Stack (从栈顶到底):
4. [Slider 活跃] → 放弃修改，回到非活跃态
3. [音频 Tab]   → 不做任何事（Tab 不入栈，用 LB/RB 切换）
2. [设置 Screen] → Pop 回主菜单
1. [主菜单]      → 弹出退出确认 Dialog

战斗中:
Cancel Stack:
3. [行动确认 Modal] → 关闭 Modal，回到指令选择
2. [指令选择]       → 取消当前选择，回到角色选择
1. [角色选择]       → 无更上层（或打开暂停菜单，若设计允许）
```

#### 输入映射

| 输入 | Action Name | 备注 |
|------|-------------|------|
| 键盘 Esc | `ui_cancel` | Godot 内建 |
| 手柄 B / Circle | `ui_cancel` | Steam Input 映射 |
| 鼠标右键 | 不绑定 Cancel | 避免与游戏内右键冲突 |

#### 音效

- Cancel 成功: `ui_back` (soft pop)
- Cancel 被屏蔽 (锁定期间): `ui_deny` (buzz)

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# CancelStack (Autoload 单例):
# var _stack: Array[Callable] = []
#
# func push(callback: Callable) -> void:
#     _stack.push_back(callback)
#
# func pop() -> void:
#     if _stack.is_empty():
#         open_pause_menu()
#         return
#     var cb = _stack.pop_back()
#     cb.call()
#     AudioManager.play("ui_back")
#
# func _unhandled_input(event):
#     if event.is_action_pressed("ui_cancel"):
#         if GameStateLock.current_mode == LockMode.FULL:
#             AudioManager.play("ui_deny")
#             return
#         pop()
#         get_viewport().set_input_as_handled()
```

### 4.3 Screen Push/Pop/Replace

**用途**：定义 Screen 级别的导航模型（类 Activity Stack），确保转场动画、焦点恢复、Cancel 栈同步。

#### 导航操作

| 操作 | 语义 | 动画 | Cancel 栈 |
|------|------|------|-----------|
| **Push** | 新 Screen 覆盖当前（保留旧 Screen 状态） | 新 Screen 从右滑入 (Slow 400ms) | 旧 Screen 注册 pop 回调 |
| **Pop** | 销毁栈顶 Screen，回到下层 | 当前 Screen 右滑出 | 移除栈顶回调 |
| **Replace** | 销毁当前 + 新 Screen 替换（不保留旧态） | 交叉淡入淡出 (Normal 200ms) | 替换栈顶回调 |

#### 焦点处理

| 时机 | 行为 |
|------|------|
| Push 前 | 记录当前 Screen 焦点位置 |
| Push 后 | 新 Screen 设置首焦点 |
| Pop 后 | 恢复下层 Screen 记录的焦点位置 |
| Replace 后 | 新 Screen 设置首焦点（旧焦点不保留） |

#### Reduce Motion 替代

| 正常 | 替代 |
|------|------|
| 滑入/滑出 | 即时 alpha 淡入淡出 (Fast 100ms) |
| 交叉淡入淡出 | 即时切换 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# ScreenManager (Autoload 单例):
# var _screen_stack: Array[PackedScene] = []
# var _focus_memory: Dictionary = {}  # screen_path → NodePath
#
# func push_screen(scene: PackedScene):
#     _save_focus()
#     var new_screen = scene.instantiate()
#     add_child(new_screen)
#     UIAnimationHelper.slide_in_from_right(new_screen, 0.4)
#     CancelStack.push(pop_screen)
#     _screen_stack.push_back(scene)
#
# func pop_screen():
#     var current = get_children().back()
#     UIAnimationHelper.slide_out_to_right(current, 0.2)
#     await current.tree_exited
#     _screen_stack.pop_back()
#     _restore_focus()
#
# func replace_screen(scene: PackedScene):
#     var current = get_children().back()
#     var new_screen = scene.instantiate()
#     UIAnimationHelper.cross_fade(current, new_screen, 0.2)
#     _screen_stack[-1] = scene
#     CancelStack.replace_top(pop_screen)
```

---

## 5. Feedback & Loading Patterns

### 5.1 Loading State

**用途**：异步加载期间的占位反馈（场景切换、数据加载）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 全屏加载 | 黑底 + 中央旋转图标 (32dp) + "加载中..." 文字 |
| 局部加载 | 替换内容区域为骨架屏 (shimmer placeholder) |
| 进度条 (可选) | 底部细条 `token/progress-fill`，有确定进度时显示 |
| 提示文字 (可选) | 随机游戏小贴士 / 背景故事 |

**动效**：旋转图标 360°循环 (1s/圈)；Reduce Motion 下改为脉冲 alpha 闪烁
**音效**：无（避免循环音疲劳）

#### Accessibility

| 要求 | 实现 |
|------|------|
| Reduce Motion (`a11y §2.5`) | 脉冲替代旋转 |
| 加载时间 (`a11y` 性能约束) | ≤10s Deck / ≤5s PC SSD |
| 文字可读 | 对比度 ≥4.5:1 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 全屏: CanvasLayer 94 + ColorRect + AnimatedSprite2D
# 局部: Placeholder Control + Tween shimmer (modulate.a 0.3↔0.7)
# 进度: ProgressBar, value 绑定 ResourceLoader.load_threaded_get_status()
```

### 5.2 Empty State

**用途**：列表/容器无内容时的空态指引（空背包、无存档、无搜索结果）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 布局 | 垂直居中：图标 (64dp) + 标题 (H3) + 说明 (Body) + 可选 CTA 按钮 |
| 图标 | 灰色线稿插图（背包空、卷轴空等） |
| 文字色 | `token/text-secondary` |
| CTA | Primary Button（如"前往商店"） |

**音效**：无
**动效**：淡入 Normal (200ms)

#### Accessibility

| 要求 | 实现 |
|------|------|
| 文字说明清晰 (`a11y §4.2`) | 明确告知"为什么空" + "下一步" |
| CTA 可聚焦 (`a11y §3.2`) | 首焦点设在 CTA 按钮 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 通用 EmptyState.tscn (可复用):
#   $Icon: TextureRect
#   $Title: Label
#   $Description: Label
#   $CTAButton: Button (可选, visible = has_action)
```

### 5.3 Error State

**用途**：操作失败时的错误反馈（网络错误、存档损坏、加载失败）。

#### Visual Spec

| 属性 | 值 |
|------|------|
| 布局 | 同 Empty State + 错误图标 (⚠ 红色) |
| 标题 | H3，`token/text-error` |
| 说明 | Body，描述原因 + 建议操作 |
| 操作按钮 | "重试" (Primary) + "返回" (Secondary) |
| 错误码 | 底部小字 `token/text-muted`（开发者模式下可见） |

**音效**：`ui_error` (单次)

#### Accessibility

| 要求 | 实现 |
|------|------|
| 双轨 (`a11y §2.3`) | ⚠ 图标 + 红色 + "错误" 文字 |
| 首焦点 | "重试"按钮 |
| 描述清晰 (`a11y §4.2`) | 非技术用语 + 明确建议 |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 复用 EmptyState.tscn 布局 + 错误主题覆写
# 错误码: $ErrorCode.visible = DeveloperSettings.show_debug
# 重试: signal("retry_requested")
```

---

## 6. Animation Standards

**用途**：定义全局 UI 动效规范——统一时长/缓动曲线/选型规则，并确保 Reduce Motion 模式下所有动画有对应降级方案。

#### Timing Tiers

| Tier | 时长 | 用途 | 示例 |
|------|------|------|------|
| **Instant** | 0ms | 状态切换（无过渡） | Toggle On/Off、Reduce Motion 替代 |
| **Fast** | 100ms | 微交互反馈 | Button Press、Hover 色变 |
| **Normal** | 200ms | 元素进出 | Toast 滑入、Tab 切换内容淡入 |
| **Slow** | 400ms | 大面积转场 | Screen Push/Pop、Modal 弹出 |
| **Cinematic** | 600-1000ms | 仅用于演出/过场 | 章节标题飘入（不用于 UI 操作） |

> **原则**：交互响应动画不超过 Normal (200ms)。用户主动操作后 100ms 内必须有视觉反馈（即使最终动画未完成）。

#### Easing 曲线标准

| 场景 | Easing | Godot Tween 常量 |
|------|--------|-------------------|
| 元素出现 (Appear) | ease-out (减速) | `Tween.EASE_OUT` + `Tween.TRANS_CUBIC` |
| 元素消失 (Disappear) | ease-in (加速) | `Tween.EASE_IN` + `Tween.TRANS_CUBIC` |
| 位移/缩放 | ease-in-out | `Tween.EASE_IN_OUT` + `Tween.TRANS_CUBIC` |
| 弹性反馈 | ease-out + overshoot | `Tween.TRANS_BACK` (仅 Cinematic) |
| 线性 | linear | `Tween.TRANS_LINEAR` (进度条/倒计时) |

#### Tween vs AnimationPlayer 选型

| 条件 | 选用 | 原因 |
|------|------|------|
| 单属性、程序化参数 | **Tween** | 灵活、无 .anim 资源开销 |
| 多属性同步、含音效/回调关键帧 | **AnimationPlayer** | 可视化编辑、精确同步 |
| 纯 UI 微交互 (hover/press/focus) | **Tween** | 统一代码路径 |
| 演出/过场动画 | **AnimationPlayer** | ADR-0013 CutsceneService 管辖 |
| 需要 Reduce Motion 立即跳过 | **Tween** (set target value directly) | 无需管理 AnimationPlayer seek |

> **TimeScale 注意**：UI Tween 必须使用 `set_speed_scale(1.0)` 或绑定 `process_mode = ALWAYS`，不受战斗 TimeScaleController 影响（参照 ADR-0013 技术规范）。

#### Reduce Motion 替代映射表

| 正常动效 | Reduce Motion 替代 | 实现 |
|----------|---------------------|------|
| 滑入/滑出 (translate) | 即时出现/消失 (alpha 0→1) | 跳过 position tween，改 modulate.a |
| 缩放弹出 (scale) | 即时出现 | 跳过 scale tween |
| 旋转/翻转 | 即时切换 | 无动画 |
| 淡入淡出 (alpha) | **保留** (≤200ms) | 不取消，仅缩短至 Fast tier |
| 打字机效果 (Dialogue) | 全文即时显示 | 跳过逐字 tween |
| Damage Number 飘动 | 固定位置显示 1.5s 后消失 | 无 position 动画 |
| 屏幕震动 (Camera Shake) | 边框闪烁替代 | 无 position offset |
| 粒子效果 (UI 层) | 静态图标替代 | 隐藏 GPUParticles2D |

#### 性能约束

| 约束 | 值 | 依据 |
|------|------|------|
| 同屏并发 Tween 上限 | ≤8 个 | Steam Deck 30 FPS 预算 |
| 单帧 Tween 创建 | ≤3 个 | 避免 GC spike |
| AnimationPlayer 同屏 | ≤2 个 (UI 层) | CanvasLayer 90-95 限定 |
| UI 动画总帧预算 | ≤2ms/frame | 16.6ms 帧预算中 UI 占比 ≤12% |

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# 全局动画工具类 (建议):
# class_name UIAnimationHelper

# static func animate_appear(node: Control, duration_tier: float = 0.2):
#     if AccessibilitySettings.reduce_motion:
#         node.modulate.a = 1.0
#         return
#     node.modulate.a = 0.0
#     var tween = node.create_tween()
#     tween.tween_property(node, "modulate:a", 1.0, duration_tier) \
#         .set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

# static func animate_press(node: Control):
#     if AccessibilitySettings.reduce_motion:
#         return
#     var tween = node.create_tween()
#     tween.tween_property(node, "scale", Vector2(0.96, 0.96), 0.1) \
#         .set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
#     tween.tween_property(node, "scale", Vector2.ONE, 0.1) \
#         .set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

# TimeScale 隔离:
# UI tween 创建后调用:
#   tween.set_speed_scale(1.0)
#   # 或将 UI 根节点 process_mode = Node.PROCESS_MODE_ALWAYS
```

#### 验证清单

- [ ] 所有 UI 动画时长不超过 Slow tier (400ms)
- [ ] Reduce Motion 开启时无位移/缩放/旋转动画
- [ ] UI Tween 不受战斗 TimeScale 影响
- [ ] 同屏 Tween ≤8 验证（Steam Deck profiling）
- [ ] Cinematic tier 仅出现在演出系统管辖范围

---

## 7. Sound Standards

**用途**：定义 UI 音效的统一事件→类别映射表，确保全局一致性和可替换性。

#### 音效类别表

| 类别 ID | 用途 | 特征 | 示例事件 |
|---------|------|------|----------|
| `ui_hover` | 焦点移动 / 鼠标悬停 | subtle tick, 极短 | Button Hover, List Item 导航 |
| `ui_confirm` | 确认/选中 (非破坏性) | 清脆 click | Button Primary press, 选中列表项 |
| `ui_back` | 返回/取消 | soft pop | Esc/B, Modal 关闭 |
| `ui_deny` | 不可用/禁止 | soft buzz | Disabled 点击, 锁定期间 Esc |
| `ui_toggle` | 开关切换 | click variant | Toggle On/Off |
| `ui_warning` | 破坏性操作 | deeper tone | Destructive Button press |
| `ui_modal_open` | Modal 打开 | soft whoosh | Modal/Dialog 弹出 |
| `ui_modal_close` | Modal 关闭 | subtle click | Modal 收起 |
| `ui_warning_open` | 危险确认弹出 | deeper whoosh | Confirmation Dialog 打开 |
| `ui_destructive_confirm` | 不可逆操作确认 | heavy click | 删除/重置确认 |
| `ui_tab_switch` | Tab 切换 | light swipe | Tab Bar 切换 |
| `ui_slider_tick` | Slider 步进 | tick (音高随值变化) | 音量 Slider 调节 |
| `ui_dropdown_open` | 下拉展开 | pop | Dropdown 展开 |
| `ui_toast` | 通知出现 | gentle chime | Toast 弹出 |
| `ui_error` | 错误发生 | alert tone (单次) | Error State 出现 |
| `dialogue_type` | 对话打字机 | 每 N 字符 tick | Dialogue Box 打字 |
| `dialogue_advance` | 对话翻页 | page turn | 下一段对话 |
| `combat_skill_select` | 技能选中 | combat-themed click | Ability Icon 选中 |
| `ui_equip` | 装备穿戴 | metallic click | Inventory 装备操作 |

#### 音量通道归属

| 通道 | 控制的类别 |
|------|-----------|
| UI 音效 | 所有 `ui_*` 类别 |
| 对话音效 | `dialogue_*` 类别 |
| 战斗 UI | `combat_*` 类别 |

> 每个通道独立音量 Slider（参照 `a11y §5.4`）。

#### 设计原则

1. **不循环**：UI 音效为单次触发，不用循环 (除 Loading 选择性省略音效)
2. **可叠加**：快速操作时允许多音效叠加（polyphony ≤3 同类型）
3. **音高变化**：连续快速触发同一音效时，微调音高 ±5% 避免机械感
4. **Reduce Motion 不影响音效**：动画可关，音效保留（除非用户关闭 UI 音效通道）
5. **无障碍音效视觉化**（`a11y §5.3`）：可选开启"音效 → 屏幕闪烁/图标"映射

#### Implementation Notes (Godot 4.7-stable)

```gdscript
# AudioManager (Autoload 单例):
# var _pools: Dictionary = {}  # category → AudioStreamPlayer pool
#
# func play(category: StringName, pitch_variation: float = 0.05):
#     var player = _get_available_player(category)
#     player.pitch_scale = randf_range(1.0 - pitch_variation, 1.0 + pitch_variation)
#     player.play()
#
# Bus 映射:
#   "UI" bus → ui_* 类别
#   "Dialogue" bus → dialogue_* 类别
#   "CombatUI" bus → combat_* 类别
#
# 音效资源:
#   res://audio/ui/{category_id}.ogg (统一命名)
```

---

## 8. Open Questions

### v1.0 已决
- 范围裁剪：保留 25 个核心模式，延后 Optimistic UI / Notification Banner / Progress Bar (长任务) / Grid Item / Input Field 至有具体 screen 需要时再补。

### v1.0 延后项（待具体场景驱动）
- Q1: Input Field（仅在存档命名/搜索时使用，待对应 screen UX spec 时定义）
- Q2: Grid Item（无库存矩阵布局明确需求时不必）
- Q3: Notification Banner / Progress Bar（长任务场景未定）
- Q4: Optimistic UI（联机/异步操作未规划）

### 待 Pre-Production 阶段澄清
- Q5: Damage Number 在数值化模式关闭时的视觉降级方案（仍需可见还是完全文学化）？
- Q6: Tooltip 在手柄场景下的触发方式细节（长按 vs 专用键）？
- Q7: Tab Bar 在嵌套层级 ≥3 时是否拆为 SegmentedControl？

---

## 9. Audit History

| 日期 | 版本 | 变更 | 审计人 |
|------|------|------|--------|
| 2026-06-09 | v1.0-skeleton | 初始 skeleton 创建，确认 25 模式范围 | UX Lead |

---

> **下一步**：按 Phase 4 协议逐节填充。优先级建议：2.1 Button → 2.6/2.7 Modal & Confirmation → 4.1 Focus Management → 3.1 Dialogue Box → 3.5 Health/Resource Bar → 3.6 Damage Number → 6 Animation Standards → 余下模式。
