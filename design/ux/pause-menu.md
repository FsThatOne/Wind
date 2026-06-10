---
title: UX Specification — 暂停菜单 (Pause Menu)
project: 风止 (Wind Stops)
status: Draft
version: 1.0
author: UX Lead
created: 2026-06-09
last_updated: 2026-06-09
platform_targets: PC (Steam), Steam Deck
accessibility_tier: Standard (WCAG 2.1 AA)
screen_name: PauseMenuScreen
related:
  - design/ux/interaction-patterns.md
  - design/ux/main-menu.md
  - design/ux/hud.md
  - design/accessibility-requirements.md
  - docs/architecture/adr-0013-cutscene-system.md
---

# UX Specification: 暂停菜单 (Pause Menu)

> **Scope**: 玩家在探索态/非演出锁定状态下按下暂停键时呈现的叠加画面。
> 提供继续、存档、设置、返回主菜单等功能。游戏世界在此画面打开期间暂停。

---

## 1. Purpose & Player Need

**What player need does this screen serve?**

让玩家在任何安全时刻可以暂时离开游戏世界——保存进度、调整设置、或安全退出——而不必担心角色处于危险中。暂停菜单是玩家与现实世界的接口，它承诺"你随时可以安全地放下控制器"。

**The player goal**:

在 2 次按键内完成最常见操作（继续游戏或快速存档），无需任何等待。

**The game goal**:

提供暂停期间的系统访问入口（存档/设置/退出），同时不破坏游戏沉浸感（画面不完全遮挡世界）。

---

## 2. Player Context on Arrival

| Question | Answer |
|----------|--------|
| What was the player just doing? | 探索世界 / 对话结束后 / 非战斗状态 |
| What is their emotional state? | 需要休息、想保存进度、或需要调整设置 |
| What cognitive load are they carrying? | 中等——仍记得当前任务和位置 |
| What information do they already have? | 知道自己在游戏中的位置和状态 |
| What are they most likely trying to do? | 快速存档后继续 (最高频) 或调整设置 |
| What are they likely afraid of? | 失去未保存的进度；不确定退出是否安全 |

**Emotional design target for this screen**:

安心感——玩家确认"我的进度是安全的"，然后快速回到游戏世界。暂停菜单不应是一个让人想停留的地方。

---

## 3. Navigation Position

**Screen hierarchy**:

```
GameWorld (Exploration)
  └── PauseMenuScreen (Overlay — game paused)
        ├── SaveGameScreen (存档管理)
        ├── SettingsScreen (设置 — 与主菜单共用)
        └── ConfirmDialog (返回主菜单确认)
```

**Modal behavior**: Overlay (renders over game world, game paused)

- 按 Esc/B/Start 关闭（CancelStack 顶层）
- 背景为模糊化的游戏世界画面（Gaussian blur + 30% 暗色叠加）

**Reachability — all entry points**:

| Entry Point | Triggered By | Notes |
|-------------|-------------|-------|
| 探索态按 Esc/Start | 玩家主动暂停 | 主入口；仅探索态可用 |
| Steam Deck Quick Menu 返回 | 系统级暂停后恢复 | 自动显示暂停菜单 |

**不可进入的状态**（暂停被屏蔽）：
- 战斗态 (Burst/Read)
- 演出锁定 (LockMode.Full / LockMode.Partial)
- 对话进行中 (DialogueManager.is_active)
- Loading 画面

---

## 4. Entry & Exit Points

**Entry table**:

| Trigger | Source | Transition Type | Data Passed In | Notes |
|---------|--------|-----------------|----------------|-------|
| Esc / Start 按键 | 探索态 GameWorld | Overlay push (blur in) | 当前场景名、游玩时长 | 游戏暂停 (Tree.paused = true) |

**Exit table**:

| Exit Action | Destination | Transition Type | Data Returned | Notes |
|-------------|------------|-----------------|---------------|-------|
| "继续" / Esc / B | GameWorld (恢复) | Overlay pop (blur out) | — | 游戏恢复 |
| "存档" → 完成 | PauseMenu (保持) | 子画面 pop | save_success: bool | 存档成功 Toast |
| "设置" → 返回 | PauseMenu (保持) | 子画面 pop | — | 设置已自动保存 |
| "返回主菜单" + 确认 | MainMenuScreen | Fade to black → Replace | — | 清除游戏状态 |
| "退出游戏" + 确认 | 应用关闭 | Fade to black → quit | — | 提示"进度已自动保存" |

---

## 5. Layout Specification

