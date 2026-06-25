using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class LivingQuarterGame : SceneGameBase
{
	protected override string AssetRoot => "res://assets/maps/living_quarter";
	protected override string DayMapPath => AssetRoot + "/maps/living_quarter_day.tmx";
	protected override string NightMapPath => AssetRoot + "/maps/living_quarter_night.tmx";
	protected override string SceneName => "住处";
	protected override string DefaultExitMarker => "exit_to_courtyard";

	private static readonly HashSet<string> _enabledStructures = new(System.StringComparer.Ordinal)
	{
		"herb_drying_rack", "medicine_pot", "bed_mat", "herb_plant",
	};

	protected override HashSet<string> GetEnabledStructures() => _enabledStructures;

	protected override void OnLoadVariant(string variant)
	{
		StatusLabel.Text = variant == "night"
			? "住处・夜：草药的清香弥漫，月光照在简朴的床铺上。"
			: "住处：弟子起居之地，师姐在此晾晒草药。";
		InventoryLabel.Text = "";
	}

	protected override void OnInteract(string markerName)
	{
		switch (markerName)
		{
			case "herb_rack_inspect":
				ShowMessage("晾药架上挂满了各色草药，是师姐精心整理的。每一束都标了名字和采摘日期。");
				return;
			case "medicine_pot_inspect":
				ShowMessage("药罐里正煎着什么，空气中弥漫着苦涩的药香。师姐说这是给师父准备的养身方。");
				return;
			case "bed_mat_inspect":
				ShowMessage("简朴的床铺，被褥叠得整齐。枕边放着一本半读的武学手札。");
				return;
			case "herb_plant_inspect":
				ShowMessage("几盆草药长得正好，嫩叶上还挂着露珠。师姐叮嘱过不要随便碰。");
				return;
			default:
				ShowMessage("这里暂时没有什么可调查的东西。");
				return;
		}
	}
}
