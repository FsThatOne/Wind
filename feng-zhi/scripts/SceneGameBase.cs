using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Geometry;
using FengZhi.Foundation.Mindset;
using FengZhi.Scripts.Audio;
using FengZhi.Scripts.Exploration;
using FengZhi.Ui;
using Godot;

namespace FengZhi;

public abstract partial class SceneGameBase : Node2D
{
	// 全局 iso 网格尺寸 128x64：与丹房地块、ADR-0022 和 IsoProjection 常量保持一致。
	// 新地图场景不得改用其它尺寸，LoadVariant 会在读取 TMX 时做护栏校验。
	protected virtual int TileWidth => 128;
	protected virtual int TileHeight => 64;

	protected abstract string AssetRoot { get; }
	protected abstract string DayMapPath { get; }
	protected abstract string NightMapPath { get; }
	protected abstract string SceneName { get; }
	protected abstract string DefaultExitMarker { get; }

	private static readonly Vector2 InteractionDiamondOffset = new(22f, 5f);
	private static readonly Color InteractionDefaultFill = new(0.08f, 0.72f, 0.24f, 0.28f);
	private static readonly Color InteractionHighlightFill = new(0.10f, 0.95f, 0.32f, 0.58f);
	private static readonly Color InteractionHighlightOutline = new(0.20f, 1.0f, 0.44f, 1.0f);
	private static readonly Color BlockedTileOverlayFill = new(0.95f, 0.08f, 0.06f, 0.44f);
	private static readonly Color BlockedTileOverlayOutline = new(1.0f, 0.18f, 0.12f, 0.95f);

	private readonly Dictionary<string, Marker> _markers = new();
	private readonly Dictionary<string, Area2D> _interactionAreas = new();
	private readonly HashSet<Vector2I> _groundTiles = new();
	private readonly HashSet<Vector2I> _blockedTiles = new();
	private int _mapWidth = 16;
	private int _mapHeight = 16;
	private bool _autoExitEnabled;
	private Vector2I _lastPlayerTile = new(-999, -999);

	private Node2D _mapRoot = null!;
	private Node2D _tileLayersRoot = null!;
	private Node2D _dayTileLayers = null!;
	private Node2D _nightTileLayers = null!;
	private Node2D _structures = null!;
	private Node2D _collision = null!;
	private Node2D _logicMarkers = null!;
	protected PlayerCharacterController Player = null!;
	private ColorRect _backgroundTint = null!;
	protected Label StatusLabel = null!;
	protected Label PromptLabel = null!;
	protected Label InventoryLabel = null!;
	private Label _hintLabel = null!;
	private Panel _messagePanel = null!;
	private Label _messageLabel = null!;
	private Panel _objectivePanel = null!;
	private Label _objectiveChapterLabel = null!;
	private Label[] _objectiveItemLabels = Array.Empty<Label>();
	private Area2D? _focusedArea;
	protected Dialogue.DialogueManager? DialogueManager;
	private Dialogue.DialoguePanel? _dialoguePanel;
	private AttributePanel? _attributePanel;
	protected Dialogue.SceneConditionValueProvider? ConditionProvider;
	private GameFlow? _flow;
	private Action? _unsubscribeQuestFlag;
	private Action? _unsubscribeMisunderstandingRegistered;
	protected string Variant = "day";

	protected virtual Vector2 Origin { get; set; } = new(576f, 96f);

	/// <summary>
	/// 场景 BGM 资源 ID（不含路径和扩展名）。留空表示不切换 BGM；
	/// 填入 "SILENCE" 表示刻意留白（无 BGM，仅有环境音）。
	/// 在 Godot 编辑器 Inspector 中设置。
	/// </summary>
	[Export]
	public string SceneBgmId { get; set; } = "";

	/// <summary>
	/// 地形基底环境音 ID（如 "ambient_bamboo_wind"、"ambient_rain"）。
	/// 留空表示不切换地形环境音。在 Inspector 中设置。
	/// </summary>
	[Export]
	public string TerrainAmbientId { get; set; } = "";

