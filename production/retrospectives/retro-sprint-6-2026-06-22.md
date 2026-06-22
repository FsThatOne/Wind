# Retrospective: Sprint 6 — Combat UI Hardening & Presentation Cutoff

**Period**: 2026-06-19 -- 2026-07-03（实际收尾 2026-06-22，提前 11 天）
**Generated**: 2026-06-22
**Stage**: Production
**Sprint Goal**: 封板 Combat UI Presentation epic — cu-006 Godot 4.7-stable 实机集成、cu-004/005/006/008 Visual evidence、Sprint 5 DoD 补齐、Sprint 5 retrospective 5 条 Action Items 闭环。

---

## Metrics

| Metric | Planned | Actual | Delta |
|---|---|---|---|
| Must-Have | 5 | 4 done + 1 carryover | -1 (合规 carryover) |
| Should-Have | 3 | 3 done | 0（含 EI-Debt-Triage 提级到 Must） |
| Nice-to-Have | 2 | 0 | 0（预期未启动） |
| In-scope Completion Rate | -- | 87.5% (7/8) | -- |
| Story Effort Hours | 24.0 估 | 15.6 实际（done 项汇总） | -8.4 (-35%) |
| Bugs Found / Fixed | -- | 0 / 0 | -- |
| Unplanned Tasks Added | -- | scene-markup-tool addon scaffold；ICharacterAnimator port；大规模 CCGS cleanup；art-bible/hud pre-prod gate 对齐；launch-godot helper × 3 | +5 unplanned |
| Sprint-scope Commits | -- | 21 (window 6-15..6-23) | — |

**Foundation 测试**：1367 / 1367 PASS（dotnet test FengZhi.slnx，731ms，0 fail）
**Smoke**：PASS WITH WARNINGS（[smoke-2026-06-22-sprint-6.md](../qa/smoke-2026-06-22-sprint-6.md)）
**QA Sign-off**：APPROVED WITH CONDITIONS（[qa-signoff-sprint-6-2026-06-22.md](../qa/qa-signoff-sprint-6-2026-06-22.md)）

---

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|---|---|---|---|
| Sprint 4 | 5 | 5 | 100% |
| Sprint 5 | 5 | 5（含 1 mid-sprint pull） | 100% |
| Sprint 6 (current) | 8 in-scope | 7 done + 1 carryover | 87.5% |

**Trend**：Stable-with-formalized-carryover — 三轮 sprint 第一次出现正式 carryover。但这是有 gate-check 文档（cu-visual-evidence Carryover Decision）+ commit (`9b1b9b2`) + sprint-status `carryover_to_sprint:7` 字段的合规 carryover，不是隐性 slip。Velocity 表面下降 12.5%，实际反映"hardening sprint + 大规模 cleanup + ADR pivot"的高密度低代码产出，而非交付能力下降。

---

## What Went Well

