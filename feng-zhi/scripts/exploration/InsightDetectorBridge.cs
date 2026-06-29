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
///
/// 使用方式：作为场景子节点挂载，调用 Configure() 或依赖自动初始化（查找 GameFlow autoload）。
/// </summary>
public partial class InsightDetectorBridge : Node
{
	private const int DefaultInsight = 10;

	[Export] public NodePath PlayerPath = "";

	private ProximityDetector? _detector;
	private InsightNodeRegistry _registry = new();
	private IEventBus? _eventBus;
	private Node2D? _player;
	private Func<int>? _getInsight;
	private Node2D? _cueContainer;

	private readonly Dictionary<string, InsightCueVisual> _activeCues = new();
	private Action? _unsubShown;
	private Action? _unsubHidden;
	private PropertyInfo? _frozenProp;
	private bool _frozenPropResolved;
	private bool _isPausedByFreeze;
	private bool _configured;

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

		if (_configured) return;
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
		_unsubShown?.Invoke();
		_unsubHidden?.Invoke();
		_unsubShown = null;
		_unsubHidden = null;
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

		ResolvePlayer();
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

	private void SubscribeEvents()
	{
		if (_eventBus == null) return;
		_unsubShown = _eventBus.Subscribe<InsightCueShownEvent>(OnCueShown);
		_unsubHidden = _eventBus.Subscribe<InsightCueHiddenEvent>(OnCueHidden);
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
	}

	private void OnCueHidden(InsightCueHiddenEvent e)
	{
		if (!_activeCues.TryGetValue(e.NodeId, out var cue)) return;
		_activeCues.Remove(e.NodeId);
		cue.FadeOut();
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

	/// <summary>
	/// v0 条件评估器：所有前置条件视为满足。
	/// 后续 story 接入 ADR-0014 ConditionEvaluator 后替换。
	/// </summary>
	private sealed class AlwaysTrueConditionEvaluator : IInsightConditionEvaluator
	{
		public bool AreMet(IReadOnlyList<InsightPrerequisite> prerequisites) => true;
	}
}
