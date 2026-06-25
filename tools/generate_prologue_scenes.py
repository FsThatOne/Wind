#!/usr/bin/env python3
"""Generate all resource files for 5 new prologue scenes in FengZhi.

Scenes generated:
  1. study            (书房)
  2. training_ground  (练武场)
  3. back_mountain_path (后山小路)
  4. mountain_gate    (山门)
  5. living_quarter   (居所)

For each scene, outputs:
  - assets/maps/{id}/maps/{id}_ground_tiles.tsx
  - assets/maps/{id}/tilesets/{id}_ground_tiles.tres
  - assets/maps/{id}/maps/{id}_day.tmx
  - assets/maps/{id}/maps/{id}_night.tmx
  - scenes/{id}/{PascalName}TileLayers.tscn
  - scenes/{id}/{PascalName}.tscn
"""
from __future__ import annotations

import base64
import struct
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"

MAP_WIDTH = 16
MAP_HEIGHT = 16
TILE_WIDTH = 64
TILE_HEIGHT = 32
TILESET_COLUMNS = 6
TILESET_ROWS = 4
TILE_COUNT = TILESET_COLUMNS * TILESET_ROWS  # 24


# ---------------------------------------------------------------------------
# Data structures
# ---------------------------------------------------------------------------


@dataclass
class Prop:
    name: str
    tile_x: int
    tile_y: int
    image: str  # relative path from maps/ dir


@dataclass
class Marker:
    name: str
    marker_type: str  # exit, inspect, interactable, npc, trigger
    tile_x: int
    tile_y: int
    properties: dict[str, Any] = field(default_factory=dict)


@dataclass
class SceneConfig:
    id: str
    scene_name: str  # Chinese display name
    pascal_name: str
    day_mood: str
    night_mood: str
    day_chapter_state: str
    night_chapter_state: str
    # Ground shape: list of (row, col_start, col_end) inclusive
    ground_rows: list[tuple[int, int, int]]
    # Props (day variant; night reuses same props unless overridden)
    props: list[Prop]
    # Night-only extra props
    night_extra_props: list[Prop] = field(default_factory=list)
    # Logic markers (day)
    day_markers: list[Marker] = field(default_factory=list)
    # Logic markers (night)
    night_markers: list[Marker] = field(default_factory=list)
    # Terrain GIDs (list of (row, col, gid) for non-zero terrain tiles)
    terrain_gids: list[tuple[int, int, int]] = field(default_factory=list)
    # Player spawn position in pixels
    player_pos: tuple[int, int] = (352, 300)


# ---------------------------------------------------------------------------
# Scene definitions
# ---------------------------------------------------------------------------


