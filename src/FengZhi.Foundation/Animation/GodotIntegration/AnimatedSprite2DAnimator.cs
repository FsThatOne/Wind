using Godot;

namespace FengZhi.Foundation.Animation.GodotIntegration;

/// <summary>
/// ICharacterAnimator 的 AnimatedSprite2D 实现。挂在角色场景节点下，
/// [Export] 一个子 AnimatedSprite2D；CharacterAnimState 映射为小写动画名
/// (Idle → "idle"、Attack → "attack"，等)。Custom 状态请改用 PlayCustom。
/// FrameEvent 在 AnimatedSprite2D 下不可用（序列帧无原生方法轨道），订阅器永远不会触发。
/// 后续 Skeleton2D / DragonBones Adapter 会真正支持帧事件。
/// </summary>
public partial class AnimatedSprite2DAnimator : Node, ICharacterAnimator
{
    [Export] public AnimatedSprite2D? Sprite { get; set; }

    private CharacterAnimState? _current;
    private string? _customAnimation;

    public CharacterAnimState? Current => _current;

    public bool IsPlaying => Sprite?.IsPlaying() == true;

    public float NormalizedProgress
    {
        get
        {
            if (Sprite is null) return 0f;
            var frames = Sprite.SpriteFrames;
            var anim = Sprite.Animation;
            if (frames is null || anim == default) return 0f;
            var count = frames.GetFrameCount(anim);
            if (count <= 0) return 0f;
            return ((float)Sprite.Frame + Sprite.FrameProgress) / count;
        }
    }

    public event Action<CharacterAnimState>? Finished;
    public event Action<AnimationFrameEvent>? FrameEvent;

    public override void _Ready()
    {
        if (Sprite is null)
        {
            GD.PushError($"{nameof(AnimatedSprite2DAnimator)} on '{Name}' is missing the [Export] Sprite reference.");
            return;
        }
        Sprite.AnimationFinished += OnSpriteAnimationFinished;
    }

    public override void _ExitTree()
    {
        if (Sprite is not null)
            Sprite.AnimationFinished -= OnSpriteAnimationFinished;
    }

    public void Play(CharacterAnimState state, bool loop = true)
    {
        if (Sprite is null) return;
        if (state == CharacterAnimState.Custom)
        {
            GD.PushWarning("Use PlayCustom(name) instead of Play(Custom).");
            return;
        }
        var animationName = state.ToString().ToLowerInvariant();
        _current = state;
        _customAnimation = null;
        ApplyLoop(animationName, loop);
        Sprite.Play(animationName);
    }

    public void PlayCustom(string animationName, bool loop = true)
    {
        if (Sprite is null) return;
        _current = CharacterAnimState.Custom;
        _customAnimation = animationName;
        ApplyLoop(animationName, loop);
        Sprite.Play(animationName);
    }

    public void Stop()
    {
        if (Sprite is null) return;
        Sprite.Stop();
        _current = null;
        _customAnimation = null;
    }

    public void SetFacing(Facing facing)
    {
        if (Sprite is null) return;
        Sprite.FlipH = (facing == Facing.Left);
    }

    private void ApplyLoop(string animationName, bool loop)
    {
        var frames = Sprite?.SpriteFrames;
        if (frames is null) return;
        if (frames.HasAnimation(animationName))
        {
            var mode = loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None;
            frames.SetAnimationLoopMode(animationName, mode);
        }
    }

    private void OnSpriteAnimationFinished()
    {
        if (_current is { } state) Finished?.Invoke(state);
    }

    private void SuppressFrameEventUnused() => FrameEvent?.Invoke(default);
}
