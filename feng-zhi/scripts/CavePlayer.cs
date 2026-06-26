using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.GodotIntegration;
using FengZhi.Foundation.Geometry;
using Godot;
using System;
using System.Collections.Generic;

namespace FengZhi;

/// <summary>
/// 探索场景主角控制器（ADR-0022 §3 输入映射 + §扩展点 1 → IIso4 端口）。
///
/// WASD 输入按 45 度等视距方向映射（W=右上/A=左上/S=左下/D=右下），
/// 屏幕方向由 <see cref="IsoProjection"/> 自然衍生；为保持屏幕速度恒定，
/// 屏幕速度对 iso 投影后归一化乘以恒定 <see cref="Speed"/>。
/// </summary>
public partial class CavePlayer : CharacterBody2D
{
	/// <summary>屏幕空间速度（像素/秒）。所有方向视觉速度一致。</summary>
	private const float Speed = 220.0f;

	private enum TileMoveAction
	{
		Up,
		Down,
		Left,
		Right,
	}

	private IIso4CharacterAnimator _animator = null!;
	private readonly Queue<Vector2I> _tilePath = new();
	private readonly List<TileMoveAction> _pressedTileMoveActions = new();
	private Func<Vector2I, Vector2>? _tileToScreen;
	private Func<Vector2I, bool>? _canEnterTile;
	private Vector2I _currentTile;
	private Vector2I? _targetTile;
	private Vector2 _targetPosition;
	private bool _tileMovementEnabled;
	private bool _movementFrozen;

	public bool TileMovementEnabled
	{
		get => _tileMovementEnabled;
		set => _tileMovementEnabled = value;
	}

