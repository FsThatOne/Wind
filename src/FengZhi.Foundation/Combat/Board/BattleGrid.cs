namespace FengZhi.Foundation.Combat.Board;

/// <summary>
/// 战棋棋盘数据模型。管理地形障碍与角色占据状态。
/// GDD §Core Rules 2: 方格棋盘, MVP 8x8-12x12。
/// </summary>
public sealed class BattleGrid
{
    public int Width { get; }
    public int Height { get; }

    private readonly HashSet<GridPosition> _obstacles = new();
    private readonly Dictionary<GridPosition, string> _occupants = new();

    public BattleGrid(int width, int height, IEnumerable<GridPosition>? obstacles = null)
    {
        Width = width;
        Height = height;
        if (obstacles != null)
        {
            foreach (var pos in obstacles)
                _obstacles.Add(pos);
        }
    }

    public bool InBounds(GridPosition pos)
        => pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

    public bool IsObstacle(GridPosition pos)
        => _obstacles.Contains(pos);

    public bool IsOccupied(GridPosition pos)
        => _occupants.ContainsKey(pos);

    public bool IsPassable(GridPosition pos)
        => InBounds(pos) && !IsObstacle(pos) && !IsOccupied(pos);

    public void SetObstacle(GridPosition pos, bool isObstacle)
    {
        if (isObstacle)
            _obstacles.Add(pos);
        else
            _obstacles.Remove(pos);
    }

    public void SetOccupant(GridPosition pos, string? combatantId)
    {
        if (combatantId == null)
            _occupants.Remove(pos);
        else
            _occupants[pos] = combatantId;
    }

    public string? GetOccupant(GridPosition pos)
        => _occupants.TryGetValue(pos, out var id) ? id : null;

    public GridPosition? FindCombatant(string combatantId)
    {
        foreach (var kvp in _occupants)
        {
            if (kvp.Value == combatantId)
                return kvp.Key;
        }
        return null;
    }
}
