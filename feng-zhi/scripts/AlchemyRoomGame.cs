using System.Collections.Generic;
using FengZhi.Foundation.Exploration.Interaction;
using FengZhi.Scripts.Audio;
using FengZhi.Scripts.Exploration;
using Godot;

namespace FengZhi;

/// <summary>
/// 炼丹房（门派公有丹房）—— v0 使用 iso 地图编辑器导出的整张 ISO 房间作为视觉层。
///
/// 不继承 SceneGameBase：iso 房间是预先渲染好的 Sprite2D 集合，
/// 不参与 .tmx / Structures / Collision / LogicMarkers 那套体系。
///
/// 交互机制全部由场景内的 InteractionController 子节点接管：
///   - 高亮 outline / 提示 / 一次性领取追踪 / 视觉状态持久化
///   - 详见 src/FengZhi.Foundation/Exploration/Interaction/
///
/// 移动机制：复用全局 PlayerCharacterController + IsoGridMovementController，
///   - 走"一步一格、自动 snap 到地砖中心"模式
///   - 可走区域由 walkable.txt 手工标注（24 cols × 16 rows，1=可走 0=障碍）
///   - tile→screen 用 half tile (64,32)，对应完整丹房菱形地块 128x64（跟全局 IsoProjection 同套数学）
///
/// 加新交互只需 2 步：
///   1. .tscn 加 Area2D 节点（命名自描述，如 PillFurnaceArea）
///   2. _Ready 里 ic.RegisterZone(...) 或 ic.RegisterOneShotZone(...)
/// </summary>
public partial class AlchemyRoomGame : Node2D
{
	private const int MapWidth = 12;
	private const int MapHeight = 7;
	private const float TileHalfWidth = 64f;
	private const float TileHalfHeight = 32f;

	// ============================================================
	// Inspector 可调参数（grid movement）
	// ============================================================

	/// <summary>
	/// iso 画布原点——即 iso tile (0,0) 的顶面菱形中心对应的世界坐标。
	/// 校准方法：运行时打印 RoomVisual/1_default_layer 下 sprite 的 GlobalPosition，
	/// 结合 stamp PNG 中顶面菱形中心在图片内的偏移（约 (85,100) for 170×204 cube stamps），
	/// 公式为 IsoOrigin = stamp_topleft(C=0,R=0) + diamond_center_in_stamp - (ColOffset*64-RowOffset*64 的偏移)。
	/// 当前值通过运行时 GlobalPosition 实测 + PNG 像素分析校准得到。
	/// 如果重新导出 iso 房间，必须重新校准。
	/// </summary>
	[Export]
	public Vector2 IsoOrigin { get; set; } = new(589f, -302f);

	/// <summary>
	/// 逻辑 tile (0,0) 对应的 iso 编辑器 col 偏移。
	/// 当前 RoomVisual 实际地板从 iso 编辑器 col=10 开始，所以默认 10。
	/// </summary>
	[Export]
	public int ColOffset { get; set; } = 10;

	/// <summary>
	/// 逻辑 tile (0,0) 对应的 iso 编辑器 row 偏移。
	/// 当前 RoomVisual 实际地板从 iso 编辑器 row=7 开始，所以默认 7。
	/// </summary>
	[Export]
	public int RowOffset { get; set; } = 7;

	/// <summary>
	/// 脚底 Y 偏移：cube sprite 中心 → 地砖菱形中心的视觉补偿。
	/// 默认 0；若发现玩家"半截埋进 cube" 或 "悬浮在 tile 上方"，调这个数。
	/// 常见取值范围 0~80。
	/// </summary>
	[Export]
	public int FootYOffset { get; set; }

	/// <summary>从 SectCompound 进入时玩家的起始 tile（房间逻辑坐标，0-indexed）。</summary>
	[Export]
	public Vector2I DefaultEntryTile { get; set; } = new(6, 6);