SCENES: list[SceneConfig] = [
    SceneConfig(
        id="study",
        scene_name="书房",
        pascal_name="Study",
        day_mood="warm_daily",
        night_mood="silent_night",
        day_chapter_state="prologue_daily",
        night_chapter_state="prologue_massacre_trigger",
        ground_rows=[
            (3, 5, 10),
            (4, 4, 11),
            (5, 4, 11),
            (6, 3, 12),
            (7, 3, 12),
            (8, 3, 12),
            (9, 4, 11),
            (10, 4, 11),
            (11, 5, 10),
        ],
        terrain_gids=[
            (4, 5, 7), (4, 10, 7),
            (6, 4, 8), (6, 11, 8),
            (8, 4, 10), (8, 11, 10),
            (10, 5, 11), (10, 10, 11),
        ],
        props=[
            Prop("bookshelf", 5, 4, "../props/bookshelf.png"),
            Prop("desk", 7, 6, "../props/desk.png"),
            Prop("scroll_pile", 9, 5, "../props/scroll_pile.png"),
            Prop("secret_compartment", 10, 9, "../props/secret_compartment.png"),
        ],
        day_markers=[
            Marker("exit_to_main_hall", "exit", 7, 11, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_study",
            }),
            Marker("bookshelf_inspect", "inspect", 5, 4, {}),
            Marker("desk_inspect", "inspect", 7, 6, {}),
            Marker("scroll_inspect", "inspect", 9, 5, {}),
        ],
        night_markers=[
            Marker("exit_to_main_hall", "exit", 7, 11, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_study",
            }),
            Marker("bookshelf_inspect", "inspect", 5, 4, {}),
            Marker("desk_inspect", "inspect", 7, 6, {}),
            Marker("secret_compartment_inspect", "inspect", 10, 9, {}),
        ],
        player_pos=(352, 320),
    ),
    SceneConfig(
        id="training_ground",
        scene_name="练武场",
        pascal_name="TrainingGround",
        day_mood="warm_daily",
        night_mood="silent_night",
        day_chapter_state="prologue_daily",
        night_chapter_state="prologue_massacre_trigger",
        ground_rows=[
            (2, 4, 11),
            (3, 3, 12),
            (4, 3, 12),
            (5, 2, 13),
            (6, 2, 13),
            (7, 2, 13),
            (8, 2, 13),
            (9, 3, 12),
            (10, 3, 12),
            (11, 4, 11),
            (12, 5, 10),
        ],
        terrain_gids=[
            (3, 7, 6), (3, 8, 6),
            (5, 3, 7), (5, 12, 7),
            (8, 3, 10), (8, 12, 10),
            (10, 7, 11), (10, 8, 11),
        ],
        props=[
            Prop("training_dummy", 6, 5, "../props/training_dummy.png"),
            Prop("training_dummy", 6, 10, "../props/training_dummy.png"),
            Prop("wooden_sword", 4, 7, "../props/wooden_sword.png"),
            Prop("stone_bench", 9, 4, "../props/stone_bench.png"),
            Prop("fence_post", 3, 5, "../props/fence_post.png"),
            Prop("fence_post", 3, 10, "../props/fence_post.png"),
        ],
        day_markers=[
            Marker("exit_to_courtyard", "exit", 7, 12, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_training",
            }),
            Marker("training_dummy_interact", "interactable", 6, 5, {}),
            Marker("wooden_sword_pickup", "interactable", 4, 7, {}),
            Marker("stone_bench_rest", "inspect", 9, 4, {}),
        ],
        night_markers=[
            Marker("exit_to_courtyard", "exit", 7, 12, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_training",
            }),
            Marker("training_dummy_inspect", "inspect", 6, 5, {}),
            Marker("stone_bench_rest", "inspect", 9, 4, {}),
        ],
        player_pos=(384, 320),
    ),
    SceneConfig(
        id="back_mountain_path",
        scene_name="后山小路",
        pascal_name="BackMountainPath",
        day_mood="warm_daily",
        night_mood="silent_night",
        day_chapter_state="prologue_daily",
        night_chapter_state="prologue_massacre_trigger",
        ground_rows=[
            (2, 6, 9),
            (3, 5, 10),
            (4, 5, 10),
            (5, 4, 11),
            (6, 4, 11),
            (7, 3, 12),
            (8, 3, 12),
            (9, 2, 11),
            (10, 2, 10),
            (11, 3, 9),
            (12, 4, 8),
            (13, 5, 7),
        ],
        terrain_gids=[
            (3, 6, 7), (3, 9, 7),
            (5, 5, 8), (5, 10, 8),
            (7, 4, 6), (7, 11, 6),
            (9, 3, 7), (9, 10, 7),
            (11, 4, 8), (11, 8, 8),
        ],
        props=[
            Prop("old_tree", 4, 6, "../props/old_tree.png"),
            Prop("path_rock", 8, 5, "../props/path_rock.png"),
            Prop("wild_grass", 6, 10, "../props/wild_grass.png"),
            Prop("path_rock", 10, 8, "../props/path_rock.png"),
        ],
        day_markers=[
            Marker("exit_to_mountain_gate", "exit", 7, 2, {
                "target_scene": "mountain_gate",
                "entry_marker": "entry_from_path",
            }),
            Marker("exit_to_cliff_cave", "exit", 5, 13, {
                "target_scene": "back_mountain_cliff_cave",
                "entry_marker": "entry_from_path",
            }),
            Marker("old_tree_inspect", "inspect", 4, 6, {}),
            Marker("wild_grass_inspect", "inspect", 6, 10, {}),
        ],
        night_markers=[
            Marker("exit_to_mountain_gate", "exit", 7, 2, {
                "target_scene": "mountain_gate",
                "entry_marker": "entry_from_path",
            }),
            Marker("exit_to_cliff_cave", "exit", 5, 13, {
                "target_scene": "back_mountain_cliff_cave",
                "entry_marker": "entry_from_path",
            }),
            Marker("old_tree_inspect", "inspect", 4, 6, {}),
        ],
        player_pos=(352, 280),
    ),
    SceneConfig(
        id="mountain_gate",
        scene_name="山门",
        pascal_name="MountainGate",
        day_mood="warm_daily",
        night_mood="silent_night",
        day_chapter_state="prologue_daily",
        night_chapter_state="prologue_massacre_trigger",
        ground_rows=[
            (3, 5, 10),
            (4, 4, 11),
            (5, 4, 11),
            (6, 3, 12),
            (7, 3, 12),
            (8, 3, 12),
            (9, 3, 12),
            (10, 4, 11),
            (11, 4, 11),
            (12, 5, 10),
        ],
        terrain_gids=[
            (4, 7, 6), (4, 8, 6),
            (6, 4, 7), (6, 11, 7),
            (9, 4, 10), (9, 11, 10),
            (11, 7, 11), (11, 8, 11),
        ],
        props=[
            Prop("gate_pillar_left", 6, 4, "../props/gate_pillar_left.png"),
            Prop("gate_pillar_right", 6, 11, "../props/gate_pillar_right.png"),
            Prop("gate_plaque", 5, 7, "../props/gate_plaque.png"),
        ],
        day_markers=[
            Marker("exit_to_courtyard", "exit", 7, 12, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_gate",
            }),
            Marker("exit_to_back_mountain", "exit", 7, 3, {
                "target_scene": "back_mountain_path",
                "entry_marker": "entry_from_gate",
            }),
            Marker("gate_plaque_inspect", "inspect", 5, 7, {}),
            Marker("gate_pillar_inspect", "inspect", 6, 4, {}),
        ],
        night_markers=[
            Marker("exit_to_courtyard", "exit", 7, 12, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_gate",
            }),
            Marker("exit_to_back_mountain", "exit", 7, 3, {
                "target_scene": "back_mountain_path",
                "entry_marker": "entry_from_gate",
            }),
            Marker("gate_plaque_inspect", "inspect", 5, 7, {}),
        ],
        player_pos=(384, 340),
    ),
    SceneConfig(
        id="living_quarter",
        scene_name="居所",
        pascal_name="LivingQuarter",
        day_mood="warm_daily",
        night_mood="silent_night",
        day_chapter_state="prologue_daily",
        night_chapter_state="prologue_massacre_trigger",
        ground_rows=[
            (3, 5, 10),
            (4, 4, 11),
            (5, 4, 11),
            (6, 3, 12),
            (7, 3, 12),
            (8, 3, 12),
            (9, 4, 11),
            (10, 4, 11),
            (11, 5, 10),
        ],
        terrain_gids=[
            (4, 5, 7), (4, 10, 7),
            (6, 4, 8), (6, 11, 8),
            (8, 4, 10), (8, 11, 10),
            (10, 5, 11), (10, 10, 11),
        ],
        props=[
            Prop("bed_mat", 5, 9, "../props/bed_mat.png"),
            Prop("herb_drying_rack", 7, 5, "../props/herb_drying_rack.png"),
            Prop("herb_plant", 8, 10, "../props/herb_plant.png"),
            Prop("medicine_pot", 6, 7, "../props/medicine_pot.png"),
        ],
        day_markers=[
            Marker("exit_to_courtyard", "exit", 7, 11, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_living",
            }),
            Marker("bed_mat_rest", "interactable", 5, 9, {}),
            Marker("herb_rack_inspect", "inspect", 7, 5, {}),
            Marker("medicine_pot_inspect", "inspect", 6, 7, {}),
        ],
        night_markers=[
            Marker("exit_to_courtyard", "exit", 7, 11, {
                "target_scene": "main_hall",
                "entry_marker": "entry_from_living",
            }),
            Marker("bed_mat_rest", "interactable", 5, 9, {}),
            Marker("herb_rack_inspect", "inspect", 7, 5, {}),
            Marker("herb_plant_inspect", "inspect", 8, 10, {}),
        ],
        player_pos=(352, 300),
    ),
]


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def ground_positions(scene: SceneConfig) -> list[tuple[int, int]]:
    """Return all (col, row) positions where ground tiles exist."""
    positions: list[tuple[int, int]] = []
    for row, col_start, col_end in scene.ground_rows:
        for col in range(col_start, col_end + 1):
            positions.append((col, row))
    return positions


