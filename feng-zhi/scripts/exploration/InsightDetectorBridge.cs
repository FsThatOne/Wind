using System;
using System.Collections.Generic;
using System.Reflection;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Exploration;
using Godot;

namespace FengZhi.Scripts.Exploration;

/// <summary>
/// Godot ↔ Foundation 桥接节点：在 _PhysicsProcess 中驱动 ProximityDetector.Tick()，
/// 并订阅事件总线渲染/隐藏水墨风格洞察提示。
/// cue 出现时自动注册交互区域，玩家按 interact 时调用 DiscoveryDispatcher。
///
/// 使用方式：作为场景子节点挂载，调用 Configure() 或依赖自动初始化（查找 GameFlow autoload）。
/// </summary>
public partial class InsightDetectorBridge : Node
{
	private const int DefaultInsight = 10;
	private const float InteractionRadius = 40f;
	private const string InvestigatePromptText = "E / 空格 追查";

	[Export] public NodePath PlayerPath = "";

	private ProximityDetector? _detector;
	private InsightNodeRegistry _registry = new();
	private IEventBus? _eventBus;
	private Node2D? _player;
	private Func<int>? _getInsight;
	private Node2D? _cueContainer;
	private DiscoveryDispatcher? _dispatcher;
	private Label? _promptLabel;

	private readonly Dictionary<string, InsightCueVisual> _activeCues = new();
	private readonly Dictionary<string, Area2D> _interactionAreas = new();
	private string? _focusedNodeId;
	private Action? _unsubShown;
	private Action? _unsubHidden;
	private Action? _unsubCommitted;
	private PropertyInfo? _frozenProp;
	private bool _frozenPropResolved;
	private bool _isPausedByFreeze;
	private bool _configured;

	/// <summary>供 InsightMonologuePresenter 读取注册表。</summary>
	public InsightNodeRegistry Registry => _registry;

	/// <summary>
	/// 外部配置入口。场景脚本可用自定义 registry/detector 覆盖自动初始化。
	/// </summary>
	public void Configure(
		ProximityDetector detector,
		IEventBus eventBus,
		Node2D player,
		Func<int> getInsight)
	{
		_detector = detector;
		_eventBus = eventBus;
		_player = player;
		_getInsight = getInsight;
		SubscribeEvents();
		_configured = true;
	}

	public override void _Ready()
	{
		_cueContainer = new Node2D { Name = "InsightCues" };
		AddChild(_cueContainer);

		if (_configured)
		{
			ResolvePromptLabel();
			return;
		}
		AutoConfigure();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_detector == null || _player == null) return;

		var isFrozen = IsPlayerFrozen();
		if (isFrozen && !_isPausedByFreeze)
		{
			_detector.PauseDetection();
			_isPausedByFreeze = true;
			RemoveAllCues();
			RemoveAllInteractionAreas();
			return;
		}
		if (!isFrozen && _isPausedByFreeze)
		{
			_detector.ResumeDetection();
			_isPausedByFreeze = false;
		}
		if (_isPausedByFreeze) return;

