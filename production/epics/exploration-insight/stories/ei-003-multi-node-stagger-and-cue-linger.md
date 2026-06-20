# Story: ei-003 — 多节点 stagger、忽略与 linger 恢复

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-14
> **Layer**: Feature
> **Type**: Logic
> **Priority**: P1
> **Estimate**: 1.0 days
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §E, §G, §H AC4/AC9
> **TR-ID**: TR-exploration-insight-003

## Context

洞察提示不能像 HUD radar 一样同时刷屏。该 story 实现多节点同时进入范围时的近到远 stagger、玩家忽略和 linger 超时恢复，保证探索提示保留朦胧感且可再次触发。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: N/A — no post-cutoff Godot API is required for this story. Timing and stagger behavior must be implemented through a pure C# tick/manual-clock path so unit tests can advance time deterministically; a future `_PhysicsProcess` adapter may call the same tick API but is not required here.

## Acceptance Criteria

- [ ] 同一场景有多个满足条件的 InsightNode 同时进入 detection_radius 时，必须按距离近到远依次触发。
- [ ] 相邻 cue shown 事件之间必须遵守 `multi_node_stagger` 间隔，默认 1.5s。
- [ ] 玩家主动忽略或 `insight_cue_linger` 超时后，当前节点提示隐藏并转为 `IGNORED`。
- [ ] 玩家离开 detection_radius 后，`DETECTED` / `IGNORED` 节点必须回退为 `UNDISCOVERED`，再次接近可重新提示。
- [ ] Pending trigger 队列不得重复入队同一个节点，也不得在节点变为 INVESTIGATED 后继续触发。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: Exploration cue timing must extend the existing `InsightNodeRegistry` + `ProximityDetector` path from `ei-001` / `ei-002`; do not introduce a parallel detector.
- Required: Multi-node triggering must use a distance-sorted snapshot and stable tie-breaker to avoid per-frame ordering jitter.
- Required: `Detected` / `Ignored` remain transient states; leaving range must reset them to `Undiscovered`, while `Investigated` must never be queued or re-triggered.
- Required: Cue hidden events only mean the current presentation cue was dismissed; they must not mark discovery completion or dispatch rewards.
- Forbidden: Do not create one `Area2D` per `InsightNode`, do not use HUD radar/quest-marker UI, and do not persist `Ignored`.
- Forbidden: Do not use real `Task.Delay` / wall-clock timers in core logic; timing must be deterministic and unit-testable.
- Guardrail: Queue and tick logic must operate only on active scene nodes from `InsightNodeRegistry`; active scene node count follows ADR-0018's budget of no more than 6 nodes.

- Stagger 队列使用距离排序快照，避免每帧重新排序导致顺序抖动。
- 计时逻辑应可用 fake clock / manual tick 测试，不依赖真实 `Task.Delay` 才能稳定单测。
- Cue hidden event 只表示提示收起，不代表节点被永久发现。
- 严禁持久化 `IGNORED`，该状态仅用于当前接近周期。

## Performance Notes

Cue timing is expected to have negligible runtime cost. The detector must process only active scene nodes from `InsightNodeRegistry`, with ADR-0018 limiting each scene to no more than 6 active nodes. Queue de-duplication, distance sorting, and linger checks should remain bounded to this small active set and must avoid file IO, real sleeps, allocations proportional to total world content, or repeated full-scene scans.

## QA Test Cases

- **AC-1**: 多节点按距离 stagger 触发。
  - Given: 3 个节点同时在范围内，距离分别为 1、2、3
  - When: 推进 detector tick 和 stagger clock
  - Then: cue shown 顺序为最近到最远，且相邻触发间隔不小于 `multi_node_stagger`
  - Edge cases: 距离相等时稳定排序、队列中节点变为 INVESTIGATED
- **AC-2**: linger 超时自动隐藏。
  - Given: 一个节点已 DETECTED
  - When: 推进时间超过 `insight_cue_linger`
  - Then: 节点转为 IGNORED，并发布 cue hidden 事件
  - Edge cases: 超时前玩家已追查、超时前玩家离开范围
- **AC-3**: 离开后再次接近可重新提示。
  - Given: 节点当前为 IGNORED
  - When: 玩家离开 detection_radius 后再次接近
  - Then: 状态先回退 UNDISCOVERED，再可重新进入 DETECTED
  - Edge cases: DETECTED 离开、IGNORED 离开、INVESTIGATED 离开

## Test Evidence

**Required evidence**:
- `tests/unit/exploration/insight_cue_timing_test.cs`

**Status**: [x] Created — 9 tests passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightCueTimingTest|InsightDetectionThresholdTest"`

## Dependencies

- Depends on: ei-002
- Unlocks: ei-005

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 5/5 passing
**Deviations**:
- Advisory: `Detect()` retains the `ei-002` immediate detection path and can set `Detected` without registering linger tracking in `_detectedElapsedSeconds`; use `Tick()` as the cue-linger path until the legacy API is reconciled.
- Advisory: An `Ignored` node leaving range may publish another `InsightCueHiddenEvent`; presentation adapters should treat hide events as idempotent until event de-duplication is tightened.
- Advisory: Pending queue cleanup for inactive scene nodes is deferred to `ei-005`; current processing skips stale entries when dequeued.
**Test Evidence**: Logic unit test at `tests/unit/exploration/insight_cue_timing_test.cs`; target command `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightCueTimingTest|InsightDetectionThresholdTest"` passed 19/19.
**Code Review**: Complete — approved with suggestions; advisory deviations logged as tech debt.