def build_ground_csv(scene: SceneConfig) -> str:
    """Build 16x16 CSV for Ground layer (GID 1 = walkable, 0 = empty)."""
    grid = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]
    for col, row in ground_positions(scene):
        grid[row][col] = 1
    rows_str: list[str] = []
    for row in grid:
        rows_str.append(",".join(str(v) for v in row))
    return ",".join(rows_str)


def build_terrain_csv(scene: SceneConfig) -> str:
    """Build 16x16 CSV for Terrain layer using terrain_gids."""
    grid = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]
    for row, col, gid in scene.terrain_gids:
        grid[row][col] = gid
    rows_str: list[str] = []
    for row in grid:
        rows_str.append(",".join(str(v) for v in row))
    return ",".join(rows_str)


def build_collision_csv(scene: SceneConfig) -> str:
    """Build 16x16 CSV for Collision layer.

    9 = outside ground (blocked), 0 = walkable, 11 = prop blocked.
    """
    ground_set = set(ground_positions(scene))
    prop_set = {(p.tile_x, p.tile_y) for p in scene.props}
    grid = [[0] * MAP_WIDTH for _ in range(MAP_HEIGHT)]
    for row in range(MAP_HEIGHT):
        for col in range(MAP_WIDTH):
            if (col, row) not in ground_set:
                grid[row][col] = 9
            elif (col, row) in prop_set:
                grid[row][col] = 11
            else:
                grid[row][col] = 0
    rows_str: list[str] = []
    for row in grid:
        rows_str.append(",".join(str(v) for v in row))
    return ",".join(rows_str)


