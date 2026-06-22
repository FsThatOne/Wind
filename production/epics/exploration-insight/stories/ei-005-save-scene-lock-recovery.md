# Story: ei-005 — 存档恢复、场景卸载清理与战斗/对话锁恢复

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-14
> **Layer**: Feature
> **Type**: Integration
> **Priority**: P0
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §E, §H AC7/AC8
> **TR-ID**: TR-exploration-insight-005

## Context

洞察发现历史必须随存档恢复，但 DETECTED/IGNORED 是瞬态提示状态，不能污染存档。战斗、对话或其他 LockMode >= Partial 的流程出现时，探索提示必须暂停并隐藏；锁释放后如果玩家仍在范围内，检测应恢复。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight; ADR-0004: Save Encryption; ADR-0006: Scene Loading Strategy
**Engine**: Godot 4.7-stable | **Risk**: LOW / MEDIUM for scene-lock timing

## Acceptance Criteria

- [x] Exploration save capture 只序列化 `INVESTIGATED` 终态节点，不保存 `DETECTED` 或 `IGNORED`。
- [x] Restore 后已发现节点保持 `INVESTIGATED`，且 one_time 节点在场景加载时不重复触发。
- [x] Restore 遇到未知 node id、非法状态或重复记录时必须安全处理并记录警告，不得崩溃或写入损坏状态。
- [x] 场景卸载时 active nodes 清空，pending trigger 队列和当前 cue 状态同步清理，避免跨场景残留提示。
- [x] LockMode >= Partial 时 detector 暂停并隐藏所有 active cues；锁释放后重新检测，若玩家仍在范围内且条件满足则恢复提示。
- [x] AC8 的战斗期间提示暂停/恢复路径必须通过集成测试覆盖，不依赖手动 QA 才能判定。

## Implementation Notes

- Save adapter 遵循 ADR-0004 `ISaveable` 或项目现有 save port 约定；不要直接操作加密文件层。
- Restore 必须严格校验状态枚举，只接受 INVESTIGATED；损坏数据不应导致未发现节点被误标记。
- Lock guard 查询共享 `LockMode` 语义，不应私有定义锁枚举。
- 场景卸载和 LockMode 暂停都必须清理 pending queue，防止锁释放后触发已离场节点。

## QA Test Cases

- **AC-1**: 存档只保存 INVESTIGATED。
  - Given: Registry 中有 UNDISCOVERED、DETECTED、IGNORED、INVESTIGATED 节点
  - When: Capture exploration save data
  - Then: save payload 只包含 INVESTIGATED 节点
  - Edge cases: 空状态表、one_time=false、重复 capture
- **AC-2**: Restore 后已发现节点不重复触发。
  - Given: save payload 包含 node_a INVESTIGATED
  - When: Restore 并加载 node_a 所在场景
  - Then: node_a 保持 INVESTIGATED，one_time=true 时不在 active nodes 中
  - Edge cases: unknown node id、非法状态字符串、重复 node id
- **AC-3**: 战斗/对话锁暂停并恢复提示。
  - Given: 玩家站在满足条件的 InsightNode 范围内，cue 已显示
  - When: 发布 LockMode Partial acquired，再 release
  - Then: acquired 时 detector 暂停且 cue hidden；release 后 detector 恢复并重新显示 cue
  - Edge cases: 锁期间玩家离开范围、锁期间节点变 INVESTIGATED、嵌套锁
- **AC-4**: 场景卸载清理 pending queue。
  - Given: 多节点 stagger 队列中仍有待触发节点
  - When: `OnSceneUnloaded()` 被调用
  - Then: pending queue 清空，active cues 隐藏，后续 tick 不触发旧场景节点
  - Edge cases: 重复 unload、scene unload 后立即 load 新 scene

## Test Evidence

**Required evidence**:
- `tests/integration/exploration/insight_save_scene_lock_test.cs`

**Status**: [x] Created — 7 save/scene/lock integration tests added; 41/41 target tests passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightSaveSceneLockTest|InsightCueTimingTest|InsightNodeRegistryTest|InsightRewardDispatchTest"`; exploration suite 51/51 passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration`; full `dotnet test` passed 1295/1295

## Dependencies

- Depends on: ei-001, ei-003, ei-004
- Unlocks: Sprint smoke-check and QA plan for exploration-insight

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 6/6 passing
**Deviations**: None blocking. Advisory story documentation gaps logged as tech debt: missing Estimate, Out of Scope, Control Manifest Rules, Engine Notes, and Performance Notes sections.
**Test Evidence**: Integration test at `tests/integration/exploration/insight_save_scene_lock_test.cs`; target reward/timing/registry/lock suite passed 41/41; exploration suite passed 51/51; full `dotnet test` passed 1295/1295.
**Code Review**: Complete — Lean mode; user confirmed `/code-review` passed after lock-pause regression fix.
**Actual Effort**: Within planned 1.5 day sprint estimate.
