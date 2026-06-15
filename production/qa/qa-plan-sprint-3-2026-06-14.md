# QA Plan: Sprint 3 — 敌方 AI 决策系统

**Date**: 2026-06-14
**Stage**: Production
**Review Mode**: lean
**Sprint Goal**: 实现完整的敌方 AI 决策管线，使 AI 可在纯 C# 环境中为任意战斗配置提供符合 GDD 统计分布的决策输出。
**Smoke Check**: `production/qa/smoke-2026-06-14.md` — PASS WITH WARNINGS

---

## Scope

This QA cycle covers Sprint 3 enemy AI Foundation/Logic stories:

- `ai-001` 性格模板与体系选择
- `ai-002` 状态机与综合修正系统
- `ai-003` 招式预兆序列
- `ai-004` 调息决策与选招逻辑
- `ai-005` 反读系统
- `ai-006` 目标选择
- `ai-007` Boss 阶段与特殊机制
- `ai-008` AI 管线集成与验证

Out of scope for this sprint:

- Godot scene, animation, or combat UI integration beyond a headless startup smoke check.
- Concrete Boss content tuning beyond framework and simulation tests.
- Tutorial-mode AI restrictions.

---

## Story Classification

| Story | Type | Automated Required | Manual Required | Blocker? |
|-------|------|--------------------|-----------------|----------|
| ai-001 性格模板与体系选择 | Foundation/Logic | Yes | No | No |
| ai-002 状态机与综合修正系统 | Logic | Yes | No | No |
| ai-003 招式预兆序列 | Logic | Yes | No | No |
| ai-004 调息决策与选招逻辑 | Logic | Yes | No | No |
| ai-005 反读系统 | Logic | Yes | No | No |
| ai-006 目标选择 | Logic | Yes | No | No |
| ai-007 Boss 阶段与特殊机制 | Logic | Yes | No | No |
| ai-008 AI 管线集成与验证 | Integration/Logic | Yes | No | No |

---

## Automated Test Requirements

| Story | Required Evidence | Status |
|-------|-------------------|--------|
| ai-001 | `tests/unit/combat/ai/personality_template_type_selection_test.cs` | Present |
| ai-002 | `tests/unit/combat/ai/ai_state_machine_modifiers_test.cs` | Present |
| ai-003 | `tests/unit/combat/ai/signature_pattern_test.cs` | Present |
| ai-004 | `tests/unit/combat/ai/meditation_move_selection_test.cs` | Present |
| ai-005 | `tests/unit/combat/ai/counter_read_system_test.cs` | Present |
| ai-006 | `tests/unit/combat/ai/target_selector_test.cs` | Present |
| ai-007 | `tests/unit/combat/ai/boss_phase_system_test.cs` | Present |
| ai-008 | `tests/unit/combat/ai/enemy_brain_integration_test.cs`; `tests/unit/combat/ai/boss_battle_simulation_test.cs` | Present |

Automated verification:

- `dotnet test` — PASS, 1214/1214.
- Godot headless startup — PASS, `BossBattle` scene loads and emits an EnemyBrain decision.
- Godot/GdUnit4 runner — NOT RUN, because `addons/gdunit4/GdUnitRunner.gd` is missing.

---

## Manual QA Scope

Manual QA is advisory for this sprint because all Sprint 3 stories are pure Foundation/Logic and have automated evidence.

Run manual checks only to resolve smoke warnings:

1. Launch `prototypes/fengzhi-vertical-slice` in a visible Godot window.
2. Confirm the BossBattle prototype reaches the first AI decision without crash or hang.
3. Confirm keyboard/mouse input responds in the visible window.
4. Confirm gamepad navigation/confirm input works if a controller is available.
5. Observe a short live session for obvious frame hitches or log spam.

Manual QA is not required to validate AI probability math; automated tests are the source of truth for those criteria.

---

## Entry Criteria

- Smoke check report exists: `production/qa/smoke-2026-06-14.md`.
- Smoke verdict is PASS or PASS WITH WARNINGS.
- Build has no automated test failures.
- All Sprint 3 Must Have stories are `done` in `production/sprint-status.yaml`.
- Automated test evidence exists for all Sprint 3 Logic/Foundation stories.

---

## Exit Criteria

- All automated test evidence remains PASS.
- No S1/S2 QA bugs are open against Sprint 3 enemy AI.
- Manual smoke warnings are either resolved or explicitly accepted as warnings.
- QA sign-off report is written to `production/qa/qa-signoff-sprint-3-2026-06-14.md`.

---

## Risks And Warnings

- Local `godot` command is not on PATH; use `/Applications/Godot_mono.app/Contents/MacOS/Godot`.
- `mcp_godot` expects `/Applications/Godot.app`, but this machine uses `/Applications/Godot_mono.app`.
- GdUnit4 addon is not installed, so the Godot-native runner cannot execute from this checkout.
- Current smoke pass is headless; visible-window UI/input/performance checks are advisory warnings.
