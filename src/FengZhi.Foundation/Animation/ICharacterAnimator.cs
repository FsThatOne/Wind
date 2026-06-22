namespace FengZhi.Foundation.Animation;

/// <summary>
/// 项目级角色动画端口。所有 Presentation 层调用角色动画必须通过本接口；
/// 禁止直接 using AnimatedSprite2D / Skeleton2D / 第三方动画库类型。
/// 实机 Adapter 落到上层工程的 GodotIntegration/ 子目录，按场景需要注入。
/// </summary>
public interface ICharacterAnimator
{
    CharacterAnimState? Current { get; }
    bool IsPlaying { get; }
    float NormalizedProgress { get; }

    void Play(CharacterAnimState state, bool loop = true);
    void Stop();
    void SetFacing(Facing facing);

    event Action<CharacterAnimState>? Finished;
    event Action<AnimationFrameEvent>? FrameEvent;
}
