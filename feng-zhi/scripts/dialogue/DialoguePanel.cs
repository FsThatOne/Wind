using FengZhi.Foundation.Dialogue;
using Godot;

namespace FengZhi.Dialogue;

public partial class DialoguePanel : PanelContainer
{
	private Label _speakerLabel = null!;
	private RichTextLabel _textLabel = null!;
	private Label _continueHint = null!;
	private VBoxContainer _choicesBox = null!;
	private Button[] _optionButtons = null!;

	public override void _Ready()
	{
		_speakerLabel = GetNode<Label>("MarginContainer/VBoxContainer/SpeakerLabel");
		_textLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/TextLabel");
		_continueHint = GetNode<Label>("MarginContainer/VBoxContainer/ContinueHint");
		_choicesBox = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ChoicesBox");
		_optionButtons = new Button[4];
		for (int i = 0; i < 4; i++)
		{
			_optionButtons[i] = _choicesBox.GetNode<Button>($"Option{i}");
		}
	}

	public void ApplySnapshot(DialogueUiSnapshot? snapshot)
	{
		if (snapshot == null || snapshot.Mode == DialogueUiMode.None)
		{
			Visible = false;
			return;
		}

		Visible = true;

		_speakerLabel.Text = snapshot.Mode switch
		{
			DialogueUiMode.InnerMonologue => "（内心）",
			DialogueUiMode.Narration => "",
			DialogueUiMode.Speech => snapshot.NameplateText ?? "",
			DialogueUiMode.Choice => snapshot.NameplateText ?? "",
			_ => ""
		};
		_speakerLabel.Visible = !string.IsNullOrEmpty(_speakerLabel.Text);

		_textLabel.Text = snapshot.VisibleText;

		_continueHint.Visible = snapshot.ShowContinueIndicator;

		_choicesBox.Visible = snapshot.ShowChoicePanel;
		if (snapshot.ShowChoicePanel)
		{
			UpdateChoices(snapshot);
		}
	}

	private void UpdateChoices(DialogueUiSnapshot snapshot)
	{
		for (int i = 0; i < _optionButtons.Length; i++)
		{
			if (i < snapshot.Options.Count)
			{
				_optionButtons[i].Visible = true;
				_optionButtons[i].Text = snapshot.Options[i].Text;

				// 选中态用 amber font_color 表达 (focus_mode=NONE, 不调 GrabFocus, 见 .tscn 注释).
				if (i == snapshot.SelectedOptionIndex)
				{
					_optionButtons[i].AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
				}
				else
				{
					_optionButtons[i].RemoveThemeColorOverride("font_color");
				}
			}
			else
			{
				_optionButtons[i].Visible = false;
			}
		}
	}

	/// <summary>以内心独白模式展示文本（洞察追查用）。</summary>
	public void ShowMonologue(string text)
	{
		Visible = true;
		_speakerLabel.Text = "（内心）";
		_speakerLabel.Visible = true;
		_textLabel.Text = text;
		_continueHint.Visible = false;
		_choicesBox.Visible = false;
	}

	/// <summary>关闭独白面板。</summary>
	public void HideMonologue()
	{
		Visible = false;
	}
}
