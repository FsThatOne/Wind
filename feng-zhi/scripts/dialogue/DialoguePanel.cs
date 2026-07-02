using FengZhi.Foundation.Dialogue;
using Godot;

namespace FengZhi.Dialogue;

public partial class DialoguePanel : PanelContainer
{
	private static readonly Color ColorStandard = new(0.95f, 0.93f, 0.88f);
	private static readonly Color ColorMindset = new(0.6f, 0.85f, 1.0f);
	private static readonly Color ColorCodePhrase = new(1.0f, 0.9f, 0.5f);
	private static readonly Color ColorFallback = new(0.6f, 0.6f, 0.6f);
	private static readonly Color ColorSelected = new(1f, 0.85f, 0.4f);
	private static readonly Color ColorMonologue = new(0.75f, 0.65f, 0.9f);

	private Label _speakerLabel = null!;
	private RichTextLabel _textLabel = null!;
	private Label _continueHint = null!;
	private VBoxContainer _choicesBox = null!;
	private Button[] _optionButtons = null!;
	private Label _insightHintLabel = null!;
	private PanelContainer _letterOverlay = null!;
	private Label _letterSenderLabel = null!;
	private RichTextLabel _letterTextLabel = null!;
	private Label _letterRecipientLabel = null!;

	public override void _Ready()
	{
		_speakerLabel = GetNode<Label>("MarginContainer/VBoxContainer/SpeakerLabel");
		_textLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/TextLabel");
		_continueHint = GetNode<Label>("MarginContainer/VBoxContainer/ContinueHint");
		_choicesBox = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ChoicesBox");
		_insightHintLabel = GetNode<Label>("MarginContainer/VBoxContainer/InsightHintLabel");
		_letterOverlay = GetNode<PanelContainer>("LetterOverlay");
		_letterSenderLabel = GetNode<Label>("LetterOverlay/LetterContent/LetterSenderLabel");
		_letterTextLabel = GetNode<RichTextLabel>("LetterOverlay/LetterContent/LetterTextLabel");
		_letterRecipientLabel = GetNode<Label>("LetterOverlay/LetterContent/LetterRecipientLabel");

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
			_letterOverlay.Visible = false;
			return;
		}

		Visible = true;

		if (snapshot.ShowLetterOverlay)
		{
			RenderLetterMode(snapshot);
			return;
		}

		_letterOverlay.Visible = false;
		RenderDialogueMode(snapshot);
	}

	private void RenderDialogueMode(DialogueUiSnapshot snapshot)
	{
		_speakerLabel.Text = snapshot.Mode switch
		{
			DialogueUiMode.InnerMonologue => "（内心）",
			DialogueUiMode.Narration => "",
			DialogueUiMode.Speech => snapshot.NameplateText ?? "",
			DialogueUiMode.Choice => snapshot.NameplateText ?? "",
			_ => ""
		};
		_speakerLabel.Visible = !string.IsNullOrEmpty(_speakerLabel.Text);

		if (snapshot.Mode == DialogueUiMode.InnerMonologue)
		{
			_textLabel.Text = $"[i][color=#{ColorMonologue.ToHtml(false)}]{EscapeBbCode(snapshot.VisibleText)}[/color][/i]";
		}
		else if (snapshot.Mode == DialogueUiMode.Narration)
		{
			_textLabel.Text = $"[center]{EscapeBbCode(snapshot.VisibleText)}[/center]";
		}
		else
		{
			_textLabel.Text = EscapeBbCode(snapshot.VisibleText);
		}

		_continueHint.Visible = snapshot.ShowContinueIndicator;
		_insightHintLabel.Visible = snapshot.HasInsightPrompt;

		_choicesBox.Visible = snapshot.ShowChoicePanel;
		if (snapshot.ShowChoicePanel)
			UpdateChoices(snapshot);
	}

	private void RenderLetterMode(DialogueUiSnapshot snapshot)
	{
		_letterOverlay.Visible = true;
		_letterSenderLabel.Text = !string.IsNullOrEmpty(snapshot.LetterSender)
			? $"寄：{snapshot.LetterSender}" : "";
		_letterRecipientLabel.Text = !string.IsNullOrEmpty(snapshot.LetterRecipient)
			? $"启：{snapshot.LetterRecipient}" : "";
		_letterTextLabel.Text = EscapeBbCode(snapshot.VisibleText);

		_speakerLabel.Visible = false;
		_textLabel.Text = "";
		_continueHint.Visible = snapshot.ShowContinueIndicator;
		_choicesBox.Visible = false;
		_insightHintLabel.Visible = false;
	}

	private void UpdateChoices(DialogueUiSnapshot snapshot)
	{
		for (int i = 0; i < _optionButtons.Length; i++)
		{
			if (i < snapshot.Options.Count)
			{
				_optionButtons[i].Visible = true;
				var option = snapshot.Options[i];

				var displayText = option.Style switch
				{
					DialogueUiOptionStyle.CodePhrase => $"「{option.Text}」",
					_ => option.Text
				};

				if (!string.IsNullOrEmpty(option.HintText))
					displayText = $"{option.HintText} {displayText}";

				_optionButtons[i].Text = displayText;

				var baseColor = option.Style switch
				{
					DialogueUiOptionStyle.Mindset => ColorMindset,
					DialogueUiOptionStyle.CodePhrase => ColorCodePhrase,
					DialogueUiOptionStyle.Fallback => ColorFallback,
					_ => ColorStandard
				};

				var finalColor = option.IsSelected ? ColorSelected : baseColor;
				_optionButtons[i].AddThemeColorOverride("font_color", finalColor);
			}
			else
			{
				_optionButtons[i].Visible = false;
			}
		}
	}

	public void ShowMonologue(string text)
	{
		Visible = true;
		_letterOverlay.Visible = false;
		_speakerLabel.Text = "（内心）";
		_speakerLabel.Visible = true;
		_textLabel.Text = $"[i][color=#{ColorMonologue.ToHtml(false)}]{EscapeBbCode(text)}[/color][/i]";
		_continueHint.Visible = false;
		_choicesBox.Visible = false;
		_insightHintLabel.Visible = false;
	}

	public void HideMonologue()
	{
		Visible = false;
	}

	private static string EscapeBbCode(string? text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		return text.Replace("[", "[lb]");
	}
}
