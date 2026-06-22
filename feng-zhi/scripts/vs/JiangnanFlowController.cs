using FengZhi.Foundation.Combat;
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

	public override void _Ready()
	{
		GD.Print("[JiangnanFlow] Boot. Awaiting first scene transition.");
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

	private static void LogTransition(Phase from, Phase to)
	{
		GD.Print($"[JiangnanFlow] {from} → {to}");
	}
}
