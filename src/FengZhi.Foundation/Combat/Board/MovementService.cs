namespace FengZhi.Foundation.Combat.Board;

/// <summary>
/// 战棋移动计算服务。BFS 算法计算可达范围与最短路径。
/// GDD §Core Rules 4: 移动到合法格, default_move_range 2-5。
/// </summary>
public static class MovementService
{
    /// <summary>
    /// 从起点 BFS 计算所有在 moveRange 步内可达的格子。
    /// 其他角色占据的格子视为不可通行（GDD：每个角色占据 1 格）。
    /// 起点本身包含在结果中（原地停留）。
    /// </summary>
    public static IReadOnlySet<GridPosition> GetReachableCells(
        BattleGrid grid, GridPosition origin, int moveRange)
    {
        var result = new HashSet<GridPosition> { origin };
        if (moveRange <= 0) return result;

        var distances = new Dictionary<GridPosition, int> { [origin] = 0 };
        var queue = new Queue<GridPosition>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distances[current];
            if (currentDist >= moveRange) continue;

            foreach (var neighbor in current.GetNeighbors())
            {
                if (distances.ContainsKey(neighbor)) continue;
                if (!grid.IsPassable(neighbor)) continue;

                distances[neighbor] = currentDist + 1;
                result.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return result;
    }

    /// <summary>
    /// 从起点到目标的 BFS 最短路径（含起终点）。
    /// 若不可达返回 null。终点必须可通行。
    /// </summary>
    public static IReadOnlyList<GridPosition>? FindPath(
        BattleGrid grid, GridPosition from, GridPosition to, int maxSteps)
    {
        if (from == to) return new[] { from };
        if (!grid.InBounds(to) || !grid.IsPassable(to)) return null;

        var parents = new Dictionary<GridPosition, GridPosition> { [from] = from };
        var distances = new Dictionary<GridPosition, int> { [from] = 0 };
        var queue = new Queue<GridPosition>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distances[current];
            if (currentDist >= maxSteps) continue;

            foreach (var neighbor in current.GetNeighbors())
            {
                if (parents.ContainsKey(neighbor)) continue;
                if (!grid.IsPassable(neighbor) && neighbor != to) continue;
                if (neighbor == to && !grid.IsPassable(neighbor)) continue;

                parents[neighbor] = current;
                distances[neighbor] = currentDist + 1;

                if (neighbor == to)
                {
                    return ReconstructPath(parents, from, to);
                }

                queue.Enqueue(neighbor);
            }
        }

        return null;
    }

    private static IReadOnlyList<GridPosition> ReconstructPath(
        Dictionary<GridPosition, GridPosition> parents, GridPosition from, GridPosition to)
    {
        var path = new List<GridPosition>();
        var current = to;
        while (current != from)
        {
            path.Add(current);
            current = parents[current];
        }
        path.Add(from);
        path.Reverse();
        return path;
    }
}
