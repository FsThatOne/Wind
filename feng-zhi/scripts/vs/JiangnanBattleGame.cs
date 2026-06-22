using System.Linq;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.Combat.Fixtures;
using FengZhi.Foundation.Combat.Runtime;
using FengZhi.Foundation.CombatUi;
using Godot;

namespace FengZhi.Vs;

/// <summary>
/// 江南战斗 scene 控制器（VS Sprint 7 MVP-A · 真实 Lite Xingqi 战斗）。
///
/// 流程：
/// 1. _Ready 初始化 BattleFacade + BattleEventBus + VsBattleLoopController + CombatUiEventAdapter
/// 2. 订阅 BattleEndEvent → 调 JiangnanFlowController.GoToOutcome(result)
/// 3. controller.Start() 推到 PlayerDecision
/// 4. 玩家点「罗汉拳 (轻)」或「铁臂横拦 (重)」按钮 → SubmitPlayerIntent(action)
/// 5. _Process 中 RefreshIfDirty → 应用 snapshot 到 HP/Neixi 显示
/// 6. 战斗结束 → 自动跳到 outcome scene
///
/// MVP-A 范围说明（spec §2.1 deviation 2026-06-22 23:55）：
/// - 用 2 个简单 Button 代替 CombatMoveSelectionPanel（cu-004 集成推到 cu-visual-evidence）
/// - 用 自有 Label 代替 CombatHudPanel（cu-001..003 集成同上）
/// - 仍复用 BattleFacade + BattleEventBus + CombatUiEventAdapter + ResolutionService
/// </summary>
public partial class JiangnanBattleGame : Node2D
{
	private BattleEventBus _bus = null!;
	private VsBattleLoopController _controller = null!;
	private CombatUiEventAdapter _adapter = null!;

	private Label _playerHpLabel = null!;
	private Label _playerNeixiLabel = null!;
	private Label _banditHpLabel = null!;
	private Label _banditNeixiLabel = null!;
	private Label _statusLabel = null!;
	private Button _lightButton = null!;
	private Button _heavyButton = null!;

	public override void _Ready()
	{
		_playerHpLabel = GetNode<Label>("UiLayer/CombatPanel/PlayerStats/HpLabel");
		_playerNeixiLabel = GetNode<Label>("UiLayer/CombatPanel/PlayerStats/NeixiLabel");
		_banditHpLabel = GetNode<Label>("UiLayer/CombatPanel/BanditStats/HpLabel");
		_banditNeixiLabel = GetNode<Label>("UiLayer/CombatPanel/BanditStats/NeixiLabel");
		_statusLabel = GetNode<Label>("UiLayer/CombatPanel/StatusLabel");
		_lightButton = GetNode<Button>("UiLayer/CombatPanel/ActionsPanel/LightButton");
		_heavyButton = GetNode<Button>("UiLayer/CombatPanel/ActionsPanel/HeavyButton");

		_lightButton.Pressed += () => OnPlayerAttackPressed(useHeavy: false);
		_heavyButton.Pressed += () => OnPlayerAttackPressed(useHeavy: true);

		var config = JiangnanBandit1v1Fixture.CreateBattleConfig();
		var facade = new BattleFacade();
		var battle = facade.InitiateBattle(config);
		_bus = new BattleEventBus();
		var enemyAI = JiangnanBandit1v1Fixture.CreateBanditAI();
		_controller = new VsBattleLoopController(battle, _bus, enemyAI);
		_adapter = new CombatUiEventAdapter(_bus);
		_adapter.EnterBattle();

		_bus.Subscribe<BattleEndEvent>(OnBattleEnd);

		RefreshHudFromCombatants();
		_statusLabel.Text = "观气";

		_controller.Start();
		ApplySnapshotIfDirty();
		UpdateButtonsForState();

		GD.Print("[JiangnanBattle] Ready. Battle started.");
	}

	public override void _Process(double delta)
	{
		ApplySnapshotIfDirty();
	}

	private void OnPlayerAttackPressed(bool useHeavy)
	{
		if (!_controller.WaitingForPlayer || _controller.IsFinished)
		{
			return;
		}

		var action = useHeavy
			? new BattleAction
			{
				ActorId = JiangnanBandit1v1Fixture.ProtagonistId,
				Type = ActionType.Move,
				TargetId = JiangnanBandit1v1Fixture.BanditId,
				MoveId = JiangnanBandit1v1Fixture.HeavyStrikeMoveId,
				MoveType = MoveType.Gang,
				NeixiCost = 4,
			}
			: new BattleAction
			{
				ActorId = JiangnanBandit1v1Fixture.ProtagonistId,
				Type = ActionType.Move,
				TargetId = JiangnanBandit1v1Fixture.BanditId,
				MoveId = JiangnanBandit1v1Fixture.LightStrikeMoveId,
				MoveType = MoveType.Gang,
				NeixiCost = 2,
			};

		var protagonist = GetCombatant(JiangnanBandit1v1Fixture.ProtagonistId);
		if (protagonist != null && protagonist.Neixi < action.NeixiCost)
		{
			_statusLabel.Text = "内息不足，自动调息";
			action = new BattleAction
			{
				ActorId = JiangnanBandit1v1Fixture.ProtagonistId,
				Type = ActionType.Breathe,
			};
		}
		else
		{
			_statusLabel.Text = "结算";
		}

		_controller.SubmitPlayerIntent(action);

		ApplySnapshotIfDirty();
		RefreshHudFromCombatants();
		UpdateButtonsForState();

		if (!_controller.IsFinished)
		{
			_statusLabel.Text = "观气";
		}
	}

	private void OnBattleEnd(BattleEndEvent evt)
	{
		_statusLabel.Text = "战斗结束";
		GD.Print($"[JiangnanBattle] BattleEnd received. Result = {evt.Result}");
		var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
		flow?.GoToOutcome(evt.Result);
	}

	private void ApplySnapshotIfDirty()
	{
		if (!_adapter.RefreshIfDirty())
		{
			return;
		}

		var snapshot = _adapter.GetSnapshot();
		_ = snapshot; // MVP-A 暂不消费 snapshot 字段；预留给 cu-visual-evidence 阶段做完整 panel 集成
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
		return _controller.Battle.PlayerParty.Concat(_controller.Battle.EnemyGroup)
			.FirstOrDefault(c => c.Id == id);
	}

	private void UpdateButtonsForState()
	{
		bool enabled = _controller.WaitingForPlayer && !_controller.IsFinished;
		_lightButton.Disabled = !enabled;
		_heavyButton.Disabled = !enabled;
	}

	public override void _ExitTree()
	{
		_adapter?.Dispose();
		_bus?.ClearAll();
		base._ExitTree();
	}
}