	[Export]
	public string WalkableMapPath { get; set; } = "res://assets/maps/alchemy_room/iso_room/walkable.txt";

	/// <summary>
	/// 调试可视化：在每个 tile 中心绘制半透明菱形（绿=可走 / 红=障碍）。
	/// 运行期按 F1 切换。
	/// </summary>
	[Export]
	public bool ShowWalkableDebug { get; set; }

	/// <summary>
	/// 场景 BGM 资源 ID（不含路径和扩展名）。留空表示不切换 BGM；
	/// 填入 "SILENCE" 表示刻意留白（无 BGM，仅有环境音）。
	/// </summary>
	[Export]
	public string SceneBgmId { get; set; } = "";

	/// <summary>
	/// 地形基底环境音 ID（如 "ambient_bamboo_wind"、"ambient_rain"）。留空表示不切换。
	/// </summary>
	[Export]
	public string TerrainAmbientId { get; set; } = "";

	// ============================================================

	private PlayerCharacterController _player = null!;
	private InteractionController _ic = null!;
	private WalkableGridDebugOverlay? _walkableOverlay;
	private bool[,] _walkable = new bool[MapHeight, MapWidth];

	public override void _Ready()
	{
		_player = GetNode<PlayerCharacterController>("Player");
		_ic = GetNode<InteractionController>("InteractionController");

		LoadWalkableGrid();

		BuildCollisionBodies();

		var entryMarker = SceneTransitionManager.PendingEntryMarker ?? "entry_from_compound";
		SceneTransitionManager.ClearPendingEntry();

		var entryTile = ResolveSafeEntryTile(DefaultEntryTile);
		_player.ConfigureTileMovement(
			entryTile,
			tile => TileToScreen(tile.X, tile.Y),
			IsWalkable,
			ScreenToTile);
		PositionInteractAreas();
		GD.Print($"[炼丹房] _Ready: spawn at entry_marker='{entryMarker}' tile=({entryTile.X},{entryTile.Y}) pos={_player.Position}");

		RegisterAllInteractions();

		BuildWalkableOverlay();

		BuildWalls();

		if (!string.IsNullOrEmpty(SceneBgmId) || !string.IsNullOrEmpty(TerrainAmbientId))
		{
			var audioDir = GetNodeOrNull<AudioDirector>("/root/AudioDirector");
			audioDir?.ApplySceneAudio(SceneBgmId, TerrainAmbientId);
		}

		GetNodeOrNull<SceneTransitionManager>("/root/SceneTransition")?.FadeIn();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F1 })
		{
			ShowWalkableDebug = !ShowWalkableDebug;
			if (_walkableOverlay != null)
			{
				_walkableOverlay.Visible = ShowWalkableDebug;
				GD.Print($"[炼丹房] walkable debug overlay = {ShowWalkableDebug}");
			}
			GetViewport().SetInputAsHandled();
		}
	}

	// ============================================================
	// Walkable grid
	// ============================================================

	private void LoadWalkableGrid()
	{
		_walkable = new bool[MapHeight, MapWidth];
		using var file = FileAccess.Open(WalkableMapPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"[炼丹房] LoadWalkableGrid: 打不开 {WalkableMapPath}，默认全可走。");
			for (var r = 0; r < MapHeight; r++)
			for (var c = 0; c < MapWidth; c++)
				_walkable[r, c] = true;
			return;
		}

		var rowIndex = 0;
		while (!file.EofReached() && rowIndex < MapHeight)
		{
			var line = file.GetLine().Trim();
			if (line.Length == 0 || line.StartsWith("#"))
			{
				continue;
			}
			if (line.Length < MapWidth)
			{
				GD.PrintErr($"[炼丹房] LoadWalkableGrid: 第 {rowIndex} 行长度 {line.Length} < {MapWidth}，按 0 补齐。");
			}
			for (var c = 0; c < MapWidth; c++)
			{
				_walkable[rowIndex, c] = c < line.Length && line[c] == '1';
			}
			rowIndex++;
		}

		if (rowIndex < MapHeight)
		{
			GD.PrintErr($"[炼丹房] LoadWalkableGrid: 只读到 {rowIndex} 行，缺 {MapHeight - rowIndex} 行，剩余视为不可走。");
		}
	}

	private bool IsWalkable(Vector2I tile) =>
		tile.X >= 0 && tile.X < MapWidth &&
		tile.Y >= 0 && tile.Y < MapHeight &&
		_walkable[tile.Y, tile.X];

	private Vector2I ResolveSafeEntryTile(Vector2I preferred)
	{
		if (IsWalkable(preferred))
		{
			return preferred;
		}
		GD.PrintErr($"[炼丹房] DefaultEntryTile ({preferred.X},{preferred.Y}) 不可走，搜索最近可走 tile。");
		// 螺旋向外搜，最远扫到房间最大维度
		for (var radius = 1; radius < Mathf.Max(MapWidth, MapHeight); radius++)
		{
			for (var dy = -radius; dy <= radius; dy++)
			for (var dx = -radius; dx <= radius; dx++)
			{
				if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
				{
					continue;
				}
				var t = new Vector2I(preferred.X + dx, preferred.Y + dy);
				if (IsWalkable(t))
				{
					return t;
				}
			}
		}
		GD.PrintErr("[炼丹房] 找不到任何可走 tile，使用 (0,0) 兜底。");
		return Vector2I.Zero;
	}

	// ============================================================
	// 坐标变换
	// ============================================================

	private Vector2 TileToScreen(int col, int row)
	{
		var canvasCol = col + ColOffset;
		var canvasRow = row + RowOffset;
		return IsoOrigin + new Vector2(
			(canvasCol - canvasRow) * TileHalfWidth,
			(canvasCol + canvasRow) * TileHalfHeight + FootYOffset);
	}

	private Vector2I ScreenToTile(Vector2 screenPos)
	{
		var ox = screenPos.X - IsoOrigin.X;
		var oy = screenPos.Y - IsoOrigin.Y - FootYOffset;
		var oxp = ox / TileHalfWidth - (ColOffset - RowOffset);
		var oyp = oy / TileHalfHeight - (ColOffset + RowOffset);
		var c = Mathf.RoundToInt((oxp + oyp) * 0.5f);
		var r = Mathf.RoundToInt((oyp - oxp) * 0.5f);
		return new Vector2I(c, r);
	}

	// ============================================================
	// 物理碰撞体构建
	// - 外墙边界由代码生成（tile 级，安全兜底）
	// - 家具（书架/丹炉/药柜/宝箱）请在 Godot 编辑器中手动摆放 StaticBody2D + CollisionPolygon2D
	//   节点到场景根节点下即可，CollisionLayer=1, CollisionMask=0，多边形形状按精灵图轮廓调整
	// ============================================================

	private void BuildCollisionBodies()
	{
		var root = new Node2D { Name = "CollisionBodies" };
		AddChild(root);

		var wallPoly = new[]
		{
			new Vector2(-64f, 0f), new Vector2(0f, -32f),
			new Vector2(64f, 0f), new Vector2(0f, 32f),
		};

		void AddWall(int col, int row)
		{
			var center = TileToScreen(col, row);
			var body = new StaticBody2D
			{
				Name = $"Wall_{col}_{row}",
				Position = center,
				CollisionLayer = 1,
				CollisionMask = 0,
			};
			body.AddChild(new CollisionPolygon2D { Polygon = wallPoly, Position = Vector2.Zero });
			root.AddChild(body);
		}

		for (var c = 0; c < MapWidth; c++) AddWall(c, 0);
		for (var r = 0; r < MapHeight; r++) AddWall(0, r);
		for (var c = 9; c < MapWidth; c++) AddWall(c, 6);
	}

	// ============================================================
	// 交互区域位置与碰撞体修正
	// ============================================================

	private void PositionInteractAreas()
	{
		SetAreaPosition("ExitArea", 6, 6, new Vector2(0f, 10f), 48f, 24f);
		SetAreaPosition("PillFurnaceArea", 4, 3, new Vector2(0f, -12f), 40f, 20f);
		SetAreaPosition("HerbCabinetArea", 8, 3, new Vector2(0f, -14f), 32f, 18f);
		SetAreaPosition("RecipeShelfArea", 8, 5, new Vector2(0f, -14f), 32f, 18f);
		SetAreaPosition("TreasureChestArea", 10, 3, new Vector2(0f, -4f), 28f, 16f);
	}

	private void SetAreaPosition(string areaName, int col, int row, Vector2 polyOffset, float halfW, float halfH)
	{
		var area = GetNodeOrNull<Area2D>(areaName);
		if (area == null) return;
		area.Position = TileToScreen(col, row);
		var cp = area.GetNodeOrNull<CollisionPolygon2D>("CollisionPolygon2D");
		if (cp == null) return;
		cp.Position = polyOffset;
		cp.Polygon = new[]
		{
			new Vector2(-halfW, 0f),
			new Vector2(0f, -halfH),
			new Vector2(halfW, 0f),
			new Vector2(0f, halfH),
		};
	}

	// ============================================================
	// 交互注册（与之前一致）
	// ============================================================

	private void RegisterAllInteractions()
	{
		_ic.RegisterZone("ExitArea", "E / 空格 离开炼丹房", ExitToCompound, supportsOutline: false);
		_ic.RegisterZone("PillFurnaceArea", "E / 空格 调查丹炉", OpenAlchemyInterface);

		_ic.RegisterOneShotZone(
			id: "alchemy_room.herb_cabinet",
			areaNodeName: "HerbCabinetArea",
			prompt: "E / 空格 取用药材柜",
			claimedEffect: ClaimedEffect.FadeOut,
			claimMessage:
				"打开药材柜，抽屉里整齐摆放着师门近月备下的药材。\n\n" +
				"[一次性获得 · v0 stub]\n" +
				"  · 甘草 ×3   · 当归 ×2   · 三七 ×1\n" +
				"  · 黄连 ×2   · 茯苓 ×1   · 金银花 ×2\n\n" +
				"（待接入：InventoryService.AddBatch；草药 id 见 design/gdd/item-system.md）\n\n" +
				"按 E / 空格 关闭。");

		_ic.RegisterOneShotZone(
			id: "alchemy_room.recipe_shelf",
			areaNodeName: "RecipeShelfArea",
			prompt: "E / 空格 翻阅药书架",
			claimedEffect: ClaimedEffect.None,
			claimMessage:
				"最上层抽出几本旧册，纸张泛黄但字迹清晰，正是师门常用的入门方子。\n\n" +
				"[一次性解锁 · v0 stub]\n" +
				"  · 清心丹（甘草 + 黄连 + 茯苓）\n" +
				"  · 续气散（当归 + 黄芪 + 人参）\n" +
				"  · 定神丸（茯苓 + 酸枣仁 + 远志）\n\n" +
				"（待接入：RecipeService.Unlock；配方 id 见 design/gdd/item-system.md §C CR-4）\n\n" +
				"按 E / 空格 关闭。");

		_ic.RegisterOneShotZone(
			id: "alchemy_room.treasure_chest",
			areaNodeName: "TreasureChestArea",
			prompt: "E / 空格 打开宝箱",
			claimedEffect: ClaimedEffect.FadeOut,
			claimMessage:
				"宝箱铜锁未扣，内里铺着褪色红绒，似是师门祭祀场合才用的物事。\n\n" +
				"[一次性获得 · v0 stub]\n" +
				"  · 银两 ×50\n" +
				"  · 上品丹瓶 ×1\n" +
				"  · 师门信物·风止印 ×1\n\n" +
				"（待接入：InventoryService.Add + KeyItemService.Set('feng_zhi_seal')）\n\n" +
				"按 E / 空格 关闭。");
	}

	// ============================================================
	// 调试可视化层
	// ============================================================

	private void BuildWalkableOverlay()
	{
		var cells = new List<(Vector2I tile, Vector2 center, bool walkable)>();
		for (var r = 0; r < MapHeight; r++)
		for (var c = 0; c < MapWidth; c++)
		{
			cells.Add((new Vector2I(c, r), TileToScreen(c, r), _walkable[r, c]));
		}

		_walkableOverlay = new WalkableGridDebugOverlay(cells, TileHalfWidth, TileHalfHeight)
		{
			Name = "WalkableDebugOverlay",
			ZIndex = 4096,
			Visible = ShowWalkableDebug,
		};
		AddChild(_walkableOverlay);
	}

	// ============================================================
	// 墙面构建（最小验证 spike）
	// ============================================================

	private void BuildWalls()
	{
		var wallsPath = "res://assets/maps/alchemy_room/iso_room/walls.txt";
		var wallBuilder = new IsoWallBuilder
		{
			Name = "IsoWallBuilder",
			TileToScreen = TileToScreen,
		};
		AddChild(wallBuilder);
		wallBuilder.BuildWalls(wallsPath);
	}

	// ============================================================
	// 场景特有的交互回调
	// ============================================================

	private void OpenAlchemyInterface()
	{
		_ic.ShowMessage(
			"丹炉炉膛微温，残留药香。\n\n" +
			"[炼丹系统]\n" +
			"  · 已实现：CraftingService（草药 → 丹药，品质极/上/中/下）\n" +
			"  · 待接入：UI（配方选择 / 草药挑选 / 进度条 / 产出展示）\n" +
			"  · 参考：design/gdd/item-system.md §C CR-4\n\n" +
			"按 E / 空格 关闭。");
	}

	private void ExitToCompound()
	{
		GD.Print("[炼丹房] Triggering exit -> SectCompound (entry_from_alchemy_room).");
		_player.MovementFrozen = true;
		var transition = GetNode<SceneTransitionManager>("/root/SceneTransition");
		transition.TransitionTo(
			"res://scenes/sect_compound/SectCompound.tscn",
			"entry_from_alchemy_room");
	}
}

