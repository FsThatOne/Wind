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
		if (key.StartsWith("manor_errand_", System.StringComparison.Ordinal))
			UpdateManorErrandCompletion();

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
				if (HasQuestFlag("prologue_herb_tutorial_seen") &&
					HasQuestFlag("prologue_manor_errands_started"))
				{
					if (HasQuestFlag("manor_errand_kitchen_done"))
					{
						ShowMessage("厨房要用的药草已经送过去了，晾架上只剩明日要晒的几束。");
						return;
					}

					SetQuestFlag("manor_errand_kitchen_done");
					ShowMessage("你从晾架上取下醒酒汤要用的药草，送到灶边。厨房弟子笑着说，庄主今晚可别真被大家灌倒。");
					return;
				}

				StartDialogue("res://assets/data/dialogues/chapter_00/sister_gather_herb_01.yaml");
				return;
			case "medicine_pot_inspect":
				if (!HasQuestFlag("prologue_herb_tutorial_seen"))
				{
					ShowMessage("师姐先指了指晾架上的药草：「先认草，再碰工具。你急起来最容易伤手。」");
					return;
				}

				if (HasQuestFlag("prologue_ore_tutorial_seen") &&
					HasQuestFlag("prologue_manor_errands_started"))
				{
					if (HasQuestFlag("manor_errand_pharmacy_done"))
					{
						ShowMessage("药包已经按颜色分好，药房那边不会再把白芷和碎矿粉混在一起了。");
						return;
					}

					SetQuestFlag("manor_errand_pharmacy_done");
					ShowMessage("你按药房弟子的嘱咐把药包分成两摞：白芷归左，碎矿粉归右。苦香散开，倒真像寿宴快开席了。");
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

	private void UpdateManorErrandCompletion()
	{
		if (HasQuestFlag("prologue_manor_errands_completed"))
			return;

		if (!HasQuestFlag("manor_errand_kitchen_done") ||
			!HasQuestFlag("manor_errand_pharmacy_done") ||
			!HasQuestFlag("manor_errand_junior_done"))
			return;

		SetQuestFlag("prologue_manor_errands_completed");
		SetCurrentObjective(
			"序章 · 主线",
			"去雾门前听师姐催你取酒",
			"寿宴准备已经帮完，师姐该催你去崖洞取寿酒了",
			flash: true);
	}
}