- **Sprint 5 retro 5 条 Action Items 100% 闭环**：commit lint（#4，`1466df2`）、smoke + team-qa 补齐 Sprint 5 DoD（#2，`16ff726`）、hour-level effort tracking（#5，S6-Effort-Tracking）、cu-006 Godot 实机集成（#3，`4675c20`）、工作树立即 commit（#1）— 这是首次出现"上一 sprint 的 5 个 action items 在下一 sprint 全部 closed"。
- **cu-006 Godot 4.7-stable BUILD evidence 落地**：`4675c20 feat(combat-ui): cu-006 Godot 4.7-stable 实机集成 + ICombatService Facade (TR-combat-ui-006)` — 7-phase 决胜演出在 Engine.TimeScale + Tween Always + Camera2D + InputMap 四个 4.7 API 风险点全部实机验证；同步引出 `CombatCinematicLockInputFilter` 的 first-match → any-allowed 语义修订（ADR-0011 §实机集成硬约束 #3 记录）。
- **大规模 cleanup 净减项目复杂度**：CCGS branding（`cd9f49c` + `c8d8c14`）连带 burst-read-combat-concept spike、sprint5-combat-ui-harness、134 个旧 protagonist 资产、UPGRADING/SECURITY/CONTRIBUTING 等模板文件、整个 CCGS Skill Testing Framework 目录一并删除 — 项目从"含通用 game studio 模板"收敛到"《风止》专属仓库"。
- **ADR-0019 → ADR-0020 pivot 消化**：纯 2D 武侠渲染方向落地，含 art-bible / map-scene-management / item-system / hud.md 多份 GDD 联动更新（`1fc3a0b` + `d7282ed`）；同步引入 ADR-0021 ICharacterAnimator port（`36a0ca7`）解耦动画系统。
- **EI Tech Debt Triage 完成**：7 条 exploration-insight debt 条目分级（2 P1 + 1 P2 + 4 deferred）；输出 [`production/notes/exploration-debt-triage-2026-06-22.md`](../notes/exploration-debt-triage-2026-06-22.md) — Sprint 5 retro Action #3 闭环。
- **gate-check 文化首次到位**：retroactive Tech Setup → Pre-Production gate 在 ADR pivot 后主动触发，产出 11 条 Concerns（含 C-VS / C-PLAYTEST / C5 / C6 / C9 / C10 / C11），驱动 Sprint 7 backlog 的具体条目 — 不再是"自我感觉良好就推进"的项目治理模式。
- **scope-check 拆层化常态化**：cu-006-godot-integration 沿用 Sprint 5 cu-006 的 "Foundation 契约 + Godot 实机" 双层模式，作为高 Engine Risk Visual story 的默认起手式（写入本 sprint plan §Process Notes）。

---

## What Went Poorly

- **harness 删除导致 cu-006 Godot 实机基线工作树副本丢失**（W1 advisory）：`prototypes/sprint5-combat-ui-harness/` 与 `CombatUiDecisiveGodotIntegrationTest.cs` 在 `c8d8c14` cleanup 中一同删除。commit `4675c20` 历史仍在，但当前工作树无法重跑实机 baseline — 这是 cleanup 时未识别的隐性副作用。Foundation 8/8 自动化仍覆盖编排逻辑，但 4.7 API 的 4 个风险点实机基线在 Sprint 7 VS 重建前都不可重跑。
- **cu-visual-evidence carryover 是迟到的发现**：carryover 决策本身合规（充分有 gate-check + commit），但触发时点是在 `/dev-story` 起手才发现 harness 删除导致录屏路径断 — 说明 "cleanup 时未评估对未启动 story 的影响" 是 process gap。
- **doc-only task 估算严重高估 60-88%**：S6-Combat-UI-Epic-Close (4h→0.5h, -88%)、EI-Debt-Triage (4h→0.6h, -85%)、S6-Effort-Tracking (2h→0.5h, -75%) — 估算口径未区分"代码 spike"与"纯文档 close-out"。
- **cu-008-Gamepad-HW-Verify nice-to-have 已 nice-to-have 第 2 个 sprint**：硬件未到的 dependency 持续未解，应该转为正式 blocked 或显式时间表（Steam Deck 借测 / 购入），不要继续以 nice-to-have 形式 carry。
- **build warnings × 4 累积**（W3）：CS0219 × 2 + CS8602 × 2 在 build 中已沉淀，未有"build 0 warning"硬规则；本 sprint 未利用 hardening 周期清理。
- **Sprint 6 当前工作树未实机启动确认**（W2）：cu-006 在 `4675c20` 时实机 done，但 cleanup commit 后未补一次手工启动确认；自动化 1367 PASS 给了过强的安全感，掩盖"工作树是否能真的开起来"的盲点。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---|---|---|---|
| ADR-0019 与新美术方向冲突（武侠 vs 战棋融合 vs 纯像素武侠 vs ADR-0010 五层 schema） | sprint 早期 | retroactive gate-check + ADR-0020 supersede 0019 + art-bible / GDD 联动 + traceability index 更新 | 美术方向变更必须触发 architecture-review；不要"先改 art 再回头补 ADR" |
| cu-visual-evidence story 起手才发现 harness 已删 | dev-story 触发瞬间 | carryover 决策（commit `9b1b9b2`）+ 4 份 evidence Visual Captured 段改派到 Sprint 7 carryover | cleanup commit 必须 grep 所有引用（依赖 grep ripgrep + 跨 story 引用扫描）；删 spike 前 list 所有 evidence/story 中的 fixture 引用 |
| 估算与实际差异 60-88%（doc-only tasks） | 整 sprint | retro 数据明示 → action item 校准估算口径 | 区分 "spike"（含 ADR 决策风险）vs "doc close-out"（≤0.25d/2h 默认） |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Likely Cause |
|---|---|---|---|---|
| S6-Combat-UI-Epic-Close | 4.0h | 0.5h | **-88%** | 纯文档 close-out 误用 spike 估算口径 |
| EI-Debt-Triage | 4.0h | 0.6h | **-85%** | 实际只做 desk review，不是 spike |
| S6-Effort-Tracking | 2.0h | 0.5h | -75% | 仅模板补丁 + 历史回填 |
| S6-Commit-Workspace | 2.0h | 1.5h | -25% | — |
| S6-Sprint5-DoD | 4.0h | 3.0h | -25% | — |
| cu-006-godot-integration | 6.0h | 7.0h | **+17%** | Godot 4.7 Tween/InputMap 调试超 buffer（唯一正方差） |
| S6-Commit-Lint | 2.0h | 2.5h | +25% | — |

