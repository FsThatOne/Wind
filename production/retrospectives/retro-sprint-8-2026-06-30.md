# Sprint 8 Retrospective

> **Sprint**: 8 — Exploration-Insight Presentation 集成
> **Period**: 2026-06-29 ~ 2026-06-30 (实际执行 2 天)
> **Goal**: EI Presentation 层集成 + 偿还 tech debt + 补做 VS Playtest + 架构文档收尾
> **Verdict**: **Goal Substantially Met** — EI 全链路 Presentation 集成完成，Playtest 待执行

---

## Metrics

| Metric | Value |
|--------|-------|
| Stories Planned (Must-Have) | 8 |
| Stories Completed (Must-Have) | 7/8 (87.5%) |
| Stories Planned (Should-Have) | 3 |
| Stories Completed (Should-Have) | 3/3 (100%) |
| Stories Planned (Nice-to-Have) | 1 |
| Stories Completed (Nice-to-Have) | 0/1 (blocked) |
| Total Dev-Owned Completed | 10/11 (90.9%) |
| Remaining | 1 user-owned (Playtest) + 1 blocked (HW) |
| Build Warnings | 0 ✅ (14→0) |
| Tests Passing | 1582 |
| Epics Closed | 1 (audio-system) |

---

## Velocity Trend

| Sprint 6 | Sprint 7 | Sprint 8 |
|----------|----------|----------|
| 10 stories / 11h actual | 13 stories / 28.5h actual | 10 stories / 14.5h actual |

Trend: **Stable-High** — 产出密度持续良好（stories/hour 持续提升）。

---

## Estimation Accuracy

| Story | Est. (h) | Actual (h) | Variance | Category |
|-------|----------|-----------|----------|----------|
| S8-EI-Debt-Paydown | 16.0 | 2.5 | **-84%** | Foundation 集成 |
| S8-EI-Presentation-Adapter | 16.0 | 6.0 | **-63%** | Foundation 集成 |
| S8-EI-Presentation-Interaction | 12.0 | 3.5 | **-71%** | Foundation 集成 |
| S8-EI-Scene-Content | 8.0 | 0.5 | **-94%** | Config/Data |
| S8-Build-Warnings-Zero | 2.0 | 0.5 | -75% | Fix |
| S8-CI-Godot-Version | 1.0 | 0.0 | -100% | 已完成 |
| S8-Audio-Epic-Close | 1.0 | 0.25 | -75% | Doc-only |
| S8-Architecture-Md-Refresh | 4.0 | 0.25 | -94% | Doc-only |
| S8-Control-Manifest-Refresh | 2.0 | 0.5 | -75% | Doc-only |
| S8-Estimate-Calibration | 1.0 | 0.25 | -75% | Doc-only |
| **TOTAL** | **63.0** | **14.5** | **-77%** | |

### Calibration History

| Sprint | Factor Used | Actual Factor | Trend |
|--------|-----------|---------------|-------|
| S6 | 1.0x (未校准) | ~0.40x | 首次观察 |
| S7 | 1.0x (未校准) | 0.38x | 确认 |
| S8 | 0.4x (已应用) | **0.23x** | 偏差持续 — 需进一步下调？ |

**Analysis**: Sprint 8 即使应用了 0.4x 系数（预期 63×0.4=25.2h），实际仍仅 14.5h（0.23x）。根因：
1. **Config/Data + Doc-only 类型需要独立估时模型** — 6 个此类 story 合计估 16h 实际仅 2h
2. **Foundation 集成类 0.4x 基本合理** — EI 三个 code stories 估 44h×0.4=17.6h，实际 12h（0.27x），偏差缩小
3. **建议**: Sprint 9 分类估时：Code stories 用 0.3x；Config/Doc stories 固定 0.5h

---

## What Went Well

