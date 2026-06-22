using Godot;

namespace FengZhi;

public partial class CavePlayer : CharacterBody2D
{
	private const float Speed = 220.0f;
	private const float DirectionSectorRadians = Mathf.Pi / 4.0f;
	private const float DirectionHysteresisRadians = Mathf.Pi / 18.0f;

	private static readonly StringName[] DirectionAnimations =
	{
		"walk_e",
		"walk_se",
		"walk_s",
		"walk_sw",
		"walk_w",
		"walk_nw",
		"walk_n",
        "walk_ne"
	};

	private AnimatedSprite2D _sprite = null!;
	private StringName _currentAnimation = "walk_s";

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Animation = _currentAnimation;
		_sprite.SetFrameAndProgress(1, 0.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = direction * Speed;

		UpdateAnimation(direction);
		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 direction)
	{
		if (direction == Vector2.Zero)
		{
			if (_sprite.IsPlaying())
			{
				_sprite.Pause();
			}

			return;
		}

		var nextAnimation = GetAnimationName(direction, _currentAnimation);
		if (_currentAnimation != nextAnimation)
		{
			var frame = _sprite.Frame;
			var frameProgress = _sprite.FrameProgress;

			_currentAnimation = nextAnimation;
			_sprite.Play(_currentAnimation);
			PreserveWalkCyclePhase(frame, frameProgress);
			return;
		}

		if (!_sprite.IsPlaying())
		{
			_sprite.Play(_currentAnimation);
		}
	}

	private void PreserveWalkCyclePhase(int frame, float frameProgress)
	{
		var frameCount = _sprite.SpriteFrames?.GetFrameCount(_currentAnimation) ?? 0;
		if (frameCount <= 0)
		{
			return;
		}

		_sprite.SetFrameAndProgress(Mathf.PosMod(frame, frameCount), frameProgress);
	}

	private static StringName GetAnimationName(Vector2 direction, StringName currentAnimation)
	{
		var angle = Mathf.Atan2(direction.Y, direction.X);
		var sector = GetSector(angle);
		var currentSector = GetSector(currentAnimation);

		if (currentSector >= 0 && currentSector != sector)
		{
			var currentCenter = GetSectorCenter(currentSector);
			var distanceFromCurrent = Mathf.Abs(NormalizeAngle(angle - currentCenter));
			if (distanceFromCurrent <= DirectionSectorRadians * 0.5f + DirectionHysteresisRadians)
			{
				sector = currentSector;
			}
		}

		return DirectionAnimations[sector];
	}

	private static int GetSector(float angle)
	{
		var rounded = Mathf.RoundToInt(angle / DirectionSectorRadians);
		return Mathf.PosMod(rounded, DirectionAnimations.Length);
	}

	private static int GetSector(StringName animation)
	{
		for (var i = 0; i < DirectionAnimations.Length; i++)
		{
			if (DirectionAnimations[i] == animation)
			{
				return i;
			}
		}

		return -1;
	}

	private static float GetSectorCenter(int sector)
	{
		return NormalizeAngle(sector * DirectionSectorRadians);
	}

	private static float NormalizeAngle(float angle)
	{
		var twoPi = Mathf.Pi * 2.0f;
		angle = Mathf.PosMod(angle + Mathf.Pi, twoPi);
		return angle - Mathf.Pi;
	}
}