### 5.1 Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│              {模糊化游戏世界背景 + 30% 暗色遮罩}              │
│                                                             │
│                                                             │
│                     ╔═══════════════════╗                    │
│                     ║                   ║                    │
│                     ║    暂 停          ║  ← 标题            │
│                     ║                   ║                    │
│                     ║  ● [ 继续 ]       ║  ← 默认焦点        │
│                     ║    [ 存档 ]       ║                    │
│                     ║    [ 设置 ]       ║                    │
│                     ║    [ 返回主菜单 ] ║                    │
│                     ║    [ 退出游戏 ]   ║                    │
│                     ║                   ║                    │
│                     ║ ─────────────────  ║                    │
│                     ║ 章节: 第二章 · 寒山 ║  ← 当前进度提示    │
│                     ║ 游玩: 3h 27m       ║                    │
│                     ╚═══════════════════╝                    │
│                                                             │
│                                                             │
│  [Esc] 继续                                                  │ ← 底部操作提示
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Zone Definitions

| Zone Name | Description | Size | Scrollable? | Overflow |
|-----------|-------------|------|-------------|----------|
| Background | 模糊化游戏世界 + 遮罩 | 100% viewport | No | Clip |
| Panel | 居中半透明暗色面板 | 360×400 dp | No | — |
| Title | "暂停" 标题 | Panel 内顶部 | No | — |
| Menu List | 纵向按钮列表 | Panel 内中部，宽 280dp | No | 最多 5 项 |
| Info | 当前章节 + 游玩时长 | Panel 内底部 | No | 截断 |
| Hint Bar | 底部操作提示 | 全宽底部 48dp | No | — |

### 5.3 Component Inventory

| Component | Type | Zone | Purpose | Reuses? |
|-----------|------|------|---------|---------|
| 模糊遮罩 | BackBufferCopy + Shader | Background | 暂停感 + 聚焦面板 | No — 专属 |
| 面板容器 | PanelContainer | Panel | 菜单承载 | Yes — 通用暗面板 |
| 菜单按钮 | Button (§2.1) | Menu List | 导航入口 | Yes — 标准 Button |
| 章节 Label | Label | Info | 当前位置提示 | Yes — 通用 |
| 时长 Label | Label | Info | 游玩统计 | Yes — 通用 |
| 操作提示 | Label + Glyph | Hint Bar | 提示关闭方式 | Yes — Context Prompt 风格 |

**Primary focus element on open**: "继续"

---

## 6. States & Variants

| State | Trigger | Visual Changes | Behavioral Changes | Notes |
|-------|---------|---------------|-------------------|-------|
| 标准态 | 正常暂停 | 全部按钮显示 | 焦点在"继续" | 最常见 |
| 退出确认 | 选择"返回主菜单"或"退出游戏" | ConfirmationDialog 叠加 | Focus Trap 在 Dialog | 默认焦点在"取消" |
| 存档成功反馈 | SaveGameScreen 返回成功 | Toast 显示"已保存" | 焦点回到"存档" | Toast 4s 消失 |
| 子画面打开 | 选择"存档"或"设置" | Panel 滑出，子画面滑入 | — | PauseMenu 保持在栈中 |

---

## 7. Interaction Map

### 7.1 Navigation Inputs

| Input | Platform | Action | Visual | Audio | Notes |
|-------|----------|--------|--------|-------|-------|
| ↑↓ / D-Pad | All | 菜单项间移动 | Focus 框移动 | ui_navigate | 循环导航 |
| Mouse Hover | PC | 高亮项 | Hover 态 | — | — |

### 7.2 Action Inputs

| Input | Platform | Context | Action | Audio | Notes |
|-------|----------|---------|--------|-------|-------|
| Enter / A | All | 菜单项 focused | 激活 | ui_confirm | — |
| Esc / B / Start | All | 暂停菜单顶层 | 关闭暂停（= "继续"） | ui_back | CancelStack pop |
| Esc / B | All | ConfirmationDialog | 取消 | ui_back | — |
| Esc / B | All | 子画面 (存档/设置) | 返回暂停菜单 | ui_back | — |

### 7.3 State-Specific Behaviors

| State | Input Restriction | Reason |
|-------|------------------|--------|
| 退出确认 Dialog | 仅 Confirm/Cancel | Modal Focus Trap |
| 子画面打开中 | 暂停菜单不可交互 | 焦点在子画面 |

---

## 8. Data Requirements

| Data Element | Source | Update Frequency | Format | Null Handling |
|--------------|--------|-----------------|--------|---------------|
| 当前章节名 | StoryManager | On screen open | string | 显示"—" |
| 游玩时长 | SessionTimer | On screen open | "{h}h {m}m" | "0h 0m" |
| 当前场景名 | SceneManager | On screen open | string (内部用) | — |
| 自动存档时间 | SaveManager | On screen open | DateTime | 不显示 (如无自动存档) |

---

## 9. Events Fired

