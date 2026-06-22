# cu-008 双焦点与手柄导航 — QA Evidence

> 日期：2026-06-15  
> Story：`production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md`  
> TR-ID：`TR-combat-ui-008`  
> 类型：UI  
> 证据状态：自动契约已覆盖；新 harness 手测通过；`BUG-0001` 已验证修复
> 录制负责人：User  
> QA 签字：TBD  
> UX 签字：TBD

## 验证目标

- 验证招式面板在无鼠标条件下可用 D-pad/摇杆完整导航并提交行动。
- 验证焦点循环限制在招式面板内部，不逃逸到 HUD、暂停按钮或场景外 Control。
- 验证反制与决胜一击行可通过手柄导航触达。
- 验证 Godot 4.7-stable dual-focus 下鼠标 hover 与手柄 focus 可同时存在且视觉不冲突。
- 验证输入模式切换后，焦点高亮规则正确且不丢失当前聚焦行动。

## 自动测试覆盖

- `CombatUiDualFocusNavigationTest`
  - `MovePanelNavigation_DefaultFocusLandsOnFirstAvailableAction`
  - `DpadNavigation_ReachesEveryInteractiveAction`
  - `DpadNavigation_WrapsFromLastToFirstAndFirstToLast`
  - `NavigationGraph_ContainsFocusInsideMovePanelOnly`
  - `ConfirmButton_SubmitsCurrentlyFocusedAction`
  - `CounterAndDecisiveRows_AreReachableWithGamepadNavigation`
  - `MouseHoverAndGamepadFocus_CanCoexistAsSeparateVisualStates`
  - `InputModeChanges_UpdateFocusStylingWithoutLosingFocusOrHover`
  - `MoveSelectionPanel_BindsNavigationSnapshotAndExposesRequiredControlContracts`

## 录制环境

| 项目 | 记录 |
|------|------|
| Engine | Godot 4.7-stable |
| Platform | macOS local dev build |
| Build | Local dev build：TBD |
| Controller | None in this recording |
| Input Device Mix | 键盘 + 鼠标 |
| Scene / Test Battle | Combat move selection panel during player decision phase |
| Recording Tool | macOS screen recording |
| Video Path | `production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4` |

## 前置设置

- 进入玩家行动阶段，并确保招式选择面板打开。
- 测试角色至少拥有 3 个可用招式、`调息`、`普通攻击`、`使用道具`。
- 准备一个反制可用场景：目标公开意图被当前招式克制，玩家内息 `>= 3`。
- 准备一个决胜一击场景：当前目标破绽 `>= 5`。
- 鼠标保持可用，用于 dual-focus 悬停验证。
- 手柄保持连接，用于 D-pad/摇杆导航与 A/确认键提交。

## 录制脚本

### Segment A — D-pad 循环与焦点不逃逸

1. 打开招式选择面板。
2. 录制面板初始状态，确认默认焦点在第一个可用行动。
3. 连续按 D-pad Down，依次经过所有可交互行动。
4. 继续按 D-pad Down，从最后一项循环回第一项。
5. 连续按 D-pad Up，从第一项循环到最后一项。
6. 观察焦点是否始终停留在招式面板内部。

Pass 条件：
- 初始焦点位于第一个可用行动。
- D-pad 可到达所有可交互行动，包括 `反制` 和 `▶ 决胜一击`。
- 首尾循环稳定。
- 焦点不会跳到 HUD、暂停按钮、场景外 Control 或不可交互装饰节点。

记录：
- 录制片段：`production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4`
- 结果：PASS（Keyboard fallback）
- 备注：复录确认进入招式面板后可直接使用键盘方向键切换；首尾循环正确。原失败录屏保留为 `production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-fail.mp4`，修复记录见 `BUG-0001`。

### Segment B — A/确认键提交当前焦点

1. 使用 D-pad 将焦点移动到一个普通招式。
2. 按 A/确认键提交当前聚焦行动。
3. 返回玩家行动阶段或重置测试战斗。
4. 使用 D-pad 将焦点移动到反制可用招式。
5. 按 A/确认键提交，确认反制意图被提交。
6. 返回玩家行动阶段或重置测试战斗。
7. 使用 D-pad 将焦点移动到 `▶ 决胜一击` 行。
8. 按 A/确认键提交，确认决胜行动意图被提交。

Pass 条件：
- A/确认键提交的是当前手柄焦点行动，而不是鼠标 hover 行动。
- 普通招式、反制招式、决胜一击都可通过手柄完成选择。
- UI 只提交行动意图，不在 UI 层执行扣费、伤害或破绽结算。

记录：
- 录制片段：TBD
- 结果：TBD
- 备注：TBD

