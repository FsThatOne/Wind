#!/usr/bin/env python3
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"
ASSET_ROOT = GODOT_ROOT / "assets/maps/back_mountain_cliff_cave"
SCENE_PATH = GODOT_ROOT / "scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn"
TILE_LAYERS_SCENE_PATH = GODOT_ROOT / "scenes/back_mountain_cliff_cave/BackMountainCliffCaveTileLayers.tscn"
SCRIPT_PATH = GODOT_ROOT / "scripts/BackMountainCliffCaveGame.cs"
PROJECT_PATH = GODOT_ROOT / "project.godot"


REQUIRED_PROPS = [
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


def require(path: Path) -> None:
    if not path.exists():
        fail(f"missing {path.relative_to(ROOT)}")


def parse_tmx(path: Path) -> ET.Element:
    require(path)
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as exc:
        fail(f"{path.relative_to(ROOT)} is not valid XML: {exc}")


def validate_tmx(path: Path) -> None:
    root = parse_tmx(path)
    if root.attrib.get("orientation") != "isometric":
        fail(f"{path.name} orientation is not isometric")
    if (root.attrib.get("width"), root.attrib.get("height")) != ("16", "16"):
        fail(f"{path.name} is not 16x16")
    if (root.attrib.get("tilewidth"), root.attrib.get("tileheight")) != ("64", "32"):
        fail(f"{path.name} tile size is not 64x32")

    layer_names = {layer.attrib.get("name", "") for layer in root.findall("layer")}
    group_names = {group.attrib.get("name", "") for group in root.findall("objectgroup")}
    for required in ["Ground", "Terrain", "Overlay", "Collision", "Structures", "LogicMarkers"]:
        if required not in layer_names | group_names:
            fail(f"{path.name} missing {required}")

    markers = {obj.attrib.get("name", "") for obj in root.findall("objectgroup[@name='LogicMarkers']/object")}
    missing = REQUIRED_MARKERS - markers
    if missing:
        fail(f"{path.name} missing markers: {sorted(missing)}")


def validate_tileset_alpha(path: Path) -> None:
    require(path)
    with Image.open(path) as image:
        if image.mode != "RGBA":
            fail(f"{path.name} must be RGBA")
        alpha = image.getchannel("A")
        alpha_values = alpha.get_flattened_data() if hasattr(alpha, "get_flattened_data") else alpha.getdata()
        transparent = sum(1 for value in alpha_values if value == 0)
        if transparent == 0:
            fail(f"{path.name} has no transparent diamond outside area")


def validate_godot_tileset(path: Path) -> None:
    require(path)
    text = path.read_text(encoding="utf-8")
    for snippet in [
        'type="TileSet"',
        'path="res://assets/maps/back_mountain_cliff_cave/tilesets/cliff_cave_ground_tiles.png"',
        "tile_shape = 1",
        "tile_layout = 5",
        "tile_size = Vector2i(64, 32)",
        'custom_data_layer_0/name = "tile_name"',
    ]:
        if snippet not in text:
            fail(f"{path.name} missing {snippet}")


def validate_tile_layers_scene(path: Path) -> None:
    require(path)
    text = path.read_text(encoding="utf-8")
    for snippet in [
        'type="TileSet"',
        "cliff_cave_ground_tiles.tres",
        '[node name="Day" type="Node2D" parent="."',
        '[node name="Night" type="Node2D" parent="."',
        'type="TileMapLayer"',
        "tile_map_data = PackedByteArray(",
        "tile_set = ExtResource",
    ]:
        if snippet not in text:
            fail(f"{path.name} missing {snippet}")
    if text.count('type="TileMapLayer"') < 6:
        fail(f"{path.name} should contain Day/Night Ground/Terrain/Overlay TileMapLayer nodes")


def main() -> None:
    require(SCENE_PATH)
    require(TILE_LAYERS_SCENE_PATH)
    require(SCRIPT_PATH)
    require(PROJECT_PATH)

    project = PROJECT_PATH.read_text(encoding="utf-8")
    if 'run/main_scene="res://scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn"' not in project:
        fail("project.godot main_scene is not BackMountainCliffCave.tscn")

    scene = SCENE_PATH.read_text(encoding="utf-8")
    for snippet in [
        "BackMountainCliffCave",
        "BackMountainCliffCaveGame.cs",
        "BackMountainCliffCaveTileLayers.tscn",
        'node name="TileLayers"',
        "CavePlayer.cs",
        "main_character.tres",
        "Camera2D",
        "UiLayer",
    ]:
        if snippet not in scene:
            fail(f"scene missing {snippet}")

    script = SCRIPT_PATH.read_text(encoding="utf-8")
    for snippet in [
        "BackMountainCliffCaveGame",
        "back_mountain_cliff_cave_day.tmx",
        "back_mountain_cliff_cave_night.tmx",
        "LoadVariant",
        "ConfigureTileLayerVariant",
        "ReadGroundTiles",
        "BuildStructures",
        "BuildCollision",
        "BuildLogicMarkers",
        "ConfigureTileMovement",
        "SetTilePath",
        "FindPath",
        "ScreenToTile",
        "InputEventMouseButton",
        "EnabledStructures",
        "StructureScales",
        "StructureTileOverrides",
    ]:
        if snippet not in script:
            fail(f"script missing {snippet}")
    if "BuildTileLayers" in script:
        fail("BackMountainCliffCaveGame.cs still builds tile sprites at runtime")

    player_script = (GODOT_ROOT / "scripts/CavePlayer.cs").read_text(encoding="utf-8")
    for snippet in [
        "ConfigureTileMovement",
        "SetTilePath",
        "TryStartTileStep",
        "MoveAlongTilePath",
        "SnapToCurrentTile",
        "AlignSpriteFeetToOrigin",
        "RefreshTileMoveActionOrder",
        "FaceBlockedTileMove",
        "TileMoveActionToDirection",
        "IsTileMoveActionPressed",
        "_pressedTileMoveActions",
        "Input.IsActionJustPressed",
        "Vector2I",
        "TileMoveAction.Up => new Vector2I(0, -1)",
        "TileMoveAction.Down => new Vector2I(0, 1)",
        "TileMoveAction.Left => new Vector2I(-1, 0)",
        "TileMoveAction.Right => new Vector2I(1, 0)",
    ]:
        if snippet not in player_script:
            fail(f"CavePlayer.cs missing {snippet}")
    if "direction += new Vector2I" in player_script:
        fail("CavePlayer.cs still combines tile directions and can create diagonal movement")
    if "FaceBlockedTileMove(direction);" not in player_script:
        fail("CavePlayer.cs does not face the requested direction when a tile move is blocked")
    if "position = Vector2(0, -74)" in scene:
        fail("BackMountainCliffCave.tscn still hardcodes the old sprite foot offset")

    validate_tileset_alpha(ASSET_ROOT / "tilesets/cliff_cave_ground_tiles.png")
    validate_godot_tileset(ASSET_ROOT / "tilesets/cliff_cave_ground_tiles.tres")
    validate_tile_layers_scene(TILE_LAYERS_SCENE_PATH)
    require(ASSET_ROOT / "maps/cliff_cave_ground_tiles.tsx")
    for name in REQUIRED_PROPS:
        require(ASSET_ROOT / "props" / name)

    validate_tmx(ASSET_ROOT / "maps/back_mountain_cliff_cave_day.tmx")
    validate_tmx(ASSET_ROOT / "maps/back_mountain_cliff_cave_night.tmx")

    print("PASS: Godot back mountain cliff cave integration")


if __name__ == "__main__":
    main()
