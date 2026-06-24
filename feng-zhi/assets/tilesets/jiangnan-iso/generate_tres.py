"""生成 Godot 4 TileSet (.tres) 资源文件，covering 64 江南等距 tiles。

输入  : tileset.manifest.json (本目录)
       两张 PNG: jiangnan-iso-tileset-sheet1.png / -sheet2.png (本目录)
输出  : jiangnan_iso_tileset.tres

格式参考  : feng-zhi/assets/maps/back_mountain_cliff_cave/tilesets/cliff_cave_ground_tiles.tres

执行 :
    python3 feng-zhi/assets/tilesets/jiangnan-iso/generate_tres.py
"""

from pathlib import Path
import json

ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "tileset.manifest.json"
OUTPUT = ROOT / "jiangnan_iso_tileset.tres"

GODOT_RES_PATH = "res://assets/tilesets/jiangnan-iso/"
SHEET1_FILENAME = "jiangnan-iso-tileset-sheet1.png"
SHEET2_FILENAME = "jiangnan-iso-tileset-sheet2.png"

# Godot 4 TileSet enum values:
#   TILE_SHAPE_ISOMETRIC      = 1
#   TILE_LAYOUT_DIAMOND_DOWN  = 5
TILE_SHAPE_ISOMETRIC = 1
TILE_LAYOUT_DIAMOND_DOWN = 5
TILE_SIZE_W = 64
TILE_SIZE_H = 32


def main() -> None:
    with MANIFEST.open() as f:
        manifest = json.load(f)

    sheet1 = manifest["tiles_sheet1"]
    sheet2 = manifest["tiles_sheet2"]

    lines: list[str] = []
    lines.append(
        '[gd_resource type="TileSet" load_steps=4 format=3 '
        'uid="uid://b_jiangnan_iso_tileset_v1"]\n'
    )
    lines.append(
        f'[ext_resource type="Texture2D" path="{GODOT_RES_PATH}{SHEET1_FILENAME}" '
        f'id="1_sheet1"]\n'
    )
    lines.append(
        f'[ext_resource type="Texture2D" path="{GODOT_RES_PATH}{SHEET2_FILENAME}" '
        f'id="2_sheet2"]\n'
    )
    lines.append("")

    def write_atlas_source(sub_id: str, ext_id: str, tiles: list[dict]) -> None:
        lines.append(f'[sub_resource type="TileSetAtlasSource" id="{sub_id}"]')
        lines.append(f'texture = ExtResource("{ext_id}")')
        lines.append(f"texture_region_size = Vector2i({TILE_SIZE_W}, {TILE_SIZE_H})")
        for t in tiles:
            col, row = t["col"], t["row"]
            name = t["id"]
            lines.append(f"{col}:{row}/0 = 0")
            lines.append(f'{col}:{row}/0/custom_data_0 = "{name}"')
        lines.append("")

    write_atlas_source("TileSetAtlasSource_sheet1", "1_sheet1", sheet1)
    write_atlas_source("TileSetAtlasSource_sheet2", "2_sheet2", sheet2)

    lines.append("[resource]")
    lines.append(f"tile_shape = {TILE_SHAPE_ISOMETRIC}")
    lines.append(f"tile_layout = {TILE_LAYOUT_DIAMOND_DOWN}")
    lines.append(f"tile_size = Vector2i({TILE_SIZE_W}, {TILE_SIZE_H})")
    lines.append('custom_data_layer_0/name = "tile_name"')
    lines.append("custom_data_layer_0/type = 4")  # 4 = TYPE_STRING
    lines.append('sources/0 = SubResource("TileSetAtlasSource_sheet1")')
    lines.append('sources/1 = SubResource("TileSetAtlasSource_sheet2")')
    lines.append("")

    OUTPUT.write_text("\n".join(lines))
    print(f"wrote {OUTPUT.name}: {len(sheet1)} tiles (sheet1) + {len(sheet2)} tiles (sheet2) = {len(sheet1) + len(sheet2)} total")


if __name__ == "__main__":
    main()
