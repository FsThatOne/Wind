using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class MountainGateGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/mountain_gate";
	protected override string DayMapPath => AssetRoot + "/maps/mountain_gate_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/mountain_gate_night.tmx";
	protected override string SceneName => "山门";
	protected override string DefaultExitMarker => "exit_to_back_mountain";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"gate_pillar_left", "gate_pillar_right", "gate_plaque",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "山门・夜：月下山门紧闭，石匾上「风止」二字隐约可见。"
			: "山门：风止庄正门，石匾上书「风止」二字。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "gate_plaque_inspect":
				ShowMessage("石匾上刻着「风止」二字，笔力雄浑——据说是开山祖师亲笔。");
				return;
			case "gate_pillar_inspect":
				ShowMessage("石柱古朴厚重，柱身刻满了历代弟子的名字。最下面一行是你和师兄师姐的。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