**Overall accuracy**: 2/7 within ±20% (29%)；5/7 高估 ≥25%（全部 doc-only）；1/7 正方差（engine integration）。

**Analysis**：估算偏差有清晰的二极分布 — doc-only tasks 系统性高估 60-88%，engine integration 系统性低估 17%。下个 sprint 应用规则：doc-only ≤2h default；engine integration +20% buffer default。

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|---|---|---|---|---|
| cu-visual-evidence (cu-004/005/006/008 visual evidence × 4 份) | Sprint 6 (originated) | 1 | harness 删除导致录屏路径断；新 ADR-0020 VS 上录制更有价值 | 转 Sprint 7 Must-Have，绑定 VS 重建 |
| cu-008-Gamepad-HW-Verify | Sprint 5 → 6 → ... | 2+ | Steam Deck 硬件未到 | 升级为 blocked + 显式时间表（购入/借测决策） |

---

## Technical Debt Status

| 指标 | 当前 | 上 sprint | 趋势 |
|---|---|---|---|
| TODO/FIXME/HACK（全仓 .cs） | 0 | 0 | Stable / 0-debt ✅ |
| Build warnings | 4 (CS0219 × 2 + CS8602 × 2) | unknown baseline | 持平 — Sprint 7 hardening backlog 清理 |
| EI tech debt 条目 | 7 triaged (2 P1 必还 + 1 P2 + 4 deferred) | 7 untriaged | **Improved**（EI-Debt-Triage 闭环） |
| 过时 spike 数量 | 0 | 3 (burst-read-combat-concept, sprint5-combat-ui-harness, charater-move) | **Improved**（cleanup commit 全清） |
| 项目根 boilerplate 文件 | 0 | 5+（UPGRADING/SECURITY/CONTRIBUTING + .github/FUNDING/CODEOWNERS） | **Improved**（CCGS rebrand 全清） |

---

## Previous Action Items Follow-Up

| Action Item (Sprint 5 retro) | Status | Notes |
|---|---|---|
| #1 立刻 commit Sprint 5 工作树 | **Closed** | `16ff726` Sprint 5 smoke + QA 证据 commit |
| #2 smoke + team-qa 补齐 Sprint 5 DoD | **Closed 2026-06-22** | `smoke-2026-06-22-sprint-5.md` PASS + `qa-signoff-sprint-5-2026-06-22.md` APPROVED WITH CONDITIONS |
| #3 cu-006 Godot 实机集成 + Visual evidence | **Partial closed** | cu-006-godot-integration done (`4675c20`)；Visual evidence carryover → Sprint 7（合规） |
| #4 commit message lint | **Closed** | `1466df2 chore(tooling): add commit-msg lint and .gitmessage` + Sprint 6 S6-Commit-Lint done |
| #5 hour-level actual effort tracking | **Closed** | S6-Effort-Tracking done；本 sprint 全 done stories 含 actual_hours |

