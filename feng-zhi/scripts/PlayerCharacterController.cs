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
	private CollisionPolygon2D _collisionPolygon = null!;

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
		sprite.Position = new Vector2(0f, -32f);
		var adapter = new Iso4AnimatedSprite2DAnimator
		{
			Name = "Animator",
			Sprite = sprite,
		};
		AddChild(adapter);
		_animator = adapter;
		_animator.Play(CharacterAnimState.Idle);

		_collisionPolygon = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
		CharacterFootprint.ApplyTo(_collisionPolygon);

		var camera = GetNode<Camera2D>("Camera2D");
		camera.Position = new Vector2(0f, -250f);
		camera.ResetSmoothing();
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
		var desiredPosition = frame.Position;
		var toDesired = desiredPosition - Position;
		var distance = toDesired.Length();

		if (distance < 0.5f)
		{
			Velocity = Vector2.Zero;
			Position = desiredPosition;
		}
		else
		{
			Velocity = toDesired.Normalized() * Speed;
			var beforePos = Position;
			MoveAndSlide();
			var afterPos = Position;
			var actualMove = afterPos - beforePos;
			var movedDistance = actualMove.Length();
			var expectedDistance = distance;

			if (movedDistance < expectedDistance * 0.4f && expectedDistance > 1.0f)
			{
				_movement.ReportPhysicsCollision(afterPos);
				Velocity = Vector2.Zero;
			}
			else if (afterPos.DistanceSquaredTo(desiredPosition) <= 2.0f)
			{
				Position = desiredPosition;
			}
		}

		_animator.SetMovementVector(frame.AnimationMovement);
		if (frame.StopAnimationAfterFacing)
		{
			_animator.SetMovementVector(Vector2.Zero);
		}
	}

	public void ConfigureTileMovement(
		Vector2I startTile,
		Func<Vector2I, Vector2> tileToScreen,
		Func<Vector2I, bool> canEnterTile,
		Func<Vector2, Vector2I>? screenToTile = null)
	{
		_movement.ConfigureTileMovement(startTile, tileToScreen, canEnterTile, screenToTile);
		_pressedMoveActions.Clear();
		Position = _movement.SnapPositionToCurrentTile(Position);
		_animator.SetMovementVector(Vector2.Zero);

		var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		var camera = GetNode<Camera2D>("Camera2D");
		GD.Print($"[Player] After ConfigureTileMovement: Player.GlobalPos=({GlobalPosition.X:F0},{GlobalPosition.Y:F0}), Sprite local=({sprite.Position.X:F0},{sprite.Position.Y:F0}), Camera local=({camera.Position.X:F0},{camera.Position.Y:F0}), Camera.GlobalPos=({camera.GlobalPosition.X:F0},{camera.GlobalPosition.Y:F0})");
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
