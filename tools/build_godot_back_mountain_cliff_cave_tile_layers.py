#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
import tempfile
import textwrap
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
GODOT_ROOT = ROOT / "feng-zhi"
GODOT = Path("/Applications/Godot_mono.app/Contents/MacOS/Godot")
MAP_ROOT = GODOT_ROOT / "assets/maps/back_mountain_cliff_cave/maps"
OUTPUT_SCENE = "res://scenes/back_mountain_cliff_cave/BackMountainCliffCaveTileLayers.tscn"
TILESET_PATH = "res://assets/maps/back_mountain_cliff_cave/tilesets/cliff_cave_ground_tiles.tres"

MAPS = {
    "Day": MAP_ROOT / "back_mountain_cliff_cave_day.tmx",
    "Night": MAP_ROOT / "back_mountain_cliff_cave_night.tmx",
}
LAYERS = ("Ground", "Terrain", "Overlay")
MAP_WIDTH = 16
MAP_HEIGHT = 16
TILESET_COLUMNS = 6


def parse_csv(text: str) -> list[int]:
    values = [int(value.strip()) for value in text.split(",") if value.strip()]
    if len(values) != MAP_WIDTH * MAP_HEIGHT:
        raise ValueError(f"expected {MAP_WIDTH * MAP_HEIGHT} gids, got {len(values)}")
    return values


def read_layer_data(path: Path) -> dict[str, list[int]]:
    root = ET.parse(path).getroot()
    data: dict[str, list[int]] = {}
    for name in LAYERS:
        layer = root.find(f"./layer[@name='{name}']")
        if layer is None:
            raise ValueError(f"{path} missing layer {name}")
        csv = layer.findtext("data") or ""
        data[name] = parse_csv(csv)
    return data


def build_gdscript(layer_data: dict[str, dict[str, list[int]]]) -> str:
    layer_data_literal = json.dumps(layer_data, ensure_ascii=False)
    return textwrap.dedent(
        f"""
        extends SceneTree

        const OUTPUT_SCENE := "{OUTPUT_SCENE}"
        const TILESET_PATH := "{TILESET_PATH}"
        const MAP_WIDTH := {MAP_WIDTH}
        const TILESET_COLUMNS := {TILESET_COLUMNS}
        const LAYERS := ["Ground", "Terrain", "Overlay"]
        const Z_INDEX := {{
            "Ground": 0,
            "Terrain": 100,
            "Overlay": 500,
        }}
        const LAYER_DATA := {layer_data_literal}

        func _init() -> void:
            var tile_set := load(TILESET_PATH)
            if tile_set == null:
                push_error("Unable to load " + TILESET_PATH)
                quit(1)
                return

            var root := Node2D.new()
            root.name = "BackMountainCliffCaveTileLayers"
            root.position = Vector2(544, 80)
            root.y_sort_enabled = true

            for variant_name in ["Day", "Night"]:
                var variant := Node2D.new()
                variant.name = variant_name
                variant.visible = variant_name == "Day"
                variant.y_sort_enabled = true
                root.add_child(variant)
                variant.owner = root

                for layer_name in LAYERS:
                    var layer := TileMapLayer.new()
                    layer.name = layer_name
                    layer.tile_set = tile_set
                    layer.z_index = Z_INDEX[layer_name]
                    layer.y_sort_enabled = true
                    variant.add_child(layer)
                    layer.owner = root

                    var gids: Array = LAYER_DATA[variant_name][layer_name]
                    for index in gids.size():
                        var gid: int = gids[index]
                        if gid == 0:
                            continue
                        var tile_id := gid - 1
                        var coords := Vector2i(tile_id % TILESET_COLUMNS, tile_id / TILESET_COLUMNS)
                        var cell := Vector2i(index % MAP_WIDTH, index / MAP_WIDTH)
                        layer.set_cell(cell, 0, coords)

            var packed := PackedScene.new()
            var pack_result := packed.pack(root)
            if pack_result != OK:
                push_error("Unable to pack tile layer scene: " + str(pack_result))
                quit(1)
                return

            var save_result := ResourceSaver.save(packed, OUTPUT_SCENE)
            if save_result != OK:
                push_error("Unable to save tile layer scene: " + str(save_result))
                quit(1)
                return

            print("Saved " + OUTPUT_SCENE)
            quit()
        """
    ).strip()


def main() -> None:
    if not GODOT.exists():
        raise FileNotFoundError(GODOT)

    layer_data = {variant: read_layer_data(path) for variant, path in MAPS.items()}
    script = build_gdscript(layer_data)

    with tempfile.TemporaryDirectory() as tmp:
        script_path = Path(tmp) / "build_back_mountain_cliff_cave_tile_layers.gd"
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
