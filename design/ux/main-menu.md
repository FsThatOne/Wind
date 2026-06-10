---
title: UX Specification — 主菜单 (Main Menu)
project: 风止 (Wind Stops)
status: Draft
version: 1.0
author: UX Lead
created: 2026-06-09
last_updated: 2026-06-09
platform_targets: PC (Steam), Steam Deck
accessibility_tier: Standard (WCAG 2.1 AA)
screen_name: MainMenuScreen
related:
  - design/ux/interaction-patterns.md
  - design/ux/hud.md
  - design/accessibility-requirements.md
  - docs/architecture/adr-0013-cutscene-system.md
---

# UX Specification: 主菜单 (Main Menu)

> **Scope**: 游戏启动后玩家看到的第一个交互画面。提供开始游戏、继续、设置、退出等入口。
> 本画面是玩家对《风止》的第一印象，需传达"水墨江湖"的世界基调。

---

## 1. Purpose & Player Need

**What player need does this screen serve?**

让玩家在进入江湖之前获得片刻宁静——确认自己的存档安全、选择如何开始旅程，并在尚未操作任何角色前就感受到这个世界的气韵。主菜单不是"功能列表"，而是游戏叙事的前奏。

**The player goal**:

在 3 次按键内进入游戏世界（继续存档或开始新游戏），无需阅读复杂说明。

**The game goal**:

引导玩家进入正确的游戏流程（新游戏 vs 续玩），展示游戏视觉基调，并提供系统设置入口。

---

## 2. Player Context on Arrival

| Question | Answer |
|----------|--------|
| What was the player just doing? | 启动游戏 / 从桌面或 Steam 进入 |
| What is their emotional state? | 期待但放松——准备沉浸；或急切——想快速续玩 |
| What cognitive load are they carrying? | 极低——无游戏内信息需要追踪 |
| What information do they already have? | 可能知道自己有存档（老玩家）或一无所知（新玩家） |
| What are they most likely trying to do? | 继续上次的游戏（最高频行为）或开始新游戏 |
| What are they likely afraid of? | 存档丢失；不知道上次玩到哪里了 |

**Emotional design target for this screen**:

静谧而有期待感——如翻开一本水墨画册的扉页，尚未读到故事，但已感受到世界在等待。

---

## 3. Navigation Position

**Screen hierarchy**:

```
MainMenuScreen (ROOT)
  ├── NewGameFlow (子流程：难度选择 → 开场)
  ├── LoadGameScreen (存档列表)
  ├── SettingsScreen (设置)
  └── CreditsScreen (制作人员)
```

**Modal behavior**: Non-modal（根画面，无父画面）

**Reachability — all entry points**:

| Entry Point | Triggered By | Notes |
|-------------|-------------|-------|
| 游戏启动 | 应用程序启动完成 | 主入口；Logo/Splash 后自动到达 |
| 返回主菜单 | 暂停菜单 → "返回主菜单" 确认 | 从游戏内退出时回到此处 |

---

## 4. Entry & Exit Points

**Entry table**:

| Trigger | Source | Transition Type | Data Passed In | Notes |
|---------|--------|-----------------|----------------|-------|
| 启动完成 | Splash / Logo 序列 | Fade in (Slow 400ms) | 存档元数据 (是否有存档) | 首次启动无存档 → "继续" 按钮隐藏 |
| 从游戏内返回 | PauseMenu → 确认退出 | Fade in (Slow 400ms) | 无 | 所有游戏状态已保存 |

**Exit table**:

| Exit Action | Destination | Transition Type | Data Returned | Notes |
|-------------|------------|-----------------|---------------|-------|
| 选择"继续" | 游戏世界 (最近存档) | Fade out → Loading | save_slot_id | 直接加载最近存档 |
| 选择"新游戏" | NewGameFlow (难度选择) | Screen Push (slide left) | — | 子流程 |
| 选择"载入" | LoadGameScreen | Screen Push (slide left) | — | 存档列表 |
| 选择"设置" | SettingsScreen | Screen Push (slide left) | — | — |
| 选择"制作人员" | CreditsScreen | Screen Push (slide left) | — | — |
| 选择"退出" | 应用程序关闭 | Fade to black → quit | — | 需 ConfirmationDialog |

---

## 5. Layout Specification

