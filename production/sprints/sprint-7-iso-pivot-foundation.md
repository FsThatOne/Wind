# Story: S7-Iso-Pivot-Foundation

**Status**: Ready
**Sprint**: 7
**Priority**: must-have（**blocks** S7-VS-Combat-Loop / cu-visual-evidence / S7-VS-Outcome-Feedback 在 iso 投影上重建）
**Owner**: Gameplay-Programmer / Tools-Programmer / Designer
**Estimate**: 1.0 day / 8.0 hours
**Created**: 2026-06-22
**Source ADR**: [ADR-0022 — Isometric Diamond Projection & 4-Directional Character Animator](../../docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md)

---

## Goal

落地 ADR-0022 的 **Migration Plan 阶段 2-5**：

- 新增 Foundation 层 iso 投影工具 + 4 斜向动画端口
- 删除 ADR-0021 §扩展点 1 的 8 方向遗留代码 / 资产
- 切换 `feng-zhi/StartCave.tscn` 到 isometric TileMapLayer + 4 斜向 sprite
- 同步 GDD / ADR-0010 / art-bible / traceability-index 文档

完成后 `feng-zhi/StartCave.tscn` 实机 WASD 控制角色走 4 斜向，sprite 切换正确，
为 S7-VS-Combat-Loop 提供 iso 战棋的可信基础。

## Embedded GDD Requirements

| GDD | TR-ID | Requirement | This Story Addresses |
|---|---|---|---|
| combat-system.md | TR-COMBAT-SPACE | 棋盘 / 角色朝向 | §战棋空间规则双轴标注（屏幕 NE / SE / SW / NW = 逻辑 X+ / Y+ / X- / Y-） |
| map-scene-management.md | TR-MAP-VIS | 场景视觉一致性 | TileMapLayer 全部 iso |
| art-bible.md | TR-ART-REF | 《大侠立志传》参考 | Reference Board 补 isometric diamond 注 |

## Embedded ADR Guidance

- ADR-0022 §1 投影几何 — `IsoProjection` 公式照抄落 `Foundation/Geometry/`
- ADR-0022 §2 4 斜方向枚举与子接口 — `Iso4Direction` / `IIso4CharacterAnimator` 契约不变
- ADR-0022 §3 输入映射 — WASD → cart 向量
- ADR-0022 §4 TileMapLayer 配置 — `tile_shape = Isometric`, `tile_layout = DiamondDown`, `tile_size = 128×64`, `y_sort_enabled = true`
- ADR-0022 §5 与 ADR-0021 的关系 — 主端口 `ICharacterAnimator` / `Facing` 不变
- ADR-0021 §扩展点 1 标 "Partially Superseded by ADR-0022 (§扩展点 1)"

## Acceptance Criteria

