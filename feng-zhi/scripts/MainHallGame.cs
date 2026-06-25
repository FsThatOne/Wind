using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class MainHallGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/main_hall";
	protected override string DayMapPath => AssetRoot + "/maps/main_hall_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/main_hall_night.tmx";
	protected override string SceneName => "正堂";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"stone_wall_motto", "weapon_rack", "tea_table", "chair", "blood_letter",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "正堂・夜：天然岩壁压在堂后，庄训前的一切都静了。"
			: "正堂・日常：正堂背靠天然岩壁，庄训刻着——风过万里，止于此山。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "stone_wall_inspect":
				ShowMessage("石壁上刻着庄训——风过万里，止于此山。我辈修身，非为争锋，唯养心性。");
				return;
			case "weapon_rack_inspect":
				ShowMessage("这里先沿用旧占位架，正式版本更适合换成礼器、竹简或山院日用器物，而不是练武场式兵器陈列。");
				return;
			case "tea_table_interact":
				ShowMessage("茶桌上搁着半壶冷茶，杯盏三两只。师父的座位空着。");
				return;
			case "master_talk":
				ShowMessage("庄主的位置。明日便是他六十大寿。");
				return;
			case "blood_letter_inspect":
				ShowMessage("地上散落着一封血书，字迹潦草而急切——是师父的笔迹。墨与血混在一起，最后几个字已看不清。");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}
}
