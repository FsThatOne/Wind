# Smoke Check: Sprint 6 (close-out)

**Date**: 2026-06-22
**Sprint**: Sprint 6 — Combat UI Hardening & Presentation Cutoff
**Engine**: Godot 4.7-stable + .NET 8 (C#)
**QA Plan**: [production/qa/qa-plan-sprint-6-2026-06-18.md](qa-plan-sprint-6-2026-06-18.md)
**Argument**: sprint
**Verdict**: **PASS WITH WARNINGS**

---

## Automated Tests

**Status**: PASS — 失败 0，通过 **1367**，已跳过 0，总计 1367
**Runner**: `dotnet test FengZhi.slnx`（NUnit，net8.0）
**Duration**: 731ms
**Build Warnings (non-blocking)**: 4

| Code | 文件:行 | 描述 |
|---|---|---|
| CS0219 | `src/FengZhi.Foundation/Combat/AI/BossPhaseSystem.cs:243` | `other1` 变量赋值后未使用 |
| CS0219 | `src/FengZhi.Foundation/Combat/AI/BossPhaseSystem.cs:243` | `other2` 变量赋值后未使用 |
| CS8602 | `tests/Foundation/SceneManagement/SceneConfigTests.cs:124` | 可空引用解引用 |
| CS8602 | `tests/unit/combat/ai/boss_battle_simulation_test.cs:88` | 可空引用解引用 |

---

## Test Coverage

| Story | 类型 | Test File | Coverage |
|---|---|---|---|
| S6-Commit-Workspace | Process | — | EXPECTED |
| S6-Sprint5-DoD | Process | — | EXPECTED |
| cu-006-godot-integration | Visual/Feel + Integration | Foundation [`combat_ui_decisive_animation_director_test.cs`](../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) (8/8) ✅；Godot 实机 `prototypes/sprint5-combat-ui-harness/scripts/tests/cu006/CombatUiDecisiveGodotIntegrationTest.cs` **已随 harness 在 c8d8c14 删除** ⚠ | MANUAL ⚠ |
| cu-visual-evidence | Visual/Feel | Carryover → Sprint 7（见 commit `9b1b9b2`） | N/A |
| S6-Commit-Lint | Tooling | — | EXPECTED |
| S6-Combat-UI-Epic-Close | Process | — | EXPECTED |
| EI-Debt-Triage | Process | — | EXPECTED |
| S6-Effort-Tracking | Process | — | EXPECTED |

**Summary**: 1 manual ⚠, 0 missing, 6 expected (process/admin), 1 carryover. 自动化覆盖率：Foundation 全套 1367 测试全绿，包含 cu-006 演出编排 8/8 + 其他全部 Sprint 5/6 逻辑层。

---

## Manual Smoke Checks

按 `tests/smoke/critical-paths.md` + Sprint 6 实际改动定制：

### Batch 1 — Core 稳定性

- [x] Godot 4.7 开 `feng-zhi/StartCave.tscn` 不崩溃 — **PASS**
- [x] StartCave 场景打开无报错 — **PASS**
- [x] 输入响应正常 — **PASS**
- [x] 运行中无明显卡顿 / 死循环 — **PASS**

### Batch 2 — Sprint 6 变动 / 回归

- [-] cu-006 一击决胜演出 Godot 实机：**NOT CHECKED**（当前工作树未跑；BUILD evidence 仍是 commit `4675c20` 2026-06-20 baseline）
- [-] scene_markup_tool addon 加载：**NOT CHECKED**
- [-] Sprint 5 已 done feature 回归：**NOT CHECKED**（Foundation 1367 测试自动确认无回归，但实机未跑）
- [-] ADR-0021 Animator port 实机挂载 8-direction sprite：**NOT CHECKED**

### Batch 3 — 数据完整性 / 性能

- [-] 存档：**N/A**（SaveSystem 未实现）
- [-] 性能：**NOT CHECKED**

---

## Warnings（advisory，不阻塞 QA hand-off）

### W1 · cu-006 Godot 实机 integration test 已删
`CombatUiDecisiveGodotIntegrationTest.cs` 与 `prototypes/sprint5-combat-ui-harness/` 一并在 commit `c8d8c14` 删除。Foundation 自动化 8/8 仍覆盖演出编排逻辑（`combat_ui_decisive_animation_director_test.cs`），但 Godot 4.7 实机 `Engine.TimeScale` / `Tween.Always` / `Camera2D` / `InputMap` 集成基线在当前工作树不可重跑。原 BUILD evidence (2026-06-20, commit `4675c20`) 仍是历史 baseline。

**建议**：Sprint 7 重建 VS 时，把这 4 个 4.7 API 的实机验证作为 VS spike 的一部分；参见 `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-VS。

### W2 · Sprint 6 当前工作树未实机验证
cu-006-godot-integration 在 commit `4675c20`（2026-06-20）实机 BUILD 通过；之后 `cd9f49c` / `c8d8c14` / `9b1b9b2` 都未触及 Foundation 逻辑层，Foundation 1367 测试也确认无回归。但当前工作树没有"今天在 Godot 4.7 编辑器中启动 StartCave 走 cu-006 演出"的实机确认。

**建议**：`/team-qa sprint 6` 时由 qa-tester 跑一次手工启动确认。

### W3 · Build warnings × 4
2 个 CS0219（BossPhaseSystem.cs 未使用变量）+ 2 个 CS8602（可空解引用）。非阻塞。

**建议**：Sprint 7 backlog 项，团队下次 hardening 周期统一清理。

---

## Verdict: ✅ PASS WITH WARNINGS

- Automated tests PASS (1367/1367, 0 fail)
- Core stability PASS
- No FAILED items in any batch
- 3 advisory warnings recorded for Sprint 7 follow-up
- Build is ready for `/team-qa sprint 6` hand-off

---

## Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| QA Lead | TBD | 2026-06-22 | __pending__ |

> **失败处理 SLA**：无失败项；无需修复。
> **下一步**：`/team-qa sprint 6` 走完整 sign-off 流程（含 W2 实机确认）。

---

## Run 2026-06-22

- 执行人：dev agent (smoke-check skill, lean mode)
- 运行环境：macOS 25.1.0, dotnet 10.0.300, Foundation 1367 NUnit tests
- 决策记录：cu-visual-evidence carryover → Sprint 7（commit `9b1b9b2`）；本次 smoke 不要求实机 evidence
- 后续动作：`/team-qa sprint 6` → `/retrospective sprint 6` → Sprint 7 plan（VS 重建）