### Segment C — 鼠标 hover 与手柄 focus 并存

1. 打开招式选择面板。
2. 用鼠标 hover 到一个招式，例如第一格。
3. 不移动鼠标，用 D-pad 将手柄焦点移动到另一个行动，例如第三格或 `▶ 决胜一击`。
4. 观察鼠标 hover 高亮与手柄 focus 高亮是否同时可见。
5. 按 A/确认键提交当前手柄焦点行动。
6. 验证提交目标是手柄焦点行动，而不是鼠标 hover 行动。

Pass 条件：
- 鼠标 hover 与手柄 focus 可同时存在。
- 两种视觉状态清晰可区分，不互相清除。
- `grab_focus()` 或手柄导航不会清掉鼠标 hover 高亮。
- A/确认键提交手柄 focus 行动。

记录：
- 录制片段：TBD
- 结果：TBD
- 备注：TBD

### Segment D — 输入模式切换

1. 使用手柄移动焦点，确认显示 gamepad focus 样式。
2. 移动鼠标到另一项，确认 hover 样式出现。
3. 再次使用 D-pad 移动焦点，确认 gamepad focus 样式恢复或保持优先级正确。
4. 使用键盘方向键或确认键，确认 keyboard focus 样式正确。

Pass 条件：
- 输入模式切换不丢失当前聚焦行动。
- 鼠标 hover 状态在切回手柄/键盘时不会被错误清除。
- 不出现两个相同优先级的焦点框导致玩家无法判断当前确认目标。

记录：
- 录制片段：TBD
- 结果：TBD
- 备注：TBD

## 手动检查清单

- [x] 面板打开时默认聚焦第一个可用行动。（Keyboard fallback verified）
- [x] D-pad Down 可到达全部可交互行动。（Keyboard Down fallback verified）
- [x] D-pad Up 可到达全部可交互行动。（Keyboard Up fallback verified）
- [x] 从最后一项继续向下循环到第一项。（Keyboard Down fallback verified）
- [x] 从第一项继续向上循环到最后一项。（Keyboard Up fallback verified）
- [x] 焦点不会逃出招式面板。（Keyboard fallback verified）
- [ ] A/确认键提交当前手柄焦点行动。
- [ ] 反制行可通过手柄导航触达。
- [ ] `▶ 决胜一击` 行可通过手柄导航触达。
- [ ] 鼠标 hover 与手柄 focus 可同时存在。
- [ ] hover 与 focus 视觉状态不冲突。
- [ ] 输入模式切换后焦点高亮规则正确。
- [ ] 禁用项不会困住焦点。
- [ ] 非交互提示、装饰和预览卡不会抢焦点。

## 证据文件

- 失败录屏：`production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-fail.mp4`
- 键盘复录通过：`production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4`
- 主手柄录屏：`production/qa/evidence/media/cu-008-dual-focus-navigation.mp4`（TBD）
- D-pad 循环截图：TBD
- hover/focus 并存截图：TBD
- 确认提交日志或截图：TBD

## 风险与观察

- Godot 4.7-stable dual-focus 是 ADR-0002 标记的 HIGH risk；自动测试只能覆盖导航状态契约，不能替代真实场景验证。
- 当前 `CombatUiNavigationController` 为纯 C# 契约层，真实 `grab_focus()` 与 hover 视觉并存必须通过 Godot 场景录制确认。
- 若发现 hover 被手柄焦点清除，需回到 `FocusManager` / `InputModeDetector` 层修复，而不是在单个按钮内散写输入模式逻辑。
- 2026-06-15 键盘实机录制发现：初始焦点未能落到招式面板的第一个可用行动，必须先鼠标点击第一招后才可继续键盘导航；方向键 Down 到最后一项后未循环回第一项。此问题已归档为 `BUG-0001`。
- 2026-06-15 键盘实机复录确认：`BUG-0001` 已修复，键盘方向键可直接导航并完成首尾循环。该证据覆盖 keyboard fallback；真实手柄 D-pad / A 键仍建议后续补录。

## 结论

2026-06-17 更新：用户在 `prototypes/sprint5-combat-ui-harness` 中手测 `cu-008`，确认 focus / hover 并存、禁用项导航行为等目标按预期工作。

当前结论：PASS VIA HARNESS — 自动契约通过，Godot 键盘实机复录通过，新 harness 手测通过，`BUG-0001` 已验证修复。真实物理手柄录屏仍可作为后续补充证据，但不再阻塞 Sprint 5 Must Have close-out。

签字：
- QA：TBD
- UX：TBD
- 日期：TBD

---

## Visual Captured (Sprint 7 carryover — Pending)

