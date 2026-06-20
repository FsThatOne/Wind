# Story: cu-006-godot-integration — 一击决胜 Godot 4.6.3 实机集成

> **Epic**: combat-ui
> **类型**: Integration
> **Type**: Integration
> **Sprint**: 6
> **优先级**: P0 — Sprint 6 Must Have（Godot 实机集成 + ICombatService Facade）
> **Estimate**: M（约 6h，对应 sprint plan 0.75 estimate-day）
> **依赖**: cu-006 (Foundation 契约 — Complete)、cu-001 (CombatBattleEventAdapter)、cu-005 (Counter / Decisive prompts)
> **阻塞**: 无（Foundation 8 fact 已 Complete；待开发即可）
> **ADR 指引**: ADR-0002（Godot 4.6.3 / dual-focus / FocusManager）、ADR-0011（CombatAnimationDirector / TimeScaleController / CameraRequestBus / CombatCinematicLock）、ADR-0009（音效同步点）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design 6 一击决胜演出流程 (L106-122)、§Formulas F2 TimeScale 插值 (L214-232)、§Edge Cases (L233-251)、§Acceptance Criteria AC-5
> **TR-ID**: TR-combat-ui-006（沿用 cu-006 Foundation；本 story 是该 TR 的 Sprint 6 实机集成增量）
> **Control Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-19
> **状态**: Ready

## 目标

把 cu-006 Foundation 契约（已锁的 8 fact）从 in-memory test double 切到 **Godot 4.6.3 实机 API**，验证 `Engine.TimeScale` / `Tween.TweenProcessMode.Always` / `Camera2D` / `InputMap` 四个高 Engine Risk 点在实机仍守约；同时把 `ICombatService` Facade `RequestDecisiveStrike` 暴露给 cu-001 / cu-005 调用方，关闭 combat-ui Presentation epic 的最后实机门槛。

## 范围

### 包含
- 新增 Godot 实机集成测试套：`tests/integration/combat-ui/combat_ui_decisive_godot_integration_test.cs`
- `TimeScaleController` 与 `Engine.TimeScale` 的实机绑定（priority 50；slow_in 0.3s → 0.2 → hold 1.0s → slow_out 0.3s → 1.0）
- `Tween.SetProcessMode(TweenProcessMode.Always)` 在 pause stack 叠加（TimeScale=0）下不自锁的实机验证
- `Camera2D` 经 `CameraRequestBus`（priority 50）在实机抢占、锁定目标、关闭 smoothing；演出后归位 default 跟随
- `InputMap` 在 cinematic lock 期间不被破坏；`CombatCinematicLock` whitelist（pause 仍可用）实机验证
- `ICombatService.RequestDecisiveStrike(actorId, targetId)` Facade 暴露：cu-001 / cu-005 经此入口触发 → Foundation director → Godot 集成层 → `BattleEventBus` 三个 lifecycle event（Started / PhaseAdvanced / Completed）
- 暂停（更高优先级 TimeScale request）介入 → 释放后从被中断 phase 继续的实机回归测试
- `production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md` 升级 **Godot Integration Captured** 段（≥1 段录屏 + TimeScale overlay 帧 + Camera 推/归位帧 + 暂停冲突测试帧）

### 不包含
- Foundation 契约层 8 fact 不动（已 Complete，禁止重复绑定）
- 决胜伤害结算逻辑（属 Combat Foundation）
- 体系专属动画美术资源（gang/rou/qiao — 属美术 sprint）
- 音效资产（属 audio 系统）
- 真实手柄硬件 walkthrough（走 cu-008-Gamepad-HW-Verify Nice to Have）

## 技术说明

