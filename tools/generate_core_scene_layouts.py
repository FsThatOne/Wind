#!/usr/bin/env python3
"""Generate larger placeholder layouts for core FengZhi scenes."""
from __future__ import annotations

import base64
import struct
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"
TILE_WIDTH = 128
TILE_HEIGHT = 64
TILESET_COLUMNS = 6


@dataclass
class Prop:
    name: str
    tile_x: int
    tile_y: int
    image: str


@dataclass
class Marker:
    name: str
    marker_type: str
    tile_x: int
    tile_y: int
    properties: dict[str, Any] = field(default_factory=dict)


@dataclass
class CoreScene:
    id: str
    scene_name: str
    pascal_name: str
    width: int
    height: int
    tileset_name: str
    tileset_tres_name: str
    ground_rows: list[tuple[int, int, int]]
    terrain_gids: list[tuple[int, int, int]]
    props: list[Prop]
    day_markers: list[Marker]
    night_markers: list[Marker]


SCENES = [
    CoreScene(
        id="main_hall",
        scene_name="正堂",
        pascal_name="MainHall",
        width=32,
        height=28,
        tileset_name="main_hall_ground_tiles",
        tileset_tres_name="main_hall_ground_tiles",
        ground_rows=[
            (3, 12, 19), (4, 10, 21), (5, 9, 22), (6, 8, 23),
            (7, 7, 24), (8, 7, 24), (9, 6, 25), (10, 6, 25),
            (11, 6, 25), (12, 7, 24), (13, 7, 24), (14, 8, 23),
            (15, 9, 22), (16, 10, 21), (17, 11, 20), (18, 13, 18),
            (19, 14, 17),
        ],
        terrain_gids=[
            (4, 14, 7), (4, 17, 7), (6, 9, 8), (6, 22, 8),
            (9, 8, 10), (9, 23, 10), (12, 10, 11), (12, 21, 11),
            (16, 13, 6), (16, 18, 6),
        ],
        props=[
            Prop("stone_wall_motto", 16, 5, "../props/stone_wall_motto.png"),
            Prop("weapon_rack", 10, 10, "../props/weapon_rack.png"),
            Prop("tea_table", 16, 11, "../props/tea_table.png"),
            Prop("chair", 18, 11, "../props/chair.png"),
            Prop("blood_letter", 17, 14, "../props/blood_letter.png"),
        ],
        day_markers=[
            Marker("exit_to_courtyard", "exit", 16, 19, {"target_scene": "res://scenes/sect_compound/SectCompound.tscn", "entry_marker": "entry_from_main_hall"}),
            Marker("exit_to_study", "exit", 22, 8, {"target_scene": "res://scenes/study/Study.tscn", "entry_marker": "entry_from_main_hall"}),
            Marker("stone_wall_inspect", "inspect", 16, 7),
            Marker("weapon_rack_inspect", "inspect", 11, 10),
            Marker("tea_table_interact", "interactable", 16, 12),
            Marker("master_talk", "interactable", 16, 8),
            Marker("entry_from_compound", "entry", 16, 19),
            Marker("entry_from_study", "entry", 21, 9),
        ],
        night_markers=[
            Marker("exit_to_courtyard", "exit", 16, 19, {"target_scene": "res://scenes/sect_compound/SectCompound.tscn", "entry_marker": "entry_from_main_hall"}),
            Marker("exit_to_study", "exit", 22, 8, {"target_scene": "res://scenes/study/Study.tscn", "entry_marker": "entry_from_main_hall"}),
            Marker("stone_wall_inspect", "inspect", 16, 7),
            Marker("weapon_rack_inspect", "inspect", 11, 10),
            Marker("tea_table_interact", "interactable", 16, 12),
            Marker("blood_letter_inspect", "inspect", 18, 14),
            Marker("entry_from_compound", "entry", 16, 19),
            Marker("entry_from_study", "entry", 21, 9),
        ],
    ),
    CoreScene(
        id="back_mountain_cliff_cave",
        scene_name="后山崖洞",
        pascal_name="BackMountainCliffCave",
        width=36,
        height=30,
        tileset_name="cliff_cave_ground_tiles",
        tileset_tres_name="cliff_cave_ground_tiles",
        ground_rows=[
            (2, 25, 29), (3, 23, 30), (4, 21, 31), (5, 19, 31),
            (6, 17, 30), (7, 14, 29), (8, 11, 28), (9, 9, 27),
            (10, 7, 26), (11, 5, 24), (12, 4, 22), (13, 4, 20),
            (14, 5, 19), (15, 6, 20), (16, 8, 22), (17, 10, 24),
            (18, 12, 26), (19, 14, 27), (20, 16, 26), (21, 18, 24),
            (22, 19, 22), (23, 20, 21),
        ],
        terrain_gids=[
            (3, 27, 7), (5, 22, 8), (7, 17, 10), (9, 12, 11),
            (11, 7, 6), (13, 6, 7), (16, 12, 8), (18, 20, 10),
            (20, 23, 11),
        ],
        props=[
            Prop("storage_shelf", 24, 5, "../props/storage_shelf.png"),
            Prop("wine_jars_group", 20, 8, "../props/wine_jars_group.png"),
            Prop("wine_jar_single", 21, 8, "../props/wine_jar_single.png"),
            Prop("oil_lamp_dim", 17, 12, "../props/oil_lamp_dim.png"),
            Prop("sister_mark", 19, 15, "../props/sister_mark.png"),
            Prop("small_stool", 20, 17, "../props/small_stool.png"),
            Prop("rest_mat", 22, 18, "../props/rest_mat.png"),
        ],
        day_markers=[
            Marker("exit_to_back_mountain", "exit", 20, 23, {"target_scene": "res://scenes/back_mountain_path/BackMountainPath.tscn", "entry_marker": "entry_from_cliff_cave"}),
            Marker("entry_from_path", "entry", 20, 23),
            Marker("wine_pickup", "interactable", 21, 9),
            Marker("storage_shelf", "inspect", 25, 5),
            Marker("memory_marker", "inspect", 19, 16),
            Marker("rest_spot", "trigger", 21, 18),
        ],
        night_markers=[
            Marker("exit_to_back_mountain", "exit", 20, 23, {"target_scene": "res://scenes/back_mountain_path/BackMountainPath.tscn", "entry_marker": "entry_from_cliff_cave"}),
            Marker("entry_from_path", "entry", 20, 23),
            Marker("wine_pickup", "interactable", 21, 9),
            Marker("storage_shelf", "inspect", 25, 5),
            Marker("memory_marker", "inspect", 19, 16),
            Marker("rest_spot", "trigger", 21, 18),
        ],
    ),
]


