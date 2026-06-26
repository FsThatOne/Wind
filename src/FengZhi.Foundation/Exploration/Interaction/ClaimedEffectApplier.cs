using Godot;

namespace FengZhi.Foundation.Exploration.Interaction;

/// <summary>
/// 应用一次性交互的持久视觉状态。
/// 可被 OnInteract 当下调用（领取瞬间）；
/// 也可被 InteractionController 在启动时统一调用（存档恢复 → 已领取 zone 立即应用）。
/// </summary>
public static class ClaimedEffectApplier
{
	private static readonly Color FadeOutTint = new(0.7f, 0.65f, 0.6f, 0.55f);

	public static void Apply(InteractZone zone)
	{
		switch (zone.ClaimedEffect)
		{
			case ClaimedEffect.None:
				return;

			case ClaimedEffect.FadeOut:
				foreach (var sp in zone.OutlineTargets)
					sp.Modulate = FadeOutTint;
				break;

			case ClaimedEffect.Hide:
				foreach (var sp in zone.OutlineTargets)
					sp.Visible = false;
				break;

			case ClaimedEffect.FrameSwap:
				if (zone.FrameSwapTexture == null)
				{
					GD.PushWarning($"[Interaction] FrameSwap on '{zone.Area.Name}' 缺少 FrameSwapTexture，跳过。");
					return;
				}
				var primary = FindPrimarySprite(zone);
				if (primary == null)
				{
					GD.PushWarning($"[Interaction] FrameSwap on '{zone.Area.Name}' 找不到主 sprite（OutlineTargets 为空？）。");
					return;
				}
				primary.Texture = zone.FrameSwapTexture;
				break;
		}
	}

	/// <summary>OutlineTargets 中离 Area 中心最近的 sprite，视为该交互物的"主 sprite"。</summary>
	public static Sprite2D? FindPrimarySprite(InteractZone zone)
	{
		Sprite2D? best = null;
		float bestDist = float.MaxValue;
		var center = zone.Area.Position;
		foreach (var sp in zone.OutlineTargets)
		{
			if (sp.Texture == null) continue;
			var spCenter = sp.Position + sp.Texture.GetSize() / 2f;
			var d = spCenter.DistanceTo(center);
			if (d < bestDist) { bestDist = d; best = sp; }
		}
		return best;
	}
}
