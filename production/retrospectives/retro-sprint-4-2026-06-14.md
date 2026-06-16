# Retrospective: Sprint 4 — Exploration Insight

**Period**: 2026-06-14 -- 2026-06-28
**Generated**: 2026-06-14
**Stage**: Production
**Sprint Goal**: 实现场景 InsightNode 的发现、追查、奖励分派、存档恢复和锁定恢复管线，让洞察属性在世界探索中形成可测试的主动发现闭环。

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Must Have Tasks | 5 | 5 | 0 |
| Completion Rate | -- | 100% | -- |
| Story Effort Days | 5.5 estimate-days | 5.5 estimate-days delivered | 0 |
| Bugs Found | -- | 0 | -- |
| Bugs Fixed | -- | 0 | -- |
| Unplanned Tasks Added | -- | 3 QA artifacts plus tech-debt updates | +3 |
| Commits | -- | 4 exploration-scope commits since 2026-06-14 | -- |

QA result:

- Smoke Check: `production/qa/smoke-2026-06-14-sprint-4.md` — PASS WITH WARNINGS
- Team QA: `production/qa/qa-signoff-sprint-4-2026-06-14.md` — APPROVED WITH CONDITIONS
- Automated suite: `dotnet test` passed 1295/1295
- Exploration suite: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration` passed 51/51
- Sprint 4 targeted suite: `InsightNodeRegistryTest|InsightDetectionThresholdTest|InsightCueTimingTest|InsightRewardDispatchTest|InsightSaveSceneLockTest` passed 50/50

---

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|--------|---------|-----------|------|
| Sprint 2 | Unknown | Unknown | Unknown |
| Sprint 3 | 8 | 8 | 100% |
| Sprint 4 | 5 | 5 | 100% |

**Trend**: Stable at 100% for the two sprints with structured retrospective data.

Sprint 4 delivered fewer stories than Sprint 3, but the story scope was deeper integration work: save/restore, event dispatch, deterministic timing, and lock recovery.

---

## What Went Well

- The Sprint 4 dependency graph held cleanly: `ei-001` unlocked detection, `ei-003` supplied deterministic cue timing, `ei-004` supplied reward dispatch, and `ei-005` closed save/scene/lock recovery.
- All 5 Must Have stories reached `Complete` / `done` with exact automated evidence paths in `tests/unit/exploration/` or `tests/integration/exploration/`.
- The C# Foundation suite stayed healthy through the sprint, ending at 1295/1295 passing tests.
- The biggest Sprint 3 process action item was improved: Sprint 4 documented `dotnet test` as the canonical C# Feature verification runner in the QA plan and smoke report.
- Critical atomicity risks were caught early: reward dispatch was refactored after code review to avoid partial reward/state/event success.
- Lock recovery received a useful late regression fix: ignored cues no longer retrigger immediately after a combat/dialogue lock releases.

---

## What Went Poorly

- Godot headless smoke still had environment friction: default `user://logs` rotation crashed before project code ran unless an explicit `--log-file` path was provided.
- Several story-readiness documentation sections were filled through implementation discipline rather than being present in the story files before implementation, especially for `ei-005`.
- Technical debt grew during the sprint, mostly around Exploration edge contracts: `Detect()` vs `Tick()` semantics, condition evaluator binding, hide event idempotency, and presentation-side monologue request timing.
- Sprint artifacts and implementation files remain partially uncommitted/untracked in the local working tree, making clean audit and handoff harder than the test results suggest.
- Presentation-layer validation remains deferred. That is correct scope control for Sprint 4, but it means the next UI/Presentation sprint must not infer final player-facing feel from this sprint's logic tests.

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| GdUnit4 / runner ambiguity from Sprint 3 | Carried into Sprint 4 planning | QA plan made `dotnet test` canonical for C# Feature stories | Keep `dotnet test` as canonical unless GdUnit4 is installed and wired into CI |
| Godot headless default log path crash | Short during smoke check | Retried with `--log-file /tmp/fengzhi-godot-smoke.log` and passed | Add explicit `--log-file` to local smoke command and MCP troubleshooting notes |
| Reward dispatch side-effect atomicity | During `ei-004` code review | Refactored port commit contracts and added downstream failure tests | Continue using preflight/commit-style ports for multi-boundary side effects |
| Lock pause behavior for ignored nodes | During `ei-005` code review | Restricted lock pause reset to visible `DETECTED` cues and added regression test | Add edge-case tests for every transient state before marking lock/lifecycle stories done |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| ei-001 | 0.75 days | Completed | Within estimate | Narrow model/registry scope and pure C# tests |
| ei-002 | 1.0 day | Completed | Within estimate | Detection contract was clear and bounded by active node cap |
| ei-003 | 1.0 day | Completed | Within estimate | Deterministic tick design avoided timing flakiness |
| ei-004 | 1.25 days | Completed with review fix | Slightly higher risk, within scope | Cross-boundary atomicity was more subtle than the initial happy path |
| ei-005 | 1.5 days | Completed with review fix | Within estimate | Save/scene/lock integration was broad but well covered by tests |