| Player Action | Event | Payload | Receiver | Notes |
|---------------|-------|---------|----------|-------|
| 打开暂停菜单 | GamePaused | {scene, playtime} | TimeController, AudioManager | 暂停游戏 + 降低 BGM 音量 |
| 关闭暂停菜单 | GameResumed | {} | TimeController, AudioManager | 恢复游戏 |
| 返回主菜单确认 | ReturnToMainMenuRequested | {} | SceneLoader | 清除状态 → 加载主菜单 |
| 退出游戏确认 | QuitGameRequested | {} | Application | 安全退出 |

---

## 10. Transition & Animation

| Transition | Trigger | Type | Duration | Easing | Reduce Motion |
|------------|---------|------|----------|--------|---------------|
| 打开 — 背景模糊 | Esc/Start | Gaussian blur 0→8px + 遮罩 alpha 0→0.3 | Normal 200ms | ease-out | Instant (遮罩直接出现，无 blur) |
| 打开 — 面板入场 | 背景模糊完成后 | Scale 0.95→1.0 + alpha 0→1 | Normal 200ms | ease-out | Instant 出现 |
| 关闭 — 面板退场 | "继续"/Esc | Alpha 1→0 + scale 1.0→0.95 | Fast 100ms | ease-in | Instant 消失 |
| 关闭 — 背景恢复 | 面板退场后 | Blur 8→0px + 遮罩 alpha 0.3→0 | Normal 200ms | ease-in | Instant |
| 焦点移动 | 方向键 | Focus 框位移 | Fast 100ms | ease-out | Instant |
| 子画面切换 | 选择"存档"/"设置" | Panel slide left，子画面 slide in from right | Normal 200ms | ease-out | Instant |
| 确认 Dialog 弹出 | 选择"退出"/"返回主菜单" | Dialog scale 0.95→1.0 + 遮罩 | Normal 200ms | ease-out | Instant |

---

## 11. Accessibility Requirements

**Text contrast**:

| Element | Background | Required | Notes |
|---------|-----------|----------|-------|
| 菜单按钮文字 | 暗面板 (~#1a1a1a 80% alpha) | ≥4.5:1 | — |
| 标题"暂停" | 暗面板 | ≥3:1 (大文本) | — |
| 章节/时长信息 | 暗面板 | ≥4.5:1 | 辅助信息，使用 secondary 色 |
| 操作提示 | 模糊背景 + 暗条 | ≥4.5:1 | — |

**Focus management**:
- 打开时自动 `grab_focus()` → "继续"
- 关闭时焦点恢复到游戏世界（无需特殊处理，HUD 无 focusable 元素）
- ConfirmationDialog 使用 Focus Trap (interaction-patterns §2.7)
- 循环导航（最后一项↓回到第一项）

**Screen Reader**:

| State Change | Announcement |
|--------------|-------------|
| 菜单打开 | "游戏已暂停。暂停菜单，5 个选项。" |
| 焦点移动 | 按钮文字 |
| 确认 Dialog 打开 | "确认：是否{动作}？未保存进度将丢失。" |

**Reduce Motion**: 参见 §10 Reduce Motion 列。核心变化：背景不使用模糊动画（直接叠加静态遮罩）。

---

## 12. Acceptance Criteria

**Performance**
- [ ] 暂停菜单从按键到首帧可见 ≤100ms（暂停反馈必须即时）
- [ ] 背景模糊不引起帧率下降（使用 BackBufferCopy 缓存，非实时模糊）
- [ ] 关闭后游戏恢复无卡顿

**Layout & Rendering**
- [ ] 1280×720 / 1920×1080 / 2560×1440 正确显示
- [ ] 面板居中且不超出安全区域
- [ ] 模糊背景正确呈现当前游戏场景

**Input**
- [ ] Esc / B / Start 均可打开和关闭暂停菜单
- [ ] 键盘、手柄、鼠标均可完成所有操作
- [ ] 焦点始终可见
- [ ] 战斗态 / 演出态下暂停键被正确屏蔽（无响应 + 无报错）

**Events & Data**
- [ ] 打开时游戏正确暂停（所有物理/AI/Timer 停止）
- [ ] 关闭时游戏正确恢复
- [ ] "返回主菜单"正确清除游戏状态并加载主菜单

**Accessibility**
- [ ] 所有文本对比度达标
- [ ] Reduce Motion 下无模糊动画
- [ ] Screen Reader 正确播报

---

## 13. Open Questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| Q1 | 战斗中是否允许暂停？（当前设计：不允许） | GDD Lead | 待确认 — 同 hud.md Q5 |
| Q2 | 暂停时是否显示"提示/攻略"（如加载画面提示）？ | UX Lead | v1.0 不做 |
| Q3 | 多人/在线功能是否影响暂停逻辑？ | — | 不适用（单人游戏） |

---

## 14. Audit History

| 日期 | 版本 | 审计人 | 变更 |
|------|------|--------|------|
| 2026-06-09 | 1.0 | AI (ux-design) | 全文创建 |
