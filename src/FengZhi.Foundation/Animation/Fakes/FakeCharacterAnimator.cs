using Godot;

namespace FengZhi.Foundation.Animation.Fakes;

/// <summary>
/// 测试用 Fake — 纯内存状态，不依赖 Godot 节点。
/// 同时实现 ICharacterAnimator 与 IDirectionalCharacterAnimator，
/// 让 8 方向消费方也可以注入同一替身。
/// </summary>
public sealed class FakeCharacterAnimator : IDirectionalCharacterAnimator
{
    public CharacterAnimState? Current { get; private set; }
    public bool IsPlaying { get; private set; }
    public float NormalizedProgress { get; private set; }
    public Facing Facing { get; private set; } = Facing.Right;
    public bool LoopRequested { get; private set; }
    public int PlayCallCount { get; private set; }
    public EightDirection? CurrentDirection { get; private set; }
    public Vector2 LastMovementVector { get; private set; }

    public event Action<CharacterAnimState>? Finished;
    public event Action<AnimationFrameEvent>? FrameEvent;

    public void Play(CharacterAnimState state, bool loop = true)
    {
        Current = state;
        LoopRequested = loop;
        IsPlaying = true;
        NormalizedProgress = 0f;
        PlayCallCount++;
    }

    public void Stop()
    {
        IsPlaying = false;
        Current = null;
        NormalizedProgress = 0f;
        CurrentDirection = null;
    }

    public void SetFacing(Facing facing) => Facing = facing;

    public void SetProgress(float progress) =>
        NormalizedProgress = Math.Clamp(progress, 0f, 1f);

    public void EmitFinished(CharacterAnimState state)
    {
        IsPlaying = false;
        NormalizedProgress = 1f;
        Finished?.Invoke(state);
    }

    public void EmitFrameEvent(string tag, float progress) =>
        FrameEvent?.Invoke(new AnimationFrameEvent(tag, progress));

    public void SetMovementVector(Vector2 movement)
    {
        LastMovementVector = movement;
        if (movement == Vector2.Zero)
        {
            IsPlaying = false;
            return;
        }

        CurrentDirection = ResolveDirection(movement);
        IsPlaying = true;
    }

    private static EightDirection ResolveDirection(Vector2 v)
    {
        var sector = (int)Math.Round(Math.Atan2(v.Y, v.X) / (Math.PI / 4.0));
        sector = ((sector % 8) + 8) % 8;
        return sector switch
        {
            0 => EightDirection.E,
            1 => EightDirection.SE,
            2 => EightDirection.S,
            3 => EightDirection.SW,
            4 => EightDirection.W,
            5 => EightDirection.NW,
            6 => EightDirection.N,
            _ => EightDirection.NE,
        };
    }
}
