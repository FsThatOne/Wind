using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class BackMountainPathGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/back_mountain_path";
	protected override string DayMapPath => AssetRoot + "/maps/back_mountain_path_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/back_mountain_path_night.tmx";
	protected override string SceneName => "后山小路";
	protected override string DefaultExitMarker => "exit_to_mountain_gate";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"old_tree", "path_rock", "wild_grass",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "后山小路・夜：月光透过枝叶洒落，远处传来风声。"
			: "后山小路：通往后山崖洞的蜿蜒小路，两旁野草丛生。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "old_tree_inspect":
				ShowMessage("一棵老松，枝干遒劲。师姐常说这棵树比风止庄的历史还长。");
				return;
			case "path_rock_inspect":
				ShowMessage("路旁的岩石上覆着青苔，依稀可见有人坐过的痕迹。");
				return;
			case "wild_grass_inspect":
				ShowMessage("野草丛生，其中夹杂着几株可入药的草药。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