def build_overlay_csv() -> str:
    """Build 16x16 CSV for Overlay layer (all zeros placeholder)."""
    return ",".join(["0"] * (MAP_WIDTH * MAP_HEIGHT))


def tile_to_pixel(tile_x: int, tile_y: int) -> tuple[float, float]:
    """Convert tile coordinates to pixel position (for TMX object placement)."""
    px = tile_x * TILE_WIDTH
    py = tile_y * TILE_HEIGHT
    return float(px), float(py)


def encode_packed_byte_array(tiles: list[tuple[int, int, int, int, int]]) -> str:
    """Encode tile data to Godot PackedByteArray base64 format.

    Each tile is (tile_x, tile_y, source_id, atlas_x, atlas_y).
    Stored as 12 bytes per tile: [0:int16, tile_x:int16, tile_y:int16,
                                  source_id:int16, atlas_x:int16, atlas_y:int16]
    Followed by a 2-byte zero trailer (Godot sentinel).
    """
    if not tiles:
        return ""
    buf = bytearray()
    for tx, ty, src, ax, ay in tiles:
        buf.extend(struct.pack("<6h", 0, tx, ty, src, ax, ay))
    # Godot appends a 2-byte zero sentinel after all tile entries
    buf.extend(struct.pack("<H", 0))
    return base64.b64encode(bytes(buf)).decode("ascii")


def gid_to_atlas(gid: int) -> tuple[int, int]:
    """Convert TMX GID (1-based) to atlas coordinates."""
    tile_id = gid - 1  # firstgid = 1
    atlas_x = tile_id % TILESET_COLUMNS
    atlas_y = tile_id // TILESET_COLUMNS
    return atlas_x, atlas_y


# ---------------------------------------------------------------------------
# TSX generation
# ---------------------------------------------------------------------------


def generate_tsx(scene: SceneConfig) -> str:
    sid = scene.id
    return (
        f"<?xml version='1.0' encoding='UTF-8'?>\n"
        f'<tileset version="1.10" tiledversion="1.10.2" name="{sid}_ground_tiles" '
        f'tilewidth="64" tileheight="32" tilecount="24" columns="6">'
        f'<image source="../tilesets/{sid}_ground_tiles.png" width="384" height="128" />'
        f"</tileset>"
    )


# ---------------------------------------------------------------------------
# .tres TileSet generation
# ---------------------------------------------------------------------------


def generate_tres(scene: SceneConfig) -> str:
    sid = scene.id
    lines: list[str] = []
    lines.append('[gd_resource type="TileSet" format=3]')
    lines.append("")
    lines.append(
        f'[ext_resource type="Texture2D" '
        f'path="res://assets/maps/{sid}/tilesets/{sid}_ground_tiles.png" id="1_atlas"]'
    )
    lines.append("")
    lines.append(
        f'[sub_resource type="TileSetAtlasSource" id="TileSetAtlasSource_{sid}"]'
    )
    lines.append('texture = ExtResource("1_atlas")')
    lines.append("texture_region_size = Vector2i(64, 32)")
    # Define all 24 tile slots
    for row in range(TILESET_ROWS):
        for col in range(TILESET_COLUMNS):
            lines.append(f"{col}:{row}/0 = 0")
    lines.append("")
    lines.append("[resource]")
    lines.append("tile_shape = 1")
    lines.append("tile_layout = 5")
    lines.append("tile_size = Vector2i(64, 32)")
    lines.append(f'sources/0 = SubResource("TileSetAtlasSource_{sid}")')
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------
# TMX generation
# ---------------------------------------------------------------------------


