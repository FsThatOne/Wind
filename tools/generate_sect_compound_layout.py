#!/usr/bin/env python3
"""Generate the large placeholder overview map for FengZhi manor."""
from __future__ import annotations

import base64
import struct
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"

SID = "sect_compound"
WIDTH = 88
HEIGHT = 68
SCALE = 2
TILE_WIDTH = 64
TILE_HEIGHT = 32
TILESET_COLUMNS = 6


GROUND_ROWS = [
    (2, 5, 10),
    (3, 4, 14),
    (4, 4, 20),
    (5, 5, 29),
    (6, 6, 34),
    (7, 7, 36),
    (8, 6, 38),
    (9, 5, 39),
    (10, 5, 40),
    (11, 4, 40),
    (12, 4, 40),
    (13, 4, 39),
    (14, 5, 39),
    (15, 5, 38),
    (16, 6, 37),
    (17, 7, 37),
    (18, 7, 36),
    (19, 8, 35),
    (20, 9, 34),
    (21, 10, 33),
    (22, 11, 32),
    (23, 12, 31),
    (24, 13, 30),
    (25, 14, 29),
    (26, 15, 28),
    (27, 16, 27),
    (28, 17, 26),
    (29, 18, 25),
    (30, 19, 24),
    (31, 20, 23),
]

TERRAIN_GIDS = [
    (3, 7, 7), (4, 12, 7), (5, 24, 8),
    (6, 17, 6), (6, 22, 6), (7, 32, 8),
    (9, 8, 10), (9, 37, 10),
    (12, 6, 7), (12, 34, 7),
    (15, 12, 11), (15, 29, 11),
    (18, 15, 8), (18, 26, 8),
    (21, 18, 10), (21, 23, 10),
    (25, 20, 11), (25, 24, 11),
    (29, 21, 6), (29, 22, 6),
]

# 只阻挡建筑占位块，出口和主要石径保持可通行。
BLOCKED_TILES = {
    (18, 5), (19, 5), (20, 5), (21, 5), (22, 5), (23, 5),  # 正堂背靠岩壁
    (29, 8), (30, 8), (31, 8), (32, 8),                    # 书房
    (10, 15), (11, 15), (12, 15), (13, 15),                # 厨房仓房
    (30, 16), (31, 16), (32, 16), (33, 16),                # 丹房锻房
    (17, 21), (18, 21), (19, 21), (20, 21), (21, 21),      # 小潭北岸
}

MARKERS: list[dict[str, Any]] = [
    {
        "name": "exit_to_main_hall",
        "type": "exit",
        "tile": (21, 7),
        "target_scene": "res://scenes/main_hall/MainHall.tscn",
        "entry_marker": "entry_from_compound",
    },
    {
        "name": "exit_to_study",
        "type": "exit",
        "tile": (31, 10),
        "target_scene": "res://scenes/study/Study.tscn",
        "entry_marker": "entry_from_compound",
    },
    {
        "name": "exit_to_living_quarter",
        "type": "exit",
        "tile": (12, 17),
        "target_scene": "res://scenes/living_quarter/LivingQuarter.tscn",
        "entry_marker": "entry_from_compound",
    },
    {
        "name": "exit_to_alchemy_room",
        "type": "exit",
        "tile": (32, 18),
        "target_scene": "res://scenes/alchemy_room/AlchemyRoom.tscn",
        "entry_marker": "entry_from_compound",
    },
    {
        "name": "exit_to_mountain_gate",
        "type": "exit",
        "tile": (21, 31),
        "target_scene": "res://scenes/mountain_gate/MountainGate.tscn",
        "entry_marker": "entry_from_compound",
    },
    {
        "name": "exit_to_back_mountain_path",
        "type": "exit",
        "tile": (7, 2),
        "target_scene": "res://scenes/back_mountain_path/BackMountainPath.tscn",
        "entry_marker": "entry_from_compound",
    },
    {"name": "entry_from_main_hall", "type": "entry", "tile": (21, 8)},
    {"name": "entry_from_study", "type": "entry", "tile": (31, 11)},
    {"name": "entry_from_living_quarter", "type": "entry", "tile": (12, 18)},
    {"name": "entry_from_alchemy_room", "type": "entry", "tile": (32, 19)},
    {"name": "entry_from_mountain_gate", "type": "entry", "tile": (21, 30)},
    {"name": "entry_from_back_mountain_path", "type": "entry", "tile": (8, 3)},
    {"name": "motto_axis_inspect", "type": "inspect", "tile": (21, 10)},
    {"name": "water_pond_inspect", "type": "inspect", "tile": (20, 22)},
    {"name": "mist_gate_inspect", "type": "inspect", "tile": (21, 28)},
]


