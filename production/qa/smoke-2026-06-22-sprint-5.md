# Smoke Check Report

**Date**: 2026-06-22
**Sprint**: Sprint 5 — Combat UI Interaction & Navigation（全作用域 close-out，含 cu-006 Foundation + cu-007）
**Engine**: Godot 4.7-stable Mono (.NET 8+)
**QA Plan**: `production/qa/qa-plan-sprint-5-2026-06-14.md`（Sprint 5 原计划）+ `production/qa/qa-plan-sprint-6-2026-06-18.md` §S6-Sprint5-DoD（闭合驱动）
**Argument**: `sprint`
**Driving Story**: S6-Sprint5-DoD（Sprint 6 process task）
**Predecessor**: `production/qa/smoke-2026-06-16.md`（Must Have only，PASS WITH WARNINGS）— 本报告作用域扩展至 cu-006 Foundation slice + cu-007，并采用 cu-006-godot-integration BUILD 2026-06-20 后的 1359/1359 baseline
**Verdict**: PASS

---

## Environment

| Check | Result |
|-------|--------|
| Test directory | Found: `tests/Foundation/` |
| Solution | Found: `FengZhi.slnx` |
| Engine binary | `/Applications/Godot_mono.app/Contents/MacOS/Godot` (4.7-stable mono official, hash 5b4e0cb0fd279832bbdd69fed5354d4e5ad26f88) |
| Engine config | Found: `docs/engine-reference/godot/VERSION.md` |
| Godot smoke runner | Found: `prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh` |
| QA plan (Sprint 5) | Found: `production/qa/qa-plan-sprint-5-2026-06-14.md` |
| Driving QA plan | Found: `production/qa/qa-plan-sprint-6-2026-06-18.md` (§S6-Sprint5-DoD) |

---

## Automated Tests

### .NET Foundation Test Suite

**Status**: PASS

Command:

```bash
dotnet test FengZhi.slnx --nologo --verbosity quiet
```

Result：

```text
已通过! - 失败:     0，通过:  1359，已跳过:     0，总计:  1359，持续时间: 643 ms - Foundation.Tests.dll (net8.0)
```

Baseline 一致性：与 cu-006-godot-integration BUILD（2026-06-20）记录的 1359/1359 一致；无 regression。Build 仅 4 条 non-blocking warning（`BossPhaseSystem` 未用 `other1`/`other2` + `boss_battle_simulation_test` / `SceneConfigTests` 各 1 处 nullable 解引用）。

### Godot 4.7-stable Headless Suites

**Status**: PASS

Smoke suite：

```bash
./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh smoke
```

```text
Suite: SmokeTest
  PASS  smoke.engine_time_scale_writable_and_restorable
  PASS  smoke.input_map_get_actions_non_empty
=== TOTAL: 2 run, 2 pass, 0 fail ===
>>> smoke suite exited with code 0
```

cu-006 integration（Sprint 6 BUILD 产物 — 在此引用为环境健全性 evidence，不计入 Sprint 5 stories 覆盖）：

```bash
./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh cu006
```

```text
Suite: CombatUiDecisiveGodotIntegrationTest
  PASS cu006.ac1.time_scale_bridge_writes_engine_time_scale_and_dispose_restores
  PASS cu006.ac2.director_tick_publishes_seven_phase_advanced_events
  PASS cu006.ac3.camera_bridge_applies_and_restores_smoothing_zoom_position
  PASS cu006.ac4.lock_filter_blocks_combat_actions_allows_ui_pause
  PASS cu006.ac5.director_two_sequential_decisive_strikes_run_serialized
  PASS cu006.ac6.combat_service_request_decisive_strike_end_to_end_releases_all_side_effects
=== TOTAL: 6 run, 6 pass, 0 fail ===
>>> cu006 suite exited with code 0
```

> 与 06-16 smoke（NOT RUN godot）相比，本次 Godot binary 已可用，smoke 与 cu006 均已实跑，verdict 不再因 NOT RUN 自动 demote 到 PASS WITH WARNINGS。

---

## Test Coverage

