using FengZhi.Foundation.Combat;
using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南战后 outcome scene 控制器。
///
/// 流程：
/// 1. 显示战斗结果文本（占位）
/// 2. 玩家选择：放过（Spare）/ 重伤（Defeat）— 触发心境位移
/// 3. 显示朦胧化战后面板（partial 实现 — 1 个 ColorRect 半透明 + 文本说明心境位移方向）
/// 4. 按 "继续" → 回 explore（带 OutcomeContext，JiangnanRiverside 读取后更新 NPC 反应）
///
/// VS Sprint 7 Lite Xingqi 范围：blurred-ui 用占位面板代替；mindset-dual-axis 仅触发 1 次位移事件（不接 epic runtime）。
/// Sprint 8 可换接 blurred-ui 完整 epic + mindset-dual-axis runtime API。
/// </summary>
public partial class JiangnanOutcomeGame : Node2D
{
	private Label _resultTitleLabel = null!;
	private Button _spareButton = null!;
	private Button _defeatButton = null!;
	private Button _continueButton = null!;
	private Label _mindsetLabel = null!;
	private Panel _blurredPanel = null!;

	public override void _Ready()
	{
		_resultTitleLabel = GetNode<Label>("UiLayer/ChoicePanel/TitleLabel");
		_spareButton = GetNode<Button>("UiLayer/ChoicePanel/SpareButton");
		_defeatButton = GetNode<Button>("UiLayer/ChoicePanel/DefeatButton");
		_continueButton = GetNode<Button>("UiLayer/BlurredPanel/ContinueButton");
		_mindsetLabel = GetNode<Label>("UiLayer/BlurredPanel/MindsetLabel");
		_blurredPanel = GetNode<Panel>("UiLayer/BlurredPanel");

		_blurredPanel.Visible = false;
		_continueButton.Visible = false;

		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		var battleResult = flow?.LastBattleResult ?? BattleResult.InProgress;
		_resultTitleLabel.Text = battleResult switch
		{
			BattleResult.Victory => "你制服了江湖小贼。如何处置？",
			BattleResult.Defeat => "你被小贼击倒在地……江湖路远，是放下还是再战？",
			BattleResult.NarrowDefeat => "两败俱伤。你与小贼都喘息着——",
			BattleResult.Draw => "战局僵持，江南雾起，小贼退去。",
			_ => "战斗结束。你如何处置对手？",
		};

		_spareButton.Pressed += () => HandleChoice(JiangnanFlowController.MindsetChoice.Spare);
		_defeatButton.Pressed += () => HandleChoice(JiangnanFlowController.MindsetChoice.Defeat);
		_continueButton.Pressed += ReturnToExplore;

		GD.Print($"[JiangnanOutcome] Ready. BattleResult = {battleResult}");
	}

	private void HandleChoice(JiangnanFlowController.MindsetChoice choice)
	{
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.RecordMindsetChoice(choice);

		_mindsetLabel.Text = choice switch
		{
			JiangnanFlowController.MindsetChoice.Spare => "心境向「仁」位移 +1",
			JiangnanFlowController.MindsetChoice.Defeat => "心境向「狠」位移 +1",
			_ => "心境未位移",
		};

		GetNode<Panel>("UiLayer/ChoicePanel").Visible = false;
		_blurredPanel.Visible = true;
		_continueButton.Visible = true;
	}

	private void ReturnToExplore()
	{
		GD.Print("[JiangnanOutcome] Continue pressed → returning to explore.");
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToExplore();
	}
}
