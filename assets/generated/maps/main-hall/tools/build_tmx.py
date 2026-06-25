#!/usr/bin/env python3
from __future__ import annotations

"""Build the Main Hall (正堂) Tiled files.

Usage:
    python3 assets/generated/maps/main-hall/tools/build_tmx.py

Outputs:
    assets/generated/maps/main-hall/maps/main_hall_ground_tiles.tsx
    assets/generated/maps/main-hall/maps/main_hall_day.tmx
    assets/generated/maps/main-hall/maps/main_hall_night.tmx

Also copies to feng-zhi/assets/maps/main_hall/maps/.
"""

import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path("assets/generated/maps/main-hall")
GODOT_MAPS = Path("feng-zhi/assets/maps/main_hall/maps")
GODOT_TILESETS = Path("feng-zhi/assets/maps/main_hall/tilesets")
MAP_W = 16
MAP_H = 16
TILE_W = 64
TILE_H = 32
TILESET_NAME = "main_hall_ground_tiles"


def layer_csv(grid: list[list[int]]) -> str:
    return ",".join(str(cell) for row in grid for cell in row)


def blank(value: int = 0) -> list[list[int]]:
    return [[value for _ in range(MAP_W)] for _ in range(MAP_H)]


def build_grids() -> dict[str, list[list[int]]]:
    ground = blank(0)
    terrain = blank(0)
    overlay = blank(0)
    collision = blank(0)

    walkable: set[tuple[int, int]] = set()

    # Diamond-shaped hall interior
    center_x, center_y = 7, 7
    for y in range(2, 13):
        for x in range(1, 15):
            dx = abs(x - center_x)
            dy = abs(y - center_y)
            if dx + dy <= 6:
                walkable.add((x, y))

    for y in range(MAP_H):
        for x in range(MAP_W):
            if (x, y) in walkable:
                ground[y][x] = 1
            else:
                collision[y][x] = 9

    # Terrain variation: wood floor patterns
    for x, y, gid in [
        (6, 5, 7), (7, 5, 7), (8, 5, 7),
        (5, 6, 8), (6, 6, 8), (8, 6, 8), (9, 6, 8),
        (5, 8, 10), (6, 8, 10), (8, 8, 10), (9, 8, 10),
        (6, 9, 11), (7, 9, 11), (8, 9, 11),
        (7, 4, 6), (7, 10, 6),
    ]:
        if (x, y) in walkable:
            terrain[y][x] = gid

    # Object blockers (props that block movement)
    for x, y in [
        (10, 7),  # stone_wall_motto
        (3, 7),   # weapon_rack
        (5, 5),   # tea_table
        (5, 4),   # chair
    ]:
        collision[y][x] = 11

    # Overlay: wall shadows at the perimeter
    for y in range(MAP_H):
        for x in range(MAP_W):
            if (x, y) not in walkable:
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nx, ny = x + dx, y + dy
                    if (nx, ny) in walkable:
                        overlay[y][x] = 13
                        break

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
    return float(tile_x * TILE_W), float(tile_y * TILE_H)


def add_marker(
    group: ET.Element,
    oid: int,
    name: str,
    obj_type: str,
    tile_x: int,
    tile_y: int,
    variant: str,
    enabled: bool = True,
) -> None:
    x, y = iso_object_xy(tile_x, tile_y)
    obj = ET.SubElement(
        group,
        "object",
        {
            "id": str(oid),
            "name": name,
            "type": obj_type,
            "x": str(x),
            "y": str(y),
            "width": str(TILE_W),
            "height": str(TILE_H),
        },
    )
    mood = "warm_daily" if variant == "day" else "silent_night"
    chapter_state = "prologue_daily" if variant == "day" else "prologue_massacre_trigger"
    add_properties(
        obj,
        {
            "id": name,
            "type": obj_type,
            "enabled": enabled,
            "interaction_id": f"{variant}.{name}",
            "chapter_state": chapter_state,
            "mood": mood,
            "tile_x": tile_x,
            "tile_y": tile_y,
        },
    )


def add_structure_objects(group: ET.Element, variant: str) -> None:
    structures = [
        (1, "stone_wall_motto", "stone_wall_motto.png", 10, 7),
        (2, "weapon_rack", "weapon_rack.png", 3, 7),
        (3, "tea_table", "tea_table.png", 5, 5),
        (4, "chair", "chair.png", 5, 4),
    ]
    if variant == "night":
        structures.append((5, "blood_letter", "blood_letter.png", 7, 8))

    mood = "warm_daily" if variant == "day" else "silent_night"
    for oid, name, image, tile_x, tile_y in structures:
        x, y = iso_object_xy(tile_x, tile_y)
        obj = ET.SubElement(
            group,
            "object",
            {
                "id": str(oid),
                "name": name,
                "type": "prop",
                "x": str(x),
                "y": str(y),
                "width": str(TILE_W),
                "height": str(TILE_H),
            },
        )
        add_properties(
            obj,
            {
                "image": f"../props/{image}",
                "anchor": "bottom_tile_baseline",
                "variant_state": variant,
                "mood": mood,
                "tile_x": tile_x,
                "tile_y": tile_y,
            },
        )


