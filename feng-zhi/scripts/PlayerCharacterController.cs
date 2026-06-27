using System;
using System.Collections.Generic;
using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.GodotIntegration;
using FengZhi.Movement;
using Godot;

namespace FengZhi;

/// <summary>
/// 探索场景主角控制器。
/// 移动规则委托给 <see cref="IsoGridMovementController"/>，本类只负责输入、动画与节点位置同步。
/// </summary>
public partial class PlayerCharacterController : CharacterBody2D
{
	private const float Speed = 220.0f;

	private readonly IsoGridMovementController _movement = new() { Speed = Speed };
	private readonly List<IsoMoveAction> _pressedMoveActions = new();
	private IIso4CharacterAnimator _animator = null!;
	private bool _movementFrozen;

	public bool TileMovementEnabled
	{
		get => _movement.TileMovementEnabled;
		set => _movement.TileMovementEnabled = value;
	}

	public bool MovementFrozen
	{
		get => _movementFrozen;
		set => _movementFrozen = value;
	}

	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		var adapter = new Iso4AnimatedSprite2DAnimator
		{
			Name = "Animator",
			Sprite = sprite,
		};
		AddChild(adapter);
		_animator = adapter;
		_animator.Play(CharacterAnimState.Idle);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_movementFrozen)
		{
			Velocity = Vector2.Zero;
			_animator.SetMovementVector(Vector2.Zero);
			return;
		}

		var frame = _movement.Update(Position, ReadRequestedMoveAction(), delta);
		Position = frame.Position;
		Velocity = Vector2.Zero;
		_animator.SetMovementVector(frame.AnimationMovement);
		if (frame.StopAnimationAfterFacing)
		{
			_animator.SetMovementVector(Vector2.Zero);
		}
	}

	public void ConfigureTileMovement(
		Vector2I startTile,
		Func<Vector2I, Vector2> tileToScreen,
		Func<Vector2I, bool> canEnterTile)
	{
		_movement.ConfigureTileMovement(startTile, tileToScreen, canEnterTile);
		_pressedMoveActions.Clear();
		Position = _movement.SnapPositionToCurrentTile(Position);
		_animator.SetMovementVector(Vector2.Zero);
	}

	public void SetTilePath(IReadOnlyList<Vector2I> path)
	{
		_movement.SetTilePath(path);
	}

	public bool TryStartTileStep(Vector2I direction)
	{
		return _movement.TryStartTileStep(direction);
	}

	private IsoMoveAction? ReadRequestedMoveAction()
	{
		RefreshMoveActionOrder();
		return _pressedMoveActions.Count == 0 ? null : _pressedMoveActions[^1];
	}

	private void RefreshMoveActionOrder()
	{
		for (var i = _pressedMoveActions.Count - 1; i >= 0; i--)
		{
			if (!IsMoveActionPressed(_pressedMoveActions[i]))
			{
				_pressedMoveActions.RemoveAt(i);
			}
		}

		RegisterMoveAction(IsoMoveAction.Up);
		RegisterMoveAction(IsoMoveAction.Down);
		RegisterMoveAction(IsoMoveAction.Left);
		RegisterMoveAction(IsoMoveAction.Right);
	}

	private void RegisterMoveAction(IsoMoveAction action)
	{
		if (!IsMoveActionPressed(action))
		{
			return;
		}

		if (!Input.IsActionJustPressed(IsoMoveInput.ToInputName(action))
			&& _pressedMoveActions.Contains(action))
		{
			return;
		}

		_pressedMoveActions.Remove(action);
		_pressedMoveActions.Add(action);
	}

	private static bool IsMoveActionPressed(IsoMoveAction action)
	{
		return Input.IsActionPressed(IsoMoveInput.ToInputName(action));
	}
}
