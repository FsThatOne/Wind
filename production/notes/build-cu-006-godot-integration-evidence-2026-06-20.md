# cu-006 Godot Integration BUILD Evidence — 2026-06-20

> **Story**: [production/epics/combat-ui/stories/cu-006-godot-integration.md](../epics/combat-ui/stories/cu-006-godot-integration.md)
> **TR-ID**: TR-combat-ui-006
> **Sprint**: 6
> **Status**: BUILD Complete — cu006 6/6 PASS + smoke 2/2 + Foundation 1359/1359（2026-06-20）
> **Sign-off**: lead-programmer __pending__ / designer __pending__

## 概要

cu-006 Foundation 契约 8 fact 切到 **Godot 4.7-stable 实机 API**；`ICombatService` Facade `RequestDecisiveStrike(actorId, targetId)` 暴露给 cu-001 / cu-005；6 条 AC 集成测试覆盖实机硬约束 5 条。Foundation 不动原则保持（Foundation 1359 测试基线零退步）。

## 测试基线（2026-06-20）

| 套件 | 入口 | 结果 |
|---|---|---|
| Foundation | `dotnet test tests/Foundation/Foundation.Tests.csproj` | 1359 / 1359 PASS |
| Smoke (Stage 1 自研 runner) | `./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh smoke` | 2 / 2 PASS（quit code 0） |
| cu006 集成 | `./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh cu006` | 6 / 6 PASS（quit code 0） |
| Foundation build | `dotnet build src/FengZhi.Foundation/FengZhi.Foundation.csproj` | 0 errors |
| Harness build | `dotnet build prototypes/sprint5-combat-ui-harness/Sprint5CombatUiHarness.csproj` | 0 errors / 0 warnings |

## 6 AC 测试方法清单

测试套：[CombatUiDecisiveGodotIntegrationTest.cs](../../prototypes/sprint5-combat-ui-harness/scripts/tests/cu006/CombatUiDecisiveGodotIntegrationTest.cs)

| AC | 测试方法 | 验证点 | 对应硬约束 |
|---|---|---|---|
| AC-1 | `cu006.ac1.time_scale_bridge_writes_engine_time_scale_and_dispose_restores` | `TimeScaleEngineBridge` 把 controller 输出投到 `Engine.TimeScale`，Dispose 还原 1.0 | #1 Tween Always + ignore_time_scale |
| AC-2 | `cu006.ac2.director_tick_publishes_seven_phase_advanced_events` | director 推进 7 phase + `DecisiveStrikeCompletedEvent` | — |
| AC-3 | `cu006.ac3.camera_bridge_applies_and_restores_smoothing_zoom_position` | `CameraRequestBusBridge` 切 smoothing/zoom/position 三态 | #4 Camera2D 必经 Bridge |
| AC-4 | `cu006.ac4.lock_filter_blocks_combat_actions_allows_ui_pause` | any-allowed filter 语义 + 白名单 pause 通行 | #2 InputMap 只读 + #3 Filter any-allowed |
| AC-5 | `cu006.ac5.director_two_sequential_decisive_strikes_run_serialized` | 串行决胜 + 并发请求抛 `InvalidOperationException` + LIFO 释放 | #5 决胜不可跳过 |
| AC-6 | `cu006.ac6.combat_service_request_decisive_strike_end_to_end_releases_all_side_effects` | `ICombatService` 端到端 + 全部副作用归位 | #1-#5 全部 |

> ADR-0011 §实机集成（cu-006 BUILD, 2026-06-20）硬约束 #1-#5：[adr-0011-combat-ui-animation.md](../../docs/architecture/adr-0011-combat-ui-animation.md)。

## Filter 语义修订（cu-006 BUILD）

- **初版**（first-match）：遍历 `InputMap.GetActions()`，遇到第一个 `EventIsAction` 命中的 action 即用其白名单成员资格决定 consume。
- **bug**：`Key.Escape` 同时映射 Godot 内置 `ui_cancel`（不在白名单）+ 项目 `ui_pause`（在白名单）时，first-match 总命中 `ui_cancel` 即 consume，pause 永远无法通行——即使白名单中明确包含 `ui_pause`。
- **修订**（any-allowed）：当 lock 锁定时遍历所有命中事件的 action；任一命中 action 在白名单即放行；只有命中至少一个且全部不在白名单才 consume。
- **落地**：[CombatCinematicLockInputFilter.cs](../../src/FengZhi.Foundation/CombatUi/GodotIntegration/CombatCinematicLockInputFilter.cs)；通过 cu006.ac4 集成测试锁定。
- ADR-0011 §实机集成 硬约束 #3 同步记录。

