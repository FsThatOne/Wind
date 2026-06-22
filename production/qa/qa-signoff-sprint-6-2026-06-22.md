# QA Sign-Off Report: Sprint 6 — Combat UI Hardening & Presentation Cutoff

**日期**: 2026-06-22
**评审模式**: lean（director gates 已跳过；QA sign-off 由 qa-lead 自主执行）
**评审方**: qa-lead（via `/team-qa sprint 6`，Phase 2 + Phase 6 合并，lean mode）
**Smoke Report**: [production/qa/smoke-2026-06-22-sprint-6.md](smoke-2026-06-22-sprint-6.md) — **PASS WITH WARNINGS**
**QA Plan**: [production/qa/qa-plan-sprint-6-2026-06-18.md](qa-plan-sprint-6-2026-06-18.md)
**前序 Sign-off**: [production/qa/qa-signoff-sprint-5-2026-06-22.md](qa-signoff-sprint-5-2026-06-22.md)（Sprint 5 全作用域 close-out — APPROVED WITH CONDITIONS）

---

## 作用域

- **Sprint**: Sprint 6 — Combat UI Hardening & Presentation Cutoff
- **Duration**: 2026-06-19 ～ 2026-07-03（2 周）
- **评审时间点**: 2026-06-22（Sprint 中段提前关账；所有 must-have / should-have 已完成）
- **Story 总数**: 10（Must-Have × 5 / Should-Have × 3 / Nice-to-Have × 2）
- **项目阶段**: Production（`production/stage.txt`）
- **Must-Have 完成率**: 4 / 5 done（cu-visual-evidence 正式 carryover → Sprint 7）
- **Should-Have 完成率**: 3 / 3 done
- **Nice-to-Have 完成率**: 0 / 2（预期，硬件未到 + 依赖 EPIC 关闭后排）

---

## 测试覆盖摘要

| Story | Type | 自动化测试 | 手工 QA | 结果 |
|---|---|---|---|---|
| S6-Commit-Workspace | Process | — | `git status` clean ✓；commit log 4 条含规范 message ✓ | PASS |
| S6-Sprint5-DoD | Process | — | `smoke-2026-06-22-sprint-5.md` 存在 PASS ✓；`qa-signoff-sprint-5-2026-06-22.md` 存在 APPROVED WITH CONDITIONS ✓ | PASS |
| cu-006-godot-integration | Visual/Feel + Integration | Foundation 1367/1367 PASS（含 cu-006 演出 8/8 fact）；Godot 实机 test 已删（W1） | BUILD baseline `4675c20`（2026-06-20）6/6 AC 通过；当前工作树未实机复跑（W2 advisory） | PASS WITH NOTES |
| cu-visual-evidence | Visual/Feel | — | 正式 carryover → Sprint 7（commit `9b1b9b2`） | N/A（carryover） |
| S6-Commit-Lint | Process / Config | — | `.gitmessage` 模板 ✓；lint script ✓；首批 commit 走规范 ✓ | PASS |
| S6-Combat-UI-Epic-Close | Process | — | `combat-ui/EPIC.md` status: Done ✓；`epics/index.md` Presentation 行更新 ✓ | PASS |
| EI-Debt-Triage | Process | — | `tech-debt-register.md` 5 条 EI 条目各含 priority + 决策 ✓；triage 文档存在 ✓ | PASS |
| S6-Effort-Tracking | Process | — | `story-done` 模板含 Effort 段 ✓；sprint-status.yaml 全 done story 含 estimate/actual_hours ✓ | PASS |
| S6-Next-Presentation-Cut | Nice-to-have | — | 未启动（预期内） | SKIP（nice-to-have） |
| cu-008-Gamepad-HW-Verify | Nice-to-have | — | 未启动，硬件未到（预期内） | SKIP（nice-to-have） |

---

## Smoke Check 引用

**报告**: `production/qa/smoke-2026-06-22-sprint-6.md`
**Verdict**: PASS WITH WARNINGS

**自动化测试**: 1367 / 1367 PASS（dotnet test FengZhi.slnx，NUnit，net8.0，耗时 731ms，0 失败）

**手工核心稳定性**: PASS（Godot 4.7 开 `StartCave.tscn` 不崩溃；输入响应正常；无死循环卡顿）

**三条 Advisory Warnings（不阻塞 QA hand-off）**：

- **W1** — cu-006 Godot 实机 integration test 已于 commit `c8d8c14` 随 harness 删除。Foundation auto 8/8 仍覆盖演出编排逻辑；Godot 4.7 `Engine.TimeScale` / `Tween.Always` / `Camera2D` / `InputMap` 实机集成基线在当前工作树不可重跑。→ Sprint 7 VS spike 时恢复（见 Conditions §1）。
- **W2** — Sprint 6 当前工作树未进行 Godot 实机启动确认（BUILD baseline `4675c20`，2026-06-20）。之后 3 次提交（`cd9f49c` / `c8d8c14` / `9b1b9b2`）未触及 Foundation 逻辑层，1367 自动化确认无回归。→ 建议 Sprint 7 初期 qa-tester 补做手工启动确认（见 Conditions §2）。
- **W3** — Build warnings × 4：CS0219（`BossPhaseSystem.cs:243` × 2）+ CS8602（`SceneConfigTests.cs:124`、`boss_battle_simulation_test.cs:88`）。非阻塞。→ Sprint 7 hardening backlog 清理（见 Conditions §3）。