| AC | 描述 | 验收方式 | 估时 |
|---|---|---|---|
| **AC1** | ADR-0022 写盘并 Accepted；ADR-0021 §扩展点 1 段加 superseded note + Status 行追加 "Partially Superseded by ADR-0022 (§扩展点 1)"；traceability-index.md 第 2 / 12 / 63 行 ADR 列追加 ADR-0022 + Coverage Summary 表更新 | `docs/architecture/adr-0022-*.md` 存在；ADR-0021 section diff 通过；traceability-index.md diff 通过 | 0.5h |
| **AC2** | Foundation 新增 `Geometry/IsoProjection.cs`（纯静态类）+ 单测覆盖 `CartToScreen` / `ScreenToCart` 互逆 | xUnit 通过；含 4 个边界 case（原点、正负、对角、半 tile） | 1.0h |
| **AC3** | 删除 `EightDirection.cs` / `IDirectionalCharacterAnimator.cs` / `EightDirectionAnimatedSprite2DAnimator.cs` + 11 旧测试 | 文件不存在；旧测试名从测试输出消失；构建无 dangling reference | 0.5h |
| **AC4** | 新增 `Animation/Iso4Direction.cs` + `Animation/IIso4CharacterAnimator.cs` + `Animation/GodotIntegration/Iso4AnimatedSprite2DAnimator.cs` + `Animation/Fakes/FakeIso4CharacterAnimator.cs` | 接口契约与 ADR-0022 §2 一致；动画名 `walk_ne / walk_se / walk_sw / walk_nw` | 1.0h |
| **AC5** | 8 个新单测覆盖：方向选区 4 区间正确性、±15° 迟滞、帧相位保持、零向量 Pause、Fake 计数 | Foundation 测试基线 1378 - 11 + 8 = **1375/1375** | 1.0h |
| **AC6** | `feng-zhi/assets/character/main_character.tres` 重建：`idle + walk_ne + walk_se + walk_sw + walk_nw`；旧 8 向资产挪到 `feng-zhi/assets/character/_archive_8dir_2026-06-22/` | `.tres` 在 4.7 编辑器无 import error；旧 PNG 在 archive 不被引用；新 4 张 sprite 来源：复用旧 NE / SE / SW / NW 4 张（首版） | 1.0h |
| **AC7** | `feng-zhi/scripts/CavePlayer.cs` 切换到 `IIso4CharacterAnimator`；输入映射 WASD → cart 向量（W=(-1,-1), A=(-1,1), S=(1,1), D=(1,-1) 各归一化） | 实机：按 W 角色走屏幕 ↖，按 D 走屏幕 ↗，sprite 切换正确 | 1.0h |
| **AC8** | `feng-zhi/StartCave.tscn` 的 TileMapLayer `tile_shape = Isometric`, `tile_layout = DiamondDown`, `tile_size = 128×64`, `y_sort_enabled = true` | 编辑器中 tile 显示为菱形；角色绕障碍走时遮挡正确 | 1.0h |
| **AC9** | 文档更新：`design/gdd/combat-system.md` §战棋空间规则双轴标注；`adr-0010` 表追加 iso 列；`art-bible.md` Reference Board 补注 | 三处 diff 通过 design-review skill 抽查 | 0.5h |
| **AC10** | `production/sprint-status.yaml` 更新：本 story 标 done；`S7-VS-Combat-Loop` 的 blocker 字段引用本 story；`docs/architecture/traceability-index.md` Recent Changes 追加 ADR-0022 entry | git diff 干净；`git status` clean | 0.5h |

**Total**: 8.0h

## Definition of Done

- [ ] AC1–AC10 全部完成
- [ ] Foundation 1375/1375 PASS
- [ ] `feng-zhi/` 在 Godot 4.7 编辑器中 Play `StartCave.tscn` 无 ERROR，WASD 走 4 斜向有效
- [ ] 视觉证据：≥ 1 段 ≤ 30s 录屏 + ≥ 3 张关键截图（NE / SE / NW 走路 + tile 遮挡），落 `production/qa/evidence/s7-iso-pivot-foundation/`
- [ ] estimate_hours: 8.0 / actual_hours: 实际值（DoD 工时对照）
- [ ] git working tree clean

## Test Evidence

- Foundation 测试报告：`production/qa/evidence/s7-iso-pivot-foundation/foundation-test-report.md`
- 实机录屏 + 截图：`production/qa/evidence/s7-iso-pivot-foundation/media/`
- 操作记录 + 设计签核：`production/qa/evidence/s7-iso-pivot-foundation/manual-smoke.md`

## Dependencies

- **Blocks**: `S7-VS-Combat-Loop`、`cu-visual-evidence`、`S7-VS-Outcome-Feedback`、`S7-VS-Playtest-Session`
- **Blocked by**: 无
- **Risk**: AC6 sprite 复用旧 NE / SE / SW / NW 4 张可能与新 isometric 视角下的"正面斜走"风格不完全契合；若实机感观不达标，回退到 generate2dsprite 重出 4 张斜向 sprite，加挂 0.5d 至 estimate

## Out of Scope

- 设置中切换到"屏幕直觉模式"（W → 走世界 N+ 而非 NW）— deferred 到 Sprint 8 backlog
- 鼠标 hover 战棋格 highlight — deferred 到 S7-VS-Combat-Loop
- 4 张斜向 sprite 重生成（generate2dsprite skill）— Sprint 7 内若旧资产可用则不做；不达标时再触发
- VS 主场景重建（`feng-zhi/scenes/vs/*.tscn`）TileMapLayer iso 切换 — 由 S7-VS-Combat-Loop 在本 story 完成后承接

## Notes

- 本 story 是 Sprint 7 投影 pivot 的 prerequisite，所有下游 story 在 backlog 中加 `blocker: depends on S7-Iso-Pivot-Foundation`
- ADR-0022 是 Sprint 7 内的第二个新 ADR（继 ADR-0021 后），traceability-index.md Recent Changes 需要 + 1 行
