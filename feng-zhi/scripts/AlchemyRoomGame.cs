using FengZhi.Foundation.Exploration.Interaction;
using FengZhi.Scripts.Exploration;
using Godot;

namespace FengZhi;

/// <summary>
/// 炼丹房（门派公有丹房）—— v0 使用 iso 地图编辑器导出的整张 ISO 房间作为视觉层。
///
/// 不继承 SceneGameBase：iso 房间是预先渲染好的 Sprite2D 集合，
/// 不参与 .tmx / Structures / Collision / LogicMarkers 那套体系。
///
/// 交互机制全部由场景内的 InteractionController 子节点接管：
///   - 高亮 outline / 提示 / 一次性领取追踪 / 视觉状态持久化
///   - 详见 src/FengZhi.Foundation/Exploration/Interaction/
///
/// 加新交互只需 2 步：
///   1. .tscn 加 Area2D 节点（命名自描述，如 PillFurnaceArea）
///   2. _Ready 里 ic.RegisterZone(...) 或 ic.RegisterOneShotZone(...)
/// </summary>
public partial class AlchemyRoomGame : Node2D
{
	// 默认入口位置（对应 iso 房间内 col=15, row=13 的地砖中心估算）。
	private static readonly Vector2 DefaultEntryPosition = new(1216, 1080);

	private CavePlayer _player = null!;
	private InteractionController _ic = null!;

	public override void _Ready()
	{
		_player = GetNode<CavePlayer>("Player");
		_ic = GetNode<InteractionController>("InteractionController");

		var entryMarker = SceneTransitionManager.PendingEntryMarker ?? "entry_from_compound";
		SceneTransitionManager.ClearPendingEntry();
		_player.Position = DefaultEntryPosition;
		GD.Print($"[炼丹房] _Ready: spawn at entry_marker='{entryMarker}' pos={_player.Position}");

		RegisterAllInteractions();

		GetNodeOrNull<SceneTransitionManager>("/root/SceneTransition")?.FadeIn();
	}

	private void RegisterAllInteractions()
	{
		_ic.RegisterZone("ExitArea", "E / 空格 离开炼丹房", ExitToCompound, supportsOutline: false);
		_ic.RegisterZone("PillFurnaceArea", "E / 空格 调查丹炉", OpenAlchemyInterface);

		_ic.RegisterOneShotZone(
			id: "alchemy_room.herb_cabinet",
			areaNodeName: "HerbCabinetArea",
			prompt: "E / 空格 取用药材柜",
			claimedEffect: ClaimedEffect.FadeOut,
			claimMessage:
				"打开药材柜，抽屉里整齐摆放着师门近月备下的药材。\n\n" +
				"[一次性获得 · v0 stub]\n" +
				"  · 甘草 ×3   · 当归 ×2   · 三七 ×1\n" +
				"  · 黄连 ×2   · 茯苓 ×1   · 金银花 ×2\n\n" +
				"（待接入：InventoryService.AddBatch；草药 id 见 design/gdd/item-system.md）\n\n" +
				"按 E / 空格 关闭。");

		_ic.RegisterOneShotZone(
			id: "alchemy_room.recipe_shelf",
			areaNodeName: "RecipeShelfArea",
			prompt: "E / 空格 翻阅药书架",
			claimedEffect: ClaimedEffect.None, // 通读后无视觉变化
			claimMessage:
				"最上层抽出几本旧册，纸张泛黄但字迹清晰，正是师门常用的入门方子。\n\n" +
				"[一次性解锁 · v0 stub]\n" +
				"  · 清心丹（甘草 + 黄连 + 茯苓）\n" +
				"  · 续气散（当归 + 黄芪 + 人参）\n" +
				"  · 定神丸（茯苓 + 酸枣仁 + 远志）\n\n" +
				"（待接入：RecipeService.Unlock；配方 id 见 design/gdd/item-system.md §C CR-4）\n\n" +
				"按 E / 空格 关闭。");

		// 宝箱：等用户提供 chest_closed.png + chest_open.png 两份地块后，按以下方式切换：
		//   1. 把 RoomVisual 里宝箱位置的 sprite 纹理换成 chest_closed.png
		//   2. 把下方调用改为：
		//        claimedEffect: ClaimedEffect.FrameSwap,
		//        frameSwapTexture: GD.Load<Texture2D>("res://assets/maps/alchemy_room/props/chest_open.png"),
		//   3. 已领取存档恢复时自动应用 FrameSwap，玩家看到的就是开盖状态
		_ic.RegisterOneShotZone(
			id: "alchemy_room.treasure_chest",
			areaNodeName: "TreasureChestArea",
			prompt: "E / 空格 打开宝箱",
			claimedEffect: ClaimedEffect.FadeOut, // 占位：资产到位后换 FrameSwap
			claimMessage:
				"宝箱铜锁未扣，内里铺着褪色红绒，似是师门祭祀场合才用的物事。\n\n" +
				"[一次性获得 · v0 stub]\n" +
				"  · 银两 ×50\n" +
				"  · 上品丹瓶 ×1\n" +
				"  · 师门信物·风止印 ×1\n\n" +
				"（待接入：InventoryService.Add + KeyItemService.Set('feng_zhi_seal')）\n\n" +
				"按 E / 空格 关闭。");
	}

	// ============================================================
	// 场景特有的交互回调
	// ============================================================

	private void OpenAlchemyInterface()
	{
		// v0 stub：占位提示。未来接入 it-005 已实现的 CraftingService。
		// 真正的 UI 流程：选配方 → 选草药 → 计算品质 → 产出丹药 → 写入背包
		_ic.ShowMessage(
			"丹炉炉膛微温，残留药香。\n\n" +
			"[炼丹系统]\n" +
			"  · 已实现：CraftingService（草药 → 丹药，品质极/上/中/下）\n" +
			"  · 待接入：UI（配方选择 / 草药挑选 / 进度条 / 产出展示）\n" +
			"  · 参考：design/gdd/item-system.md §C CR-4\n\n" +
			"按 E / 空格 关闭。");
	}

	private void ExitToCompound()
	{
		GD.Print("[炼丹房] Triggering exit -> SectCompound (entry_from_alchemy_room).");
		_player.MovementFrozen = true;
		var transition = GetNode<SceneTransitionManager>("/root/SceneTransition");
		transition.TransitionTo(
			"res://scenes/sect_compound/SectCompound.tscn",
			"entry_from_alchemy_room");
	}
}
