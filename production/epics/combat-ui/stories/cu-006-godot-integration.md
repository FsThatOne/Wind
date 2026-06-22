# Story: cu-006-godot-integration — 一击决胜 Godot 4.7-stable 实机集成

> **Epic**: combat-ui
> **类型**: Integration
> **Type**: Integration
> **Sprint**: 6
> **优先级**: P0 — Sprint 6 Must Have（Godot 实机集成 + ICombatService Facade）
> **Estimate**: M（约 6h，对应 sprint plan 0.75 estimate-day）
> **依赖**: cu-006 (Foundation 契约 — Complete)、cu-001 (CombatBattleEventAdapter)、cu-005 (Counter / Decisive prompts)
> **阻塞**: 无（Foundation 8 fact 已 Complete；待开发即可）
> **ADR 指引**: ADR-0002（Godot 4.7-stable / dual-focus / FocusManager）、ADR-0011（CombatAnimationDirector / TimeScaleController / CameraRequestBus / CombatCinematicLock）、ADR-0009（音效同步点）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design 6 一击决胜演出流程 (L106-122)、§Formulas F2 TimeScale 插值 (L214-232)、§Edge Cases (L233-251)、§Acceptance Criteria AC-5
> **TR-ID**: TR-combat-ui-006（沿用 cu-006 Foundation；本 story 是该 TR 的 Sprint 6 实机集成增量）
> **Control Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-20
> **状态**: Complete

## 目标

把 cu-006 Foundation 契约（已锁的 8 fact）从 in-memory test double 切到 **Godot 4.7-stable 实机 API**，验证 `Engine.TimeScale` / `Tween.TweenProcessMode.Always` / `Camera2D` / `InputMap` 四个高 Engine Risk 点在实机仍守约；同时把 `ICombatService` Facade `RequestDecisiveStrike` 暴露给 cu-001 / cu-005 调用方，关闭 combat-ui Presentation epic 的最后实机门槛。

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

- [x] **AC-1（TimeScale 实机插值）** ✅ 2026-06-20 — `cu006.ac1.time_scale_bridge_writes_engine_time_scale_and_dispose_restores`（[CombatUiDecisiveGodotIntegrationTest.cs](../../../../prototypes/sprint5-combat-ui-harness/scripts/tests/cu006/CombatUiDecisiveGodotIntegrationTest.cs)）：`TimeScaleEngineBridge` 把 Controller 的 1.0/0.2/0.0 分别投射到 `Engine.TimeScale`，Dispose 后恢复 1.0
- [x] **AC-2（Tween Always 不自锁）** ✅ 2026-06-20 — Sprint 6 Spike 4/4 PASS 已验证 Tween + `set_ignore_time_scale(true)` 在 `time_scale = 0` 下按 wall-clock 推进（[spike-cu-006-godot-4.7-api-verify-2026-06-20.md](../../../notes/spike-cu-006-godot-4.7-api-verify-2026-06-20.md)）；本 story 沿用该 spike artifact（不在集成测试中重复）
- [x] **AC-3（Camera2D priority bus）** ✅ 2026-06-20 — `cu006.ac3.camera_bridge_applies_and_restores_smoothing_zoom_position`：`CameraRequestBusBridge` 在请求 active 时关闭 smoothing + 投射 zoom/position；释放后恢复 default
- [x] **AC-4（InputMap 守约 + cinematic lock）** ✅ 2026-06-20 — `cu006.ac4.lock_filter_blocks_combat_actions_allows_ui_pause`：测试只读 `InputMap.GetActions()` / `EventIsAction()`；锁锁定时 `CombatCinematicLockInputFilter.ShouldConsume` 用 any-allowed 语义（任一命中 action 在白名单即放行），独立白名单验证 pause 仍可通行
- [x] **AC-5（ICombatService Facade）** ✅ 2026-06-20 — `cu006.ac6.combat_service_request_decisive_strike_end_to_end_releases_all_side_effects` 与 `cu006.ac2.director_tick_publishes_seven_phase_advanced_events`：`ICombatService.RequestDecisiveStrike(actorId, targetId)` 经 `IDecisiveContextProvider` → director → 7 次 `DecisiveStrikePhaseAdvancedEvent` → `DecisiveStrikeCompletedEvent`；Foundation 1359/1359 + harness 2/2 smoke + cu006 6/6 不退步
- [x] **AC-6（暂停冲突恢复）** ✅ 2026-06-20 — Foundation fact `DecisiveSequence_PauseDuringSlowMotion_ContinuesFromInterruptedPhase` 与 `TimeScaleController_PauseStackPreemptsDecisiveAndRestoresOnRelease`（Foundation 锁定）+ `cu006.ac5.director_two_sequential_decisive_strikes_run_serialized` 验证两段串行决胜各自完成 + LIFO 释放
- [x] **AC-7（Visual evidence）** ✅ 2026-06-20 — `production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md` 升级 **Godot Integration Captured** 段（cu-006 BUILD evidence note 链接：[build-cu-006-godot-integration-evidence-2026-06-20.md](../../../notes/build-cu-006-godot-integration-evidence-2026-06-20.md)）

