using Godot;

namespace FengZhi.Foundation.Animation.GodotIntegration;

/// <summary>
/// IDirectionalCharacterAnimator 的 AnimatedSprite2D 实现（ADR-0021 §扩展点 1）。
///
/// 上层只需每帧调 SetMovementVector(direction)；本 Adapter 内部负责：
///   1. 把 2D 向量分到 8 个 sector（atan2 + π/4 半角）
///   2. 边界迟滞（默认 ±10°）抑制扇区边缘抖动
///   3. 切换 walk_&lt;dir&gt; 时保留 Frame / FrameProgress 做相位连续
///   4. 零向量时 Pause（不切到 Idle、不清 CurrentDirection）
///
/// 动画名约定：walk_n / walk_ne / walk_e / walk_se / walk_s / walk_sw / walk_w / walk_nw，
/// 加上 idle 兜底。其余 CharacterAnimState（Attack/Hurt/...）走 ICharacterAnimator
/// 的小写映射，与 AnimatedSprite2DAnimator 保持一致；方向逻辑在这些状态下静默，
/// 直到下次非零 SetMovementVector 重新接管。
/// </summary>
public partial class EightDirectionAnimatedSprite2DAnimator : Node, IDirectionalCharacterAnimator
{
    private const float SectorRadians = Mathf.Pi / 4.0f;
    private const float HysteresisRadians = Mathf.Pi / 18.0f;

    [Export] public AnimatedSprite2D? Sprite { get; set; }

    private CharacterAnimState? _current;
    private string? _customAnimation;
    private EightDirection? _currentDirection;

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

    public EightDirection? CurrentDirection => _currentDirection;

    public event Action<CharacterAnimState>? Finished;
    public event Action<AnimationFrameEvent>? FrameEvent;

    public override void _Ready()
    {
        if (Sprite is null)
        {
            GD.PushError($"{nameof(EightDirectionAnimatedSprite2DAnimator)} on '{Name}' is missing the [Export] Sprite reference.");
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

        _current = state;
        _customAnimation = null;

        if (state == CharacterAnimState.Walk)
        {
            if (_currentDirection is { } dir)
            {
                PlayWalkAnimation(dir, loop);
            }
            return;
        }

        var animationName = state.ToString().ToLowerInvariant();
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
        _currentDirection = null;
    }

    public void SetFacing(Facing facing)
    {
        if (Sprite is null) return;
        Sprite.FlipH = (facing == Facing.Left);
    }

    /// <summary>
    /// 由上层每帧调用，驱动 walk_&lt;dir&gt; 自动切换。
    ///
    /// Contract：非零向量会强制把 <see cref="Current"/> 设为 <see cref="CharacterAnimState.Walk"/>，
    /// 即使上层正在播放 Attack/Hurt 等非 Walk 状态。需要保持非 Walk 动画播放完整时，
    /// 上层应在该期间停发非零向量（传 <see cref="Vector2.Zero"/> 或干脆不调）。
    /// </summary>
    public void SetMovementVector(Vector2 movement)
    {
        if (Sprite is null) return;

        if (movement == Vector2.Zero)
        {
            if (Sprite.IsPlaying())
                Sprite.Pause();
            return;
        }

        var nextDirection = ResolveDirection(movement, _currentDirection);
        _current = CharacterAnimState.Walk;
        _customAnimation = null;

        if (_currentDirection != nextDirection)
        {
            var frame = Sprite.Frame;
            var frameProgress = Sprite.FrameProgress;
            _currentDirection = nextDirection;
            PlayWalkAnimation(nextDirection, loop: true);
            PreserveFramePhase(frame, frameProgress);
            return;
        }

        if (!Sprite.IsPlaying())
        {
            PlayWalkAnimation(nextDirection, loop: true);
        }
    }

    private void PlayWalkAnimation(EightDirection direction, bool loop)
    {
        if (Sprite is null) return;
        var name = AnimationNameFor(direction);
        ApplyLoop(name, loop);
        Sprite.Play(name);
    }

    private void PreserveFramePhase(int frame, float frameProgress)
    {
        if (Sprite?.SpriteFrames is null) return;
        var frameCount = Sprite.SpriteFrames.GetFrameCount(Sprite.Animation);
        if (frameCount <= 0) return;
        Sprite.SetFrameAndProgress(Mathf.PosMod(frame, frameCount), frameProgress);
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

    private static string AnimationNameFor(EightDirection direction) => direction switch
    {
        EightDirection.N => "walk_n",
        EightDirection.NE => "walk_ne",
        EightDirection.E => "walk_e",
        EightDirection.SE => "walk_se",
        EightDirection.S => "walk_s",
        EightDirection.SW => "walk_sw",
        EightDirection.W => "walk_w",
        EightDirection.NW => "walk_nw",
        _ => "walk_s",
    };

    private static EightDirection ResolveDirection(Vector2 v, EightDirection? current)
    {
        var angle = Mathf.Atan2(v.Y, v.X);
        var sector = SectorFromAngle(angle);

        if (current is { } existing)
        {
            var existingSector = (int)existing;
            if (existingSector != sector)
            {
                var existingCenter = SectorCenter(existingSector);
                var distanceFromExisting = Mathf.Abs(NormalizeAngle(angle - existingCenter));
                if (distanceFromExisting <= SectorRadians * 0.5f + HysteresisRadians)
                {
                    sector = existingSector;
                }
            }
        }

        return (EightDirection)sector;
    }

    private static int SectorFromAngle(float angle)
    {
        var rounded = Mathf.RoundToInt(angle / SectorRadians);
        var sector = Mathf.PosMod(rounded, 8);
        return sector switch
        {
            0 => (int)EightDirection.E,
            1 => (int)EightDirection.SE,
            2 => (int)EightDirection.S,
            3 => (int)EightDirection.SW,
            4 => (int)EightDirection.W,
            5 => (int)EightDirection.NW,
            6 => (int)EightDirection.N,
            _ => (int)EightDirection.NE,
        };
    }

    private static float SectorCenter(int directionEnumValue)
    {
        var atanIndex = directionEnumValue switch
        {
            (int)EightDirection.E => 0,
            (int)EightDirection.SE => 1,
            (int)EightDirection.S => 2,
            (int)EightDirection.SW => 3,
            (int)EightDirection.W => 4,
            (int)EightDirection.NW => 5,
            (int)EightDirection.N => 6,
            (int)EightDirection.NE => 7,
            _ => 0,
        };
        return NormalizeAngle(atanIndex * SectorRadians);
    }

    private static float NormalizeAngle(float angle)
    {
        var twoPi = Mathf.Pi * 2.0f;
        angle = Mathf.PosMod(angle + Mathf.Pi, twoPi);
        return angle - Mathf.Pi;
    }
}