def ground_positions(scene: CoreScene) -> list[tuple[int, int]]:
    return [(col, row) for row, start, end in scene.ground_rows for col in range(start, end + 1)]


def csv_grid(grid: list[list[int]]) -> str:
    return ",\n".join(",".join(str(v) for v in row) for row in grid)


def layer_csv(scene: CoreScene, kind: str) -> str:
    grid = [[0] * scene.width for _ in range(scene.height)]
    if kind == "ground":
        for col, row in ground_positions(scene):
            grid[row][col] = 1
    elif kind == "terrain":
        for row, col, gid in scene.terrain_gids:
            grid[row][col] = gid
    elif kind == "collision":
        ground = set(ground_positions(scene))
        blocked = {(p.tile_x, p.tile_y) for p in scene.props}
        for row in range(scene.height):
            for col in range(scene.width):
                if (col, row) not in ground:
                    grid[row][col] = 9
                elif (col, row) in blocked:
                    grid[row][col] = 11
    return csv_grid(grid)


def tile_to_pixel(tile_x: int, tile_y: int) -> tuple[float, float]:
    return float(tile_x * TILE_WIDTH), float(tile_y * TILE_HEIGHT)


def structures_xml(scene: CoreScene) -> str:
    parts = []
    for i, prop in enumerate(scene.props, start=1):
        px, py = tile_to_pixel(prop.tile_x, prop.tile_y)
        parts.append(
            f'<object id="{i}" name="{prop.name}" type="prop" x="{px}" y="{py}" width="128" height="64">'
            f'<properties><property name="image" value="{prop.image}" /><property name="anchor" value="bottom_tile_baseline" />'
            f'<property name="variant_state" value="both" /><property name="mood" value="placeholder" />'
            f'<property name="tile_x" type="int" value="{prop.tile_x}" /><property name="tile_y" type="int" value="{prop.tile_y}" />'
            f'</properties></object>'
        )
    return "".join(parts)


