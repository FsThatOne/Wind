using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class TrainingGroundGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/training_ground";
	protected override string DayMapPath => AssetRoot + "/maps/training_ground_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/training_ground_night.tmx";
	protected override string SceneName => "练武场";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"training_dummy", "wooden_sword", "stone_bench", "fence_post",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "练武场・夜：空旷的练武场上只剩月影。木人桩在风中微微摇晃。"
			: "练武场：庄内弟子日常练功之地，木人桩旁散落着几柄木剑。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "training_dummy_inspect":
				ShowMessage("木人桩上满是刀剑痕迹，看得出弟子们练得很勤。你的那一道在最低处——入门时力气还小。");
				return;
			case "wooden_sword_inspect":
				ShowMessage("师父说，兵器不过是手的延伸。可你偷偷觉得，拿着剑时确实比空手帅一些。");
				return;
			case "stone_bench_inspect":
				ShowMessage("石凳上放着一壶凉了的茶，大概是师弟练功时搁下忘了收。");
				return;
			case "fence_post_inspect":
				ShowMessage("围栏有些年头了，木桩子上长满了青苔。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
