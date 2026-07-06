using System.Collections.Generic;
using FengZhi.Foundation.Exploration;
using FengZhi.Scripts.Exploration;
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
		["storage_shelf"] = new Vector2I(24, 5),
		["wine_jars_group"] = new Vector2I(20, 8),
		["wine_jar_single"] = new Vector2I(21, 8),
		["oil_lamp_dim"] = new Vector2I(17, 12),
		["sister_mark"] = new Vector2I(19, 15),
		["small_stool"] = new Vector2I(20, 17),
		["rest_mat"] = new Vector2I(22, 18),
	};

	private bool _hasBirthdayWine;

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override string GetInitialVariant()
		=> HasQuestFlag("prologue_wine_obtained") ||
			HasQuestFlag("prologue_cave_wall_memory_seen") ||
			HasQuestFlag("prologue_cave_rain_trapped") ||
			HasQuestFlag("prologue_cave_overnight")
			? "night"
			: "day";

	protected override float GetStructureScale(string name)
		=> _structureScales.TryGetValue(name, out var s) ? s : 0.25f;

	protected override Vector2I? GetStructureTileOverride(string name)
		=> _structureTileOverrides.TryGetValue(name, out var v) ? v : null;

	protected override void OnReady()
	{
		UpdateInventoryLabel();
		RegisterChapter00InsightNodes();
		if (HasQuestFlag("prologue_wine_obtained") &&
			!HasQuestFlag("prologue_cave_wall_memory_seen"))
			GetTree().CreateTimer(0.05).Timeout += StartCaveWallMemoryIfNeeded;

		if (HasQuestFlag("prologue_cave_wall_memory_seen") &&
			!HasQuestFlag("prologue_cave_rain_trapped") &&
			!HasQuestFlag("prologue_cave_overnight"))
			GetTree().CreateTimer(0.05).Timeout += SwitchToNightAfterWallMemoryIfNeeded;
	}

	private void RegisterChapter00InsightNodes()
	{
		var bridge = GetNodeOrNull<InsightDetectorBridge>("InsightDetectorBridge");
		if (bridge == null) return;

		RegisterInsightNode(
			bridge,
			id: "cave_wall_technique_sketch",
			tile: new Vector2I(25, 5),
			threshold: 5,
			type: DiscoveryType.EnvironmentDetail,
			reward: new DiscoveryReward(),
			narrative: "石壁上有几道浅浅的招式刻画，起手歪得厉害。师姐那时拿树枝教你，说风止尺法先要把心放平。");

		RegisterInsightNode(
			bridge,
			id: "cave_small_stool_memory",
			tile: new Vector2I(24, 6),
			threshold: 5,
			type: DiscoveryType.EnvironmentDetail,
			reward: new DiscoveryReward(),
			narrative: "小木凳腿上还留着一道旧划痕。那年你偷藏桂花糕，被师姐发现后，她坐在这里分走了最大的一块。");

		RegisterInsightNode(
			bridge,
			id: "cave_wine_stain_pattern",
			tile: new Vector2I(21, 9),
			threshold: 5,
			type: DiscoveryType.EnvironmentDetail,
			reward: new DiscoveryReward(),
			narrative: "酒坛边的旧渍绕开一小块干净石面。三年前你和师姐在这里封坛，她按住封泥，笑你手抖。");

		RegisterInsightNode(
			bridge,
			id: "cave_medicine_pot_residue",
			tile: new Vector2I(17, 12),
			threshold: 10,
			type: DiscoveryType.EnvironmentDetail,
			reward: new DiscoveryReward(),
			narrative: "药壶里只剩一点淡淡苦香。你记得有年淋雨发热，师姐守着这只壶，嫌你喝药像赴刑。");

		ActivateCaveInsightsIfReady();
	}

	private void RegisterInsightNode(
		InsightDetectorBridge bridge,
		string id,
		Vector2I tile,
		int threshold,
		DiscoveryType type,
		DiscoveryReward reward,
		string narrative)
	{
		bridge.RegisterInsightNode(new InsightNode
		{
			Id = id,
			SceneId = "back_mountain_cliff_cave",
			Position = TileToScreen(tile.X, tile.Y),
			DetectionRadius = 120f,
			InsightThreshold = threshold,
			DiscoveryType = type,
			Reward = reward,
			NarrativeContext = narrative,
		});
	}

	protected override void OnLoadVariant(string variant)
	{
		if (HasQuestFlag("prologue_wine_obtained"))
			_hasBirthdayWine = true;

		StatusLabel.Text = variant == "night"
			? "后山崖洞・夜：邻峰半山的洞口被瀑雾遮住，酒坛与旧物都沉在暗处。"
			: "后山崖洞・日常：单线山路尽头的半山洞穴，藏酒、储物，也是你和师姐的秘密基地。";
		UpdateInventoryLabel();
		UpdatePrologueObjective();
	}

	protected override void OnQuestFlagChanged(string key, string value)
	{
		if (key == "prologue_wine_obtained")
		{
			_hasBirthdayWine = true;
			GetTree().CreateTimer(0.05).Timeout += StartCaveWallMemoryIfNeeded;
		}

		if (key == "prologue_cave_wall_memory_seen")
			GetTree().CreateTimer(0.05).Timeout += SwitchToNightAfterWallMemoryIfNeeded;

		if (key == "prologue_cave_rain_trapped" ||
			key == "prologue_cave_overnight")
			ActivateCaveInsightsIfReady();

		UpdateInventoryLabel();
		UpdatePrologueObjective();
	}

	protected override bool ShouldCreateInteractionForMarker(Marker marker)
	{
		if (marker.Type is "entry" or "blocker")
			return true;

		if (HasQuestFlag("prologue_cave_rain_trapped") ||
			HasQuestFlag("prologue_cave_overnight"))
			return true;

		return marker.Name == "wine_pickup";
	}

	protected override void OnPlayerTileChanged(Vector2I tile)
	{
		if (!HasQuestFlag("prologue_cave_wall_memory_seen") ||
			HasQuestFlag("prologue_cave_rain_trapped") ||
			HasQuestFlag("prologue_cave_overnight"))
			return;

		if (!TryGetMarkerTile("exit_to_back_mountain", out var exitTile))
			return;

		var delta = tile - exitTile;
		if (Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) > 3)
			return;

		SetQuestFlag("prologue_cave_rain_trapped");
		LoadVariant("night", repositionPlayer: false);
		ShowMessage("洞口雨声忽然砸下来，山路被水冲得发亮。你抱着寿酒站了片刻，只能承认：今夜不得不在这里过了。");
	}

	private void UpdatePrologueObjective()
	{
		SetPrologueMainObjective();
	}

	protected override void OnExitInteract(string markerName)
	{
		if (markerName == "exit_to_back_mountain" &&
			(_hasBirthdayWine || HasQuestFlag("prologue_wine_obtained")) &&
			!HasQuestFlag("prologue_cave_overnight"))
		{
			if (!HasQuestFlag("prologue_cave_rain_trapped"))
			{
				SetQuestFlag("prologue_cave_rain_trapped");
				LoadVariant("night", repositionPlayer: false);
				ShowMessage("洞外雨势骤急，雾和水把山阶吞成一片白。你犹豫片刻，还是决定先在洞里等雨小些。");
				return;
			}

			ShowMessage("暴雨如注，洞外石阶湿滑得站不稳。还是在这里凑合一晚，等天亮雨停再走。");
			return;
		}

		base.OnExitInteract(markerName);
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "wine_pickup":
				if (!HasQuestFlag("prologue_wine_delayed"))
				{
					ShowMessage("酒坛静静靠在石壁边。你想起师姐方才只是随口提醒，还没真催你出发。");
					return;
				}

				if (_hasBirthdayWine)
				{
					ShowMessage("寿酒已经取好了，回去莫要耽搁。");
					return;
				}
				_hasBirthdayWine = true;
				UpdateInventoryLabel();
				SetCurrentObjective(
					"序章 · 主线",
					"在崖洞草席处歇一夜",
					"天黑路湿，你觉得等山路安全些再回也来得及",
					flash: true);
				StartDialogue("res://assets/data/dialogues/chapter_00/wine_pickup_01.yaml");
				return;
			case "storage_shelf":
				StartDialogue("res://assets/data/dialogues/chapter_00/storage_shelf_01.yaml");
				return;
			case "memory_marker":
				StartDialogue("res://assets/data/dialogues/chapter_00/memory_marker_01.yaml");
				return;
			case "rest_spot":
				if (!HasQuestFlag("prologue_wine_obtained"))
				{
					ShowMessage("草席铺在避风处。酒还没取，你不好意思先想着偷懒。");
					return;
				}

				if (!HasQuestFlag("prologue_cave_rain_trapped"))
				{
					ShowMessage("草席安静地铺在角落。洞口雨声渐近，你还是先看看外面的路。");
					return;
				}

				if (!HasQuestFlag("prologue_cave_rest_memory_seen"))
				{
					StartDialogue("res://assets/data/dialogues/chapter_00/cave_rest_mat_memory_cg_01.yaml");
					return;
				}

				StartDialogue("res://assets/data/dialogues/chapter_00/rest_spot_01.yaml");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}

	private void StartCaveWallMemoryIfNeeded()
	{
		if (!HasQuestFlag("prologue_wine_obtained") ||
			HasQuestFlag("prologue_cave_wall_memory_seen"))
			return;

		if (DialogueManager?.IsDialogueActive == true)
		{
			GetTree().CreateTimer(0.05).Timeout += StartCaveWallMemoryIfNeeded;
			return;
		}

		StartDialogue("res://assets/data/dialogues/chapter_00/cave_wall_memory_cg_01.yaml");
	}

	private void SwitchToNightAfterWallMemoryIfNeeded()
	{
		if (!HasQuestFlag("prologue_cave_wall_memory_seen"))
			return;

		if (DialogueManager?.IsDialogueActive == true)
		{
			GetTree().CreateTimer(0.05).Timeout += SwitchToNightAfterWallMemoryIfNeeded;
			return;
		}

		if (Variant != "night")
			LoadVariant("night", repositionPlayer: false);

		SetCurrentObjective(
			"序章 · 主线",
			"去洞口看看山雨是否能停",
			"壁画里的旧影刚散，洞外天色已经沉下来了",
			flash: true);
	}

	private void ActivateCaveInsightsIfReady()
	{
		if (!HasQuestFlag("prologue_cave_rain_trapped") &&
			!HasQuestFlag("prologue_cave_overnight"))
			return;

		var bridge = GetNodeOrNull<InsightDetectorBridge>("InsightDetectorBridge");
		bridge?.ActivateScene("back_mountain_cliff_cave");
	}

	private void UpdateInventoryLabel()
	{
		InventoryLabel.Text = _hasBirthdayWine
			? "任务物品：寿酒"
			: "任务物品：未取得寿酒";
	}
}
