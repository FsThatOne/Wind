# Story: ei-001 — InsightNode 数据模型、注册表与场景激活

> **Epic**: exploration-insight
> **Status**: Complete
> **Last Updated**: 2026-06-14
> **Layer**: Feature
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 0.75 days
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/exploration-insight.md §D, §E, §H AC1/AC7
> **TR-ID**: TR-exploration-insight-001

## Context

探索/洞察系统需要先建立稳定的数据模型、状态机和场景注册表，供后续距离检测、奖励分派和存档恢复复用。该 story 不实现视觉提示渲染，只保证当前场景的 InsightNode 生命周期可被逻辑层稳定查询。

**ADR Governing Implementation**: ADR-0018: Exploration & Insight
**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: N/A — no post-cutoff engine API is required for registry-only logic. Godot `Resource` compatibility follows ADR-0018, and scene lifecycle integration is limited to explicit `OnSceneLoaded` / `OnSceneUnloaded` calls rather than engine-specific callbacks in this story.

## Acceptance Criteria

- [x] 定义 `InsightNode`、`DiscoveryReward`、`DiscoveryType` 和 `DiscoveryState`，字段覆盖 GDD 中的 id、scene_id、position、detection_radius、insight_threshold、discovery_type、reward、narrative_context、prerequisite、one_time。
- [x] `InsightNodeRegistry.OnSceneLoaded(sceneId)` 只激活当前 scene_id 的节点，并跳过已 `INVESTIGATED` 且 `one_time=true` 的节点。
- [x] `InsightNodeRegistry.OnSceneUnloaded()` 清空 active scene nodes，并将 `DETECTED` / `IGNORED` 瞬态状态回退为 `UNDISCOVERED`。
- [x] Registry 查询接口不得暴露可变内部集合；未知 node id 查询必须返回安全默认值或明确失败结果。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: Exploration architecture uses `InsightNode` Resource + `InsightNodeRegistry` + later `ProximityDetector` + `DiscoveryDispatcher`; this story implements only the model and registry slice.
- Required: Insight state must support `Undiscovered → Detected → Ignored → Investigated`, with `Detected` / `Ignored` treated as transient states.
- Required: Scene hooks must align with `OnSceneLoaded` / `OnSceneUnloaded`; one-time `Investigated` nodes are skipped during scene activation.
- Required: Exploration flags and persisted discovery markers must use the registered `insight_` prefix when flags are introduced by later stories.
- Forbidden: Do not create one `Area2D` per `InsightNode`; detection belongs to later centralized `ProximityDetector` work.
- Guardrail: Scene active node count follows ADR-0018 content budget of no more than 6 nodes per scene.

- 数据模型应保持 C# POCO/Resource 兼容，避免把检测逻辑塞进 Resource。
- Registry 拥有运行时状态，但只允许后续存档 story 持久化 `INVESTIGATED`。
- 场景生命周期钩子必须与 ADR-0006 的 scene loaded/unloaded 边界对齐。
- Flag 命名后续统一使用 `insight_` 前缀，避免与 `jianghu_` / `narrative_` 混写。

## Performance Notes

No measurable frame impact is expected in this story. Registry activation happens only on scene load/unload boundaries, and active node counts follow ADR-0018's scene budget of no more than 6 nodes per scene. Runtime per-frame proximity scanning is explicitly out of scope for `ei-001` and handled by `ei-002` / `ei-003`.

## QA Test Cases

- **AC-1**: InsightNode 数据模型覆盖 GDD 字段。
  - Given: 一个包含全部字段的 InsightNode fixture
  - When: 构造并读取节点定义
  - Then: 所有字段保留原值，DiscoveryType 与 DiscoveryState 枚举可表达 6 种发现类型和 4 种状态
  - Edge cases: 空 prerequisite、默认 one_time、空 narrative_context
- **AC-2**: 场景加载只激活当前场景节点。
  - Given: Registry 中有两个 scene_id 的节点
  - When: 调用 `OnSceneLoaded("scene_a")`
  - Then: active nodes 只包含 scene_a，且已 INVESTIGATED one_time 节点被跳过
  - Edge cases: unknown scene、重复加载同一 scene、one_time=false 的 INVESTIGATED 节点
- **AC-3**: 场景卸载清理瞬态状态。
  - Given: active nodes 中存在 DETECTED、IGNORED、INVESTIGATED
  - When: 调用 `OnSceneUnloaded()`
  - Then: DETECTED/IGNORED 回退 UNDISCOVERED，INVESTIGATED 保持，active nodes 清空
  - Edge cases: 空 active list、重复卸载

## Test Evidence

**Required evidence**:
- `tests/unit/exploration/insight_node_registry_test.cs`

**Status**: [x] Created — 9 tests passing via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightNodeRegistryTest`

## Dependencies

- Depends on: None
- Unlocks: ei-002, ei-005

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 4/4 passing
**Deviations**: Advisory only — implementation uses POCO-compatible C# models instead of Godot `Resource` / `[GlobalClass]` data assets. This is accepted for `ei-001` because the story explicitly scoped model/registry logic and deferred asset loading; converge before exploration data loading stories.
**Test Evidence**: Logic unit test at `tests/unit/exploration/insight_node_registry_test.cs` (9 tests)
**Verification**: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightNodeRegistryTest` passed 9/9; `GetDiagnostics` reported no errors
**Code Review**: Complete — `/code-review` returned APPROVED WITH SUGGESTIONS
