using System;
using System.Collections.Generic;
using System.Linq;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.Fixtures;
using FengZhi.Foundation.Combat.Runtime;
using FengZhi.Foundation.Combat.Board;
using FengZhi.Foundation.Combat.Xingqi;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.MartialArts;
using FengZhi.Foundation.Presentation.Shared;
using FengZhi.Vs.Combat;
using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南战斗 scene 控制器（VS Sprint 7 · cu-004-vs-integration 起 · 真实 Lite Xingqi 战斗）。
///
/// 流程：
/// 1. _Ready 初始化 BattleFacade + BattleEventBus + VsBattleLoopController + CombatUiEventAdapter
///    + CombatMoveSelectionBinder（封装 Foundation CombatMoveSelectionPanel + Presenter）
/// 2. 订阅 BattleEndEvent → 调 JiangnanFlowController.GoToOutcome(result)
/// 3. controller.Start() 推到 PlayerDecision，binder.OpenForPlayerDecision()
/// 4. 玩家用 D-pad / 方向键导航招式面板，按 ui_accept 提交 → binder 翻译成 BattleAction → controller.SubmitPlayerIntent
/// 5. _Process 中 RefreshIfDirty → 应用 snapshot 到 HP/Neixi 显示
/// 6. 战斗结束 → 自动跳到 outcome scene
///
/// cu-004-vs-integration 范围（2026-06-24）：
/// - 用 CombatMoveSelectionBinder + Foundation CombatMoveSelectionPanel 替换原 2 个简单 Button
/// - demo seed: JiangnanBandit1v1Fixture.CreateDemoConfig_Cu004Showcase()（6 装备 + 心法专属 + 置灰原因）
/// - cu-005/006/008 集成在后续子 story 扩 binder，本文件不再大改
/// </summary>
public partial class JiangnanBattleGame : Node2D
{
	/// <summary>
	/// Demo seed 选择（cu-visual-evidence harness 子 story E 预留入口；cu-005-vs-integration 起就启用）。
	///   ""       → 真实战斗 fixture（无 demo 注入；hotkey 也无效）
	///   "cu-004" → cu-004 demo seed（6 招 + 心法封印 + 无道具置灰）
	///   "cu-005" → cu-005 demo seed（同 cu-004 + 决胜目标预置 + 反制启用边界 + 内息 hotkey 切换）
	///   "cu-006" → cu-006 demo seed（敌方破绽预置阈值 + 决胜目标预置 → 第一回合按 Enter 直接演 7-phase）
	///   "cu-008" → cu-008 demo seed（dual-focus + hover/focus 双背景层 + InputMode 自动切换）
	/// </summary>
	[Export] public string DemoSeed { get; set; } = "cu-004";

	/// <summary>
	/// 行气模式开关。true 时使用 XingqiBattleLoopController（脉冲驱动逐一行动）。
	/// </summary>
	[Export] public bool UseXingqiMode { get; set; }

	private BattleEventBus _bus = null!;
	private VsBattleLoopController? _controller;
	private XingqiBattleLoopController? _xingqiController;
	private CombatUiEventAdapter? _adapter;
	private CombatMoveSelectionBinder _moveBinder = null!;
	private DecisiveStrikeGodotAdapter _decisiveAdapter = null!;

	private Label _playerHpLabel = null!;
	private Label _playerNeixiLabel = null!;
	private Label _banditHpLabel = null!;
	private Label _banditNeixiLabel = null!;
	private Label _statusLabel = null!;
	private Control _moveSelectionMount = null!;
	private Camera2D _mainCamera = null!;
	private CanvasLayer _decisiveOverlay = null!;
	private Sprite2D _playerSprite = null!;
	private Sprite2D _banditSprite = null!;
	private ProgressBar? _playerXingqiBar;
	private ProgressBar? _banditXingqiBar;

	private string _protagonistId = null!;
	private string _banditId = null!;

	private bool _isCu005Demo;
	private bool _isCu006Demo;
	private int _cu006PrecomputedDamage;
	private InputMode? _lastInputMode;

