using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace FengZhi.Foundation.Exploration.Interaction;

/// <summary>
/// Outline 匹配算法。与场景类型无关；调用方传入 sprite 列表 + anchor 候选集（一般已过滤地板）。
///
/// 匹配优先级：
///   1. 手动标注：任意 sprite 加入 group "outline:{Area2D节点名}"，或有 metadata/outline_group == Area2D 节点名 → 用标注列表
///   2. anchor 不存在或距 Area 中心 > anchorMaxDistance → Skipped（Area 摆在空地）
///   3. anchor 无 iso_tileset_id → 仅 anchor
///   4. anchor 有 iso_tileset_id → 仅 anchor。多 sprite 交互物必须使用手动标注，避免同类素材全图高亮。
/// </summary>
public static class OutlineMatcher
{
	private const string OutlineGroupMetaKey = "outline_group";
	private const string IsoTilesetIdKey = "iso_tileset_id";
	private const string OutlineGroupPrefix = "outline:";

	// iso 地图编辑器把一个大件家具拆成 -1/-2 两个 atlas sub-tile，base hash 一致。
	private static readonly Regex TilesetVariantSuffix =
		new(@"-[12]$", RegexOptions.Compiled);

	public enum MatchMode
	{
		Manual,
		Skipped,
		AnchorOnly,
		AutoByTilesetHash,
	}

	public readonly record struct MatchResult(
		MatchMode Mode,
		int Count,
		float AnchorDistance,
		string? BaseTilesetHash);

	/// <summary>
	/// 为单个 zone 匹配 outline targets，结果写入 zone.OutlineTargets（先清空）。
	/// </summary>
	/// <param name="zone">要匹配的 zone</param>
	/// <param name="allSprites">场景内全部 sprite（用于检索 outline_group 手动标注）</param>
	/// <param name="anchorCandidates">参与 anchor 搜索的 sprite（一般 = allSprites − 地板等）</param>
	/// <param name="anchorMaxDistance">anchor 距 Area 中心的最大允许距离</param>
	public static MatchResult Match(
		InteractZone zone,
		IReadOnlyList<Sprite2D> allSprites,
		IReadOnlyList<Sprite2D> anchorCandidates,
		float anchorMaxDistance = 200f)
	{
		zone.OutlineTargets.Clear();
		var areaName = zone.Area.Name.ToString();

		// 1. 手动标注
		List<Sprite2D>? manual = null;
		var groupName = OutlineGroupPrefix + areaName;
		foreach (var sp in allSprites)
		{
			var groupMatched = sp.IsInGroup(groupName);
			var metaMatched = sp.HasMeta(OutlineGroupMetaKey) &&
				sp.GetMeta(OutlineGroupMetaKey).AsString() == areaName;
			if (!groupMatched && !metaMatched) continue;
			manual ??= new List<Sprite2D>();
			manual.Add(sp);
		}
		if (manual != null)
		{
			zone.OutlineTargets.AddRange(manual);
			return new MatchResult(MatchMode.Manual, manual.Count, AnchorDistance: 0f, BaseTilesetHash: null);
		}

		// 2. anchor
		Sprite2D? anchor = null;
		float bestDist = float.MaxValue;
		var areaCenter = zone.Area.Position;
		foreach (var sp in anchorCandidates)
		{
			if (sp.Texture == null) continue;
			var spCenter = sp.Position + sp.Texture.GetSize() / 2f;
			var d = spCenter.DistanceTo(areaCenter);
			if (d < bestDist) { bestDist = d; anchor = sp; }
		}
		if (anchor == null || bestDist > anchorMaxDistance)
		{
			return new MatchResult(MatchMode.Skipped, 0, AnchorDistance: bestDist, BaseTilesetHash: null);
		}

		// 3. 自动模式只高亮最近的 sprite 实例。
		// 多 tile / 多 sprite 物件请用 group "outline:{AreaName}" 或 metadata/outline_group 手动圈定。
		if (!anchor.HasMeta(IsoTilesetIdKey))
		{
			zone.OutlineTargets.Add(anchor);
			return new MatchResult(MatchMode.AnchorOnly, 1, AnchorDistance: bestDist, BaseTilesetHash: null);
		}

		var anchorTsid = anchor.GetMeta(IsoTilesetIdKey).AsString();
		var baseTsid = TilesetVariantSuffix.Replace(anchorTsid, "");
		zone.OutlineTargets.Add(anchor);
		return new MatchResult(
			MatchMode.AnchorOnly,
			1,
			AnchorDistance: bestDist,
			BaseTilesetHash: baseTsid);
	}

	/// <summary>递归收集树下所有 Sprite2D。常用于在 _Ready 时获取场景视觉层的全部 sprite。</summary>
	public static void CollectSprites(Node root, List<Sprite2D> output)
	{
		foreach (var child in root.GetChildren())
		{
			if (child is Sprite2D sp) output.Add(sp);
			CollectSprites(child, output);
		}
	}

	/// <summary>
	/// 判定一个 sprite 是否为"非交互背景"（地板等）。
	/// 当 sprite 的 iso_tileset_id 包含任意一个 excluded marker 时视为非交互。
	/// </summary>
	public static bool IsExcludedByMarkers(Sprite2D sprite, IReadOnlyList<string> excludedMarkers)
	{
		if (excludedMarkers.Count == 0) return false;
		if (!sprite.HasMeta(IsoTilesetIdKey)) return false;
		var tsid = sprite.GetMeta(IsoTilesetIdKey).AsString();
		foreach (var marker in excludedMarkers)
		{
			if (tsid.Contains(marker)) return true;
		}
		return false;
	}
}
