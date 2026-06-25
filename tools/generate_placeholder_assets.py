#!/usr/bin/env python3
"""Generate placeholder assets (gray blocks) for the full prologue demo.

Usage:
    python tools/generate_placeholder_assets.py

Outputs tilesets, character sprites, and prop images under feng-zhi/assets/.
"""
from __future__ import annotations

import json
import sys
from dataclasses import dataclass, field
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("Error: Pillow is required. Install with: pip install Pillow", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parent.parent
GODOT_ASSETS = ROOT / "feng-zhi" / "assets"

TILE_W, TILE_H = 64, 32
ATLAS_COLS, ATLAS_ROWS = 6, 4
CHAR_FRAME_SIZE = 128
PROP_W, PROP_H = 128, 96
DIRECTIONS = ["ne", "se", "sw", "nw"]
WALK_FRAMES = 4


# ---------------------------------------------------------------------------
# Data definitions
# ---------------------------------------------------------------------------

@dataclass
class TileSpec:
    id: int
    name: str
    category: str
    walkable: bool
    color: tuple[int, int, int]


@dataclass
class SceneSpec:
    key: str
    dir_name: str
    tiles: list[TileSpec] = field(default_factory=list)
    props: list[str] = field(default_factory=list)


@dataclass
class CharSpec:
    key: str
    dir_name: str
    color: tuple[int, int, int]


SCENES: list[SceneSpec] = [
    SceneSpec("cliff_cave", "back_mountain_cliff_cave", props=[
        "wine_jars_group", "wine_jar_single", "storage_shelf",
        "bamboo_basket_herbs", "cloth_bundle", "wooden_crate_low",
        "rest_mat", "small_stool", "sister_mark", "oil_lamp_dim",
    ]),
    SceneSpec("main_hall", "main_hall", props=[
        "stone_wall_motto", "weapon_rack", "tea_table", "chair", "blood_letter",
    ]),
    SceneSpec("study", "study", props=[
        "desk", "bookshelf", "secret_compartment", "scroll_pile",
    ]),
    SceneSpec("training_ground", "training_ground", props=[
        "wooden_sword", "training_dummy", "stone_bench", "fence_post",
    ]),
    SceneSpec("living_quarter", "living_quarter", props=[
        "herb_drying_rack", "medicine_pot", "bed_mat", "herb_plant",
    ]),
    SceneSpec("mountain_gate", "mountain_gate", props=[
        "gate_pillar_left", "gate_pillar_right", "gate_plaque",
    ]),
    SceneSpec("back_mountain_path", "back_mountain_path", props=[
        "path_rock", "wild_grass", "old_tree",
    ]),
]

CHARACTERS: list[CharSpec] = [
    CharSpec("main_character", "main_character_iso4", (200, 200, 200)),
    CharSpec("sister_baitan", "sister_baitan_iso4", (200, 180, 180)),
    CharSpec("master", "master_iso4", (120, 120, 120)),
    CharSpec("senior_brother", "senior_brother_iso4", (160, 160, 160)),
    CharSpec("junior_brother", "junior_brother_iso4", (190, 190, 190)),
]

TILE_TEMPLATES: list[TileSpec] = [
    TileSpec(0, "ground_01", "ground", True, (180, 180, 180)),
    TileSpec(1, "ground_02", "ground", True, (175, 175, 175)),
    TileSpec(2, "ground_03", "ground", True, (170, 170, 170)),
    TileSpec(3, "ground_04", "ground", True, (185, 185, 185)),
    TileSpec(4, "ground_05", "ground", True, (178, 178, 178)),
    TileSpec(5, "ground_06", "ground", True, (172, 172, 172)),
    TileSpec(6, "terrain_01", "terrain", True, (145, 145, 145)),
    TileSpec(7, "terrain_02", "terrain", True, (140, 140, 140)),
    TileSpec(8, "terrain_03", "terrain", True, (135, 135, 135)),
    TileSpec(9, "terrain_04", "terrain", True, (150, 150, 150)),
    TileSpec(10, "terrain_05", "terrain", True, (142, 142, 142)),
    TileSpec(11, "terrain_06", "terrain", True, (138, 138, 138)),
    TileSpec(12, "wall_01", "boundary", False, (80, 80, 80)),
    TileSpec(13, "wall_02", "boundary", False, (75, 75, 75)),
    TileSpec(14, "wall_03", "boundary", False, (85, 85, 85)),
    TileSpec(15, "wall_04", "boundary", False, (70, 70, 70)),
    TileSpec(16, "wall_05", "boundary", False, (90, 90, 90)),
    TileSpec(17, "wall_06", "boundary", False, (78, 78, 78)),
    TileSpec(18, "overlay_01", "overlay", True, (110, 115, 120)),
    TileSpec(19, "overlay_02", "overlay", True, (105, 110, 115)),
    TileSpec(20, "overlay_03", "overlay", True, (115, 120, 125)),
    TileSpec(21, "overlay_04", "overlay", True, (108, 113, 118)),
    TileSpec(22, "special_01", "ground", True, (160, 165, 170)),
    TileSpec(23, "special_02", "ground", True, (155, 160, 165)),
]


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------

def _diamond_polygon(cx: int, cy: int, w: int, h: int) -> list[tuple[int, int]]:
    hw, hh = w // 2, h // 2
    return [(cx, cy - hh), (cx + hw, cy), (cx, cy + hh), (cx - hw, cy)]


def _draw_diamond_tile(draw: ImageDraw.ImageDraw, x: int, y: int, color: tuple[int, int, int]) -> None:
    cx = x + TILE_W // 2
    cy = y + TILE_H // 2
    poly = _diamond_polygon(cx, cy, TILE_W - 2, TILE_H - 2)
    draw.polygon(poly, fill=(*color, 255), outline=(60, 60, 60, 255))


def _try_load_font(size: int) -> ImageFont.ImageFont:
    for name in ["Arial.ttf", "DejaVuSans.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"]:
        try:
            return ImageFont.truetype(name, size)
        except (OSError, IOError):
            continue
    return ImageFont.load_default()


FONT_SMALL = _try_load_font(10)
FONT_MEDIUM = _try_load_font(14)
FONT_LABEL = _try_load_font(11)


# ---------------------------------------------------------------------------
# Tileset generation
# ---------------------------------------------------------------------------

def generate_tileset(scene: SceneSpec) -> tuple[int, int]:
    out_dir = GODOT_ASSETS / "maps" / scene.dir_name / "tilesets"
    out_dir.mkdir(parents=True, exist_ok=True)

    tileset_name = f"{scene.dir_name}_ground_tiles"
    tiles = scene.tiles if scene.tiles else TILE_TEMPLATES

    img = Image.new("RGBA", (ATLAS_COLS * TILE_W, ATLAS_ROWS * TILE_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    for tile in tiles:
        col = tile.id % ATLAS_COLS
        row = tile.id // ATLAS_COLS
        x = col * TILE_W
        y = row * TILE_H
        _draw_diamond_tile(draw, x, y, tile.color)

    png_path = out_dir / f"{tileset_name}.png"
    img.save(png_path)

    manifest = {
        "name": tileset_name,
        "tile_width": TILE_W,
        "tile_height": TILE_H,
        "columns": ATLAS_COLS,
        "rows": ATLAS_ROWS,
        "tile_count": len(tiles),
        "image": f"{tileset_name}.png",
        "image_width": ATLAS_COLS * TILE_W,
        "image_height": ATLAS_ROWS * TILE_H,
        "tiles": [
            {
                "id": t.id,
                "name": t.name,
                "category": t.category,
                "walkable": t.walkable,
            }
            for t in tiles
        ],
    }
    json_path = out_dir / f"{tileset_name}.json"
    json_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")

    return 1, 1  # png_count, json_count


# ---------------------------------------------------------------------------
# Character sprite generation
# ---------------------------------------------------------------------------

def _draw_character_frame(
    char: CharSpec, direction: str, frame: int
) -> Image.Image:
    img = Image.new("RGBA", (CHAR_FRAME_SIZE, CHAR_FRAME_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx, cy = CHAR_FRAME_SIZE // 2, CHAR_FRAME_SIZE // 2
    body_w, body_h = 40, 60

    sway = (frame % 2) * 3 - 1
    bx = cx + sway

    draw.ellipse(
        [bx - 12, cy - body_h // 2 - 18, bx + 12, cy - body_h // 2 + 6],
        fill=(*char.color, 255),
        outline=(60, 60, 60, 255),
    )

    draw.rectangle(
        [bx - body_w // 2, cy - body_h // 2 + 4, bx + body_w // 2, cy + body_h // 2],
        fill=(*char.color, 255),
        outline=(60, 60, 60, 255),
    )

    label = f"{direction.upper()}"
    bbox = draw.textbbox((0, 0), label, font=FONT_SMALL)
    tw = bbox[2] - bbox[0]
    draw.text((cx - tw // 2, cy + body_h // 2 + 4), label, fill=(255, 255, 255, 255), font=FONT_SMALL)

    return img


def generate_characters() -> int:
    total = 0
    for char in CHARACTERS:
        char_dir = GODOT_ASSETS / "character" / char.dir_name / "walk"
        char_dir.mkdir(parents=True, exist_ok=True)

        frames: dict[str, list[Image.Image]] = {}
        for direction in DIRECTIONS:
            frames[direction] = []
            for f in range(WALK_FRAMES):
                frame_img = _draw_character_frame(char, direction, f)
                frame_path = char_dir / f"walk_{direction}_{f:02d}.png"
                frame_img.save(frame_path)
                frames[direction].append(frame_img)
                total += 1

        atlas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        for di, direction in enumerate(DIRECTIONS):
            for fi, frame_img in enumerate(frames[direction]):
                x = fi * CHAR_FRAME_SIZE
                y = di * CHAR_FRAME_SIZE
                atlas.alpha_composite(frame_img, (x, y))

        atlas_path = GODOT_ASSETS / "character" / char.dir_name / f"{char.key}_atlas.png"
        atlas.save(atlas_path)
        total += 1

    return total


# ---------------------------------------------------------------------------
# Prop generation
# ---------------------------------------------------------------------------

def generate_props() -> int:
    total = 0
    for scene in SCENES:
        props_dir = GODOT_ASSETS / "maps" / scene.dir_name / "props"
        props_dir.mkdir(parents=True, exist_ok=True)

        for prop_name in scene.props:
            img = Image.new("RGBA", (PROP_W, PROP_H), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)

            draw.rounded_rectangle(
                [4, 4, PROP_W - 4, PROP_H - 4],
                radius=6,
                fill=(130, 130, 130, 200),
                outline=(80, 80, 80, 255),
                width=2,
            )

            label = prop_name.replace("_", " ")
            max_chars = 14
            lines = []
            words = label.split()
            current = ""
            for w in words:
                if current and len(current) + 1 + len(w) > max_chars:
                    lines.append(current)
                    current = w
                else:
                    current = f"{current} {w}".strip()
            if current:
                lines.append(current)

            line_h = 14
            start_y = (PROP_H - len(lines) * line_h) // 2
            for li, line in enumerate(lines):
                bbox = draw.textbbox((0, 0), line, font=FONT_LABEL)
                tw = bbox[2] - bbox[0]
                draw.text(
                    ((PROP_W - tw) // 2, start_y + li * line_h),
                    line,
                    fill=(255, 255, 255, 255),
                    font=FONT_LABEL,
                )

            prop_path = props_dir / f"{prop_name}.png"
            img.save(prop_path)
            total += 1

    return total


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    print("=== Placeholder Asset Generator ===\n")

    print("Generating tilesets...")
    ts_png, ts_json = 0, 0
    for scene in SCENES:
        p, j = generate_tileset(scene)
        ts_png += p
        ts_json += j
        print(f"  [{scene.key}] tileset PNG + JSON")
    print(f"  Total: {ts_png} PNGs, {ts_json} JSONs\n")

    print("Generating character sprites...")
    char_count = generate_characters()
    print(f"  Total: {char_count} files ({len(CHARACTERS)} characters)\n")

    print("Generating props...")
    prop_count = generate_props()
    print(f"  Total: {prop_count} prop PNGs\n")

    grand_total = ts_png + ts_json + char_count + prop_count
    print(f"=== Done: {grand_total} files generated ===")


if __name__ == "__main__":
    main()