## Implementation Notes

- **3 个 Bridge**：[TimeScaleEngineBridge.cs](../../../../src/FengZhi.Foundation/CombatUi/GodotIntegration/TimeScaleEngineBridge.cs)、[CameraRequestBusBridge.cs](../../../../src/FengZhi.Foundation/CombatUi/GodotIntegration/CameraRequestBusBridge.cs)、[CombatCinematicLockInputFilter.cs](../../../../src/FengZhi.Foundation/CombatUi/GodotIntegration/CombatCinematicLockInputFilter.cs)
- **Facade 与 Provider**：[ICombatService.cs](../../../../src/FengZhi.Foundation/CombatUi/ICombatService.cs)、[CombatService.cs](../../../../src/FengZhi.Foundation/CombatUi/CombatService.cs)、[IDecisiveContextProvider.cs](../../../../src/FengZhi.Foundation/CombatUi/IDecisiveContextProvider.cs)、[InMemoryDecisiveContextProvider.cs](../../../../src/FengZhi.Foundation/CombatUi/InMemoryDecisiveContextProvider.cs)（cu-005 BUILD 时新建 BattleInstance-aware 替换实现）
- **自研测试 runner**：[GodotTestRunner.cs](../../../../prototypes/sprint5-combat-ui-harness/scripts/tests/GodotTestRunner.cs) + [GodotTestAttribute.cs](../../../../prototypes/sprint5-combat-ui-harness/scripts/tests/GodotTestAttribute.cs) + [TestAssert.cs](../../../../prototypes/sprint5-combat-ui-harness/scripts/tests/TestAssert.cs)；headless quit code = 失败数；`./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh smoke|cu006`
- **Filter 语义修订**：cu-006 BUILD 期间发现 `CombatCinematicLockInputFilter.ShouldConsume` 初版 first-match 逻辑在 Escape 同时映射 `ui_cancel` + `ui_pause` 时会错误屏蔽白名单 action（`ui_cancel` 不在白名单 → 直接 consume）；改为 any-allowed 语义（任一命中 action 在白名单即放行）。详见 ADR-0011 §实机集成（cu-006 BUILD）。
- **BattleFacade 不动**：本 story 走 `IDecisiveContextProvider` 抽象注入，避免污染 Foundation 测试基线（1359/1359 不退步）。

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_decisive_godot_integration_test.cs`（**新增** — 与 Foundation 文件 `combat_ui_decisive_animation_director_test.cs` 并存；Foundation 8 fact 不动）

**Required automated coverage**（Godot 4.7-stable 实机集成）：
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

> **[2026-06-20 verified against 4.7-stable]** — Godot 4.7 API spike PASS (4/4 verifiers).
> - `Engine.time_scale` write/read contract holds → AC-1 / AC-6 unblocked
> - `Tween` `process_mode = ALWAYS` + `ignore_time_scale = true` advances by wall-clock under `time_scale = 0` → AC-2 unblocked
> - `Camera2D.position_smoothing_enabled` toggle un-broken by 4.7 Control offset transforms → AC-3 unblocked
> - `InputMap.get_actions()` snapshot stable across cinematic-equivalent wait window → AC-4 unblocked
>
> Spike report: [production/notes/spike-cu-006-godot-4.7-api-verify-2026-06-20.md](../../../notes/spike-cu-006-godot-4.7-api-verify-2026-06-20.md)
> Spike artifacts (regression baseline): [prototypes/sprint5-combat-ui-harness/scripts/spike/](../../../../prototypes/sprint5-combat-ui-harness/scripts/spike/)
> Implementation must call `tween.set_ignore_time_scale(true)` explicitly — `process_mode` alone does NOT bypass `time_scale = 0` in 4.7.

- Godot 4.7-stable `Engine.TimeScale` 是 process-level 全局缩放；本 story 唯一通过 `TimeScaleController` 写入此值
- `Tween.TweenProcessMode.Always` 是 4.x 起的 process-mode 枚举（与 4.7-stable 一致）；任何替代 API（如 `Tween.TweenPauseMode`）都不允许
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
