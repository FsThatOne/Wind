# QA Sign-Off Report: Sprint 4 — Exploration Insight

**Date**: 2026-06-14
**Stage**: Production
**Review Mode**: lean
**Scope**: 5 stories across `exploration-insight`
**QA Plan**: `production/qa/qa-plan-sprint-4-2026-06-14.md`
**Smoke Check**: `production/qa/smoke-2026-06-14-sprint-4.md`

---

## Entry Criteria

- [x] Smoke check report exists and passed with warnings.
- [x] Build is stable under canonical Sprint 4 automated runner (`dotnet test`).
- [x] Godot headless launch succeeds when using explicit `--log-file`.
- [x] All Sprint 4 Must Have stories are `Complete` / `done`.
- [x] All Logic and Integration stories have automated test evidence.
- [x] No Sprint 4 QA blocker or open S1/S2 bug was found.

---

## Test Coverage Summary

| Story | Type | Auto Test | Manual QA | Result |
|-------|------|-----------|-----------|--------|
| ei-001 — InsightNode 数据模型、注册表与场景激活 | Logic | PASS — `tests/unit/exploration/insight_node_registry_test.cs` | N/A | PASS |
| ei-002 — 洞察距离检测、门槛检定与重访发现 | Logic | PASS — `tests/unit/exploration/insight_detection_threshold_test.cs` | N/A | PASS |
| ei-003 — 多节点 stagger、忽略与 linger 恢复 | Logic | PASS — `tests/unit/exploration/insight_cue_timing_test.cs` | N/A | PASS |
| ei-004 — 发现奖励分派：Clue 与 CodePhrase | Integration | PASS — `tests/integration/exploration/insight_reward_dispatch_test.cs` | Smoke only | PASS |
| ei-005 — 存档恢复、场景卸载清理与战斗/对话锁恢复 | Integration | PASS — `tests/integration/exploration/insight_save_scene_lock_test.cs` | Smoke only | PASS |

---

## Automated Evidence

- `dotnet test`: PASS — 1295 passed, 0 failed, 0 skipped.
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration`: PASS — 51 passed, 0 failed, 0 skipped.
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightNodeRegistryTest|InsightDetectionThresholdTest|InsightCueTimingTest|InsightRewardDispatchTest|InsightSaveSceneLockTest"`: PASS — 50 passed, 0 failed, 0 skipped.
- Diagnostics: clean.

---

## Manual QA Scope

Sprint 4 does not require standalone manual story QA before sign-off. The sprint is logic/integration heavy and is covered by automated tests plus smoke verification.

Smoke observations:

- [x] Godot project launches in headless mode when using explicit `--log-file`.
- [x] Exploration filtered tests pass.
- [x] Save/load exploration state tests pass.
- [x] Lock pause/resume tests pass.
- [x] No Sprint 4 regression was reported during smoke confirmation.
- [-] Windowed main menu, gamepad traversal, and final Presentation cue rendering are out of scope for Sprint 4.

---

## Bugs Found

| ID | Story | Severity | Status |
|----|-------|----------|--------|
| None | — | — | — |

---

## Conditions

- Headless Godot launch in this environment should use an explicit writable `--log-file` path; default `user://logs` rotation can crash before project code runs.
- Existing compiler/nullability warnings remain outside Sprint 4 Exploration Insight scope.
- Presentation-layer visual cue rendering is not part of Sprint 4 and should be validated in a later UI/Presentation sprint.

---

## Verdict: APPROVED WITH CONDITIONS

All Sprint 4 stories pass their automated evidence requirements, smoke check passed with warnings, and no S1/S2 bugs are open.

The build is approved for hand-off with the conditions listed above. Resolve or track those conditions before using this sprint as a clean release-quality baseline.

---

## Next Step

Run `/retrospective` to capture Sprint 4 lessons, then use `/gate-check` only after confirming conditions are accepted or tracked.
