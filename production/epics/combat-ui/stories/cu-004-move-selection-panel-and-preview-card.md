# Story: cu-004 — 招式选择面板与预览卡

> **Epic**: combat-ui
> **类型**: UI
> **优先级**: P0 — 玩家主要决策界面
> **Estimate**: L（约 8h）
> **依赖**: cu-001, cu-002
> **阻塞**: cu-005, cu-008
> **ADR 指引**: ADR-0002（Control / FocusManager / dual-focus），ADR-0011（预览反馈动效）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 招式选择面板, §Interactions with Other Systems, §Edge Cases
> **TR-ID**: TR-combat-ui-004
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-15

## 目标

实现玩家行动时的招式选择面板，展示 6 个装备招式、当前 GDD 允许的基础行动、消耗/条件/效果摘要和预览卡，让玩家基于体系关系做判断，但不看到胜率、期望值或最终结算输出。

## 范围

### 包含
- 玩家行动时弹出招式面板
- 6 个装备招式格：招式名、体系图标、内息消耗、触发条件图标、特殊效果摘要
- 心法专属招式显示“心法”角标
- 调息、使用道具两类基础行动；普通攻击不属于当前 `combat_action_types`
- 内息不足置灰并显示“差 X 内息”
- 心法封印时心法专属招式置灰并显示“心法封印中”
- 持有 0 个可用战斗道具时“使用道具”置灰
- 悬停/聚焦招式时显示预览卡
- 预览卡显示克制、被克、中性关系、预计伤害区间、预计破绽变化、合法范围和提示，但不显示胜率、期望值或最终结算输出
- 面板打开时默认聚焦第一个可用招式

### 不包含
- 反制标签最终确认逻辑 → cu-005
- 决胜一击特殊行 → cu-005
- 手柄循环导航完整验证 → cu-008
- 战斗背包详情界面

## 技术说明

- 招式数据通过 MartialArts 查询契约读取 `GetEquippedMoves()` 与 `GetMoveDetails(id)`
- UI 只显示招式详情、条件摘要、效果摘要和 GDD 允许的预览信息，不执行实际结算
- 克制关系可显示“关系”、预计伤害区间、预计破绽变化和合法范围，但不得显示胜率、期望值或最终结算输出
- 所有可交互 Control 必须 `focus_mode = FOCUS_ALL`
- 面板打开时通过 `FocusManager.PushFocus` 与 `grab_focus()` 聚焦首个可用项
- 面板关闭时通过 `FocusManager.PopFocus` 恢复焦点
- 招式面板刷新使用脏标记，战斗事件后台刷新不得关闭玩家正在操作的面板

## Control Manifest Rules

- 所有 UI 必须基于 Godot Control 节点树 + 薄抽象层，面板继承 `BaseUiPanel`（ADR-0002）。
- 所有可交互 Control 必须 `focus_mode = FOCUS_ALL`，非交互或池化节点不得抢焦点。
- 面板打开/关闭必须通过 `FocusManager.PushFocus` / `FocusManager.PopFocus` 管理焦点栈。
- UI 刷新必须使用 `_dirty + _Process` 脏标记模式，禁止事件回调中每帧无条件重建整面板。
- 禁止 hover-only 交互；键鼠与手柄都必须能完成招式选择。
- Godot 4.6 dual-focus 行为必须留证据：`grab_focus()` 不得清除鼠标 hover 高亮。

## Performance Notes

- 招式面板刷新预算继承 Combat HUD 规则：刷新目标 `<= 1ms/frame`。
- 资源事件只标记 dirty，不在事件回调中直接重建所有 Control。
- 面板空闲显示期间不得产生 per-frame allocation；仅在装备列表、资源状态或目标关系变化时刷新受影响项。
- 预览卡只读取 MartialArts / Combat 快照，不输出胜率、期望值或最终结算结果。

## 验收标准

- [ ] 玩家行动时招式面板展示 6 个装备招式、调息和使用道具，不展示普通攻击
- [ ] 每个招式格显示名称、体系、内息消耗、触发条件图标和一行效果摘要
- [ ] 心法专属招式有“心法”角标
- [ ] 内息不足、心法封印、无可用战斗道具时对应项置灰并显示原因
- [ ] 聚焦或悬停招式时预览卡显示体系关系和反制提示
- [ ] 预览卡不显示胜率、期望值或最终结算输出
- [ ] 面板打开时默认聚焦第一个可用招式
- [ ] 面板打开期间收到资源刷新事件时，交互不中断且显示最新可用状态

## QA 手动检查

- **AC-1**：面板信息完整
  - Setup：给角色装备 6 个不同招式、1 个心法专属招式并进入战斗
  - Verify：所有格子字段可读，调息和使用道具存在，普通攻击不存在
  - Pass condition：玩家无需打开额外界面即可理解每个行动的大致用途

- **AC-2**：置灰逻辑
  - Setup：分别制造内息不足、心法封印、无道具状态
  - Verify：对应行动置灰并显示原因
  - Pass condition：不可用原因明确，确认键不会提交不可用行动

- **AC-3**：预览卡限制
  - Setup：聚焦克制、被克、中性招式
  - Verify：预览卡显示关系和反制提示
  - Pass condition：没有出现胜率、期望值或最终结算数字

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs`

Required automated coverage:
- Panel opens during player action and shows 6 equipped moves plus Rest/Meditate and Use Item; it does not show Basic Attack.
- Each move entry displays name, type icon, neixi cost, condition icon, effect summary, and Xinfa badge where applicable.
- Insufficient neixi, sealed Xinfa, and no usable combat item disable the relevant actions and expose reason text.
- Focus or hover updates the preview card with type relationship and counter hint.
- Preview card does not expose damage prediction, win-rate, expected value, or settlement output.
- Default focus lands on the first available action.
- Resource refresh events while the panel is open update availability without closing the panel.

Manual evidence required:
- Capture `production/qa/evidence/cu-004-move-selection-panel-and-preview-card-evidence.md`.
- Include screenshots or clips for full panel, disabled reasons, preview card, and default focus.

## 测试证据路径

`production/qa/evidence/cu-004-move-selection-panel-and-preview-card-evidence.md`

## 依赖关系

- Depends on: cu-001, cu-002
- Unlocks: cu-005, cu-008

## Completion Notes

**Completed**: 2026-06-15
**Criteria**: 8/8 passing
**Deviations**: Advisory — UI manual walkthrough evidence is not yet captured at `production/qa/evidence/cu-004-move-selection-panel-and-preview-card-evidence.md`; tracked as tech debt for QA sign-off.
**Test Evidence**: `tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs` passed 18/18; full `Foundation.Tests` passed 1313/1313 with SDK 8.0.421 using `DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0`.
**Code Review**: Complete — `/code-review` approved with suggestions after FocusManager lifecycle fix.