| Story | Type | Test File | Coverage Status |
|-------|------|-----------|----------------|
| cu-004 Move Selection Panel & Preview Card | UI | `tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs` | COVERED |
| cu-005 Counter & Decisive Action Prompts | Integration / UI | `tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs` | COVERED |
| cu-006 Decisive Strike Animation Director（Foundation slice） | Integration | `tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs` | COVERED（8/8 fact） |
| cu-007 Synergy & Round Warning Feedback | UI / Integration | `tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs` | COVERED |
| cu-008 Dual Focus & Gamepad Navigation | UI | `tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs` | COVERED |
| Combat UI Foundation Event Adapter（Sprint 5 跨多 story） | Integration | `tests/integration/combat-ui/combat_ui_foundation_event_adapter_test.cs` | COVERED |
| Combat UI Resource Bars & Damage Feedback（Sprint 5 跨多 story） | UI | `tests/integration/combat-ui/combat_ui_resource_bars_damage_feedback_test.cs` | COVERED |
| Combat UI Intent Icons & HUD Summary（Sprint 5 跨多 story） | UI | `tests/integration/combat-ui/combat_ui_intent_icons_hud_summary_test.cs` | COVERED |
| S5-Preflight | Process | n/a | EXPECTED（process task，无自动化） |
| S5-SmokeLog | Process | n/a | EXPECTED（process task，无自动化） |

合计 Sprint 5 closed stories：5 features（cu-004/005/006-foundation/007/008）+ 2 process（S5-Preflight、S5-SmokeLog）。Combat UI 累计 fact 95+，全在 Foundation 1359/1359 PASS 内。

---

## Manual Smoke Checks

### Batch 1 · Core Stability

| Item | Result |
|------|--------|
| Foundation+Godot+harness 启动不崩 / build 能过 | PASS |
| 主要 panel 资源加载（combat UI / panel scenes） | PASS |

### Batch 2 · Sprint Changes

| Item | Result |
|------|--------|
| cu-004 Move Selection Panel 与 Preview Card | PASS |
| cu-005 Counter+Decisive Prompts | PASS |
| cu-006 Decisive Strike Director（Foundation+Godot Integration）+ cu-007 Synergy & Round Warning | PASS |
| cu-008 Dual Focus & Gamepad Navigation | PASS |

### Batch 3 · Data Integrity & Performance & Regression

| Item | Result |
|------|--------|
| Fixture 与资源序列化 | PASS |
| 帧率 / TimeScale | PASS |
| 原 Sprint 5 功能在 cu-006 BUILD / S6-Commit-Lint 后保持 | PASS |

---

## Missing Test Evidence

无强阻塞缺口。以下属 known deferred 项，已在追踪：

- cu-004 / cu-005 / cu-006 / cu-008 视觉素材（截图 / 录屏）— 由 Sprint 6 `cu-visual-evidence` story 收口；4 份 evidence MD 已追加 Visual Captured 模板段（参见 [cu-004 evidence](./evidence/cu-004-move-selection-panel-and-preview-card-evidence.md) / [cu-005 evidence](./evidence/cu-005-counter-and-decisive-action-prompts-evidence.md) / [cu-006 evidence](./evidence/cu-006-decisive-strike-animation-director-evidence.md) / [cu-008 evidence](./evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md)）；待 designer 录屏后升级至 Visual Captured。
- cu-008 真实手柄硬件 walkthrough — 走 `cu-008-Gamepad-HW-Verify` story（Sprint 6 Nice to Have），不阻塞本 sign-off。
- BUG-0002 / BUG-0003 — 已判为 stale target（旧 `fengzhi-vertical-slice`），由 [qa-signoff-sprint-5-2026-06-16.md](./qa-signoff-sprint-5-2026-06-16.md) Conditions 条款记录处理路径。

---

## Verdict

**PASS**

判定依据：
- Automated PASS（Foundation 1359/1359 + Godot smoke 2/2 + cu006 integration 6/6）
- Manual Batch 1 / 2 / 3 全部 PASS（无 PASS WITH NOTES，无 FAIL）
- 无 MISSING coverage（known deferred 视觉素材已显式追踪到 cu-visual-evidence story 与 Sprint 6 Nice to Have 的硬件 walkthrough）
- 与 06-16 smoke 的差异：本次 Godot binary 实跑（不再 NOT RUN），扩大覆盖至 cu-006 Foundation slice + cu-007，故无 PASS WITH WARNINGS 触发条件

---

## Closure

- 满足 [qa-plan-sprint-6](./qa-plan-sprint-6-2026-06-18.md) §S6-Sprint5-DoD 第 1 条（`smoke-2026-06-XX-sprint-5.md` 存在；verdict ∈ {PASS, PASS WITH WARNINGS}）
- 下一步：`/team-qa sprint 5` 生成 `qa-signoff-sprint-5-2026-06-22.md`，进而勾选 sprint-5.md DoD 三条 + retro Action Item #2
