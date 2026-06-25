using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class BackMountainCliffCaveGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/back_mountain_cliff_cave";
	protected override string DayMapPath => AssetRoot + "/maps/back_mountain_cliff_cave_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/back_mountain_cliff_cave_night.tmx";
	protected override string SceneName => "后山崖洞";
	protected override string DefaultExitMarker => "exit_to_back_mountain";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"wine_jars_group", "wine_jar_single", "storage_shelf",
		"rest_mat", "small_stool", "sister_mark", "oil_lamp_dim",
	};
	private static readonly Dictionary<string, float> _structureScales = new(System.StringComparer.Ordinal)
	{
		["wine_jars_group"] = 0.24f,
		["wine_jar_single"] = 0.26f,
		["storage_shelf"] = 0.28f,
		["rest_mat"] = 0.22f,
		["small_stool"] = 0.22f,
		["sister_mark"] = 0.20f,
		["oil_lamp_dim"] = 0.20f,
	};
	private static readonly Dictionary<string, Vector2I> _structureTileOverrides = new(System.StringComparer.Ordinal)
	{
		["storage_shelf"] = new Vector2I(6, 4),
		["wine_jars_group"] = new Vector2I(4, 7),
		["wine_jar_single"] = new Vector2I(5, 7),
		["oil_lamp_dim"] = new Vector2I(10, 8),
		["sister_mark"] = new Vector2I(13, 8),
		["small_stool"] = new Vector2I(11, 10),
		["rest_mat"] = new Vector2I(12, 11),
	};

	private bool _hasBirthdayWine;

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override float GetStructureScale(string name)
		=> _structureScales.TryGetValue(name, out var s) ? s : 0.25f;

	protected override Vector2I? GetStructureTileOverride(string name)
		=> _structureTileOverrides.TryGetValue(name, out var v) ? v : null;

	protected override void OnReady()
	{
		UpdateInventoryLabel();
	}

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "后山崖洞・夜：风声止住，酒坛与旧物都沉在暗处。"
			: "后山崖洞・日常：藏酒、储物，也是你和师姐的秘密基地。";
	}

	protected override void OnInteract(string markerName)
	{
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
				StartDialogue("res://assets/data/dialogues/chapter_00/wine_pickup_01.yaml");
				return;
			case "storage_shelf":
				StartDialogue("res://assets/data/dialogues/chapter_00/storage_shelf_01.yaml");
				return;
			case "memory_marker":
				StartDialogue("res://assets/data/dialogues/chapter_00/memory_marker_01.yaml");
				return;
			case "rest_spot":
				StartDialogue("res://assets/data/dialogues/chapter_00/rest_spot_01.yaml");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}

	private void UpdateInventoryLabel()
	{
		InventoryLabel.Text = _hasBirthdayWine
			? "任务物品：寿酒"
			: "任务物品：未取得寿酒";
	}
}
