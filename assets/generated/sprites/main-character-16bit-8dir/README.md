# Main Character 16-Bit 8-Direction Sprite Bundle

Generated from `/Users/bytedance/Downloads/主角.png`.

## Identity Lock

- Young male wuxia protagonist
- Long black hair in a low ponytail
- White and ink-black layered robe
- Black inner garment
- Dark waist sash with small ornament
- Pale blue-green ribbon accent

## Output

- `idle/sheet-transparent.png`: 2x2 idle sheet, 128 px cells
- `walk_8dir/sheet-transparent.png`: 8x4 walk sheet, 128 px cells
- `run_8dir/sheet-transparent.png`: 8x4 run sheet, 128 px cells
- `walk_8dir/direction_frames/`: direction-named walk frames
- `run_8dir/direction_frames/`: direction-named run frames
- `walk_8dir/direction_strips/`: one horizontal strip per direction
- `run_8dir/direction_strips/`: one horizontal strip per direction
- `walk_8dir/direction_gifs/`: one preview GIF per direction
- `run_8dir/direction_gifs/`: one preview GIF per direction
- `raw/`: copied raw generated images

## Direction Order

Rows in `walk_8dir/sheet-transparent.png` and `run_8dir/sheet-transparent.png`:

1. `s`
2. `sw`
3. `w`
4. `nw`
5. `n`
6. `ne`
7. `e`
8. `se`

Columns are frames `00..03` for each loop.

## QC Notes

- Postprocessed with transparent background.
- Cell size: 128x128.
- `edge_touch_frames` is empty for idle, walk, and run.
- `run-8dir-raw.png` was rejected because some frames touched cell edges.
- `run-8dir-raw-v2.png` is the accepted running source.
- Direction correction applied after visual QC:
  - `ne` uses a deterministic per-frame horizontal mirror of `nw`.
  - `se` uses a deterministic per-frame horizontal mirror of `sw`.
  - Frame order is preserved.
  - This fixes the raw generated sheets repeating east-facing poses in the diagonal east rows.
