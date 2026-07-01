using Godot;

namespace FengZhi.Ui;

/// <summary>
/// 主菜单 / 启动页面控制器。
///
/// 设计参考: design/ux/main-menu.md (UX spec v1.0, 2026-06-09)
/// 实施范围: MVP 4 按钮 (新游戏 / 读档 / 设置 / 退出); brainstorm decisions Q1=a / Q2=a / Q3=a / Q4=a
///
/// 职责:
/// 1. _Ready 时连接 4 按钮 pressed + 1 quit-confirm signal
/// 2. 入场动画: 整体 fade in 400ms + 菜单按钮 stagger 50ms × 4 (spec §10)
/// 3. 默认焦点落在 NewGameButton (SaveManager 落地后可改成 ContinueButton)
/// 4. focus_neighbor 循环 (最后一项 ↓ 回到第一项, spec §11)
/// 5. dual-focus 互斥: MouseEntered -> GrabFocus, 永远只有一个按钮高亮
///
/// 跳转目标:
/// - 新游戏 -> PrologueScenePath (= SectCompound.tscn, 序章实际起点; Q3=a 不走 NewGameFlow,
///   等难度系统 GDD 落地后再扩 NavigateToNewGameFlow() 替换跳转目标)
/// - 读档 -> 敬请期待 dialog (等 SaveManager 落地换真 LoadGameScreen)
/// - 设置 -> 敬请期待 dialog (v1.0 收尾再换真 SettingsScreen)
/// - 退出 -> QuitConfirmDialog -> GetTree().Quit()
///
/// 子画面预留 (sprint 8+):
/// - LoadGameScreen (等 SaveManager)
/// - SettingsScreen (等 settings-options.md)
/// - CreditsButton (v1.0 收尾)
/// - DifficultyScreen + Prologue 标题卡 (等难度系统 GDD + 开场过场 GDD)
/// </summary>
public partial class MainMenuGame : Control
{
	/// <summary>
	/// 新游戏跳转目标 — 当前 = 序章实际起点风止山院.
	/// Q3=a 决策: 不走 NewGameFlow (难度选择 / 开场过场 GDD 未落地, 走 placeholder 会做白工);
	/// 等 GDD 落地后只需修改此常量或新增 NavigateToNewGameFlow() 走子流程画面.
	/// </summary>
	private const string PrologueScenePath = "res://scenes/sect_compound/SectCompound.tscn";

	/// <summary>整体入场 fade in 持续 (spec §10 Slow 400ms).</summary>
	private const double FadeInDurationSec = 0.4;

	/// <summary>菜单按钮 stagger 间隔 (spec §10 stagger 50ms).</summary>
	private const double MenuStaggerSec = 0.05;

	/// <summary>每个菜单按钮入场滑入持续 (spec §10 Normal 200ms each).</summary>
	private const double MenuButtonEntrySec = 0.2;

	/// <summary>选中后退出 fade out 持续 (spec §10 Normal 200ms).</summary>
	private const double ExitFadeOutSec = 0.2;

	private Button _newGameButton = null!;
	private Button _loadGameButton = null!;
	private Button _settingsButton = null!;
	private Button _quitButton = null!;
	private VBoxContainer _menuList = null!;
	private ConfirmationDialog _quitConfirmDialog = null!;
	private AcceptDialog _placeholderDialog = null!;

	private bool _isExiting;

	public override void _Ready()
	{
		_newGameButton = GetNode<Button>("MenuList/NewGameButton");
		_loadGameButton = GetNode<Button>("MenuList/LoadGameButton");
		_settingsButton = GetNode<Button>("MenuList/SettingsButton");
		_quitButton = GetNode<Button>("MenuList/QuitButton");
		_menuList = GetNode<VBoxContainer>("MenuList");
		_quitConfirmDialog = GetNode<ConfirmationDialog>("QuitConfirmDialog");
		_placeholderDialog = GetNode<AcceptDialog>("SettingsPlaceholderDialog");

		_newGameButton.Pressed += OnNewGamePressed;
		_loadGameButton.Pressed += OnLoadGamePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_quitConfirmDialog.Confirmed += OnQuitConfirmed;

		// dual-focus 互斥: 鼠标 hover 任一按钮 -> GrabFocus 到该按钮, 让键盘焦点跟随鼠标.
		// 这样 4 个按钮永远只有一个高亮 (focus + hover 共享视觉, 见 MainMenu.tscn Theme).
		// 鼠标离开后该按钮保留 focus 状态 (玩家继续用键盘从该项导航).
		_newGameButton.MouseEntered += () => _newGameButton.GrabFocus();
		_loadGameButton.MouseEntered += () => _loadGameButton.GrabFocus();
		_settingsButton.MouseEntered += () => _settingsButton.GrabFocus();
		_quitButton.MouseEntered += () => _quitButton.GrabFocus();

		// spec §11 focus_neighbor 循环: 最后一项 ↓ 回到第一项, 第一项 ↑ 到最后一项
		_newGameButton.FocusNeighborTop = _quitButton.GetPath();
		_newGameButton.FocusNeighborBottom = _loadGameButton.GetPath();
		_loadGameButton.FocusNeighborTop = _newGameButton.GetPath();
		_loadGameButton.FocusNeighborBottom = _settingsButton.GetPath();
		_settingsButton.FocusNeighborTop = _loadGameButton.GetPath();
		_settingsButton.FocusNeighborBottom = _quitButton.GetPath();
		_quitButton.FocusNeighborTop = _settingsButton.GetPath();
		_quitButton.FocusNeighborBottom = _newGameButton.GetPath();

		PlayEntryAnimation();

		GD.Print("[MainMenu] Ready. Entry animation kicked off.");
	}