def write_tsx() -> None:
    tsx = ET.Element(
        "tileset",
        {
            "version": "1.10",
            "tiledversion": "1.10.2",
            "name": TILESET_NAME,
            "tilewidth": str(TILE_W),
            "tileheight": str(TILE_H),
            "tilecount": "24",
            "columns": "6",
        },
    )
    ET.SubElement(
        tsx,
        "image",
        {
            "source": f"../tilesets/{TILESET_NAME}.png",
            "width": "384",
            "height": "128",
        },
    )
    path = ROOT / f"maps/{TILESET_NAME}.tsx"
    ET.ElementTree(tsx).write(path, encoding="UTF-8", xml_declaration=True)


def write_tmx(variant: str) -> None:
    grids = build_grids()
    root = ET.Element(
        "map",
        {
            "version": "1.10",
            "tiledversion": "1.10.2",
            "orientation": "isometric",
            "renderorder": "right-down",
            "width": str(MAP_W),
            "height": str(MAP_H),
            "tilewidth": str(TILE_W),
            "tileheight": str(TILE_H),
            "infinite": "0",
            "nextlayerid": "7",
            "nextobjectid": "20",
        },
    )
    add_properties(
        root,
        {
            "scene_id": f"main_hall_{variant}",
            "scene_name": "正堂",
            "variant": variant,
            "mood": "warm_daily" if variant == "day" else "silent_night",
            "chapter_state": "prologue_daily"
            if variant == "day"
            else "prologue_massacre_trigger",
        },
    )
    ET.SubElement(root, "tileset", {"firstgid": "1", "source": f"{TILESET_NAME}.tsx"})

    for layer_id, name in enumerate(["Ground", "Terrain"], start=1):
        layer = ET.SubElement(
            root,
            "layer",
            {"id": str(layer_id), "name": name, "width": str(MAP_W), "height": str(MAP_H)},
        )
        data = ET.SubElement(layer, "data", {"encoding": "csv"})
        data.text = "\n" + layer_csv(grids[name]) + "\n"

    structures = ET.SubElement(root, "objectgroup", {"id": "3", "name": "Structures"})
    add_structure_objects(structures, variant)

    overlay = ET.SubElement(
        root, "layer", {"id": "4", "name": "Overlay", "width": str(MAP_W), "height": str(MAP_H)}
    )
    overlay_data = ET.SubElement(overlay, "data", {"encoding": "csv"})
    overlay_data.text = "\n" + layer_csv(grids["Overlay"]) + "\n"

    collision = ET.SubElement(
        root,
        "layer",
        {
            "id": "5",
            "name": "Collision",
            "width": str(MAP_W),
            "height": str(MAP_H),
            "visible": "0",
            "opacity": "0.35",
        },
    )
    collision_data = ET.SubElement(collision, "data", {"encoding": "csv"})
    collision_data.text = "\n" + layer_csv(grids["Collision"]) + "\n"

    logic = ET.SubElement(root, "objectgroup", {"id": "6", "name": "LogicMarkers"})
    markers = [
        ("exit_to_courtyard", "exit", 7, 11, True),
        ("stone_wall_inspect", "inspect", 10, 7, True),
        ("weapon_rack_inspect", "inspect", 3, 7, True),
        ("tea_table_interact", "interactable", 5, 5, True),
        ("master_talk", "interactable", 7, 3, True),
    ]
    for oid, (name, obj_type, tile_x, tile_y, enabled) in enumerate(markers, start=11):
        add_marker(logic, oid, name, obj_type, tile_x, tile_y, variant, enabled)

    filename = f"main_hall_{variant}.tmx"
    ET.ElementTree(root).write(ROOT / "maps" / filename, encoding="UTF-8", xml_declaration=True)


def copy_to_godot() -> None:
    GODOT_MAPS.mkdir(parents=True, exist_ok=True)
    src_maps = ROOT / "maps"
    for f in src_maps.iterdir():
        if f.suffix in (".tmx", ".tsx"):
            shutil.copy2(f, GODOT_MAPS / f.name)
            print(f"  Copied -> {GODOT_MAPS / f.name}")


def main() -> None:
    (ROOT / "maps").mkdir(parents=True, exist_ok=True)
    write_tsx()
    write_tmx("day")
    write_tmx("night")
    print(f"Generated:")
    print(f"  {ROOT / 'maps' / f'{TILESET_NAME}.tsx'}")
    print(f"  {ROOT / 'maps/main_hall_day.tmx'}")
    print(f"  {ROOT / 'maps/main_hall_night.tmx'}")
    print()
    copy_to_godot()


if __name__ == "__main__":
    main()
