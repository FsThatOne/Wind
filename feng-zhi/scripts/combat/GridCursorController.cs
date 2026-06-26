using System.Collections.Generic;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// 方向键/鼠标选格控制器。在可达范围内选择目标格。
/// </summary>
public partial class GridCursorController : Node2D
{
    [Signal]
    public delegate void CellConfirmedEventHandler(int x, int y);

    private GridPosition _cursorPos;
    private IReadOnlySet<GridPosition>? _validCells;
    private bool _enabled;
    private Sprite2D? _cursorSprite;

    public GridPosition CursorPosition => _cursorPos;

    public override void _Ready()
    {
        _cursorSprite = GetNodeOrNull<Sprite2D>("CursorSprite");
        Visible = false;
    }

    public void Enable(IReadOnlySet<GridPosition> validCells, GridPosition startPos)
    {
        _validCells = validCells;
        _cursorPos = startPos;
        _enabled = true;
        Visible = true;
        UpdateVisual();
    }

    public void Disable()
    {
        _enabled = false;
        _validCells = null;
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_enabled || _validCells == null) return;

        if (@event.IsActionPressed("ui_up"))
        {
            TryMove(new GridPosition(_cursorPos.X, _cursorPos.Y - 1));
        }
        else if (@event.IsActionPressed("ui_down"))
        {
            TryMove(new GridPosition(_cursorPos.X, _cursorPos.Y + 1));
        }
        else if (@event.IsActionPressed("ui_left"))
        {
            TryMove(new GridPosition(_cursorPos.X - 1, _cursorPos.Y));
        }
        else if (@event.IsActionPressed("ui_right"))
        {
            TryMove(new GridPosition(_cursorPos.X + 1, _cursorPos.Y));
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            if (_validCells.Contains(_cursorPos))
            {
                EmitSignal(SignalName.CellConfirmed, _cursorPos.X, _cursorPos.Y);
            }
        }
    }

    private void TryMove(GridPosition newPos)
    {
        if (_validCells != null && _validCells.Contains(newPos))
        {
            _cursorPos = newPos;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        var screenPos = IsoProjection.CartToScreen(new Vector2(_cursorPos.X, _cursorPos.Y));
        Position = screenPos;
    }
}