### 5.1 Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                    {动态水墨背景}                             │
│                    (CanvasLayer 0)                           │
│                                                             │
│                                                             │
│                                                             │
│           ┌─────────────────────┐                           │
│           │                     │                           │
│           │    风    止         │  ← 游戏标题 (水墨字)       │
│           │    Wind Stops       │     英文副标题 (小字)       │
│           │                     │                           │
│           └─────────────────────┘                           │
│                                                             │
│                                                             │
│              ● [ 继续 ]            ← 默认焦点 (有存档时)     │
│                [ 新游戏 ]                                    │
│                [ 载入存档 ]                                   │
│                [ 设置 ]                                      │
│                [ 制作人员 ]                                   │
│                [ 退出游戏 ]                                   │
│                                                             │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ v1.0.0            "风止于此，心动于彼"            © 2026│  │ ← 底栏
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Zone Definitions

| Zone Name | Description | Approximate Size | Scrollable? | Overflow |
|-----------|-------------|-----------------|-------------|----------|
| Background | 全屏动态水墨背景（粒子/Shader） | 100% viewport | No | Clip |
| Title | 游戏标题 + 英文副标题 | 居中，宽 40%，高 ~15% | No | — |
| Menu List | 纵向按钮列表 | 居中偏左，宽 240dp，高自适应 | No | 最多 6 项无需滚动 |
| Footer | 版本号 + 引言 + 版权 | 底部全宽，高 48dp | No | 截断 |

### 5.3 Component Inventory

| Component | Type | Zone | Purpose | Reuses? |
|-----------|------|------|---------|---------|
| 水墨背景 | Shader / ParticleSystem | Background | 营造氛围 | No — 专属 |
| 标题 Label | RichTextLabel | Title | 游戏名 | No — 专属字体/效果 |
| 菜单按钮 | Button (interaction-patterns §2.1) | Menu List | 导航入口 | Yes — 标准 Button |
| 版本号 Label | Label | Footer | 显示构建版本 | Yes — 通用 |
| 引言 Label | Label | Footer | 氛围文字 | No |

**Primary focus element on open**: 有存档时 → "继续"；无存档时 → "新游戏"

---

## 6. States & Variants

| State | Trigger | Visual Changes | Behavioral Changes | Notes |
|-------|---------|---------------|-------------------|-------|
| 首次启动 (无存档) | 无存档文件检测到 | "继续" 和 "载入存档" 隐藏 | 焦点落在 "新游戏" | 按钮列表更短 |
| 有存档 (常态) | 检测到 ≥1 存档 | 全部按钮显示 | 焦点落在 "继续" | "继续" 下方微小字显示存档时间 |
| 退出确认 | 选择 "退出游戏" | ConfirmationDialog 弹出 (Modal) | 背景交互锁定 | 默认焦点在"取消" |
| 从游戏内返回 | PauseMenu 退出 | 同"有存档"状态 | 背景可能切换为当前章节主题 | — |

---

## 7. Interaction Map

### 7.1 Navigation Inputs

| Input | Platform | Action | Visual | Audio | Notes |
|-------|----------|--------|--------|-------|-------|
| ↑↓ / D-Pad | All | 菜单项间移动焦点 | Focus 框移动 | ui_navigate | 循环：末尾↓回到首项 |
| Mouse Hover | PC | 高亮悬停项 | Hover 态 | — | Hover 不移动 Focus |
| Mouse Click | PC | 选中并激活 | Press → Activate | ui_confirm | — |

### 7.2 Action Inputs

| Input | Platform | Context | Action | Audio | Notes |
|-------|----------|---------|--------|-------|-------|
| Enter / A | All | 任意菜单项 focused | 激活该菜单项 | ui_confirm | — |
| Esc / B | All | 主菜单画面 | 无操作（根画面，无可返回） | ui_deny (轻) | 不退出游戏 |
| Esc / B | All | ConfirmationDialog 中 | 取消退出 | ui_back | — |

### 7.3 State-Specific Behaviors

| State | Input Restriction | Reason |
|-------|------------------|--------|
| 退出确认 Dialog | 仅 Confirm/Cancel 可用 | Modal Focus Trap |

---

## 8. Data Requirements

| Data Element | Source System | Update Frequency | Format | Null Handling |
|--------------|-------------|-----------------|--------|---------------|
| 存档是否存在 | SaveManager | On screen open | bool | false → 隐藏"继续"和"载入" |
| 最近存档时间 | SaveManager | On screen open | DateTime string | — |
| 最近存档章节名 | SaveManager | On screen open | string | 不显示章节提示 |
| 游戏版本号 | ProjectSettings | Static | "v{major}.{minor}.{patch}" | — |

---

## 9. Events Fired