**Overall estimation accuracy**: 5/5 Must Have tasks completed within planned sprint scope.

Actual hour-level variance is still not tracked consistently across all stories, so this remains estimate-day accuracy rather than timesheet accuracy.

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| QA-Preflight: canonical local smoke runner | Sprint 3 | 1 | GdUnit4 missing while C# validation uses `dotnet test` | Completed for Sprint 4 as QA plan/smoke convention; keep tracking Godot `--log-file` environment note |
| EI-Reward-Ext deferred reward adapters | Sprint 4 Nice to Have | 0 | Nice-to-have was intentionally not pulled in after Must Have completion | Re-evaluate only when Loot, MartialFragment, or SideQuestEntry rewards enter scope |
| Presentation cue rendering validation | Sprint 4 out of scope | 0 | Sprint 4 was logic/integration only | Schedule in later UI/Presentation sprint with manual evidence |

---

## Technical Debt Status

- Current TODO/FIXME/HACK count: 28 total occurrences across docs/examples/skill framework files; no new Sprint 4 source-code TODO cluster was identified.
- Current tech-debt register entries: 9 total, including 7 dated 2026-06-14 and 5 directly tied to Exploration Insight.
- Trend: Growing but mostly intentional and documented.
- Concern: Exploration debt is concentrated around boundary contracts rather than implementation gaps: condition evaluator binding, `Detect()` legacy semantics, hide event idempotency, monologue side-effect timing, and story documentation freshness.

---

## Previous Action Items Follow-Up

| Action Item (from Sprint 3) | Status | Notes |
|-----------------------------|--------|-------|
| Decide and document canonical local smoke runner | Done with condition | Sprint 4 QA plan and smoke report use `dotnet test`; Godot smoke still needs explicit `--log-file`. |
| Ensure pulled-in stories are added to sprint status before implementation | Done for Sprint 4 | `production/sprint-status.yaml` tracked all 5 Must Have Exploration stories through completion. |
| Keep WIP limited to one active story unless explicitly replanning | Done | Stories were closed sequentially through readiness, dev, review, and done gates. |
| Record actual effort or elapsed implementation time per story | Partial | Later stories record effort better than earlier ones; still not consistent enough for precise estimation. |

---

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | Update local smoke convention to always pass explicit `--log-file` for Godot headless runs, or configure the default `user://logs` path so it no longer crashes. | Dev/QA | High | Before next `/smoke-check sprint` |
| 2 | Before the next integration-heavy story, add readiness sections for Estimate, Out of Scope, Control Manifest Rules, Engine Notes, and Performance Notes before `/dev-story`. | Producer/Dev | High | Before next story implementation |
| 3 | Triage Exploration tech debt and choose which boundary-contract item must be paid down before Presentation integration. | Dev/QA | High | Before UI/Presentation sprint planning |
| 4 | Record actual effort consistently in every story completion note, not just later sprint stories. | Producer/Dev | Medium | During next sprint |
| 5 | Commit or intentionally shelve Sprint 4 artifacts before starting the next sprint to preserve a clean audit baseline. | Dev | Medium | Before next `/sprint-plan new` |

---

## Process Improvements

- Treat smoke runner configuration as a first-class sprint entry criterion, not a QA cleanup item at the end of the sprint.
- Keep the story-readiness gate strict even when the implementation path is clear; missing design metadata should be fixed before code starts, not logged afterward.
- Continue requiring code review before `/story-done`; both `ei-004` and `ei-005` found real edge-case issues that tests alone did not initially cover.

---

## Summary

Sprint 4 was a strong delivery sprint: the Exploration Insight logic pipeline moved from registry to detection, cue timing, reward dispatch, save/restore, scene cleanup, and lock recovery with full automated coverage and QA approval. The main improvement for the next iteration is process hygiene rather than gameplay logic: lock down the Godot smoke command, keep story readiness complete before coding, and pay down Exploration boundary-contract debt before Presentation integration begins.
