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
		StatusLabel.Text = variant == "night"
			? "书房・夜：案上烛火摇曳，书架间弥漫着陈年墨香。"
			: "书房：师父的书房，古籍满架，笔墨纸砚俱全。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "desk_inspect":
				ShowMessage("案上笔墨整齐，一幅未完的字帖摊开着——「止戈为武」四字只写了前三。");
				return;
			case "bookshelf_inspect":
				ShowMessage("书架上古籍满列，从武学到医术再到棋谱诗集，无所不包。师父说「武人不可只知武」。");
				return;
			case "scroll_pile_inspect":
				if (!_booksOrganized)
				{
					_booksOrganized = true;
					ConditionProvider?.SetFlag("books_organized", "true");
					ShowMessage("你帮师父整理散落的卷轴，无意间发现书架最里层的木板似乎有些松动……");
				}
				else
				{
					ShowMessage("卷轴已经整理好了。");
				}
				return;
			case "secret_compartment_inspect":
				if (_booksOrganized)
					ShowMessage("推开松动的木板，里面是一个暗格。放着一封泛黄的书信和一块令牌——上面的字你看不懂。");
				else
					ShowMessage("满是灰尘的角落，看起来很久没人动过了。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
