using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南战斗 scene 控制器（VS Sprint 7 Lite Xingqi 骨架）。
///
/// 当前阶段为 S7-VS-Foundation-Scene 占位实现：
/// - 显示 placeholder 战斗 UI（"江南遭遇：江湖小贼" / 模拟回合按钮）
/// - 按"立即胜利" / "立即失败" 两个按钮触发 outcome 转场
///
/// S7-VS-Combat-Loop（下一 story）会替换为真实 cu-001..008 + cb-001..010 集成：
/// - 复用 BattleFacade 启动 1v1 战斗
/// - 复用 combat-ui CombatUiHud / MoveSelectionPanel / DecisiveStrikeDirector
/// - "观气 → 出招 → 破绽 → 决胜" 语言层包装
/// - lite 不实现行气条 / 棋盘
///
/// 详见 docs/superpowers/specs/2026-06-23-vs-scope-spike.md §4.1 战斗 scene 子图。
/// </summary>
public partial class JiangnanBattleGame : Node2D
{
	public override void _Ready()
	{
		var winButton = GetNode<Button>("UiLayer/PlaceholderPanel/WinButton");
		var loseButton = GetNode<Button>("UiLayer/PlaceholderPanel/LoseButton");

		winButton.Pressed += () => TransitionToOutcome(victory: true);
		loseButton.Pressed += () => TransitionToOutcome(victory: false);

		GD.Print("[JiangnanBattle] Ready (Lite skeleton; awaiting S7-VS-Combat-Loop integration).");
	}

	private void TransitionToOutcome(bool victory)
	{
		GD.Print($"[JiangnanBattle] Battle finished. Victory={victory}. Transitioning to outcome.");

		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToOutcome();
	}
}