	/// <summary>
	/// spec §10 入场动画:
	/// - 整体 fade in 400ms (Modulate.a 0 -> 1)
	/// - 菜单 4 按钮 stagger 50ms 间隔逐项 alpha 0 -> 1, 每个 200ms
	///
	/// 注: spec 原文有 "stagger 从下方滑入" 但 VBoxContainer 管理子节点 Position,
	/// 手动改子节点 position 会被 layout 覆盖, 这里改成纯 alpha stagger 也能传达
	/// "逐项入场" 的视觉感. 滑入版可留给后续 Margin/Anchor 重构 (cu-feel 类 polish).
	/// </summary>
	private void PlayEntryAnimation()
	{
		// 整体起点全透明
		Modulate = new Color(1, 1, 1, 0);

		// 菜单按钮各自先全透明 (不动 Position, VBox 管布局)
		var buttons = new[] { _newGameButton, _loadGameButton, _settingsButton, _quitButton };
		foreach (var btn in buttons)
		{
			btn.Modulate = new Color(1, 1, 1, 0);
		}

		var tween = CreateTween();
		tween.SetParallel(false);

		// 1. 整体 fade in 400ms
		tween.TweenProperty(this, "modulate:a", 1.0f, FadeInDurationSec)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		// 2. 菜单按钮 stagger alpha 入场 (fade in 完后开始, 各 stagger 并行)
		for (int i = 0; i < buttons.Length; i++)
		{
			var btn = buttons[i];
			double delaySec = i * MenuStaggerSec;
			tween.Parallel()
				.TweenProperty(btn, "modulate:a", 1.0f, MenuButtonEntrySec)
				.SetDelay(delaySec)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);
		}

		// 入场动画完成后 grab focus (spec §11 入场后自动 grab_focus 到首个可用项)
		tween.TweenCallback(Callable.From(() => _newGameButton.GrabFocus()));
	}

	private void OnNewGamePressed()
	{
		if (_isExiting)
			return;
		_isExiting = true;
		GD.Print($"[MainMenu] NewGame pressed -> {PrologueScenePath}");
		PlayExitFadeAndChangeScene(PrologueScenePath);
	}

	private void OnLoadGamePressed()
	{
		if (_isExiting)
			return;
		// 复用同一 dialog 节点, 动态切 title + text (placeholder 阶段都是 "敬请期待")
		_placeholderDialog.Title = "读档";
		_placeholderDialog.DialogText = "存档系统将在 SaveManager 落地后开放。\n（敬请期待）";
		_placeholderDialog.PopupCentered();
	}

	private void OnSettingsPressed()
	{
		if (_isExiting)
			return;
		var settingsScene = GD.Load<PackedScene>("res://scenes/ui/SettingsPanel.tscn");
		var instance = settingsScene.Instantiate<Control>();
		AddChild(instance);
	}

	private void OnQuitPressed()
	{
		if (_isExiting)
			return;
		_quitConfirmDialog.PopupCentered();
	}

	private void OnQuitConfirmed()
	{
		_isExiting = true;
		GD.Print("[MainMenu] Quit confirmed -> GetTree().Quit()");
		GetTree().Quit();
	}

	/// <summary>
	/// spec §10 选中后退出: 整体 fade out 200ms ease-in -> ChangeSceneToFile.
	/// </summary>
	private void PlayExitFadeAndChangeScene(string scenePath)
	{
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0.0f, ExitFadeOutSec)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() =>
		{
			var err = GetTree().ChangeSceneToFile(scenePath);
			if (err != Error.Ok)
				GD.PushError($"[MainMenu] ChangeSceneToFile('{scenePath}') failed: {err}");
		}));
	}
}
