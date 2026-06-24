# 江南等距 tileset (jiangnan-iso-v1) — 64 tiles, 2 atlas pages

> **生成日期**: 2026-06-23
> **来源**: `/generate2dmap` skill, `tile_mode + tilemap + Godot_TileMap` 选型
> **对齐**: ADR-0022 isometric diamond projection (`tile_size = 64×32`, `tile_shape = Isometric`, `tile_layout = DiamondDown`)
> **覆盖**: 64 tiles = sheet1 16 + sheet2 48

## Atlas Page 总览

| Page | 标题 | Grid | 总 tile | Godot-ready PNG | 原生尺寸 |
|---|---|---|---|---|---|
| **sheet1_base** | 基础地面 / 水域 / 岸 / 转角 | 4×4 | 16 | `jiangnan-iso-tileset-64x32.png` | 256×128 |
| **sheet2_supplement** | 扩展地面 / 路径 / 桥 / 水变体 / 4 斜向岸 / 转角补全 | 6×8 | 48 | `jiangnan-iso-tileset-supplement-64x32.png` | 384×256 |

## 文件清单

### Page 1 (sheet1, 16 tiles)

| 文件 | 尺寸 | 用途 |
|---|---|---|
| `jiangnan-iso-tileset-4x4.png` | 1536×1024 RGB | image_gen 原始输出（白底） |
| `jiangnan-iso-tileset-source-rgba.png` | 1536×1024 RGBA | chroma-key 后主源 |
| `jiangnan-iso-tileset-1024x512.png` | 1024×512 RGBA | 4× QA 预览 |
| **`jiangnan-iso-tileset-64x32.png`** | **256×128 RGBA** | **Godot 4 TileMap 导入** |

### Page 2 (sheet2, 48 tiles)

| 文件 | 尺寸 | 用途 |
|---|---|---|
| `jiangnan-iso-tileset-6x8-supplement-raw.png` | 1536×1024 RGB | image_gen 原始输出 |
| `jiangnan-iso-tileset-supplement-source-rgba.png` | 1536×1024 RGBA | chroma-key 后主源 |
| `jiangnan-iso-tileset-supplement-1536x1024.png` | 1536×1024 RGBA | 4× QA 预览 |
| **`jiangnan-iso-tileset-supplement-64x32.png`** | **384×256 RGBA** | **Godot 4 TileMap 导入** |

### 共享

| 文件 | 内容 |
|---|---|
| `tileset.prompt.txt` | 两张 sheet 的 image_gen 原始提示词 + 生成笔记 |
| `tileset.manifest.json` | 64 个 tile 的命名 / atlas 坐标 / 用例 / 限制 |
| `process_tileset.py` | 后处理脚本：chroma-key + 下采样（可重跑） |

## 4×4 布局速查 (Page 1, sheet1)

```
Row 0  | bluestone-A   | bluestone-B   | mud          | grass-moss    |
Row 1  | water-shallow | water-deep    | water-grad   | water-strip   |
Row 2  | bank-N (v1)   | bank-S (v1)   | bank-N (v2)  | bank-NE/SW    |
Row 3  | corner-in-N   | corner-in-E   | corner-out-SW| corner-out-SE |
```

## 6×8 布局速查 (Page 2, sheet2)

```
Row 0  | stone-road   | cobble       | wood-light   | wood-dark    | sand        | mossy-heavy   |
Row 1  | path-NW-SE   | path-NE-SW   | path-cross   | path-T       | path-cr-NW  | path-cr-NE    |
Row 2  | path-cr-SE   | path-cr-SW   | path-end-N   | path-end-S   | path-end-E  | path-end-W    |
Row 3  | stone-NW-SE  | stone-NE-SW  | stepping     | stone-gL     | stone-gR    | threshold     |
Row 4  | bridge-NW-SE | bridge-NE-SW | bridge-rL    | bridge-rR    | arch-mid    | arch-pillar   |
Row 5  | lotus        | reeds        | stream-NWSE  | stream-NESW  | still-pond  | dock          |
Row 6  | bank-iso-NE  | bank-iso-SE  | bank-iso-SW  | bank-iso-NW  | half-N-land | half-S-land   |
Row 7  | corner-in-SW | corner-in-SE | inner-full   | corner-ou-NW | corner-ou-NE| outer-full    |
```

详细 ID / 别名 / 说明见 `tileset.manifest.json` `tiles_sheet1[]` 与 `tiles_sheet2[]`。

## Godot 4.7 集成步骤

1. 把两张 Godot-ready PNG 复制到 `feng-zhi/assets/tilesets/jiangnan-iso/`（项目 Godot 资源根）：
   - `jiangnan-iso-tileset-64x32.png`（sheet1, 16 tiles）
   - `jiangnan-iso-tileset-supplement-64x32.png`（sheet2, 48 tiles）
