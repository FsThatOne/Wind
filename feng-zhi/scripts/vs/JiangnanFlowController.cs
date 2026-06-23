using System.Collections.Generic;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南 VS 流程控制器（Autoload 单例）。
///
/// 负责跨场景的 explore → battle → outcome → explore 闭环转场。
/// VS Sprint 7 Lite Xingqi 范围：1 场 1v1 战斗，不实现行气条 / 棋盘。
///
/// 装载方式：作为 Autoload 注册（Project Settings → Autoload）。
/// 也可以由 jiangnan_riverside.tscn 在 _Ready 中懒加载。
///
/// 状态机：
///   Explore  → BattleTrigger 进入  → Battle
///   Battle   → 战斗结束（胜利 / 失败）→ Outcome
///   Outcome  → dialog 收尾完成      → Explore（带 OutcomeContext，更新 NPC 反应）
///
/// 参见：
/// - docs/superpowers/specs/2026-06-23-vs-scope-spike.md §4 场景骨架
/// - production/sprints/sprint-7.md §Must Have S7-VS-Foundation-Scene
/// </summary>
public partial class JiangnanFlowController : Node
{
	public enum Phase
	{
		Boot,
		Explore,
		Battle,
		Outcome,
	}

	/// <summary>玩家在 outcome scene 做出的心境选择。</summary>
	public enum MindsetChoice
	{
		None,
		Spare,       // 放过：心境 → 仁
		Defeat,      // 重伤：心境 → 狠
	}

	[Signal]
	public delegate void PhaseChangedEventHandler(int newPhase);

	private const string JiangnanRiversideScenePath = "res://scenes/vs/jiangnan_riverside.tscn";
	private const string BattleJiangnanScenePath = "res://scenes/vs/battle_jiangnan_bandit.tscn";
	private const string OutcomeJiangnanScenePath = "res://scenes/vs/battle_outcome_jiangnan.tscn";

	public Phase CurrentPhase { get; private set; } = Phase.Boot;
	public MindsetChoice LastMindsetChoice { get; private set; } = MindsetChoice.None;
	public bool HasCompletedBattleOnce { get; private set; }

	/// <summary>上一场战斗结果（仅 outcome scene 期间有意义）。</summary>
	public BattleResult LastBattleResult { get; private set; } = BattleResult.InProgress;

	/// <summary>
	/// VS 范围内全局共享的 EventBus（mindset / battle ui 等跨场景事件总线）。
	///
	/// 由 Autoload _Ready 创建并存活整个进程；scene transition 不会清掉
	/// （Autoload 节点不会随 ChangeSceneToFile 重建）。
	/// </summary>
	public IEventBus EventBus { get; private set; } = null!;

	/// <summary>
	/// VS 范围内的玩家心境状态服务。
	///
	/// 单例：所有 outcome 选择都通过这一份 MindsetService 累积位移，
	/// scene transition 不丢状态。Sprint 7 不持久化，整局会话内存 only。
	/// </summary>
	public MindsetService MindsetService { get; private set; } = null!;

	/// <summary>
	/// 上一次 outcome 选择产生的心境位移快照（含 oldState / newState / 应用的 deltas）。
	/// OutcomeGame UI 读取此快照渲染朦胧化前后对比文本。
	/// 在 ApplyOutcomeChoice 时刷新；GoToOutcome 进入时仍是上一帧旧值（首次进入时为 null）。
	/// </summary>
	public MindsetShiftSnapshot? LastMindsetShiftSnapshot { get; private set; }

	public override void _Ready()
	{
		EventBus = new EventBus();
		MindsetService = new MindsetService(eventBus: EventBus);
		GD.Print($"[JiangnanFlow] Boot. EventBus + MindsetService autoload ready. " +
				 $"Initial state R={MindsetService.State.Resolve} W={MindsetService.State.Worldly} M={MindsetService.State.Morality}");
	}

	public void GoToExplore()
	{
		LogTransition(CurrentPhase, Phase.Explore);
		CurrentPhase = Phase.Explore;
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		GetTree().ChangeSceneToFile(JiangnanRiversideScenePath);
	}

	public void GoToBattle()
	{
		LogTransition(CurrentPhase, Phase.Battle);
		CurrentPhase = Phase.Battle;
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		GetTree().ChangeSceneToFile(BattleJiangnanScenePath);
	}