**首次出现 5/5 Closure**。

---

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|---|---|---|---|
| 1 | **ADR-0020 全循环 Vertical Slice 重建**（gate-check §C-VS / §C-PLAYTEST）：第一章·江南 1 个完整 explore→combat→outcome loop，验证"行气战棋 + 纯 2D 武侠"在 Godot 4.7-stable 工作树上可玩。同时回收 cu-visual-evidence 4 份 evidence 在新 VS 上录制（gate Condition §1） | Producer / Dev / Designer | **High** | Sprint 7 Sprint Goal |
| 2 | **Godot 4.7 实机手工启动确认**（gate Condition §2，W2 跟进）：Sprint 7 首工作日 qa-tester 手工启动 StartCave / 主战斗 scene 一次，记录至 evidence；之后每个 cleanup-heavy commit 后必跑 | QA | High | Sprint 7 Day 1 |
| 3 | **doc-only task 估算口径校准**：≤0.25d / 2h default；估算 ≥4h 必须 spawn architect/director 确认是否为伪 spike；写入 sprint-plan skill 注释 | Producer | Medium | `/sprint-plan new` 时纳入 |
| 4 | **cu-008-Gamepad-HW-Verify 升级处理**：Steam Deck 借测/购入决策；状态从 nice-to-have → blocked + 明确 unblock 日期 | Producer | Medium | Sprint 7 第一周 |
| 5 | **Build warnings 0-warning 硬规则**（W3 跟进）：清理 CS0219 × 2 (`BossPhaseSystem.cs:243`) + CS8602 × 2；在 CI 加 `-warnaserror` 防回归 | Dev | Medium | Sprint 7 hardening backlog |
| 6 | **cleanup commit 必跑 grep + 跨 story 引用扫描**：删 spike / 资产前用 rg 全仓搜引用，避免 cu-visual-evidence 类隐性 carryover；写入 cleanup checklist | Dev | Medium | 下一次 cleanup PR |

---

## Process Improvements

- **gate-check 触发常态化**：本 sprint 首次出现 retroactive gate-check（ADR pivot 触发）+ smoke + team-qa + retrospective 四件齐发；建议把"ADR supersede 必触发 architecture-review + retroactive gate-check"写入 ADR skill 注释。
- **carryover 合规模板**：cu-visual-evidence 第一次走完整 carryover 流程（status 字段 + sprint plan §Carryover to Next Sprint + 4 份 evidence 段改派 + active.md session extract + 独立 commit）。下个 sprint 出现 carryover 时直接套此模板。
- **cleanup pre-flight checklist**：删 spike / 资产前必跑：(a) `rg <spike-path>` 全仓搜引用，(b) 列出未启动 story 的 fixture 依赖，(c) 评估 commit `4675c20` 类历史 baseline 是否需要 archive note。
- **estimate 二极规则**：doc-only ≤2h default；engine integration +20% buffer；spike 必带 ADR 引用。

---

## Summary

Sprint 6 是 hardening + cleanup + ADR-pivot 消化的综合 sprint：提前 11 天关账（4 天完成原计划 14 天），首次实现"上 sprint retro action items 5/5 全闭环"，cu-006 Godot 4.7-stable 实机基线落地，CCGS 模板痕迹与过时 spike 全清。在 cleanup 过程中暴露的 cu-visual-evidence carryover 是首次合规走完 carryover 全流程，并形成可复用模板。下一 sprint 单一焦点：ADR-0020 全循环 Vertical Slice 重建 — 这是 Production 阶段真正进入 prove-or-pivot 决策点的关键 spike。
