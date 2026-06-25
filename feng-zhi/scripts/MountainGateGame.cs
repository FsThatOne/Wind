using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class MountainGateGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/mountain_gate";
	protected override string DayMapPath => AssetRoot + "/maps/mountain_gate_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/mountain_gate_night.tmx";
	protected override string SceneName => "雾林侧门";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"gate_pillar_left", "gate_pillar_right", "gate_plaque",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "雾林侧门・夜：林雾压低，木门后的山院灯火被遮得若有若无。"
			: "雾林侧门：这不是迎客正门，只是一条从雾林摸入凹谷的窄径。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "gate_plaque_inspect":
				ShowMessage("旧木匾藏在藤蔓后，刻着很小的「风止」二字。山庄似乎从不急着让外人看见自己。");
				return;
			case "gate_pillar_inspect":
				ShowMessage("两侧石柱更像界桩而非门面，苔痕很深，标出雾林与山院的分界。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