	public override void _Ready()
	{
		GD.Print($"[{SceneName}] _Ready: begin node binding...");
		_mapRoot = GetNode<Node2D>("MapRoot");
		_tileLayersRoot = GetNode<Node2D>("Tiles/TileLayers");
		_dayTileLayers = GetNode<Node2D>("Tiles/TileLayers/Day");
		_nightTileLayers = GetNode<Node2D>("Tiles/TileLayers/Night");
		_structures = GetNode<Node2D>("MapRoot/Structures");
		_collision = GetNode<Node2D>("MapRoot/Collision");
		_logicMarkers = GetNode<Node2D>("MapRoot/LogicMarkers");
		Player = GetNode<PlayerCharacterController>("MapRoot/Player");
		_backgroundTint = GetNode<ColorRect>("BackgroundTint");
		StatusLabel = GetNode<Label>("UiLayer/StatusLabel");
		PromptLabel = GetNode<Label>("UiLayer/PromptLabel");
		InventoryLabel = GetNode<Label>("UiLayer/InventoryLabel");
		_hintLabel = GetNode<Label>("UiLayer/HintLabel");
		_messagePanel = GetNode<Panel>("UiLayer/MessagePanel");
		_messageLabel = GetNode<Label>("UiLayer/MessagePanel/MessageLabel");
                CreateObjectiveHud(GetNode<CanvasLayer>("UiLayer"));
		GD.Print($"[{SceneName}] _Ready: all nodes bound.");

		ConfigureTileLayerAnchor();
		ConfigureResponsiveHud();
		_messagePanel.Visible = false;

		GD.Print($"[{SceneName}] _Ready: loading DialoguePanel.tscn...");
		var panelScene = GD.Load<PackedScene>("res://scenes/ui/DialoguePanel.tscn");
		_dialoguePanel = panelScene.Instantiate<Dialogue.DialoguePanel>();
		var dialogueLayer = new CanvasLayer { Layer = 40, Name = "DialogueLayer" };
		AddChild(dialogueLayer);
		dialogueLayer.AddChild(_dialoguePanel);

		var attrScene = GD.Load<PackedScene>("res://scenes/ui/AttributePanel.tscn");
		_attributePanel = attrScene.Instantiate<AttributePanel>();
		var attrLayer = new CanvasLayer { Layer = 30, Name = "AttributeLayer" };
		AddChild(attrLayer);
		attrLayer.AddChild(_attributePanel);

		DialogueManager = new Dialogue.DialogueManager();
		AddChild(DialogueManager);

		IEventBus eventBus;
		MindsetService mindsetService;
		_flow = GetNodeOrNull<GameFlow>("/root/GameFlow");
		if (_flow != null)
		{
			GD.Print($"[{SceneName}] _Ready: GameFlow autoload found, using shared EventBus/MindsetService.");
			eventBus = _flow.EventBus;
			mindsetService = _flow.MindsetService;
		}
		else
		{
			GD.Print($"[{SceneName}] _Ready: GameFlow not found, creating standalone EventBus/MindsetService.");
			eventBus = new EventBus();
			mindsetService = new MindsetService(eventBus: eventBus);
		}

		ConditionProvider = new Dialogue.SceneConditionValueProvider(mindsetService);
		ConditionProvider.SetFlag("variant", Variant);
		ImportQuestFlagsFromGameFlow();
		ImportMisunderstandingModsFromGameFlow();
		_unsubscribeQuestFlag = eventBus.Subscribe<DialogueQuestFlagEvent>(OnDialogueQuestFlag);
		_unsubscribeMisunderstandingRegistered = eventBus.Subscribe<DialogueRegisterMisunderstandingEvent>(OnDialogueRegisterMisunderstanding);
		DialogueManager.Initialize(eventBus, mindsetService, _dialoguePanel, ConditionProvider);
		DialogueManager.DialogueEnded += OnDialogueEnded;
		GD.Print($"[{SceneName}] _Ready: dialogue system initialized.");

		var insightBridge = new InsightDetectorBridge { Name = "InsightDetectorBridge" };
		AddChild(insightBridge);

		var monologuePresenter = new InsightMonologuePresenter { Name = "InsightMonologuePresenter" };
		AddChild(monologuePresenter);

		LoadVariant(GetInitialVariant(), repositionPlayer: true);
		OnReady();

		if (!string.IsNullOrEmpty(SceneBgmId) || !string.IsNullOrEmpty(TerrainAmbientId))
		{
			var audioDir = GetNodeOrNull<AudioDirector>("/root/AudioDirector");
			audioDir?.ApplySceneAudio(SceneBgmId, TerrainAmbientId);
		}

		var transition = GetNodeOrNull<SceneTransitionManager>("/root/SceneTransition");
		if (transition != null)
			transition.FadeIn();

		_autoExitEnabled = false;
		GetTree().CreateTimer(0.5).Timeout += () => _autoExitEnabled = true;

		GD.Print($"[{SceneName}] _Ready: complete.");
	}

	public override void _ExitTree()
	{
		_unsubscribeQuestFlag?.Invoke();
		_unsubscribeQuestFlag = null;
		_unsubscribeMisunderstandingRegistered?.Invoke();
		_unsubscribeMisunderstandingRegistered = null;

		if (DialogueManager != null)
			DialogueManager.DialogueEnded -= OnDialogueEnded;

		base._ExitTree();
	}

	protected virtual void OnReady() { }
	protected virtual string GetInitialVariant() => "day";

	private void ImportQuestFlagsFromGameFlow()
	{
		if (_flow == null || ConditionProvider == null)
			return;

		foreach (var (key, value) in _flow.QuestFlags)
			ConditionProvider.SetFlag(key, value);
	}

	private void ImportMisunderstandingModsFromGameFlow()
	{
		if (_flow == null || ConditionProvider == null)
			return;

		foreach (var (npcId, mod) in _flow.MisunderstandingMods)
			ConditionProvider.SetMisunderstandingMod(npcId, mod);
	}

	private void OnDialogueQuestFlag(DialogueQuestFlagEvent gameEvent)
	{
		SetQuestFlag(gameEvent.Key, gameEvent.Value);
		ImportMisunderstandingModsFromGameFlow();
	}

	private void OnDialogueRegisterMisunderstanding(DialogueRegisterMisunderstandingEvent gameEvent)
	{
		if (string.IsNullOrWhiteSpace(gameEvent.Key))
			return;

		ConditionProvider?.SetFlag(gameEvent.Key, string.IsNullOrWhiteSpace(gameEvent.Value) ? "active" : gameEvent.Value);
		ImportMisunderstandingModsFromGameFlow();
	}

	protected void SetQuestFlag(string key, string? value = "true")
	{
		if (string.IsNullOrWhiteSpace(key))
			return;

		var normalized = string.IsNullOrWhiteSpace(value) ? "true" : value;
		ConditionProvider?.SetFlag(key, normalized);
		_flow?.RecordQuestFlag(key, normalized);
		OnQuestFlagChanged(key, normalized);
	}

	protected bool HasQuestFlag(string key, string expectedValue = "true")
		=> _flow?.HasQuestFlag(key, expectedValue) == true ||
			ConditionProviderHasFlag(key, expectedValue);

	protected string? GetQuestFlag(string key)
		=> _flow?.GetQuestFlag(key);

	protected virtual void OnQuestFlagChanged(string key, string value) { }

	private bool ConditionProviderHasFlag(string key, string expectedValue)
	{
		if (ConditionProvider == null)
			return false;

		var condition = new DialogueConditionSpec
		{
			Source = "flag",
			Key = key,
			Op = "==",
			Value = expectedValue,
		};
		return ConditionProvider.TryGetValue(condition, out var value, out _) &&
			string.Equals(value, expectedValue, StringComparison.Ordinal);
	}