def build_structures_xml(
    props: list[Prop], variant: str, mood: str
) -> str:
    """Generate the <objectgroup> Structures XML."""
    parts: list[str] = []
    for i, prop in enumerate(props, start=1):
        px, py = tile_to_pixel(prop.tile_x, prop.tile_y)
        parts.append(
            f'<object id="{i}" name="{prop.name}" type="prop" '
            f'x="{px}" y="{py}" width="64" height="32">'
            f"<properties>"
            f'<property name="image" value="{prop.image}" />'
            f'<property name="anchor" value="bottom_tile_baseline" />'
            f'<property name="variant_state" value="{variant}" />'
            f'<property name="mood" value="{mood}" />'
            f'<property name="tile_x" type="int" value="{prop.tile_x}" />'
            f'<property name="tile_y" type="int" value="{prop.tile_y}" />'
            f"</properties></object>"
        )
    return parts


def build_markers_xml(
    markers: list[Marker],
    variant: str,
    mood: str,
    chapter_state: str,
    start_id: int,
) -> list[str]:
    """Generate the <objectgroup> LogicMarkers XML objects."""
    parts: list[str] = []
    for i, marker in enumerate(markers, start=start_id):
        px, py = tile_to_pixel(marker.tile_x, marker.tile_y)
        props_xml = (
            f'<property name="id" value="{marker.name}" />'
            f'<property name="type" value="{marker.marker_type}" />'
            f'<property name="enabled" type="bool" value="true" />'
            f'<property name="interaction_id" value="{variant}.{marker.name}" />'
            f'<property name="chapter_state" value="{chapter_state}" />'
            f'<property name="mood" value="{mood}" />'
            f'<property name="tile_x" type="int" value="{marker.tile_x}" />'
            f'<property name="tile_y" type="int" value="{marker.tile_y}" />'
        )
        # Add extra properties based on marker type
        if marker.marker_type == "exit":
            target = marker.properties.get("target_scene", "")
            entry = marker.properties.get("entry_marker", "")
            props_xml += (
                f'<property name="target_scene" value="{target}" />'
                f'<property name="entry_marker" value="{entry}" />'
            )
        elif marker.marker_type == "npc":
            for key in ("character_id", "facing", "dialogue_id"):
                val = marker.properties.get(key, "")
                props_xml += f'<property name="{key}" value="{val}" />'

        parts.append(
            f'<object id="{i}" name="{marker.name}" type="{marker.marker_type}" '
            f'x="{px}" y="{py}" width="64" height="32">'
            f"<properties>{props_xml}</properties></object>"
        )
    return parts


def generate_tmx(scene: SceneConfig, variant: str) -> str:
    """Generate a complete TMX file for a given variant (day/night)."""
    is_day = variant == "day"
    mood = scene.day_mood if is_day else scene.night_mood
    chapter_state = scene.day_chapter_state if is_day else scene.night_chapter_state
    scene_id_str = f"{scene.id}_{variant}"

    ground_csv = build_ground_csv(scene)
    terrain_csv = build_terrain_csv(scene)
    overlay_csv = build_overlay_csv()
    collision_csv = build_collision_csv(scene)

    # Props
    all_props = list(scene.props)
    if not is_day:
        all_props = all_props + scene.night_extra_props
    structures_objs = build_structures_xml(all_props, variant, mood)

    # Markers
    markers = scene.day_markers if is_day else scene.night_markers
    marker_start_id = len(all_props) + 11
    markers_objs = build_markers_xml(
        markers, variant, mood, chapter_state, marker_start_id
    )

    # Next object id
    next_obj_id = marker_start_id + len(markers)

    # Build TMX
    parts: list[str] = []
    parts.append("<?xml version='1.0' encoding='UTF-8'?>")
    parts.append(
        f'<map version="1.10" tiledversion="1.10.2" orientation="isometric" '
        f'renderorder="right-down" width="16" height="16" tilewidth="64" '
        f'tileheight="32" infinite="0" nextlayerid="7" nextobjectid="{next_obj_id}">'
    )
    # Properties
    parts.append(
        f"<properties>"
        f'<property name="scene_id" value="{scene_id_str}" />'
        f'<property name="scene_name" value="{scene.scene_name}" />'
        f'<property name="variant" value="{variant}" />'
        f'<property name="mood" value="{mood}" />'
        f'<property name="chapter_state" value="{chapter_state}" />'
        f"</properties>"
    )
    # Tileset reference
    parts.append(
        f'<tileset firstgid="1" source="{scene.id}_ground_tiles.tsx" />'
    )
    # Ground layer
    parts.append(
        f'<layer id="1" name="Ground" width="16" height="16">'
        f'<data encoding="csv">\n{ground_csv}\n</data></layer>'
    )
    # Terrain layer
    parts.append(
        f'<layer id="2" name="Terrain" width="16" height="16">'
        f'<data encoding="csv">\n{terrain_csv}\n</data></layer>'
    )
    # Structures objectgroup
    parts.append(
        f'<objectgroup id="3" name="Structures">'
        + "".join(structures_objs)
        + "</objectgroup>"
    )
    # Overlay layer
    parts.append(
        f'<layer id="4" name="Overlay" width="16" height="16">'
        f'<data encoding="csv">\n{overlay_csv}\n</data></layer>'
    )
    # Collision layer
    parts.append(
        f'<layer id="5" name="Collision" width="16" height="16" visible="0" '
        f'opacity="0.35"><data encoding="csv">\n{collision_csv}\n</data></layer>'
    )
    # LogicMarkers objectgroup
    parts.append(
        f'<objectgroup id="6" name="LogicMarkers">'
        + "".join(markers_objs)
        + "</objectgroup>"
    )
    parts.append("</map>")
    return "".join(parts)


