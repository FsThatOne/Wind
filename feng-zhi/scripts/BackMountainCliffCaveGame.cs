using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Godot;

namespace FengZhi;

/// <summary>
/// 后山崖洞首版可运行场景。
/// 运行时读取 Tiled TMX：地面 tile、物件、碰撞和逻辑标记都来自生成资产包。
/// </summary>
public partial class BackMountainCliffCaveGame : Node2D
{
	private const int MapWidth = 16;
	private const int MapHeight = 16;
	private const int TileWidth = 64;
	private const int TileHeight = 32;
	private const int TilesetColumns = 6;
	private const string AssetRoot = "res://assets/maps/back_mountain_cliff_cave";
	private const string DayMap = AssetRoot + "/maps/back_mountain_cliff_cave_day.tmx";
	private const string NightMap = AssetRoot + "/maps/back_mountain_cliff_cave_night.tmx";
	private const string TilesetPath = AssetRoot + "/tilesets/cliff_cave_ground_tiles.png";

	private readonly Vector2 _origin = new(576f, 96f);
	private readonly Dictionary<string, Marker> _markers = new();

	private Node2D _mapRoot = null!;
	private Node2D _tiles = null!;
	private Node2D _structures = null!;
	private Node2D _collision = null!;
	private Node2D _logicMarkers = null!;
	private CharacterBody2D _player = null!;
	private ColorRect _backgroundTint = null!;
	private Label _statusLabel = null!;
	private Label _promptLabel = null!;
	private Label _inventoryLabel = null!;
	private Panel _messagePanel = null!;
	private Label _messageLabel = null!;
	private Area2D? _focusedArea;
	private string _variant = "day";
	private bool _hasBirthdayWine;

	public override void _Ready()
	{
		_mapRoot = GetNode<Node2D>("MapRoot");
		_tiles = GetNode<Node2D>("MapRoot/Tiles");
		_structures = GetNode<Node2D>("MapRoot/Structures");
		_collision = GetNode<Node2D>("MapRoot/Collision");
		_logicMarkers = GetNode<Node2D>("MapRoot/LogicMarkers");
		_player = GetNode<CharacterBody2D>("Player");
		_backgroundTint = GetNode<ColorRect>("BackgroundTint");
		_statusLabel = GetNode<Label>("UiLayer/StatusLabel");
		_promptLabel = GetNode<Label>("UiLayer/PromptLabel");
		_inventoryLabel = GetNode<Label>("UiLayer/InventoryLabel");
		_messagePanel = GetNode<Panel>("UiLayer/MessagePanel");
		_messageLabel = GetNode<Label>("UiLayer/MessagePanel/MessageLabel");

		_messagePanel.Visible = false;
		LoadVariant("day", repositionPlayer: true);
		UpdateInventoryLabel();
		GD.Print("[BackMountainCliffCave] Ready. TMX day/night runtime loader active.");
	}

