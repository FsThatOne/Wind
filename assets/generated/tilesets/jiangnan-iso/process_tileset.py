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


def apply_diamond_mask(img: Image.Image, cols: int, rows: int) -> Image.Image:
    """对每个 cell 内的 2:1 inscribed diamond 应用几何遮罩。

    每个 cell 大小 = (W/cols, H/rows)，中心 (cw/2, ch/2)。
    Diamond 内部：|x - cx|/(cw/2) + |y - cy|/(ch/2) <= 1
    Diamond 外部：alpha = 0；内部：保留原 RGB + alpha=255。

    优于 chroma-key 白底法：image_gen 的 BG 渐变到 #eaeaea 而 tile 内部高光
    可达 #ffffff，颜色阈值无法分离；几何遮罩按 cell 切干净，与 ADR-0022
    inscribed diamond 规约完全对齐。
    """
    rgba = img.convert("RGBA")
    pixels = rgba.load()
    w, h = rgba.size
    cell_w = w // cols
    cell_h = h // rows
    half_w = cell_w / 2.0
    half_h = cell_h / 2.0

    for cell_row in range(rows):
        for cell_col in range(cols):
            x0 = cell_col * cell_w
            y0 = cell_row * cell_h
            for ly in range(cell_h):
                for lx in range(cell_w):
                    # 归一化到 [-1, 1] × [-1, 1]
                    nx = (lx - half_w + 0.5) / half_w
                    ny = (ly - half_h + 0.5) / half_h
                    # 0.85 激进缩小 15%：image_gen 画的钻石实际小于 inscribed
                    # 边界，inscribed 边缘几像素本就是 BG 色 (≈#f0f0f0)，必须
                    # 缩到钻石"内核"。配套 .png.import 应保持 fix_alpha_border。
                    if abs(nx) + abs(ny) > 0.85:
                        pixels[x0 + lx, y0 + ly] = (0, 0, 0, 0)
                    else:
                        r, g, b, _ = pixels[x0 + lx, y0 + ly]
                        pixels[x0 + lx, y0 + ly] = (r, g, b, 255)

    return rgba


# 兼容 main() 旧调用名 (现在用 apply_diamond_mask 替代)
def chroma_key_white_to_alpha(img: Image.Image) -> Image.Image:
    raise NotImplementedError("旧法已废弃，请使用 apply_diamond_mask(img, cols, rows)")


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

    rgba = apply_diamond_mask(src, cols, rows)
    rgba.save(ROOT / alpha_out_name)
    print(f"  saved alpha source: {alpha_out_name} (size={rgba.size})")

    godot_w = cols * 64
    godot_h = rows * 32
    # BOX 滤波 (区域均值) 而非 NEAREST：image_gen 在 diamond 内沿画了接近 #f0f0f0
    # 的亮像素，NEAREST 6× 下采样会保留这些亮带形成白晕；BOX 把边缘亮带与
    # 内部色块混合，保留 pixel-art 块感的同时消除白边伪影。
    godot = rgba.resize((godot_w, godot_h), Image.BOX)
    # 在小分辨率再次几何 mask 一次，确保 cell 角 4 像素 alpha=0
    godot = apply_diamond_mask(godot, cols, rows)
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
