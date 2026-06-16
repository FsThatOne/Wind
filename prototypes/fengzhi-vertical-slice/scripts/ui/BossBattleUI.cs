// VERTICAL SLICE - Boss Battle UI
// Extends combat display with Boss-specific info: phase, charge, counter-read
using Godot;

namespace FengzhiSlice;

/// <summary>
/// Boss Battle UI - shows combat state, boss phase, and player controls.
/// Reuses signal pattern from CombatUI but connects to BossCombatManager.
/// </summary>
public partial class BossBattleUI : Control
{
    private Label? _intentLabel;
    private Label? _turnLabel;
    private Label? _phaseLabel;
    private ProgressBar? _playerHpBar;
    private ProgressBar? _playerNeiliBar;
    private ProgressBar? _enemyHpBar;
    private ProgressBar? _enemyStaggerBar;
    private Label? _playerHpText;
    private Label? _enemyHpText;
    private VBoxContainer? _moveButtons;
    private Button? _burstButton;
    private RichTextLabel? _combatLog;
    private Label? _flashLabel;

    private BossCombatManager? _manager;
    private string _logText = "";

    public override void _Ready()
    {
        _intentLabel = GetNodeOrNull<Label>("%IntentLabel");
        _turnLabel = GetNodeOrNull<Label>("%TurnLabel");
        _phaseLabel = GetNodeOrNull<Label>("%PhaseLabel");
        _playerHpBar = GetNodeOrNull<ProgressBar>("%PlayerHpBar");
        _playerNeiliBar = GetNodeOrNull<ProgressBar>("%PlayerNeiliBar");
        _enemyHpBar = GetNodeOrNull<ProgressBar>("%EnemyHpBar");
        _enemyStaggerBar = GetNodeOrNull<ProgressBar>("%EnemyStaggerBar");
        _playerHpText = GetNodeOrNull<Label>("%PlayerHpText");
        _enemyHpText = GetNodeOrNull<Label>("%EnemyHpText");
        _moveButtons = GetNodeOrNull<VBoxContainer>("%MoveButtons");
        _burstButton = GetNodeOrNull<Button>("%BurstButton");
        _combatLog = GetNodeOrNull<RichTextLabel>("%CombatLog");
        _flashLabel = GetNodeOrNull<Label>("%FlashLabel");

        if (_burstButton != null)
        {
            _burstButton.Pressed += OnBurstPressed;
            _burstButton.Visible = false;
        }
        if (_flashLabel != null) _flashLabel.Visible = false;
    }

    public void BindManager(BossCombatManager manager)
    {
        _manager = manager;
        manager.IntentRevealed += OnIntentRevealed;
        manager.WaitingForPlayerInput += OnWaitingForInput;
        manager.TurnAnimating += OnTurnAnimating;
        manager.BurstAvailable += OnBurstAvailable;
        manager.BurstExecuted += OnBurstExecuted;
        manager.CombatOver += OnCombatOver;
        manager.InvalidMoveSelected += OnInvalidMoveSelected;
        manager.BossPhaseChanged += OnPhaseChanged;
        manager.BossChargeAnnounced += OnChargeAnnounced;
        manager.BossMeditated += OnBossMeditated;
        manager.CounterReadTriggered += OnCounterRead;

        SetupMoveButtons();
        UpdateBars();

        if (_phaseLabel != null)
            _phaseLabel.Text = "阶段: 试探";
    }

