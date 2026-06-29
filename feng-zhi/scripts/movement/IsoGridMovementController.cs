using System;
using System.Collections.Generic;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi.Movement;

/// <summary>
/// 全局等视距逐格移动控制器。
/// 只负责移动状态机与格子目标计算，不依赖 Godot Node、输入系统或动画节点。
/// 支持物理碰撞中断：当外部物理引擎（如 MoveAndSlide）阻挡移动时，
/// 控制器可接受中断位置并更新当前 tile，允许玩家停在家具前方的 sub-tile 位置。
/// </summary>
public sealed class IsoGridMovementController
{
	private readonly Queue<Vector2I> _tilePath = new();
	private Func<Vector2I, Vector2>? _tileToScreen;
	private Func<Vector2I, bool>? _canEnterTile;
	private Func<Vector2, Vector2I>? _screenToTile;
	private Vector2I _currentTile;
	private Vector2I? _targetTile;
	private Vector2 _targetPosition;
	private Vector2? _freeStepTargetPosition;
	private Vector2 _freeStepMovement;

	/// <summary>
	/// 玩家在当前 tile 内的实际停留偏移（相对于 tile 中心）。
	/// 正常走格时为零；被物理碰撞阻挡停在家具前方时非零。
	/// </summary>
	private Vector2 _intraTileOffset;

	public float Speed { get; set; } = 220.0f;

	public bool TileMovementEnabled { get; set; }

	public void ConfigureTileMovement(
		Vector2I startTile,
		Func<Vector2I, Vector2> tileToScreen,
		Func<Vector2I, bool> canEnterTile,
		Func<Vector2, Vector2I>? screenToTile = null)
	{
		TileMovementEnabled = true;
		_currentTile = startTile;
		_tileToScreen = tileToScreen;
		_canEnterTile = canEnterTile;
		_screenToTile = screenToTile;
		_tilePath.Clear();
		_targetTile = null;
		_freeStepTargetPosition = null;
		_intraTileOffset = Vector2.Zero;
	}

	public Vector2 GetCurrentTileCenter() =>
		_tileToScreen?.Invoke(_currentTile) ?? Vector2.Zero;

	public Vector2 GetActualRestPosition() =>
		GetCurrentTileCenter() + _intraTileOffset;

	public Vector2I CurrentTile => _currentTile;

	public bool IsMidStep => _targetTile.HasValue;

	public Vector2 SnapPositionToCurrentTile(Vector2 fallbackPosition) =>
		_tileToScreen?.Invoke(_currentTile) ?? fallbackPosition;

	public void SetTilePath(IReadOnlyList<Vector2I> path)
	{
		_tilePath.Clear();
		for (var i = 0; i < path.Count; i++)
		{
			if (path[i] != _currentTile)
			{
				_tilePath.Enqueue(path[i]);
			}
		}
	}

	public bool TryStartTileStep(Vector2I direction)
	{
		if (!TileMovementEnabled || _targetTile is not null)
		{
			return false;
		}

		var next = _currentTile + direction;
		if (_canEnterTile?.Invoke(next) != true)
		{
			return false;
		}

		_tilePath.Clear();
		_tilePath.Enqueue(next);
		return true;
	}

	public IsoGridMovementFrame Update(
		Vector2 currentPosition,
		IsoMoveAction? requestedAction,
		double delta)
	{
		return TileMovementEnabled
			? MoveAlongTilePath(currentPosition, requestedAction, delta)
			: MoveFreeIsoStep(currentPosition, requestedAction, delta);
	}

	private IsoGridMovementFrame MoveAlongTilePath(
		Vector2 currentPosition,
		IsoMoveAction? requestedAction,
		double delta)
	{
		if (_tileToScreen is null)
		{
			return IsoGridMovementFrame.Idle(currentPosition);
		}

		if (_targetTile is null)
		{
			var consumed = TryConsumeRequestedStep(requestedAction, out var blockedDirection);
			if (!consumed && _tilePath.Count == 0)
			{
				return blockedDirection is null
					? IsoGridMovementFrame.Idle(currentPosition)
					: IsoGridMovementFrame.Face(currentPosition, blockedDirection.Value);
			}

			_targetTile = _tilePath.Dequeue();
			_targetPosition = _tileToScreen(_targetTile.Value);
		}

		var targetTile = _targetTile.Value;
		var cartMovement = new Vector2(targetTile.X - _currentTile.X, targetTile.Y - _currentTile.Y);
		var nextPosition = currentPosition.MoveToward(_targetPosition, Speed * (float)delta);
		if (nextPosition.DistanceSquaredTo(_targetPosition) > 0.25f)
		{
			return IsoGridMovementFrame.Move(nextPosition, cartMovement);
		}

		_currentTile = targetTile;
		_targetTile = null;
		_intraTileOffset = Vector2.Zero;
		nextPosition = _tileToScreen(_currentTile);

		var nextConsumed = TryConsumeRequestedStep(requestedAction, out _);
		if (nextConsumed || _tilePath.Count > 0)
		{
			_targetTile = _tilePath.Dequeue();
			_targetPosition = _tileToScreen(_targetTile.Value);
		}

		return IsoGridMovementFrame.Move(nextPosition, cartMovement);
	}