	/// <summary>
	/// 战斗结束后进入 outcome scene。
	/// </summary>
	/// <param name="result">战斗结果；BattleResult.InProgress 视为占位（旧 placeholder 调用兼容）。</param>
	public void GoToOutcome(BattleResult result = BattleResult.InProgress)
	{
		LogTransition(CurrentPhase, Phase.Outcome);
		CurrentPhase = Phase.Outcome;
		HasCompletedBattleOnce = true;
		LastBattleResult = result;
		GD.Print($"[JiangnanFlow] LastBattleResult = {result}");
		EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
		GetTree().ChangeSceneToFile(OutcomeJiangnanScenePath);
	}

	public void RecordMindsetChoice(MindsetChoice choice)
	{
		LastMindsetChoice = choice;
		GD.Print($"[JiangnanFlow] Mindset choice recorded: {choice}");
	}

	/// <summary>
	/// 应用 outcome scene 玩家选择产生的心境位移。
	///
	/// 数值来源：dev-story spec §3（owner 2026-06-23 sign-off）。
	///   - Spare  → Morality +5（关键善行）+ Resolve +2（放下一寸）
	///   - Defeat → Morality -5（关键恶行）+ Resolve -2（执念加深）
	///   - None / 未识别 → 不位移
	///
	/// 位移通过 MindsetService.ApplyShifts 批量应用；EventBus 会广播每个
	/// <see cref="MindsetShiftedEvent"/>（一次调用可能发 2 个），并在区域 / 道义档位变化时
	/// 发 <see cref="MindsetZoneChangedEvent"/> / <see cref="MoralityTierChangedEvent"/>。
	///
	/// 同步刷新 LastMindsetShiftSnapshot，供 OutcomeGame 渲染前后对比文本。
	/// </summary>
	public void ApplyOutcomeChoice(MindsetChoice choice)
	{
		LastMindsetChoice = choice;

		var shifts = ResolveShifts(choice);
		if (shifts.Count == 0)
		{
			GD.Print($"[JiangnanFlow] Outcome choice {choice}: no shifts to apply.");
			LastMindsetShiftSnapshot = null;
			return;
		}

		var oldState = MindsetService.State;
		var oldZone = MindsetService.CurrentZone;
		var oldTier = MindsetService.CurrentMoralityTier;

		MindsetService.ApplyShifts(shifts);

		var newState = MindsetService.State;
		var newZone = MindsetService.CurrentZone;
		var newTier = MindsetService.CurrentMoralityTier;

		LastMindsetShiftSnapshot = new MindsetShiftSnapshot(
			Choice: choice,
			OldState: oldState,
			NewState: newState,
			OldZone: oldZone,
			NewZone: newZone,
			OldMoralityTier: oldTier,
			NewMoralityTier: newTier,
			AppliedShifts: shifts);

		GD.Print(
			$"[JiangnanFlow] Outcome choice {choice} applied: " +
			$"R {oldState.Resolve}→{newState.Resolve}, " +
			$"W {oldState.Worldly}→{newState.Worldly}, " +
			$"M {oldState.Morality}→{newState.Morality}; " +
			$"zone {oldZone}→{newZone}; tier {oldTier}→{newTier}");
	}

	/// <summary>
	/// dev-story spec §3 规约的 choice → shifts 映射。
	/// 抽成静态方法是为了 integration tests 直接验证映射表，不必构造 Autoload。
	/// </summary>
	public static IReadOnlyList<MindsetShift> ResolveShifts(MindsetChoice choice)
	{
		return choice switch
		{
			MindsetChoice.Spare => new[]
			{
				new MindsetShift(MindsetAxis.Morality, +5),
				new MindsetShift(MindsetAxis.Resolve, +2),
			},
			MindsetChoice.Defeat => new[]
			{
				new MindsetShift(MindsetAxis.Morality, -5),
				new MindsetShift(MindsetAxis.Resolve, -2),
			},
			_ => System.Array.Empty<MindsetShift>(),
		};
	}

	private static void LogTransition(Phase from, Phase to)
	{
		GD.Print($"[JiangnanFlow] {from} → {to}");
	}
}

/// <summary>
/// outcome scene 一次心境位移的完整快照。
///
/// 字段一次性冻结调用 <see cref="JiangnanFlowController.ApplyOutcomeChoice"/> 前后的状态，
/// 让 UI 不依赖事件顺序就能渲染朦胧化「前 → 后」对比展示。
/// </summary>
public sealed record MindsetShiftSnapshot(
	JiangnanFlowController.MindsetChoice Choice,
	MindsetState OldState,
	MindsetState NewState,
	MindsetZone OldZone,
	MindsetZone NewZone,
	MoralityTier OldMoralityTier,
	MoralityTier NewMoralityTier,
	IReadOnlyList<MindsetShift> AppliedShifts);