- **Foundation 不动**：Foundation 契约 8 fact 在 `tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs`，本 story 仅在新文件 `combat_ui_decisive_godot_integration_test.cs` 中验证 Godot API 守约，不重复绑定 Foundation fact
- **TimeScale**：必须经 `TimeScaleController` 优先级栈，禁止直接写 `Engine.TimeScale`；优先级 50；演出 Tween 必须 `SetProcessMode(Tween.TweenProcessMode.Always)` 才能在 TimeScale=0 时仍按 wall-clock 推进
- **Camera2D**：必须经 `CameraRequestBus`（priority 50）；演出期间禁用 smoothing，归位后恢复
- **InputMap**：禁止任何写操作；屏蔽走 `CombatCinematicLock` whitelist；pause action 必须留在 whitelist 内
- **ICombatService Facade**：在 `FengZhi.Foundation.CombatUi` 暴露 `RequestDecisiveStrike(actorId, targetId)`；内部转 Foundation director；publish `DecisiveStrikeStarted / PhaseAdvanced / Completed` 三个事件到 `BattleEventBus`；cu-001 / cu-005 切到此入口（不再自己 new director）
- **Test harness**：复用 `prototypes/sprint5-combat-ui-harness/` scene 加 Godot integration runner；测试以 `[GodotTestFact]` / scene-test 方式启动（具体框架沿用现有 integration test runner）
- **决胜不可跳过**：`AllowDecisiveSkip = false`（默认）下任意输入键不跳过演出；如 sprint 6 不开放跳过，则 Edge case "AllowDecisiveSkip = true" 标 deferred

## 验收标准

