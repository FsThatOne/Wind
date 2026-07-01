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
		SetCurrentObjective(
			"序章 · 正堂",
			"查看正堂与师父留下的痕迹",
			variant == "night" ? "庄训前的一切都静得不合常理" : "寿宴前的正堂仍保持着山庄日常的秩序");
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "stone_wall_inspect":
				if (Variant == "night")
				{
					StartDialogue("res://assets/data/dialogues/chapter_00/massacre_evidence_01.yaml");
				}
				else
				{
					ShowMessage("石壁上刻着庄训——风过万里，止于此山。我辈修身，非为争锋，唯养心性。");
				}
				return;
			case "weapon_rack_inspect":
				if (Variant == "night")
				{
					if (!HasQuestFlag("prologue_joint_burial_completed"))
					{
						ShowMessage("架上的旧物沉默着。师兄还在院中等你，眼下不是谈传承的时候。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/farewell_inheritance_01.yaml");
				}
				else
				{
					ShowMessage("这里先沿用旧占位架，正式版本更适合换成礼器、竹简或山院日用器物，而不是练武场式兵器陈列。");
				}
				return;
			case "tea_table_interact":
				if (Variant == "night")
				{
					if (!HasQuestFlag("prologue_joint_burial_completed"))
					{
						ShowMessage("茶盏冷透了。你还没和师兄一起把该收拾的人安顿好。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/letter_promise_01.yaml");
				}
				else
				{
					ShowMessage("茶桌上搁着半壶温茶，杯盏三两只。师父今日见谁都笑得比平日宽些。");
				}
				return;
			case "master_talk":
				StartDialogue("res://assets/data/dialogues/chapter_00/master_study_01.yaml");
				return;
			case "blood_letter_inspect":
				SetCurrentObjective(
					"序章 · 正堂",
					"记住血书上的八字，继续追查山庄变故",
					"风起渊底，鹤归无枝",
					flash: true);
				StartDialogue("res://assets/data/dialogues/chapter_00/massacre_evidence_01.yaml");
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}
}
