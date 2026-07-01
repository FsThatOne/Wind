# ADR-0024: iso Tileset 资产层暂撤 ≠ 方向否决（jiangnan-iso-v1 弃用 + 重设计前置）

## Status
Accepted（决策落地 2026-06-24，commit 24c34ba 已删 v1 资产；2026-07-01 复核后，旧江南过渡占位资产也已退役；重设计 story `S8-tileset-iso-redesign` 尚未拍板）

## Date
2026-06-24

## Supersedes
None（不取代任何 ADR；本 ADR 记录的是**实施路径调整**，[ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md) 方向 **保持不变并继续生效**）

## Summary

2026-06-24 项目以 commit `24c34ba` 删除全部 `jiangnan-iso-v1` 资产（`feng-zhi/assets/tilesets/jiangnan-iso/` 共 8 文件 + 配套 showcase scene + scene_markup_tool 插件）。该批资产由 `image_gen` 单步生成，用户对出图质量不满意。

**本次删除仅作用于资产层；[ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md) iso projection 方向继续保留**，项目仍按 isometric diamond + 4 斜向架构推进。`jiangnan-iso` tileset 将在未来的 `S8-tileset-iso-redesign` story 中重新生产，重设方法不再依赖单一 `image_gen` 路径。

2026-07-01 复核确认：江南正式场景尚未设计，旧 `jiangnan_riverside` 平面 tileset 也不得继续作为当前过渡方案。正式江南场景必须等场景设计和 `S8-tileset-iso-redesign` 或等价资产决策后重新建立。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Asset Pipeline, TileSet Resource |
| **Knowledge Risk** | **LOW** — 不引入任何新 API；删除 `.tres` / `.png` / `.import` 是 Godot 标准操作 |
| **References Consulted** | [ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md), `assets/generated/tilesets/jiangnan-iso/` (history), `production/qa/evidence/s7-iso-pivot-foundation/pond-showcase-2026-06-24.md` (history) |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) git log 6734093 / 32f2cd5 仍可恢复历史资产；2) 当前项目源文件不再保留未设计江南占位 tileset；3) ADR-0022 §扩展点 1 的 4 斜向角色动画与 tileset 选择正交，角色侧不受影响 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md) — iso projection + 4 向方向 |
| **Enables** | 未来 `S8-tileset-iso-redesign` 拍板时，可以从一个清洁的资产基线出发，选任意生产路径（手绘 / Aseprite / 二次手绘 image_gen / 购买现成 asset） |
| **Blocks** | 所有依赖 iso tileset 的下游 story：`S8-room-jiangnan-iso-pond-test` / `S8-tileset-edge-polish` / `S8-tileset-bank-direction-completion` / `S8-tileset-terrain-bitmask` / `S8-tileset-collision-layer` / `S8-art-bible-iso-palette` — 全部 6 个 backlog story 已在 `production/sprint-status.yaml` 标 `depends-on S8-tileset-iso-redesign` |
| **Ordering Note** | `S8-tileset-iso-redesign` 必须先有 brainstorm + 路径选择决策才能进入 dev，否则 Sprint 8 的 6 个 tileset 后续 story 无法启动 |

## Context

### Problem Statement

ADR-0022 接受后，Sprint 7 同步落了 `S7-Iso-Pivot-Foundation` 故事的视觉证据 — 通过 `image_gen` 单步生成 jiangnan-iso-v1 tileset（sheet1 16 tile + sheet2 48 tile，总 64 tile，commit `6734093`），并搭建 `jiangnan_iso_pond_showcase.tscn` 作为 visual evidence（commit `32f2cd5`）。

但实际产出存在以下质量问题（部分在 `pond-showcase-2026-06-24.md` 已标已知限制）：

