using FengZhi;
using FengZhi.Dialogue;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Godot;

namespace FengZhi.Test;

public partial class DialogueSmokeTestScene : Control
{
	private DialogueManager _dialogueManager = null!;
	private DialoguePanel _dialoguePanel = null!;
	private Label _statusLabel = null!;

	public override void _Ready()
	{
		var panelScene = GD.Load<PackedScene>("res://scenes/ui/DialoguePanel.tscn");
		_dialoguePanel = panelScene.Instantiate<DialoguePanel>();
		var dialogueLayer = new CanvasLayer { Layer = 40, Name = "DialogueLayer" };
		AddChild(dialogueLayer);
		dialogueLayer.AddChild(_dialoguePanel);

		_dialogueManager = new DialogueManager();
		AddChild(_dialogueManager);

		var flow = GetNodeOrNull<GameFlow>("/root/GameFlow");
		IEventBus eventBus;
		MindsetService mindsetService;
		if (flow != null)
		{
			eventBus = flow.EventBus;
			mindsetService = flow.MindsetService;
		}
		else
		{
			eventBus = new EventBus();
			mindsetService = new MindsetService(eventBus: eventBus);
		}

		var conditionProvider = new SceneConditionValueProvider(mindsetService);
		_dialogueManager.Initialize(eventBus, mindsetService, _dialoguePanel, conditionProvider);
		_dialogueManager.DialogueEnded += OnDialogueEnded;

		_statusLabel = new Label
		{
			Text = "对话系统 Smoke Test — 自动启动中…",
			Position = new Vector2(20, 10),
		};
		_statusLabel.AddThemeColorOverride("font_color", Colors.White);
		AddChild(_statusLabel);

		var bg = new ColorRect
		{
			Color = new Color(0.08f, 0.08f, 0.12f, 1f),
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		bg.ZIndex = -1;
		AddChild(bg);

		GD.Print("[SmokeTest] Starting dialogue_smoke_test.yaml ...");
		_dialogueManager.StartDialogue("res://assets/data/dialogues/test/dialogue_smoke_test.yaml");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_dialogueManager.IsDialogueActive) return;

		if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
		{
			_dialogueManager.HandleConfirm();
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			_dialogueManager.HandleMoveSelection(-1);
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			_dialogueManager.HandleMoveSelection(1);
		}
	}

	private void OnDialogueEnded()
	{
		_statusLabel.Text = "对话结束。查看控制台输出确认 mindset shift + quest_flag 事件。按 Esc 退出。";
		GD.Print("[SmokeTest] Dialogue ended successfully!");
	}
}