	public override void _Process(double delta)
	{
		_player.ZIndex = 1000 + Mathf.RoundToInt(_player.Position.Y);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
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
			LoadVariant(_variant == "day" ? "night" : "day", repositionPlayer: false);
		}
	}

	private void LoadVariant(string variant, bool repositionPlayer)
	{
		_variant = variant;
		_focusedArea = null;
		_markers.Clear();
		ClearChildren(_tiles);
		ClearChildren(_structures);
		ClearChildren(_collision);
		ClearChildren(_logicMarkers);

		var mapPath = variant == "day" ? DayMap : NightMap;
		var root = LoadTmx(mapPath);
		var atlas = LoadTexture(TilesetPath);

		BuildTileLayers(root, atlas);
		BuildStructures(root);
		BuildCollision(root);
		BuildLogicMarkers(root);

		if (repositionPlayer && _markers.TryGetValue("exit_to_back_mountain", out var exit))
		{
			_player.Position = TileToScreen(exit.TileX, exit.TileY) + new Vector2(0f, -8f);
		}

		var isNight = variant == "night";
		_mapRoot.Modulate = isNight ? new Color(0.72f, 0.78f, 1.0f, 1f) : Colors.White;
		_backgroundTint.Color = isNight ? new Color(0.04f, 0.06f, 0.10f, 1f) : new Color(0.14f, 0.13f, 0.10f, 1f);
		_statusLabel.Text = isNight
			? "后山崖洞・夜：风声止住，酒坛与旧物都沉在暗处。"
			: "后山崖洞・日常：藏酒、储物，也是你和师姐的秘密基地。";
		UpdatePrompt();
	}

	private void BuildTileLayers(XElement root, Texture2D atlas)
	{
		foreach (var layerName in new[] { "Ground", "Terrain", "Overlay" })
		{
			var layer = FindLayer(root, layerName);
			var gids = ParseCsv(layer.Element("data")?.Value ?? string.Empty);
			for (var y = 0; y < MapHeight; y++)
			{
				for (var x = 0; x < MapWidth; x++)
				{
					var gid = gids[y * MapWidth + x];
					if (gid == 0)
					{
						continue;
					}

					var tileId = gid - 1;
					var sprite = new Sprite2D
					{
						Name = $"{layerName}_{x}_{y}",
						Texture = atlas,
						RegionEnabled = true,
						RegionRect = new Rect2(
							(tileId % TilesetColumns) * TileWidth,
							(tileId / TilesetColumns) * TileHeight,
							TileWidth,
							TileHeight),
						Centered = false,
						Position = TileToScreen(x, y) - new Vector2(TileWidth / 2f, TileHeight / 2f),
						ZIndex = y * 10 + x + (layerName == "Overlay" ? 500 : 0),
					};
					_tiles.AddChild(sprite);
				}
			}
		}
	}

	private void BuildStructures(XElement root)
	{
		var group = FindObjectGroup(root, "Structures");
		foreach (var obj in group.Elements("object"))
		{
			var props = ReadProperties(obj);
			if (!props.TryGetValue("image", out var image) ||
				!props.TryGetValue("tile_x", out var tileXText) ||
				!props.TryGetValue("tile_y", out var tileYText))
			{
				continue;
			}

			var tileX = ParseInt(tileXText);
			var tileY = ParseInt(tileYText);
			var texture = LoadTexture($"{AssetRoot}/props/{System.IO.Path.GetFileName(image)}");
			var size = texture.GetSize();
			var anchor = TileToScreen(tileX, tileY);
			var sprite = new Sprite2D
			{
				Name = obj.Attribute("name")?.Value ?? "prop",
				Texture = texture,
				Centered = false,
				Position = anchor - new Vector2(size.X / 2f, size.Y - TileHeight / 2f),
				ZIndex = 700 + tileY * 10 + tileX,
			};
			_structures.AddChild(sprite);
		}
	}

	private void BuildCollision(XElement root)
	{
		var layer = FindLayer(root, "Collision");
		var gids = ParseCsv(layer.Element("data")?.Value ?? string.Empty);
		for (var y = 0; y < MapHeight; y++)
		{
			for (var x = 0; x < MapWidth; x++)
			{
				if (gids[y * MapWidth + x] == 0)
				{
					continue;
				}

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
			{
				continue;
			}

			var marker = new Marker(name, type, ParseInt(tileXText), ParseInt(tileYText));
			_markers[name] = marker;

			if (type == "blocker" || type == "exit")
			{
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

	private void OnInteractionEntered(Area2D area, Node2D body)
	{
		if (body != _player)
		{
			return;
		}

		_focusedArea = area;
		UpdatePrompt();
	}

	private void OnInteractionExited(Area2D area, Node2D body)
	{
		if (body != _player || _focusedArea != area)
		{
			return;
		}

		_focusedArea = null;
		UpdatePrompt();
	}

	private void InteractWithFocusedArea()
	{
		if (_focusedArea is null)
		{
			return;
		}

		var markerName = _focusedArea.Name.ToString();
		switch (markerName)
		{
			case "wine_pickup":
				if (_hasBirthdayWine)
				{
					ShowMessage("寿酒已经取好了，回去莫要耽搁。");
					return;
				}

				_hasBirthdayWine = true;
				UpdateInventoryLabel();
				ShowMessage(_variant == "night"
					? "酒坛还在原处，封泥微凉。你想起师姐说过：寿酒要慢慢开，不能惊了香。"
					: "取得：寿酒。\n\n这是你和师姐一起守了三年的酒，今日要送去给庄主祝寿。");
				return;
			case "storage_shelf":
				ShowMessage("木架上放着旧布包、草药和空坛。师姐总能从这些杂物里翻出正好要用的东西。");
				return;
			case "memory_marker":
				ShowMessage(_variant == "night"
					? "石壁上的刻痕隐在暗处。那几笔像还新着，却再没有人笑你腕太硬。"
					: "石壁上留着师姐补过的起手式。她说剑未出时，心先要松。");
				return;
			case "rest_spot":
				ShowMessage(_variant == "night"
					? "草席仍在。你若在这里睡下，外头的风声似乎会远一些。"
					: "秘密小窝收拾得很干净。师姐偶尔会把偷藏的点心放在这里。");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}

	private void ShowMessage(string text)
	{
		_messageLabel.Text = text;
		_messagePanel.Visible = true;
		_promptLabel.Visible = false;
	}

	private void HideMessage()
	{
		_messagePanel.Visible = false;
		UpdatePrompt();
	}

	private void UpdatePrompt()
	{
		if (_focusedArea is null || _messagePanel.Visible)
		{
			_promptLabel.Visible = false;
			return;
		}

		_promptLabel.Text = "E / 空格 调查    N 切换日夜";
		_promptLabel.Visible = true;
	}

	private void UpdateInventoryLabel()
	{
		_inventoryLabel.Text = _hasBirthdayWine
			? "任务物品：寿酒"
			: "任务物品：未取得寿酒";
	}

	private Vector2 TileToScreen(int x, int y)
	{
		return new Vector2(
			_origin.X + (x - y) * TileWidth / 2f,
			_origin.Y + (x + y) * TileHeight / 2f);
	}

	private static XElement LoadTmx(string resPath)
	{
		using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
		if (file is null)
		{
			throw new InvalidOperationException($"Unable to open {resPath}");
		}

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
		return root.Elements("layer").FirstOrDefault(layer => layer.Attribute("name")?.Value == name) ??
			throw new InvalidOperationException($"TMX layer not found: {name}");
	}

	private static XElement FindObjectGroup(XElement root, string name)
	{
		return root.Elements("objectgroup").FirstOrDefault(group => group.Attribute("name")?.Value == name) ??
			throw new InvalidOperationException($"TMX object group not found: {name}");
	}

	private static int[] ParseCsv(string csv)
	{
		var values = csv
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(ParseInt)
			.ToArray();
		if (values.Length != MapWidth * MapHeight)
		{
			throw new InvalidOperationException($"Expected {MapWidth * MapHeight} gids, got {values.Length}");
		}

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

	private static void ClearChildren(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	private readonly record struct Marker(string Name, string Type, int TileX, int TileY);
}
