using System;
using System.Collections.Generic;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// 棋盘可视化器。用运行时 128x64 等距菱形显示移动、光标、招式范围和目标预览。
/// </summary>
public partial class BoardVisualizer : Node2D
{
    private enum HighlightKind
    {
        MoveReachable,
        Cursor,
        AttackRange,
        TargetPreview,
    }

    private readonly Dictionary<HighlightKind, List<Node2D>> _highlights = new();

    private static readonly Vector2[] DiamondPoints =
    {
        new(-64f, 0f),
        new(0f, -32f),
        new(64f, 0f),
        new(0f, 32f),
    };

    public override void _Ready()
    {
        foreach (HighlightKind kind in Enum.GetValues(typeof(HighlightKind)))
            _highlights[kind] = new List<Node2D>();
    }

    public void ShowReachableCells(IReadOnlySet<GridPosition> cells)
    {
        ShowMoveReachable(cells);
    }

    public void ShowMoveReachable(IEnumerable<GridPosition> cells)
    {
        ClearMoveReachable();
        foreach (var cell in cells)
        {
            AddDiamond(cell, HighlightKind.MoveReachable, new Color(0.10f, 0.95f, 0.65f, 0.24f), new Color(0.20f, 1.00f, 0.75f, 0.90f));
        }
    }

    public void ShowAttackRange(IEnumerable<GridPosition> cells)
    {
        ClearAttackRange();
        foreach (var cell in cells)
        {
            AddDiamond(cell, HighlightKind.AttackRange, new Color(1.00f, 0.35f, 0.18f, 0.22f), new Color(1.00f, 0.56f, 0.25f, 0.80f));
        }
    }

    public void ShowTargetPreview(GridPosition cell)
    {
        ClearTargetPreview();
        AddDiamond(cell, HighlightKind.TargetPreview, new Color(1.00f, 0.80f, 0.15f, 0.16f), new Color(1.00f, 0.85f, 0.20f, 1.00f), outlineWidth: 3f);
    }

    public void SetCursorCell(GridPosition cell)
    {
        ClearCursor();
        AddDiamond(cell, HighlightKind.Cursor, new Color(1.00f, 0.95f, 0.55f, 0.08f), new Color(1.00f, 0.96f, 0.58f, 1.00f), outlineWidth: 2.5f);
    }

    public void ClearMoveReachable() => ClearKind(HighlightKind.MoveReachable);

    public void ClearCursor() => ClearKind(HighlightKind.Cursor);

    public void ClearAttackRange() => ClearKind(HighlightKind.AttackRange);

    public void ClearTargetPreview() => ClearKind(HighlightKind.TargetPreview);

    public void ClearTacticalPreview()
    {
        ClearAttackRange();
        ClearTargetPreview();
    }

    public void ClearHighlights()
    {
        foreach (HighlightKind kind in Enum.GetValues(typeof(HighlightKind)))
            ClearKind(kind);
    }

    private void AddDiamond(GridPosition cell, HighlightKind kind, Color fill, Color outline, float outlineWidth = 1.5f)
    {
        var root = new Node2D
        {
            Position = IsoProjection.CartToScreen(new Vector2(cell.X, cell.Y)),
            ZIndex = 4,
            YSortEnabled = false,
        };

        var polygon = new Polygon2D
        {
            Polygon = DiamondPoints,
            Color = fill,
            Antialiased = false,
        };
        root.AddChild(polygon);

        var line = new Line2D
        {
            Points = new[]
            {
                DiamondPoints[0], DiamondPoints[1], DiamondPoints[2], DiamondPoints[3], DiamondPoints[0],
            },
            Width = outlineWidth,
            DefaultColor = outline,
            Antialiased = false,
            Closed = true,
        };
        root.AddChild(line);

        AddChild(root);
        _highlights[kind].Add(root);
    }

    private void ClearKind(HighlightKind kind)
    {
        if (!_highlights.TryGetValue(kind, out var nodes))
            return;

        foreach (var node in nodes)
            node.QueueFree();
        nodes.Clear();
    }
}