## Bridge 与 Facade 路径

| 组件 | 路径 |
|---|---|
| `TimeScaleEngineBridge` | [src/FengZhi.Foundation/CombatUi/GodotIntegration/TimeScaleEngineBridge.cs](../../src/FengZhi.Foundation/CombatUi/GodotIntegration/TimeScaleEngineBridge.cs) |
| `CameraRequestBusBridge` | [src/FengZhi.Foundation/CombatUi/GodotIntegration/CameraRequestBusBridge.cs](../../src/FengZhi.Foundation/CombatUi/GodotIntegration/CameraRequestBusBridge.cs) |
| `CombatCinematicLockInputFilter` | [src/FengZhi.Foundation/CombatUi/GodotIntegration/CombatCinematicLockInputFilter.cs](../../src/FengZhi.Foundation/CombatUi/GodotIntegration/CombatCinematicLockInputFilter.cs) |
| `ICombatService` | [src/FengZhi.Foundation/CombatUi/ICombatService.cs](../../src/FengZhi.Foundation/CombatUi/ICombatService.cs) |
| `CombatService` | [src/FengZhi.Foundation/CombatUi/CombatService.cs](../../src/FengZhi.Foundation/CombatUi/CombatService.cs) |
| `IDecisiveContextProvider` | [src/FengZhi.Foundation/CombatUi/IDecisiveContextProvider.cs](../../src/FengZhi.Foundation/CombatUi/IDecisiveContextProvider.cs) |
| `InMemoryDecisiveContextProvider` | [src/FengZhi.Foundation/CombatUi/InMemoryDecisiveContextProvider.cs](../../src/FengZhi.Foundation/CombatUi/InMemoryDecisiveContextProvider.cs) |
| 自研 runner | [GodotTestRunner.cs](../../prototypes/sprint5-combat-ui-harness/scripts/tests/GodotTestRunner.cs) / [GodotTestAttribute.cs](../../prototypes/sprint5-combat-ui-harness/scripts/tests/GodotTestAttribute.cs) / [TestAssert.cs](../../prototypes/sprint5-combat-ui-harness/scripts/tests/TestAssert.cs) |

## Visual artifact

> **占位（pending capture）**：本 BUILD 自动化测试已锁定全部 6 AC + 5 硬约束；视觉 artifact 由 designer 在录屏环境补齐，补齐后双签字此 note。

| 类型 | 路径 | 状态 |
|---|---|---|
| 30s mp4 — 7 phase 完整演出 + 暂停冲突 + LIFO 释放 | `production/qa/evidence/media/cu-006-godot-integration.mp4` | pending |
| 截图 — TimeScale overlay（1.0 → 0.2 → 1.0） | `production/qa/evidence/media/cu-006-time-scale.png` | pending |
| 截图 — Camera 推/锁/归位 | `production/qa/evidence/media/cu-006-camera.png` | pending |
| 截图 — InputMap 守约 + cinematic lock 过滤层日志 | `production/qa/evidence/media/cu-006-input-filter.png` | pending |

## 关联

- Story：[cu-006-godot-integration.md](../epics/combat-ui/stories/cu-006-godot-integration.md)（Status: Complete）
- ADR：[ADR-0011 §实机集成（cu-006 BUILD, 2026-06-20）](../../docs/architecture/adr-0011-combat-ui-animation.md)
- QA plan：[qa-plan-sprint-6-2026-06-18.md §cu-006-godot-integration](../qa/qa-plan-sprint-6-2026-06-18.md)
- Sprint status：[sprint-status.yaml](../sprint-status.yaml)（cu-006-godot-integration → done, 2026-06-20）
- Foundation evidence（Foundation 8 fact）：[cu-006-decisive-strike-animation-director-evidence.md](../qa/evidence/cu-006-decisive-strike-animation-director-evidence.md)
- Spike report：[spike-cu-006-godot-4.7-api-verify-2026-06-20.md](spike-cu-006-godot-4.7-api-verify-2026-06-20.md)
