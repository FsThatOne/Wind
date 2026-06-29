using System;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;

namespace FengZhi.Scripts.Exploration;

/// <summary>
/// 订阅 MonologueRequest 两阶段事件，驱动 DialoguePanel 展示 narrative_context 独白，
/// 并在 Committed 后显示奖励反馈。
/// </summary>
public partial class InsightMonologuePresenter : Node
{
	private const float RewardFeedbackDuration = 2.0f;

	private IEventBus? _eventBus;
	private InsightNodeRegistry? _registry;
	private Dialogue.DialoguePanel? _dialoguePanel;
	private Label? _promptLabel;
	private Node2D? _player;

	private Action? _unsubPending;
	private Action? _unsubCommitted;
	private Action? _unsubCanceled;
	private string? _pendingNodeId;
	private System.Reflection.PropertyInfo? _frozenProp;
	private bool _frozenPropResolved;
	private bool _configured;

	public void Configure(
		IEventBus eventBus,
		InsightNodeRegistry registry,
		Dialogue.DialoguePanel dialoguePanel,
		Label promptLabel,
		Node2D player)
	{
		_eventBus = eventBus;
		_registry = registry;
		_dialoguePanel = dialoguePanel;
		_promptLabel = promptLabel;
		_player = player;
		SubscribeEvents();
		_configured = true;
	}

	public override void _Ready()
	{
		if (_configured) return;
		AutoConfigure();
	}

	public override void _ExitTree()
	{
		_unsubPending?.Invoke();
		_unsubCommitted?.Invoke();
		_unsubCanceled?.Invoke();
		_unsubPending = null;
		_unsubCommitted = null;
		_unsubCanceled = null;
	}

	private void AutoConfigure()
	{
		var gameFlow = GetNodeOrNull<GameFlow>("/root/GameFlow");
		if (gameFlow == null)
		{
			GD.PushWarning("[InsightMonologuePresenter] GameFlow 未找到，presenter 未配置。");
			return;
		}

		_eventBus = gameFlow.EventBus;

		var bridge = GetParent()?.GetNodeOrNull<InsightDetectorBridge>("InsightDetectorBridge");
		if (bridge != null)
		{
			_registry = bridge.Registry;
		}

		var dialogueLayer = GetParent()?.GetNodeOrNull("DialogueLayer");
		_dialoguePanel = dialogueLayer?.GetNodeOrNull<Dialogue.DialoguePanel>("DialoguePanel");

		_promptLabel = GetParent()?.GetNodeOrNull<Label>("UiLayer/PromptLabel");
		_player = GetParent()?.GetNodeOrNull<Node2D>("MapRoot/Player");

		if (_eventBus != null)
		{
			SubscribeEvents();
			_configured = true;
		}
	}

	private void SubscribeEvents()
	{
		if (_eventBus == null) return;
		_unsubPending = _eventBus.Subscribe<MonologueRequestPendingEvent>(OnPending);
		_unsubCommitted = _eventBus.Subscribe<MonologueRequestCommittedEvent>(OnCommitted);
		_unsubCanceled = _eventBus.Subscribe<MonologueRequestCanceledEvent>(OnCanceled);
	}

	private void OnPending(MonologueRequestPendingEvent e)
	{
		_pendingNodeId = e.NodeId;
		FreezePlayer(true);

		if (_registry == null || _dialoguePanel == null) return;
		if (!_registry.TryGetNode(e.NodeId, out var node) || node == null) return;

		_dialoguePanel.ShowMonologue(node.NarrativeContext);
	}

	private void OnCommitted(MonologueRequestCommittedEvent e)
	{
		if (_pendingNodeId != e.NodeId) return;
		_pendingNodeId = null;

		_dialoguePanel?.HideMonologue();

		if (_registry != null && _registry.TryGetNode(e.NodeId, out var node) && node != null)
		{
			var feedbackText = GetRewardFeedback(node);
			if (!string.IsNullOrEmpty(feedbackText))
			{
				ShowRewardFeedback(feedbackText);
				return;
			}
		}

		FreezePlayer(false);
	}

	private void OnCanceled(MonologueRequestCanceledEvent e)
	{
		if (_pendingNodeId != e.NodeId) return;
		_pendingNodeId = null;

		_dialoguePanel?.HideMonologue();
		FreezePlayer(false);
	}

	private static string GetRewardFeedback(InsightNode node)
	{
		return node.DiscoveryType switch
		{
			DiscoveryType.Clue => $"获得线索：{node.Reward.FlagId}",
			DiscoveryType.CodePhrase => $"习得暗号：{node.Reward.PhraseId}",
			DiscoveryType.Loot => $"获得物品：{node.Reward.ItemId} ×{node.Reward.Quantity}",
			DiscoveryType.MartialFragment => $"获得残卷：{node.Reward.MartialId}",
			DiscoveryType.SideQuestEntry => "触发支线",
			DiscoveryType.EnvironmentDetail => "",
			_ => ""
		};
	}

	private void ShowRewardFeedback(string text)
	{
		if (_promptLabel == null)
		{
			FreezePlayer(false);
			return;
		}

		_promptLabel.Text = text;
		_promptLabel.Visible = true;

		var timer = GetTree().CreateTimer(RewardFeedbackDuration);
		timer.Timeout += () =>
		{
			if (_promptLabel != null)
			{
				_promptLabel.Text = "";
				_promptLabel.Visible = false;
			}
			FreezePlayer(false);
		};
	}

	private void FreezePlayer(bool frozen)
	{
		if (_player == null) return;
		if (!_frozenPropResolved)
		{
			_frozenProp = _player.GetType().GetProperty("MovementFrozen");
			_frozenPropResolved = true;
		}
		if (_frozenProp != null && _frozenProp.CanWrite)
			_frozenProp.SetValue(_player, frozen);
	}
}
