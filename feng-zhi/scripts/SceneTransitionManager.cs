using Godot;

namespace FengZhi;

public partial class SceneTransitionManager : Node
{
	public static string? PendingEntryMarker { get; private set; }

	private ColorRect _fadeRect = null!;
	private bool _transitioning;

	public override void _Ready()
	{
		var layer = new CanvasLayer { Layer = 100, Name = "FadeLayer" };
		AddChild(layer);

		_fadeRect = new ColorRect
		{
			Color = new Color(0, 0, 0, 0),
			AnchorRight = 1,
			AnchorBottom = 1,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		layer.AddChild(_fadeRect);
	}

	public void TransitionTo(string scenePath, string? entryMarker)
	{
		if (_transitioning)
			return;
		_transitioning = true;
		PendingEntryMarker = entryMarker;

		var tween = CreateTween();
		tween.TweenProperty(_fadeRect, "color:a", 1.0f, 0.4f);
		tween.TweenCallback(Callable.From(() =>
		{
			GetTree().ChangeSceneToFile(scenePath);
			_transitioning = false;
		}));
	}

	public void FadeIn()
	{
		_fadeRect.Color = new Color(0, 0, 0, 1);
		var tween = CreateTween();
		tween.TweenProperty(_fadeRect, "color:a", 0.0f, 0.4f);
	}

	public static void ClearPendingEntry()
	{
		PendingEntryMarker = null;
	}
}