# ---------------------------------------------------------------------------
# TileLayers.tscn generation
# ---------------------------------------------------------------------------


def build_ground_tile_data(scene: SceneConfig) -> str:
    """Encode ground layer tiles as PackedByteArray base64.

    Ground tiles all use GID=1 -> atlas (0, 0).
    """
    tiles: list[tuple[int, int, int, int, int]] = []
    # Sort by row then column for consistent output
    positions = sorted(ground_positions(scene), key=lambda p: (p[1], p[0]))
    for col, row in positions:
        tiles.append((col, row, 0, 0, 0))
    return encode_packed_byte_array(tiles)


def build_terrain_tile_data(scene: SceneConfig) -> str:
    """Encode terrain layer tiles as PackedByteArray base64."""
    tiles: list[tuple[int, int, int, int, int]] = []
    sorted_terrain = sorted(scene.terrain_gids, key=lambda t: (t[0], t[1]))
    for row, col, gid in sorted_terrain:
        ax, ay = gid_to_atlas(gid)
        tiles.append((col, row, 0, ax, ay))
    return encode_packed_byte_array(tiles)


def build_overlay_tile_data() -> str:
    """Overlay is empty placeholder - return empty base64."""
    return ""


def generate_tile_layers_tscn(scene: SceneConfig) -> str:
    """Generate the TileLayers.tscn file."""
    sid = scene.id
    pname = scene.pascal_name

    ground_data = build_ground_tile_data(scene)
    terrain_data = build_terrain_tile_data(scene)

    lines: list[str] = []
    lines.append("[gd_scene format=4]")
    lines.append("")
    lines.append(
        f'[ext_resource type="TileSet" '
        f'path="res://assets/maps/{sid}/tilesets/{sid}_ground_tiles.tres" '
        f'id="1_ts"]'
    )
    lines.append("")
    lines.append(
        f'[node name="{pname}TileLayers" type="Node2D"]'
    )
    lines.append("y_sort_enabled = true")
    lines.append("position = Vector2(544, 80)")
    lines.append("")

    # Day and Night variants
    for variant_name in ("Day", "Night"):
        lines.append(f'[node name="{variant_name}" type="Node2D" parent="."]')
        if variant_name == "Night":
            lines.append("visible = false")
        lines.append("y_sort_enabled = true")
        lines.append("")

        # Ground layer
        lines.append(
            f'[node name="Ground" type="TileMapLayer" parent="{variant_name}"]'
        )
        lines.append("y_sort_enabled = true")
        lines.append(f'tile_map_data = PackedByteArray("{ground_data}")')
        lines.append('tile_set = ExtResource("1_ts")')
        lines.append("")

        # Terrain layer
        lines.append(
            f'[node name="Terrain" type="TileMapLayer" parent="{variant_name}"]'
        )
        lines.append("y_sort_enabled = true")
        if terrain_data:
            lines.append(f'tile_map_data = PackedByteArray("{terrain_data}")')
        else:
            lines.append('tile_map_data = PackedByteArray("")')
        lines.append('tile_set = ExtResource("1_ts")')
        lines.append("")

        # Overlay layer (empty)
        lines.append(
            f'[node name="Overlay" type="TileMapLayer" parent="{variant_name}"]'
        )
        lines.append("y_sort_enabled = true")
        lines.append('tile_map_data = PackedByteArray("")')
        lines.append('tile_set = ExtResource("1_ts")')
        lines.append("")

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Main scene .tscn generation
# ---------------------------------------------------------------------------


