# Sprint 9 — EI Epic Close + System Polish

> **Period**: 2026-06-30 ~ 2026-07-06
> **Goal**: 关闭 Exploration-Insight Epic + 系统质量 Polish + Playtest 硬性执行
> **Calibration**: Code 0.3x / Config+Doc 固定 0.5h / Greenfield 0.5x

---

## Must Have (Critical Path)

| ID | Task | Type | Owner | Est. | Confidence | Dependencies | Status |
|----|------|------|-------|------|-----------|-------------|--------|
| S9-001 | EI Epic 正式关闭 — EPIC.md Status→Done + DoD 逐项验收 | Doc | Dev | 0.5h | high | None | Not Started |
| S9-002 | Playtest Session (硬性 deadline Day 3) | Playtest | User | 4h | medium | S9-001 | Not Started |
| S9-003 | EI 系统集成走查 — 全链路冒烟 + regression 补充 | Integration | Dev | 1h | high | S9-001 | Not Started |
| S9-004 | cu-008 正式 descope 至 Polish phase | Doc | Producer | 0.5h | high | None | Not Started |

## Should Have

| ID | Task | Type | Owner | Est. | Confidence | Dependencies | Status |
|----|------|------|-------|------|-----------|-------------|--------|
| S9-005 | EI 测试覆盖度审查 — GDD AC 逐项对照 test traceability | QA | Dev | 1h | high | S9-001 | Not Started |
| S9-006 | InsightNode 瞬态恢复 stress test — 高频场景切换不泄漏 | Logic | Dev | 1h | medium | S9-003 | Not Started |
| S9-007 | EI + Dialogue lockMode 交互验证 — 战斗/对话期间 cue 暂停 | Integration | Dev | 1h | medium | S9-003 | Not Started |
| S9-008 | Sprint 9 retrospective | Doc | Dev | 0.5h | high | All | Not Started |

## Nice to Have (Cut First)

| ID | Task | Type | Owner | Est. | Confidence | Dependencies | Status |
|----|------|------|-------|------|-----------|-------------|--------|
| S9-009 | EI 视觉 Polish — InsightCueVisual 动画微调 + 颜色主题 | Visual/Feel | Dev | 2h | low | S9-002 反馈 | Not Started |
| S9-010 | 序章 pc-001 主线节点骨架（Sprint 10 准备） | Config/Data | Dev | 2h | medium | None | Not Started |

---

## Capacity

| Resource | Available Days | Notes |
|----------|---------------|-------|
| Dev (Agent) | 7d | 全量可用 |
| User | 部分 | Playtest + 方向决策 |

**总预估工时**: Must-Have 6h + Should-Have 3.5h + Nice-to-Have 4h = 13.5h max
**校准后预期**: Must-Have ~3h + Should-Have ~2h = ~5h 实际

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Playtest 再次拖延 | Medium | High (prove-or-pivot 持续悬挂) | Day 3 硬性 deadline + 内部同事备选 + 若 Day 3 未完成则标记 PROVE(tech-only) 继续 |
| EI DoD 验收发现缺口 | Low | Medium | 6 test files 已覆盖核心路径；缺口小修即可 |
| 场景切换 stress test 暴露泄漏 | Low | Medium | 已有 scene-lock 测试基础，增量修复 |

---

## Carry-over Actions (from Sprint 8 retro)

| # | Action | Status | Sprint 9 Mapping |
|---|--------|--------|-----------------|
| 1 | 估时校准制度化 | ✅ Done (S8) | — |
| 2 | Playtest 硬性执行 | ❌ 未完成 | → S9-002 (Day 3 deadline) |
| 3 | Build warnings 清零 + CI | ✅ Done (S8) | — |
| 4 | Godot headless 文档化 | Deferred | 推迟至有 CI headless 需求时 |
| 5 | cu-008 正式 descope | ❌ 未完成 | → S9-004 |

---

## Definition of Done (Sprint Level)

Sprint 9 视为完成当：
- EI EPIC.md Status = Done，DoD 全部验收通过
- Playtest 报告已提交 OR 标记 PROVE(tech-only) 并记入 retro
- cu-008 正式移入 Polish backlog
- 无 P0/P1 regression
- Retrospective 已写入 `production/retrospectives/`

---

## EI Epic DoD 验收清单 (S9-001 细化)

| # | Definition of Done Item | Evidence | Verdict |
|---|------------------------|----------|---------|
| 1 | All stories implemented, reviewed, and closed | ei-001~009 Status: Complete | TBD |
| 2 | All AC from `design/gdd/exploration-insight.md` verified | Test traceability (S9-005) | TBD |
| 3 | Detection, prerequisite, reward dispatch, transient reset, save restore have tests | 6 test files in tests/ | TBD |
| 4 | Scene cleanup prevents stale nodes or leaked prompts | insight_save_scene_lock_test (S9-006) | TBD |
| 5 | Exploration discovery does not create HUD radar/quest-marker style UI | Code review — InsightCueVisual is subtle ink cue only | TBD |
