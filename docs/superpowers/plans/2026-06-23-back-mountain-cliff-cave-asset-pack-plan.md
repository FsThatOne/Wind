# 后山崖洞资产包实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 生成“后山崖洞”首版完整资产包：64x32 等距洞内 tile 图集、透明物件 PNG、复用现有 iso4 主角行走帧、两份 Tiled `.tmx` 地图与 QA 预览。

**架构：** 可见美术资产必须来自 `image_gen`，本地脚本只负责切片、透明清理、拼接预览、`.tmx` 写入和验证。地图使用同一套 tileset / props，输出 day 与 night 两份独立 `.tmx`，通过逻辑对象属性表达剧情状态差异。主角行走图集复用已存在的 `feng-zhi/assets/character/main_character_iso4/walk/*.png`。

**技术栈：** Godot 4.7-stable、Tiled `.tmx` XML、PNG32/RGBA、Python 3 + Pillow、内置 `image_gen`、现有 `generate2dmap` / `generate2dsprite` 工作流。

## 全局约束

- Tile 规格固定为 `64x32`，2:1 等距菱形。
- 首版地图固定为 `16x16`。
- 美术风格固定为 `16-bit 像素武侠`。
- 洞穴定位固定为“风止山庄后山藏酒储物洞 + 主角和师姐秘密基地”，禁止做成矿洞、遗迹、怪物巢穴、藏宝室或战斗副本。
- 可见美术资产必须由 `image_gen` 生成或复用既有主角 PNG；不得用 PIL / SVG / Canvas 程序绘制最终美术。
- 本地脚本只能用于后处理、切片、组装、`.tmx` 写入、预览和验证。
- 不提交 commit，除非用户另行明确要求。
- 写入资产文件前遵守项目协作协议：如果执行者要新增或修改本计划未列出的路径，必须先询问用户。

---

## 文件结构

### 新增目录

- `assets/generated/maps/back-mountain-cliff-cave/`
  - 首版资产包根目录。
- `assets/generated/maps/back-mountain-cliff-cave/prompts/`
  - 保存所有手写 image generation prompts。
- `assets/generated/maps/back-mountain-cliff-cave/raw/`
  - 保存 image_gen 原图副本。
- `assets/generated/maps/back-mountain-cliff-cave/tilesets/`
  - 保存最终 tile 图集和 tileset 元数据。
- `assets/generated/maps/back-mountain-cliff-cave/props/`
  - 保存最终透明物件 PNG。
- `assets/generated/maps/back-mountain-cliff-cave/characters/`
  - 保存从现有主角 iso4 帧组装出的 atlas 副本。
- `assets/generated/maps/back-mountain-cliff-cave/maps/`
  - 保存 `.tmx` 和 `.tsx`。
- `assets/generated/maps/back-mountain-cliff-cave/previews/`
  - 保存拼接预览、day/night 地图预览、碰撞/逻辑标记调试预览。
- `assets/generated/maps/back-mountain-cliff-cave/tools/`
  - 保存本资产包专用后处理与验证脚本。

### 新增文件

- `assets/generated/maps/back-mountain-cliff-cave/manifest.json`
  - 记录资产包名称、规格、输出文件、验证结果。
- `assets/generated/maps/back-mountain-cliff-cave/prompts/tileset.prompt.txt`
  - 地面 tile 图集 image_gen prompt。
- `assets/generated/maps/back-mountain-cliff-cave/prompts/props.prompt.txt`
  - 物件图集 image_gen prompt。
- `assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.png`
  - 最终 6x4、每格 64x32 的 PNG32 tile atlas。
- `assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.json`
  - tile id、名称、分类、默认碰撞说明。
- `assets/generated/maps/back-mountain-cliff-cave/props/*.png`
  - 10 个透明物件。
- `assets/generated/maps/back-mountain-cliff-cave/characters/main_character_iso4_walk_atlas.png`
  - 4x4、每格 128x128 的主角行走 atlas。
- `assets/generated/maps/back-mountain-cliff-cave/maps/cliff_cave_ground_tiles.tsx`
  - Tiled 外部 tileset。
- `assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_day.tmx`
  - 日常版地图。
- `assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_night.tmx`
  - 灭门夜版地图。
- `assets/generated/maps/back-mountain-cliff-cave/tools/assemble_character_atlas.py`
  - 复用现有 iso4 主角帧并组装 atlas。
- `assets/generated/maps/back-mountain-cliff-cave/tools/build_tmx.py`
  - 写入 `.tsx` 和两份 `.tmx`。
- `assets/generated/maps/back-mountain-cliff-cave/tools/validate_pack.py`
  - 验证 PNG、地图 XML、图层名、marker、路径可达性。

### 复用文件

- `feng-zhi/assets/character/main_character_iso4/walk/walk_ne_00.png` 到 `walk_nw_03.png`
  - 现有主角 iso4 行走帧，16 张，`128x128 RGBA`。

---

### Task 1: 建立资产包目录、清单和验证脚本骨架

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/manifest.json`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/tools/validate_pack.py`

**接口：**
- 产出：资产包根目录和 `manifest.json`，后续任务持续更新。
- 产出：`validate_pack.py --root assets/generated/maps/back-mountain-cliff-cave` 验证命令。

- [ ] **Step 1: 创建目录结构**

运行：

```bash
mkdir -p assets/generated/maps/back-mountain-cliff-cave/{prompts,raw,tilesets,props,characters,maps,previews,tools}
```

预期：命令退出码为 0。

- [ ] **Step 2: 写入初始 `manifest.json`**

写入文件 `assets/generated/maps/back-mountain-cliff-cave/manifest.json`：