1. **浅色 tile 边缘白晕**：image_gen 在 water/bank/bridge 钻形内沿绘制了 `#f0f0f0` 湿润反光像素，下采样后呈细白边。`process_tileset.py` 用 `apply_diamond_mask(radius=0.85)` 激进缩 mask 清掉大部分，但浅色仍残留
2. **bank 4 斜向不齐**：sheet1 row 2 的 `bank_legacy` 4 张没按 ADR-0022 NE/SE/SW/NW 严格 4 斜向产出（sheet2 row 6 `bank_iso4` 是后补的）
3. **整体视觉调性偏差**：image_gen 出图风格与项目目标的"《大侠立志传》/《仙剑》系列 muted 江南调"不够一致
4. **资产生产路径锁死**：当前流水线（`tileset.prompt.txt` → image_gen → `process_tileset.py` → `generate_tres.py`）让所有质量改善只能走"调 prompt + 重出图"路径，回报递减

### 2026-06-24 决策语境

用户在 `production/sprint-status.yaml` `S8-tileset-edge-polish` 等 6 个 backlog 上线后表态：

> "iso tileset 方向我没有否，只是资源我不太满意，暂时先删掉做别的，后边再重新设计地块。"

明确两层语义：
- **方向层**：ADR-0022 iso projection 保留 — `IsoProjection` Foundation 单元测试、`Iso4*` 角色动画、`IIso4CharacterAnimator` 端口、cliff_cave_ground_tiles iso 蒙皮 全部继续有效
- **资产层**：jiangnan-iso-v1 这批资产质量不满意，回收清理；未来重设但**何时重设、走哪条生产路径都未拍板**

### Constraints

- **不退回正交方格** — ADR-0022 的几何投影规则（cart ↔ iso 双向变换、y-sort、4 斜向 sector）继续生效
- **不替换 ADR-0022** — 本 ADR 不修改 ADR-0022 任何决策；只补充实施路径
- **保留 git 历史可追溯** — 删除资产后，`6734093` / `32f2cd5` 仍是 history 可达，未来若要回滚或参考可 `git show` 取出
- **旧过渡 tileset 不污染长期方向** — 2026-07-01 复核后，未设计江南场景的占位 tileset 已退役；不在 art-bible / GDD 提升为正式方向

## Decision

### Decision 1：jiangnan-iso-v1 资产全删

`commit 24c34ba chore(cleanup): remove obsolete assets and plugins` 删除以下：

| 路径 | 文件数 | 删除原因 |
|---|---|---|
| `feng-zhi/assets/tilesets/jiangnan-iso/` | 8 | v1 tileset (sheet1.png + sheet2.png + .tres + .import × 2 + README + manifest + generate_tres.py) |
| `feng-zhi/scenes/showcase/jiangnan_iso_pond_showcase.tscn` | 1 | v1 showcase scene（依赖被删的 .tres） |
| `feng-zhi/scenes/test/jiangnan_tileset_tester.tscn` + `JiangnanTilesetTester.cs` | 2 | v1 测试场景 |
| `feng-zhi/addons/scene_markup_tool/` | 9 | 早期实验性插件，未被任何长期方向消费 |

### Decision 2：旧江南过渡平面 tileset 退役

`commit fbbc5be fix(tiles): tiles` 曾引入 `feng-zhi/assets/maps/jiangnan_riverside/tilesets/jiangnan_ground_tiles.tres` 作为非-iso 平面占位。2026-07-01 复核后，该占位不再代表当前工程状态：江南场景尚未正式设计，因此源文件中不继续保留这批占位资产。

**约束**：
- 不得用旧江南占位 tileset 启动正式场景开发
- 不得复用到战斗场景（battle scene 仍按 ADR-0022 iso 走）
- 江南正式场景必须由后续场景设计和资产重设任务重新产出

### Decision 3：重设地块的拍板标准（留 future story 选择）

未来 `S8-tileset-iso-redesign` 决策时遵循以下原则（**本 ADR 不替路径选择拍板**）：