2. Godot 编辑器新建一个 `TileSet` (`.tres`)，设置：
   - `Tile Shape = Isometric`
   - `Tile Layout = Diamond Down`
   - `Tile Size = (64, 32)`
3. 在该 `TileSet` 上 **添加 2 个 Atlas Source**，分别指向两张 PNG。每个 source：
   - `Texture Region Size = (64, 32)`
   - 全选 atlas 网格一次性 "Create Tiles"
4. （可选）用 Godot Terrain set 给 `bank_iso4` / `corner` / `path_grass*` 等系列配 bitmask 实现 autotile。manifest 暂未含 bitmask 数据。
5. （可选）用 `physics_layer` + `physics_polygon` 给 `water_*` / `bridge_*` / `bridge_stone_arch_pillar` 等配水陆碰撞。
6. 新建场景挂 `TileMapLayer`（Godot 4.7 推荐），把 `TileSet` 资源引到 layer 的 `tile_set` 字段。

## 用例索引

参考 manifest `usage_examples`：

| 用例 | 关键 tile |
|---|---|
| **2×2 全 iso 水池** | `bank_iso_ne/se/sw/nw` + `water_deep` |
| **3×3 池塘+拱桥** | 8 个 bank/corner + `water_deep` + `bridge_stone_arch_mid/pillar` |
| **1×1 微池** | `corner_inner_full` 单块 |
| **1×1 微岛** | `corner_outer_full` 单块 |
| **村落小径网** | `path_grass_*` 12 件（NW-SE / NE-SW / +/T / 4 corner / 4 end） |
| **石阶 vs 土路阶层** | `stone_path_*` 6 件 + `path_grass_*` 12 件混用 |
| **木桥/拱桥** | `bridge_wood_*` + `bridge_stone_arch_*` |
| **荷塘场景** | `water_lotus` + `water_reeds` + `water_still_pond` |

## 已知限制

1. **sheet1 row 2 `bank_legacy` 朝向不严格**：image_gen 没按 ADR-0022 NE/SE/SW/NW 严格 4 斜向产出，而是 ~"land-top vs water-top" 2 类对称 + 2 个变体。**已用 sheet2 row 6 `bank_iso4` 4 张严格斜向补齐**，sheet1 row 2 保留作低保真备份。
2. **sheet2 row 4 木桥与 row 0 木地板视觉相近**：image_gen 把"无栏木桥"画成与"木地板"几乎一样的纹理。使用时配合 Godot terrain bit / object layer / collision layer 区分。
3. **sheet2 row 5 col 2-3 流水方向线被画成路面标线状的十字**：可作为石阶水路或装饰用；纯流水推荐 `water_still_pond` (col 4) 或 sheet1 的 `water_shallow / water_deep`。
4. **Diamond inset**：菱形没占满 64×32 cell（上下少量透明 padding）。导入不影响功能，与严格满格 tile 混用时需 SubTexture origin 校准。
5. **无 autotile bitmask**：留给后续 room story 在 Godot Terrain set 里配。
6. **无 collision polygon**：room scene 用 `physics_layer` + `physics_polygon` 或外挂 Area2D 区分水陆/桥/楼梯。

## 后处理工具

`process_tileset.py` 可重跑：

```bash
python3 assets/generated/tilesets/jiangnan-iso/process_tileset.py
```

依赖 `Pillow`。脚本对每张 sheet 输出 3 个文件（alpha source / 4× preview / Godot-ready 64×32）。

## 下一步候选 story (Sprint 8+)

- **room-jiangnan-iso-pond-test**：用本 tileset 在 `feng-zhi/scenes/` 建一个 2×2 / 3×3 池塘 + 木桥的测试 room，验证 ADR-0022 iso projection + 多 atlas source 实际导入效果
- **tileset-bank-direction-completion**：image_gen 第二次专项 pass 把 sheet1 row 2 `bank_legacy` 重置为严格 4 斜向（或干脆弃用 sheet1 row 2，全用 sheet2 row 6）
- **tileset-terrain-bitmask**：用 Godot 4 Terrain set 给 `bank_iso4` / `corner` / `path_grass*` 配 autotile bitmask
- **tileset-collision-layer**：给 `water_*` / `bridge_*` / `corner_*` 配 collision polygon，让 NPC AI 能判断水陆
- **art-bible-iso-palette**：把本 tileset 用的 muted Jiangnan palette 写进 `design/art/art-bible.md`

## 关联工件

- `docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md` — iso projection 规约
- `docs/architecture/adr-0010-tilemaplayer-usage.md` — TileMapLayer 使用约束
- `src/FengZhi.Foundation/Geometry/IsoProjection.cs` — cart ↔ screen 投影 (64×32)
- `design/gdd/map-scene-management.md` — 场景管理 GDD