```json
{
  "name": "back-mountain-cliff-cave",
  "scene_name_zh": "后山崖洞",
  "date": "2026-06-23",
  "art_style": "16-bit pixel wuxia",
  "tile": {
    "width": 64,
    "height": 32,
    "projection": "isometric_diamond_2_to_1",
    "atlas_columns": 6,
    "atlas_rows": 4
  },
  "map": {
    "width_tiles": 16,
    "height_tiles": 16,
    "orientation": "isometric",
    "variants": ["day", "night"]
  },
  "character": {
    "source": "feng-zhi/assets/character/main_character_iso4/walk",
    "cell_width": 128,
    "cell_height": 128,
    "directions": ["walk_ne", "walk_se", "walk_sw", "walk_nw"],
    "frames_per_direction": 4
  },
  "outputs": {
    "tileset_png": "tilesets/cliff_cave_ground_tiles.png",
    "tileset_json": "tilesets/cliff_cave_ground_tiles.json",
    "character_atlas": "characters/main_character_iso4_walk_atlas.png",
    "tsx": "maps/cliff_cave_ground_tiles.tsx",
    "day_tmx": "maps/back_mountain_cliff_cave_day.tmx",
    "night_tmx": "maps/back_mountain_cliff_cave_night.tmx"
  },
  "validation": {
    "last_run": "",
    "status": "not_run",
    "notes": []
  }
}
```

- [ ] **Step 3: 写入验证脚本**

写入文件 `assets/generated/maps/back-mountain-cliff-cave/tools/validate_pack.py`：

```python
#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from collections import deque
from pathlib import Path

from PIL import Image

REQUIRED_LAYERS = ["Ground", "Terrain", "Structures", "Overlay", "Collision", "LogicMarkers"]
REQUIRED_MARKERS = {
    "exit_to_back_mountain",
    "wine_pickup",
    "storage_shelf",
    "memory_marker",
    "rest_spot",
    "blocked_rock",
    "blocked_storage",
}


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    sys.exit(1)


def check_png(path: Path, expected_size: tuple[int, int] | None = None) -> None:
    if not path.exists():
        fail(f"missing PNG: {path}")
    with Image.open(path) as image:
        if image.mode != "RGBA":
            fail(f"{path} mode is {image.mode}, expected RGBA")
        if expected_size is not None and image.size != expected_size:
            fail(f"{path} size is {image.size}, expected {expected_size}")


def gid_grid(csv_text: str, width: int, height: int) -> list[list[int]]:
    values = [int(item.strip()) for item in csv_text.replace("\n", "").split(",") if item.strip()]
    if len(values) != width * height:
        fail(f"tile layer has {len(values)} gids, expected {width * height}")
    return [values[row * width:(row + 1) * width] for row in range(height)]


def marker_names(root: ET.Element) -> set[str]:
    found: set[str] = set()
    for obj in root.findall(".//objectgroup[@name='LogicMarkers']/object"):
        name = obj.attrib.get("name", "")
        if name:
            found.add(name)
    return found


def collision_blocked(root: ET.Element, width: int, height: int) -> set[tuple[int, int]]:
    blocked: set[tuple[int, int]] = set()
    layer = root.find(".//layer[@name='Collision']")
    if layer is not None:
        data = layer.find("data")
        if data is not None and data.text:
            grid = gid_grid(data.text, width, height)
            for y, row in enumerate(grid):
                for x, gid in enumerate(row):
                    if gid:
                        blocked.add((x, y))

    # Object blockers may use tile-space custom properties.
    for obj in root.findall(".//objectgroup[@name='LogicMarkers']/object"):
        obj_type = obj.attrib.get("type", "")
        if obj_type == "blocker":
            props = {p.attrib.get("name"): p.attrib.get("value") for p in obj.findall("./properties/property")}
            x = props.get("tile_x")
            y = props.get("tile_y")
            if x is not None and y is not None:
                blocked.add((int(x), int(y)))
    return blocked


def has_path(root: ET.Element, width: int, height: int) -> None:
    blocked = collision_blocked(root, width, height)
    targets = {}
    for obj in root.findall(".//objectgroup[@name='LogicMarkers']/object"):
        props = {p.attrib.get("name"): p.attrib.get("value") for p in obj.findall("./properties/property")}
        if "tile_x" in props and "tile_y" in props:
            targets[obj.attrib.get("name", "")] = (int(props["tile_x"]), int(props["tile_y"]))

    start = targets.get("exit_to_back_mountain")
    wine = targets.get("wine_pickup")
    rest = targets.get("rest_spot")
    if start is None or wine is None or rest is None:
        fail("path check needs exit_to_back_mountain, wine_pickup, and rest_spot tile_x/tile_y")

    reachable = flood_fill(start, blocked, width, height)
    if wine not in reachable:
        fail(f"wine_pickup at {wine} is not reachable from exit {start}")
    if rest not in reachable:
        fail(f"rest_spot at {rest} is not reachable from exit {start}")


def flood_fill(start: tuple[int, int], blocked: set[tuple[int, int]], width: int, height: int) -> set[tuple[int, int]]:
    queue: deque[tuple[int, int]] = deque([start])
    seen: set[tuple[int, int]] = set()
    while queue:
        cell = queue.popleft()
        if cell in seen or cell in blocked:
            continue
        x, y = cell
        if x < 0 or y < 0 or x >= width or y >= height:
            continue
        seen.add(cell)
        queue.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    return seen


def validate_tmx(path: Path) -> None:
    if not path.exists():
        fail(f"missing TMX: {path}")
    tree = ET.parse(path)
    root = tree.getroot()
    if root.attrib.get("orientation") != "isometric":
        fail(f"{path} orientation is {root.attrib.get('orientation')}, expected isometric")
    width = int(root.attrib["width"])
    height = int(root.attrib["height"])
    if (width, height) != (16, 16):
        fail(f"{path} map size is {(width, height)}, expected (16, 16)")
    if (int(root.attrib["tilewidth"]), int(root.attrib["tileheight"])) != (64, 32):
        fail(f"{path} tile size mismatch")

    layers = [layer.attrib.get("name") for layer in root.findall("layer")]
    object_layers = [group.attrib.get("name") for group in root.findall("objectgroup")]
    combined = layers + object_layers
    for required in REQUIRED_LAYERS:
        if required not in combined:
            fail(f"{path} missing layer {required}")

    missing = REQUIRED_MARKERS - marker_names(root)
    if missing:
        fail(f"{path} missing logic markers: {sorted(missing)}")
    has_path(root, width, height)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    args = parser.parse_args()

    root = Path(args.root)
    manifest_path = root / "manifest.json"
    if not manifest_path.exists():
        fail(f"missing manifest: {manifest_path}")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

    check_png(root / manifest["outputs"]["tileset_png"], (384, 128))
    check_png(root / manifest["outputs"]["character_atlas"], (512, 512))

    for prop_name in [
        "wine_jar_single.png",
        "wine_jars_group.png",
        "storage_shelf.png",
        "bamboo_basket_herbs.png",
        "cloth_bundle.png",
        "wooden_crate_low.png",
        "rest_mat.png",
        "small_stool.png",
        "sister_mark.png",
        "oil_lamp_dim.png",
    ]:
        check_png(root / "props" / prop_name)

    validate_tmx(root / manifest["outputs"]["day_tmx"])
    validate_tmx(root / manifest["outputs"]["night_tmx"])
    print("PASS: back-mountain-cliff-cave asset pack validation")


if __name__ == "__main__":
    main()
```

