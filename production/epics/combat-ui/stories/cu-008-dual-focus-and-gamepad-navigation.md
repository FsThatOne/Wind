# Story: cu-008 — 双焦点与手柄导航

> **Epic**: combat-ui
> **类型**: UI
> **优先级**: P0 — Steam Deck / 手柄可玩性
> **Estimate**: M（约 6h）
> **依赖**: cu-004, cu-005
> **阻塞**: 无
> **ADR 指引**: ADR-0002（Godot 4.6 dual-focus / FocusManager / InputModeDetector）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 手柄键盘完整导航, §Edge Cases
> **TR-ID**: TR-combat-ui-008
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-30
> **Hardware Verification**: Descoped to Polish — 实机手柄走查待硬件购入后执行

## 目标

确保战斗招式面板和关键提示可用键鼠与手柄完整操作，验证 Godot 4.6 dual-focus 下鼠标 hover 与手柄焦点互不干扰。

## 范围

### 包含
- D-pad 上下导航招式面板
- A/确认键选定当前聚焦行动
- 反制和决胜一击作为面板内独立行可被导航触达
- 面板打开时默认聚焦第一个可用招式
- `focus_neighbor_*` 在面板内循环，焦点不逃出面板
- 鼠标 hover 和手柄焦点可同时独立存在
- 输入模式切换时视觉反馈正确
- 所有可交互 Control 设置 `focus_mode = FOCUS_ALL`

### 不包含
- 招式面板信息内容 → cu-004
- 反制/决胜提示规则 → cu-005
- 全局输入重映射

## 技术说明

- 必须通过 `FocusManager.PushFocus`/`PopFocus` 管理面板打开与关闭
- 手柄输入由 `InputModeDetector` 识别，不允许每个按钮各自散写模式切换逻辑
- 默认焦点使用 `grab_focus()`，但需验证不影响鼠标 hover 高亮
- 所有导航邻居必须留在面板内部循环，避免焦点落到 HUD 其他区域
- Godot 4.6 dual-focus 是 HIGH risk，必须产出手动验证证据
- 禁止 hover-only 交互；鼠标能做的选择，手柄也必须能完成

## Control Manifest Rules

- 所有 UI 必须基于 Godot Control 节点树 + 薄抽象层，战斗面板必须继承或组合 `BaseUiPanel`（ADR-0002）。
- 所有可交互行动 Control 必须使用 `FocusModeEnum.All`；非交互提示、装饰和池化节点必须使用 `FocusModeEnum.None` 且回收时释放焦点。
- 面板打开/关闭必须通过 `IFocusManager.PushFocus` / `PopFocus` 管理焦点栈，不得在各按钮中分散实现焦点恢复。
- UI 刷新必须使用 dirty 标记集中刷新；输入事件只更新焦点/模式状态，不得每帧无条件重建面板。
- 禁止 hover-only 交互；鼠标可执行的行动，键盘和手柄必须同样可完成。
- `focus_neighbor_*` 或等效导航图必须限制在招式面板内部循环，禁止焦点逃逸到 HUD、暂停按钮或场景外 Control。

## Performance Notes

- 继承 Combat HUD 刷新预算：焦点/输入模式反馈刷新目标 `<= 1ms/frame`。
- D-pad/摇杆导航必须基于当前面板行动列表做 O(1) 或 O(n<=10) 的稳定索引切换，不得扫描场景树或依赖文件 IO。
- 面板空闲期间不得产生 per-frame allocation；仅在输入模式、hover 目标、手柄焦点或行动列表变化时刷新受影响视觉状态。
- 焦点循环和确认提交不得触发 Combat 结算查询；只提交当前聚焦行动意图。
- 反制与决胜行动行复用现有行动列表顺序，不得引入额外运行时 Control 重建循环。

## Engine Notes

- Godot 4.7-stable dual-focus 属于 post-cutoff 高风险 API 行为，必须通过手动 evidence 验证并记录。
- `grab_focus()` 必须受 FocusManager 生命周期保护，且不得清除当前鼠标 hover 高亮。
- 鼠标 hover 与手柄焦点需要维护独立视觉状态；输入模式切换只改变样式优先级，不得丢失 selected/focused action。
- `focus_neighbor_top/bottom` 或等效 API 的循环设置必须在 Godot 4.7-stable 下验证，避免面板动态刷新后邻居引用失效。
- 自动测试可覆盖导航图和状态契约；真实手柄、Steam Deck 或 Godot scene 中的 dual-focus 表现必须由 `production/qa/evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md` 留证。