def marker_xml(marker: Marker, object_id: int, variant: str, chapter_state: str, mood: str) -> str:
    px, py = tile_to_pixel(marker.tile_x, marker.tile_y)
    props = [
        f'<property name="id" value="{marker.name}" />',
        f'<property name="type" value="{marker.marker_type}" />',
        '<property name="enabled" type="bool" value="true" />',
        f'<property name="interaction_id" value="{variant}.{marker.name}" />',
        f'<property name="chapter_state" value="{chapter_state}" />',
        f'<property name="mood" value="{mood}" />',
        f'<property name="tile_x" type="int" value="{marker.tile_x}" />',
        f'<property name="tile_y" type="int" value="{marker.tile_y}" />',
    ]
    if marker.marker_type == "exit":
        props.append(f'<property name="target_scene" value="{marker.properties["target_scene"]}" />')
        props.append(f'<property name="entry_marker" value="{marker.properties["entry_marker"]}" />')
    return (
        f'<object id="{object_id}" name="{marker.name}" type="{marker.marker_type}" x="{px}" y="{py}" width="128" height="64">'
        f'<properties>{"".join(props)}</properties></object>'
    )


def generate_tmx(scene: CoreScene, variant: str) -> str:
    is_day = variant == "day"
    mood = "warm_daily" if is_day else "silent_night"
    chapter_state = "prologue_daily" if is_day else "prologue_massacre_trigger"
    markers = scene.day_markers if is_day else scene.night_markers
    marker_start = len(scene.props) + 10
    marker_parts = [marker_xml(marker, i, variant, chapter_state, mood) for i, marker in enumerate(markers, start=marker_start)]
    return "".join([
        "<?xml version='1.0' encoding='UTF-8'?>",
        f'<map version="1.10" tiledversion="1.10.2" orientation="isometric" renderorder="right-down" width="{scene.width}" height="{scene.height}" tilewidth="128" tileheight="64" infinite="0" nextlayerid="7" nextobjectid="{marker_start + len(markers)}">',
        "<properties>",
        f'<property name="scene_id" value="{scene.id}_{variant}" />',
        f'<property name="scene_name" value="{scene.scene_name}" />',
        f'<property name="variant" value="{variant}" />',
        f'<property name="mood" value="{mood}" />',
        f'<property name="chapter_state" value="{chapter_state}" />',
        "</properties>",
        f'<tileset firstgid="1" source="{scene.tileset_name}.tsx" />',
        f'<layer id="1" name="Ground" width="{scene.width}" height="{scene.height}"><data encoding="csv">\n{layer_csv(scene, "ground")}\n</data></layer>',
        f'<layer id="2" name="Terrain" width="{scene.width}" height="{scene.height}"><data encoding="csv">\n{layer_csv(scene, "terrain")}\n</data></layer>',
        f'<objectgroup id="3" name="Structures">{structures_xml(scene)}</objectgroup>',
        f'<layer id="4" name="Overlay" width="{scene.width}" height="{scene.height}"><data encoding="csv">\n{csv_grid([[0] * scene.width for _ in range(scene.height)])}\n</data></layer>',
        f'<layer id="5" name="Collision" width="{scene.width}" height="{scene.height}" visible="0" opacity="0.35"><data encoding="csv">\n{layer_csv(scene, "collision")}\n</data></layer>',
        f'<objectgroup id="6" name="LogicMarkers">{"".join(marker_parts)}</objectgroup>',
        "</map>",
    ])