> **2026-06-22 改派**：原计划 Sprint 6 在 `prototypes/sprint5-combat-ui-harness` 上录屏。harness 已于 commit `c8d8c14` 主动删除（与 burst-read spike / 134 个旧 protagonist 资产同清理）。本段 visual evidence 推迟到 Sprint 7 的 ADR-0020 全循环 VS 重建上录制，确保 evidence 代表当前架构（纯 2D 武侠 + 行气战棋）。原 Foundation Captured 段保持只读。

> 本段为 Sprint 6 `cu-visual-evidence` story 的模板占位；待 designer 录屏 / 截图后填入并切换为 Visual Captured。Sprint 5 keyboard fallback 录屏 `cu-008-keyboard-dual-focus-navigation-rerun.mp4` 保留为既有覆盖；本段补主手柄 D-pad 录屏 + dual-focus 截图。真实手柄硬件 walkthrough 不在本 story 范围（走 cu-008-Gamepad-HW-Verify）。

### Metadata

| 项 | 值 |
|---|---|
| Capture date | TBD |
| Captured by | TBD（designer 名 / 工号） |
| Engine | Godot 4.7-stable Mono |
| Build | TBD（local dev / commit hash） |
| Platform | TBD（macOS / Windows / Linux） |
| Recording tool | TBD（macOS Screen Recording / OBS / etc.） |
| Harness | `prototypes/sprint5-combat-ui-harness` |
| Fixture used | Combat move selection panel during player decision phase（含反制可用 + 决胜行可见场景） |
| Input device | TBD（D-pad / Xbox / DualSense；无硬件时复用 keyboard fallback） |

### Required Shots（per qa-plan-sprint-6 §cu-visual-evidence — hover+focus 1 张 + D-pad 循环录屏）

| # | 类型 | 内容 | 文件 | 状态 |
|---|---|---|---|---|
| 1 | 截图 | 鼠标 hover + 手柄 focus 共存 — hover 高亮在 A 招式格、gamepad focus 高亮在 C 招式格，两种视觉状态同时可见且清晰可区分 | `production/qa/evidence/media/cu-008-hover-focus-coexist.png` | TBD |
| 2 | 录屏 | D-pad 循环 — 从默认聚焦第一行动开始，连续 D-pad Down 经过所有可交互行动（含反制 + ▶ 决胜一击）、最后一项循环回第一项；再连续 D-pad Up 反向循环 | `production/qa/evidence/media/cu-008-dual-focus-navigation.mp4` | TBD |
| 3 | 录屏（fallback / 已存） | 键盘方向键 fallback — `BUG-0001` 修复复录，覆盖 keyboard 首尾循环 | [`production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4`](./media/cu-008-keyboard-dual-focus-navigation-rerun.mp4) | PASS（Sprint 5 已录） |

### Capture Checklist

- [ ] ≥1 张 hover+focus 共存截图（必备 #1）
- [ ] ≥1 段 D-pad 循环录屏（必备 #2，无硬件时显式标注 keyboard fallback 复用 #3，并在签字段注明）
- [ ] 所有素材挂在 `production/qa/evidence/media/` 下并被本文件相对引用
- [ ] 文件名使用 kebab-case；视频 ≥ 1280×720 30fps；截图 ≥ 1280×720
- [ ] 验证点：鼠标 hover 与手柄 focus 可同时存在，两种视觉状态清晰可区分，互不清除（覆盖 cu-008 GDD dual-focus AC + Sprint 5 ADR-0002 HIGH risk）
- [ ] 验证点：A/确认键提交的是当前手柄焦点行动，而不是鼠标 hover 行动
- [ ] 验证点：焦点不会逃出招式面板（HUD / 暂停按钮 / 装饰节点不抢焦点）
- [ ] 验证点：D-pad 首尾循环稳定（最后一项 Down → 第一项；第一项 Up → 最后一项）
- [ ] 验证点：反制行 + ▶ 决胜一击行均可通过 D-pad 触达
- [ ] header `证据状态` 字段升级为 `Visual Captured`
- [ ] story 文件 `production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md` 内 `Foundation Captured` / `Visual evidence not yet captured` 升级为 `Visual Captured`
- [ ] tech-debt-register 中 cu-008 Visual evidence deferred 条目改为 `Resolved`
- [ ] 真实手柄硬件 walkthrough（D-pad / A 键 / B 键 / Steam Deck）属于 cu-008-Gamepad-HW-Verify story 范围，本 story **不**阻塞硬件录屏

### Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| Designer | TBD | TBD | __pending__ |
| QA Lead | TBD | TBD | __pending__ |
| UX | TBD | TBD | __pending__ |