	public override void _Ready()
	{
		_playerHpLabel = GetNode<Label>("UiLayer/CombatPanel/PlayerStats/HpLabel");
		_playerNeixiLabel = GetNode<Label>("UiLayer/CombatPanel/PlayerStats/NeixiLabel");
		_banditHpLabel = GetNode<Label>("UiLayer/CombatPanel/BanditStats/HpLabel");
		_banditNeixiLabel = GetNode<Label>("UiLayer/CombatPanel/BanditStats/NeixiLabel");
		_statusLabel = GetNode<Label>("UiLayer/CombatPanel/StatusLabel");
		_moveSelectionMount = GetNode<Control>("UiLayer/CombatPanel/MoveSelectionMount");
		_mainCamera = GetNode<Camera2D>("MainCamera");
		_decisiveOverlay = GetNode<CanvasLayer>("DecisiveStrikeOverlay");
		_decisiveAdapter = GetNode<DecisiveStrikeGodotAdapter>("DecisiveStrikeAdapter");
		_playerSprite = GetNode<Sprite2D>("IsoBoard/PlayerSprite");
		_banditSprite = GetNode<Sprite2D>("IsoBoard/BanditSprite");
		_playerXingqiBar = GetNodeOrNull<ProgressBar>("UiLayer/CombatPanel/PlayerStats/XingqiBar");
		_banditXingqiBar = GetNodeOrNull<ProgressBar>("UiLayer/CombatPanel/BanditStats/XingqiBar");

		PaintIsoBoard();

		// === Demo seed 分发 ===
		BattleConfig battleConfig;
		IBattleAI enemyAI;
		BattlePanelDisplayData panelDisplay;
		bool isXinfaSealed;
		int usableCombatItemCount;
		string[]? preloadedDecisiveTargets = null;
		int? initialDemoNeixi = null;
		int? enemyStaggerOverride = null;

		switch (DemoSeed)
		{
			case "xingqi":
				{
					battleConfig = XingqiDemoFixture.CreateXingqiBattleConfig();
					enemyAI = XingqiDemoFixture.CreateBanditAI();
					panelDisplay = new BattlePanelDisplayData { Entries = Array.Empty<BattlePanelMoveEntry>() };
					isXinfaSealed = false;
					usableCombatItemCount = 0;
					break;
				}
			case "movement":
				{
					battleConfig = MovementDemoFixture.CreateBattleConfig();
					enemyAI = MovementDemoFixture.CreateBanditAI();
					panelDisplay = new BattlePanelDisplayData { Entries = Array.Empty<BattlePanelMoveEntry>() };
					isXinfaSealed = false;
					usableCombatItemCount = 0;
					break;
				}
			case "cu-005":
				{
					var seed = JiangnanBandit1v1Fixture.CreateDemoConfig_Cu005Showcase();
					battleConfig = seed.BattleConfig;
					enemyAI = JiangnanBandit1v1Fixture.CreateBanditAICu005();
					panelDisplay = seed.PanelDisplay;
					isXinfaSealed = seed.IsXinfaSealed;
					usableCombatItemCount = seed.UsableCombatItemCount;
					preloadedDecisiveTargets = seed.PreloadedDecisiveTargetIds.ToArray();
					initialDemoNeixi = seed.InitialPlayerNeixi;
					_isCu005Demo = true;
					break;
				}
			case "cu-006":
				{
					var seed = JiangnanBandit1v1Fixture.CreateDemoConfig_Cu006Showcase();
					battleConfig = seed.BattleConfig;
					enemyAI = JiangnanBandit1v1Fixture.CreateBanditAI();
					panelDisplay = seed.PanelDisplay;
					isXinfaSealed = seed.IsXinfaSealed;
					usableCombatItemCount = seed.UsableCombatItemCount;
					preloadedDecisiveTargets = seed.PreloadedDecisiveTargetIds.ToArray();
					initialDemoNeixi = seed.InitialPlayerNeixi;
					enemyStaggerOverride = seed.EnemyInitialStaggerOverride;
					_isCu006Demo = true;
					break;
				}
			case "cu-008":
				{
					var seed = JiangnanBandit1v1Fixture.CreateDemoConfig_Cu008Showcase();
					battleConfig = seed.BattleConfig;
					enemyAI = JiangnanBandit1v1Fixture.CreateBanditAI();
					panelDisplay = seed.PanelDisplay;
					isXinfaSealed = seed.IsXinfaSealed;
					usableCombatItemCount = seed.UsableCombatItemCount;
					preloadedDecisiveTargets = seed.PreloadedDecisiveTargetIds.ToArray();
					initialDemoNeixi = seed.InitialPlayerNeixi;
					if (seed.PrefersGamepadOnlyHint)
						GD.Print("[JiangnanBattle] cu-008 demo: 建议拔鼠标或用方向键模拟 D-pad 录制 dual-focus.");
					break;
				}
			case "cu-004":
			default:
				{
					var seed = JiangnanBandit1v1Fixture.CreateDemoConfig_Cu004Showcase();
					battleConfig = seed.BattleConfig;
					enemyAI = JiangnanBandit1v1Fixture.CreateBanditAI();
					panelDisplay = seed.PanelDisplay;
					isXinfaSealed = seed.IsXinfaSealed;
					usableCombatItemCount = seed.UsableCombatItemCount;
					break;
				}
		}

		var facade = new BattleFacade();
		var battle = facade.InitiateBattle(battleConfig);
		_bus = new BattleEventBus();

		if (UseXingqiMode)
		{
			InitXingqiMode(battleConfig, enemyAI);
			return;
		}

		_controller = new VsBattleLoopController(battle, _bus, enemyAI);
		_adapter = new CombatUiEventAdapter(_bus);
		_adapter.EnterBattle();

		// cu-006 demo: 在 facade.InitiateBattle 之后、controller.Start() 之前预置敌方破绽到阈值，
		// 让玩家第一回合就能在 panel 顶部看到"决胜一击"行 + 按 Enter 立即触发 7-phase 演出
		if (enemyStaggerOverride.HasValue)
		{
			var enemy = battle.EnemyGroup.FirstOrDefault();
			enemy?.AddStagger(enemyStaggerOverride.Value);
#if DEBUG
			AssertDemo(enemy != null && enemy.Stagger >= enemyStaggerOverride.Value,
				$"cu-006 AddStagger 后敌方破绽必须 ≥{enemyStaggerOverride.Value} (实际 = {enemy?.Stagger})");
#endif
		}

		// cu-006 一击决胜 Godot 适配器初始化：注入 BattleEventBus + Camera2D + Overlay 节点
		_decisiveAdapter.Initialize(_bus, _mainCamera, _decisiveOverlay);
		_decisiveAdapter.RegisterTarget(JiangnanBandit1v1Fixture.ProtagonistId, () => _playerSprite.GlobalPosition);
		_decisiveAdapter.RegisterTarget(JiangnanBandit1v1Fixture.BanditId, () => _banditSprite.GlobalPosition);
		_decisiveAdapter.SequenceCompleted = OnDecisiveSequenceCompleted;
		_decisiveAdapter.SequenceCancelled = OnDecisiveSequenceCancelled;

		_moveBinder = new CombatMoveSelectionBinder(
			_bus,
			_moveSelectionMount,
			actorId: JiangnanBandit1v1Fixture.ProtagonistId,
			defaultTargetId: JiangnanBandit1v1Fixture.BanditId,
			submitAction: SubmitPlayerAction);
		_moveBinder.SetDisplayData(panelDisplay, isXinfaSealed, usableCombatItemCount);

		_bus.Subscribe<BattleEndEvent>(OnBattleEnd);
		// cu-006: 监听结算阶段发出的决胜 DamageDealtEvent → 触发 7-phase 演出
		// (Foundation Combat 结算已经把 PrecomputedDamage 算好并应用了, 演出只承担视觉)
		_bus.Subscribe<DamageDealtEvent>(OnDamageDealtForDecisive);

		RefreshHudFromCombatants();
		_statusLabel.Text = "观气";

		_controller!.Start();

		// cu-005 demo seed: 预置决胜目标 + 覆盖 UI 内息显示（必须在 controller.Start() 后做，
		// 因为 Start 会触发 RoundStartEvent 让 binder 清空 _decisiveStrikeTargetIds）
		if (preloadedDecisiveTargets != null && preloadedDecisiveTargetsHasItems(preloadedDecisiveTargets))
			_moveBinder.SetDecisiveStrikeTargets(preloadedDecisiveTargets);
		if (initialDemoNeixi.HasValue)
			_moveBinder.OverridePlayerNeixiForDemo(initialDemoNeixi.Value);

		ApplySnapshotIfDirty();
		UpdateMovePanelForState();

#if DEBUG
		// cu-visual-evidence-harness · E.4: state assertion fail-fast 防 demo seed 漂移
		// 在 _Ready 末尾跑, 让录屏者在录到一半之前就发现状态不对
		AssertDemoState(panelDisplay, isXinfaSealed, usableCombatItemCount,
			preloadedDecisiveTargets, initialDemoNeixi, enemyStaggerOverride,
			battle.EnemyGroup.FirstOrDefault());
#endif

		GD.Print($"[JiangnanBattle] Ready. DemoSeed='{DemoSeed}'.");
	}

