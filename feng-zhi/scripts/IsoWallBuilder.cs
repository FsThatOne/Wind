using Godot;
using System;
using System.Collections.Generic;
using FileAccess = Godot.FileAccess;

namespace FengZhi;

/// <summary>
/// iso 墙面构建器：从 walls.txt 读取墙面定义，批量生成 Sprite2D 墙段。
///
/// 墙段和家具一样是 Sprite2D，挂在当前节点下，父节点开启 y_sort_enabled 后
/// 会自动和玩家、家具按 Y 坐标排序遮挡。
///
/// 使用方式（在场景脚本 _Ready 里）：
///   var walls = new IsoWallBuilder { TileToScreen = TileToScreen };
///   AddChild(walls);
///   walls.BuildWalls("res://assets/maps/xxx/walls.txt");
///
/// walls.txt 格式（每行一条，# 开头为注释）：
///   col,row,type,facing
///   - type: wood_plain | wood_gate | corner_iron | corner_worn
///           | door_landscape | door_cloud | window_lattice
///   - facing: ew（横墙，南立面↗） | ns（纵墙，↖，原型用 flip_h 代替）| corner（L形内角）
/// </summary>
public partial class IsoWallBuilder : Node2D
{
	private static readonly Dictionary<string, string> WallTextures = new()
	{
		["wood_plain"] = "res://assets/walls/interior/wood_plain.png",
		["wood_gate"] = "res://assets/walls/interior/wood_gate.png",
		["corner_iron"] = "res://assets/walls/interior/corner_iron.png",
		["corner_worn"] = "res://assets/walls/interior/corner_worn.png",
		["door_landscape"] = "res://assets/walls/interior/door_landscape.png",
		["door_cloud"] = "res://assets/walls/interior/door_cloud.png",
		["window_lattice"] = "res://assets/walls/interior/window_lattice.png",
	};

	/// <summary>
	/// 横墙（facing=ew/ns）sprite 的视觉偏移。
	/// 墙根应对齐到地块菱形的南边缘（玩家站立的地面）。
	/// 默认值是估算初值，在编辑器里微调直到墙根与地面 cube 顶面贴合。
	/// </summary>
	[Export] public Vector2 WallVisualOffset { get; set; } = new(0f, -56f);

	/// <summary>
	/// 内角转角（facing=corner）sprite 的视觉偏移。
	/// 内角顶点应对齐到地块菱形的北顶点。
	/// </summary>
	[Export] public Vector2 CornerVisualOffset { get; set; } = new(0f, -48f);

	/// <summary>
	/// 坐标变换回调：(col, row) → 屏幕像素位置。
	/// 必须在 BuildWalls 前由场景脚本注入，通常传入场景自身的 TileToScreen 方法。
	/// </summary>
	public Func<int, int, Vector2>? TileToScreen { get; set; }

	/// <summary>
	/// 构建完成后返回所有墙段的 Sprite2D 节点，方便外部做交互（开门、高亮等）。
	/// </summary>
	public List<Sprite2D> WallSprites { get; } = new();

	public void BuildWalls(string wallsPath)
	{
		if (TileToScreen == null)
		{
			GD.PrintErr("[IsoWallBuilder] 必须先设置 TileToScreen 回调！");
			return;
		}
		var tileToScreen = TileToScreen;

		using var file = FileAccess.Open(wallsPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"[IsoWallBuilder] 打不开 {wallsPath}");
			return;
		}

		var lineIndex = 0;
		while (!file.EofReached())
		{
			var line = file.GetLine().Trim();
			lineIndex++;
			if (line.Length == 0 || line.StartsWith("#"))
			{
				continue;
			}

			var parts = line.Split(',');
			if (parts.Length < 4)
			{
				GD.PrintErr($"[IsoWallBuilder] 第{lineIndex}行格式错误（需要 col,row,type,facing）: {line}");
				continue;
			}

			if (!int.TryParse(parts[0].Trim(), out var col) ||
			    !int.TryParse(parts[1].Trim(), out var row) ||
			    !WallTextures.TryGetValue(parts[2].Trim(), out var texPath))
			{
				GD.PrintErr($"[IsoWallBuilder] 第{lineIndex}行列/行/类型无效: {line}");
				continue;
			}

			var facing = parts[3].Trim();
			CreateWallSegment(col, row, texPath, facing, tileToScreen);
		}

		GD.Print($"[IsoWallBuilder] 构建完成：共 {WallSprites.Count} 段墙。");
	}

	private void CreateWallSegment(int col, int row, string texturePath, string facing, Func<int, int, Vector2> tileToScreen)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		if (texture == null)
		{
			GD.PrintErr($"[IsoWallBuilder] 找不到贴图 {texturePath}");
			return;
		}

		var sprite = new Sprite2D
		{
			Name = $"Wall_{col}_{row}_{facing}",
			Texture = texture,
			Position = tileToScreen(col, row),
			YSortEnabled = true,
		};

		switch (facing)
		{
			case "ew":
				sprite.Offset = WallVisualOffset;
				break;

			case "ns":
				// 原型阶段：纵墙直段素材缺失，用横墙 flip_h 临时顶替
				// 正式版需要补一套↖方向的直墙美术
				sprite.Offset = WallVisualOffset;
				sprite.FlipH = true;
				GD.PushWarning($"[IsoWallBuilder] ({col},{row}) ns 朝向使用 flip_h 横墙临时顶替，顶面方向会反。");
				break;

			case "corner":
				sprite.Offset = CornerVisualOffset;
				break;

			default:
				GD.PrintErr($"[IsoWallBuilder] 未知 facing '{facing}'，按 ew 处理。");
				sprite.Offset = WallVisualOffset;
				break;
		}

		AddChild(sprite);
		WallSprites.Add(sprite);
	}
}
