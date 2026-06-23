#!/usr/bin/env python3
from __future__ import annotations

"""Build the Back Mountain Cliff Cave Tiled files.

Usage:
    python3 assets/generated/maps/back-mountain-cliff-cave/tools/build_tmx.py

Outputs:
    assets/generated/maps/back-mountain-cliff-cave/maps/cliff_cave_ground_tiles.tsx
    assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_day.tmx
    assets/generated/maps/back-mountain-cliff-cave/maps/back_mountain_cliff_cave_night.tmx
"""

import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path("assets/generated/maps/back-mountain-cliff-cave")
MAP_W = 16
MAP_H = 16
TILE_W = 64
TILE_H = 32


def layer_csv(grid: list[list[int]]) -> str:
    return ",".join(str(cell) for row in grid for cell in row)


def blank(value: int = 0) -> list[list[int]]:
    return [[value for _ in range(MAP_W)] for _ in range(MAP_H)]


def build_grids() -> dict[str, list[list[int]]]:
    ground = blank(0)
    terrain = blank(0)
    overlay = blank(0)
    collision = blank(0)

    # Tiled gid: tileset local id + 1. 0 means empty.
    # Walkable cave footprint for approved "storage + nook" layout.
    walkable: set[tuple[int, int]] = set()
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
        (4, 4, 3),
        (5, 4, 3),
        (6, 4, 16),
        (7, 4, 16),
        (10, 8, 15),
        (11, 8, 15),
        (12, 8, 18),
        (13, 8, 18),
        (9, 13, 6),
        (10, 13, 6),
        (11, 13, 23),
        (12, 13, 23),
        (3, 8, 14),
        (4, 8, 14),
        (5, 8, 21),
        (8, 9, 19),
        (9, 9, 19),
    ]:
        if 0 <= x < MAP_W and 0 <= y < MAP_H:
            terrain[y][x] = gid

    # Object blockers represented in collision.
    for x, y in [
        (5, 5),
        (6, 5),
        (4, 9),
        (5, 9),
        (6, 9),
        (11, 6),
        (12, 6),
        (2, 3),
        (5, 10),
    ]:
        collision[y][x] = 11

    # Overlay cave wall shadows near rear.
    for x, y in [
        (3, 3),
        (4, 3),
        (5, 3),
        (6, 3),
        (10, 6),
        (11, 6),
        (12, 6),
        (13, 6),
    ]:
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
    # Tiled object coords for isometric maps are editor-space; stable tile
    # anchors keep validation independent of large prop image dimensions.
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


def write_tsx() -> None:
    tsx = ET.Element(
        "tileset",
        {
            "version": "1.10",
            "tiledversion": "1.10.2",
            "name": "cliff_cave_ground_tiles",
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
            "source": "../tilesets/cliff_cave_ground_tiles.png",
            "width": "384",
            "height": "128",
        },
    )
    path = ROOT / "maps/cliff_cave_ground_tiles.tsx"
    ET.ElementTree(tsx).write(path, encoding="UTF-8", xml_declaration=True)


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
            "scene_id": f"back_mountain_cliff_cave_{variant}",
            "scene_name": "后山崖洞",
            "variant": variant,
            "mood": "warm_daily" if variant == "day" else "silent_night",
            "chapter_state": "prologue_daily"
            if variant == "day"
            else "prologue_massacre_trigger",
        },
    )
    ET.SubElement(root, "tileset", {"firstgid": "1", "source": "cliff_cave_ground_tiles.tsx"})

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
        ("exit_to_back_mountain", "exit", 1, 8, True),
        ("wine_pickup", "interactable", 7, 6, True),
        ("storage_shelf", "inspect", 7, 9, True),
        ("memory_marker", "inspect", 13, 8, True),
        ("rest_spot", "trigger", 11, 11, True),
        ("blocked_rock", "blocker", 2, 3, True),
        ("blocked_storage", "blocker", 5, 10, True),
    ]
    for oid, (name, obj_type, tile_x, tile_y, enabled) in enumerate(markers, start=11):
        add_marker(logic, oid, name, obj_type, tile_x, tile_y, variant, enabled)

    filename = f"back_mountain_cliff_cave_{variant}.tmx"
    ET.ElementTree(root).write(ROOT / "maps" / filename, encoding="UTF-8", xml_declaration=True)


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
