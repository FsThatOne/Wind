using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南河畔 explore scene 控制器（VS Sprint 7 Lite Xingqi 骨架）。
///
/// Code-first placeholder 范围：
/// - 不接入 PlayerCharacterController 移动 + Area2D 触发链路（推迟到 Sprint 8 或更晚）
/// - 用按钮代替交互：「对话老者」/「前往战斗」
/// - 根据 JiangnanFlowController.HasCompletedBattleOnce + LastMindsetChoice 动态切换 NPC 反应文本
///
/// 详见 docs/superpowers/specs/2026-06-23-vs-scope-spike.md §4.1 explore scene 子图。
/// </summary>
public partial class JiangnanRiversideGame : Node2D
{
	private Label _statusLabel = null!;
	private Panel _messagePanel = null!;
	private Label _messageLabel = null!;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("UiLayer/StatusLabel");
		_messagePanel = GetNode<Panel>("UiLayer/MessagePanel");
		_messageLabel = GetNode<Label>("UiLayer/MessagePanel/MessageLabel");

		var talkButton = GetNode<Button>("UiLayer/ActionsPanel/TalkButton");
		var battleButton = GetNode<Button>("UiLayer/ActionsPanel/BattleButton");

		talkButton.Pressed += OnTalkPressed;
		battleButton.Pressed += OnBattlePressed;

		_messagePanel.Visible = false;

		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		if (flow != null && flow.HasCompletedBattleOnce)
		{
			_statusLabel.Text = flow.LastMindsetChoice switch
			{
				JiangnanFlowController.MindsetChoice.Spare =>
					"江南河畔（战后）— 老者神色温了几分。",
				JiangnanFlowController.MindsetChoice.Defeat =>
					"江南河畔（战后）— 老者眉间隐隐戒备。",
				_ => "江南河畔（战后）",
			};
		}
		else
		{
			_statusLabel.Text = "江南河畔 — 前方似有人影。";
		}

		GD.Print($"[JiangnanRiverside] Ready. HasCompletedBattleOnce={flow?.HasCompletedBattleOnce ?? false}");
	}

	private void OnTalkPressed()
	{
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		var text = flow != null && flow.HasCompletedBattleOnce
			? flow.LastMindsetChoice switch
			{
				JiangnanFlowController.MindsetChoice.Spare =>
					"老者：年轻人，今日见你手下留情，倒像旧识。",
				JiangnanFlowController.MindsetChoice.Defeat =>
					"老者：……年轻人，江湖路远，下手太重，会折了自己的气。",
				_ => "老者：江湖再见。",
			}
			: "老者：江南春迟，年轻人远来，可是寻人？";
		ShowMessage(text);
	}

	private void OnBattlePressed()
	{
		GD.Print("[JiangnanRiverside] BattleButton pressed → transitioning to battle.");
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToBattle();
	}

	private void ShowMessage(string text)
	{
		_messageLabel.Text = text;
		_messagePanel.Visible = true;
	}
}
