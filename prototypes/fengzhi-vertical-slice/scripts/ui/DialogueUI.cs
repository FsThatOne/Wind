// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;

namespace FengzhiSlice;

/// <summary>
/// Dialogue display UI. Connects to DialogueManager signals.
/// Shows speaker name, text, advance via click anywhere + keyboard shortcuts.
/// Art Bible: 宣纸白 #F5F0E8 text on 墨黑 #1A1A2E panel.
/// </summary>
public partial class DialogueUI : Control
{
    private Label? _speakerLabel;
    private RichTextLabel? _textLabel;
    private VBoxContainer? _choiceContainer;
    private Panel? _clickArea;
    
    private DialogueManager? _dialogueManager;
    private bool _hasChoices = false;
    
    public override void _Ready()
    {
        _speakerLabel = GetNodeOrNull<Label>("%SpeakerLabel");
        _textLabel = GetNodeOrNull<RichTextLabel>("%TextLabel");
        _choiceContainer = GetNodeOrNull<VBoxContainer>("%ChoiceContainer");
        _clickArea = GetNodeOrNull<Panel>("%ClickArea");
        
        if (_clickArea != null)
        {
            _clickArea.MouseFilter = MouseFilterEnum.Stop;
            _clickArea.GuiInput += OnClickAreaPressed;
        }
        
        Visible = false;
        
        _dialogueManager = GetTree().Root.GetNodeOrNull<DialogueManager>("Main/DialogueManager");
        if (_dialogueManager != null)
        {
            _dialogueManager.DialogueLineShown += OnDialogueLineShown;
            _dialogueManager.ChoicesPresented += OnChoicesPresented;
            _dialogueManager.DialogueComplete += OnDialogueComplete;
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter)
            {
                if (!_hasChoices)
                {
                    _dialogueManager?.AdvanceDialogue();
                }
            }
        }
    }
    
    private void OnClickAreaPressed(InputEvent @event)
    {
        if (!Visible || _hasChoices) return;
        
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _dialogueManager?.AdvanceDialogue();
        }
    }
    
    private void OnDialogueLineShown(string speaker, string text)
    {
        Visible = true;
        _hasChoices = false;
        
        if (_speakerLabel != null) _speakerLabel.Text = speaker;
        if (_textLabel != null) _textLabel.Text = text;
        if (_choiceContainer != null) _choiceContainer.Visible = false;
        if (_clickArea != null) _clickArea.Visible = true;
    }
    
    private void OnChoicesPresented(string[] choices)
    {
        _hasChoices = true;
        
        if (_clickArea != null) _clickArea.Visible = false;
        if (_choiceContainer == null) return;
        
        foreach (var child in _choiceContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        _choiceContainer.Visible = true;
        
        for (int i = 0; i < choices.Length; i++)
        {
            var btn = new Button();
            btn.Text = $"{i + 1}. {choices[i]}";
            btn.CustomMinimumSize = new Vector2(400, 48);
            int choiceIndex = i;
            btn.Pressed += () => OnChoiceSelected(choiceIndex);
            _choiceContainer.AddChild(btn);
        }
    }
    
    private void OnChoiceSelected(int index)
    {
        _dialogueManager?.SelectChoice(index);
    }
    
    private void OnDialogueComplete()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.2f);
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            Modulate = new Color(1, 1, 1, 1);
        }));
    }
}
