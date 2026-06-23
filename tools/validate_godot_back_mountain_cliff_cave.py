#!/usr/bin/env python3
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"
ASSET_ROOT = GODOT_ROOT / "assets/maps/back_mountain_cliff_cave"
SCENE_PATH = GODOT_ROOT / "scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn"
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


def main() -> None:
    require(SCENE_PATH)
    require(SCRIPT_PATH)
    require(PROJECT_PATH)

    project = PROJECT_PATH.read_text(encoding="utf-8")
    if 'run/main_scene="res://scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn"' not in project:
        fail("project.godot main_scene is not BackMountainCliffCave.tscn")

    scene = SCENE_PATH.read_text(encoding="utf-8")
    for snippet in [
        "BackMountainCliffCave",
        "BackMountainCliffCaveGame.cs",
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
        "BuildTileLayers",
        "BuildStructures",
        "BuildCollision",
        "BuildLogicMarkers",
    ]:
        if snippet not in script:
            fail(f"script missing {snippet}")

    require(ASSET_ROOT / "tilesets/cliff_cave_ground_tiles.png")
    require(ASSET_ROOT / "maps/cliff_cave_ground_tiles.tsx")
    for name in REQUIRED_PROPS:
        require(ASSET_ROOT / "props" / name)

    validate_tmx(ASSET_ROOT / "maps/back_mountain_cliff_cave_day.tmx")
    validate_tmx(ASSET_ROOT / "maps/back_mountain_cliff_cave_night.tmx")

    print("PASS: Godot back mountain cliff cave integration")


if __name__ == "__main__":
    main()
