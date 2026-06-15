# Retrospective: Sprint 3 — 敌方 AI 决策系统

**Period**: 2026-06-12 -- 2026-06-26
**Generated**: 2026-06-14
**Stage**: Production
**Sprint Goal**: 实现完整的敌方 AI 决策管线，使 AI 可在纯 C# 环境中为任意战斗配置提供符合 GDD 统计分布的决策输出。

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Tasks | 8 | 8 | 0 |
| Completion Rate | -- | 100% | -- |
| Effort Days | 6.0 estimate-days | 6.0 estimate-days delivered | 0 |
| Bugs Found | -- | 0 | -- |
| Bugs Fixed | -- | 0 | -- |
| Unplanned Tasks Added | -- | 2 story closures plus QA artifacts | +2 |
| Commits | -- | 7 since 2026-06-12 | -- |

QA result:

- Smoke Check: `production/qa/smoke-2026-06-14.md` — PASS WITH WARNINGS
- Team QA: `production/qa/qa-signoff-sprint-3-2026-06-14.md` — APPROVED WITH CONDITIONS
- Automated suite: `dotnet test` passed 1214/1214
- Godot visible smoke: PASS, `BossBattle` advanced through 10 turns with `finalErrors` empty

---

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|--------|---------|-----------|------|
| Sprint 1 | Unknown | Unknown | Unknown |
| Sprint 2 | Unknown | Unknown | Unknown |
| Sprint 3 | 8 | 8 | 100% |

**Trend**: Unknown but currently strong. Earlier sprint status data is not present in a structured status file, so only Sprint 3 can be measured reliably.

---

## What Went Well

- The Sprint 3 dependency graph worked: `ai-001` unlocked the state/modifier, signature, meditation, counter-read, and Boss work without visible rework in QA.
- All 8 Sprint 3 stories were completed with automated test evidence in `tests/unit/combat/ai/`.
- The C# Foundation test suite remained healthy after additional romance-system work, ending at 1214/1214 passing tests.
- `mcp_godot` visible smoke validated the vertical-slice BossBattle path beyond pure unit tests.
- QA hand-off produced concrete artifacts: smoke report, QA plan, and sign-off report.

---

## What Went Poorly

- The local smoke workflow initially assumed `godot` on PATH and GdUnit4 availability, but the repository currently relies on `dotnet test` for C# verification.
- Mid-sprint romance-system work completed outside `production/sprint-status.yaml`, making sprint reporting less accurate without manual context.
- QA environment issues consumed extra coordination time even though the product code was passing.
- Existing unrelated working-tree changes made commit/scoping guidance riskier than usual.

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| Godot CLI not initially available on PATH | Short | Used `/Applications/Godot.app/Contents/MacOS/Godot` and `mcp_godot` | Document expected local Godot path and ensure MCP path remains valid |
| GdUnit4 runner missing | Still open | Treated as QA condition; used `dotnet test` as canonical C# evidence | Decide whether to install GdUnit4 or update smoke-check convention to `dotnet test` |
| Sprint status did not include romance work | Still open | Recorded in session state and story completion notes | Replan before pulling non-sprint stories into active work |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| ai-001 | 0.5 days | Completed | Within estimate | Small, isolated foundation model |
| ai-002 | 1 day | Completed | Within estimate | Clear dependency on ai-001 |
| ai-003 | 0.5 days | Completed | Within estimate | Narrow logic scope |
| ai-004 | 0.5 days | Completed | Within estimate | Narrow logic scope |
| ai-005 | 1 day | Completed | Within estimate | Covered by focused unit tests |
| ai-006 | 0.5 days | Completed | Within estimate | Independent target selection logic |
| ai-007 | 1 day | Completed | Within estimate | Boss phase scope remained framework-level |
| ai-008 | 1 day | Completed | Within estimate | Integration tests existed and remained deterministic |

**Overall estimation accuracy**: 8/8 tasks completed within planned sprint scope. Actual hour-level variance is not tracked, so this is based on story completion against estimate-days rather than timesheets.

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| None | Sprint 3 | 0 | All Sprint 3 tasks completed | Close sprint after retro/planning |

---

## Technical Debt Status

- Current TODO count: 6 across non-generated docs/examples
- Current FIXME count: 2 across skill/test framework docs
- Current HACK count: 16 across skill/test framework docs
- Trend: Unknown, no previous retrospective baseline
- Concern: the largest actionable debt is not code TODOs, but QA tooling ambiguity around GdUnit4 vs `dotnet test`.

---

## Previous Action Items Follow-Up

No previous retrospective files were found in `production/retrospectives/`, so there are no prior action items to evaluate.

---

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | Decide and document the canonical local smoke runner: install GdUnit4 or update smoke workflow to `dotnet test` for C# Foundation stories | Dev/QA | High | Before next sprint smoke-check |
| 2 | Ensure pulled-in stories are added to sprint status before implementation, especially cross-epic work like romance-system | Producer/Dev | High | Before starting Sprint 4 stories |
| 3 | Keep WIP limited to one active story unless explicitly replanning the sprint | Dev | Medium | During Sprint 4 |
| 4 | Record actual effort or elapsed implementation time per story to improve future estimation accuracy | Producer/Dev | Medium | During Sprint 4 |

---

## Process Improvements

- Treat `production/sprint-status.yaml` as the source of truth for active sprint scope; update or regenerate it before taking extra stories.
- Add a lightweight QA tooling preflight at sprint start: Godot path, runner availability, and expected automated command.
- Keep story completion evidence tied to exact tests and reports so `/story-done`, `/smoke-check`, and `/team-qa` can reuse artifacts without re-discovery.

---

## Summary

Sprint 3 was successful: all planned enemy AI stories completed, automated coverage was present for every story, and QA approved the sprint with only environment/tooling conditions. The most important process change for Sprint 4 is to make sprint scope and smoke tooling explicit before implementation begins, so ad-hoc cross-epic work and runner ambiguity do not obscure an otherwise healthy delivery flow.