		var playerPos = _player.GlobalPosition;
		var insight = _getInsight?.Invoke() ?? DefaultInsight;
		_detector.Tick(playerPos, insight, (float)delta);
	}

	public override void _ExitTree()
	{
		_detector?.OnSceneUnloaded();
		RemoveAllCuesImmediate();
		RemoveAllInteractionAreas();
		_unsubShown?.Invoke();
		_unsubHidden?.Invoke();
		_unsubCommitted?.Invoke();
		_unsubShown = null;
		_unsubHidden = null;
		_unsubCommitted = null;
	}

	/// <summary>向 registry 注册一个 InsightNode，用于场景运行时动态添加。</summary>
	public void RegisterInsightNode(InsightNode node)
	{
		_registry.RegisterNode(node);
	}

	/// <summary>激活指定场景的 InsightNode 集合。</summary>
	public void ActivateScene(string sceneId)
	{
		_registry.OnSceneLoaded(sceneId);
	}

	private void AutoConfigure()
	{
		var gameFlow = GetNodeOrNull<GameFlow>("/root/GameFlow");

		IEventBus eventBus;
		Func<int> getInsight;

		if (gameFlow != null)
		{
			eventBus = gameFlow.EventBus;
			getInsight = () => gameFlow.PlayerInstance?.Attributes.Insight ?? DefaultInsight;
			GD.Print("[InsightDetectorBridge] 使用 GameFlow EventBus + PlayerInstance.Insight");
		}
		else
		{
			eventBus = new EventBus();
			getInsight = () => DefaultInsight;
			GD.Print("[InsightDetectorBridge] GameFlow 未找到，使用独立 EventBus + 默认 insight=10");
		}

		_eventBus = eventBus;
		_getInsight = getInsight;

		_detector = new ProximityDetector(
			_registry,
			new AlwaysTrueConditionEvaluator(),
			_eventBus);

		_dispatcher = new DiscoveryDispatcher(
			_registry,
			new StubNarrativePort(),
			new StubCodePhraseBookPort(),
			_eventBus);

		ResolvePlayer();
		ResolvePromptLabel();
		SubscribeEvents();
		_configured = true;
	}

	private void ResolvePlayer()
	{
		if (!string.IsNullOrEmpty(PlayerPath))
		{
			_player = GetNodeOrNull<Node2D>(PlayerPath);
		}

		if (_player != null) return;

		var parent = GetParent();
		_player = parent?.GetNodeOrNull<Node2D>("MapRoot/Player")
			?? parent?.GetNodeOrNull<Node2D>("Player");

		if (_player == null)
			GD.PushWarning("[InsightDetectorBridge] 未找到 Player 节点，Tick 将被跳过。");
	}

	private void ResolvePromptLabel()
	{
		_promptLabel = GetParent()?.GetNodeOrNull<Label>("UiLayer/PromptLabel");
	}

	private void SubscribeEvents()
	{
		if (_eventBus == null) return;
		_unsubShown = _eventBus.Subscribe<InsightCueShownEvent>(OnCueShown);
		_unsubHidden = _eventBus.Subscribe<InsightCueHiddenEvent>(OnCueHidden);
		_unsubCommitted = _eventBus.Subscribe<MonologueRequestCommittedEvent>(OnInvestigateCommitted);
	}

	private void OnCueShown(InsightCueShownEvent e)
	{
		if (_activeCues.ContainsKey(e.NodeId)) return;

		var cue = new InsightCueVisual
		{
			Name = $"Cue_{e.NodeId}",
			NodeId = e.NodeId,
			Position = e.Position,
		};
		_cueContainer?.AddChild(cue);
		cue.FadeIn();
		_activeCues[e.NodeId] = cue;

		CreateInteractionArea(e.NodeId, e.Position);
	}

	private void OnCueHidden(InsightCueHiddenEvent e)
	{
		if (_activeCues.TryGetValue(e.NodeId, out var cue))
		{
			_activeCues.Remove(e.NodeId);
			cue.FadeOut();
		}
		RemoveInteractionArea(e.NodeId);
	}

	private void OnInvestigateCommitted(MonologueRequestCommittedEvent e)
	{
		if (_activeCues.TryGetValue(e.NodeId, out var cue))
		{
			_activeCues.Remove(e.NodeId);
			cue.FadeOut();
		}
		RemoveInteractionArea(e.NodeId);
	}

	private void RemoveAllCues()
	{
		foreach (var cue in _activeCues.Values)
			cue.FadeOut();
		_activeCues.Clear();
	}

	private void RemoveAllCuesImmediate()
	{
		foreach (var cue in _activeCues.Values)
		{
			if (IsInstanceValid(cue))
				cue.QueueFree();
		}
		_activeCues.Clear();
	}

	// ============================================================
	// 交互区域管理
	// ============================================================

	private void CreateInteractionArea(string nodeId, Vector2 position)
	{
		if (_interactionAreas.ContainsKey(nodeId)) return;

		var area = new Area2D
		{
			Name = $"InsightArea_{nodeId}",
			Position = position,
		};
		var shape = new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = InteractionRadius },
		};
		area.AddChild(shape);
		area.BodyEntered += body => OnInsightAreaEntered(nodeId, body);
		area.BodyExited += body => OnInsightAreaExited(nodeId, body);
		_cueContainer?.AddChild(area);
		_interactionAreas[nodeId] = area;
	}

	private void RemoveInteractionArea(string nodeId)
	{
		if (!_interactionAreas.TryGetValue(nodeId, out var area)) return;
		_interactionAreas.Remove(nodeId);
		if (_focusedNodeId == nodeId)
		{
			_focusedNodeId = null;
			HideInvestigationPrompt();
		}
		if (IsInstanceValid(area))
			area.QueueFree();
	}

	private void RemoveAllInteractionAreas()
	{
		foreach (var area in _interactionAreas.Values)
		{
			if (IsInstanceValid(area))
				area.QueueFree();
		}
		_interactionAreas.Clear();
		_focusedNodeId = null;
		HideInvestigationPrompt();
	}

	private void OnInsightAreaEntered(string nodeId, Node2D body)
	{
		if (body != _player) return;
		_focusedNodeId = nodeId;
		ShowInvestigationPrompt();
	}

	private void OnInsightAreaExited(string nodeId, Node2D body)
	{
		if (body != _player || _focusedNodeId != nodeId) return;
		_focusedNodeId = null;
		HideInvestigationPrompt();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_focusedNodeId == null) return;
		if (!@event.IsActionPressed("interact")) return;
		if (IsPlayerFrozen()) return;

		var nodeId = _focusedNodeId;
		_focusedNodeId = null;
		HideInvestigationPrompt();

		if (_dispatcher == null) return;
		_dispatcher.OnPlayerInvestigate(nodeId);
		GetViewport().SetInputAsHandled();
	}

	private void ShowInvestigationPrompt()
	{
		if (_promptLabel == null) return;
		_promptLabel.Text = InvestigatePromptText;
		_promptLabel.Visible = true;
	}

	private void HideInvestigationPrompt()
	{
		if (_promptLabel == null || _promptLabel.Text != InvestigatePromptText) return;
		_promptLabel.Visible = false;
	}

	private bool IsPlayerFrozen()
	{
		if (_player == null) return false;
		if (!_frozenPropResolved)
		{
			_frozenProp = _player.GetType().GetProperty("MovementFrozen");
			_frozenPropResolved = true;
		}
		if (_frozenProp != null && _frozenProp.CanRead)
			return (bool)(_frozenProp.GetValue(_player) ?? false);
		return false;
	}

	// ============================================================
	// 内部类型
	// ============================================================

	/// <summary>
	/// v0 条件评估器：所有前置条件视为满足。
	/// 后续 story 接入 ADR-0014 ConditionEvaluator 后替换。
	/// </summary>
	private sealed class AlwaysTrueConditionEvaluator : IInsightConditionEvaluator
	{
		public bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites) => true;
	}

	/// <summary>v0 narrative port：总是允许独白播放和 flag 设置。</summary>
	private sealed class StubNarrativePort : IInsightNarrativePort
	{
		public bool CanPlayInnerMonologue(string narrativeContext) => true;
		public bool TryPlayInnerMonologue(string narrativeContext) => true;
		public bool CanSetQuestFlag(string flagId) => true;
		public bool TrySetQuestFlag(string flagId, string value) => true;
	}

	/// <summary>v0 code phrase book port：总是允许学习。</summary>
	private sealed class StubCodePhraseBookPort : IInsightCodePhraseBookPort
	{
		public bool CanLearnPhrase(string phraseId) => true;
		public bool TryLearnPhrase(string phraseId) => true;
	}
}
