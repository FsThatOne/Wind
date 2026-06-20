using Godot;

namespace FengZhi;

public partial class CavePlayer : CharacterBody2D
{
    private const float Speed = 220.0f;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * Speed;
        MoveAndSlide();
    }
}