- [ ] **Step 4: 运行验证脚本确认当前失败**

运行：

```bash
python3 assets/generated/maps/back-mountain-cliff-cave/tools/validate_pack.py --root assets/generated/maps/back-mountain-cliff-cave
```

预期：失败，输出包含 `missing PNG: assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.png`。这是正确状态，因为资产还没生成。

---

### Task 2: 生成并整理 64x32 洞内 tile 图集

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/prompts/tileset.prompt.txt`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/raw/cliff_cave_ground_tiles_raw.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.json`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/previews/tileset_repeat_preview.png`

**接口：**
- 产出：6x4 tile atlas，尺寸 `384x128`，每 tile `64x32`。
- 产出：Tiled `.tsx` 后续可引用的 tile id 清单。

- [ ] **Step 1: 写入 tileset prompt**

写入 `assets/generated/maps/back-mountain-cliff-cave/prompts/tileset.prompt.txt`：

```text
Create a 16-bit pixel art isometric diamond tileset for a Chinese wuxia RPG cave interior.

Exact sheet contract:
- PNG image, 384x128 pixels.
- 6 columns x 4 rows.
- Each tile is exactly 64x32 pixels.
- 2:1 isometric diamond footprint.
- All 24 tiles must be seamless or edge-compatible.
- No labels, no text, no numbers, no UI, no characters.

Scene identity:
This is not a dungeon, mine, ruin, monster cave, or treasure room. It is a lived-in storage cave behind FengZhi Manor, used to store medicinal wine and supplies, and also used as a secret base by the protagonist and his senior sister.

Art style:
16-bit pixel wuxia, readable pixel edges, restrained ink-and-stone palette, warm domestic undertone, cave stone in low saturation gray-green and muted ochre, small moss and wet stone details. Avoid modern HD painting, blur, painterly smears, noisy textures, and realistic rendering.

Tile list, left to right, top to bottom:
1 dry stone floor
2 wet stone floor
3 flatter storage-area stone floor
4 moss-marked stone floor
5 pebble scatter floor
6 shallow water puddle floor
7 muddy water edge
8 dark corner shadow floor
9 cave wall foot line north-east edge
10 cave wall foot line north-west edge
11 low stone ledge edge
12 wooden floor pad under storage
13 cave wall shadow transition
14 worn path stone
15 cleaner secret-base nook stone
16 wine-jar placement stain
17 rest-mat floor imprint
18 subtle warm-lamp floor glow
19 damp cave crack floor
20 small moss corner floor
21 storage scuff floor
22 darker rear-cave floor
23 shallow water highlight
24 plain fallback dry stone floor

Keep every tile aligned to its own 64x32 cell. Do not let art cross cell boundaries except exact seamless edge continuation.
```

- [ ] **Step 2: 使用 `image_gen` 生成 tile 图集**

调用内置 `image_gen`，prompt 使用 Step 1 的完整文本。

预期：生成一张 6x4、64x32 tile atlas 概念图。若生成图不是 `384x128`，下一步用确定性缩放/裁切规范化，但不得改变 6x4 结构。

- [ ] **Step 3: 保存原图副本**

将 image_gen 输出的 PNG 复制为：

```text
assets/generated/maps/back-mountain-cliff-cave/raw/cliff_cave_ground_tiles_raw.png
```

复制后用以下命令检查：

```bash
python3 - <<'PY'
from PIL import Image
path = "assets/generated/maps/back-mountain-cliff-cave/raw/cliff_cave_ground_tiles_raw.png"
im = Image.open(path)
print(path, im.size, im.mode)
PY
```

预期：输出包含文件路径，模式为 `RGBA` 或可转换为 `RGBA`。

- [ ] **Step 4: 规范化图集尺寸**

如果原图已经是 `384x128`，直接转 RGBA 保存为 `tilesets/cliff_cave_ground_tiles.png`。如果不是，按整图缩放到 `384x128`，保留 6x4 网格，不单独重排 tile。

运行：

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image

root = Path("assets/generated/maps/back-mountain-cliff-cave")
src = root / "raw/cliff_cave_ground_tiles_raw.png"
dst = root / "tilesets/cliff_cave_ground_tiles.png"
im = Image.open(src).convert("RGBA")
if im.size != (384, 128):
    im = im.resize((384, 128), Image.Resampling.NEAREST)
im.save(dst)
print(dst, Image.open(dst).size, Image.open(dst).mode)
PY
```

