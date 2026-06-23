using Godot;

namespace FengZhi.Foundation.Animation.Fakes;

/// <summary>
/// 测试用 Fake — 纯内存状态，不依赖 Godot 节点（ADR-0022 §2，
/// 取代 ADR-0021 §扩展点 1 的 8 向 Fake 路径）。
///
/// 同时实现 <see cref="ICharacterAnimator"/> 与 <see cref="IIso4CharacterAnimator"/>，
/// 让 4 斜向消费方也可以注入同一替身。Sector 选区与
/// <see cref="GodotIntegration.Iso4AnimatedSprite2DAnimator"/> 保持一致（无迟滞版本，
/// 测试通过 <see cref="LastMovementVector"/> 间接验证）。
/// </summary>
public sealed class FakeIso4CharacterAnimator : IIso4CharacterAnimator
{
    public CharacterAnimState? Current { get; private set; }
    public bool IsPlaying { get; private set; }
    public float NormalizedProgress { get; private set; }
    public Facing Facing { get; private set; } = Facing.Right;
    public bool LoopRequested { get; private set; }
    public int PlayCallCount { get; private set; }
    public Iso4Direction? CurrentDirection { get; private set; }
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

    private static Iso4Direction ResolveDirection(Vector2 v)
    {
        var angle = Mathf.Atan2(v.Y, v.X);
        if (angle < -Mathf.Pi / 2.0f) return Iso4Direction.NW;
        if (angle < 0f) return Iso4Direction.NE;
        if (angle < Mathf.Pi / 2.0f) return Iso4Direction.SE;
        return Iso4Direction.SW;
    }
}