/// <summary>
/// alchemy_room 的可走区域调试叠加层：在每个 tile 中心画半透明菱形。
/// 绿=可走，红=障碍，黄边=高亮。
/// </summary>
internal sealed partial class WalkableGridDebugOverlay : Node2D
{
	private readonly List<(Vector2I tile, Vector2 center, bool walkable)> _cells;
	private readonly float _halfWidth;
	private readonly float _halfHeight;

	public WalkableGridDebugOverlay(
		List<(Vector2I tile, Vector2 center, bool walkable)> cells,
		float halfWidth,
		float halfHeight)
	{
		_cells = cells;
		_halfWidth = halfWidth;
		_halfHeight = halfHeight;
	}

	public override void _Draw()
	{
		var diamond = new Vector2[]
		{
			new(-_halfWidth, 0f),
			new(0f, -_halfHeight),
			new(_halfWidth, 0f),
			new(0f, _halfHeight),
		};
		var greenFill = new Color(0.3f, 0.9f, 0.3f, 0.25f);
		var redFill = new Color(0.9f, 0.3f, 0.3f, 0.35f);
		var edge = new Color(1f, 0.85f, 0.2f, 0.6f);

		foreach (var (_, center, walkable) in _cells)
		{
			var pts = new Vector2[4];
			for (var i = 0; i < 4; i++)
			{
				pts[i] = center + diamond[i];
			}
			DrawColoredPolygon(pts, walkable ? greenFill : redFill);
			DrawPolyline(new[] { pts[0], pts[1], pts[2], pts[3], pts[0] }, edge, 1f, true);
		}
	}
}
