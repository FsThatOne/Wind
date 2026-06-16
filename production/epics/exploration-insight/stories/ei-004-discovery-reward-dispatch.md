# Story: ei-004 — 发现奖励分派：Clue 与 CodePhrase

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-14
> **Layer**: Feature
> **Type**: Integration
> **Priority**: P0
> **Estimate**: 1.25 days
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §D, §H AC5/AC6
> **TR-ID**: TR-exploration-insight-004

## Context

玩家选择追查后，探索系统必须播放叙事独白并按 discovery_type 分派副作用。Sprint 4 优先验证主线叙事 Clue 与暗号 CodePhrase 两条关键集成路径，其他 reward type 保留同一分派接口供后续扩展。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight; ADR-0014: Living Jianghu Layer
**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: N/A — no post-cutoff Godot API is required for this story. Reward dispatch should remain pure C# / Port-Adapter logic so integration tests can verify Narrative and CodePhrase side effects without loading Godot scenes. A future scene adapter may call `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)` from an interaction input path, but this story should not depend on `_Process`, UI nodes, or real scene timing.

## Acceptance Criteria

- [x] `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)` 只允许 `DETECTED` 状态的节点被追查，重复追查或未知节点不得产生副作用。
- [x] 追查时必须请求播放 `narrative_context` 对应的 InnerMonologue 或等价叙事事件。
- [x] Clue 类型必须设置对应 `quest_flag`，并通过 Narrative port/event 通知主线叙事。
- [x] CodePhrase 类型必须通过 CodePhraseBook port/event 学会对应 `phrase_id`。
- [x] 成功追查后节点必须转为 `INVESTIGATED`，发布 `InsightDiscoveredEvent`，并隐藏当前 cue。
- [x] 所有探索 flag 写入必须使用 `insight_` 或被下游系统明确拥有的注册前缀，不允许无前缀 flag。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: Exploration reward flow must extend `InsightNodeRegistry` + `DiscoveryDispatcher`; do not introduce a parallel discovery state store.
- Required: `OnPlayerInvestigate(nodeId)` must first validate node existence, active/reachable state, `DETECTED` status, reward payload completeness, and downstream port availability before committing any state transition.
- Required: Successful investigate must publish an immutable `InsightDiscoveredEvent`, mark the node `INVESTIGATED`, and request cue hide; failed reward dispatch must leave the node non-investigated and must not partially write downstream state.
- Required: Clue dispatch must cross a Narrative port/event boundary and may write `narrative_` quest flags only when owned by the Narrative system; exploration-owned discovery history must use `insight_` prefix.
- Required: CodePhrase dispatch must cross a CodePhraseBook port/event boundary and learn the configured `phrase_id` exactly once or treat duplicate learn as a safe no-op according to the port contract.
- Forbidden: Do not let Feature code directly reference concrete UI/dialogue implementations, do not emit unprefixed flags, and do not silently succeed for unsupported `DiscoveryType` values.
- Guardrail: Reward dispatch is interaction-driven, not per-frame. It must perform no file IO, no scene scans, and no work proportional to total world content.

- 使用 Port/Adapter 或事件边界连接 Narrative 与 CodePhraseBook，避免 Feature 模块直接持有具体 UI/对话实现。
- Reward dispatch 必须先校验可执行性，再提交状态变更；若下游奖励失败，不得把节点标记为 INVESTIGATED。
- Clue 的 quest flag 可以属于 `narrative_`，但探索发现历史必须使用 `insight_` 前缀。
- EnvironmentDetail、Loot、MartialFragment、SideQuestEntry 可保留 switch 分支或 NotSupported 明确失败，不应静默成功。

## Performance Notes

Reward dispatch is expected to be O(1) per player investigate action. The dispatcher should resolve the target node by id through `InsightNodeRegistry`, validate one reward payload, call at most one downstream Narrative or CodePhraseBook port for the supported types in this story, and publish a small number of events. It must avoid file IO, runtime YAML parsing, full-scene scans, or repeated traversal of all InsightNodes. Because this is an interaction path rather than a frame-loop path, allocations for result objects/events are acceptable if they remain bounded and deterministic.

## QA Test Cases

- **AC-1**: Clue 节点设置主线 quest_flag。
  - Given: 一个 DETECTED 的 Clue InsightNode，reward.flag_id=`narrative_clue_old_letter`
  - When: 调用 `OnPlayerInvestigate(nodeId)`
  - Then: Narrative flag port 收到设置请求，节点转为 INVESTIGATED，并发布 discovered event
  - Edge cases: flag_id 为空、下游 flag 写入失败、重复 investigate
- **AC-2**: CodePhrase 节点写入暗号簿。
  - Given: 一个 DETECTED 的 CodePhrase InsightNode，reward.phrase_id=`inn_beam_mark`
  - When: 调用 `OnPlayerInvestigate(nodeId)`
  - Then: CodePhraseBook 收到 Learn 请求，节点转为 INVESTIGATED
  - Edge cases: phrase_id 为空、已学会同一暗号、下游拒绝写入
- **AC-3**: 非 DETECTED 状态不得分派奖励。
  - Given: 节点状态为 UNDISCOVERED、IGNORED 或 INVESTIGATED
  - When: 调用 `OnPlayerInvestigate(nodeId)`
  - Then: 不播放独白，不写 flag，不学习暗号，不重复发布 discovered event
  - Edge cases: unknown node id、reward type 未支持

## Test Evidence

**Required evidence**:
- `tests/integration/exploration/insight_reward_dispatch_test.cs`

**Status**: [x] Created — 13 reward-dispatch tests passing; 34/34 target tests passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightRewardDispatchTest|InsightCueTimingTest|InsightDetectionThresholdTest"`; exploration suite 44/44 passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration`

## Dependencies

- Depends on: ei-002
- Unlocks: ei-005

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 6/6 passing
**Deviations**: None blocking. Advisory tech debt logged for the remaining presentation-side monologue request when reward commit is rejected after preflight.
**Test Evidence**: Integration test at `tests/integration/exploration/insight_reward_dispatch_test.cs`; target reward/timing/detection suite passed 34/34; exploration suite passed 44/44.
**Code Review**: Complete — Lean mode; user confirmed post-fix small contract change can be treated as reviewed.
**Actual Effort**: Within planned 1.25 day story scope.
