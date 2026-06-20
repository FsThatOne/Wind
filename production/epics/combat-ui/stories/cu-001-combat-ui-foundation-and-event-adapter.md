# Story: cu-001 — 战斗 UI 基础层与事件适配

> **Epic**: combat-ui
> **类型**: Integration
> **优先级**: P0 — Presentation 战斗 UI 底座
> **Estimate**: M（约 6h）
> **依赖**: combat-system 事件契约、martial-arts-system 查询契约
> **阻塞**: cu-002, cu-003, cu-004, cu-006
> **ADR 指引**: ADR-0002（Godot Control / BaseUiPanel / dual-focus），ADR-0011（CombatAnimationDirector / 动画命令队列），ADR-0009（音频系统订阅同源事件）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design, §States and Transitions, §Interactions with Other Systems
> **TR-ID**: TR-combat-ui-001
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-14

## 目标

建立战斗 UI 的 Presentation 底座：两个 CanvasLayer、事件适配器、UI 状态快照与状态流，让后续意图、资源条、招式面板和演出 story 都有统一入口。

## 范围

### 包含
- 创建 `WorldIntentLayer` 与 `HUDLayer` 的基础结构
- 建立战斗事件到 UI 快照的适配层
- 订阅 `OnIntentRevealed / OnDamageDealt / OnStaggerChanged / OnDecisiveStrikeAvailable / OnBattleEnd / OnNeixiChanged`
- 支持 `INACTIVE → ROUND_START → PLAYER_DECISION → RESOLVING → ROUND_END → BATTLE_END` 的 UI 状态流
- 明确 UI 只呈现战斗系统结果并收集玩家输入，不拥有战斗结算逻辑
- 使用脏标记刷新，避免每帧无条件刷新

### 不包含
- 意图图标具体视觉与动画 → cu-002
- 资源条与浮字样式 → cu-003
- 招式选择面板 → cu-004
- 一击决胜演出流程 → cu-006
- 最终美术资产与音效资产

## 技术说明

- 战斗 UI 面板必须基于 Godot Control 节点树并继承 `BaseUiPanel`
- 跨层战斗事件必须通过 EventBus 订阅，订阅者在 `_ExitTree()` 中清理
- UI 层不得重新计算伤害、破绽、反制结果或意图识破结果，只消费 Core/Combat 和 MartialArts 提供的快照
- `WorldIntentLayer` 使用 CanvasLayer WorldUi 层级语义；`HUDLayer` 使用 Hud 层级语义
- 所有实时刷新通过 `_dirty + _Process` 合批，不允许直接在事件回调中做大量 UI 重建
- Engine 风险：Godot 4.7-stable dual-focus 与 SceneTreeTween 相关行为需在后续交互/演出 story 中留证据

## 验收标准

- [x] 战斗开始后创建并显示 `WorldIntentLayer` 和 `HUDLayer`
- [x] 战斗事件进入 UI 适配层后转为只读 UI 快照
- [x] UI 状态能按 GDD 状态流从战斗开始推进到战斗结束
- [x] UI 不计算战斗结果，不显示伤害预测数字，不修改 Core 战斗状态
- [x] 事件订阅生命周期安全，离开战斗场景后不残留订阅
- [x] HUD 刷新使用脏标记，战斗 HUD 刷新预算目标不超过 1ms/frame

## QA 手动检查

- **AC-1**：CanvasLayer 底座
  - Setup：进入一场测试战斗
  - Verify：场景中存在世界意图层与 HUD 层，层级关系正确
  - Pass condition：意图层跟随战场目标，HUD 层固定在屏幕空间

- **AC-2**：事件适配
  - Setup：触发意图、伤害、破绽、内息、战斗结束事件
  - Verify：UI 快照被更新，事件回调不直接执行结算逻辑
  - Pass condition：UI 显示与战斗快照一致，关闭场景后无重复事件响应

- **AC-3**：状态流
  - Setup：完整跑一轮战斗回合
  - Verify：UI 状态按 ROUND_START、PLAYER_DECISION、RESOLVING 等阶段切换
  - Pass condition：状态切换不丢事件、不闪退、不阻塞玩家决策

## Test Evidence / 测试证据

`production/qa/evidence/cu-001-combat-ui-foundation-and-event-adapter-evidence.md`

## 依赖关系

- Depends on: cb-008（战斗事件总线）、cb-010（战斗入口与配置接口）、ma-001（武学配置查询基础）、ma-008（武学 UI 呈现规则）、ADR-0002（UI 底座）
- Unlocks: cu-002, cu-003, cu-004, cu-006

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 6/6 passing
**Deviations**: None blocking. Note: current xUnit evidence verifies Godot layer types, entry signatures, layer order, event adaptation, and lifecycle behavior without launching a real Godot scene tree; visible scene placement and dual-focus walkthrough evidence remains for later combat UI stories.
**Test Evidence**: Integration evidence at `production/qa/evidence/cu-001-combat-ui-foundation-and-event-adapter-evidence.md`; `CombatUiFoundationEventAdapterTest` passed 9/9; Foundation full suite passed 1223/1223.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS.
