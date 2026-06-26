using System;
using System.Collections.Generic;
using Godot;

namespace FengZhi.Foundation.Exploration.Interaction;

/// <summary>
/// 一个交互区域的完整状态。
/// 由 InteractionController 创建/管理：通过 OutlineMatcher 填充 OutlineTargets；
/// 通过 ClaimedEffectApplier 应用领取后视觉。
///
/// 字段直接公开（非 properties），因为这是数据载体，无封装语义。
/// </summary>
public sealed class InteractZone
{
	/// <summary>对应的 Area2D 节点（玩家进入此区域时触发）。</summary>
	public required Area2D Area;

	/// <summary>
	/// 提示文案的延迟取值器。允许根据上下文（如已领取/未领取）动态返回不同文案。
	/// </summary>
	public required Func<string> PromptProvider;

	/// <summary>玩家按交互键时触发。</summary>
	public required Action OnInteract;

	/// <summary>是否参与 outline 高亮匹配。门口/纯逻辑触发器设为 false。</summary>
	public bool SupportsOutline = true;

	/// <summary>true = 这是一个已领取一次性交互的占位 zone（area 已禁用，仅用于持有视觉状态）。</summary>
	public bool IsClaimedStub = false;

	public ClaimedEffect ClaimedEffect = ClaimedEffect.None;

	/// <summary>FrameSwap 效果需要的目标纹理；其他效果可为 null。</summary>
	public Texture2D? FrameSwapTexture = null;

	/// <summary>OutlineMatcher 填充的目标 sprite 列表（高亮 / fade / hide / frame-swap 都作用于此组）。</summary>
	public List<Sprite2D> OutlineTargets = new();
}
