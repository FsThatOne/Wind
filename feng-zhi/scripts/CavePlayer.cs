using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.GodotIntegration;
using FengZhi.Foundation.Geometry;
using Godot;

namespace FengZhi;

/// <summary>
/// 探索场景主角控制器（ADR-0022 §3 输入映射 + §扩展点 1 → IIso4 端口）。
///
/// WASD 输入按 ADR §3 映射成 cart 对角向量（W=(-1,-1)/A=(-1,+1)/S=(+1,+1)/D=(+1,-1)），
/// 屏幕方向由 <see cref="IsoProjection"/> 自然衍生；为保持屏幕速度恒定，
/// 屏幕速度对 iso 投影后归一化乘以恒定 <see cref="Speed"/>。
/// </summary>
public partial class CavePlayer : CharacterBody2D
{
	/// <summary>屏幕空间速度（像素/秒）。所有方向视觉速度一致。</summary>
	private const float Speed = 220.0f;

	private IIso4CharacterAnimator _animator = null!;

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
		// ADR-0022 §3：WASD → cart 对角向量。
		var cart = Vector2.Zero;
		if (Input.IsActionPressed("move_up"))    cart += new Vector2(-1f, -1f); // W → NW
		if (Input.IsActionPressed("move_down"))  cart += new Vector2(+1f, +1f); // S → SE
		if (Input.IsActionPressed("move_left"))  cart += new Vector2(-1f, +1f); // A → SW
		if (Input.IsActionPressed("move_right")) cart += new Vector2(+1f, -1f); // D → NE

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
}
