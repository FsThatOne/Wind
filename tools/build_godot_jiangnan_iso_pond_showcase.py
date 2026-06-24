#!/usr/bin/env python3
"""江南等距 tileset 展示场景生成器。

用 jiangnan_iso_tileset (双 atlas source, 64 tiles) 拼一个 7×5 = 35 格的
3×3 池塘 + 1×1 池 + 1×1 岛 + 拱桥 + 路径 showcase scene。

参考: tools/build_godot_back_mountain_cliff_cave_tile_layers.py 装配模式
       (GDScript --headless 生成 .tscn)

执行:
    python3 tools/build_godot_jiangnan_iso_pond_showcase.py
"""
from __future__ import annotations

import json
import subprocess
import tempfile
import textwrap
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"
GODOT = Path("/Applications/Godot_mono.app/Contents/MacOS/Godot")

OUTPUT_SCENE = "res://scenes/showcase/jiangnan_iso_pond_showcase.tscn"
TILESET_PATH = "res://assets/tilesets/jiangnan-iso/jiangnan_iso_tileset.tres"

# 单层 7×5 layout。元素值 = (source_id, atlas_col, atlas_row, tile_name)。
# source_id 0 = sheet1 (4×4 = 16 tiles), source_id 1 = sheet2 (6×8 = 48 tiles)
# None = 留空（不放 tile, 即 TileMap 自动隐藏该 cell）

G = (1, 5, 0, "ground_mossy_heavy")              # sheet2 重苔石 — 当 grass-ish 底
G_GRASS = (0, 3, 0, "ground_grass_moss")         # sheet1 草苔（更典型的草地）
COBBLE = (1, 1, 0, "ground_cobblestone")         # sheet2 圆鹅卵石
PATH_END_S = (1, 3, 2, "path_grass_end_s")       # sheet2 草地径端 S
BANK_NW = (1, 3, 6, "bank_iso_nw")               # sheet2 严格 NW 岸
BANK_NE = (1, 0, 6, "bank_iso_ne")
BANK_SW = (1, 2, 6, "bank_iso_sw")
BANK_SE = (1, 1, 6, "bank_iso_se")
BANK_HN = (1, 4, 6, "bank_half_north_land")
BANK_HS = (1, 5, 6, "bank_half_south_land")
W_SH = (0, 0, 1, "water_shallow")                # sheet1 浅水
W_DEEP = (0, 1, 1, "water_deep")                 # sheet1 深水
BRIDGE = (1, 4, 4, "bridge_stone_arch_mid")       # sheet2 拱桥中段
CIF = (1, 2, 7, "corner_inner_full")             # sheet2 1×1 微池
COF = (1, 5, 7, "corner_outer_full")             # sheet2 1×1 微岛

# 7 cols × 5 rows. 行内顺序 = col 0..6.
LAYOUT: list[list[tuple[int, int, int, str] | None]] = [
    # row 0
    [G_GRASS, G_GRASS, PATH_END_S, G_GRASS, G_GRASS,    G_GRASS, G_GRASS],
    # row 1: pond top row + 1×1 micro-pond
    [G_GRASS, BANK_NW, BANK_HN,    BANK_NE, G_GRASS,    CIF,     G_GRASS],
    # row 2: pond mid row + arched bridge to E
    [COBBLE,  W_SH,    W_DEEP,     W_SH,    BRIDGE,     G_GRASS, G_GRASS],
    # row 3: pond bot row + 1×1 micro-island
    [G_GRASS, BANK_SW, BANK_HS,    BANK_SE, G_GRASS,    COF,     G_GRASS],
    # row 4
    [G_GRASS, G_GRASS, G_GRASS,    G_GRASS, G_GRASS,    G_GRASS, G_GRASS],
]


def build_gdscript() -> str:
    # 把 LAYOUT 转成 GDScript 可读的 Array 字面量。每个 cell 编码为
    # [col, row, source_id, atlas_x, atlas_y, name] 或 null。
    cells: list[list] = []
    for row_idx, row in enumerate(LAYOUT):
        for col_idx, entry in enumerate(row):
            if entry is None:
                continue
            source_id, atlas_x, atlas_y, name = entry
            cells.append([col_idx, row_idx, source_id, atlas_x, atlas_y, name])
    cells_literal = json.dumps(cells, ensure_ascii=False)

    return textwrap.dedent(
        f"""
        extends SceneTree

        const OUTPUT_SCENE := "{OUTPUT_SCENE}"
        const TILESET_PATH := "{TILESET_PATH}"
        const CELLS := {cells_literal}

        func _init() -> void:
            var tile_set := load(TILESET_PATH)
            if tile_set == null:
                push_error("Unable to load " + TILESET_PATH)
                quit(1)
                return

            var root := Node2D.new()
            root.name = "JiangnanIsoPondShowcase"
            root.position = Vector2(640, 200)
            root.y_sort_enabled = true

            var camera := Camera2D.new()
            camera.name = "Camera"
            camera.position = Vector2(192, 96)
            camera.zoom = Vector2(2.0, 2.0)
            root.add_child(camera)
            camera.owner = root

            var tiles_root := Node2D.new()
            tiles_root.name = "Tiles"
            tiles_root.y_sort_enabled = true
            root.add_child(tiles_root)
            tiles_root.owner = root

            var ground := TileMapLayer.new()
            ground.name = "Ground"
            ground.tile_set = tile_set
            ground.y_sort_enabled = true
            ground.z_index = 0
            tiles_root.add_child(ground)
            ground.owner = root

            for cell in CELLS:
                var coords := Vector2i(cell[0], cell[1])
                var source_id: int = cell[2]
                var atlas := Vector2i(cell[3], cell[4])
                ground.set_cell(coords, source_id, atlas)

            var packed := PackedScene.new()
            var pack_result := packed.pack(root)
            if pack_result != OK:
                push_error("Unable to pack pond showcase scene: " + str(pack_result))
                quit(1)
                return

            var save_result := ResourceSaver.save(packed, OUTPUT_SCENE)
            if save_result != OK:
                push_error("Unable to save pond showcase scene: " + str(save_result))
                quit(1)
                return

            print("Saved " + OUTPUT_SCENE + " (cells=" + str(CELLS.size()) + ")")
            quit()
        """
    ).strip()


def main() -> None:
    if not GODOT.exists():
        raise FileNotFoundError(GODOT)

    # 确保输出目录存在
    out_dir = GODOT_ROOT / "scenes" / "showcase"
    out_dir.mkdir(parents=True, exist_ok=True)

    script = build_gdscript()
    with tempfile.TemporaryDirectory() as tmp:
        script_path = Path(tmp) / "build_jiangnan_iso_pond_showcase.gd"
        script_path.write_text(script, encoding="utf-8")
        subprocess.run(
            [
                str(GODOT),
                "--headless",
                "--path",
                str(GODOT_ROOT),
                "--script",
                str(script_path),
            ],
            check=True,
            cwd=ROOT,
        )


if __name__ == "__main__":
    main()
