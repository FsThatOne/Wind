# Protagonist Walk 8-Direction Sprite Sheet

Generated protagonist walking assets for Godot 4.7.

## Files

- `protagonist_walk_8dir_source.png` - green-background inspection sheet.
- `protagonist_walk_8dir.png` - transparent runtime sheet.
- `protagonist_walk_8dir_spriteframes.tres` - Godot `SpriteFrames` resource.
- `frames/walk_<direction>_<frame>.png` - individual frame PNGs.

## Layout

- Sheet size: `2560x1696`
- Cell size: `320x212`
- Directions: `s`, `se`, `e`, `ne`, `n`, `nw`, `w`, `sw`
- Frames per direction: `8`
- Animation speed: `10 FPS`

Rows are ordered top-to-bottom as:

1. `walk_s`
2. `walk_se`
3. `walk_e`
4. `walk_ne`
5. `walk_n`
6. `walk_nw`
7. `walk_w`
8. `walk_sw`

## Regeneration Notes

The accepted source candidate is `/Users/bytedance/.codex/generated_images/019ee2f0-1cd2-7f40-97f4-092cfecdac57/ig_0c1f18b5dee02044016a3663025f4481918a8b25eac04d63ba.png`. The final sheet was rebuilt by detecting each row's foreground components, assigning connected components to the nearest character body, dropping tiny orphan specks, scaling all frames uniformly by `1.42`, and centering each foreground bounding box in a stable `320x212` canvas. This avoids horizontal bleed from neighboring source columns while preserving the protagonist's in-game visual size.

The canonical frame order in `frames/walk_<direction>_00..07.png` is the playback order. Earlier working files used the generated order `0,1,5,6,4,2,7,3` only inside `SpriteFrames`; the PNG contents have now been reordered so filename order and animation order match.

The candidate provides independent `s`, `se`, `e`, `ne`, `n`, `w`, and `sw` rows. Most `nw` frames are derived from `ne` because the candidate does not contain an independent northwest row; `walk_nw_00.png` uses the unmirrored `walk_ne_00.png` orientation to stay consistent with the rest of the `nw` cycle. The final runtime PNGs have transparent backgrounds, no magenta outline, no baked shadow, and visible feet.
