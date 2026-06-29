# Story: ei-006 — EI P1/P2 Tech Debt 偿还

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-29
> **Layer**: Feature
> **Type**: Logic
> **Priority**: P1
> **Estimate**: 2.0 days (校准后预期 ~0.8d actual)
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §F1 场景洞察检定, §Edge Cases E2/E4
> **TR-ID**: TR-exploration-insight-002, TR-exploration-insight-003, TR-exploration-insight-004

## Context

ei-003 和 ei-004 关账时记录了 3 条 boundary-contract 债务（见 `production/notes/exploration-debt-triage-2026-06-22.md`）。Presentation 层接入前必须偿还，否则双 API 时序漂移、孤立 monologue 状态和重复 hide event 会直接暴露给 UI 层。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: N/A — 纯 Foundation 层重构，不涉及引擎 API。所有改动在 `src/FengZhi.Foundation/Exploration/` 内完成。

## Acceptance Criteria

- [ ] **AC-1 Detect path 统一**: `ProximityDetector.Detect()` 标记 `[Obsolete]`，内部实现转发到 `Tick(delta=0)` 路径，确保 linger 计时从 `Tick()` 统一起算（无双路径时序漂移）。
- [ ] **AC-2 Tick-only detection**: 通过 `Tick(delta)` 推进时间后，已进入 detection_radius 的节点正确触发 `InsightCueShownEvent`，行为与旧 `Detect()` 一致但 linger 立即注册。
- [ ] **AC-3 Monologue lifecycle 两阶段**: `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)` 在调用 reward 分派前发布 `MonologueRequestPendingEvent`；reward 成功时发布 `MonologueRequestCommittedEvent`；reward 失败时发布 `MonologueRequestCanceledEvent`。
- [ ] **AC-4 Canceled 不误标**: reward 失败后节点保持 `DETECTED` 状态（不误标 `INVESTIGATED`），下次追查仍可触发。
- [ ] **AC-5 Hide event dedup**: 同一 nodeId 在同一次离开 detection_radius 周期内只发布 1 次 `InsightCueHiddenEvent`，`ProximityDetector` 内部加 dedup guard。
- [ ] **AC-6 Ignored hide dedup**: `IGNORED` 节点离开范围时只发 1 次 hide event，不因多订阅方产生重复。
- [ ] **AC-7 回归绿**: 现有 ei-001~ei-005 所有 Foundation tests 继续通过（baseline 无退步）。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: EventBus publish 必须使用 `IEventBus.Publish<T>()` 单一入口 — source: ADR-0001
- Required: Foundation 层不得引用 Godot namespace（现有 `using Godot` 仅限 `Vector2` 值类型） — source: ADR-0018 boundary
- Required: Monologue lifecycle 事件采用两阶段模式（Pending → Committed/Canceled），与 cu-006 BattleEventBus 决胜事件模式一致
- Forbidden: 不得在 Foundation 层引入 async/await 或 Task.Delay — source: control-manifest Platform Layer
- Guardrail: Detect() 标 [Obsolete] 但保留编译兼容，不 break 现有调用方（迁移在 Presentation Adapter story 完成）

**设计决策（monologue lifecycle 方案 b）：**
- 新增 3 个事件 record: `MonologueRequestPendingEvent(nodeId)`, `MonologueRequestCommittedEvent(nodeId)`, `MonologueRequestCanceledEvent(nodeId)`
- 放置于 `InsightEvents.cs`，与现有 cue/discovery 事件同文件
- `DiscoveryDispatcher.OnPlayerInvestigate` 在 preflight 通过后立即 publish Pending，在 DispatchSideEffects 成功后 publish Committed，在任何 failure return 前 publish Canceled

**Hide event dedup 实现：**
- 在 `ProximityDetector` 的 `ResetTransientNodesOutsideRange` 和 `HideVisibleCuesForLockPause` 中，publish 前检查 `_hiddenPublishedThisCycle` HashSet
- 每次 `Tick()` 开头或 `ResumeDetection()` 时清空该 set

## Out of Scope

- 不修改 `IInsightNarrativePort` / `IInsightCodePhraseBookPort` 接口签名
- 不做 Presentation 层的 Godot 节点适配（属于 S8-EI-Presentation-Adapter）
- 不做 `Detect()` 调用方的迁移（调用方迁移在 Adapter story 中完成，本 story 仅标 Obsolete）
- 不修改 ei-002 的 `IInsightConditionEvaluator` 绑定方式

## Performance Notes

无性能影响。Dedup guard 仅增加一个 HashSet<string> lookup（≤6 active nodes），单帧开销可忽略。

## QA Test Cases

- **AC-1/AC-2**: Detect path 统一
  - Given: `Detect()` 被标 `[Obsolete]`，内部转发 `Tick(0)`
  - When: 调用 `Detect(pos, insight)` 和 `Tick(pos, insight, 0)` 对同一节点
  - Then: 两者行为一致——cue shown event 发出且 linger 已注册
  - Edge: delta=0 的 Tick 不推进 linger 计时；已 INVESTIGATED 节点无事件
- **AC-3/AC-4**: Monologue lifecycle 两阶段
  - Given: 节点处于 DETECTED，调用 `OnPlayerInvestigate(nodeId)`
  - When: reward 成功 / reward 失败
  - Then: 成功路径 publish Pending → Committed + 标 INVESTIGATED；失败路径 publish Pending → Canceled + 保持 DETECTED
  - Edge: pending 状态重入同节点 → 拒绝（不发重复 Pending）
- **AC-5/AC-6**: Hide event dedup
  - Given: 节点已 DETECTED 或 IGNORED
  - When: 离开 detection_radius
  - Then: 只发 1 次 InsightCueHiddenEvent
  - Edge: 场景卸载后 publish → 无事件无 crash；多订阅方不产生重复

## Test Evidence

**Required evidence**:
- `tests/unit/exploration/ei_debt_paydown_test.cs`

**Status**: [ ] Pending — ~12-15 unit tests expected

## Dependencies

- Depends on: ei-003, ei-004 (both Complete)
- Unlocks: S8-EI-Presentation-Adapter

## Completion Notes

**Completed**: 2026-06-29
**Criteria**: 7/7 passing
**Deviations**: None
**Test Evidence**: Logic: unit test at `tests/unit/exploration/ei_debt_paydown_test.cs` (13 tests, all pass)
**Code Review**: Complete — verdict APPROVED (2 LOW suggestions accepted as-is)
**Effort**: estimate 16.0h / actual ~2.5h (variance -84%)