| Player Action | Event | Payload | Receiver | Notes |
|---------------|-------|---------|----------|-------|
| 选择"继续" | ContinueGameRequested | {save_slot_id} | SaveManager → SceneLoader | 加载最近存档 |
| 选择"新游戏" | NewGameFlowStarted | {} | ScreenManager | Push NewGameFlow |
| 选择"退出" + 确认 | QuitGameRequested | {} | Application | 安全退出 |
| 画面打开 | MainMenuOpened | {has_save: bool} | Analytics | 追踪新/老玩家比例 |

---

## 10. Transition & Animation

| Transition | Trigger | Type | Duration | Easing | Reduce Motion |
|------------|---------|------|----------|--------|---------------|
| 画面入场 | Splash 结束 / 从游戏返回 | 整体 Fade in | Slow 400ms | ease-out | Fast 100ms fade |
| 菜单项入场 | 画面入场完成后 | 逐项从下方滑入 (stagger 50ms) | Normal 200ms each | ease-out | 直接出现 (Instant) |
| 选中后退出 | 玩家选择菜单项 | 整体 Fade out | Normal 200ms | ease-in | Fast 100ms fade |
| 焦点移动 | 方向键/Hover | Focus 框位移 | Fast 100ms | ease-out | Instant |
| 水墨背景 | 常驻 | 缓慢粒子漂浮 | Continuous (loop) | — | 静态画面（粒子停止） |
| 退出确认弹出 | 选择"退出" | Dialog scale 0.95→1.0 + 遮罩淡入 | Normal 200ms | ease-out | Instant 出现 |

---

## 11. Accessibility Requirements

**Text contrast**:

| Element | Background | Required | Notes |
|---------|-----------|----------|-------|
| 菜单按钮文字 | 半透明暗底 (~70% black) | ≥4.5:1 | 水墨背景上叠暗底保证对比 |
| 标题文字 | 动态背景 | ≥3:1 (大文本) | 标题为大字，WCAG AA 大文本标准 |
| 底栏文字 | 暗色底栏 | ≥4.5:1 | — |

**Focus management**:
- 所有菜单按钮 `focus_mode = ALL`
- 入场后自动 `grab_focus()` 到首个可用项
- 循环导航（最后一项↓回到第一项）
- 无 Focus Trap（非 Modal 画面）

**Screen Reader**:

| State Change | Announcement |
|--------------|-------------|
| 画面打开 | "主菜单。{N} 个选项。" |
| 焦点移动 | 按钮文字（"继续"、"新游戏"等） |
| "继续" focused | "继续 — 上次游玩: {date}, {chapter}" |

**Reduce Motion**: 参见 §10 各行 Reduce Motion 列。背景动画完全停止为静态水墨画。

---

## 12. Acceptance Criteria

**Performance**
- [ ] 画面从 Splash 结束到首帧可见 ≤300ms
- [ ] 画面完全可交互 ≤500ms
- [ ] Steam Deck 720p 下稳定 60 FPS（主菜单不应有性能问题）

**Layout & Rendering**
- [ ] 1280×720 / 1920×1080 / 2560×1440 / 3840×2160 均正确显示
- [ ] 16:9 / 16:10 / 21:9 宽高比无裁切无溢出
- [ ] 无存档状态下仅显示 "新游戏 / 设置 / 制作人员 / 退出"
- [ ] 有存档状态下显示全部 6 项

**Input**
- [ ] 键盘 (↑↓ + Enter + Esc) 可完成所有操作
- [ ] 手柄 (D-Pad + A + B) 可完成所有操作
- [ ] 鼠标 (Hover + Click) 可完成所有操作
- [ ] 焦点始终可见

**Accessibility**
- [ ] 所有文本对比度 ≥4.5:1 (标题 ≥3:1)
- [ ] Reduce Motion 下无动画（背景静止 + 菜单直接出现）
- [ ] Screen Reader 正确播报画面名和按钮文字

---

## 13. Open Questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| Q1 | 主菜单背景是否根据游戏进度变化（如解锁新章节后切换场景）？ | Art Director | 待 Art Bible |
| Q2 | 是否需要"DLC"或"商店"入口？ | Producer | v1.0 不需要 |
| Q3 | "继续"是否显示存档预览截图？ | UX Lead | 待技术验证（截图系统） |

---

## 14. Audit History

| 日期 | 版本 | 审计人 | 变更 |
|------|------|--------|------|
| 2026-06-09 | 1.0 | AI (ux-design) | 全文创建 |