预期：输出 `assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.png (384, 128) RGBA`。

- [ ] **Step 5: 写入 tile 元数据**

写入 `assets/generated/maps/back-mountain-cliff-cave/tilesets/cliff_cave_ground_tiles.json`：

```json
{
  "tile_width": 64,
  "tile_height": 32,
  "columns": 6,
  "rows": 4,
  "tiles": [
    {"id": 0, "name": "dry_stone_floor", "category": "ground", "walkable": true},
    {"id": 1, "name": "wet_stone_floor", "category": "ground", "walkable": true},
    {"id": 2, "name": "storage_flat_stone_floor", "category": "ground", "walkable": true},
    {"id": 3, "name": "moss_marked_stone_floor", "category": "ground", "walkable": true},
    {"id": 4, "name": "pebble_scatter_floor", "category": "terrain", "walkable": true},
    {"id": 5, "name": "shallow_water_puddle_floor", "category": "terrain", "walkable": true},
    {"id": 6, "name": "muddy_water_edge", "category": "terrain", "walkable": true},
    {"id": 7, "name": "dark_corner_shadow_floor", "category": "terrain", "walkable": true},
    {"id": 8, "name": "cave_wall_foot_ne", "category": "boundary", "walkable": false},
    {"id": 9, "name": "cave_wall_foot_nw", "category": "boundary", "walkable": false},
    {"id": 10, "name": "low_stone_ledge_edge", "category": "boundary", "walkable": false},
    {"id": 11, "name": "wooden_storage_pad", "category": "terrain", "walkable": true},
    {"id": 12, "name": "cave_wall_shadow_transition", "category": "overlay", "walkable": true},
    {"id": 13, "name": "worn_path_stone", "category": "ground", "walkable": true},
    {"id": 14, "name": "clean_secret_nook_stone", "category": "ground", "walkable": true},
    {"id": 15, "name": "wine_jar_stain", "category": "terrain", "walkable": true},
    {"id": 16, "name": "rest_mat_imprint", "category": "terrain", "walkable": true},
    {"id": 17, "name": "warm_lamp_floor_glow", "category": "terrain", "walkable": true},
    {"id": 18, "name": "damp_cave_crack_floor", "category": "terrain", "walkable": true},
    {"id": 19, "name": "small_moss_corner_floor", "category": "terrain", "walkable": true},
    {"id": 20, "name": "storage_scuff_floor", "category": "terrain", "walkable": true},
    {"id": 21, "name": "darker_rear_cave_floor", "category": "ground", "walkable": true},
    {"id": 22, "name": "shallow_water_highlight", "category": "terrain", "walkable": true},
    {"id": 23, "name": "plain_fallback_dry_stone", "category": "ground", "walkable": true}
  ]
}
```

- [ ] **Step 6: 生成拼接预览**

运行：

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image

