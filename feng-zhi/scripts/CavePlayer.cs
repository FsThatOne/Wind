using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.GodotIntegration;
using Godot;

namespace FengZhi;

public partial class CavePlayer : CharacterBody2D
{
	private const float Speed = 220.0f;

	private IDirectionalCharacterAnimator _animator = null!;

	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		var adapter = new EightDirectionAnimatedSprite2DAnimator
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
		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = direction * Speed;

		_animator.SetMovementVector(direction);
		MoveAndSlide();
	}
}
