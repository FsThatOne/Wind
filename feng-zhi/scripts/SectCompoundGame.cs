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

	protected override void OnReady()
	{
		CallDeferred(nameof(StartPrologueOpeningIfNeeded));
	}

	protected override string GetInitialVariant()
	{
		return HasQuestFlag("prologue_cave_overnight") ||
			HasQuestFlag("prologue_silent_return_seen") ||
			HasQuestFlag("prologue_massacre_discovered") ||
			HasQuestFlag("prologue_senior_brother_returned") ||
			HasQuestFlag("senior_brother_mis_resolved") ||
			HasQuestFlag("prologue_joint_burial_completed")
				? "night"
				: "day";
	}

	private void StartPrologueOpeningIfNeeded()
	{
		if (HasQuestFlag("prologue_opening_seen"))
			return;

		SetQuestFlag("prologue_opening_seen");
		SetQuestFlag("prologue_daily_life_started");
		SetCurrentObjective(
			"序章 · 风止山院",
			"去厨房仓房小潭找师姐",
			"寿宴前一日，师姐说要带你学些平日不许碰的活",
			flash: true);
		StartDialogue("res://assets/data/dialogues/chapter_00/prologue_opening_01.yaml");
	}

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "风止山院・夜：旧火山口凹谷沉入雾色，岩壁庄训只剩一线暗痕。"
			: "风止山院：清修小庄藏在凹谷中央，正堂、书房、厨仓、炼丹房沿水脉疏落分布。";
		InventoryLabel.Text = "";
		UpdatePrologueObjective();
	}

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
			case "motto_axis_inspect":
				if (Variant == "night")
				{
					if (!HasQuestFlag("senior_brother_mis_resolved"))
					{
						ShowMessage("庄训前的风声低得像人在屏息。你还没能把所有话同师兄说清。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/joint_burial_01.yaml");
				}
				else
				{
					ShowMessage("正堂背后的天然岩壁是山院的精神轴线，庄训刻在石上：风过万里，止于此山。");
				}
				return;
			case "water_pond_inspect":
				if (Variant == "night")
				{
					if (!HasQuestFlag("prologue_senior_brother_returned"))
					{
						ShowMessage("小潭边只有水声。此刻你更想先确认山门外那阵脚步声。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/senior_brother_misunderstanding_01.yaml");
				}
				else
				{
					if (!HasQuestFlag("prologue_mount_foreshadowed"))
					{
						ShowMessage("小潭边的水声轻快。今日还有师姐交代的采集事没做完，厨房暂时不缺你添乱。");
						return;
					}

					if (HasQuestFlag("prologue_manor_errands_started"))
					{
						if (HasQuestFlag("prologue_manor_errands_completed"))
							ShowMessage("寿宴前的小事都帮完了。师姐大概正在雾门那边等你取酒。");
						else
							ShowMessage("寿宴准备已经接下了：厨房送药草、药房分拣药包、给师弟传话。先把三件事跑完。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/manor_errands_01.yaml");
				}
				return;
			case "mist_gate_inspect":
				if (Variant == "night")
				{
					if (!HasQuestFlag("prologue_massacre_discovered"))
					{
						ShowMessage("雾门外一片死寂。你还没弄清山院里究竟发生了什么。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/senior_brother_return_01.yaml");
				}
				else
				{
					if (!HasQuestFlag("prologue_manor_errands_completed"))
					{
						ShowMessage("雾门后的山路你熟得很，但师姐说过：寿宴前先把院里的三件事做完。");
						return;
					}

					StartDialogue("res://assets/data/dialogues/chapter_00/sister_wine_reminder_01.yaml");
				}
				return;
			default:
				ShowMessage("这里暂时没有可调查的东西。");
				return;
		}
	}

	protected override HashSet<string> GetEnabledStructures() => new(System.StringComparer.Ordinal);

	protected override bool TryHandleInteractionBeforeDefault(string markerName, Marker marker)
	{
		if (markerName != "junior_brother_npc" || Variant != "day")
			return false;

		if (!HasQuestFlag("prologue_manor_errands_started"))
			return false;

		if (HasQuestFlag("manor_errand_junior_done"))
		{
			ShowMessage("师弟已经抱着红绳跑去正堂边了，边跑边喊这次一定不会绑错。");
			return true;
		}

		SetQuestFlag("manor_errand_junior_done");
		ShowMessage("你把寿宴红绳的位置告诉师弟。他一拍脑门，拖着旧绳跑走，临走还不忘说要替你留一块最甜的糕。");
		return true;
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
