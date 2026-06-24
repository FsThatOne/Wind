using Godot;
using System.Collections.Generic;

namespace FengZhi;

/// <summary>
/// 江南水乡 tileset 平铺验证场景。
/// 运行时动态铺设 tiles，验证：
///   1) 青石/泥地/草苔大面积平铺无缝
///   2) 单 tile 深水闭合水池（1深+4浅+8岸）菱形闭合
///   3) 2x2 深水水池（菱形更大水面）闭合
///   4) 苔藓小岛（四面环水）
///   5) 泥地-草苔-青石材质过渡
/// 按 ESC 退出，按 R 重置视角，按 WASD/方向键移动相机。
/// </summary>
public partial class JiangnanTilesetTester : Node2D
{
	private const int TileW = 64;
	private const int TileH = 32;

	private static readonly Vector2I Bluestone      = new(0, 0);
	private static readonly Vector2I WetMud         = new(1, 0);
	private static readonly Vector2I MossGround     = new(2, 0);
	private static readonly Vector2I BluestoneWorn  = new(3, 0);
	private static readonly Vector2I ShallowWater   = new(0, 1);
	private static readonly Vector2I DeepWater      = new(1, 1);
	private static readonly Vector2I ShoreN         = new(2, 1);
	private static readonly Vector2I ShoreE         = new(3, 1);
	private static readonly Vector2I ShoreS         = new(0, 2);
	private static readonly Vector2I ShoreW         = new(1, 2);
	private static readonly Vector2I CornerInnerNE  = new(2, 2);
	private static readonly Vector2I CornerInnerSE  = new(3, 2);
	private static readonly Vector2I CornerInnerSW  = new(0, 3);
	private static readonly Vector2I CornerInnerNW  = new(1, 3);
	private static readonly Vector2I WetMudPuddles  = new(2, 3);
	private static readonly Vector2I MossyIslet     = new(3, 3);

	private const int SourceId = 0;

	private TileMapLayer _tileMap = null!;
	private Camera2D _camera = null!;
	private Label _infoLabel = null!;

	private Vector2 _cameraDragStart;
	private bool _dragging;

	public override void _Ready()
	{
		_tileMap = GetNode<TileMapLayer>("TileMap");
		_camera = GetNode<Camera2D>("Camera2D");
		_infoLabel = GetNode<Label>("UiLayer/InfoLabel");

		BuildTestMap();
		CenterCameraOnMap();
		UpdateInfoLabel();
	}

