# Story: ei-002 — 洞察距离检测、门槛检定与重访发现

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-14
> **Layer**: Feature
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1.0 days
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §D, §F, §H AC1/AC2/AC3
> **TR-ID**: TR-exploration-insight-002

## Context

该 story 实现探索洞察的核心门槛规则：玩家进入 detection_radius 后，系统实时评估 prerequisite 和 `player.insight >= node.insight_threshold`。洞察不足时不出现任何提示；洞察提升后重访同一位置可以重新发现。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: N/A — no post-cutoff Godot API is required for the core detection rule. The detection logic should be implemented as a pure C# service that can be called from a future `_PhysicsProcess` adapter; this story must not depend on engine callbacks to remain unit-testable.

## Acceptance Criteria

- [ ] `ProximityDetector` 能基于玩家位置和 active InsightNode 的 `DetectionRadius` 检测进入范围的候选节点。
- [ ] prerequisite 必须通过共享 `ConditionEvaluator` 实时评估，不允许在探索系统内复制条件逻辑。
- [ ] 当 `player.insight >= node.InsightThreshold` 时，节点进入 `DETECTED` 并发布可供 Presentation 层渲染的 cue shown 事件。
- [ ] 当 `player.insight < node.InsightThreshold` 时，节点保持 `UNDISCOVERED`，不得发布提示事件或泄露存在感。
- [ ] 玩家洞察提升后重访曾经失败的节点，必须重新检定并可触发 `DETECTED`。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: Exploration architecture must use `InsightNodeRegistry` + `ProximityDetector` + later `DiscoveryDispatcher`; this story implements the detection and threshold slice only.
- Required: Prerequisites must reuse the shared `ConditionEvaluator` contract from ADR-0014/ADR-0018; do not duplicate condition parsing or evaluation logic inside Exploration.
- Required: Detection must use the pure boolean rule `playerInsight >= InsightThreshold`; failed checks must leave the node `Undiscovered`.
- Required: Cue shown events may include node id, position, narrative tone/context, and presentation-safe metadata only.
- Forbidden: Do not introduce probability, random rolls, HUD radar markers, or per-node `Area2D` instances.
- Forbidden: Do not expose `InsightThreshold`, `playerInsight`, hidden prerequisite state, or failure reason to Presentation.
- Guardrail: Detection operates only over active scene nodes from `InsightNodeRegistry`; scene active node count follows ADR-0018 budget of no more than 6 nodes.

- 检定为纯布尔规则，严禁引入概率或随机源。
- 距离检测使用集中式 `ProximityDetector._PhysicsProcess` 或等价纯逻辑服务，不为每个 InsightNode 创建独立 Area2D。
- Cue event 只传递 node id / position / tone 所需信息，不暴露 threshold 或 player insight 数值。
- 如果 prerequisite 在场景内变化，下次检测必须读取最新 ConditionEvaluator 结果。

## Performance Notes

Detection is expected to have negligible frame impact. The detector must iterate only over active scene nodes from `InsightNodeRegistry`, with ADR-0018 limiting active nodes to no more than 6 per scene. A single detection pass must avoid file IO, random sources, allocations proportional to world content, and duplicated prerequisite parsing; prerequisite checks should call the shared evaluator using already-loaded runtime state.

## QA Test Cases

- **AC-1**: insight 足够时进入范围显示提示。
  - Given: player insight=15，InsightNode threshold=10，玩家位置进入 detection_radius
  - When: 执行一次 proximity detection
  - Then: Registry 中节点状态为 DETECTED，并发布 cue shown 事件
  - Edge cases: detection_radius 边界等于、略大于、略小于
- **AC-2**: insight 不足时无提示。
  - Given: player insight=8，InsightNode threshold=10
  - When: 玩家进入 detection_radius
  - Then: 节点保持 UNDISCOVERED，事件总线没有 cue shown 事件
  - Edge cases: prerequisite 满足但 insight 不足、insight 为最低值 5
- **AC-3**: 洞察提升后重访可发现。
  - Given: 玩家第一次 insight=8 未触发，随后 insight 提升至 10
  - When: 玩家离开后再次进入 detection_radius
  - Then: 节点重新检定并进入 DETECTED
  - Edge cases: prerequisite 同时从 false 变 true、节点仍为 UNDISCOVERED

## Test Evidence

**Required evidence**:
- `tests/unit/exploration/insight_detection_threshold_test.cs`

**Status**: [x] Created — 9 tests passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightDetectionThresholdTest`

## Dependencies

- Depends on: ei-001
- Unlocks: ei-003, ei-004

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 5/5 passing
**Deviations**:
- Advisory: `ProximityDetector.Detect` returns a new result list per pass. This is acceptable under ADR-0018's active node budget (<= 6), but should be revisited if called directly from a long-running `_PhysicsProcess` hot path.
- Advisory: `IInsightConditionEvaluator` is an adapter seam for ADR-0014's shared `ConditionEvaluator`; later integration must bind it to the real shared evaluator rather than leaving only test fakes.
**Test Evidence**: Logic unit tests at `tests/unit/exploration/insight_detection_threshold_test.cs` passed 9/9 via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightDetectionThresholdTest`.
**Code Review**: Complete — `/code-review` approved with suggestions.
