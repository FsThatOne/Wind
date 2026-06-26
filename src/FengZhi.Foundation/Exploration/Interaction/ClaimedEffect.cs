namespace FengZhi.Foundation.Exploration.Interaction;

/// <summary>
/// 一次性交互领取后，可应用于其 outline targets 的持久视觉状态。
/// </summary>
public enum ClaimedEffect
{
	/// <summary>无视觉变化（书架：书还在，只是已通读）。</summary>
	None,

	/// <summary>压暗 + 半透明，传达"已开/已空"（药材柜等的轻量 fallback）。</summary>
	FadeOut,

	/// <summary>整组 sprite 完全隐藏（一次性拾取的草药等）。</summary>
	Hide,

	/// <summary>替换主 sprite 纹理（宝箱：closed → opened）。需在注册时传 frameSwapTexture。</summary>
	FrameSwap,
}
