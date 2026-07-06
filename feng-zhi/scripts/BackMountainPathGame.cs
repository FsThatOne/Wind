using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class BackMountainPathGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/back_mountain_path";
	protected override string DayMapPath => AssetRoot + "/maps/back_mountain_path_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/back_mountain_path_night.tmx";
	protected override string SceneName => "雾林小径";
	protected override string DefaultExitMarker => "entry_from_compound";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"old_tree", "path_rock", "wild_grass",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override string GetInitialVariant()
		=> HasQuestFlag("prologue_cave_overnight") ||
			HasQuestFlag("prologue_silent_return_seen")
			? "night"
			: "day";

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "雾林小径・夜：主潭瀑声被雾压低，通向邻峰崖洞的山阶隐在暗处。"
			: "雾林小径：邻峰瀑布落入主潭，水雾遮住一段通往后山崖洞的长路。";
		InventoryLabel.Text = "";
		UpdatePrologueObjective();
	}

	protected override void OnQuestFlagChanged(string key, string value)
	{
		UpdatePrologueObjective();
	}

	private void UpdatePrologueObjective()
	{
		SetPrologueMainObjective();
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "old_tree_inspect":
				if (Variant == "night")
				{
					if (!HasQuestFlag("prologue_cave_overnight"))
					{
						ShowMessage("夜雾贴着石阶。你心里还惦着崖洞里的寿酒，暂时没有回山院的理由。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/massacre_return_01.yaml");
				}
				else
				{
					if (!HasQuestFlag("prologue_ore_tutorial_seen"))
					{
						ShowMessage("老松下的泥土还湿着。师姐在前头喊你：「先把手上的活学完，别又到处乱看。」");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/animal_tracks_mount_foreshadow_01.yaml");
				}
				return;
			case "path_rock_inspect":
				ShowMessage("岩石上覆着青苔，水汽从瀑布方向吹来。正式素材可替换成前景水帘与湿石。");
				return;
			case "wild_grass_inspect":
				ShowMessage("雾林野草里夹着几株可入药的草，说明这条路仍被山院的人偶尔照看。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