| 维度 | 标准 |
|---|---|
| 生产路径 | 不再单一依赖 `image_gen` 一步出图。候选：(a) 手绘 / (b) Aseprite 工作流 / (c) 购买现成 asset + 改色 / (d) image_gen 二次手绘修整 |
| 覆盖度 | 必须涵盖 ADR-0022 §扩展点 1 的 4 斜向 NE/SE/SW/NW + bank/path/grass/water/bridge 全分类 |
| 验证场景 | 在 `BackMountainCliffCave` / 新 chapter-01 江南 explore 等**真实场景**试装；不再依赖独立 showcase scene 作为唯一验证 |
| 视觉门槛 | 浅色 tile 无白晕；4 斜向风格统一；与角色 sprite 调色板协调 |
| 调色板权威 | 重设结果同步落 `design/art/art-bible.md`（候选 `S8-art-bible-iso-palette` 消费） |

### Decision 4：6 个 S8-tileset-* backlog 全部 blocked

`production/sprint-status.yaml` 已同步落（commit `75883c6`）：

- `S8-room-jiangnan-iso-pond-test` → blocker: `depends on S8-tileset-iso-redesign + S8-tileset-edge-polish`
- `S8-tileset-edge-polish` → blocker: `depends on S8-tileset-iso-redesign (新资产到位)`
- `S8-tileset-bank-direction-completion` → blocker: `depends on S8-tileset-iso-redesign`
- `S8-tileset-terrain-bitmask` → blocker: `depends on S8-tileset-iso-redesign + bank-direction-completion`
- `S8-tileset-collision-layer` → blocker: `depends on S8-tileset-iso-redesign`
- `S8-art-bible-iso-palette` → blocker: `depends on S8-tileset-iso-redesign (调色板由重设结果决定)`

### Decision 5：本 ADR 不规定 redesign 何时启动

`S8-tileset-iso-redesign` 何时开题由 **owner**（项目方向决策方）按以下信号决定：

- 信号 A：chapter 01 江南正式开发需要 iso tileset（不再容忍占位）
- 信号 B：战斗场景需要新地形（cliff_cave 之外的）
- 信号 C：有空档窗口 / 灵感涌现愿意拍板生产路径

在 owner 拍板前，6 个 backlog story **不允许任何方启动**（避免基于"假想资产"做下游工程）。

## Consequences

### Positive

- **不让低质量资产污染 master**：v1 资产删除后，commit 历史的设计意图清洁 — 未来 contributor 不会看到"半成品 iso tileset"误以为这是正式方向参考
- **保留 ADR-0022 决策**：iso 方向不动，[ADR-0022 §扩展点 1](adr-0022-isometric-projection-and-iso4-animator.md) 的 4 斜向角色动画、`IsoProjection` Foundation 测试、battle scene iso 蒙皮 全部继续有效
- **解锁灵活生产路径**：未来重设不必绑定 image_gen，可以选最适合当前阶段的路径
- **明确占位边界**：旧江南占位资产退役 — 后续不会有人误把它"扶正"作为长期方向

### Negative

- **失去 7.5h 已投入产出**：v1 tileset + showcase scene + process_tileset.py 调参约 7.5h（含 `apply_diamond_mask` 几次迭代），现在沉没到 git history
- **Sprint 8 部分 backlog 短期 blocked**：6 个 S8-tileset-* backlog 全部 blocked on iso-redesign，Sprint 8 真正可启动的 tileset 工作 = 0
- **短期缺少江南占位素材**：江南正式场景启动前，需要先完成场景设计与资产路径选择
- **再次失败风险**：重设如果继续走 image_gen 单一路径，可能继续不满意。本 ADR 已约束「不再单一依赖 image_gen」缓解

### Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| 未来读到 `24c34ba` 删除 commit 误以为 ADR-0022 方向被否决 | High | High | **本 ADR 本身就是缓解** — `production/sprint-status.yaml` 头部 comment + 本 ADR 在 ADR 目录均明确写"方向 NOT 否决" |
| 历史江南占位资产被新人误读为"项目最终风格" | Medium | Medium | 源文件删除旧占位；ADR 中明确标为历史，不作为当前开发入口 |
| 6 个 S8-tileset-* backlog 长期 blocked 让 Sprint 8 等容量浪费 | Medium | Low | owner 在 Sprint 8 plan 时根据 redesign 拍板信号 A/B/C 决定是否优先 iso-redesign；其它 backlog（如 dialogue 内容生产、Foundation 系统深化）足以填充 Sprint 8 工时 |
| 重设方向决策长期挂在 owner 那边卡 6 个 backlog | Medium | Medium | 在 Sprint 8 plan / Sprint 9 plan 主动 surface 此决策；不主动启动 iso-redesign 但每个 sprint plan 时检视一次 |

