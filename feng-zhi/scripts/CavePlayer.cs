using Godot;

namespace FengZhi;

public partial class CavePlayer : CharacterBody2D
{
    private const float Speed = 220.0f;
    private const float CardinalThreshold = 0.35f;

    private AnimatedSprite2D _sprite = null!;
    private StringName _currentAnimation = "walk_s";

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _sprite.Animation = _currentAnimation;
        _sprite.Frame = 1;
        _sprite.Stop();
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
                _sprite.Stop();
            }

            return;
        }

        var nextAnimation = GetAnimationName(direction);
        if (_currentAnimation != nextAnimation)
        {
            _currentAnimation = nextAnimation;
            _sprite.Play(_currentAnimation);
            return;
        }

        if (!_sprite.IsPlaying())
        {
            _sprite.Play(_currentAnimation);
        }
    }

    private static StringName GetAnimationName(Vector2 direction)
    {
        var horizontal = direction.X;
        var vertical = direction.Y;
        var hasHorizontal = Mathf.Abs(horizontal) >= CardinalThreshold;
        var hasVertical = Mathf.Abs(vertical) >= CardinalThreshold;

        if (hasVertical && hasHorizontal)
        {
            return vertical < 0.0f
                ? horizontal < 0.0f ? "walk_nw" : "walk_ne"
                : horizontal < 0.0f ? "walk_sw" : "walk_se";
        }

        if (hasVertical)
        {
            return vertical < 0.0f ? "walk_n" : "walk_s";
        }

        return horizontal < 0.0f ? "walk_w" : "walk_e";
    }
}
