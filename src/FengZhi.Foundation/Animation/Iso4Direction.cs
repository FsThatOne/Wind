namespace FengZhi.Foundation.Animation;

/// <summary>
/// 4 斜方向枚举（ADR-0022 §2）。
///
/// 屏幕方向直觉对应：
/// <list type="bullet">
///   <item><description>NE = ↗（cart x+ 朝向）</description></item>
///   <item><description>SE = ↘（cart y+ 朝向）</description></item>
///   <item><description>SW = ↙（cart x- 朝向）</description></item>
///   <item><description>NW = ↖（cart y- 朝向）</description></item>
/// </list>
///
/// 替换 ADR-0021 §扩展点 1 的 8 方向 <c>EightDirection</c>；
/// 与 ADR-0021 主端口 <see cref="Facing"/>（Left/Right，sprite FlipH）正交。
/// </summary>
public enum Iso4Direction
{
    NE,
    SE,
    SW,
    NW,
}