## Migration Plan

| 阶段 | 内容 | 状态 |
|---|---|---|
| 1 | 删除 v1 资产 + showcase + 测试场景 + scene_markup_tool 插件 | ✅ commit 24c34ba |
| 2 | 同步过渡 tileset `jiangnan_ground_tiles.tres` 落到 chapter_00 explore | ✅ 历史 commit fbbc5be；2026-07-01 已退役 |
| 3 | `production/sprint-status.yaml` 6 个 S8-tileset-* backlog 加 `depends-on S8-tileset-iso-redesign` blocker + 头部 comment 块说明方向 NOT 否决 | ✅ commit 75883c6 |
| 4 | 本 ADR 接受 + 链接到 ADR-0022 / sprint-status.yaml backlog | ✅ commit （本提交） |
| 5 | owner 信号 A/B/C 任一触发时启动 `S8-tileset-iso-redesign` brainstorm story | ⏳ 待 owner 拍板 |
| 6 | redesign 完成后：6 个 S8-tileset-* backlog 解除 blocker + 正式 tileset 接入 | ⏳ 留 redesign 完成后 |

## Validation Criteria

1. ✅ git log `6734093` / `32f2cd5` 仍可恢复 v1 资产历史（`git show 6734093:feng-zhi/assets/tilesets/jiangnan-iso/jiangnan_iso_tileset.tres` 仍可读）
2. ✅ ADR-0022 内容**未被修改**（本 ADR 不动 ADR-0022 任何字段）
3. ✅ Foundation `IsoProjection` + `Iso4*` 测试全部继续通过（与 tileset 资产正交）
4. ✅ 当前项目源文件不再保留未设计江南占位 tileset（2026-07-01 复核）
5. ✅ `production/sprint-status.yaml` 6 个 S8-tileset-* backlog 全部 `blocker` 字段含 `depends on S8-tileset-iso-redesign`（commit `75883c6` 已落）
6. ⏳ owner 在 Sprint 8 plan 时主动检视 iso-redesign 是否启动（plan-time review，非 ADR 一次性验证）

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| art-bible.md Reference Board | 《大侠立志传》/《仙剑》系列 isometric muted 调性 | 不修改方向；明确占位 tileset 不进 art-bible，未来重设结果由 `S8-art-bible-iso-palette` 落 art-bible |
| map-scene-management.md | 场景视觉一致性 | 过渡期视觉错位记入 risk；redesign 完成后恢复一致 |
| combat-system.md §战棋空间规则 | iso diamond + 4 朝向 | ADR-0022 继续生效；本 ADR 不动战棋空间规则 |

## Related Decisions

- [ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md) — **核心依赖**，方向继续生效，本 ADR 仅调整实施路径
- [ADR-0010](adr-0010-tilemaplayer-usage.md) — TileMapLayer 五层结构，与 tileset 资产选择正交，不受影响
- [ADR-0020](adr-0020-pure-2d-wuxia-rendering-direction.md) — 纯 2D 路线，与 tileset 重设无关
- [ADR-0021](adr-0021-character-animation-port.md) — `ICharacterAnimator` 端口，与 tileset 正交

## Open Questions（留 future story / ADR）

- **Q1**：`S8-tileset-iso-redesign` brainstorm story 应何时开题？— 由 owner 按信号 A/B/C 决定，本 ADR 不规定
- **Q2**：重设的生产路径选哪条（手绘 / Aseprite / 购买 / image_gen 二次手绘）？— 留 brainstorm story 评估
- **Q3**：是否需要新的临时江南占位？— 当前不需要；正式江南场景启动前先做场景设计和资产路径选择
- **Q4**：如果重设后想保留 v1 某些 tile（例如部分 water / bridge）作参考混入，是否要恢复部分文件？— 由 redesign 任务自然决定，不在本 ADR 预设
