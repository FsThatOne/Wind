using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class TrainingGroundGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/training_ground";
	protected override string DayMapPath => AssetRoot + "/maps/training_ground_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/training_ground_night.tmx";
	protected override string SceneName => "丹锻药圃";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"training_dummy", "wooden_sword", "stone_bench", "fence_post",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "丹锻药圃・夜：炉火收成暗红，药畦里只有水汽与草木气息。"
			: "丹锻药圃：丹房、锻房和药圃挨着山壁铺开，是风止山院自给自修的一角。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "training_dummy_interact":
			case "training_dummy_inspect":
				ShowMessage("这里先用旧占位物表示丹炉和锻炉。正式资源到位后，可替换成一明一暗两处炉台。");
				return;
			case "wooden_sword_pickup":
			case "wooden_sword_inspect":
				ShowMessage("长条形占位物暂作铁砧和工具架。风止不设练武场，器物更多服务修补、采药与日常。");
				return;
			case "stone_bench_rest":
			case "stone_bench_inspect":
				ShowMessage("石台旁留出一片空地，后续可以摆放药篓、火钳、矿石与晒药席。");
				return;
			case "fence_post_inspect":
				ShowMessage("围栏标出药圃边界，既防山风，也防小兽踩坏药苗。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
