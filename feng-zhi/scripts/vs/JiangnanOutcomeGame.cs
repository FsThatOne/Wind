using System;
using System.Collections.Generic;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Mindset;
using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南战后 outcome scene 控制器。
///
/// 流程（dev-story spec §1）：
/// 1. _Ready 读 LastBattleResult，渲染 ChoicePanel 提示文案
/// 2. 玩家选择「放过」/「重伤」→ flow.ApplyOutcomeChoice(choice) 真实触发 Mindset 位移
///    （位移数值见 spec §3 / JiangnanFlowController.ResolveShifts）
/// 3. 订阅 MindsetShiftedEvent 收集本次位移（用于 GD.Print trace；UI 不依赖事件序）
/// 4. 读 flow.LastMindsetShiftSnapshot 在 BlurredPanel 渲染：
///    - 当前 zone 文学名 + 副标（MindsetZoneLiteraryNames）
///    - Resolve / Worldly 前后描述对比（MindsetPresentationService）
///    - 道义档位名（MindsetZoneLiteraryNames）
/// 5. 按「继续」→ flow.GoToExplore 回 explore scene
///
/// AC3 范围（spec §5）：接入真实服务 + 文学化文本渲染。
/// AC4 视觉（modulate / StyleBoxFlat）由 Subtask 5 完成。
/// </summary>
public partial class JiangnanOutcomeGame : Node2D
{
	private Label _resultTitleLabel = null!;
	private Button _spareButton = null!;
	private Button _defeatButton = null!;
	private Button _continueButton = null!;
	private Panel _choicePanel = null!;
	private Panel _blurredPanel = null!;
	private Label _zoneNameLabel = null!;
	private Label _resolveBeforeLabel = null!;
	private Label _resolveAfterLabel = null!;
	private Label _worldlyBeforeLabel = null!;
	private Label _worldlyAfterLabel = null!;
	private Label _moralityTierLabel = null!;

	private Action? _unsubscribeShifted;

	public override void _Ready()
	{
		_resultTitleLabel = GetNode<Label>("UiLayer/ChoicePanel/TitleLabel");
		_spareButton = GetNode<Button>("UiLayer/ChoicePanel/SpareButton");
		_defeatButton = GetNode<Button>("UiLayer/ChoicePanel/DefeatButton");
		_choicePanel = GetNode<Panel>("UiLayer/ChoicePanel");
		_blurredPanel = GetNode<Panel>("UiLayer/BlurredPanel");
		_zoneNameLabel = GetNode<Label>("UiLayer/BlurredPanel/ZoneNameLabel");
		_resolveBeforeLabel = GetNode<Label>("UiLayer/BlurredPanel/ResolveBeforeLabel");
		_resolveAfterLabel = GetNode<Label>("UiLayer/BlurredPanel/ResolveAfterLabel");
		_worldlyBeforeLabel = GetNode<Label>("UiLayer/BlurredPanel/WorldlyBeforeLabel");
		_worldlyAfterLabel = GetNode<Label>("UiLayer/BlurredPanel/WorldlyAfterLabel");
		_moralityTierLabel = GetNode<Label>("UiLayer/BlurredPanel/MoralityTierLabel");
		_continueButton = GetNode<Button>("UiLayer/BlurredPanel/ContinueButton");

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

		if (flow?.EventBus != null)
		{
			_unsubscribeShifted = flow.EventBus.Subscribe<MindsetShiftedEvent>(OnMindsetShifted);
		}

		GD.Print($"[JiangnanOutcome] Ready. BattleResult = {battleResult}");
	}

	public override void _ExitTree()
	{
		_unsubscribeShifted?.Invoke();
		_unsubscribeShifted = null;
	}

	private void HandleChoice(JiangnanFlowController.MindsetChoice choice)
	{
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		if (flow == null)
		{
			GD.PushWarning("[JiangnanOutcome] JiangnanFlowController autoload not found; falling back to placeholder text.");
			RenderFallback(choice);
			return;
		}

		flow.ApplyOutcomeChoice(choice);

		var snapshot = flow.LastMindsetShiftSnapshot;
		if (snapshot == null)
		{
			GD.PushWarning($"[JiangnanOutcome] No snapshot returned for choice {choice}; falling back.");
			RenderFallback(choice);
		}
		else
		{
			RenderSnapshot(snapshot);
		}

		_choicePanel.Visible = false;
		_blurredPanel.Visible = true;
		_continueButton.Visible = true;
	}

	private void RenderSnapshot(MindsetShiftSnapshot snapshot)
	{
		var (zoneName, zoneSubtitle) = MindsetZoneLiteraryNames.GetZoneName(snapshot.NewZone);
		_zoneNameLabel.Text = $"{zoneName} · {zoneSubtitle}";

		_resolveBeforeLabel.Text = MindsetPresentationService.GetResolveDescription(snapshot.OldState);
		_resolveAfterLabel.Text = MindsetPresentationService.GetResolveDescription(snapshot.NewState);

		_worldlyBeforeLabel.Text = MindsetPresentationService.GetWorldlyDescription(snapshot.OldState);
		_worldlyAfterLabel.Text = MindsetPresentationService.GetWorldlyDescription(snapshot.NewState);

		var oldTierName = MindsetZoneLiteraryNames.GetMoralityTierName(snapshot.OldMoralityTier);
		var newTierName = MindsetZoneLiteraryNames.GetMoralityTierName(snapshot.NewMoralityTier);
		_moralityTierLabel.Text = oldTierName == newTierName
			? $"道义：{newTierName}"
			: $"道义：{oldTierName} → {newTierName}";

		GD.Print(
			$"[JiangnanOutcome] Snapshot rendered: zone={zoneName}; " +
			$"resolve {snapshot.OldState.Resolve}→{snapshot.NewState.Resolve}; " +
			$"worldly {snapshot.OldState.Worldly}→{snapshot.NewState.Worldly}; " +
			$"morality {snapshot.OldState.Morality}→{snapshot.NewState.Morality} ({newTierName})");
	}

	private void RenderFallback(JiangnanFlowController.MindsetChoice choice)
	{
		_zoneNameLabel.Text = choice switch
		{
			JiangnanFlowController.MindsetChoice.Spare => "心境向「仁」位移",
			JiangnanFlowController.MindsetChoice.Defeat => "心境向「狠」位移",
			_ => "心境未位移",
		};
		_resolveBeforeLabel.Text = "—";
		_resolveAfterLabel.Text = "（fallback：Autoload 缺失）";
		_worldlyBeforeLabel.Text = "—";
		_worldlyAfterLabel.Text = "—";
		_moralityTierLabel.Text = "—";
	}

	private void OnMindsetShifted(MindsetShiftedEvent evt)
	{
		GD.Print(
			$"[JiangnanOutcome] MindsetShiftedEvent axis={evt.Axis} " +
			$"{evt.OldValue} → {evt.NewValue} (Δ={evt.Delta:+#;-#;0})");
	}

	private void ReturnToExplore()
	{
		GD.Print("[JiangnanOutcome] Continue pressed → returning to explore.");
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToExplore();
	}
}