- [ ] **AC-1（TimeScale 实机插值）**：`Engine.TimeScale` 经 `TimeScaleController`（priority 50）从 1.0 在 ≤ 0.3s 内插值降至 `decisive_timescale_min = 0.2`，hold ≥ 1.0s，再在 ≤ 0.3s 内回到 1.0；终值必须为 1.0 ±0.001（对应 GDD F2 + AC-5；Godot integration test：`Engine_TimeScale_DecisiveRequest_LerpsTo0_2AndRestoresTo1_0`）
- [ ] **AC-2（Tween Always 不自锁）**：演出 Tween `SetProcessMode(TweenProcessMode.Always)`；在 pause stack 叠加（TimeScale=0）情形下 Tween 仍按 wall-clock 推进至少一个 sample tick（Godot integration test：`Tween_AlwaysMode_UnderTimeScaleZero_StillAdvancesByWallClock`）
- [ ] **AC-3（Camera2D priority bus）**：`Camera2D` 经 `CameraRequestBus`（priority 50）抢占 → 锁定目标 → smoothing_enabled = false；演出 Completed 后 1 帧内恢复 default 跟随 + smoothing 还原（Godot integration test：`Camera2D_DecisiveRequest_LocksTargetDisablesSmoothingAndRestoresAfterCompleted`）
- [ ] **AC-4（InputMap 守约 + cinematic lock）**：演出全程 `InputMap.GetActions()` 集合与开演前完全一致；战斗 action（`combat_confirm` / `combat_cancel` / `combat_move_*`）经 `CombatCinematicLock.IsBlocked(...)` 返回 true；`pause` action whitelisted（Godot integration test：`InputMap_DuringDecisive_NotMutatedAndCombatActionsBlockedExceptPauseWhitelist`）
- [ ] **AC-5（ICombatService Facade）**：`ICombatService.RequestDecisiveStrike(actorId, targetId)` 调用 → Foundation director 接收 → Godot 集成层执行 7 phase → `BattleEventBus` 依次 publish `DecisiveStrikeStarted` / `DecisiveStrikePhaseAdvanced × 7` / `DecisiveStrikeCompleted`；cu-001 / cu-005 现有自动化（Foundation 1295/1295 + Combat UI 95/95）不退步（Godot integration test：`ICombatService_RequestDecisiveStrike_PublishesLifecycleEventsAndReachesGodotIntegrationLayer`）
- [ ] **AC-6（暂停冲突恢复）**：演出途中收到更高优先级 TimeScale request（pause stack）→ TimeScale 切到 0；release 后从被中断 phase 继续，phase 计数不丢、不重发；最终 TimeScale = 1.0、Camera 默认跟随、InputMap 完整（Godot integration test：`PauseStackInjectedDuringDecisive_OnReleaseResumesFromInterruptedPhase`，覆盖 GDD §Edge Cases "Engine.TimeScale 在演出途中被其他系统修改"）
- [ ] **AC-7（Visual evidence）**：`production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md` 新增 **Godot Integration Captured** 段：≥1 段录屏（mp4 / mov / gif）+ TimeScale overlay 数值帧 + Camera 推/锁/归位帧 + 暂停冲突测试帧 + lead-programmer + designer sign-off

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_decisive_godot_integration_test.cs`（**新增** — 与 Foundation 文件 `combat_ui_decisive_animation_director_test.cs` 并存；Foundation 8 fact 不动）

**Required automated coverage**（Godot 4.6.3 实机集成）：
- `Engine_TimeScale_DecisiveRequest_LerpsTo0_2AndRestoresTo1_0`
- `Tween_AlwaysMode_UnderTimeScaleZero_StillAdvancesByWallClock`
- `Camera2D_DecisiveRequest_LocksTargetDisablesSmoothingAndRestoresAfterCompleted`
- `InputMap_DuringDecisive_NotMutatedAndCombatActionsBlockedExceptPauseWhitelist`
- `ICombatService_RequestDecisiveStrike_PublishesLifecycleEventsAndReachesGodotIntegrationLayer`
- `PauseStackInjectedDuringDecisive_OnReleaseResumesFromInterruptedPhase`

**Edge cases to cover**（GDD §Edge Cases + qa-plan-sprint-6）：
- Decisive Strike 触发瞬间 actor 已落败 → 演出请求被 Foundation 契约拒绝并清栈，不崩溃
- 演出途中再次触发 Decisive Strike → 按 Foundation 契约 throw / cancel-current 路径（沿用 Foundation fact）
- TimeScale 栈在最后一帧恢复时与 Tween 回调竞争 → 终值 1.0 ±0.001
- `AllowDecisiveSkip = false` 下任意输入键 → 演出不跳过；`AllowDecisiveSkip = true` 路径标 deferred 到下个 sprint

**Manual evidence required**：
- 升级 `production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md`：
  - Foundation Captured 段保留
  - 新增 **Godot Integration Captured** 段：≥1 段录屏 + TimeScale overlay 数值 + Camera 推/拉/归位帧 + 暂停冲突测试
  - Sign-off：lead-programmer + designer
- Reference QA plan: `production/qa/qa-plan-sprint-6-2026-06-18.md` Automated Tests Required §cu-006-godot-integration

## Test Evidence

`production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md`（升级 Visual Captured 段）

## Performance Notes

- 演出全程 ≤ 1.6s wall-clock（GDD F2 默认参数：slow_in 0.3 + hold 1.0 + slow_out 0.3）；不引入额外帧预算压力
- Tween Always 模式不增加每帧成本（Godot 内部仍走单一 process pass）
- Camera priority bus 操作 O(1)；测试中允许的最大 frame budget 与 cu-006 Foundation 一致

## Engine Notes

- Godot 4.6.3 `Engine.TimeScale` 是 process-level 全局缩放；本 story 唯一通过 `TimeScaleController` 写入此值
- `Tween.TweenProcessMode.Always` 是 4.x 起的 process-mode 枚举（与 4.6.3 一致）；任何替代 API（如 `Tween.TweenPauseMode`）都不允许
- `Camera2D.PositionSmoothingEnabled` 命名为 4.x 风格；不要使用 3.x 的 `smoothing_enabled` 字符串
- `InputMap` 写 API（`AddAction` / `EraseAction`）在演出全程禁止调用
- 集成 test 必须以 Godot 实机 scene runner 启动（不能仅用 in-memory mock）

## 依赖关系

- Depends on: cu-006 Foundation 契约（Complete）、cu-001（CombatBattleEventAdapter Ready）、cu-005（Counter / Decisive prompts Ready）
- Unlocks: combat-ui EPIC.md status: Ready → Done（经 S6-Combat-UI-Epic-Close）；后续 audio / blurred-ui Presentation epic 拆分（S6-Next-Presentation-Cut）
- Blocks: cu-visual-evidence（cu-006 Visual evidence 段升级依赖本 story 录屏）

## Out of Scope（再次明确）

- Foundation 契约层 fact 重写
- 决胜伤害公式调整
- 美术资源（gang/rou/qiao 体系动画）
- 音效资产
- 真实手柄实机走查（→ cu-008-Gamepad-HW-Verify）
- `AllowDecisiveSkip = true` 路径开放（→ 下个 sprint tuning knob 决议）
