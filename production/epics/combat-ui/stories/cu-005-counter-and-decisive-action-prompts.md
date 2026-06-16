# Story: cu-005 — 反制与决胜行动提示

> **Epic**: combat-ui
> **类型**: Integration
> **优先级**: P0 — 战斗关键决策提示
> **Estimate**: M（约 6h）
> **依赖**: cu-003, cu-004
> **阻塞**: cu-006
> **ADR 指引**: ADR-0011（战斗动画与提示编排），ADR-0002（面板可交互项）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 反制按钮 / 决胜提示, §Edge Cases
> **TR-ID**: TR-combat-ui-005
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-15

## 目标

在招式面板中呈现反制和决胜窗口：当选中招式克制目标公开意图时提示“反制”，当目标破绽 ≥5 时插入并高亮“决胜一击”。

## 范围

### 包含
- 选中招式克制目标公开意图且内息 ≥3 时显示金色“反制”标签
- 选中招式克制目标公开意图但内息 <3 时显示置灰“反制 / 内息不足”
- 玩家确认带反制标签的行动时，把 `isCounter` 意图传回战斗系统
- 当前目标破绽 ≥5 时，在面板顶部插入“决胜一击”高亮行
- 多个目标破绽达标时，默认高亮第一个目标，允许目标切换后刷新提示
- 切换目标时关闭旧目标决胜高亮并重新检查新目标破绽
- 当前选择确认前内息被刷新时，即时更新反制可用状态

### 不包含
- 反制判定和内息扣除的战斗结算
- 一击决胜演出 → cu-006
- 协同提示 → cu-007

## 技术说明

- 反制可用性必须以战斗系统公开意图、选中招式体系和当前内息快照为来源
- UI 不计算反制伤害，只传递玩家是否选择反制
- “决胜一击”行是面板内可聚焦的独立行动项，不需要额外隐藏快捷键
- 多目标决胜状态来自 `OnDecisiveStrikeAvailable` 或 combat service opportunity snapshot
- 资源事件在面板打开期间到达时，不关闭面板，只刷新可用状态

## Control Manifest Rules

- 所有 UI 必须基于 Godot Control 节点树 + 薄抽象层，新增面板/行项继承或组合 `BaseUiPanel` / Control 契约（ADR-0002）。
- “反制”标签和“决胜一击”行只能展示 Combat / MartialArts 快照结果，UI 不得执行扣费、伤害、破绽或结算逻辑。
- “决胜一击”行属于可交互行动项，必须使用 `FocusModeEnum.All`，非交互提示/装饰节点必须使用 `FocusModeEnum.None`。
- 面板打开、关闭与目标切换必须通过 `IFocusManager.PushFocus` / `PopFocus` 或同等焦点栈生命周期处理，不得依赖 hover-only 交互。
- 资源、目标和公开意图事件只标记 dirty；刷新必须集中在 dirty pass 中完成，禁止事件回调中无条件重建整面板。
- 禁止在预览、反制或决胜提示中显示伤害预测、胜率、期望值或结算输出。

## Performance Notes

- 继承 Combat HUD 刷新预算：反制/决胜提示刷新目标 `<= 1ms/frame`。
- 面板空闲显示期间不得产生 per-frame allocation；仅在内息、目标、公开意图或决胜机会快照变化时刷新受影响行项。
- 反制可用性只做 O(1) 快照比较和阈值检查，不得扫描场景树、读取文件或查询实时战斗结算器。
- 决胜多目标切换必须基于稳定目标快照刷新，不得在 UI 层重新计算破绽来源或累计值。
- 资源刷新不得关闭面板、重置玩家当前选择或重复 push 焦点栈。

## Engine Notes

- Godot 4.6.3 dual-focus 行为必须保留：键盘/手柄焦点切换不得清除鼠标 hover 高亮，hover 也不得抢占手柄焦点。
- “决胜一击”行必须是可聚焦 Control，并参与与普通招式相同的导航顺序；打开面板时默认焦点仍遵守第一个可用行动规则。
- 使用 `grab_focus()` 时必须受 FocusManager 生命周期保护，避免目标切换或资源刷新重复压栈。
- 若后续接入 SceneTreeTween 或 TimeScale 演出，演出编排属于 `cu-006`，本 story 只产出提示与行动意图。

## 验收标准

- [ ] 招式克制目标公开意图且内息 ≥3 时显示“反制”标签
- [ ] 内息不足时反制标签置灰并显示“内息不足”
- [ ] 确认反制行动时 UI 向战斗系统提交 `isCounter=true`
- [ ] 目标破绽 ≥5 时面板顶部插入“决胜一击”高亮行
- [ ] 多目标破绽达标时切换目标会刷新决胜提示
- [ ] 面板打开期间内息变化会即时刷新反制可用状态
- [ ] UI 不执行反制扣费、伤害、破绽结算

## QA 手动检查

- **AC-1**：反制可用
  - Setup：敌方公开柔意图，玩家聚焦刚系招式且内息 ≥3
  - Verify：招式旁出现“反制”标签
  - Pass condition：确认后提交反制意图，战斗结算由 Core 接管

- **AC-2**：反制不可用
  - Setup：同样克制关系但内息 <3
  - Verify：反制标签置灰并显示内息不足
  - Pass condition：玩家能看见机会，但不会误以为当前可触发

- **AC-3**：决胜目标切换
  - Setup：两个敌人破绽达标，打开招式面板并切换目标
  - Verify：“决胜一击”行随当前目标刷新
  - Pass condition：旧目标提示关闭，新目标满足条件时高亮

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs`

Required automated coverage:
- Selected move counters target public intent and neixi >= 3 shows enabled gold `反制`.
- Same counter relationship with neixi < 3 shows disabled `反制 / 内息不足`.
- Confirming a counter action emits `MoveSelected(actorId, moveId, targetId, isCounter: true)`.
- Target stagger >= 5 inserts a Decisive Strike row.
- Multiple decisive targets refresh highlight when target changes.
- Neixi changes while the panel is open update counter availability immediately.
- UI does not deduct neixi, calculate counter damage, calculate stagger, or execute settlement.

Manual evidence required:
- Capture `production/qa/evidence/cu-005-counter-and-decisive-action-prompts-evidence.md`.
- Include enabled counter, disabled counter, Decisive Strike row, and target-switch prompt refresh.

## 测试证据路径

`production/qa/evidence/cu-005-counter-and-decisive-action-prompts-evidence.md`

## 依赖关系

- Depends on: cu-003, cu-004
- Unlocks: cu-006

## Completion Notes

**Completed**: 2026-06-15
**Criteria**: 7/7 passing
**Deviations**: None blocking. Advisory: manual UI evidence not yet captured for `production/qa/evidence/cu-005-counter-and-decisive-action-prompts-evidence.md`.
**Test Evidence**: Integration test at `tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs` (17 tests). `CombatUiCounterDecisivePromptTest` passed 17/17.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS. User accepted Lean-mode review result before closure.
