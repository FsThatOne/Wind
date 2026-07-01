using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class LivingQuarterGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/living_quarter";
	protected override string DayMapPath => AssetRoot + "/maps/living_quarter_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/living_quarter_night.tmx";
	protected override string SceneName => "厨房仓房小潭";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"herb_drying_rack", "medicine_pot", "bed_mat", "herb_plant",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "厨房仓房小潭・夜：灶火已熄，小潭映着仓檐，水声在夜里格外清。"
			: "厨房仓房小潭：山院自给自足的生活核，米粮、药草、灶台和溪水都聚在这里。";
		InventoryLabel.Text = "";
		UpdatePrologueObjective();
	}

	protected override string GetInitialVariant()
		=> HasQuestFlag("prologue_cave_overnight") ? "night" : "day";

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
			case "herb_rack_inspect":
				StartDialogue("res://assets/data/dialogues/chapter_00/sister_gather_herb_01.yaml");
				return;
			case "medicine_pot_inspect":
				if (!HasQuestFlag("prologue_herb_tutorial_seen"))
				{
					ShowMessage("师姐先指了指晾架上的药草：「先认草，再碰工具。你急起来最容易伤手。」");
					return;
				}

				SetCurrentObjective(
					"序章 · 厨房仓房小潭",
					"把厨房仓房的准备情况记下",
					"灶边的小锅还留着温意，山院仍像平日一样运转",
					flash: true);
				StartDialogue("res://assets/data/dialogues/chapter_00/sister_gather_ore_01.yaml");
				return;
			case "bed_mat_rest":
			case "bed_mat_inspect":
				ShowMessage("仓房角落铺着临时草席，守夜的人偶尔会在这里歇一会儿。");
				return;
			case "herb_plant_inspect":
				ShowMessage("小潭边的草药长得很好，嫩叶上挂着水汽。这里以后可以替换成正式药圃和水岸素材。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
