"""江南等距 tileset 后处理：

支持两张 atlas page：
* sheet1 (16 tiles, 4×4)：基础地面 / 水域 / 岸 / 转角
* sheet2 (48 tiles, 6×8)：扩展地面 / 路径 / 桥 / 水变体 / 4 斜向岸 / 转角补全

每张 sheet：
1. 源图 RGB 白底 → chroma-key 白色 → 透明 RGBA
2. 下采样到 Godot 原生导入分辨率（每块 64×32, ADR-0022 规约）
3. 同时产出 4× 缩放 QA 预览

用法：
    python3 assets/generated/tilesets/jiangnan-iso/process_tileset.py
"""

from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
WHITE_THRESHOLD = 240  # all RGB ≥ 240 → 视作白底背景 → alpha 0


def chroma_key_white_to_alpha(img: Image.Image) -> Image.Image:
    """把近白像素转为 alpha=0。"""
    rgba = img.convert("RGBA")
    pixels = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = pixels[x, y]
            if r >= WHITE_THRESHOLD and g >= WHITE_THRESHOLD and b >= WHITE_THRESHOLD:
                pixels[x, y] = (0, 0, 0, 0)
    return rgba


def process_sheet(
    *,
    source_name: str,
    cols: int,
    rows: int,
    alpha_out_name: str,
    preview_out_name: str,
    godot_out_name: str,
) -> None:
    src_path = ROOT / source_name
    assert src_path.exists(), f"source missing: {src_path}"
    src = Image.open(src_path)
    print(f"\n[{source_name}] source: mode={src.mode} size={src.size}")

    rgba = chroma_key_white_to_alpha(src)
    rgba.save(ROOT / alpha_out_name)
    print(f"  saved alpha source: {alpha_out_name} (size={rgba.size})")

    godot_w = cols * 64
    godot_h = rows * 32
    godot = rgba.resize((godot_w, godot_h), Image.NEAREST)
    godot.save(ROOT / godot_out_name)
    print(
        f"  saved Godot-ready: {godot_out_name} "
        f"(size={godot.size}, per-tile=64×32, grid={cols}×{rows} tiles)"
    )

    preview_w = godot_w * 4
    preview_h = godot_h * 4
    preview = godot.resize((preview_w, preview_h), Image.NEAREST)
    preview.save(ROOT / preview_out_name)
    print(f"  saved 4× QA preview: {preview_out_name} (size={preview.size})")


def main() -> None:
    process_sheet(
        source_name="jiangnan-iso-tileset-4x4.png",
        cols=4,
        rows=4,
        alpha_out_name="jiangnan-iso-tileset-source-rgba.png",
        preview_out_name="jiangnan-iso-tileset-1024x512.png",
        godot_out_name="jiangnan-iso-tileset-64x32.png",
    )

    process_sheet(
        source_name="jiangnan-iso-tileset-6x8-supplement-raw.png",
        cols=6,
        rows=8,
        alpha_out_name="jiangnan-iso-tileset-supplement-source-rgba.png",
        preview_out_name="jiangnan-iso-tileset-supplement-1536x1024.png",
        godot_out_name="jiangnan-iso-tileset-supplement-64x32.png",
    )


if __name__ == "__main__":
    main()