def generate_main_tscn(scene: SceneConfig) -> str:
    """Generate the main scene .tscn file."""
    sid = scene.id
    pname = scene.pascal_name
    px, py = scene.player_pos

    lines: list[str] = []
    lines.append("[gd_scene format=3]")
    lines.append("")
    lines.append(
        f'[ext_resource type="Script" path="res://scripts/CavePlayer.cs" '
        f'id="1_player"]'
    )
    lines.append(
        f'[ext_resource type="SpriteFrames" '
        f'path="res://assets/character/main_character.tres" id="2_frames"]'
    )
    lines.append(
        f'[ext_resource type="PackedScene" '
        f'path="res://scenes/{sid}/{pname}TileLayers.tscn" id="3_tile_layers"]'
    )
    lines.append("")
    lines.append('[sub_resource type="RectangleShape2D" id="RectangleShape2D_player"]')
    lines.append("size = Vector2(26, 13)")
    lines.append("")
    lines.append(f'[node name="{pname}" type="Node2D"]')
    lines.append("")
    lines.append('[node name="BackgroundTint" type="ColorRect" parent="."]')
    lines.append("offset_left = -1738.0")
    lines.append("offset_top = -1714.0")
    lines.append("offset_right = 2262.0")
    lines.append("offset_bottom = 2286.0")
    lines.append("color = Color(0.14, 0.13, 0.1, 1)")
    lines.append("")
    lines.append('[node name="Tiles" type="Node2D" parent="."]')
    lines.append("")
    lines.append(
        '[node name="TileLayers" parent="Tiles" instance=ExtResource("3_tile_layers")]'
    )
    lines.append("")
    lines.append('[node name="MapRoot" type="Node2D" parent="."]')
    lines.append("y_sort_enabled = true")
    lines.append("")
    lines.append('[node name="Structures" type="Node2D" parent="MapRoot"]')
    lines.append("y_sort_enabled = true")
    lines.append("")
    lines.append('[node name="Collision" type="Node2D" parent="MapRoot"]')
    lines.append("")
    lines.append('[node name="LogicMarkers" type="Node2D" parent="MapRoot"]')
    lines.append("")
    lines.append('[node name="Player" type="CharacterBody2D" parent="MapRoot"]')
    lines.append(f"position = Vector2({px}, {py})")
    lines.append('script = ExtResource("1_player")')
    lines.append("")
    lines.append(
        '[node name="AnimatedSprite2D" type="AnimatedSprite2D" parent="MapRoot/Player"]'
    )
    lines.append("position = Vector2(-2.66, -18.0)")
    lines.append("scale = Vector2(0.6147, 0.62)")
    lines.append('sprite_frames = ExtResource("2_frames")')
    lines.append('animation = &"idle"')
    lines.append("")
    lines.append(
        '[node name="CollisionShape2D" type="CollisionShape2D" parent="MapRoot/Player"]'
    )
    lines.append("position = Vector2(0, 8)")
    lines.append('shape = SubResource("RectangleShape2D_player")')
    lines.append("")
    lines.append('[node name="Camera2D" type="Camera2D" parent="MapRoot/Player"]')
    lines.append("position = Vector2(0, -80)")
    lines.append("zoom = Vector2(1.15, 1.15)")
    lines.append("position_smoothing_enabled = true")
    lines.append("position_smoothing_speed = 6.0")
    lines.append("")
    lines.append('[node name="UiLayer" type="CanvasLayer" parent="."]')
    lines.append("")
    lines.append('[node name="StatusLabel" type="Label" parent="UiLayer"]')
    lines.append("offset_left = 20.0")
    lines.append("offset_top = 16.0")
    lines.append("offset_right = 900.0")
    lines.append("offset_bottom = 52.0")
    lines.append(f'text = "{scene.scene_name}"')
    lines.append("")
    lines.append('[node name="InventoryLabel" type="Label" parent="UiLayer"]')
    lines.append("offset_left = 20.0")
    lines.append("offset_top = 52.0")
    lines.append("offset_right = 520.0")
    lines.append("offset_bottom = 84.0")
    lines.append("")
    lines.append('[node name="PromptLabel" type="Label" parent="UiLayer"]')
    lines.append("visible = false")
    lines.append("offset_left = 360.0")
    lines.append("offset_top = 560.0")
    lines.append("offset_right = 820.0")
    lines.append("offset_bottom = 604.0")
    lines.append('text = "E / 空格 调查    N 切换日夜"')
    lines.append("horizontal_alignment = 1")
    lines.append("")
    lines.append('[node name="HintLabel" type="Label" parent="UiLayer"]')
    lines.append("offset_left = 20.0")
    lines.append("offset_top = 600.0")
    lines.append("offset_right = 740.0")
    lines.append("offset_bottom = 632.0")
    lines.append(
        'text = "左键点击寻路移动；WASD / 方向键逐格移动；E / 空格调查；N 切换日常 / 夜晚"'
    )
    lines.append("")
    lines.append('[node name="MessagePanel" type="Panel" parent="UiLayer"]')
    lines.append("visible = false")
    lines.append("offset_left = 176.0")
    lines.append("offset_top = 456.0")
    lines.append("offset_right = 976.0")
    lines.append("offset_bottom = 622.0")
    lines.append("")
    lines.append(
        '[node name="MessageLabel" type="Label" parent="UiLayer/MessagePanel"]'
    )
    lines.append("layout_mode = 0")
    lines.append("offset_left = 22.0")
    lines.append("offset_top = 18.0")
    lines.append("offset_right = 778.0")
    lines.append("offset_bottom = 142.0")
    lines.append("autowrap_mode = 3")
    lines.append("")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main() -> None:
    total_files = 0

    for scene in SCENES:
        sid = scene.id
        pname = scene.pascal_name
        print(f"\n{'='*60}")
        print(f"Generating scene: {sid} ({scene.scene_name})")
        print(f"{'='*60}")

        # Create directories
        maps_dir = GODOT_ROOT / "assets" / "maps" / sid / "maps"
        tilesets_dir = GODOT_ROOT / "assets" / "maps" / sid / "tilesets"
        scenes_dir = GODOT_ROOT / "scenes" / sid
        maps_dir.mkdir(parents=True, exist_ok=True)
        tilesets_dir.mkdir(parents=True, exist_ok=True)
        scenes_dir.mkdir(parents=True, exist_ok=True)

        # 1. TSX
        tsx_path = maps_dir / f"{sid}_ground_tiles.tsx"
        tsx_path.write_text(generate_tsx(scene), encoding="utf-8")
        print(f"  [1/6] {tsx_path.relative_to(ROOT)}")
        total_files += 1

        # 2. .tres TileSet
        tres_path = tilesets_dir / f"{sid}_ground_tiles.tres"
        tres_path.write_text(generate_tres(scene), encoding="utf-8")
        print(f"  [2/6] {tres_path.relative_to(ROOT)}")
        total_files += 1

        # 3. Day TMX
        day_tmx_path = maps_dir / f"{sid}_day.tmx"
        day_tmx_path.write_text(generate_tmx(scene, "day"), encoding="utf-8")
        print(f"  [3/6] {day_tmx_path.relative_to(ROOT)}")
        total_files += 1

        # 4. Night TMX
        night_tmx_path = maps_dir / f"{sid}_night.tmx"
        night_tmx_path.write_text(generate_tmx(scene, "night"), encoding="utf-8")
        print(f"  [4/6] {night_tmx_path.relative_to(ROOT)}")
        total_files += 1

        # 5. TileLayers.tscn
        tile_layers_path = scenes_dir / f"{pname}TileLayers.tscn"
        tile_layers_path.write_text(
            generate_tile_layers_tscn(scene), encoding="utf-8"
        )
        print(f"  [5/6] {tile_layers_path.relative_to(ROOT)}")
        total_files += 1

        # 6. Main scene .tscn
        main_scene_path = scenes_dir / f"{pname}.tscn"
        main_scene_path.write_text(generate_main_tscn(scene), encoding="utf-8")
        print(f"  [6/6] {main_scene_path.relative_to(ROOT)}")
        total_files += 1

    print(f"\n{'='*60}")
    print(f"Done! Generated {total_files} files for {len(SCENES)} scenes.")
    print(f"{'='*60}")


if __name__ == "__main__":
    main()