	private IsoGridMovementFrame MoveFreeIsoStep(
		Vector2 currentPosition,
		IsoMoveAction? requestedAction,
		double delta)
	{
		if (_freeStepTargetPosition is null)
		{
			if (requestedAction is null)
			{
				return IsoGridMovementFrame.Idle(currentPosition);
			}

			var direction = IsoMoveInput.ToDirection(requestedAction.Value);
			_freeStepMovement = new Vector2(direction.X, direction.Y);
			_freeStepTargetPosition = currentPosition + IsoProjection.CartToScreen(_freeStepMovement);
		}

		var nextPosition = currentPosition.MoveToward(_freeStepTargetPosition.Value, Speed * (float)delta);
		if (nextPosition.DistanceSquaredTo(_freeStepTargetPosition.Value) > 0.25f)
		{
			return IsoGridMovementFrame.Move(nextPosition, _freeStepMovement);
		}

		nextPosition = _freeStepTargetPosition.Value;
		_freeStepTargetPosition = null;
		return requestedAction is null
			? IsoGridMovementFrame.Idle(nextPosition)
			: IsoGridMovementFrame.Move(nextPosition, _freeStepMovement);
	}

	private bool TryConsumeRequestedStep(IsoMoveAction? requestedAction, out Vector2? blockedDirection)
	{
		blockedDirection = null;
		if (requestedAction is null)
		{
			return false;
		}

		var direction = IsoMoveInput.ToDirection(requestedAction.Value);
		if (TryStartTileStep(direction))
		{
			return true;
		}

		blockedDirection = new Vector2(direction.X, direction.Y);
		return false;
	}

	/// <summary>
	/// 物理碰撞中断回调：当 MoveAndSlide 因碰撞未能到达目标 tile 中心时调用。
	/// 控制器将当前 tile 更新为玩家实际所在 tile，记录 intra-tile 偏移，
	/// 并清除未完成的 step 状态。
	/// </summary>
	public void ReportPhysicsCollision(Vector2 actualPosition)
	{
		_targetTile = null;
		_tilePath.Clear();

		if (_screenToTile != null)
		{
			_currentTile = _screenToTile(actualPosition);
		}

		var center = _tileToScreen?.Invoke(_currentTile) ?? actualPosition;
		_intraTileOffset = actualPosition - center;

		if (_intraTileOffset.Length() > 64f)
		{
			_intraTileOffset = Vector2.Zero;
		}
	}

	/// <summary>
	/// 重置当前位置到 tile 中心（清除 intra-tile 偏移）。
	/// 用于离开碰撞区域时自动对齐。
	/// </summary>
	public void SnapToCenter()
	{
		_intraTileOffset = Vector2.Zero;
	}
}

public readonly record struct IsoGridMovementFrame(
	Vector2 Position,
	Vector2 AnimationMovement,
	bool StopAnimationAfterFacing)
{
	public static IsoGridMovementFrame Idle(Vector2 position) => new(position, Vector2.Zero, false);

	public static IsoGridMovementFrame Move(Vector2 position, Vector2 movement) => new(position, movement, false);

	public static IsoGridMovementFrame Face(Vector2 position, Vector2 direction) => new(position, direction, true);
}

public enum IsoMoveAction
{
	Up,
	Down,
	Left,
	Right,
}

public static class IsoMoveInput
{
	public static string ToInputName(IsoMoveAction action)
	{
		return action switch
		{
			IsoMoveAction.Up => "move_up",
			IsoMoveAction.Down => "move_down",
			IsoMoveAction.Left => "move_left",
			IsoMoveAction.Right => "move_right",
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
		};
	}

	public static Vector2I ToDirection(IsoMoveAction action)
	{
		return action switch
		{
			IsoMoveAction.Up => new Vector2I(0, -1),     // W / ↑ → 右上 / walk_ne
			IsoMoveAction.Down => new Vector2I(0, 1),    // S / ↓ → 左下 / walk_sw
			IsoMoveAction.Left => new Vector2I(-1, 0),   // A / ← → 左上 / walk_nw
			IsoMoveAction.Right => new Vector2I(1, 0),   // D / → → 右下 / walk_se
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
		};
	}
}
