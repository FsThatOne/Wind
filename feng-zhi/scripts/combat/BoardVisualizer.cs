using System.Collections.Generic;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// 棋盘可视化器。在 TileMapLayer 上高亮可达格，显示光标。
/// </summary>
public partial class BoardVisualizer : Node2D
{
    private TileMapLayer? _highlightLayer;
    private readonly HashSet<GridPosition> _highlightedCells = new();

    public override void _Ready()
    {
        _highlightLayer = GetNodeOrNull<TileMapLayer>("HighlightLayer");
    }

    public void ShowReachableCells(IReadOnlySet<GridPosition> cells)
    {
        ClearHighlights();
        if (_highlightLayer == null) return;

        foreach (var cell in cells)
        {
            _highlightedCells.Add(cell);
            _highlightLayer.SetCell(new Vector2I(cell.X, cell.Y), 0, new Vector2I(0, 0));
        }
    }

    public void ClearHighlights()
    {
        if (_highlightLayer == null) return;
        foreach (var cell in _highlightedCells)
        {
            _highlightLayer.EraseCell(new Vector2I(cell.X, cell.Y));
        }
        _highlightedCells.Clear();
    }

    public void SetCursorCell(GridPosition cell)
    {
        // 光标位置同步到屏幕坐标
        var screenPos = IsoProjection.CartToScreen(new Vector2(cell.X, cell.Y));
        Position = screenPos;
    }
}
