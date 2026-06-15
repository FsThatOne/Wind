# Smoke Check Report

**Date**: 2026-06-14
**Sprint**: Sprint 4 — Exploration Insight
**Engine**: Godot 4.6.3 mono
**QA Plan**: `production/qa/qa-plan-sprint-4-2026-06-14.md`
**Argument**: sprint

---

## Environment

- Test directory: found at `tests/`
- CI configured: yes, `.github/workflows/tests.yml`
- Smoke checklist: found at `tests/smoke/critical-paths.md`
- Godot executable: `/Applications/Godot.app/Contents/MacOS/Godot`
- Godot version: `4.6.3.stable.mono.official.7d41c59c4`
- Canonical Sprint 4 automated runner: `dotnet test`

---

## Automated Tests

**Status**: PASS

- `dotnet test`: PASS — 1295 passed, 0 failed, 0 skipped
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration`: PASS — 51 passed, 0 failed, 0 skipped
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightNodeRegistryTest|InsightDetectionThresholdTest|InsightCueTimingTest|InsightRewardDispatchTest|InsightSaveSceneLockTest"`: PASS — 50 passed, 0 failed, 0 skipped

Warnings observed but non-blocking:

- Existing C# warnings remain in `BossPhaseSystem.cs`, `SceneConfigTests.cs`, and `boss_battle_simulation_test.cs`.
- Shell startup prints `/Users/bytedance/.profile:7: command not found: neval`.

---

## Headless Startup Smoke

**Status**: PASS WITH ENVIRONMENT NOTE

Initial command failed because Godot could not rotate/open the default `user://logs` file and crashed with signal 11:

```bash
/Applications/Godot.app/Contents/MacOS/Godot --headless --path /Users/bytedance/my-game/prototypes/fengzhi-vertical-slice --quit-after 2
```

Retry command passed by directing the log to a writable explicit path:

```bash
/Applications/Godot.app/Contents/MacOS/Godot --headless --path /Users/bytedance/my-game/prototypes/fengzhi-vertical-slice --log-file /tmp/fengzhi-godot-smoke.log --quit-after 2
```

Observed output:

```text
Godot Engine v4.6.3.stable.mono.official.7d41c59c4 - https://godotengine.org
[BossBattle] === 铁冠道人 Boss 战 Prototype ===
[BossBattle] Foundation EnemyBrain 4阶段 AI 已加载
[BossBattle] === 第 1 回合 ===
[BossBattle] Boss决策: Attack 体系:Gang
```

---

## Test Coverage

| Story | Type | Test File | Coverage Status |
|-------|------|-----------|----------------|
| ei-001 — InsightNode 数据模型、注册表与场景激活 | Logic | `tests/unit/exploration/insight_node_registry_test.cs` | COVERED |
| ei-002 — 洞察距离检测、门槛检定与重访发现 | Logic | `tests/unit/exploration/insight_detection_threshold_test.cs` | COVERED |
| ei-003 — 多节点 stagger、忽略与 linger 恢复 | Logic | `tests/unit/exploration/insight_cue_timing_test.cs` | COVERED |
| ei-004 — 发现奖励分派：Clue 与 CodePhrase | Integration | `tests/integration/exploration/insight_reward_dispatch_test.cs` | COVERED |
| ei-005 — 存档恢复、场景卸载清理与战斗/对话锁恢复 | Integration | `tests/integration/exploration/insight_save_scene_lock_test.cs` | COVERED |

**Summary**: 5 covered, 0 manual, 0 missing, 0 expected.

---

## Manual Smoke Checks

- [x] Godot project launches in headless mode without project-level crash — PASS after explicit `--log-file`
- [x] Full Foundation automated suite passes — PASS
- [x] Exploration filtered tests pass — PASS
- [x] Save/load exploration state tests pass — PASS
- [x] Lock pause/resume tests pass — PASS
- [x] No Sprint 4 regression was reported during manual confirmation — PASS
- [x] No save/load failure was reported during manual confirmation — PASS
- [x] No performance failure was reported during manual confirmation — PASS
- [-] Windowed main menu, gamepad traversal, and visual Presentation cue rendering — N/A for Sprint 4 logic/integration smoke; Presentation-layer cue rendering is deferred to a later sprint.

---

## Missing Test Evidence

All Sprint 4 Logic and Integration stories have automated test coverage.

---

## Verdict: PASS WITH WARNINGS

The build is ready for manual QA hand-off.

Warnings to resolve or track:

- Headless Godot launch should use an explicit writable `--log-file` path in this environment to avoid default `user://logs` crash during logger rotation.
- Existing compiler/nullability warnings remain outside Sprint 4 Exploration Insight scope.
- Presentation-layer visual cue rendering is not part of Sprint 4 and remains N/A for this smoke pass.