root = Path("assets/generated/maps/back-mountain-cliff-cave")
atlas = Image.open(root / "tilesets/cliff_cave_ground_tiles.png").convert("RGBA")
preview = Image.new("RGBA", (64 * 8, 32 * 4), (0, 0, 0, 0))
tile_ids = [0, 1, 2, 3, 13, 14, 21, 23]
for row in range(4):
    for col in range(8):
        tid = tile_ids[(row + col) % len(tile_ids)]
        sx = (tid % 6) * 64
        sy = (tid // 6) * 32
        tile = atlas.crop((sx, sy, sx + 64, sy + 32))
        preview.alpha_composite(tile, (col * 64, row * 32))
preview.save(root / "previews/tileset_repeat_preview.png")
print(root / "previews/tileset_repeat_preview.png")
PY
```

预期：生成 `previews/tileset_repeat_preview.png`。人工查看该图，确认基础石地没有明显断裂或非洞穴元素。

---

### Task 3: 生成并切出透明物件 PNG

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/prompts/props.prompt.txt`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/raw/cliff_cave_props_raw.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/props/*.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/previews/props_contact_sheet.png`

**接口：**
- 产出：10 个透明物件 PNG。
- 产出：物件接触表预览，后续 `.tmx` object layer 使用这些 PNG 名称。

- [ ] **Step 1: 写入 props prompt**

写入 `assets/generated/maps/back-mountain-cliff-cave/prompts/props.prompt.txt`：

```text
Create a 16-bit pixel art prop sheet for a Chinese wuxia RPG cave interior.

Exact sheet contract:
- PNG image.
- Solid #FF00FF magenta background.
- 2 columns x 5 rows, 10 prop cells.
- Each prop must be isolated inside its cell with generous padding.
- No labels, no text, no numbers, no UI, no characters.
- Pixel art with readable edges.

Scene identity:
This is a lived-in storage cave behind FengZhi Manor, used for medicinal wine, supplies, and as a secret base shared by the protagonist and his senior sister. It must feel humble, domestic, safe, and personal. Do not include swords, treasure chests, tomb objects, bones, monsters, mine carts, ancient ruins, or combat props.

Props, row-major order:
1 single sealed medicinal wine jar with cloth and clay seal
2 group of 2-3 sealed medicinal wine jars, main pickup object
3 wall-side wooden storage shelf with small jars and folded cloth
4 bamboo basket with dried herbs
5 stored cloth bundle
6 low wooden crate for storage clutter
7 old rest mat or straw mat, low and humble
8 small wooden stool
9 senior sister memory marker, a small cloth strip or paper tag tied to a stone, gentle and personal
10 small dim oil lamp, warm but modest

All props must have their visual bottom aligned consistently so they can be placed on an isometric tile baseline after chroma-key cleanup.
```

- [ ] **Step 2: 使用 `image_gen` 生成物件图集**

调用内置 `image_gen`，prompt 使用 Step 1 的完整文本。

预期：生成一张 `#FF00FF` 背景上的 2x5 物件图集。

- [ ] **Step 3: 保存原图副本**

将 image_gen 输出 PNG 复制为：

```text
assets/generated/maps/back-mountain-cliff-cave/raw/cliff_cave_props_raw.png
```

- [ ] **Step 4: 切出并透明化物件**

运行：

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image

root = Path("assets/generated/maps/back-mountain-cliff-cave")
src = root / "raw/cliff_cave_props_raw.png"
out_dir = root / "props"
out_dir.mkdir(parents=True, exist_ok=True)

names = [
    "wine_jar_single.png",
    "wine_jars_group.png",
    "storage_shelf.png",
    "bamboo_basket_herbs.png",
    "cloth_bundle.png",
    "wooden_crate_low.png",
    "rest_mat.png",
    "small_stool.png",
    "sister_mark.png",
    "oil_lamp_dim.png",
]

image = Image.open(src).convert("RGBA")
width, height = image.size
cell_w = width // 2
cell_h = height // 5

def chroma_key_magenta(im: Image.Image) -> Image.Image:
    rgba = im.convert("RGBA")
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = pixels[x, y]
            if r > 210 and g < 70 and b > 210:
                pixels[x, y] = (0, 0, 0, 0)
    return rgba

for index, name in enumerate(names):
    col = index % 2
    row = index // 2
    crop = image.crop((col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h))
    clean = chroma_key_magenta(crop)
    bbox = clean.getbbox()
    if bbox is None:
        raise SystemExit(f"empty prop cell: {name}")
    trimmed = clean.crop(bbox)
    canvas_w = max(trimmed.width + 16, 64)
    canvas_h = max(trimmed.height + 16, 64)
    canvas = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    x = (canvas_w - trimmed.width) // 2
    y = canvas_h - trimmed.height - 4
    canvas.alpha_composite(trimmed, (x, y))
    canvas.save(out_dir / name)
    print(out_dir / name, canvas.size)
PY
```

预期：`props/` 下出现 10 个 RGBA PNG。

- [ ] **Step 5: 生成物件接触表**

运行：

```bash
python3 - <<'PY'
from pathlib import Path
from PIL import Image, ImageDraw

root = Path("assets/generated/maps/back-mountain-cliff-cave")
names = [
    "wine_jar_single.png",
    "wine_jars_group.png",
    "storage_shelf.png",
    "bamboo_basket_herbs.png",
    "cloth_bundle.png",
    "wooden_crate_low.png",
    "rest_mat.png",
    "small_stool.png",
    "sister_mark.png",
    "oil_lamp_dim.png",
]
thumb_w, thumb_h = 160, 120
sheet = Image.new("RGBA", (thumb_w * 2, thumb_h * 5), (28, 28, 34, 255))
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(names):
    prop = Image.open(root / "props" / name).convert("RGBA")
    prop.thumbnail((120, 80), Image.Resampling.NEAREST)
    x = (i % 2) * thumb_w + (thumb_w - prop.width) // 2
    y = (i // 2) * thumb_h + 10
    sheet.alpha_composite(prop, (x, y))
    draw.text(((i % 2) * thumb_w + 8, (i // 2) * thumb_h + 94), name.replace(".png", ""), fill=(235, 235, 220, 255))
sheet.save(root / "previews/props_contact_sheet.png")
print(root / "previews/props_contact_sheet.png")
PY
```

预期：生成 `previews/props_contact_sheet.png`。人工查看没有错位、背景残留或跑题物件。

---

### Task 4: 组装主角 iso4 行走 atlas

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/tools/assemble_character_atlas.py`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/characters/main_character_iso4_walk_atlas.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/previews/main_character_iso4_walk_atlas_preview.png`

**接口：**
- 消费：`feng-zhi/assets/character/main_character_iso4/walk/walk_{dir}_{frame}.png`
- 产出：`512x512 RGBA` atlas，4 行 x 4 列，每格 `128x128`。

- [ ] **Step 1: 写入 atlas 脚本**

写入 `assets/generated/maps/back-mountain-cliff-cave/tools/assemble_character_atlas.py`：

```python
#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

DIRECTIONS = ["ne", "se", "sw", "nw"]
CELL = 128


def main() -> None:
    source = Path("feng-zhi/assets/character/main_character_iso4/walk")
    output_root = Path("assets/generated/maps/back-mountain-cliff-cave")
    atlas = Image.new("RGBA", (CELL * 4, CELL * 4), (0, 0, 0, 0))

    for row, direction in enumerate(DIRECTIONS):
        for col in range(4):
            frame_path = source / f"walk_{direction}_{col:02d}.png"
            if not frame_path.exists():
                raise SystemExit(f"missing frame: {frame_path}")
            frame = Image.open(frame_path).convert("RGBA")
            if frame.size != (CELL, CELL):
                raise SystemExit(f"{frame_path} size is {frame.size}, expected {(CELL, CELL)}")
            atlas.alpha_composite(frame, (col * CELL, row * CELL))

    out = output_root / "characters/main_character_iso4_walk_atlas.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(out)

    preview = Image.new("RGBA", atlas.size, (32, 32, 38, 255))
    preview.alpha_composite(atlas)
    draw = ImageDraw.Draw(preview)
    for i in range(5):
        draw.line((i * CELL, 0, i * CELL, CELL * 4), fill=(255, 255, 255, 60))
        draw.line((0, i * CELL, CELL * 4, i * CELL), fill=(255, 255, 255, 60))
    draw.line((0, CELL - 1, CELL * 4, CELL - 1), fill=(255, 80, 80, 180))
    draw.line((0, CELL * 2 - 1, CELL * 4, CELL * 2 - 1), fill=(255, 80, 80, 180))
    draw.line((0, CELL * 3 - 1, CELL * 4, CELL * 3 - 1), fill=(255, 80, 80, 180))
    draw.line((0, CELL * 4 - 1, CELL * 4, CELL * 4 - 1), fill=(255, 80, 80, 180))
    preview_out = output_root / "previews/main_character_iso4_walk_atlas_preview.png"
    preview_out.parent.mkdir(parents=True, exist_ok=True)
    preview.save(preview_out)
    print(out)
    print(preview_out)


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: 运行 atlas 脚本**

运行：

```bash
python3 assets/generated/maps/back-mountain-cliff-cave/tools/assemble_character_atlas.py
```

预期：输出 atlas 和 preview 两个路径。

- [ ] **Step 3: 检查 atlas 尺寸**

运行：

```bash
python3 - <<'PY'
from PIL import Image
path = "assets/generated/maps/back-mountain-cliff-cave/characters/main_character_iso4_walk_atlas.png"
im = Image.open(path)
print(path, im.size, im.mode)
PY
```

预期：`(512, 512) RGBA`。

---

### Task 5: 写入 Tiled `.tsx` 与 day/night `.tmx`

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/tools/build_tmx.py`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/maps/cliff_cave_ground_tiles.tsx`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_day.tmx`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_night.tmx`

**接口：**
- 消费：`tilesets/cliff_cave_ground_tiles.png`
- 产出：Tiled 可打开的 `.tsx` 和两份 `.tmx`。

- [ ] **Step 1: 写入 `.tmx` 构建脚本**

写入 `assets/generated/maps/back-mountain-cliff-cave/tools/build_tmx.py`：

```python
#!/usr/bin/env python3
from __future__ import annotations

import csv
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path("assets/generated/maps/back-mountain-cliff-cave")
MAP_W = 16
MAP_H = 16


def layer_csv(grid: list[list[int]]) -> str:
    flat = [str(cell) for row in grid for cell in row]
    lines = []
    for row in range(MAP_H):
        lines.append(",".join(flat[row * MAP_W:(row + 1) * MAP_W]))
    return ",\n".join(lines)


def blank(value: int = 0) -> list[list[int]]:
    return [[value for _ in range(MAP_W)] for _ in range(MAP_H)]


def build_grids() -> dict[str, list[list[int]]]:
    ground = blank(0)
    terrain = blank(0)
    overlay = blank(0)
    collision = blank(0)

    # Tiled gid: tileset local id + 1. 0 means empty.
    # Walkable cave footprint for approved "storage + nook" layout.
    walkable = set()
    for y in range(3, 12):
        for x in range(2, 9):
            walkable.add((x, y))
    for y in range(6, 14):
        for x in range(9, 15):
            walkable.add((x, y))
    for y in range(7, 10):
        for x in range(0, 12):
            walkable.add((x, y))

    for y in range(MAP_H):
        for x in range(MAP_W):
            if (x, y) in walkable:
                ground[y][x] = 1
            else:
                collision[y][x] = 9

    # Ground variation.
    for x, y, gid in [
        (4, 4, 3), (5, 4, 3), (6, 4, 16), (7, 4, 16),
        (10, 8, 15), (11, 8, 15), (12, 8, 18), (13, 8, 18),
        (9, 13, 6), (10, 13, 6), (11, 13, 23), (12, 13, 23),
        (3, 8, 14), (4, 8, 14), (5, 8, 21),
        (8, 9, 19), (9, 9, 19),
    ]:
        if 0 <= x < MAP_W and 0 <= y < MAP_H:
            terrain[y][x] = gid

    # Object blockers represented in collision.
    for x, y in [(5, 5), (6, 5), (4, 9), (5, 9), (6, 9), (11, 6), (12, 6), (5, 10)]:
        collision[y][x] = 11

    # Overlay cave wall shadows near rear.
    for x, y in [(3, 3), (4, 3), (5, 3), (6, 3), (10, 6), (11, 6), (12, 6), (13, 6)]:
        overlay[y][x] = 13

    return {
        "Ground": ground,
        "Terrain": terrain,
        "Overlay": overlay,
        "Collision": collision,
    }


def add_properties(parent: ET.Element, props: dict[str, str | int | bool]) -> None:
    properties = ET.SubElement(parent, "properties")
    for name, value in props.items():
        attrib = {"name": name}
        if isinstance(value, bool):
            attrib["type"] = "bool"
            attrib["value"] = "true" if value else "false"
        elif isinstance(value, int):
            attrib["type"] = "int"
            attrib["value"] = str(value)
        else:
            attrib["value"] = str(value)
        ET.SubElement(properties, "property", attrib)


def iso_object_xy(tile_x: int, tile_y: int) -> tuple[float, float]:
    # Tiled object coords for isometric maps are editor-space; these stable
    # coordinates are sufficient for marker placement and validation.
    return float(tile_x * 64), float(tile_y * 32)


def add_marker(group: ET.Element, oid: int, name: str, obj_type: str, tile_x: int, tile_y: int, variant: str, enabled: bool = True) -> None:
    x, y = iso_object_xy(tile_x, tile_y)
    obj = ET.SubElement(group, "object", {
        "id": str(oid),
        "name": name,
        "type": obj_type,
        "x": str(x),
        "y": str(y),
        "width": "64",
        "height": "32",
    })
    mood = "warm_daily" if variant == "day" else "silent_night"
    chapter_state = "prologue_daily" if variant == "day" else "prologue_massacre_trigger"
    add_properties(obj, {
        "id": name,
        "type": obj_type,
        "enabled": enabled,
        "interaction_id": f"{variant}.{name}",
        "chapter_state": chapter_state,
        "mood": mood,
        "tile_x": tile_x,
        "tile_y": tile_y,
    })


def write_tsx() -> None:
    tsx = ET.Element("tileset", {
        "version": "1.10",
        "tiledversion": "1.10.2",
        "name": "cliff_cave_ground_tiles",
        "tilewidth": "64",
        "tileheight": "32",
        "tilecount": "24",
        "columns": "6",
    })
    ET.SubElement(tsx, "image", {
        "source": "../tilesets/cliff_cave_ground_tiles.png",
        "width": "384",
        "height": "128",
    })
    path = ROOT / "maps/cliff_cave_ground_tiles.tsx"
    ET.ElementTree(tsx).write(path, encoding="UTF-8", xml_declaration=True)


def write_tmx(variant: str) -> None:
    grids = build_grids()
    root = ET.Element("map", {
        "version": "1.10",
        "tiledversion": "1.10.2",
        "orientation": "isometric",
        "renderorder": "right-down",
        "width": str(MAP_W),
        "height": str(MAP_H),
        "tilewidth": "64",
        "tileheight": "32",
        "infinite": "0",
        "nextlayerid": "7",
        "nextobjectid": "20",
    })
    add_properties(root, {
        "scene_id": f"back_mountain_cliff_cave_{variant}",
        "scene_name": "后山崖洞",
        "variant": variant,
        "mood": "warm_daily" if variant == "day" else "silent_night",
        "chapter_state": "prologue_daily" if variant == "day" else "prologue_massacre_trigger",
    })
    ET.SubElement(root, "tileset", {"firstgid": "1", "source": "cliff_cave_ground_tiles.tsx"})

    for layer_id, name in enumerate(["Ground", "Terrain"], start=1):
        layer = ET.SubElement(root, "layer", {"id": str(layer_id), "name": name, "width": str(MAP_W), "height": str(MAP_H)})
        data = ET.SubElement(layer, "data", {"encoding": "csv"})
        data.text = "\n" + layer_csv(grids[name]) + "\n"

    structures = ET.SubElement(root, "objectgroup", {"id": "3", "name": "Structures"})
    add_structure_objects(structures, variant)

    overlay = ET.SubElement(root, "layer", {"id": "4", "name": "Overlay", "width": str(MAP_W), "height": str(MAP_H)})
    overlay_data = ET.SubElement(overlay, "data", {"encoding": "csv"})
    overlay_data.text = "\n" + layer_csv(grids["Overlay"]) + "\n"

    collision = ET.SubElement(root, "layer", {"id": "5", "name": "Collision", "width": str(MAP_W), "height": str(MAP_H), "visible": "0", "opacity": "0.35"})
    collision_data = ET.SubElement(collision, "data", {"encoding": "csv"})
    collision_data.text = "\n" + layer_csv(grids["Collision"]) + "\n"

    logic = ET.SubElement(root, "objectgroup", {"id": "6", "name": "LogicMarkers"})
    markers = [
        ("exit_to_back_mountain", "exit", 1, 8, True),
        ("wine_pickup", "interactable", 6, 5, True),
        ("storage_shelf", "inspect", 5, 9, True),
        ("memory_marker", "inspect", 13, 8, True),
        ("rest_spot", "trigger", 11, 11, True),
        ("blocked_rock", "blocker", 2, 3, True),
        ("blocked_storage", "blocker", 5, 10, True),
    ]
    for oid, (name, obj_type, tile_x, tile_y, enabled) in enumerate(markers, start=10):
        add_marker(logic, oid, name, obj_type, tile_x, tile_y, variant, enabled)

    filename = f"back_mountain_cliff_cave_{variant}.tmx"
    ET.ElementTree(root).write(ROOT / "maps" / filename, encoding="UTF-8", xml_declaration=True)


def add_structure_objects(group: ET.Element, variant: str) -> None:
    structures = [
        (1, "wine_jars_group", "wine_jars_group.png", 6, 5),
        (2, "wine_jar_single", "wine_jar_single.png", 7, 5),
        (3, "storage_shelf", "storage_shelf.png", 5, 9),
        (4, "bamboo_basket_herbs", "bamboo_basket_herbs.png", 4, 10),
        (5, "cloth_bundle", "cloth_bundle.png", 6, 10),
        (6, "wooden_crate_low", "wooden_crate_low.png", 5, 10),
        (7, "rest_mat", "rest_mat.png", 11, 11),
        (8, "small_stool", "small_stool.png", 12, 11),
        (9, "sister_mark", "sister_mark.png", 13, 8),
        (10, "oil_lamp_dim", "oil_lamp_dim.png", 12, 9),
    ]
    for oid, name, image, tile_x, tile_y in structures:
        x, y = iso_object_xy(tile_x, tile_y)
        obj = ET.SubElement(group, "object", {
            "id": str(oid),
            "name": name,
            "type": "prop",
            "x": str(x),
            "y": str(y),
            "width": "64",
            "height": "64",
        })
        add_properties(obj, {
            "image": f"../props/{image}",
            "anchor": "bottom_tile_baseline",
            "variant_state": variant,
            "mood": "warm_daily" if variant == "day" else "silent_night",
        })


def main() -> None:
    (ROOT / "maps").mkdir(parents=True, exist_ok=True)
    write_tsx()
    write_tmx("day")
    write_tmx("night")
    print(ROOT / "maps/cliff_cave_ground_tiles.tsx")
    print(ROOT / "maps/back_mountain_cliff_cave_day.tmx")
    print(ROOT / "maps/back_mountain_cliff_cave_night.tmx")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: 运行构建脚本**

运行：

```bash
python3 assets/generated/maps/back-mountain-cliff-cave/tools/build_tmx.py
```

预期：输出 `.tsx`、day `.tmx`、night `.tmx` 三个路径。

- [ ] **Step 3: 检查 `.tmx` XML 可解析**

运行：

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET
for path in [
    "assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_day.tmx",
    "assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_night.tmx",
]:
    root = ET.parse(path).getroot()
    print(path, root.attrib["orientation"], root.attrib["width"], root.attrib["height"], root.attrib["tilewidth"], root.attrib["tileheight"])
PY
```

预期：两行都显示 `isometric 16 16 64 32`。

---

### Task 6: 生成地图 QA 预览并跑完整验证

**文件：**
- 新建：`assets/generated/maps/back-mountain-cliff-cave/previews/back_mountain_cliff_cave_day_debug_preview.png`
- 新建：`assets/generated/maps/back-mountain-cliff-cave/previews/back_mountain_cliff_cave_night_debug_preview.png`
- 修改：`assets/generated/maps/back-mountain-cliff-cave/manifest.json`

**接口：**
- 消费：Task 2-5 全部输出。
- 产出：人工审核用地图预览与 `PASS` 验证结果。

- [ ] **Step 1: 生成 debug 预览**

运行：

```bash
python3 - <<'PY'
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

ROOT = Path("assets/generated/maps/back-mountain-cliff-cave")
TILE_W, TILE_H = 64, 32
MAP_W, MAP_H = 16, 16

def parse_csv(text):
    values = [int(x.strip()) for x in text.replace("\n", "").split(",") if x.strip()]
    return [values[y * MAP_W:(y + 1) * MAP_W] for y in range(MAP_H)]

def iso_xy(x, y):
    origin_x = 512
    origin_y = 32
    return int(origin_x + (x - y) * TILE_W / 2), int(origin_y + (x + y) * TILE_H / 2)

def draw_map(tmx_name, out_name, tint):
    root = ET.parse(ROOT / "maps" / tmx_name).getroot()
    atlas = Image.open(ROOT / "tilesets/cliff_cave_ground_tiles.png").convert("RGBA")
    canvas = Image.new("RGBA", (1024, 640), tint)
    for layer_name in ["Ground", "Terrain", "Overlay", "Collision"]:
        layer = root.find(f"layer[@name='{layer_name}']")
        if layer is None:
            continue
        data = layer.find("data")
        grid = parse_csv(data.text or "")
        for y, row in enumerate(grid):
            for x, gid in enumerate(row):
                if gid == 0:
                    continue
                tid = gid - 1
                sx = (tid % 6) * TILE_W
                sy = (tid // 6) * TILE_H
                tile = atlas.crop((sx, sy, sx + TILE_W, sy + TILE_H))
                if layer_name == "Collision":
                    overlay = Image.new("RGBA", tile.size, (255, 60, 60, 80))
                    tile = Image.alpha_composite(tile, overlay)
                px, py = iso_xy(x, y)
                canvas.alpha_composite(tile, (px - TILE_W // 2, py - TILE_H // 2))
    draw = ImageDraw.Draw(canvas)
    logic = root.find("objectgroup[@name='LogicMarkers']")
    if logic is not None:
        for obj in logic.findall("object"):
            props = {p.attrib.get("name"): p.attrib.get("value") for p in obj.findall("./properties/property")}
            if "tile_x" not in props or "tile_y" not in props:
                continue
            px, py = iso_xy(int(props["tile_x"]), int(props["tile_y"]))
            draw.ellipse((px - 5, py - 5, px + 5, py + 5), fill=(255, 235, 80, 255))
            draw.text((px + 8, py - 8), obj.attrib.get("name", ""), fill=(255, 255, 220, 255))
    out = ROOT / "previews" / out_name
    canvas.save(out)
    print(out)

draw_map("back_mountain_cliff_cave_day.tmx", "back_mountain_cliff_cave_day_debug_preview.png", (44, 42, 34, 255))
draw_map("back_mountain_cliff_cave_night.tmx", "back_mountain_cliff_cave_night_debug_preview.png", (20, 25, 35, 255))
PY
```

预期：生成 day/night 两张 debug preview。人工查看主通路、酒坛区、小窝和 marker 位置合理。

- [ ] **Step 2: 跑完整验证**

运行：

```bash
python3 assets/generated/maps/back-mountain-cliff-cave/tools/validate_pack.py --root assets/generated/maps/back-mountain-cliff-cave
```

预期：输出 `PASS: back-mountain-cliff-cave asset pack validation`。

- [ ] **Step 3: 更新 manifest 验证状态**

运行：

```bash
python3 - <<'PY'
from datetime import datetime
from pathlib import Path
import json

path = Path("assets/generated/maps/back-mountain-cliff-cave/manifest.json")
data = json.loads(path.read_text(encoding="utf-8"))
data["validation"] = {
    "last_run": datetime.now().isoformat(timespec="seconds"),
    "status": "pass",
    "notes": [
        "validate_pack.py passed",
        "day/night debug previews generated",
        "manual visual review still required for final art taste"
    ]
}
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(path)
PY
```

预期：`manifest.json` 中 `validation.status` 为 `pass`。

---

## 自查清单

实现计划覆盖的 spec 要求：

- `64x32` PNG32 tile 图集：Task 2。
- 独立透明 PNG 物件：Task 3。
- 复用现有主角 iso4 行走帧并组装 atlas：Task 4。
- 两份独立 Tiled `.tmx`：Task 5。
- 碰撞与逻辑标记：Task 5 和 Task 6。
- day/night 同空间不同状态：Task 5。
- 验收/QC：Task 6。

本计划不包含：

- Godot `.tscn` 集成。
- 外部后山场景。
- 提交 commit。
- 完整 dialogue 文案。