- **EI 全链路 Presentation 完整落地**: 从 tech debt 偿还 → adapter 桥接 → 交互追查 → 内容配置，4 个 story 一气呵成
- **音频系统 Epic 正式关闭**: 8/8 stories Complete，`systems-index` 标记 Implemented
- **架构文档大幅更新**: ADR Registry 全量（24 条）+ Control Manifest 覆盖 ADR-0020~0024
- **Build warnings 彻底清零 + CI 硬门控**: 14 warnings → 0 + `-warnaserror` 永不回归
- **估时校准制度化**: 模板已固化 0.4x 规则 + retro 追踪机制

---

## What Went Poorly

- **Playtest Session 仍未执行**: 连续 2 个 sprint carry-over（S7→S8→S9?），prove-or-pivot 终判仍未闭合
- **cu-008 手柄验证连续 4 sprint blocked**: Sprint 5→6→7→8，硬件购入决策持续搁置
- **估时模型仍不收敛**: 即使应用 0.4x 仍高估 77%，说明估时基数本身过大（尤其 doc/config 类）

---

## Blockers

| Blocker | Frequency | Resolution Time | Prevention |
|---------|-----------|----------------|-----------|
| 外部玩家招募 | 2 sprints | 未解决 | 明确 deadline + 备选方案（内部同事） |
| Steam Deck 硬件 | 4 sprints | 未解决 | 做 descope 决策或购入 |

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Action |
|------|----------------|---------------|--------|
| Playtest Session | S7 → S8 → S9 | 2 | ⚠️ 再次 carry — 建议 S9 设 hard deadline Day 3 |
| cu-008-Gamepad-HW-Verify | S5 → S6 → S7 → S8 → ? | 4 | 🔴 正式 descope 至 Polish phase |

---

## Technical Debt Status

| 指标 | Sprint 8 | Sprint 7 | 趋势 |
|------|----------|----------|------|
| TODO/FIXME/HACK (.cs) | 1 | 1 | 持平 |
| Build warnings | **0** | ≥4 | ✅ 清零 |
| EI tech debt | 0 P1/P2 remaining | 2 P1 + 1 P2 | ✅ 全部偿还 |
| jiangnan-iso 方向 | paused (ADR-0024) | paused | 持平 |
| Audio Epic | **Done** | In Progress | ✅ 关闭 |

---

## Previous Action Items Follow-Up

| Action Item (Sprint 7 retro) | Status | Notes |
|-------------------------------|--------|-------|
| #1 估时校准系数制度化 | **Done** ✅ | sprint-plan 模板 + retro 模板均已更新 |
| #2 Playtest session 补做 | **Not Done** ❌ | 再次未执行 — carry 至 S9 |
| #3 Build warnings 清零 + CI -warnaserror | **Done** ✅ | 14 warnings → 0 + CI gate |
| #4 Godot headless 路径文档化 | Not Started | 未排入 Sprint 8 |

---

## Action Items for Sprint 9

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | **Playtest 硬性 deadline**: Day 3 前必须完成试玩（内部同事也可），否则标记 prove-or-pivot = PROVE(tech-only) 并继续 | User | **Critical** | S9 Day 3 |
| 2 | **cu-008 正式 descope**: 将手柄验证移至 Polish phase backlog，不再 carry | Producer | High | S9 Day 1 |
| 3 | **估时模型分类**: Code stories 0.3x / Config+Doc stories 固定 0.5h / Greenfield stories 0.5x | Producer | Medium | S9 planning |
| 4 | **EI Epic 关闭评审**: ei-001~009 全 Complete → EPIC.md Status: Done + 全链路 playtest 验证 | Dev+QA | Medium | S9 Week 1 |
| 5 | **Sprint 9 方向决策**: 下一步重点 — 战斗 Polish / 新章节内容 / Tileset 重设计 | User | High | S9 Day 1 |

---

## Sprint 8 Summary

Sprint 8 以极高效率（2 天 14.5h）完成了 EI Presentation 全链路集成、音频 Epic 关闭、架构文档全量更新和估时制度化。技术产出超预期，但流程性事项（Playtest、硬件购入）持续拖延。Sprint 9 最关键的决策是明确下一阶段方向（新内容 vs 系统 Polish vs Tileset 重设计），以及设定 Playtest 的硬性截止日。
