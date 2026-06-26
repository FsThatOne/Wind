using System.Collections.Generic;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// 沿路径逐格移动 Sprite 的动画控制器。使用 Tween。
/// </summary>
public partial class CombatantMover : Node2D
{
    private Sprite2D? _sprite;
    private bool _isMoving;

    public bool IsMoving => _isMoving;

    public void Initialize(Sprite2D sprite)
    {
        _sprite = sprite;
    }

    public async void MoveAlongPath(IReadOnlyList<GridPosition> path, float stepDuration = 0.15f)
    {
        if (_sprite == null || path.Count < 2) return;

        _isMoving = true;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Linear);

        for (int i = 1; i < path.Count; i++)
        {
            var screenPos = IsoProjection.CartToScreen(new Vector2(path[i].X, path[i].Y));
            tween.TweenProperty(_sprite, "position", screenPos, stepDuration);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        _isMoving = false;
    }
}
