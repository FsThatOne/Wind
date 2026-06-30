# ei-009: EI Scene Content — chapter_00 cave InsightNode 配置 + narrative_context

> **Status**: Complete
> **Last Updated**: 2026-06-29
> **Type**: Config/Data
> **Layer**: Presentation
> **Estimate**: 1 day (校准后预期 ~0.4d actual)
> **GDD 来源**: design/gdd/exploration-insight.md §3 发现类型 + §4 洞察检定流程 + F3 门槛分级
> **TR-IDs**: TR-exploration-insight-005
> **ADR**: docs/architecture/adr-0018-exploration-insight.md
> **Manifest Version**: 2026-06-10
> **Depends On**: ei-008 (Presentation Interaction — ✅ Complete)

## Context

ei-008 在 BackMountainCliffCave 中配置了 1 个测试 InsightNode (`cave_old_mark`, EnvironmentDetail, threshold=5)。
本 story 将其扩展为 3-4 个有叙事目的的节点，覆盖至少 2 种 DiscoveryType，为 chapter_00 序章提供探索深度。

场景设定（后山崖洞）：
- 单线山路尽头的半山洞穴，藏酒、储物
- 主角和师姐的秘密基地
- 现有交互点：wine_pickup、storage_shelf、memory_marker、rest_spot

GDD 设计原则：
- 每个洞察节点必须有叙事目的
- 门槛 5-10 为低难度（氛围细节）
- 门槛 11-20 为中难度（线索/暗号）
- EnvironmentDetail 无奖励仅独白；Clue 设 quest_flag

## Scope

**In scope:**
- 在 BackMountainCliffCaveGame 中注册 3-4 个 InsightNode（覆盖 EnvironmentDetail + Clue 类型）
- 为每个节点编写 narrative_context 独白文本（1-2 句中文叙事）
- 合理分配门槛值（低 + 中难度）
- 替换 ei-008 的测试节点或保留并完善其文案

**Out of scope:**
- MartialFragment / CodePhrase / Loot 类型节点（chapter_01+ 使用）
- 新场景（仅 BackMountainCliffCave）
- 前置条件（prerequisite）配置（v0 使用 AlwaysTrueConditionEvaluator）
- 独白演出动画（v0 用 DialoguePanel 文本模式）

## Acceptance Criteria

- AC-1: BackMountainCliffCave 场景中注册至少 3 个 InsightNode（不含 ei-008 的 cave_old_mark 测试节点，或将其升级为正式节点）
- AC-2: 节点覆盖至少 2 种 DiscoveryType（EnvironmentDetail + Clue）
- AC-3: 每个节点的 narrative_context 为 1-2 句有叙事意义的中文独白，与场景氛围一致
- AC-4: 门槛值按 GDD F3 分级设置：EnvironmentDetail 用 5-10，Clue 用 11-20
- AC-5: Clue 类型节点配置有效的 FlagId（格式 `ch00_[描述]`）
- AC-6: 端到端可玩——进入场景、靠近各节点、追查触发独白和奖励反馈均正常

## Implementation Notes

- 所有配置在 `BackMountainCliffCaveGame.RegisterTestInsightNode()` 中完成（改名为 `RegisterInsightNodes()`）
- 节点位置使用 `TileToScreen(x, y)` 转换，选择与现有交互点不重叠的 tile
- 参考现有 tile overrides: storage_shelf(24,5), wine_jars(20,8), oil_lamp(17,12), sister_mark(19,15), rest_mat(22,18)

## Files to Create/Modify

**Modify:**
- `feng-zhi/scripts/BackMountainCliffCaveGame.cs` — 扩展 InsightNode 注册（3-4 个节点 + narrative_context）

## QA Test Cases

1. **节点可见性**: 玩家 insight=10 时，3 个 threshold≤10 的节点在接近时出现水墨提示
2. **中难度节点**: 玩家 insight=10 时，threshold=15 的 Clue 节点不出现；insight≥15 时可见
3. **追查完整性**: 追查 EnvironmentDetail 节点 → 仅显示独白 → 无奖励反馈文字
4. **Clue 反馈**: 追查 Clue 节点 → 独白 + "获得线索：ch00_xxx" 文字反馈

## Test Evidence

- Config/Data: smoke check — 编译通过 + 场景可运行

## Engine Notes

- InsightNode.Position 使用 Godot Vector2（与玩家 GlobalPosition 同坐标系）
- DetectionRadius 单位为像素，cave 场景 tile=128x64，建议 radius 100-150

## Performance Notes

- 4 个 InsightNode + 对应 Area2D 远在 200 draw call budget 之内
- ProximityDetector.Tick 每帧遍历 registry 中 active 节点，4 个节点无性能影响