	public override void _Process(double delta)
	{
		float panSpeed = 400f * (float)delta;
		Vector2 move = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))    move += new Vector2(-panSpeed, -panSpeed) * 0.707f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))  move += new Vector2( panSpeed,  panSpeed) * 0.707f;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))  move += new Vector2(-panSpeed,  panSpeed) * 0.707f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) move += new Vector2( panSpeed, -panSpeed) * 0.707f;
		_camera.GlobalPosition += move;

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			GetTree().Quit();
		}
		if (Input.IsKeyPressed(Key.R))
		{
			CenterCameraOnMap();
		}
	}

	private void BuildTestMap()
	{
		_tileMap.Clear();

		int areaGap = 6;

		int ox = 0, oy = 0;
		PaintBluestoneField(ox, oy, 10, 8);
		PaintGroundPatches(ox + 12, oy, 6, 6);

		ox = 0; oy = areaGap;
		PaintSmallPond(ox, oy);

		ox = 14; oy = areaGap - 1;
		PaintLargePond(ox, oy);

		ox = 0; oy = areaGap * 2;
		PaintIsland(ox, oy);

		ox = 12; oy = areaGap * 2 - 1;
		PaintRiverBand(ox, oy, 14);

		ox = 0; oy = areaGap * 3;
		PaintMixedGround(ox, oy, 10, 6);
	}

	private void PaintBluestoneField(int cx, int cy, int w, int h)
	{
		for (int x = cx - w / 2; x < cx + w / 2; x++)
		{
			for (int y = cy - h / 2; y < cy + h / 2; y++)
			{
				_tileMap.SetCell(new Vector2I(x, y), SourceId, Bluestone);
			}
		}
		_tileMap.SetCell(new Vector2I(cx - 1, cy - 1), SourceId, BluestoneWorn);
		_tileMap.SetCell(new Vector2I(cx + 1, cy + 1), SourceId, BluestoneWorn);
	}

	private void PaintGroundPatches(int cx, int cy, int w, int h)
	{
		for (int x = cx - w / 2; x < cx + w / 2; x++)
		{
			for (int y = cy - h / 2; y < cy + h / 2; y++)
			{
				int d = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
				Vector2I atlas;
				if (d <= 1) atlas = MossGround;
				else if (d <= 3) atlas = WetMud;
				else atlas = Bluestone;
				_tileMap.SetCell(new Vector2I(x, y), SourceId, atlas);
			}
		}
	}

	private void PaintSmallPond(int cx, int cy)
	{
		FillDiamond(cx - 4, cy - 4, 9, 9, Bluestone);

		_tileMap.SetCell(new Vector2I(cx, cy), SourceId, DeepWater);
		_tileMap.SetCell(new Vector2I(cx, cy - 1), SourceId, ShallowWater);
		_tileMap.SetCell(new Vector2I(cx + 1, cy), SourceId, ShallowWater);
		_tileMap.SetCell(new Vector2I(cx, cy + 1), SourceId, ShallowWater);
		_tileMap.SetCell(new Vector2I(cx - 1, cy), SourceId, ShallowWater);

		PlaceShoreTile(cx, cy - 2, cx, cy - 1);
		PlaceShoreTile(cx + 2, cy, cx + 1, cy);
		PlaceShoreTile(cx, cy + 2, cx, cy + 1);
		PlaceShoreTile(cx - 2, cy, cx - 1, cy);

		PlaceCornerTile(new(cx + 1, cy - 1), new(cx, cy - 1), new(cx + 1, cy));
		PlaceCornerTile(new(cx + 1, cy + 1), new(cx + 1, cy), new(cx, cy + 1));
		PlaceCornerTile(new(cx - 1, cy + 1), new(cx, cy + 1), new(cx - 1, cy));
		PlaceCornerTile(new(cx - 1, cy - 1), new(cx - 1, cy), new(cx, cy - 1));
	}

	private void PaintLargePond(int cx, int cy)
	{
		FillDiamond(cx - 5, cy - 5, 11, 11, Bluestone);

		_tileMap.SetCell(new Vector2I(cx, cy), SourceId, DeepWater);
		_tileMap.SetCell(new Vector2I(cx, cy - 1), SourceId, DeepWater);
		_tileMap.SetCell(new Vector2I(cx + 1, cy), SourceId, DeepWater);
		_tileMap.SetCell(new Vector2I(cx, cy + 1), SourceId, DeepWater);
		_tileMap.SetCell(new Vector2I(cx - 1, cy), SourceId, DeepWater);

		Vector2I[] shallowRing = {
			new(cx, cy - 2), new(cx + 1, cy - 1), new(cx + 2, cy),
			new(cx + 1, cy + 1), new(cx, cy + 2), new(cx - 1, cy + 1),
			new(cx - 2, cy), new(cx - 1, cy - 1),
		};
		foreach (var p in shallowRing)
			_tileMap.SetCell(p, SourceId, ShallowWater);

		PlaceShoreTile(cx, cy - 3, cx, cy - 2);
		PlaceShoreTile(cx + 3, cy, cx + 2, cy);
		PlaceShoreTile(cx, cy + 3, cx, cy + 2);
		PlaceShoreTile(cx - 3, cy, cx - 2, cy);

		PlaceCornerTile(new(cx + 2, cy - 1), new(cx + 1, cy - 1), new(cx + 2, cy));
		PlaceCornerTile(new(cx + 2, cy + 1), new(cx + 2, cy), new(cx + 1, cy + 1));
		PlaceCornerTile(new(cx - 1, cy + 2), new(cx, cy + 2), new(cx - 1, cy + 1));
		PlaceCornerTile(new(cx - 2, cy + 1), new(cx - 1, cy + 1), new(cx - 2, cy));
		PlaceCornerTile(new(cx - 2, cy - 1), new(cx - 2, cy), new(cx - 1, cy - 1));
		PlaceCornerTile(new(cx - 1, cy - 2), new(cx - 1, cy - 1), new(cx, cy - 2));

		PlaceStraightShoreBetween(new(cx + 1, cy - 2), new(cx, cy - 2), new(cx + 1, cy - 1));
		PlaceStraightShoreBetween(new(cx + 2, cy - 1), new(cx + 1, cy - 1), new(cx + 2, cy));
		PlaceStraightShoreBetween(new(cx + 2, cy + 1), new(cx + 2, cy), new(cx + 1, cy + 1));
		PlaceStraightShoreBetween(new(cx + 1, cy + 2), new(cx + 1, cy + 1), new(cx, cy + 2));
		PlaceStraightShoreBetween(new(cx - 1, cy + 2), new(cx, cy + 2), new(cx - 1, cy + 1));
		PlaceStraightShoreBetween(new(cx - 2, cy + 1), new(cx - 1, cy + 1), new(cx - 2, cy));
		PlaceStraightShoreBetween(new(cx - 2, cy - 1), new(cx - 2, cy), new(cx - 1, cy - 1));
		PlaceStraightShoreBetween(new(cx - 1, cy - 2), new(cx - 1, cy - 1), new(cx, cy - 2));
	}

	private void PaintIsland(int cx, int cy)
	{
		FillDiamond(cx - 5, cy - 5, 11, 11, Bluestone);

		for (int dx = -4; dx <= 4; dx++)
			for (int dy = -4; dy <= 4; dy++)
				if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 4)
					_tileMap.SetCell(new Vector2I(cx + dx, cy + dy), SourceId, DeepWater);

		for (int dx = -3; dx <= 3; dx++)
			for (int dy = -3; dy <= 3; dy++)
				if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 3)
					_tileMap.SetCell(new Vector2I(cx + dx, cy + dy), SourceId, ShallowWater);

		_tileMap.SetCell(new Vector2I(cx, cy), SourceId, MossyIslet);
		_tileMap.SetCell(new Vector2I(cx, cy - 1), SourceId, WetMudPuddles);
		_tileMap.SetCell(new Vector2I(cx + 1, cy), SourceId, WetMudPuddles);
		_tileMap.SetCell(new Vector2I(cx, cy + 1), SourceId, WetMudPuddles);
		_tileMap.SetCell(new Vector2I(cx - 1, cy), SourceId, WetMudPuddles);
	}

	private void PaintRiverBand(int cx, int cy, int length)
	{
		int hw = length / 2;

		for (int i = -hw - 1; i <= hw + 1; i++)
		{
			_tileMap.SetCell(new Vector2I(cx + i, cy + i), SourceId, Bluestone);
		}

		for (int i = -hw; i <= hw; i++)
		{
			_tileMap.SetCell(new Vector2I(cx + i, cy + i - 1), SourceId, DeepWater);
			_tileMap.SetCell(new Vector2I(cx + i + 1, cy + i), SourceId, ShallowWater);
		}

		for (int i = -hw; i <= hw; i++)
		{
			PlaceShoreTile(new(cx + i, cy + i - 2), new(cx + i, cy + i - 1));
			PlaceShoreTile(new(cx + i + 2, cy + i), new(cx + i + 1, cy + i));
			PlaceShoreTile(new(cx + i, cy + i), new(cx + i, cy + i - 1));
		}
	}

	private void PaintMixedGround(int cx, int cy, int w, int h)
	{
		for (int x = cx - w / 2; x < cx + w / 2; x++)
		{
			for (int y = cy - h / 2; y < cy + h / 2; y++)
			{
				float nx = (x - cx) * 0.4f;
				float ny = (y - cy) * 0.4f;
				float n = Mathf.Sin(nx) * Mathf.Cos(ny) + Mathf.Sin(nx * 2.3f + ny * 1.7f) * 0.5f;
				Vector2I atlas;
				if (n < -0.5f) atlas = WetMudPuddles;
				else if (n < 0f) atlas = WetMud;
				else if (n < 0.6f) atlas = MossGround;
				else atlas = Bluestone;
				_tileMap.SetCell(new Vector2I(x, y), SourceId, atlas);
			}
		}
	}

	private void FillDiamond(int startX, int startY, int w, int h, Vector2I atlas)
	{
		int cx = startX + w / 2;
		int cy = startY + h / 2;
		int radius = Mathf.Min(w, h) / 2;
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
					_tileMap.SetCell(new Vector2I(cx + dx, cy + dy), SourceId, atlas);
			}
		}
	}

	private bool IsWater(Vector2I p)
	{
		var src = _tileMap.GetCellSourceId(p);
		if (src == -1) return false;
		var atlas = _tileMap.GetCellAtlasCoords(p);
		return atlas == ShallowWater || atlas == DeepWater;
	}

	private void PlaceShoreTile(Vector2I landPos, Vector2I waterNeighbor)
	{
		PlaceShoreTile(landPos.X, landPos.Y, waterNeighbor.X, waterNeighbor.Y);
	}

	private void PlaceShoreTile(int lx, int ly, int wx, int wy)
	{
		Vector2I land = new(lx, ly);
		bool waterNE = IsWater(new Vector2I(lx, ly - 1));
		bool waterSE = IsWater(new Vector2I(lx + 1, ly));
		bool waterSW = IsWater(new Vector2I(lx, ly + 1));
		bool waterNW = IsWater(new Vector2I(lx - 1, ly));

		int count = (waterNE ? 1 : 0) + (waterSE ? 1 : 0) + (waterSW ? 1 : 0) + (waterNW ? 1 : 0);
		Vector2I atlas;

		if (count >= 2)
		{
			if (waterNE && waterSE) atlas = CornerInnerNE;
			else if (waterSE && waterSW) atlas = CornerInnerSE;
			else if (waterSW && waterNW) atlas = CornerInnerSW;
			else if (waterNW && waterNE) atlas = CornerInnerNW;
			else if (waterNE && waterSW) atlas = ShoreN;
			else atlas = ShoreE;
		}
		else if (waterNE) atlas = ShoreN;
		else if (waterSE) atlas = ShoreE;
		else if (waterSW) atlas = ShoreS;
		else if (waterNW) atlas = ShoreW;
		else atlas = Bluestone;

		_tileMap.SetCell(land, SourceId, atlas);
	}

	private void PlaceCornerTile(Vector2I pos, Vector2I w1, Vector2I w2)
	{
		PlaceShoreTile(pos, w1);
	}

	private void PlaceStraightShoreBetween(Vector2I pos, Vector2I w1, Vector2I w2)
	{
		PlaceShoreTile(pos, w1);
	}

	private void CenterCameraOnMap()
	{
		var usedRect = _tileMap.GetUsedRect();
		Vector2I mapCenter = usedRect.Position + usedRect.Size / 2;
		Vector2 screenCenter = _tileMap.MapToLocal(mapCenter);
		_camera.GlobalPosition = screenCenter + _tileMap.GlobalPosition;
	}

	private void UpdateInfoLabel()
	{
		if (_infoLabel != null)
		{
			_infoLabel.Text =
				"[Jiangnan Tileset Tester]\n" +
				"WASD/方向键 — 移动镜头\n" +
				"R — 重置视角    ESC — 退出\n\n" +
				"测试区域:\n" +
				"  [左上] 青石大平铺 / 材质圆斑\n" +
				"  [中上] 1-深水菱形池闭合\n" +
				"  [右上] 2x2-深水大池闭合\n" +
				"  [左下] 苔藓小岛(四面环水)\n" +
				"  [中下] 斜向河流带\n" +
				"  [右下] 噪波混合材质地面";
		}
	}
}