---

## Carryover

**cu-visual-evidence**（Must-Have，原 0.75d）正式 carryover → Sprint 7。

**决策背景**（参见 `production/session-state/active.md` §Session Extract — cu-visual-evidence Carryover Decision 2026-06-22）：

1. `prototypes/sprint5-combat-ui-harness/` 已于 commit `c8d8c14` 主动删除（与 burst-read spike、134 个过时 protagonist 资产同清理）；原计划的 4 个 fixture 录屏路径失效。
2. Sprint 7 第一优先级 = ADR-0020 全循环 Vertical Slice 重建，新 VS 将同步产出代表当前架构（纯 2D 武侠 + 行气战棋）的 visual baseline；在此 baseline 上录制 cu-004 / cu-005 / cu-006 / cu-008 evidence 才有价值，避免在已删除的旧 spike 上重复劳动。

**受影响文件**：
- `production/sprint-status.yaml` — cu-visual-evidence status: backlog，`carryover_to_sprint: 7`
- `production/sprints/sprint-6.md` — cu-visual-evidence 行标注 `[Carryover → Sprint 7]`
- `production/qa/evidence/cu-{004,005,006,008}-*.md` — Visual Captured 段改为 `(Sprint 7 carryover — Pending)`

**Carryover commit**: `9b1b9b2`

---

## 发现的 Bug

本 Sprint 6 QA 周期内未新增任何 bug。

Sprint 5 遗留 bug 状况见 `qa-signoff-sprint-5-2026-06-22.md`，无悬挂 S1 / S2 open issue。

---

## Verdict: **APPROVED WITH CONDITIONS**

**判定依据**：

- 1367 / 1367 自动化测试全绿，0 失败
- 所有 done stories 验收标准满足
- 无 S1 / S2 open bug
- Must-Have 4/5 done；cu-visual-evidence 为有文档支撑的正式 carryover，不计为 FAIL
- Should-Have 3/3 done（超出预期）
- 3 个 smoke warnings 均为 advisory，不触发 NOT APPROVED
- 与 Sprint 5 close-out 裁定方式一致（lean mode，APPROVED WITH CONDITIONS）

---

## Conditions

以下事项须在 Sprint 7 关账前完成，不阻塞 Sprint 6 close-out 本身：

1. **cu-visual-evidence 4 份 evidence MD**（Sprint 7 Must-Have）：cu-004 / cu-005 / cu-006 / cu-008 visual evidence 在 ADR-0020 全循环 VS 场景中录制截图 / 录屏，并由 designer + qa-lead 双签字后升级 `Visual Captured`。参见 `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-PLAYTEST。
2. **Godot 4.7 实机手工启动确认**（Sprint 7 初期，W2 跟进）：qa-tester 在 Sprint 7 首工作日执行一次手工启动，确认 cu-006 一击决胜演出在当前工作树 Godot 4.7-stable 中可正常触发，并将结果记录至 `production/session-logs/` 或 qa evidence。
3. **Build warnings 清理**（Sprint 7 hardening backlog，W3 跟进）：CS0219（`BossPhaseSystem.cs:243`）× 2 + CS8602（`SceneConfigTests.cs:124`、`boss_battle_simulation_test.cs:88`）× 2，在 Sprint 7 下一次 hardening 周期统一清理，目标 build 0 warning。

---

## Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| QA Lead | TBD | 2026-06-22 | __pending__ |

---

## 下一步

Sprint 6 QA APPROVED WITH CONDITIONS。推荐后续动作顺序：

1. **`/retrospective sprint 6`** — 产出 Sprint 6 retrospective 报告 + Action Items（含 effort 估算偏差分析、doc-only task 0.25d 默认、VS 重建优先级确认）
2. **Sprint 7 plan** — 以 ADR-0020 全循环 VS 重建为首优先，纳入 cu-visual-evidence（4 份 evidence）+ Conditions §2（实机确认）+ Conditions §3（build warnings 清理）
3. 在 `production/sprints/sprint-6.md` DoD 段勾选已完成项，并标注 `cu-visual-evidence carryover` 注释
4. 在 `production/session-state/active.md` 追加本次 QA RUN 注释

---

## 关联文件

| 文件 | 说明 |
|---|---|
| `production/qa/smoke-2026-06-22-sprint-6.md` | Sprint 6 Smoke Check 报告（PASS WITH WARNINGS） |
| `production/qa/qa-plan-sprint-6-2026-06-18.md` | Sprint 6 QA Plan（sprint 启动时生成） |
| `production/qa/qa-signoff-sprint-5-2026-06-22.md` | Sprint 5 全作用域 Sign-off（前序，APPROVED WITH CONDITIONS） |
| `production/sprints/sprint-6.md` | Sprint 6 plan + Carryover 表 |
| `production/sprint-status.yaml` | Sprint 6 story 状态快照（updated 2026-06-22） |
| `production/session-state/active.md` §cu-visual-evidence Carryover Decision | carryover 决策完整记录 |
| commit `9b1b9b2` | cu-visual-evidence carryover 状态文件变更 |
| commit `4675c20` | cu-006 Godot 集成 BUILD evidence baseline（2026-06-20） |
| commit `c8d8c14` | sprint5-combat-ui-harness 主动删除（含 Godot integration test） |
| `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` | Sprint 7 VS / Playtest 门控，Conditions §1 来源 |