def encode_tiles(tiles: list[tuple[int, int, int, int, int]]) -> str:
    buf = bytearray()
    for tx, ty, src, ax, ay in tiles:
        buf.extend(struct.pack("<6h", 0, tx, ty, src, ax, ay))
    buf.extend(struct.pack("<H", 0))
    return base64.b64encode(bytes(buf)).decode("ascii")


def gid_to_atlas(gid: int) -> tuple[int, int]:
    tile_id = gid - 1
    return tile_id % TILESET_COLUMNS, tile_id // TILESET_COLUMNS


def tile_data(scene: CoreScene) -> tuple[str, str]:
    ground = [(col, row, 0, 0, 0) for col, row in sorted(ground_positions(scene), key=lambda p: (p[1], p[0]))]
    terrain = []
    for row, col, gid in sorted(scene.terrain_gids, key=lambda t: (t[0], t[1])):
        ax, ay = gid_to_atlas(gid)
        terrain.append((col, row, 0, ax, ay))
    return encode_tiles(ground), encode_tiles(terrain)


def generate_tile_layers(scene: CoreScene) -> str:
    ground_data, terrain_data = tile_data(scene)
    return "\n".join([
        "[gd_scene format=4]", "",
        f'[ext_resource type="TileSet" path="res://assets/maps/{scene.id}/tilesets/{scene.tileset_tres_name}.tres" id="1_ts"]',
        "",
        f'[node name="{scene.pascal_name}TileLayers" type="Node2D"]',
        "y_sort_enabled = true",
        "position = Vector2(512, 64)",
        "",
        '[node name="Day" type="Node2D" parent="."]',
        "y_sort_enabled = true", "",
        '[node name="Ground" type="TileMapLayer" parent="Day"]',
        "y_sort_enabled = true",
        f'tile_map_data = PackedByteArray("{ground_data}")',
        'tile_set = ExtResource("1_ts")', "",
        '[node name="Terrain" type="TileMapLayer" parent="Day"]',
        "y_sort_enabled = true",
        f'tile_map_data = PackedByteArray("{terrain_data}")',
        'tile_set = ExtResource("1_ts")', "",
        '[node name="Overlay" type="TileMapLayer" parent="Day"]',
        "y_sort_enabled = true",
        'tile_map_data = PackedByteArray("")',
        'tile_set = ExtResource("1_ts")', "",
        '[node name="Night" type="Node2D" parent="."]',
        "visible = false",
        "y_sort_enabled = true", "",
        '[node name="Ground" type="TileMapLayer" parent="Night"]',
        "y_sort_enabled = true",
        f'tile_map_data = PackedByteArray("{ground_data}")',
        'tile_set = ExtResource("1_ts")', "",
        '[node name="Terrain" type="TileMapLayer" parent="Night"]',
        "y_sort_enabled = true",
        f'tile_map_data = PackedByteArray("{terrain_data}")',
        'tile_set = ExtResource("1_ts")', "",
        '[node name="Overlay" type="TileMapLayer" parent="Night"]',
        "y_sort_enabled = true",
        'tile_map_data = PackedByteArray("")',
        'tile_set = ExtResource("1_ts")', "",
    ])


def main() -> None:
    for scene in SCENES:
        maps_dir = GODOT_ROOT / "assets" / "maps" / scene.id / "maps"
        scenes_dir = GODOT_ROOT / "scenes" / scene.id
        maps_dir.mkdir(parents=True, exist_ok=True)
        scenes_dir.mkdir(parents=True, exist_ok=True)
        for variant in ("day", "night"):
            path = maps_dir / f"{scene.id}_{variant}.tmx"
            path.write_text(generate_tmx(scene, variant), encoding="utf-8")
            print(path.relative_to(ROOT))
        tile_layers_path = scenes_dir / f"{scene.pascal_name}TileLayers.tscn"
        tile_layers_path.write_text(generate_tile_layers(scene), encoding="utf-8")
        print(tile_layers_path.relative_to(ROOT))


if __name__ == "__main__":
    main()