	public bool MovementFrozen
	{
		get => _movementFrozen;
		set => _movementFrozen = value;
	}

	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AlignSpriteFeetToOrigin(sprite);
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
			return;
		}

		if (_tileMovementEnabled)
		{
			MoveAlongTilePath(delta);
			return;
		}

		// 45 度等视距：W/S/A/D 分别对应右上/左下/左上/右下。
		var cart = Vector2.Zero;
		if (Input.IsActionPressed("move_up"))    cart += new Vector2(0f, -1f); // W → 右上 / NE
		if (Input.IsActionPressed("move_down"))  cart += new Vector2(0f, +1f); // S → 左下 / SW
		if (Input.IsActionPressed("move_left"))  cart += new Vector2(-1f, 0f); // A → 左上 / NW
		if (Input.IsActionPressed("move_right")) cart += new Vector2(+1f, 0f); // D → 右下 / SE

		if (cart != Vector2.Zero)
			cart = cart.Normalized();

		// cart 方向 → iso 投影 → 屏幕方向归一化 → 屏幕速度恒定。
		var screenDir = IsoProjection.CartToScreen(cart);
		if (screenDir != Vector2.Zero)
			screenDir = screenDir.Normalized();
		Velocity = screenDir * Speed;

		_animator.SetMovementVector(cart);
		MoveAndSlide();
	}

	public void ConfigureTileMovement(
		Vector2I startTile,
		Func<Vector2I, Vector2> tileToScreen,
		Func<Vector2I, bool> canEnterTile)
	{
		_tileMovementEnabled = true;
		_currentTile = startTile;
		_tileToScreen = tileToScreen;
		_canEnterTile = canEnterTile;
		_tilePath.Clear();
		_pressedTileMoveActions.Clear();
		_targetTile = null;
		SnapToCurrentTile();
		_animator.SetMovementVector(Vector2.Zero);
	}

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
		if (!_tileMovementEnabled || _targetTile is not null)
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

	private void MoveAlongTilePath(double delta)
	{
		if (_tileToScreen is null)
		{
			Velocity = Vector2.Zero;
			_animator.SetMovementVector(Vector2.Zero);
			return;
		}

		if (_targetTile is null)
		{
			if (!TryConsumeKeyboardStep() && _tilePath.Count == 0)
			{
				Velocity = Vector2.Zero;
				_animator.SetMovementVector(Vector2.Zero);
				return;
			}

			_targetTile = _tilePath.Dequeue();
			_targetPosition = _tileToScreen(_targetTile.Value);
		}

		var targetTile = _targetTile.Value;
		var cartMovement = new Vector2(targetTile.X - _currentTile.X, targetTile.Y - _currentTile.Y);
		var nextPosition = Position.MoveToward(_targetPosition, Speed * (float)delta);
		Position = nextPosition;
		_animator.SetMovementVector(cartMovement);

		if (Position.DistanceSquaredTo(_targetPosition) > 0.25f)
		{
			return;
		}

		_currentTile = targetTile;
		_targetTile = null;
		SnapToCurrentTile();

		if (TryConsumeKeyboardStep() || _tilePath.Count > 0)
		{
			_targetTile = _tilePath.Dequeue();
			_targetPosition = _tileToScreen(_targetTile.Value);
		}
		else
		{
			_animator.SetMovementVector(Vector2.Zero);
		}
	}

	private bool TryConsumeKeyboardStep()
	{
		RefreshTileMoveActionOrder();
		if (_pressedTileMoveActions.Count == 0)
		{
			return false;
		}

		var action = _pressedTileMoveActions[^1];
		var direction = TileMoveActionToDirection(action);
		if (TryStartTileStep(direction))
		{
			return true;
		}

		FaceBlockedTileMove(direction);
		return false;
	}

	private void FaceBlockedTileMove(Vector2I direction)
	{
		if (direction == Vector2I.Zero)
		{
			return;
		}

		_animator.SetMovementVector(new Vector2(direction.X, direction.Y));
		_animator.SetMovementVector(Vector2.Zero);
	}

	private void RefreshTileMoveActionOrder()
	{
		for (var i = _pressedTileMoveActions.Count - 1; i >= 0; i--)
		{
			if (!IsTileMoveActionPressed(_pressedTileMoveActions[i]))
			{
				_pressedTileMoveActions.RemoveAt(i);
			}
		}

		RegisterTileMoveAction(TileMoveAction.Up);
		RegisterTileMoveAction(TileMoveAction.Down);
		RegisterTileMoveAction(TileMoveAction.Left);
		RegisterTileMoveAction(TileMoveAction.Right);
	}

	private void RegisterTileMoveAction(TileMoveAction action)
	{
		if (!IsTileMoveActionPressed(action))
		{
			return;
		}

		if (!Input.IsActionJustPressed(TileMoveActionToInputName(action))
			&& _pressedTileMoveActions.Contains(action))
		{
			return;
		}

		_pressedTileMoveActions.Remove(action);
		_pressedTileMoveActions.Add(action);
	}

	private static bool IsTileMoveActionPressed(TileMoveAction action)
	{
		return Input.IsActionPressed(TileMoveActionToInputName(action));
	}

	private static string TileMoveActionToInputName(TileMoveAction action)
	{
		return action switch
		{
			TileMoveAction.Up => "move_up",
			TileMoveAction.Down => "move_down",
			TileMoveAction.Left => "move_left",
			TileMoveAction.Right => "move_right",
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
		};
	}

	private static Vector2I TileMoveActionToDirection(TileMoveAction action)
	{
		return action switch
		{
			TileMoveAction.Up => new Vector2I(0, -1),     // W / ↑ → 右上
			TileMoveAction.Down => new Vector2I(0, 1),    // S / ↓ → 左下
			TileMoveAction.Left => new Vector2I(-1, 0),   // A / ← → 左上
			TileMoveAction.Right => new Vector2I(1, 0),   // D / → → 右下
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
		};
	}

	private void SnapToCurrentTile()
	{
		if (_tileToScreen is not null)
		{
			Position = _tileToScreen(_currentTile);
		}
	}

	private static void AlignSpriteFeetToOrigin(AnimatedSprite2D sprite)
	{
		var frames = sprite.SpriteFrames;
		if (frames is null)
		{
			return;
		}

		var hasUsedRect = false;
		var textureWidth = 0;
		var textureHeight = 0;
		var minX = int.MaxValue;
		var maxX = int.MinValue;
		var maxY = int.MinValue;

		foreach (var animationName in frames.GetAnimationNames())
		{
			var frameCount = frames.GetFrameCount(animationName);
			for (var frame = 0; frame < frameCount; frame++)
			{
				var texture = frames.GetFrameTexture(animationName, frame);
				var image = texture?.GetImage();
				if (image is null)
				{
					continue;
				}

				var used = image.GetUsedRect();
				if (used.Size == Vector2I.Zero)
				{
					continue;
				}

				hasUsedRect = true;
				textureWidth = image.GetWidth();
				textureHeight = image.GetHeight();
				minX = Math.Min(minX, used.Position.X);
				maxX = Math.Max(maxX, used.Position.X + used.Size.X);
				maxY = Math.Max(maxY, used.Position.Y + used.Size.Y);
			}
		}

		if (!hasUsedRect || textureWidth <= 0 || textureHeight <= 0)
		{
			return;
		}

		var footCenterX = (minX + maxX) * 0.5f;
		var localFootX = sprite.Centered ? footCenterX - textureWidth * 0.5f : footCenterX;
		var localFootY = sprite.Centered ? maxY - textureHeight * 0.5f : maxY;
		sprite.Position = new Vector2(
			-localFootX * sprite.Scale.X,
			-localFootY * sprite.Scale.Y);
	}
}