	private void CreateObjectiveHud(CanvasLayer uiLayer)
	{
		_objectivePanel = new Panel
		{
			Name = "ObjectivePanel",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		uiLayer.AddChild(_objectivePanel);

		var box = new VBoxContainer
		{
			Name = "ObjectiveContent",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_objectivePanel.AddChild(box);

		_objectiveChapterLabel = new Label
		{
			Name = "ChapterLabel",
			Text = "任务追踪",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		var itemLabels = new List<Label>();
		for (var i = 0; i < 3; i++)
		{
			var label = new Label
			{
				Name = $"ObjectiveItem{i + 1}",
				Text = "",
				Visible = false,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			itemLabels.Add(label);
		}

		_objectiveItemLabels = itemLabels.ToArray();

		box.AddChild(_objectiveChapterLabel);
		foreach (var label in _objectiveItemLabels)
			box.AddChild(label);

		_objectiveChapterLabel.AddThemeFontSizeOverride("font_size", 13);
		foreach (var label in _objectiveItemLabels)
			label.AddThemeFontSizeOverride("font_size", 14);

		_objectiveChapterLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.84f, 0.84f, 0.86f));
		foreach (var label in _objectiveItemLabels)
			label.AddThemeColorOverride("font_color", new Color(0.96f, 0.94f, 0.86f, 1f));
	}

	private void ConfigureResponsiveHud()
	{
		ConfigureTopLeftLabel(StatusLabel, top: 16f, height: 36f);
		StatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		ConfigureTopLeftLabel(InventoryLabel, top: 52f, height: 32f);
		InventoryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		PromptLabel.AnchorLeft = 0.5f;
		PromptLabel.AnchorRight = 0.5f;
		PromptLabel.AnchorTop = 1.0f;
		PromptLabel.AnchorBottom = 1.0f;
		PromptLabel.OffsetLeft = -300f;
		PromptLabel.OffsetRight = 300f;
		PromptLabel.OffsetTop = -92f;
		PromptLabel.OffsetBottom = -52f;
		PromptLabel.HorizontalAlignment = HorizontalAlignment.Center;

		_hintLabel.AnchorLeft = 0.0f;
		_hintLabel.AnchorRight = 1.0f;
		_hintLabel.AnchorTop = 1.0f;
		_hintLabel.AnchorBottom = 1.0f;
		_hintLabel.OffsetLeft = 20f;
		_hintLabel.OffsetRight = -20f;
		_hintLabel.OffsetTop = -44f;
		_hintLabel.OffsetBottom = -12f;
		_hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		_messagePanel.AnchorLeft = 0.5f;
		_messagePanel.AnchorRight = 0.5f;
		_messagePanel.AnchorTop = 1.0f;
		_messagePanel.AnchorBottom = 1.0f;
		_messagePanel.OffsetLeft = -420f;
		_messagePanel.OffsetRight = 420f;
		_messagePanel.OffsetTop = -228f;
		_messagePanel.OffsetBottom = -56f;

		_messageLabel.AnchorLeft = 0.0f;
		_messageLabel.AnchorRight = 1.0f;
		_messageLabel.AnchorTop = 0.0f;
		_messageLabel.AnchorBottom = 1.0f;
		_messageLabel.OffsetLeft = 22f;
		_messageLabel.OffsetRight = -22f;
		_messageLabel.OffsetTop = 18f;
		_messageLabel.OffsetBottom = -18f;
		_messageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		ConfigureObjectiveHudLayout();
	}

	private void ConfigureObjectiveHudLayout()
	{
		_objectivePanel.AnchorLeft = 0.0f;
		_objectivePanel.AnchorRight = 0.0f;
		_objectivePanel.AnchorTop = 0.0f;
		_objectivePanel.AnchorBottom = 0.0f;
		_objectivePanel.OffsetLeft = 20f;
		_objectivePanel.OffsetRight = 420f;
		_objectivePanel.OffsetTop = 88f;
		_objectivePanel.OffsetBottom = 206f;

		var box = _objectivePanel.GetNode<VBoxContainer>("ObjectiveContent");
		box.AnchorLeft = 0.0f;
		box.AnchorRight = 1.0f;
		box.AnchorTop = 0.0f;
		box.AnchorBottom = 1.0f;
		box.OffsetLeft = 16f;
		box.OffsetRight = -16f;
		box.OffsetTop = 12f;
		box.OffsetBottom = -12f;
	}

	private static void ConfigureTopLeftLabel(Label label, float top, float height)
	{
		label.AnchorLeft = 0.0f;
		label.AnchorRight = 1.0f;
		label.AnchorTop = 0.0f;
		label.AnchorBottom = 0.0f;
		label.OffsetLeft = 20f;
		label.OffsetRight = -20f;
		label.OffsetTop = top;
		label.OffsetBottom = top + height;
	}

	public override void _Process(double delta)
	{
		RefreshObjectiveHudVisibility();
		UpdatePlayerTileMarker();
		CheckAutoExit();
	}

	protected void SetCurrentObjective(
		string chapter,
		string objective,
		string reason = "",
		bool flash = false,
		GameFlow.TrackedQuestKind kind = GameFlow.TrackedQuestKind.Mainline,
		string? id = null)
	{
		id ??= kind == GameFlow.TrackedQuestKind.Mainline ? "prologue_mainline" : null;
		_flow?.TrackObjective(chapter, objective, reason, kind, id);
		RefreshObjectiveHudItems();

		if (flash)
			FlashObjectiveHud();
	}

	protected void SetPrologueMainObjective(bool flash = false)
	{
		(string objective, string reason) = ResolvePrologueMainObjective();
		SetCurrentObjective("序章 · 主线", objective, reason, flash);
	}

	private (string Objective, string Reason) ResolvePrologueMainObjective()
	{
		if (!HasQuestFlag("prologue_opening_seen"))
			return ("听师姐安排今日的寿宴准备", "清晨的山院仍像往常一样热闹");
		if (!HasQuestFlag("prologue_herb_tutorial_seen"))
			return ("去厨房仓房小潭找师姐学采药", "主角小时候被护得太好，师姐今日才肯正式教");
		if (!HasQuestFlag("prologue_ore_tutorial_seen"))
			return ("听师姐教你识别浅表矿脉", "师姐只准你碰安全位置，不许逞强靠近崖边");
		if (!HasQuestFlag("prologue_mount_foreshadowed"))
			return ("去雾林小径查看溪边动物足迹", "这只是山兽留下的蹄印，不是危险的外人痕迹");
		if (!HasQuestFlag("prologue_manor_errands_started"))
			return ("回山院帮大家准备寿宴", "山庄里还有许多温柔小事等着你");
		if (!HasQuestFlag("prologue_manor_errands_completed"))
			return (BuildManorErrandObjectiveText(), "选了先帮哪边都只是接下差事，得亲自跑到对应位置做完");
		if (!HasQuestFlag("prologue_wine_delayed"))
			return ("去雾门前听师姐催你取酒", "你答应得很快，却还想再帮山庄里的人一会儿");
		if (!HasQuestFlag("prologue_wine_obtained"))
			return ("沿后山小径去崖洞取寿酒", "天色已经压低，你却觉得路熟得闭着眼也能到");
		if (!HasQuestFlag("prologue_cave_overnight"))
			return ("在崖洞草席处歇一夜", "天黑路湿，你觉得等山路安全些再回也来得及");
		if (!HasQuestFlag("prologue_silent_return_seen"))
			return ("沿雾林小径返回风止山院", "虫鸣全消，连风声都像停了");
		if (!HasQuestFlag("prologue_massacre_discovered"))
			return ("去正堂与书房搜证", "不要急着复仇，先确认发生了什么");
		if (!HasQuestFlag("prologue_senior_brother_returned"))
			return ("去雾门外确认归山脚步声", "你已经知道山院出了事，远处却传来采买车轮声");
		if (!HasQuestFlag("senior_brother_mis_resolved"))
			return ("到小潭边向师兄说清误会", "师兄只看见你独自活着，怀里还有血书与未开的酒");
		if (!HasQuestFlag("prologue_joint_burial_completed"))
			return ("到庄训前与师兄安葬门人", "活着的人，先把他们送好");
		if (!HasQuestFlag("senior_brother_letter_contact_unlocked"))
			return ("回正堂接受师兄临别传承与书信约定", "以后没人护你了，你得会自保");

		return ("带着血书下山查明真相", "风起渊底，鹤归无枝");
	}

	private string BuildManorErrandObjectiveText()
	{
		static string Mark(bool done) => done ? "[x]" : "[ ]";
		return "完成寿宴准备：\n" +
			$"{Mark(HasQuestFlag("manor_errand_kitchen_done"))} 厨房送药草\n" +
			$"{Mark(HasQuestFlag("manor_errand_pharmacy_done"))} 药房分拣药包\n" +
			$"{Mark(HasQuestFlag("manor_errand_junior_done"))} 给师弟传话";
	}

	protected void ClearCurrentObjective()
	{
		_objectivePanel.Visible = false;
		foreach (var label in _objectiveItemLabels)
		{
			label.Text = "";
			label.Visible = false;
		}
	}

	private void RefreshObjectiveHudVisibility()
	{
		if (_objectiveItemLabels.All(label => string.IsNullOrWhiteSpace(label.Text)))
			return;

		_objectivePanel.Visible =
			DialogueManager?.IsDialogueActive != true &&
			!_messagePanel.Visible;
	}

	private void RefreshObjectiveHudItems()
	{
		IReadOnlyList<GameFlow.TrackedQuestObjective> tracked =
			_flow?.GetTrackedObjectivesForHud() ?? Array.Empty<GameFlow.TrackedQuestObjective>();
		for (var i = 0; i < _objectiveItemLabels.Length; i++)
		{
			var label = _objectiveItemLabels[i];
			if (i >= tracked.Count)
			{
				label.Text = "";
				label.Visible = false;
				continue;
			}

			var objective = tracked[i];
			var prefix = objective.Kind switch
			{
				GameFlow.TrackedQuestKind.Mainline => "主线",
				GameFlow.TrackedQuestKind.Side => "支线",
				GameFlow.TrackedQuestKind.Tutorial => "教学",
				_ => "目标",
			};
			label.Text = $"{prefix}：{objective.Text}";
			label.Visible = true;
		}

		_objectivePanel.Visible = tracked.Count > 0;
	}

	private void FlashObjectiveHud()
	{
		_objectivePanel.SelfModulate = new Color(1.0f, 0.92f, 0.68f, 1.0f);
		var tween = CreateTween();
		tween.TweenProperty(_objectivePanel, "self_modulate", Colors.White, 0.8)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (DialogueManager?.IsDialogueActive == true)
		{
			HandleDialogueInput(@event);
			return;
		}

		if (@event.IsActionPressed("toggle_attribute_panel"))
		{
			_attributePanel?.Toggle();
			Player.MovementFrozen = _attributePanel?.Visible == true;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event.IsActionPressed("interact"))
		{
			if (_messagePanel.Visible)
			{
				HideMessage();
				return;
			}

			InteractWithFocusedArea();
		}

		if (@event is InputEventMouseButton mouse &&
			mouse.Pressed &&
			mouse.ButtonIndex == MouseButton.Left &&
			!_messagePanel.Visible)
		{
			var clickedTile = ScreenToTile(GetGlobalMousePosition());
			var currentTile = ScreenToTile(Player.Position);
			var path = FindPath(currentTile, clickedTile);
			if (path is { Count: > 0 })
			{
				Player.SetTilePath(path);
			}
		}
	}

	protected void LoadVariant(string variant, bool repositionPlayer)
	{
		GD.Print($"[{SceneName}] LoadVariant: switching to '{variant}' (reposition={repositionPlayer})...");
		Variant = variant;
		_focusedArea = null;
		_markers.Clear();
		_interactionAreas.Clear();
		_groundTiles.Clear();
		_blockedTiles.Clear();
		ClearChildren(_structures);
		ClearChildren(_collision);
		ClearChildren(_logicMarkers);

		var mapPath = variant == "day" ? DayMapPath : NightMapPath;
		GD.Print($"[{SceneName}] LoadVariant: loading TMX from {mapPath}...");
		var root = LoadTmx(mapPath);
		_mapWidth = int.Parse(root.Attribute("width")?.Value ?? "16", CultureInfo.InvariantCulture);
		_mapHeight = int.Parse(root.Attribute("height")?.Value ?? "16", CultureInfo.InvariantCulture);
		ValidateTmxTileSize(root, mapPath);

		ConfigureTileLayerVariant(variant);
		ReadGroundTiles(root);
		GD.Print($"[{SceneName}] LoadVariant: {_groundTiles.Count} walkable ground tiles parsed.");
		BuildStructures(root);
		BuildCollision(root);
		GD.Print($"[{SceneName}] LoadVariant: {_blockedTiles.Count} blocked tiles parsed.");
		BuildLogicMarkers(root);
		GD.Print($"[{SceneName}] LoadVariant: {_markers.Count} logic markers registered.");

		if (repositionPlayer)
		{
			var entryMarker = SceneTransitionManager.PendingEntryMarker ?? DefaultExitMarker;
			SceneTransitionManager.ClearPendingEntry();
			if (_markers.TryGetValue(entryMarker, out var entry))
			{
				GD.Print($"[{SceneName}] LoadVariant: positioning player at '{entryMarker}' tile=({entry.TileX},{entry.TileY}).");
				ConfigurePlayerTileMovement(new Vector2I(entry.TileX, entry.TileY));
			}
			else if (_markers.TryGetValue(DefaultExitMarker, out var fallback))
			{
				GD.Print($"[{SceneName}] LoadVariant: positioning player at default exit '{DefaultExitMarker}' tile=({fallback.TileX},{fallback.TileY}).");
				ConfigurePlayerTileMovement(new Vector2I(fallback.TileX, fallback.TileY));
			}
			else
			{
				ConfigurePlayerTileMovement(ScreenToTile(Player.Position));
			}
		}
		else
		{
			ConfigurePlayerTileMovement(ScreenToTile(Player.Position));
		}

		var isNight = variant == "night";
		_mapRoot.Modulate = isNight ? new Color(0.72f, 0.78f, 1.0f, 1f) : Colors.White;
		_backgroundTint.Color = isNight
			? new Color(0.04f, 0.06f, 0.10f, 1f)
			: new Color(0.14f, 0.13f, 0.10f, 1f);
		ConditionProvider?.SetFlag("variant", variant);
		OnLoadVariant(variant);
		UpdatePrompt();
		GD.Print($"[{SceneName}] LoadVariant: '{variant}' loaded successfully.");
	}

	protected abstract void OnLoadVariant(string variant);
	protected abstract void OnInteract(string markerName);
	protected virtual bool ShouldCreateInteractionForMarker(Marker marker) => true;
	protected virtual bool TryHandleInteractionBeforeDefault(string markerName, Marker marker) => false;
	protected virtual void OnPlayerTileChanged(Vector2I tile) { }
	protected virtual HashSet<string> GetEnabledStructures() => new(StringComparer.Ordinal);
	// 默认道具缩放按 128x64 tile 校准，确保物件 footprint 与丹房地块一致。
	protected virtual float GetStructureScale(string name) => 0.5f;
	protected virtual Vector2I? GetStructureTileOverride(string name) => null;
	protected bool TryGetMarkerTile(string markerName, out Vector2I tile)
	{
		if (_markers.TryGetValue(markerName, out var marker))
		{
			tile = new Vector2I(marker.TileX, marker.TileY);
			return true;
		}

		tile = default;
		return false;
	}

	private void ConfigureTileLayerVariant(string variant)
	{
		var isNight = variant == "night";
		_dayTileLayers.Visible = !isNight;
		_nightTileLayers.Visible = isNight;
	}

	private void ConfigureTileLayerAnchor()
	{
		_tileLayersRoot.Position = new Vector2(
			Origin.X - TileWidth / 2f,
			Origin.Y - TileHeight / 2f);
	}

	private void ValidateTmxTileSize(XElement root, string mapPath)
	{
		var tmxTileWidth = int.Parse(root.Attribute("tilewidth")?.Value ?? "0", CultureInfo.InvariantCulture);
		var tmxTileHeight = int.Parse(root.Attribute("tileheight")?.Value ?? "0", CultureInfo.InvariantCulture);
		if (tmxTileWidth == TileWidth && tmxTileHeight == TileHeight)
		{
			return;
		}

		throw new InvalidOperationException(
			$"{mapPath} 的 tile 尺寸是 {tmxTileWidth}x{tmxTileHeight}，但全局地图场景必须使用丹房同规格 {TileWidth}x{TileHeight}。");
	}

	private void ReadGroundTiles(XElement root)
	{
		var layer = FindLayer(root, "Ground");
		var gids = ParseCsv(layer.Element("data")?.Value ?? string.Empty);
		for (var y = 0; y < _mapHeight; y++)
		{
			for (var x = 0; x < _mapWidth; x++)
			{
				if (gids[y * _mapWidth + x] != 0)
				{
					_groundTiles.Add(new Vector2I(x, y));
				}
			}
		}
	}

	private void BuildStructures(XElement root)
	{
		var enabled = GetEnabledStructures();
		var group = FindObjectGroup(root, "Structures");
		var count = 0;
		foreach (var obj in group.Elements("object"))
		{
			var name = obj.Attribute("name")?.Value ?? "prop";
			if (!enabled.Contains(name))
				continue;

			var props = ReadProperties(obj);
			if (!props.TryGetValue("image", out var image) ||
				!props.TryGetValue("tile_x", out var tileXText) ||
				!props.TryGetValue("tile_y", out var tileYText))
				continue;

			var tileX = ParseInt(tileXText);
			var tileY = ParseInt(tileYText);
			var overrideTile = GetStructureTileOverride(name);
			if (overrideTile is not null)
			{
				tileX = overrideTile.Value.X;
				tileY = overrideTile.Value.Y;
			}

			var texturePath = $"{AssetRoot}/props/{System.IO.Path.GetFileName(image)}";
			GD.Print($"[{SceneName}] BuildStructures: loading '{name}' from {texturePath} at tile=({tileX},{tileY}).");
			var texture = LoadTexture(texturePath);
			var size = texture.GetSize();
			var scale = GetStructureScale(name);
			var anchor = TileToScreen(tileX, tileY);
			var sprite = new Sprite2D
			{
				Name = name,
				Texture = texture,
				Centered = false,
				Scale = new Vector2(scale, scale),
				Position = new Vector2(anchor.X, anchor.Y + TileHeight / 2f),
				Offset = new Vector2(-size.X / 2f, -size.Y),
			};
			_structures.AddChild(sprite);
			count++;
		}
		GD.Print($"[{SceneName}] BuildStructures: {count} structures placed.");
	}

	private void BuildCollision(XElement root)
	{
		var layer = FindLayer(root, "Collision");
		var gids = ParseCsv(layer.Element("data")?.Value ?? string.Empty);
		for (var y = 0; y < _mapHeight; y++)
		{
			for (var x = 0; x < _mapWidth; x++)
			{
				if (gids[y * _mapWidth + x] == 0)
					continue;

				_blockedTiles.Add(new Vector2I(x, y));
				var body = new StaticBody2D
				{
					Name = $"Block_{x}_{y}",
					Position = TileToScreen(x, y),
				};
				var polygon = new CollisionPolygon2D
				{
					Name = "BlockedTileCollision",
					Polygon = GetIsoDiamondPolygon(),
				};
				body.AddChild(polygon);
				body.AddChild(CreateBlockedTileOverlay());
				body.AddChild(CreateBlockedTileOutline());
				_collision.AddChild(body);
			}
		}
	}

	private Polygon2D CreateBlockedTileOverlay() =>
		new()
		{
			Name = "BlockedTileOverlay",
			Polygon = GetIsoDiamondPolygon(),
			Color = BlockedTileOverlayFill,
			ZIndex = 20,
		};

	private Line2D CreateBlockedTileOutline() =>
		new()
		{
			Name = "BlockedTileOutline",
			Points = GetIsoDiamondOutline(),
			DefaultColor = BlockedTileOverlayOutline,
			Width = 2.5f,
			ZIndex = 21,
		};

	private void BuildLogicMarkers(XElement root)
	{
		var group = FindObjectGroup(root, "LogicMarkers");
		foreach (var obj in group.Elements("object"))
		{
			var name = obj.Attribute("name")?.Value ?? string.Empty;
			var type = obj.Attribute("type")?.Value ?? string.Empty;
			var props = ReadProperties(obj);
			if (!props.TryGetValue("tile_x", out var tileXText) ||
				!props.TryGetValue("tile_y", out var tileYText))
				continue;

			var marker = new Marker(name, type, ParseInt(tileXText), ParseInt(tileYText), props);
			_markers[name] = marker;
			GD.Print($"[{SceneName}] BuildLogicMarkers: registered '{name}' type='{type}' at tile=({marker.TileX},{marker.TileY}).");

			if (IsBlockingInteractionMarker(type))
				_blockedTiles.Add(new Vector2I(marker.TileX, marker.TileY));

			if (!ShouldCreateInteractionForMarker(marker))
				continue;

			if (type == "blocker" || type == "entry")
				continue;

			if (type == "npc")
			{
				SpawnStaticNpc(marker);
				continue;
			}

			if (type == "exit")
			{
				var targetScene = marker.Props?.GetValueOrDefault("target_scene");
				if (!string.IsNullOrEmpty(targetScene))
				{
					var highlight = new Polygon2D
					{
						Name = $"{name}_highlight",
						Polygon = new Vector2[]
						{
							new(-TileWidth / 2f, 0f),
							new(0f, -TileHeight / 2f),
							new(TileWidth / 2f, 0f),
							new(0f, TileHeight / 2f),
						},
						Color = new Color(1f, 0.82f, 0.3f, 0.35f),
						Position = TileToScreen(marker.TileX, marker.TileY),
					};
					_logicMarkers.AddChild(highlight);
				}
				continue;
			}

			var area = CreateInteractionArea(name, TileToScreen(marker.TileX, marker.TileY));
			_logicMarkers.AddChild(area);
		}
	}

	private void SpawnStaticNpc(Marker marker)
	{
		var characterId = marker.Props?.GetValueOrDefault("character_id") ?? "main_character";
		var facing = marker.Props?.GetValueOrDefault("facing") ?? "se";

		var tresPath = $"res://assets/character/{characterId}.tres";
		var frames = GD.Load<SpriteFrames>(tresPath);
		if (frames == null)
		{
			GD.PrintErr($"[{SceneName}] SpawnStaticNpc: failed to load SpriteFrames '{tresPath}'");
			return;
		}

		var npcRoot = new Node2D
		{
			Name = marker.Name,
			Position = TileToScreen(marker.TileX, marker.TileY),
		};

		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Animation = "idle",
			Position = new Vector2(-2.66f, -18f),
			Scale = new Vector2(0.6147f, 0.62f),
		};
		npcRoot.AddChild(sprite);
		sprite.Play();

		var area = CreateInteractionArea(marker.Name, Vector2.Zero);
		npcRoot.AddChild(area);
		npcRoot.AddChild(CreateCharacterBlockingBody(marker.Name));

		_mapRoot.AddChild(npcRoot);
		GD.Print($"[{SceneName}] SpawnStaticNpc: '{marker.Name}' ({characterId}) placed at tile=({marker.TileX},{marker.TileY}).");
	}

	private static StaticBody2D CreateCharacterBlockingBody(string markerName)
	{
		var body = new StaticBody2D
		{
			Name = $"{markerName}_body",
			CollisionLayer = 1,
			CollisionMask = 0,
		};
		var collision = new CollisionPolygon2D
		{
			Name = "CharacterFootprintCollision",
		};
		CharacterFootprint.ApplyTo(collision);
		body.AddChild(collision);
		return body;
	}

	private Area2D CreateInteractionArea(string name, Vector2 position)
	{
		var area = new Area2D
		{
			Name = name,
			Position = position - InteractionDiamondOffset,
		};

		var collision = new CollisionPolygon2D
		{
			Name = "InteractionDiamondCollision",
			Polygon = GetIsoDiamondPolygon(),
			Position = InteractionDiamondOffset,
		};
		area.AddChild(collision);

		var highlight = new Polygon2D
		{
			Name = "InteractionHighlight",
			Polygon = GetIsoDiamondPolygon(),
			Color = InteractionDefaultFill,
			Position = InteractionDiamondOffset,
			Visible = true,
			ZIndex = 60,
		};
		area.AddChild(highlight);

		var outline = new Line2D
		{
			Name = "InteractionHighlightOutline",
			Points = GetIsoDiamondOutline(),
			DefaultColor = InteractionHighlightOutline,
			Width = 3f,
			Position = InteractionDiamondOffset,
			Visible = false,
			ZIndex = 61,
		};
		area.AddChild(outline);

		area.BodyEntered += body => OnInteractionEntered(area, body);
		area.BodyExited += body => OnInteractionExited(area, body);
		_interactionAreas[name] = area;
		return area;
	}

	private static bool IsBlockingInteractionMarker(string type) =>
		type != "blocker" &&
		type != "entry" &&
		type != "exit";

	private Vector2[] GetIsoDiamondPolygon() =>
		new Vector2[]
		{
			new(-TileWidth / 2f, 0f),
			new(0f, -TileHeight / 2f),
			new(TileWidth / 2f, 0f),
			new(0f, TileHeight / 2f),
		};

	private Vector2[] GetIsoDiamondOutline()
	{
		var polygon = GetIsoDiamondPolygon();
		return new Vector2[]
		{
			polygon[0],
			polygon[1],
			polygon[2],
			polygon[3],
			polygon[0],
		};
	}

	private void ConfigurePlayerTileMovement(Vector2I tile)
	{
		if (!IsWalkableTile(tile) && _markers.TryGetValue(DefaultExitMarker, out var exit))
			tile = new Vector2I(exit.TileX, exit.TileY);

		_lastPlayerTile = tile;
		Player.ConfigureTileMovement(
			tile,
			nextTile => TileToScreen(nextTile.X, nextTile.Y),
			IsWalkableTile);
		_markers["player_tile"] = new Marker("player_tile", "runtime", tile.X, tile.Y);

		var camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
		camera?.ResetSmoothing();
	}

	protected void StartDialogue(string yamlPath)
	{
		GD.Print($"[{SceneName}] StartDialogue: {yamlPath}");
		Player.MovementFrozen = true;
		DialogueManager?.StartDialogue(yamlPath);
	}

	protected void ShowMessage(string text)
	{
		_messageLabel.Text = text;
		_messagePanel.Visible = true;
		PromptLabel.Visible = false;
		Player.MovementFrozen = true;
	}

	private void HideMessage()
	{
		_messagePanel.Visible = false;
		Player.MovementFrozen = false;
		UpdatePrompt();
	}

	private void HandleDialogueInput(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
			DialogueManager!.HandleConfirm();
		else if (@event.IsActionPressed("ui_up"))
			DialogueManager!.HandleMoveSelection(-1);
		else if (@event.IsActionPressed("ui_down"))
			DialogueManager!.HandleMoveSelection(1);
		else if (@event.IsActionPressed("investigate"))
			DialogueManager!.HandleInvestigate();
	}

	private void OnDialogueEnded()
	{
		Player.MovementFrozen = false;
		UpdatePrompt();
	}

	private void InteractWithFocusedArea()
	{
		if (_focusedArea is null)
			return;
		var markerName = _focusedArea.Name.ToString();
		GD.Print($"[{SceneName}] Interact: '{markerName}'");

		if (_markers.TryGetValue(markerName, out var marker))
		{
			if (TryHandleInteractionBeforeDefault(markerName, marker))
				return;

			if (marker.Type == "npc")
			{
				var dialogueId = marker.Props?.GetValueOrDefault("dialogue_id");
				if (!string.IsNullOrEmpty(dialogueId))
				{
					StartDialogue(dialogueId);
					return;
				}
			}
		}

		OnInteract(markerName);
	}

	protected virtual void OnExitInteract(string markerName)
	{
		if (!_markers.TryGetValue(markerName, out var marker))
			return;

		var targetScene = marker.Props?.GetValueOrDefault("target_scene");
		var entryMarker = marker.Props?.GetValueOrDefault("entry_marker");

		if (string.IsNullOrEmpty(targetScene))
		{
			ShowMessage("前方通往其他区域。（场景切换尚未实现）");
			return;
		}

		Player.MovementFrozen = true;
		var transition = GetNode<SceneTransitionManager>("/root/SceneTransition");
		transition.TransitionTo(targetScene, entryMarker);
	}

	private void OnInteractionEntered(Area2D area, Node2D body)
	{
		if (body != Player)
			return;

		FocusInteractionArea(area);
	}

	private void OnInteractionExited(Area2D area, Node2D body)
	{
		if (body != Player || _focusedArea != area)
			return;
		GD.Print($"[{SceneName}] Player exited interaction zone: '{area.Name}'");
		if (!IsPlayerAdjacentToArea(area))
			ClearFocusedInteraction();
	}

	private static void SetInteractionHighlight(Area2D? area, bool visible)
	{
		if (area is null)
			return;

		var fill = area.GetNodeOrNull<Polygon2D>("InteractionHighlight");
		if (fill != null)
		{
			fill.Visible = true;
			fill.Color = visible ? InteractionHighlightFill : InteractionDefaultFill;
		}

		var outline = area.GetNodeOrNull<Line2D>("InteractionHighlightOutline");
		if (outline != null)
			outline.Visible = visible;
	}

	private void UpdatePrompt()
	{
		if (_focusedArea is null || _messagePanel.Visible)
		{
			PromptLabel.Visible = false;
			return;
		}

		PromptLabel.Text = "E / 空格 调查";
		PromptLabel.Visible = true;
	}

	private void UpdatePlayerTileMarker()
	{
		var tile = ScreenToTile(Player.Position);
		_markers["player_tile"] = new Marker("player_tile", "runtime", tile.X, tile.Y);
		if (tile != _lastPlayerTile)
			OnPlayerTileChanged(tile);
		UpdateAdjacentInteractionFocus(tile);
	}

	private void UpdateAdjacentInteractionFocus(Vector2I playerTile)
	{
		foreach (var (name, marker) in _markers)
		{
			if (!IsBlockingInteractionMarker(marker.Type) ||
				!IsAdjacent(playerTile, new Vector2I(marker.TileX, marker.TileY)) ||
				!_interactionAreas.TryGetValue(name, out var area))
			{
				continue;
			}

			FocusInteractionArea(area);
			return;
		}

		ClearFocusedInteraction();
	}

	private void FocusInteractionArea(Area2D area)
	{
		if (_focusedArea == area)
			return;

		SetInteractionHighlight(_focusedArea, false);
		_focusedArea = area;
		SetInteractionHighlight(area, true);
		GD.Print($"[{SceneName}] Player focused interaction zone: '{area.Name}'");
		UpdatePrompt();
	}

	private void ClearFocusedInteraction()
	{
		if (_focusedArea == null)
			return;

		SetInteractionHighlight(_focusedArea, false);
		_focusedArea = null;
		UpdatePrompt();
	}

	private bool IsPlayerAdjacentToArea(Area2D area)
	{
		var areaName = area.Name.ToString();
		if (!_markers.TryGetValue(areaName, out var marker))
			return false;

		return IsAdjacent(
			ScreenToTile(Player.Position),
			new Vector2I(marker.TileX, marker.TileY));
	}

	private static bool IsAdjacent(Vector2I a, Vector2I b)
	{
		var delta = a - b;
		return Math.Abs(delta.X) + Math.Abs(delta.Y) == 1;
	}

	private void CheckAutoExit()
	{
		if (!_autoExitEnabled || Player.MovementFrozen)
			return;

		var tile = ScreenToTile(Player.Position);
		if (tile == _lastPlayerTile)
			return;
		_lastPlayerTile = tile;

		foreach (var (name, marker) in _markers)
		{
			if (marker.Type != "exit")
				continue;
			if (marker.TileX == tile.X && marker.TileY == tile.Y)
			{
				GD.Print($"[{SceneName}] Auto-exit triggered: '{name}'");
				OnExitInteract(name);
				return;
			}
		}
	}

	protected Vector2 TileToScreen(int x, int y)
	{
		return new Vector2(
			Origin.X + (x - y) * TileWidth / 2f,
			Origin.Y + (x + y) * TileHeight / 2f);
	}

	private Vector2I ScreenToTile(Vector2 worldPosition)
	{
		var cart = IsoProjection.ScreenToCart(worldPosition - Origin);
		return new Vector2I(Mathf.RoundToInt(cart.X), Mathf.RoundToInt(cart.Y));
	}

	private List<Vector2I>? FindPath(Vector2I start, Vector2I goal)
	{
		if (!IsWalkableTile(start) || !IsWalkableTile(goal))
			return null;

		var queue = new Queue<Vector2I>();
		var cameFrom = new Dictionary<Vector2I, Vector2I>();
		queue.Enqueue(start);
		cameFrom[start] = start;

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (current == goal)
				return ReconstructPath(cameFrom, start, goal);

			foreach (var next in Neighbors(current))
			{
				if (cameFrom.ContainsKey(next) || !IsWalkableTile(next))
					continue;
				cameFrom[next] = current;
				queue.Enqueue(next);
			}
		}

		return null;
	}

