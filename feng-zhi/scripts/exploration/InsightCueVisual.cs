using Godot;

namespace FengZhi.Scripts.Exploration;

/// <summary>
/// 水墨风格洞察提示视觉节点。
/// 由 InsightDetectorBridge 在检测到 InsightCueShownEvent 时实例化，
/// 使用 Tween 驱动淡入/淡出 + 脉冲动画。
/// </summary>
public partial class InsightCueVisual : Node2D
{
	private const float FadeInDuration = 0.6f;
	private const float FadeOutDuration = 0.4f;
	private const float PulseMinScale = 0.85f;
	private const float PulseMaxScale = 1.15f;
	private const float PulseDuration = 1.8f;

	private Sprite2D _sprite = null!;
	private Tween? _fadeTween;
	private Tween? _pulseTween;

	public string NodeId { get; set; } = "";

	public override void _Ready()
	{
		const string texturePath = "res://assets/vfx/insight_cue_circle.png";
		Texture2D? texture = null;
		if (ResourceLoader.Exists(texturePath))
			texture = GD.Load<Texture2D>(texturePath);

		if (texture == null)
		{
			var img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
			img.Fill(new Color(0.3f, 0.6f, 0.9f, 0.4f));
			texture = ImageTexture.CreateFromImage(img);
		}

		_sprite = new Sprite2D
		{
			Name = "CueSprite",
			Texture = texture,
			Modulate = new Color(0.3f, 0.6f, 0.9f, 0f),
			Scale = Vector2.One * 0.5f,
		};

		AddChild(_sprite);
	}

	public void FadeIn()
	{
		_fadeTween?.Kill();
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_sprite, "modulate:a", 0.6f, FadeInDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_fadeTween.TweenCallback(Callable.From(StartPulse));
	}

	public void FadeOut(System.Action? onComplete = null)
	{
		_fadeTween?.Kill();
		_fadeTween = null;
		_pulseTween?.Kill();
		_pulseTween = null;

		var tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate:a", 0f, FadeOutDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() =>
		{
			onComplete?.Invoke();
			QueueFree();
		}));
	}

	private void StartPulse()
	{
		_pulseTween = CreateTween();
		_pulseTween.SetLoops();
		_pulseTween.TweenProperty(_sprite, "scale", Vector2.One * PulseMaxScale * 0.5f, PulseDuration * 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_pulseTween.TweenProperty(_sprite, "scale", Vector2.One * PulseMinScale * 0.5f, PulseDuration * 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}
}
