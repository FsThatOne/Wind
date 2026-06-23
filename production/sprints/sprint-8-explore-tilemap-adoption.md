# Story: S8-Explore-TileMap-Adoption

**Status**: backlog (Sprint 8 候选)
**Sprint**: 8
**Priority**: tbd（建议 must-have，理由见下）
**Owner**: ""
**Estimate**: 1.5–2.0 day / 12.0–16.0 hours
**Created**: 2026-06-22
**Source ADR**: [ADR-0022 — Isometric Diamond Projection & 4-Directional Character Animator](../../docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md)
**Carried-over From**: S7-Iso-Pivot-Foundation §AC8（标 N/A 时承接到此 story）+ A 路线落地 follow-up（`battle_jiangnan_bandit.tscn` iso 蒙皮在 cu-visual-evidence 阶段已完成；本 story 解决 explore 场景的同步迁移）

---

## Goal

把 explore 场景从「散落 Sprite2D 占位」迁移到「真正的 iso TileMapLayer + 共享 TileSet 资源」，
彻底兑现 ADR-0022 「一套 isometric 投影统一战斗与探索」的承诺。

完成后所有 explore + battle 场景共享同一份 `cliff_cave_ground_tiles.tres` TileSet 资源，
战斗与探索的 tile 视觉与几何完全一致。

## Embedded GDD Requirements

| GDD | TR-ID | Requirement | This Story Addresses |
|---|---|---|---|
| map-scene-management.md | TR-MAP-VIS | 场景视觉一致性（探索 / 战斗共享 tile 视觉） | 把 explore scene 的散落 Sprite2D 替换为 TileMapLayer |
| combat-system.md | TR-COMBAT-SPACE | 棋盘几何 isometric diamond | explore scene 也按 iso 投影渲染 |
| art-bible.md | TR-ART-REF | 《大侠立志传》参考的 isometric 视觉一致性 | 全场景 iso 统一 |

## Embedded ADR Guidance

- ADR-0022 §4 TileMapLayer 配置 — 所有层 `tile_shape = Isometric`, `tile_layout = DiamondDown`, `tile_size = 64×32`（按真实资产值，非 ADR 中 128×64 暂定值）, `y_sort_enabled = true`
- ADR-0010 五层结构 — Ground / Terrain / Structures / Overlay / Collision，本 story 以 Ground + Terrain 为主先行
- 战斗已使用 [cliff_cave_ground_tiles.tres](../../feng-zhi/assets/maps/back_mountain_cliff_cave/tilesets/cliff_cave_ground_tiles.tres) — 本 story 复用同份资源，零额外资产

## Acceptance Criteria

| AC | 描述 | 验收方式 | 估时 |
|---|---|---|---|
| **AC1** | 把 [back_mountain_cliff_cave_day.tmx](../../feng-zhi/assets/maps/back_mountain_cliff_cave/maps/back_mountain_cliff_cave_day.tmx) 与 `_night.tmx` 的图层数据**导入或翻译**为 Godot TileMapLayer 的 `SetCell` 调用 / `tile_map_data` PackedByteArray（推荐前者，便于审阅 + 单测） | TileMapLayer 在 Godot 编辑器中可见完整 cave 地形；与 .tmx 视觉一致 | 4.0h |
| **AC2** | 重写 [BackMountainCliffCave.tscn](../../feng-zhi/scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn)：替换散落 Sprite2D 为 TileMapLayer 节点（按 ADR-0010 五层结构落 Ground / Terrain / Overlay 三层最小集合） | scene 加载 0 err / 0 warn；视觉与原版 .tmx preview 一致 | 3.0h |
| **AC3** | [CavePlayer.cs](../../feng-zhi/scripts/CavePlayer.cs) 切换到 TileMapLayer 坐标系：玩家 position 改用 `IsoProjection.CartToScreen` 计算，逻辑坐标存储为 `Vector2I cellPos`；碰撞/可走判定改用 `TileMapLayer.GetCellTileData(cellPos).GetCustomData("tile_name")` 反向查 walkable | 实机：玩家行走与 .tmx 中的可走/不可走区域一致；走到 boundary tile 不可入 | 3.0h |
| **AC4** | Props 节点（11 张道具 sprite，wine_jar / oil_lamp / rest_mat 等）保留为 Sprite2D 但 parent 加 `y_sort_enabled = true`，`y_sort_origin` 设为 sprite 脚下 | 实机：玩家走过 oil_lamp 时正确遮挡 / 被遮挡 | 1.5h |
| **AC5** | 视觉证据：1 段 ≤ 30s 录屏（玩家走遍 cave 主要可达区域）+ 5 张关键截图（4 斜向走路 + 1 张全景对比 .tmx preview）落 `production/qa/evidence/s8-explore-tilemap-adoption/` | 录屏 + 截图齐备 | 0.5h |
| **AC6** | 文档更新：sprint-7.md 的 S7-Iso-Pivot-Foundation note 中 AC8 N/A 注解链接到本 story；ADR-0022 §4 表加 "实际 tile_size = 64×32（按 cliff_cave_ground_tiles.png）" 注 | diff 干净 | 0.5h |
| **AC7** | sprint-status.yaml 本 story 标 done；DoD 工时记录 | git status clean | 0.5h |

**Total**: 13.0h（≈ 1.6d）

## Definition of Done

- [ ] AC1–AC7 全部完成
- [ ] BackMountainCliffCave 实机走查 PASS（玩家可走全部 walkable 区域、boundary 不可入、props y-sort 正确）
- [ ] Foundation + feng-zhi build 0 err / 0 new warn
- [ ] estimate_hours: 13.0 / actual_hours: 实际值
- [ ] git working tree clean

## Dependencies

- **Blocks**: 任何后续 explore 场景（陇右官道 / 江南水乡）使用 TileMapLayer 的迁移（同一套 pattern）
- **Blocked by**: S7-Iso-Pivot-Foundation done（已 done）+ S7 cu-visual-evidence battle iso 蒙皮 done（A 路线已 done）
- **Risk**:
  1. .tmx → Godot TileMapLayer 的图层 ID 与 atlas coords 映射可能有偏差，需逐 tile 比对
  2. 当前 BackMountainCliffCave 是 owner 验证 PASS 状态，迁移可能引入回归（CavePlayer 输入 / 碰撞）— 需要严格回归走查

## Out of Scope

- 江南水乡、陇右官道等其他 explore 场景的 iso 迁移 — 待本 story PASS 后立项 follow-up
- Collision 层物理碰撞细节（本 story 用 `walkable` custom data 做软碰撞即可）
- TileMap 编辑工具链（Tiled ↔ Godot 双向同步） — 现阶段单向（Tiled 设计 → Godot 一次性导入）即可

## Notes

- **A 路线已落地**：`battle_jiangnan_bandit.tscn` 在 Sprint 7 cu-visual-evidence 阶段已使用 [cliff_cave_ground_tiles.tres](../../feng-zhi/assets/maps/back_mountain_cliff_cave/tilesets/cliff_cave_ground_tiles.tres) 铺 5×5 战棋格；本 story 是**对称的 explore 落地**
- 估时偏差预期：±30%，主要风险在 AC1 .tmx 映射；如果发现 .tmx 图层定义复杂（≥ 5 层 / 变体 tile / 旋转），向上调到 2.0d
- 本 story 完成后，Sprint 8 下一个候选是 `S8-In-Place-Combat`（探索/战斗就地融合，不切场）— 见 ADR-0022 issue tracker
