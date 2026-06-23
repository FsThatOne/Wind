#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

DIRECTIONS = ["ne", "se", "sw", "nw"]
CELL = 128


def main() -> None:
    repo_root = Path(__file__).resolve().parents[5]
    source = repo_root / "feng-zhi/assets/character/main_character_iso4/walk"
    output_root = repo_root / "assets/generated/maps/back-mountain-cliff-cave"

    atlas = Image.new("RGBA", (CELL * 4, CELL * 4), (0, 0, 0, 0))

    for row, direction in enumerate(DIRECTIONS):
        for col in range(4):
            frame_path = source / f"walk_{direction}_{col:02d}.png"
            if not frame_path.exists():
                raise SystemExit(f"missing frame: {frame_path}")
            with Image.open(frame_path) as frame_image:
                frame = frame_image.convert("RGBA")
            if frame.size != (CELL, CELL):
                raise SystemExit(f"{frame_path} size is {frame.size}, expected {(CELL, CELL)}")
            atlas.alpha_composite(frame, (col * CELL, row * CELL))

    out = output_root / "characters/main_character_iso4_walk_atlas.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(out)

    preview = Image.new("RGBA", atlas.size, (32, 32, 38, 255))
    preview.alpha_composite(atlas)
    draw = ImageDraw.Draw(preview)
    for i in range(5):
        draw.line((i * CELL, 0, i * CELL, CELL * 4), fill=(255, 255, 255, 60))
        draw.line((0, i * CELL, CELL * 4, i * CELL), fill=(255, 255, 255, 60))
    for i in range(1, 4):
        draw.line((0, i * CELL - 1, CELL * 4, i * CELL - 1), fill=(255, 80, 80, 180))
    preview_out = output_root / "previews/main_character_iso4_walk_atlas_preview.png"
    preview_out.parent.mkdir(parents=True, exist_ok=True)
    preview.save(preview_out)

    print(out)
    print(preview_out)


if __name__ == "__main__":
    main()
