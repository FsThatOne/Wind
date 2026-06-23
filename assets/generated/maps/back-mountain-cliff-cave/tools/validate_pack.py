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
    required_targets = {
        name: targets.get(name)
        for name in ["wine_pickup", "storage_shelf", "memory_marker", "rest_spot"]
    }
    missing = [name for name, point in required_targets.items() if point is None]
    if start is None or missing:
        fail(
            "path check needs exit_to_back_mountain and reachable marker tile_x/tile_y "
            f"for {missing}"
        )

    reachable = flood_fill(start, blocked, width, height)
    for name, point in required_targets.items():
        if point not in reachable:
            fail(f"{name} at {point} is not reachable from exit {start}")


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
