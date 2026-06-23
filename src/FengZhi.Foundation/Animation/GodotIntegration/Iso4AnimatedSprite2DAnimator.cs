using Godot;

namespace FengZhi.Foundation.Animation.GodotIntegration;

/// <summary>
/// <see cref="IIso4CharacterAnimator"/> 的 AnimatedSprite2D 实现（ADR-0022 §2，
/// 取代 ADR-0021 §扩展点 1 的 <c>EightDirectionAnimatedSprite2DAnimator</c>）。
///
/// 上层只需每帧调 <see cref="SetMovementVector"/>（cart 空间向量）；本 Adapter 内部承担：
/// <list type="number">
///   <item><description>把 cart 向量分到 4 个 sector：NW / NE / SE / SW（sector 中心对齐 ADR-0022 §3 WASD cart 对角向量）</description></item>
///   <item><description>边界迟滞（±15° per ADR-0022 §2）抑制扇区边缘抖动</description></item>
///   <item><description>切换 walk_&lt;dir&gt; 时保留 Frame / FrameProgress 做相位连续</description></item>
///   <item><description>零向量时 Pause（不切到 Idle、不清 CurrentDirection）</description></item>
/// </list>
///
/// 动画名约定：walk_ne / walk_se / walk_sw / walk_nw，加 idle 兜底。
/// 其余 <see cref="CharacterAnimState"/>（Attack/Hurt/...）走 <see cref="ICharacterAnimator"/>
/// 的小写映射，与 <see cref="AnimatedSprite2DAnimator"/> 保持一致；方向逻辑在这些状态下静默，
/// 直到下次非零 <see cref="SetMovementVector"/> 重新接管。
/// </summary>
public partial class Iso4AnimatedSprite2DAnimator : Node, IIso4CharacterAnimator
{
    private const float SectorRadians = Mathf.Pi / 2.0f;
    private const float HysteresisRadians = Mathf.Pi / 12.0f;

    [Export] public AnimatedSprite2D? Sprite { get; set; }

    private CharacterAnimState? _current;
    private string? _customAnimation;
    private Iso4Direction? _currentDirection;

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

    public Iso4Direction? CurrentDirection => _currentDirection;

    public event Action<CharacterAnimState>? Finished;
    public event Action<AnimationFrameEvent>? FrameEvent;

    public override void _Ready()
    {
        if (Sprite is null)
        {
            GD.PushError($"{nameof(Iso4AnimatedSprite2DAnimator)} on '{Name}' is missing the [Export] Sprite reference.");
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
    ///
    /// 入参为 cart 空间向量；屏幕投影由 <see cref="Geometry.IsoProjection"/> 在调用方处理。
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

    private void PlayWalkAnimation(Iso4Direction direction, bool loop)
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

    private static string AnimationNameFor(Iso4Direction direction) => direction switch
    {
        Iso4Direction.NE => "walk_ne",
        Iso4Direction.SE => "walk_se",
        Iso4Direction.SW => "walk_sw",
        Iso4Direction.NW => "walk_nw",
        _ => "walk_se",
    };

    /// <summary>
    /// cart 空间向量 → <see cref="Iso4Direction"/>。
    ///
    /// Sector 中心对齐 ADR-0022 §3 WASD cart 对角向量：
    /// <list type="bullet">
    ///   <item><description>NW 中心 cart_angle = -3π/4 (W key cart -1,-1)</description></item>
    ///   <item><description>NE 中心 cart_angle = -π/4 (D key cart +1,-1)</description></item>
    ///   <item><description>SE 中心 cart_angle = +π/4 (S key cart +1,+1)</description></item>
    ///   <item><description>SW 中心 cart_angle = +3π/4 (A key cart -1,+1)</description></item>
    /// </list>
    ///
    /// <para>
    /// 注：与 ADR-0022 §2 表中"sector 边界在 cart 对角 (±π/4, ±3π/4)"略有出入；本实现按
    /// §3 WASD 期望（W=NW、D=NE、S=SE、A=SW）选择把 sector 中心对齐 cart 对角，使 WASD
    /// 落入 sector 内部而非边界。ADR-0022 erratum 已记入该 ADR 文末 Validation Criteria 后。
    /// </para>
    /// </summary>
    private static Iso4Direction ResolveDirection(Vector2 v, Iso4Direction? current)
    {
        var angle = Mathf.Atan2(v.Y, v.X);
        var sector = SectorFromAngle(angle);

        if (current is { } existing && existing != sector)
        {
            var existingCenter = SectorCenter(existing);
            var distanceFromExisting = Mathf.Abs(NormalizeAngle(angle - existingCenter));
            if (distanceFromExisting <= SectorRadians * 0.5f + HysteresisRadians)
            {
                sector = existing;
            }
        }

        return sector;
    }

    private static Iso4Direction SectorFromAngle(float angle)
    {
        if (angle < -Mathf.Pi / 2.0f) return Iso4Direction.NW;
        if (angle < 0f) return Iso4Direction.NE;
        if (angle < Mathf.Pi / 2.0f) return Iso4Direction.SE;
        return Iso4Direction.SW;
    }

    private static float SectorCenter(Iso4Direction direction) => direction switch
    {
        Iso4Direction.NE => -Mathf.Pi / 4.0f,
        Iso4Direction.SE => +Mathf.Pi / 4.0f,
        Iso4Direction.SW => +3.0f * Mathf.Pi / 4.0f,
        Iso4Direction.NW => -3.0f * Mathf.Pi / 4.0f,
        _ => 0f,
    };

    private static float NormalizeAngle(float angle)
    {
        var twoPi = Mathf.Pi * 2.0f;
        angle = Mathf.PosMod(angle + Mathf.Pi, twoPi);
        return angle - Mathf.Pi;
    }
}