	private static bool preloadedDecisiveTargetsHasItems(string[] arr) => arr.Length > 0;

	/// <summary>
	/// cu-visual-evidence-harness · E.4 fail-fast state assertion.
	/// 在 _Ready 末尾验证 demo seed 输出的关键状态没有漂移，避免录屏者录到一半发现状态不对。
	/// 每个 DemoSeed 的不变量来自 docs/superpowers/specs/2026-06-24-cu-visual-evidence-harness.md。
	/// </summary>
	private void AssertDemoState(
		BattlePanelDisplayData panelDisplay,
		bool isXinfaSealed,
		int usableCombatItemCount,
		string[]? preloadedDecisiveTargets,
		int? initialDemoNeixi,
		int? enemyStaggerOverride,
		BattleCombatant? enemy)
	{
		if (string.IsNullOrEmpty(DemoSeed))
			return;

		int entryCount = panelDisplay.Entries?.Count ?? 0;
		bool hasXinfaExclusive = panelDisplay.Entries?.Any(e => e.Source == MoveSource.XinfaExclusive) ?? false;
		bool hasDecisiveTargets = preloadedDecisiveTargets is { Length: > 0 };

		switch (DemoSeed)
		{
			case "cu-004":
				AssertDemo(entryCount >= 6, "cu-004 需要 ≥6 招式 (实际 = " + entryCount + ")");
				AssertDemo(isXinfaSealed, "cu-004 需要 IsXinfaSealed=true 才能演心法封印置灰");
				AssertDemo(usableCombatItemCount == 0, "cu-004 需要 UsableCombatItemCount=0 才能演无道具置灰");
				AssertDemo(hasXinfaExclusive, "cu-004 需要至少 1 个 MoveSource.XinfaExclusive 招式演心法角标");
				break;
			case "cu-005":
				AssertDemo(entryCount >= 6, "cu-005 需要 ≥6 招式 (复用 cu-004 panel, 实际 = " + entryCount + ")");
				AssertDemo(hasDecisiveTargets, "cu-005 需要 PreloadedDecisiveTargetIds 非空演决胜行");
				AssertDemo(initialDemoNeixi == 3, "cu-005 需要 InitialPlayerNeixi=3 (反制启用边界) 实际 = " + (initialDemoNeixi?.ToString() ?? "null"));
				break;
			case "cu-006":
				AssertDemo(enemyStaggerOverride == 5, "cu-006 需要 EnemyInitialStaggerOverride=5 实际 = " + (enemyStaggerOverride?.ToString() ?? "null"));
				// stagger 实际值断言已提前到 AddStagger 之后、Start() 之前，避免回合逻辑消耗后误报
				AssertDemo(hasDecisiveTargets, "cu-006 需要 PreloadedDecisiveTargetIds 非空让 panel 顶部显示决胜行");
				break;
			case "cu-008":
				AssertDemo(entryCount >= 6, "cu-008 需要 ≥6 招式 (dual-focus 循环 ≥3 行才有意义, 实际 = " + entryCount + ")");
				AssertDemo(hasDecisiveTargets, "cu-008 需要决胜行参与 focus_neighbor (走访所有可交互行)");
				AssertDemo(initialDemoNeixi.HasValue && initialDemoNeixi.Value >= 6,
					"cu-008 需要 InitialPlayerNeixi≥6 让多招可用让循环穿过更多有效行 (实际 = " + (initialDemoNeixi?.ToString() ?? "null") + ")");
				break;
		}
	}