	private static List<Vector2I> ReconstructPath(
		IReadOnlyDictionary<Vector2I, Vector2I> cameFrom,
		Vector2I start,
		Vector2I goal)
	{
		var path = new List<Vector2I>();
		var current = goal;
		while (current != start)
		{
			path.Add(current);
			current = cameFrom[current];
		}

		path.Add(start);
		path.Reverse();
		return path;
	}

	private static IEnumerable<Vector2I> Neighbors(Vector2I tile)
	{
		yield return tile + new Vector2I(1, 0);
		yield return tile + new Vector2I(-1, 0);
		yield return tile + new Vector2I(0, 1);
		yield return tile + new Vector2I(0, -1);
	}

	private bool IsWalkableTile(Vector2I tile)
	{
		return tile.X >= 0 &&
			tile.Y >= 0 &&
			tile.X < _mapWidth &&
			tile.Y < _mapHeight &&
			_groundTiles.Contains(tile) &&
			!_blockedTiles.Contains(tile);
	}

	private static XElement LoadTmx(string resPath)
	{
		using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
		if (file is null)
			throw new InvalidOperationException($"Unable to open {resPath}");
		return XDocument.Parse(file.GetAsText()).Root ??
			throw new InvalidOperationException($"TMX has no root: {resPath}");
	}

