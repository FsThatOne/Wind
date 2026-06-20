# Protagonist Movement Sprite

Source portrait: `/Users/bytedance/Downloads/主角.png`

Generated asset for the pseudo-2.5D pixel wuxia prototype.

## Files

- `protagonist_walk_8dir_source.png` - original generated sheet with chroma-key background.
- `protagonist_walk_8dir.png` - transparent 8-direction spritesheet.
- `protagonist_walk_8dir_spriteframes.tres` - Godot `SpriteFrames` resource for `AnimatedSprite2D`.
- `frames/` - 32 sliced transparent frame PNGs.

## Sheet Layout

- Sheet size: `888x1776`
- Grid: `4 columns x 8 rows`
- Cell size: `222x222`
- Frame count: `32`
- Animation speed: `8 FPS`

Direction rows, top to bottom:

1. `walk_s`
2. `walk_se`
3. `walk_e`
4. `walk_ne`
5. `walk_n`
6. `walk_nw`
7. `walk_w`
8. `walk_sw`

Frame columns, left to right:

1. step A
2. neutral
3. step B
4. neutral

## Godot Usage

Use `protagonist_walk_8dir_spriteframes.tres` with an `AnimatedSprite2D`.

Suggested node setup:

```text
CharacterBody2D
└── AnimatedSprite2D
    sprite_frames = res://assets/characters/protagonist/protagonist_walk_8dir_spriteframes.tres
```

The project already uses nearest-neighbor canvas texture filtering in `project.godot`,
which is the desired import behavior for pixel art.

If Godot reimports these PNGs, keep:

- Filter: nearest / disabled
- Mipmaps: disabled
- Compression: lossless
- Alpha border fix: enabled

## Notes

The source image was generated at `887x1774`, then padded to `888x1776` so the
sheet divides cleanly into a `4x8` grid. The character pixels were not scaled.
