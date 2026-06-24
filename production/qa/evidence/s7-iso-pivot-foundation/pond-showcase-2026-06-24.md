# S7-Iso-Pivot-Foundation · Jiangnan Iso Pond Showcase Evidence

> **生成日期**: 2026-06-24
> **作者**: AI agent (codex/cursor)
> **关联 story**: `S7-Iso-Pivot-Foundation` (DoD §视觉证据补充)
> **关联资产**: `jiangnan-iso-v1` 64-tile 双 atlas tileset (commit pending after this evidence)

## 目的

验证 `S7-Iso-Pivot-Foundation` 落地的 isometric diamond projection (`tile_size=64×32`,
`tile_shape=Isometric`, `tile_layout=DiamondDown`) 在 `feng-zhi` Godot 4.7-stable
工作树里**端到端可用**：

- TileSet `.tres` 配置 (双 atlas source, custom_data_0 = tile_name)
- TileMapLayer 程序化填充 + 装配 (PackedScene + ResourceSaver)
- Iso 渲染 (Camera2D + y_sort + nearest filter pixel-art)
- 多 atlas source 在同一 scene 共存

并为 64-tile `jiangnan-iso-v1` tileset 提供首张 in-engine 渲染证据。

## 工件

| 工件 | 路径 |
|---|---|
| Showcase scene | `feng-zhi/scenes/showcase/jiangnan_iso_pond_showcase.tscn` |
| Scene 生成器 (可重跑) | `tools/build_godot_jiangnan_iso_pond_showcase.py` |
| 截图自动化脚本 | `tools/capture_jiangnan_iso_pond_showcase.gd` |
| Tileset .tres | `feng-zhi/assets/tilesets/jiangnan-iso/jiangnan_iso_tileset.tres` |
| Godot-ready PNG sheet1 | `feng-zhi/assets/tilesets/jiangnan-iso/jiangnan-iso-tileset-sheet1.png` |
| Godot-ready PNG sheet2 | `feng-zhi/assets/tilesets/jiangnan-iso/jiangnan-iso-tileset-sheet2.png` |
| **In-engine 截图** | `production/qa/evidence/s7-iso-pivot-foundation/jiangnan_iso_pond_showcase.png` |

## 场景布局 (7×5 = 35 cells, 单层 TileMapLayer)

```
        col 0           col 1           col 2           col 3           col 4               col 5             col 6
row 0:  grass           grass           path_end_s      grass           grass               grass             grass
row 1:  grass           bank_iso_nw     bank_half_n     bank_iso_ne     grass               corner_inner_full grass
row 2:  ground_cobble   water_shallow   water_deep      water_shallow   bridge_arch_mid     grass             grass
row 3:  grass           bank_iso_sw     bank_half_s     bank_iso_se     grass               corner_outer_full grass
row 4:  grass           grass           grass           grass           grass               grass             grass
```

涵盖 12 个 tile 类别，跨双 atlas source：
- **sheet1 (source_id=0)**: `ground_grass_moss`, `water_shallow`, `water_deep`
- **sheet2 (source_id=1)**: `ground_cobblestone`, `path_grass_end_s`, `bank_iso_{nw,ne,sw,se}`, `bank_half_{north,south}_land`, `bridge_stone_arch_mid`, `corner_inner_full`, `corner_outer_full`

## 自动化流程

```bash
# 1) 生成 / 重生成 scene (改 LAYOUT 后重跑)
python3 tools/build_godot_jiangnan_iso_pond_showcase.py
# 输出: feng-zhi/scenes/showcase/jiangnan_iso_pond_showcase.tscn

# 2) 截图 (SubViewport 离屏渲染, 1600×900)
godot --path feng-zhi \
      --script ../tools/capture_jiangnan_iso_pond_showcase.gd \
      --quit-after 90
# 输出: production/qa/evidence/s7-iso-pivot-foundation/jiangnan_iso_pond_showcase.png
```

两步全程零交互。`build_*.py` 用 PackedScene + ResourceSaver 模式（参考
`tools/build_godot_back_mountain_cliff_cave_tile_layers.py`），`capture_*.gd`
在 SubViewport 里渲染 + `Image.save_png()`。

## 验证清单

| 项 | 期望 | 结果 |
|---|---|---|
| `godot --headless --import` 双 atlas PNG | 零 error | ✅ DONE |
| `tools/build_godot_jiangnan_iso_pond_showcase.py` 成功保存 .tscn | `Saved res://...tscn (cells=35)` | ✅ |
| Scene 重打开后 TileMapLayer.tile_map_data 35 cells 全部正确 | base64 byte 长度对应 35 records | ✅ |
| `capture` 脚本输出 1600×900 PNG | PNG image data, 8-bit/color RGB | ✅ |
| In-engine 截图：iso 钻形网格清晰 | 35 cells 全部呈现 diamond + 正确 z_order | ✅ (见截图) |
| 双 atlas source 同一 layer 内混用 | sheet1 + sheet2 tile 都正确渲染 | ✅ |
| 池塘区域可辨识 | 3×3 jade water + 4 角 bank + 2 边 half-bank | ✅ |
| 1×1 微池 + 1×1 微岛 + 拱桥 + 卵石路 + 草地径 | 全部呈现 | ✅ |

## 已知视觉限制 (image_gen 源质量)

浅色 tile (`water_*`, `bank_*`, `bridge_*`) 钻形内沿有 1-2 px 接近 `#f0f0f0`
的"湿润反光"像素，下采样后呈现为细白边。深色 ground tile (`grass`, `bluestone`,
`mud`, `mossy_heavy`) 完全干净。

已用 `process_tileset.py:apply_diamond_mask(radius=0.85)` 激进缩 mask 把大部分
边缘像素清掉；Showcase 截图可见浅色 tile 仍残留细白边，但**钻形结构清晰可辨**，
作为 S7-Iso-Pivot-Foundation **end-to-end iso projection 可工作**的证据足够。

后续 Sprint 8 候选 story `tileset-edge-polish` 用手绘或更高质量重生成对浅色
tile 做边缘清理。

## 与 BackMountainCliffCave 兄弟工件的关系

| 资产 | 用途 | 阶段 |
|---|---|---|
| `BackMountainCliffCaveTileLayers.tscn` | 真实 explore 场景，`.tmx` → tile_map_data 自动翻译 | S8-Explore-TileMap-Adoption (in-progress) |
| `jiangnan_iso_pond_showcase.tscn` (本工件) | jiangnan-iso 64-tile tileset 渲染验证 + multi-atlas 验证 | S7-Iso-Pivot-Foundation (visual evidence) |

两个工件共同证明 ADR-0022 iso projection 在 Godot 4.7-stable **同时支持**：
1. 单 atlas source + 大型 multi-layer scene (cliff_cave)
2. 多 atlas source + 单 layer 程序化 scene (jiangnan iso pond)

## 关联文档

- `docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md` — iso projection 规约
- `docs/architecture/adr-0010-tilemaplayer-usage.md` — TileMapLayer 使用约束
- `feng-zhi/assets/tilesets/jiangnan-iso/README.md` — tileset 用户文档
- `assets/generated/tilesets/jiangnan-iso/tileset.manifest.json` — 64-tile 完整元数据
