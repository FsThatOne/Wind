using Godot;

namespace FengZhi.Foundation.Geometry;

/// <summary>
/// Isometric diamond 投影工具（ADR-0022 §1）。
///
/// 纯静态、无状态、零 Godot 节点依赖；逻辑层（战棋、寻路）保持 cart 整数坐标，
/// 仅渲染层调用 <see cref="CartToScreen"/> 完成视觉投影；鼠标拾取等反向操作走
/// <see cref="ScreenToCart"/>。
///
/// 单 tile 几何：宽 64 / 高 32（2:1 菱形），与 ADR-0010 §层级规范 +
/// ADR-0022 §4 TileMapLayer 默认值对齐。
/// </summary>
public static class IsoProjection
{
    /// <summary>菱形 tile 宽度（像素）。对应 TileMapLayer.tile_size.x。</summary>
    public const float TileWidth = 64f;

    /// <summary>菱形 tile 高度（像素）。对应 TileMapLayer.tile_size.y。</summary>
    public const float TileHeight = 32f;

    /// <summary>
    /// cart 逻辑坐标 → 屏幕坐标。
    /// cart (1, 0) → (+W/2, +H/2)；cart (0, 1) → (-W/2, +H/2)；cart (1, 1) → (0, +H)。
    /// </summary>
    public static Vector2 CartToScreen(Vector2 cart) => new(
        (cart.X - cart.Y) * TileWidth * 0.5f,
        (cart.X + cart.Y) * TileHeight * 0.5f);

    /// <summary>
    /// 屏幕坐标 → cart 逻辑坐标。<see cref="CartToScreen"/> 的解析逆变换；
    /// 双向连续可逆（浮点精度内）。
    /// </summary>
    public static Vector2 ScreenToCart(Vector2 screen) => new(
        screen.X / TileWidth + screen.Y / TileHeight,
        -screen.X / TileWidth + screen.Y / TileHeight);
}