def ground_positions() -> list[tuple[int, int]]:
    positions: list[tuple[int, int]] = []
    for row, start, end in GROUND_ROWS:
        for col in range(start, end + 1):
            for dx in range(SCALE):
                for dy in range(SCALE):
                    positions.append((col * SCALE + dx, row * SCALE + dy))
    return positions


def csv_from_grid(grid: list[list[int]]) -> str:
    return ",\n".join(",".join(str(value) for value in row) for row in grid)


def build_ground_csv() -> str:
    grid = [[0] * WIDTH for _ in range(HEIGHT)]
    for col, row in ground_positions():
        grid[row][col] = 1
    return csv_from_grid(grid)


def build_terrain_csv() -> str:
    grid = [[0] * WIDTH for _ in range(HEIGHT)]
    for row, col, gid in TERRAIN_GIDS:
        grid[row * SCALE][col * SCALE] = gid
    return csv_from_grid(grid)


def build_collision_csv() -> str:
    ground = set(ground_positions())
    grid = [[0] * WIDTH for _ in range(HEIGHT)]
    for row in range(HEIGHT):
        for col in range(WIDTH):
            if (col, row) not in ground:
                grid[row][col] = 9
            elif (col // SCALE, row // SCALE) in BLOCKED_TILES:
                grid[row][col] = 11
    return csv_from_grid(grid)


def encode_tiles(tiles: list[tuple[int, int, int, int, int]]) -> str:
    buf = bytearray()
    for tx, ty, src, ax, ay in tiles:
        buf.extend(struct.pack("<6h", 0, tx, ty, src, ax, ay))
    buf.extend(struct.pack("<H", 0))
    return base64.b64encode(bytes(buf)).decode("ascii")


def gid_to_atlas(gid: int) -> tuple[int, int]:
    tile_id = gid - 1
    return tile_id % TILESET_COLUMNS, tile_id // TILESET_COLUMNS


def build_tile_data() -> tuple[str, str]:
    ground_tiles = [(col, row, 0, 0, 0) for col, row in sorted(ground_positions(), key=lambda p: (p[1], p[0]))]
    terrain_tiles = []
    for row, col, gid in sorted(TERRAIN_GIDS, key=lambda t: (t[0], t[1])):
        atlas_x, atlas_y = gid_to_atlas(gid)
        terrain_tiles.append((col * SCALE, row * SCALE, 0, atlas_x, atlas_y))
    return encode_tiles(ground_tiles), encode_tiles(terrain_tiles)


def object_xml(marker: dict[str, Any], object_id: int) -> str:
    base_x, base_y = marker["tile"]
    tile_x = base_x * SCALE
    tile_y = base_y * SCALE
    px = float(tile_x * TILE_WIDTH)
    py = float(tile_y * TILE_HEIGHT)
    props = [
        f'<property name="id" value="{marker["name"]}" />',
        f'<property name="type" value="{marker["type"]}" />',
        '<property name="enabled" type="bool" value="true" />',
        f'<property name="tile_x" type="int" value="{tile_x}" />',
        f'<property name="tile_y" type="int" value="{tile_y}" />',
    ]
    if marker["type"] == "exit":
        props.append(f'<property name="target_scene" value="{marker["target_scene"]}" />')
        props.append(f'<property name="entry_marker" value="{marker["entry_marker"]}" />')
    return (
        f'<object id="{object_id}" name="{marker["name"]}" type="{marker["type"]}" '
        f'x="{px}" y="{py}" width="64" height="32"><properties>'
        + "".join(props)
        + "</properties></object>"
    )


def generate_tmx(variant: str) -> str:
    mood = "warm_daily" if variant == "day" else "silent_night"
    chapter_state = "prologue_daily" if variant == "day" else "prologue_massacre_trigger"
    objects = "".join(object_xml(marker, i) for i, marker in enumerate(MARKERS, start=1))
    overlay_csv = csv_from_grid([[0] * WIDTH for _ in range(HEIGHT)])
    return "".join(
        [
            "<?xml version='1.0' encoding='UTF-8'?>",
            f'<map version="1.10" tiledversion="1.10.2" orientation="isometric" renderorder="right-down" width="{WIDTH}" height="{HEIGHT}" tilewidth="64" tileheight="32" infinite="0" nextlayerid="7" nextobjectid="{len(MARKERS) + 1}">',
            "<properties>",
            f'<property name="scene_id" value="{SID}_{variant}" />',
            '<property name="scene_name" value="风止山院" />',
            f'<property name="variant" value="{variant}" />',
            f'<property name="mood" value="{mood}" />',
            f'<property name="chapter_state" value="{chapter_state}" />',
            "</properties>",
            f'<tileset firstgid="1" source="{SID}_ground_tiles.tsx" />',
            f'<layer id="1" name="Ground" width="{WIDTH}" height="{HEIGHT}"><data encoding="csv">\n{build_ground_csv()}\n</data></layer>',
            f'<layer id="2" name="Terrain" width="{WIDTH}" height="{HEIGHT}"><data encoding="csv">\n{build_terrain_csv()}\n</data></layer>',
            '<objectgroup id="3" name="Structures"></objectgroup>',
            f'<layer id="4" name="Overlay" width="{WIDTH}" height="{HEIGHT}"><data encoding="csv">\n{overlay_csv}\n</data></layer>',
            f'<layer id="5" name="Collision" width="{WIDTH}" height="{HEIGHT}" visible="0" opacity="0.35"><data encoding="csv">\n{build_collision_csv()}\n</data></layer>',
            f'<objectgroup id="6" name="LogicMarkers">{objects}</objectgroup>',
            "</map>",
        ]
    )


def generate_tile_layers() -> str:
    ground_data, terrain_data = build_tile_data()
    return "\n".join(
        [
            "[gd_scene format=4]",
            "",
            '[ext_resource type="TileSet" path="res://assets/maps/sect_compound/tilesets/sect_compound_ground_tiles.tres" id="1_ts"]',
            "",
            '[node name="SectCompoundTileLayers" type="Node2D"]',
            "y_sort_enabled = true",
            "position = Vector2(544, 80)",
            "",
            '[node name="Day" type="Node2D" parent="."]',
            "y_sort_enabled = true",
            "",
            '[node name="Ground" type="TileMapLayer" parent="Day"]',
            "y_sort_enabled = true",
            f'tile_map_data = PackedByteArray("{ground_data}")',
            'tile_set = ExtResource("1_ts")',
            "",
            '[node name="Terrain" type="TileMapLayer" parent="Day"]',
            "y_sort_enabled = true",
            f'tile_map_data = PackedByteArray("{terrain_data}")',
            'tile_set = ExtResource("1_ts")',
            "",
            '[node name="Overlay" type="TileMapLayer" parent="Day"]',
            "y_sort_enabled = true",
            'tile_map_data = PackedByteArray("")',
            'tile_set = ExtResource("1_ts")',
            "",
            '[node name="Night" type="Node2D" parent="."]',
            "visible = false",
            "y_sort_enabled = true",
            "",
            '[node name="Ground" type="TileMapLayer" parent="Night"]',
            "y_sort_enabled = true",
            f'tile_map_data = PackedByteArray("{ground_data}")',
            'tile_set = ExtResource("1_ts")',
            "",
            '[node name="Terrain" type="TileMapLayer" parent="Night"]',
            "y_sort_enabled = true",
            f'tile_map_data = PackedByteArray("{terrain_data}")',
            'tile_set = ExtResource("1_ts")',
            "",
            '[node name="Overlay" type="TileMapLayer" parent="Night"]',
            "y_sort_enabled = true",
            'tile_map_data = PackedByteArray("")',
            'tile_set = ExtResource("1_ts")',
            "",
        ]
    )


def main() -> None:
    maps_dir = GODOT_ROOT / "assets" / "maps" / SID / "maps"
    scenes_dir = GODOT_ROOT / "scenes" / SID
    maps_dir.mkdir(parents=True, exist_ok=True)
    scenes_dir.mkdir(parents=True, exist_ok=True)

    for variant in ("day", "night"):
        path = maps_dir / f"{SID}_{variant}.tmx"
        path.write_text(generate_tmx(variant), encoding="utf-8")
        print(path.relative_to(ROOT))

    tile_layers_path = scenes_dir / "SectCompoundTileLayers.tscn"
    tile_layers_path.write_text(generate_tile_layers(), encoding="utf-8")
    print(tile_layers_path.relative_to(ROOT))


if __name__ == "__main__":
    main()
