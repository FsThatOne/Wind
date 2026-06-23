namespace FengZhi.Foundation.Animation.Fakes;

/// <summary>
/// 测试用 Fake — 纯内存状态，不依赖 Godot 节点。
/// 实现 <see cref="ICharacterAnimator"/> 主端口；
/// 4 斜向场景请使用 <see cref="FakeIso4CharacterAnimator"/>（ADR-0022 §2）。
/// </summary>
public sealed class FakeCharacterAnimator : ICharacterAnimator
{
    public CharacterAnimState? Current { get; private set; }
    public bool IsPlaying { get; private set; }
    public float NormalizedProgress { get; private set; }
    public Facing Facing { get; private set; } = Facing.Right;
    public bool LoopRequested { get; private set; }
    public int PlayCallCount { get; private set; }

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
}