    private void SetupMoveButtons()
    {
        if (_moveButtons == null || _manager == null) return;

        foreach (var child in _moveButtons.GetChildren())
            child.QueueFree();

        var moves = _manager.GetPlayerMoves();
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            var btn = new Button();
            string typeIcon = move.Type switch
            {
                MoveType.Gang => "[color=#C73E3E]刚[/color]",
                MoveType.Rou => "[color=#3A5B8C]柔[/color]",
                MoveType.Qiao => "[color=#4A9E7A]巧[/color]",
                _ => ""
            };
            btn.Text = $"【{TypeChar(move.Type)}】{move.Name} (内息:{move.NeiliCost} 伤害:{move.BaseDamage})";
            btn.CustomMinimumSize = new Vector2(320, 44);
            int idx = i;
            btn.Pressed += () => OnMoveSelected(idx);
            _moveButtons.AddChild(btn);
        }
    }

    private void OnIntentRevealed(string intentType)
    {
        if (_intentLabel != null)
        {
            string color = intentType switch
            {
                "刚" => "#C73E3E",
                "柔" => "#3A5B8C",
                "巧" => "#4A9E7A",
                _ => "#F5F0E8"
            };
            if (intentType.StartsWith("蓄力"))
                _intentLabel.Text = $"⚡ 敌方: {intentType}";
            else if (intentType == "调息")
                _intentLabel.Text = "💨 敌方: 调息回复中...";
            else
                _intentLabel.Text = $"敌方意图: {intentType}";
        }
    }

    private void OnWaitingForInput()
    {
        if (_turnLabel != null)
            _turnLabel.Text = $"第 {_manager?.TurnCount ?? 0} 回合 — 选择招式";

        RefreshMoveButtonStates();
        UpdateBars();
    }

    private void RefreshMoveButtonStates()
    {
        if (_moveButtons == null || _manager == null) return;

        int i = 0;
        var moves = _manager.GetPlayerMoves();
        var player = _manager.GetPlayer();
        foreach (var child in _moveButtons.GetChildren())
        {
            if (child is Button btn && i < moves.Length)
            {
                var move = moves[i];
                int cd = _manager.GetMoveCooldown(i);
                bool canUse = _manager.CanSelectMove(i);
                btn.Disabled = !canUse;

                string status = "";
                if (cd > 0)
                {
                    status = $" [冷却{cd}]";
                    btn.Modulate = new Color(0.4f, 0.4f, 0.5f, 0.7f);
                }
                else if (move.NeiliCost > player.CurrentNeili)
                {
                    status = " [内息不足]";
                    btn.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                else
                {
                    btn.Modulate = new Color(1, 1, 1, 1);
                }

                btn.Text = $"【{TypeChar(move.Type)}】{move.Name} (内息:{move.NeiliCost} 伤害:{move.BaseDamage}){status}";
            }
            i++;
        }
    }

    private void OnTurnAnimating(string playerMove, string enemyMove, int playerDmg, int enemyDmg, bool counter)
    {
        string counterText = counter ? " [color=#C73E3E]克制![/color]" : "";
        string line = $"你: {playerMove}{counterText} → 敌 -{playerDmg} | 敌: {enemyMove} → 你 -{enemyDmg}";
        AppendLog(line);

        if (counter) ShowFlash("克制！", "#C73E3E");

        UpdateBars();
    }

    private void OnPhaseChanged(string phaseName)
    {
        if (_phaseLabel != null)
            _phaseLabel.Text = $"阶段: {phaseName}";
        ShowFlash($"— {phaseName} —", "#FFD700");
        AppendLog($"[color=#FFD700]*** Boss 进入 {phaseName} 阶段 ***[/color]");
    }

    private void OnChargeAnnounced(string moveName)
    {
        AppendLog($"[color=#FF8C00]⚡ Boss 正在蓄力: {moveName}！下回合释放！[/color]");
        ShowFlash("蓄力中!", "#FF8C00");
    }

    private void OnBossMeditated()
    {
        AppendLog("[color=#3A5B8C]💨 Boss 调息回复了内息[/color]");
    }

    private void OnCounterRead()
    {
        AppendLog("[color=#9B59B6]👁 Boss 反读了你的招式模式！[/color]");
        ShowFlash("被反读!", "#9B59B6");
    }

    private void OnBurstAvailable()
    {
        if (_burstButton != null)
        {
            _burstButton.Visible = true;
            _burstButton.Text = "⚡ 一击决胜 (内息×3)";
        }
    }

    private void OnBurstExecuted(int damage)
    {
        if (_burstButton != null) _burstButton.Visible = false;
        AppendLog($"[color=#FFD700]=== 一击决胜！伤害 {damage} ===[/color]");
        ShowFlash("一击决胜！", "#FFD700");
        UpdateBars();
    }

    private void OnInvalidMoveSelected(string message)
    {
        AppendLog($"[color=#888888]⚠ {message}[/color]");
    }

    private void OnMoveSelected(int index)
    {
        if (_manager == null) return;
        if (_moveButtons != null)
        {
            foreach (var child in _moveButtons.GetChildren())
                if (child is Button btn) btn.Disabled = true;
        }
        _manager.PlayerSelectMove(index);
    }

    private void OnBurstPressed()
    {
        _manager?.PlayerActivateBurst();
    }

    private void OnCombatOver(bool playerWon)
    {
        if (_moveButtons != null)
        {
            foreach (var child in _moveButtons.GetChildren())
                if (child is Button btn) btn.Disabled = true;
        }
        if (_burstButton != null) _burstButton.Visible = false;

        string result = playerWon
            ? "[color=#4A9E7A]你赢了。铁冠道人缓缓放下了手中兵器……[/color]"
            : "[color=#C73E3E]你倒下了……「还不够……远远不够。」[/color]";
        AppendLog(result);
        ShowFlash(playerWon ? "胜利" : "落败", playerWon ? "#4A9E7A" : "#C73E3E");
    }

    private void UpdateBars()
    {
        if (_manager == null) return;
        var player = _manager.GetPlayer();
        var enemy = _manager.GetEnemy();

        if (_playerHpBar != null)
        {
            _playerHpBar.MaxValue = player.MaxHp;
            _playerHpBar.Value = player.CurrentHp;
        }
        if (_playerNeiliBar != null)
        {
            _playerNeiliBar.MaxValue = player.MaxNeili;
            _playerNeiliBar.Value = player.CurrentNeili;
        }
        if (_enemyHpBar != null)
        {
            _enemyHpBar.MaxValue = enemy.MaxHp;
            _enemyHpBar.Value = enemy.CurrentHp;
        }
        if (_enemyStaggerBar != null)
        {
            _enemyStaggerBar.MaxValue = enemy.StaggerCap;
            _enemyStaggerBar.Value = enemy.CurrentStagger;
        }
        if (_playerHpText != null)
            _playerHpText.Text = $"无名  HP:{player.CurrentHp}/{player.MaxHp}  内息:{player.CurrentNeili}/{player.MaxNeili}";
        if (_enemyHpText != null)
            _enemyHpText.Text = $"铁冠道人  HP:{enemy.CurrentHp}/{enemy.MaxHp}  破绽:{enemy.CurrentStagger}/{enemy.StaggerCap}";
    }

    private void AppendLog(string line)
    {
        _logText += line + "\n";
        if (_combatLog != null)
        {
            _combatLog.Text = _logText;
            // Scroll to bottom
            _combatLog.ScrollFollowing = true;
        }
    }

    private void ShowFlash(string text, string color)
    {
        if (_flashLabel == null) return;
        _flashLabel.Text = text;
        _flashLabel.Modulate = new Color(color);
        _flashLabel.Visible = true;

        var tween = CreateTween();
        tween.TweenInterval(1.2);
        tween.TweenCallback(Callable.From(() => { if (_flashLabel != null) _flashLabel.Visible = false; }));
    }

    private static string TypeChar(MoveType t) => t switch
    {
        MoveType.Gang => "刚",
        MoveType.Rou => "柔",
        MoveType.Qiao => "巧",
        _ => "?"
    };
}