	private static Texture2D LoadTexture(string resPath)
	{
		return GD.Load<Texture2D>(resPath) ??
			throw new InvalidOperationException($"Unable to load texture {resPath}");
	}

	private static XElement FindLayer(XElement root, string name)
	{
		return root.Elements("layer").FirstOrDefault(l => l.Attribute("name")?.Value == name) ??
			throw new InvalidOperationException($"TMX layer not found: {name}");
	}

	private static XElement FindObjectGroup(XElement root, string name)
	{
		return root.Elements("objectgroup").FirstOrDefault(g => g.Attribute("name")?.Value == name) ??
			throw new InvalidOperationException($"TMX object group not found: {name}");
	}

	private int[] ParseCsv(string csv)
	{
		var values = csv
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(ParseInt)
			.ToArray();
		if (values.Length != _mapWidth * _mapHeight)
			throw new InvalidOperationException($"Expected {_mapWidth * _mapHeight} gids, got {values.Length}");
		return values;
	}

	private static Dictionary<string, string> ReadProperties(XElement element)
	{
		return element
			.Elements("properties")
			.Elements("property")
			.Where(prop => prop.Attribute("name") is not null)
			.ToDictionary(
				prop => prop.Attribute("name")!.Value,
				prop => prop.Attribute("value")?.Value ?? string.Empty);
	}

	private static int ParseInt(string value)
	{
		return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	protected static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	protected readonly record struct Marker(string Name, string Type, int TileX, int TileY, Dictionary<string, string>? Props = null);
}
