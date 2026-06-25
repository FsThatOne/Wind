using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class SectCompoundGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/sect_compound";
	protected override string DayMapPath => AssetRoot + "/maps/sect_compound_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/sect_compound_night.tmx";
	protected override string SceneName => "风止山院";
	protected override string DefaultExitMarker => "entry_from_mountain_gate";

	protected override Vector2 Origin { get; set; } = new(1408f, 160f);

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "风止山院・夜：旧火山口凹谷沉入雾色，岩壁庄训只剩一线暗痕。"
			: "风止山院：清修小庄藏在凹谷中央，正堂、书房、厨仓、丹锻药圃沿水脉疏落分布。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "motto_axis_inspect":
				ShowMessage("正堂背后的天然岩壁是山院的精神轴线，庄训刻在石上：风过万里，止于此山。");
				return;
			case "water_pond_inspect":
				ShowMessage("一脉细流从雾林方向入庄，在厨房与药圃之间汇成小潭。山院的日常都绕着这点水声展开。");
				return;
			case "mist_gate_inspect":
				ShowMessage("南侧不是张扬的正门，只是一道藏在雾林里的侧门。外人若不知路径，很难走到这里。");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}

	protected override HashSet<string> GetEnabledStructures() => new(System.StringComparer.Ordinal);
}