	private static void AssertDemo(bool condition, string message)
	{
		if (condition)
			return;
		GD.PushError($"[JiangnanBattle.AssertDemoState] FAIL: {message}");
		throw new InvalidOperationException($"[demo state assertion] {message}");
	}

	/// <summary>
	/// cu-008 dual-focus: 把 InputEvent 设备类型映射到 InputMode，并在变化时通知 binder。
	/// </summary>
	private void DetectAndApplyInputMode(InputEvent @event)
	{
		InputMode? mode = @event switch
		{
			InputEventMouseMotion or InputEventMouseButton => InputMode.Mouse,
			InputEventJoypadButton or InputEventJoypadMotion => InputMode.Gamepad,
			InputEventKey { Echo: false } => InputMode.Keyboard,
			_ => null,
		};

		if (mode is null || mode == _lastInputMode)
			return;

		_lastInputMode = mode;
		_moveBinder.SetInputMode(mode.Value);
	}

	public override void _Process(double delta)
	{
		ApplySnapshotIfDirty();

		// Headless 自动提交：无头模式下自动让玩家操作，用于查看完整循环日志
		if (UseXingqiMode && DisplayServer.GetName() == "headless"
			&& _xingqiController is { WaitingForPlayer: true, IsFinished: false })
		{
			if (_xingqiController.CurrentSubPhase == TurnSubPhase.WaitingForMovement)
			{
				// 自动选择原地停留
				_xingqiController.SubmitPlayerMovement(null);
			}
			else if (_xingqiController.CurrentSubPhase == TurnSubPhase.WaitingForAction)
			{
				string actorId = DemoSeed == "movement"
					? MovementDemoFixture.ProtagonistId
					: XingqiDemoFixture.ProtagonistId;
				string targetId = DemoSeed == "movement"
					? MovementDemoFixture.BanditId
					: XingqiDemoFixture.BanditId;
				var autoAction = new BattleAction
				{
					ActorId = actorId,
					Type = ActionType.Move,
					TargetId = targetId,
					MoveId = "luo_han_quan",
					MoveType = MoveType.Gang,
					NeixiCost = 2,
				};
				SubmitXingqiPlayerAction(autoAction);
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		// 使用 _Input 而非 _UnhandledInput：slot 有 FocusMode.All，
		// Godot 内建焦点系统会在 GUI 层消费方向键，导致 _UnhandledInput 收不到。
		// _Input 在 GUI 处理之前执行，确保我们的导航代码优先响应。

		// cu-006 AC-2: 演出期间屏蔽除 whitelist (ui_pause/ui_system_back) 之外的所有输入
		// 必须放在 _moveBinder.IsOpen 检查之前 — binder.Close 后才进入演出阶段，
		// 但 cinematic lock 可能在 binder 关闭后继续生效（Phase1-Completed 全程）
		if (_decisiveAdapter.ShouldConsumeInput(@event))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!_moveBinder.IsOpen || (UseXingqiMode ? _xingqiController?.IsFinished ?? true : _controller?.IsFinished ?? true))
			return;

		// cu-008 dual-focus: 检测输入设备类型自动切 InputMode（仅在变化时通知 binder）
		DetectAndApplyInputMode(@event);

		if (@event.IsActionPressed("ui_accept"))
		{
			GetViewport().SetInputAsHandled();
			_statusLabel.Text = "结算";
			_moveBinder.ConfirmFocused();
			return;
		}

		if (@event.IsActionPressed("ui_up"))
		{
			GetViewport().SetInputAsHandled();
			_moveBinder.NavigateUp();
			return;
		}

		if (@event.IsActionPressed("ui_down"))
		{
			GetViewport().SetInputAsHandled();
			_moveBinder.NavigateDown();
			return;
		}

		// cu-005 demo hotkey: [ / ] 调整 UI 内息显示值，演反制 enable/disable 切换
		if (_isCu005Demo && @event is InputEventKey { Pressed: true } keyEvt)
		{
			switch (keyEvt.Keycode)
			{
				case Key.Bracketleft:
					GetViewport().SetInputAsHandled();
					_moveBinder.OverridePlayerNeixiForDemo(_moveBinder.CurrentDisplayNeixi - 1);
					break;
				case Key.Bracketright:
					GetViewport().SetInputAsHandled();
					_moveBinder.OverridePlayerNeixiForDemo(_moveBinder.CurrentDisplayNeixi + 1);
					break;
			}
		}
	}

	private bool SubmitPlayerAction(BattleAction action)
	{
		if (_controller == null || !_controller.WaitingForPlayer || _controller.IsFinished)
			return false;

		var protagonist = GetCombatant(JiangnanBandit1v1Fixture.ProtagonistId);
		if (action.Type == ActionType.Move
			&& protagonist != null
			&& protagonist.Neixi < action.NeixiCost)
		{
			_statusLabel.Text = "内息不足，自动调息";
			action = new BattleAction
			{
				ActorId = JiangnanBandit1v1Fixture.ProtagonistId,
				Type = ActionType.Breathe,
			};
		}

		var accepted = _controller.SubmitPlayerIntent(action);
		if (!accepted)
			return false;

		ApplySnapshotIfDirty();
		RefreshHudFromCombatants();
		UpdateMovePanelForState();

		if (_controller is { IsFinished: false })
			_statusLabel.Text = "观气";
		return true;
	}

	/// <summary>
	/// cu-006 决胜演出触发点：监听 Foundation Combat 结算完成后发的 DamageDealtEvent，
	/// 看到 VisualRelation == Decisive 即代表一击决胜已结算 (PrecomputedDamage = evt.Amount)，
	/// 直接转发到 adapter.RequestDecisive 启动 7-phase 演出。
	/// </summary>
	private void OnDamageDealtForDecisive(DamageDealtEvent evt)
	{
		if (evt.VisualRelation != DamageVisualRelation.Decisive)
			return;
		// MoveType 当前从 ResolvedAction 丢失（DamageDealtEvent 不带 MoveType）；
		// cu-006 BUILD 没有体系动画资源，默认 Gang，phase 演出中只用作 placeholder 文本
		_decisiveAdapter.RequestDecisive(evt.SourceId, evt.TargetId, evt.Amount, MoveType.Gang);
		_statusLabel.Text = "一击决胜";
	}

	private void OnDecisiveSequenceCompleted()
	{
		_statusLabel.Text = "演出结束 · 观气";
	}

	private void OnDecisiveSequenceCancelled()
	{
		_statusLabel.Text = "演出中断 · 观气";
	}

	private void OnBattleEnd(BattleEndEvent evt)
	{
		_statusLabel.Text = "战斗结束";
		_moveBinder.Close();
		GD.Print($"[JiangnanBattle] BattleEnd received. Result = {evt.Result}");
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToOutcome(evt.Result);
	}

	private void ApplySnapshotIfDirty()
	{
		if (_adapter == null || !_adapter.RefreshIfDirty())
			return;

		var snapshot = _adapter.GetSnapshot();
		_ = snapshot; // cu-001..003 (HUD widget) 仍由 cu-visual-evidence 后续子 story 集成
		RefreshHudFromCombatants();
	}

	private void RefreshHudFromCombatants()
	{
		var player = GetCombatant(JiangnanBandit1v1Fixture.ProtagonistId);
		var bandit = GetCombatant(JiangnanBandit1v1Fixture.BanditId);

		if (player != null)
		{
			_playerHpLabel.Text = $"主角 HP {player.HP} / {player.MaxHP}";
			_playerNeixiLabel.Text = $"内息 {player.Neixi} / {player.MaxNeixi}";
		}
		if (bandit != null)
		{
			_banditHpLabel.Text = $"江湖小贼 HP {bandit.HP} / {bandit.MaxHP}";
			_banditNeixiLabel.Text = $"内息 {bandit.Neixi} / {bandit.MaxNeixi}";
		}
	}

	private BattleCombatant? GetCombatant(string id)
	{
		if (_controller == null) return null;
		return _controller.Battle.PlayerParty.Concat(_controller.Battle.EnemyGroup)
			.FirstOrDefault(c => c.Id == id);
	}

	private void UpdateMovePanelForState()
	{
		if (_controller == null || _controller.IsFinished)
		{
			_moveBinder.Close();
			return;
		}

		if (_controller.WaitingForPlayer)
		{
			var protagonist = GetCombatant(JiangnanBandit1v1Fixture.ProtagonistId);
			_moveBinder.OpenForPlayerDecision(protagonist?.Neixi ?? 0);
		}
		else
		{
			_moveBinder.Close();
		}
	}

	public override void _ExitTree()
	{
		_moveBinder?.Dispose();
		_adapter?.Dispose();
		_bus?.ClearAll();
		base._ExitTree();
	}

	private void PaintIsoBoard()
	{
		var tileMap = GetNodeOrNull<TileMapLayer>("IsoBoard/TileMap");
		if (tileMap == null || tileMap.TileSet == null)
		{
			return;
		}

		var dryStone = new Vector2I(0, 0);
		var wornPath = new Vector2I(1, 2);
		var mossMarked = new Vector2I(3, 0);
		var darkRear = new Vector2I(3, 3);
		var source = 0;

		for (int x = -1; x <= 3; x++)
		{
			for (int y = -1; y <= 3; y++)
			{
				Vector2I atlas;
				if (x == 1 && y == 1) atlas = mossMarked;
				else if ((x == -1 && y == 3) || (x == 3 && y == -1)) atlas = darkRear;
				else if (x + y == 2) atlas = wornPath;
				else atlas = dryStone;

				tileMap.SetCell(new Vector2I(x, y), source, atlas);
			}
		}
	}

	// ========== 行气模式 ==========

	private void InitXingqiMode(BattleConfig battleConfig, IBattleAI enemyAI)
	{
		var xingqiConfig = DemoSeed == "movement"
			? MovementDemoFixture.CreateXingqiConfig()
			: XingqiDemoFixture.CreateXingqiConfig();

		var playerParty = battleConfig.PlayerParty.Select(c => CreateCombatantFromConfig(c)).ToList();
		var enemyGroup = battleConfig.EnemyGroup.Select(c => CreateCombatantFromConfig(c)).ToList();

		BattleGrid? grid = null;
		if (DemoSeed == "movement")
		{
			grid = MovementDemoFixture.CreateDemoGrid();
		}

		_xingqiController = new XingqiBattleLoopController(
			playerParty, enemyGroup, _bus, enemyAI, xingqiConfig, grid: grid);

		// 面板
		var panelDisplay = new BattlePanelDisplayData
		{
			Entries = new[]
			{
				new BattlePanelMoveEntry
				{
					MoveId = "luo_han_quan", Name = "罗汉拳 · 轻击",
					Source = MoveSource.BaseSlot, ColorTheme = TypeColorTheme.WarmGold,
					NeixiCost = 2, EffectiveMultiplier = 1.0f,
					TriggerConditions = new[] { "always" }, SpecialEffects = new[] { "稳定输出" },
				},
				new BattlePanelMoveEntry
				{
					MoveId = "tie_bi_heng_lan", Name = "铁臂横拦 · 重击",
					Source = MoveSource.BaseSlot, ColorTheme = TypeColorTheme.WarmGold,
					NeixiCost = 4, EffectiveMultiplier = 1.4f,
					TriggerConditions = new[] { "always" }, SpecialEffects = new[] { "破绽 +1" },
				},
			}
		};

		string actorId = DemoSeed == "movement"
			? MovementDemoFixture.ProtagonistId
			: XingqiDemoFixture.ProtagonistId;
		string targetId = DemoSeed == "movement"
			? MovementDemoFixture.BanditId
			: XingqiDemoFixture.BanditId;

		_protagonistId = actorId;
		_banditId = targetId;

		_moveBinder = new CombatMoveSelectionBinder(
			_bus, _moveSelectionMount,
			actorId: actorId,
			defaultTargetId: targetId,
			submitAction: SubmitXingqiPlayerAction);
		_moveBinder.SetDisplayData(panelDisplay, isXinfaSealed: false, usableCombatItemCount: 0);

		_bus.Subscribe<XingqiAdvancedEvent>(OnXingqiAdvanced);
		_bus.Subscribe<ActorTurnStartedEvent>(OnActorTurnStarted);
		_bus.Subscribe<BattleEndEvent>(OnBattleEnd);
		_bus.Subscribe<MovementRangeCalculatedEvent>(OnMovementRangeCalculated);
		_bus.Subscribe<ActorMovedEvent>(OnActorMoved);

		RefreshXingqiHud(playerParty, enemyGroup);
		_statusLabel.Text = "行气中…";

		_xingqiController.Start();
		UpdateXingqiMovePanelForState();

		GD.Print($"[JiangnanBattle] Xingqi mode ready. DemoSeed='{DemoSeed}'. Grid={(grid != null ? $"{grid.Width}x{grid.Height}" : "none")}");
	}

	private static BattleCombatant CreateCombatantFromConfig(CombatantConfig config)
	{
		return new BattleCombatant(
			config.Id, config.Name, config.MaxHP, config.MaxNeixi,
			config.AttackGang, config.AttackRou, config.AttackQiao,
			config.Defense, config.Speed, config.CritRate,
			config.InsightStat, config.NeixiRecovery,
			config.StaggerThreshold, agility: config.Agility,
			initialPosition: config.InitialPosition,
			initialFacing: config.InitialFacing,
			moveRange: config.MoveRange);
	}

	private void OnXingqiAdvanced(XingqiAdvancedEvent evt)
	{
		GD.Print($"[Xingqi] Pulse #{evt.PulseNumber}:");
		foreach (var snap in evt.Snapshots)
		{
			GD.Print($"  {snap.CombatantId}: {snap.CurrentXingqi}/{snap.Threshold} {(snap.IsReady ? "★ READY" : "")}");
			if (snap.CombatantId == _protagonistId && _playerXingqiBar != null)
			{
				_playerXingqiBar.Value = snap.CurrentXingqi;
			}
			else if (snap.CombatantId == _banditId && _banditXingqiBar != null)
			{
				_banditXingqiBar.Value = snap.CurrentXingqi;
			}
		}
	}

	private void OnActorTurnStarted(ActorTurnStartedEvent evt)
	{
		string side = evt.IsPlayerSide ? "玩家" : "敌方";
		GD.Print($"[Xingqi] >>> {side} 行动开始: {evt.ActorId}");
		if (evt.IsPlayerSide)
		{
			_statusLabel.Text = _xingqiController?.Grid != null ? "选择移动目标" : "你的回合 — 选择招式";
		}
		else
		{
			_statusLabel.Text = $"敌方行动";
		}
		RefreshXingqiHudFromController();
	}

	private void OnMovementRangeCalculated(MovementRangeCalculatedEvent evt)
	{
		GD.Print($"[Movement] 可达范围计算: {evt.ReachableCells.Count} 格, 起点={evt.Origin}");
	}

	private void OnActorMoved(ActorMovedEvent evt)
	{
		GD.Print($"[Movement] {evt.ActorId} 移动: {evt.From} → {evt.To}, 朝向={evt.NewFacing}");
	}

	private bool SubmitXingqiPlayerAction(BattleAction action)
	{
		if (_xingqiController == null || !_xingqiController.WaitingForPlayer || _xingqiController.IsFinished)
			return false;

		GD.Print($"[Xingqi] 玩家提交: {action.Type} → {action.TargetId ?? "无目标"}");
		var accepted = _xingqiController.SubmitPlayerIntent(action);
		if (!accepted) return false;

		RefreshXingqiHudFromController();
		UpdateXingqiMovePanelForState();

		if (!_xingqiController.IsFinished)
			_statusLabel.Text = "行气中…";
		return true;
	}

	private void UpdateXingqiMovePanelForState()
	{
		if (_xingqiController == null) return;

		if (_xingqiController.IsFinished)
		{
			_moveBinder?.Close();
			return;
		}

		if (_xingqiController.WaitingForPlayer)
		{
			var player = _xingqiController.PlayerParty.FirstOrDefault(c => c.Id == _protagonistId);
			_moveBinder?.OpenForPlayerDecision(player?.Neixi ?? 0);
		}
		else
		{
			_moveBinder?.Close();
		}
	}

	private void RefreshXingqiHud(IReadOnlyList<BattleCombatant> players, IReadOnlyList<BattleCombatant> enemies)
	{
		var player = players.FirstOrDefault(c => c.Id == _protagonistId);
		var bandit = enemies.FirstOrDefault(c => c.Id == _banditId);
		if (player != null)
		{
			_playerHpLabel.Text = $"主角 HP {player.HP} / {player.MaxHP}";
			_playerNeixiLabel.Text = $"内息 {player.Neixi} / {player.MaxNeixi}";
		}
		if (bandit != null)
		{
			_banditHpLabel.Text = $"江湖小贼 HP {bandit.HP} / {bandit.MaxHP}";
			_banditNeixiLabel.Text = $"内息 {bandit.Neixi} / {bandit.MaxNeixi}";
		}
	}

	private void RefreshXingqiHudFromController()
	{
		if (_xingqiController == null) return;
		RefreshXingqiHud(_xingqiController.PlayerParty, _xingqiController.EnemyGroup);
	}
}
