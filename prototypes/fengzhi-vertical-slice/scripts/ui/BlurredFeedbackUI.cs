// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;

namespace FengzhiSlice;

/// <summary>
/// 朦胧化 feedback UI. Shows literary text instead of numbers.
/// Displays after combat resolution + mindset choice.
/// Art Bible colors: 墨黑 background, 宣纸白 text.
/// </summary>
public partial class BlurredFeedbackUI : Control
{
    private Label? _feedbackText;
    private Button? _continueButton;
    private ColorRect? _background;
    
    public override void _Ready()
    {
        _feedbackText = GetNodeOrNull<Label>("%FeedbackText");
        _continueButton = GetNodeOrNull<Button>("%ContinueButton");
        _background = GetNodeOrNull<ColorRect>("%Background");
        
        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }
        
        EventBus.ShowBlurredFeedback += OnShowFeedback;
        Visible = false;
    }
    
    public override void _ExitTree()
    {
        EventBus.ShowBlurredFeedback -= OnShowFeedback;
    }
    
    private void OnShowFeedback(string text)
    {
        if (_feedbackText != null) _feedbackText.Text = text;
        Visible = true;
        
        // Fade in effect
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.4f);
    }
    
    private void OnContinuePressed()
    {
        // Fade out then signal completion
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.2f);
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            // Find GameManager and transition to Complete
            var gameManager = GetTree().Root.GetNodeOrNull<GameManager>("Main/GameManager");
            gameManager?.TransitionTo(GameManager.GamePhase.Complete);
        }));
    }
}
