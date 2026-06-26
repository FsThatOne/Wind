using FengZhi.Foundation.Animation;

namespace FengZhi.Foundation.Combat.Board;

/// <summary>
/// 棋盘逻辑坐标。ADR-0022 §R1: 逻辑层保持 (int X, int Y)，零 isometric 知识。
/// </summary>
public readonly record struct GridPosition(int X, int Y)
{
    public int ManhattanDistance(GridPosition other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public IReadOnlyList<GridPosition> GetNeighbors() => new[]
    {
        new GridPosition(X + 1, Y),
        new GridPosition(X - 1, Y),
        new GridPosition(X, Y + 1),
        new GridPosition(X, Y - 1),
    };

    /// <summary>
    /// 从 from 到 to 推断 Iso4Direction 朝向。
    /// 优先 X 方向差值，平手看 Y。若重合返回 SE（默认朝向）。
    /// </summary>
    public static Iso4Direction InferFacing(GridPosition from, GridPosition to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (dx == 0 && dy == 0) return Iso4Direction.SE;

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? Iso4Direction.NE : Iso4Direction.SW;
        else
            return dy > 0 ? Iso4Direction.SE : Iso4Direction.NW;
    }

    public override string ToString() => $"({X},{Y})";
}
