using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class StudyGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/study";
	protected override string DayMapPath => AssetRoot + "/maps/study_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/study_night.tmx";
	protected override string SceneName => "书房";
	protected override string DefaultExitMarker => "exit_to_main_hall";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"desk", "bookshelf", "scroll_pile", "secret_compartment",
	};

	private bool _booksOrganized;

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		if (HasQuestFlag("books_organized"))
			_booksOrganized = true;

		StatusLabel.Text = variant == "night"
			? "书房・夜：案上烛火摇曳，书架间弥漫着陈年墨香。"
			: "书房：师父的书房，古籍满架，笔墨纸砚俱全。";
		InventoryLabel.Text = "";
		SetCurrentObjective(
			"序章 · 书房",
			_booksOrganized ? "查看书架深处的松动木板" : "整理散落卷轴，看看书房是否藏着线索",
			_booksOrganized ? "卷轴后露出了不寻常的暗格痕迹" : "师父的书房向来不只放书");
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "desk_inspect":
				if (Variant == "night")
				{
					ShowMessage("案上笔墨被碰乱了，纸页边缘沾着灰。这里像是被人急急翻过。");
				}
				else
				{
					ShowMessage("案上笔墨整齐，一幅未完的字帖摊开着——「止戈为武」四字只写了前三。");
				}
				return;
			case "bookshelf_inspect":
				ShowMessage("书架上古籍满列，从武学到医术再到棋谱诗集，无所不包。师父说「武人不可只知武」。");
				return;
			case "scroll_pile_inspect":
				if (!_booksOrganized)
				{
					_booksOrganized = true;
					SetQuestFlag("books_organized");
					SetCurrentObjective(
						"序章 · 书房",
						"查看书架深处的松动木板",
						"卷轴后露出了不寻常的暗格痕迹",
						flash: true);
					ShowMessage("你帮师父整理散落的卷轴，无意间发现书架最里层的木板似乎有些松动……");
				}
				else
				{
					ShowMessage("卷轴已经整理好了。");
				}
				return;
			case "secret_compartment_inspect":
				if (_booksOrganized)
				{
					SetCurrentObjective(
						"序章 · 书房",
						"记住暗格已经空了，再回正堂整理线索",
						"这里曾经放着什么，你现在还不知道",
						flash: true);
					if (Variant == "night")
					{
						SetQuestFlag("prologue_study_compartment_empty_seen");
						ShowMessage("推开松动的木板，暗格里空空如也，只剩木屑和被擦乱的灰。");
					}
					else
					{
						StartDialogue("res://assets/data/dialogues/chapter_00/master_study_01.yaml");
					}
				}
				else
				{
					ShowMessage("满是灰尘的角落，看起来很久没人动过了。");
				}
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
