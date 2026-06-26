using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Geometry;
using FengZhi.Foundation.Mindset;
using FengZhi.Ui;
using Godot;

namespace FengZhi;

public abstract partial class SceneGameBase : Node2D
{
	// 默认 iso 网格尺寸 128x64（ADR-0022 rev 2，跟 IsoProjection 常量保持一致）。
	// 个别场景如有特殊需要可以 override，但项目方向是所有 tile 资产统一 128x64。
	protected virtual int TileWidth => 128;
	protected virtual int TileHeight => 64;

	protected abstract string AssetRoot { get; }
	protected abstract string DayMapPath { get; }
	protected abstract string NightMapPath { get; }
	protected abstract string SceneName { get; }
	protected abstract string DefaultExitMarker { get; }

	private readonly Dictionary<string, Marker> _markers = new();
	private readonly HashSet<Vector2I> _groundTiles = new();
	private readonly HashSet<Vector2I> _blockedTiles = new();
	private int _mapWidth = 16;
	private int _mapHeight = 16;
	private bool _autoExitEnabled;
	private Vector2I _lastPlayerTile = new(-999, -999);

	private Node2D _mapRoot = null!;
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
	private Area2D? _focusedArea;
	protected Dialogue.DialogueManager? DialogueManager;
	private Dialogue.DialoguePanel? _dialoguePanel;
	private AttributePanel? _attributePanel;
	protected Dialogue.SceneConditionValueProvider? ConditionProvider;
	protected string Variant = "day";

	protected virtual Vector2 Origin { get; set; } = new(576f, 96f);

	public override void _Ready()
	{
		GD.Print($"[{SceneName}] _Ready: begin node binding...");
		_mapRoot = GetNode<Node2D>("MapRoot");
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
		GD.Print($"[{SceneName}] _Ready: all nodes bound.");

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

		var flow = GetNodeOrNull<Vs.JiangnanFlowController>("/root/JiangnanFlow");
		IEventBus eventBus;
		MindsetService mindsetService;
		if (flow != null)
		{
			GD.Print($"[{SceneName}] _Ready: JiangnanFlow autoload found, using shared EventBus/MindsetService.");
			eventBus = flow.EventBus;
			mindsetService = flow.MindsetService;
		}
		else
		{
			GD.Print($"[{SceneName}] _Ready: JiangnanFlow not found, creating standalone EventBus/MindsetService.");
			eventBus = new EventBus();
			mindsetService = new MindsetService(eventBus: eventBus);
		}

		ConditionProvider = new Dialogue.SceneConditionValueProvider(mindsetService);
		ConditionProvider.SetFlag("variant", Variant);
		DialogueManager.Initialize(eventBus, mindsetService, _dialoguePanel, ConditionProvider);
		DialogueManager.DialogueEnded += OnDialogueEnded;
		GD.Print($"[{SceneName}] _Ready: dialogue system initialized.");

		LoadVariant("day", repositionPlayer: true);
		OnReady();

		var transition = GetNodeOrNull<SceneTransitionManager>("/root/SceneTransition");
		if (transition != null)
			transition.FadeIn();

		_autoExitEnabled = false;
		GetTree().CreateTimer(0.5).Timeout += () => _autoExitEnabled = true;

		GD.Print($"[{SceneName}] _Ready: complete.");
	}

	protected virtual void OnReady() { }

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
		UpdatePlayerTileMarker();
		CheckAutoExit();
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

		if (@event is InputEventKey key &&
			key.Pressed &&
			!key.Echo &&
			key.PhysicalKeycode == Key.N)
		{
			LoadVariant(Variant == "day" ? "night" : "day", repositionPlayer: false);
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
	protected virtual HashSet<string> GetEnabledStructures() => new(StringComparer.Ordinal);
	// 默认道具缩放 0.5（按 128x64 tile 校准；原 0.25 对应已废弃的 64x32 网格）。
	protected virtual float GetStructureScale(string name) => 0.5f;
	protected virtual Vector2I? GetStructureTileOverride(string name) => null;

	private void ConfigureTileLayerVariant(string variant)
	{
		var isNight = variant == "night";
		_dayTileLayers.Visible = !isNight;
		_nightTileLayers.Visible = isNight;
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
					Polygon = new Vector2[]
					{
						new(-TileWidth / 2f, 0f),
						new(0f, -TileHeight / 2f),
						new(TileWidth / 2f, 0f),
						new(0f, TileHeight / 2f),
					},
				};
				body.AddChild(polygon);
				_collision.AddChild(body);
			}
		}
	}

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

			var area = new Area2D
			{
				Name = name,
				Position = TileToScreen(marker.TileX, marker.TileY),
			};
			var shape = new CollisionShape2D
			{
				Shape = new CircleShape2D { Radius = 30f },
			};
			area.AddChild(shape);
			area.BodyEntered += body => OnInteractionEntered(area, body);
			area.BodyExited += body => OnInteractionExited(area, body);
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

		var area = new Area2D { Name = marker.Name };
		var shape = new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 30f },
		};
		area.AddChild(shape);
		area.BodyEntered += body => OnInteractionEntered(area, body);
		area.BodyExited += body => OnInteractionExited(area, body);
		npcRoot.AddChild(area);

		_mapRoot.AddChild(npcRoot);
		GD.Print($"[{SceneName}] SpawnStaticNpc: '{marker.Name}' ({characterId}) placed at tile=({marker.TileX},{marker.TileY}).");
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
		_focusedArea = area;
		GD.Print($"[{SceneName}] Player entered interaction zone: '{area.Name}'");
		UpdatePrompt();
	}

	private void OnInteractionExited(Area2D area, Node2D body)
	{
		if (body != Player || _focusedArea != area)
			return;
		GD.Print($"[{SceneName}] Player exited interaction zone: '{area.Name}'");
		_focusedArea = null;
		UpdatePrompt();
	}

	private void UpdatePrompt()
	{
		if (_focusedArea is null || _messagePanel.Visible)
		{
			PromptLabel.Visible = false;
			return;
		}

		PromptLabel.Text = "E / 空格 调查    N 切换日夜";
		PromptLabel.Visible = true;
	}

	private void UpdatePlayerTileMarker()
	{
		var tile = ScreenToTile(Player.Position);
		_markers["player_tile"] = new Marker("player_tile", "runtime", tile.X, tile.Y);
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