## 验收标准

- [x] 招式面板打开时默认聚焦第一个可用招式
- [x] D-pad 可上下导航所有可交互行动
- [x] 从最后一项继续向下会循环到第一项，向上同理
- [x] 焦点不会逃出招式面板
- [x] A/确认键可提交当前聚焦行动
- [x] 反制和决胜一击行可通过手柄触达
- [x] 鼠标 hover 与手柄焦点可同时存在且视觉不冲突
- [x] 切换输入模式后焦点高亮显示规则正确

## QA 手动检查

- **AC-1**：纯手柄操作
  - Setup：拔掉或不使用鼠标，进入玩家行动阶段
  - Verify：D-pad 和确认键可完成选招、反制、决胜选择
  - Pass condition：无需鼠标即可完成整轮玩家决策

- **AC-2**：循环焦点
  - Setup：打开招式面板并持续向下/向上按方向键
  - Verify：焦点在面板内循环
  - Pass condition：焦点不会跳到 HUD、暂停按钮或场景外

- **AC-3**：dual-focus 验证
  - Setup：鼠标悬停一个招式，同时用手柄聚焦另一个招式
  - Verify：hover 与手柄焦点各自显示，不互相清掉
  - Pass condition：玩家能明确分辨鼠标位置和手柄当前确认目标

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs`

Required automated coverage:
- Move panel default focus lands on the first available action.
- D-pad up/down reaches every interactive action.
- Navigation wraps from last to first and first to last.
- Focus cannot escape the move panel to HUD or unrelated controls.
- Confirm/A submits the currently focused action.
- Counter and Decisive Strike rows are reachable with gamepad navigation.
- Mouse hover and gamepad focus can coexist and expose separate visual states.
- Input mode changes update focus styling without losing selected/focused action.

Manual evidence required:
- Capture `production/qa/evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md`.
- Include a clip showing D-pad cycle, confirm submit, focus containment, and simultaneous mouse hover/gamepad focus.

**Sprint 6 升级 (cu-visual-evidence Must Have)**：
- 把 evidence MD 升级为 **Visual Captured** 状态。
- ≥1 段 D-pad 循环录屏（无硬件时键盘 fallback 录屏，硬件 walkthrough 走 cu-008-Gamepad-HW-Verify Nice to Have）。
- ≥1 张鼠标 hover + 手柄/键盘焦点共存截图。
- Sign-off：designer + qa-lead。
- tech-debt-register 中 cu-008 Visual evidence deferred 条目改为 `Resolved`（手柄硬件部分仍为 deferred until cu-008-Gamepad-HW-Verify）。
- Reference QA plan: `production/qa/qa-plan-sprint-6-2026-06-18.md`.

## 测试证据路径

`production/qa/evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md`

## 依赖关系

- Depends on: cu-004, cu-005
- Unlocks: None

## Completion Notes

**Completed**: 2026-06-15
**Verdict**: COMPLETE WITH NOTES
**Criteria**: 8/8 covered by automated contract tests; keyboard fallback manually verified in Godot vertical slice.
**Test Evidence**:
- `tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs`
- `production/qa/evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md`
- `production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4`
**Verification**:
- `CombatUiMoveSelectionPanelTest|CombatUiCounterDecisivePromptTest|CombatUiDualFocusNavigationTest` passed 45/45.
- `dotnet build prototypes/fengzhi-vertical-slice/FengzhiSlice.csproj` passed with 0 warnings / 0 errors.
- `BUG-0001` reproduced the original Godot scene failure and was closed after successful keyboard fallback rerun.
**Deviations / Advisory Notes**:
- No physical controller was available during closure. Real hand controller D-pad / A-key submission and gamepad-hover dual-focus recording remain tracked as tech debt.
- Code review gate was recorded as pending for sprint close-out per user decision; previous `/code-review` covered the Foundation implementation before the vertical-slice adapter fix.
**Code Review**: Pending — rerun before Sprint 5 close-out for `CombatUiMoveSelection.cs`, `BossBattleUI.cs`, and `combat_ui_dual_focus_navigation_test.cs`.
