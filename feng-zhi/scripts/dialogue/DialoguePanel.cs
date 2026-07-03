using System.Collections.Generic;
using FengZhi.Foundation.Dialogue;
using Godot;

namespace FengZhi.Dialogue;

public partial class DialoguePanel : Control
{
	private static readonly Dictionary<string, string> SpeakerNames = new()
	{
		["player"] = "停云",
		["bai_tan"] = "白檀",
		["senior_brother"] = "师兄",
		["manor_master"] = "庄主",
		["junior_brother"] = "师弟",
		["kitchen_disciple"] = "厨房弟子",
		["pharmacy_disciple"] = "药房弟子",
	};

	private static readonly Color ColorStandard = new(0.95f, 0.93f, 0.88f);
	private static readonly Color ColorMindset = new(0.6f, 0.85f, 1.0f);
	private static readonly Color ColorCodePhrase = new(1.0f, 0.9f, 0.5f);
	private static readonly Color ColorFallback = new(0.6f, 0.6f, 0.6f);
	private static readonly Color ColorSelected = new(1f, 0.85f, 0.4f);
	private static readonly Color ColorMonologue = new(0.75f, 0.65f, 0.9f);

	private VBoxContainer _portraitColumn = null!;
	private TextureRect _portraitTexture = null!;
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

	private string? _currentPortraitSpeakerId;

	public override void _Ready()
	{
		_portraitColumn = GetNode<VBoxContainer>("PortraitColumn");
		_portraitTexture = GetNode<TextureRect>("PortraitColumn/PortraitTexture");
		_speakerLabel = GetNode<Label>("PortraitColumn/SpeakerLabel");
		_textLabel = GetNode<RichTextLabel>("TextPanel/MarginContainer/VBoxContainer/TextLabel");
		_continueHint = GetNode<Label>("TextPanel/MarginContainer/VBoxContainer/ContinueHint");
		_choicesBox = GetNode<VBoxContainer>("TextPanel/MarginContainer/VBoxContainer/ChoicesBox");
		_insightHintLabel = GetNode<Label>("TextPanel/MarginContainer/VBoxContainer/InsightHintLabel");
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
		UpdatePortrait(snapshot);

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
		_portraitColumn.Visible = false;
		_letterOverlay.Visible = true;
		_letterSenderLabel.Text = !string.IsNullOrEmpty(snapshot.LetterSender)
			? $"寄：{snapshot.LetterSender}" : "";
		_letterRecipientLabel.Text = !string.IsNullOrEmpty(snapshot.LetterRecipient)
			? $"启：{snapshot.LetterRecipient}" : "";
		_letterTextLabel.Text = EscapeBbCode(snapshot.VisibleText);

		_textLabel.Text = "";
		_continueHint.Visible = snapshot.ShowContinueIndicator;
		_choicesBox.Visible = false;
		_insightHintLabel.Visible = false;
	}

	private void UpdatePortrait(DialogueUiSnapshot snapshot)
	{
		if (!snapshot.ShowPortrait || string.IsNullOrEmpty(snapshot.SpeakerId))
		{
			_portraitColumn.Visible = false;
			_speakerLabel.Text = "";
			return;
		}

		var speakerId = snapshot.SpeakerId;
		_speakerLabel.Text = ResolveSpeakerName(snapshot.NameplateText);

		if (speakerId == _currentPortraitSpeakerId && _portraitTexture.Texture != null)
		{
			_portraitColumn.Visible = true;
			return;
		}

		var texture = LoadPortraitTexture(speakerId);
		if (texture != null)
		{
			_portraitTexture.Texture = texture;
			_portraitColumn.Visible = true;
			_currentPortraitSpeakerId = speakerId;
		}
		else
		{
			_portraitColumn.Visible = false;
			_currentPortraitSpeakerId = null;
		}
	}

	private static Texture2D? LoadPortraitTexture(string speakerId)
	{
		var path = $"res://assets/character/portraits/{speakerId}_full_neutral.png";
		if (!ResourceLoader.Exists(path))
			return null;
		return GD.Load<Texture2D>(path);
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
		_portraitColumn.Visible = false;
		_letterOverlay.Visible = false;
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

	private static string ResolveSpeakerName(string? speakerId)
	{
		if (string.IsNullOrEmpty(speakerId)) return "";
		return SpeakerNames.TryGetValue(speakerId, out var name) ? name : speakerId;
	}
}
