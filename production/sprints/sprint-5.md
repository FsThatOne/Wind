# Sprint 5 — Combat UI Interaction & Navigation

> **Sprint Goal**: Complete the Combat UI player-decision loop by implementing the move selection panel, counter/decisive prompts, dual-focus navigation, and the QA/tooling fixes needed for reliable Presentation sprint validation.
>
> **Duration**: 2026-06-14 ~ 2026-06-28 (2 weeks)
>
> **Prerequisite**: Sprint 4 QA sign-off is APPROVED WITH CONDITIONS; Sprint 5 must resolve stable Combat UI TR-IDs and Godot headless `--log-file` smoke convention before implementation proceeds.

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for QA/tooling fixes, manual evidence, and Godot UI risk
- Available: 8 estimate-days
- Planned story load: 5.0 estimate-days

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S5-Preflight | Stabilize Sprint 5 Combat UI readiness metadata for `cu-004` through `cu-008` | Producer/Dev | 0.5 | Sprint 4 retrospective | `TR-combat-ui-???` placeholders are replaced with active registry IDs; readiness metadata includes Estimate, Out of Scope, Control Manifest rules, Engine Notes, Performance Notes, and test evidence paths |
| S5-SmokeLog | Document and apply Godot headless smoke `--log-file` convention | Dev/QA | 0.5 | Sprint 4 smoke condition | Local smoke commands and Sprint 5 QA plan use an explicit writable `--log-file` path and no longer hit default `user://logs` crash |
| cu-004 | 招式选择面板与预览卡 | godot-csharp-specialist | 1.0 | cu-001, cu-002, S5-Preflight | Player action panel shows 6 equipped moves and base actions; unavailable actions show reasons; preview card shows relationship/counter hint; no damage prediction; default focus lands on the first available move |
| cu-005 | 反制与决胜行动提示 | godot-csharp-specialist | 0.75 | cu-003, cu-004 | Counter label reflects public intent + selected move + neixi; `isCounter=true` is submitted only as UI intent; Decisive Strike row appears for stagger >=5 and refreshes on target/resource changes; UI does not execute settlement |
| cu-008 | 双焦点与手柄导航 | godot-csharp-specialist | 0.75 | cu-004, cu-005 | D-pad cycles through all interactive actions; confirm submits focused action; focus never escapes the panel; mouse hover and gamepad focus can coexist without visual conflict |

### Should Have

> **Scope decision (2026-06-18, `/scope-check combat-ui`)**: `cu-006` 完整实现延期到下一个 Sprint；`cu-007` 在 Sprint 5 剩余窗口启动。

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria | Sprint 5 Decision |
|----|------|-------------|-----------|--------------|---------------------|-------------------|
| cu-006 | 一击决胜演出编排 | godot-csharp-specialist | 1.0 | cu-005 | Decisive Strike plays the 7-phase sequence; TimeScale slows to 0.2 and restores to 1.0; Camera request and cinematic input lock recover correctly after conflicts | **Defer to next sprint** — Engine Risk HIGH（TimeScale + Tween + Camera + cinematic lock）；Sprint 5 隐性 buffer 已被 harness/合同漂移修复/GDD rerun 消耗；保留可选 0.5d Spike，仅验证 Godot 4.7-stable TimeScale + Tween 自锁与 Camera 归位风险，不算 Done |
| cu-007 | 协同与回合警戒反馈 | godot-csharp-specialist | 0.5 | cu-003 | Synergy feedback appears only for valid same-target cooperative counters; turn counter displays warning colors at turns 12 and 14 without hurting HUD readability | **Start now** — Engine Risk 偏低；依赖 `cu-001` / `cu-003` 已 Complete；不动 TimeScale / Camera |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| EI-Debt-Triage | Triage Exploration Insight boundary-contract debt before Presentation integration | Dev/QA | 0.5 | Sprint 4 retrospective | Decide whether `Detect()` legacy semantics, condition evaluator binding, hide event idempotency, or monologue request timing must be paid down before UI/Presentation integration |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| Godot headless `--log-file` convention | Sprint 4 smoke passed only after explicit writable log path; default `user://logs` rotation crashed before project code ran | 0.5 days |
| Combat UI TR/readiness stabilization | `cu-004` through `cu-008` currently contain `TR-combat-ui-???` placeholders and need readiness metadata before `/dev-story` | 0.5 days |
| Presentation manual evidence discipline | Sprint 4 was logic/integration only; Sprint 5 includes UI/Visual stories that require evidence docs | Included in buffer |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| `cu-004` through `cu-008` fail `/story-readiness` due to unstable TR-IDs | High | High | Complete S5-Preflight before implementation; update `docs/architecture/tr-registry.yaml` and story headers first |
| Godot 4.6 dual-focus behavior differs from pure C# assumptions | Medium | High | Keep `cu-008` as an explicit Must Have; require manual evidence and/or UI automation |
| TimeScale + Tween + Camera behavior in `cu-006` becomes high-risk | Medium | High | Keep `cu-006` as Should Have unless `cu-004`/`cu-005` land early and Godot evidence is available |
| UI starts computing combat outcomes or leaking damage prediction | Medium | High | Enforce Presentation-only DTOs and code review against GDD; tests must assert no damage prediction fields |
| Sprint carries too many Visual/Feel items without manual QA capacity | Medium | Medium | Preserve 20% buffer and make evidence docs required before `/story-done` |

## Dependencies on External Factors

- Godot executable remains available at `/Applications/Godot.app/Contents/MacOS/Godot`.
- Headless smoke uses an explicit writable `--log-file` path.
- Combat Core exposes stable intent, resource, stagger, decisive, and selection intent contracts.
- MartialArts query contracts provide equipped move details without forcing UI to calculate outcomes.
- Manual evidence can be captured for UI/Visual stories before sign-off.

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-5-*.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] UI/Visual stories have evidence docs in `production/qa/evidence/`
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged
- [ ] Godot headless smoke command includes explicit `--log-file`
- [ ] Actual effort or elapsed implementation time is recorded in story completion notes

## QA Plan

QA Plan: `production/qa/qa-plan-sprint-5-2026-06-14.md`. Run `S5-Preflight` before starting `cu-004`, then use the QA plan's automated paths and evidence requirements as required story completion evidence.

## Scope Check

If this sprint includes stories added beyond the original `combat-ui` epic scope, run `/scope-check combat-ui` before implementation begins.
