# Sprint 4 — 探索 / 洞察 Feature Sprint

> **Sprint Goal**: 实现场景 InsightNode 的发现、追查、奖励分派、存档恢复和锁定恢复管线，让洞察属性在世界探索中形成可测试的主动发现闭环。
>
> **Duration**: 2026-06-14 ~ 2026-06-28 (2 weeks)
>
> **Prerequisite**: Sprint 3 QA sign-off is APPROVED WITH CONDITIONS; Sprint 4 must resolve the smoke runner convention before final smoke-check.

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for QA/tooling fixes and unplanned integration work
- Available: 8 estimate-days
- Planned story load: 5.5 estimate-days

## Sprint Scope

This sprint implements the `exploration-insight` Feature-layer logic and integration contracts. It does not implement final Presentation-layer ink animation assets, independent collection UI, Loot/MartialFragment/SideQuestEntry full reward flows, or new scene content beyond test fixtures.

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| ei-001 | InsightNode 数据模型、注册表与场景激活 | godot-csharp-specialist | 0.75 | None | Data model covers GDD fields; scene load filters active nodes; scene unload resets DETECTED/IGNORED; registry does not expose mutable internals |
| ei-002 | 洞察距离检测、门槛检定与重访发现 | godot-csharp-specialist | 1.0 | ei-001 | Proximity detection uses `player.insight >= threshold`; insufficient insight emits no cue; upgraded insight can revisit and detect |
| ei-003 | 多节点 stagger、忽略与 linger 恢复 | godot-csharp-specialist | 1.0 | ei-002 | Multiple nodes trigger nearest-first with stagger; linger timeout hides cue; leaving radius resets transient states |
| ei-004 | 发现奖励分派：Clue 与 CodePhrase | godot-csharp-specialist | 1.25 | ei-002 | DETECTED-only investigation dispatches InnerMonologue, Narrative quest flag, CodePhrase learn event, INVESTIGATED state, and discovered event atomically |
| ei-005 | 存档恢复、场景卸载清理与战斗/对话锁恢复 | godot-csharp-specialist | 1.5 | ei-001, ei-003, ei-004 | Save captures only INVESTIGATED; restore rejects corrupt states; scene unload clears queues; LockMode >= Partial hides cues and release resumes detection |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| QA-Preflight | Decide and document canonical local smoke runner for C# Feature stories | Dev/QA | 0.5 | None | Sprint QA plan states whether `dotnet test` or GdUnit4 is canonical, and smoke-check command path is unambiguous |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| EI-Reward-Ext | Sketch deferred reward adapters for Loot, MartialFragment and SideQuestEntry | Dev | 0.5 | ei-004 | Adapter boundaries are documented without implementing non-sprint reward behavior |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| Decide canonical smoke runner | Sprint 3 QA condition: GdUnit4 runner missing while C# verification uses `dotnet test` | 0.5 days |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Reward dispatch touches Narrative/CodePhrase ports that differ from expected contracts | Medium | Medium | Keep adapters thin, test with fakes, fail atomically before setting INVESTIGATED |
| Lock pause/resume creates stale pending cues across scene or combat transitions | Medium | High | Centralize queue clearing and cover nested lock/unload paths in ei-005 integration tests |
| QA runner ambiguity repeats from Sprint 3 | Medium | Medium | Run `/qa-plan sprint` before implementation and document canonical automated command |
| Scope creep into ink animation, radar UI, loot rewards, or scene content | Medium | Medium | Keep this sprint logic/integration only; defer Presentation and content work |

## Dependencies on External Factors

- Godot executable remains available at `/Applications/Godot.app/Contents/MacOS/Godot` for MCP smoke checks.
- C# Foundation test project remains runnable through `.NET 8`.
- `ConditionEvaluator`, EventBus, Save ports, Narrative flags and CodePhraseBook contracts are reused through ports/fakes rather than rewritten.

## Definition of Done for this Sprint

- [ ] All Must Have stories completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-4.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged
- [ ] Sprint status is updated before any cross-epic pull-in work
- [ ] WIP is limited to one active story unless the sprint is explicitly replanned
- [ ] Actual effort or elapsed implementation time is recorded in story completion notes

## QA Plan

QA Plan: `production/qa/qa-plan-sprint-4-2026-06-14.md`. Run story readiness before starting `ei-001`, and use the QA plan's test paths as required evidence.

## Scope Check

If this sprint includes stories added beyond the original `exploration-insight` epic scope, run `/scope-check exploration-insight` before implementation begins.
