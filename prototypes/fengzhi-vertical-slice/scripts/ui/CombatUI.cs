// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;
using System;

namespace FengzhiSlice;

/// <summary>
/// Combat UI: displays intent tell, resource bars, move buttons, burst trigger.
/// Uses Art Bible colors: 墨黑 #1A1A2E, 宣纸白 #F5F0E8, 朱砂红 #C73E3E,
/// 青山蓝 #3A5B8C, 翠生 #4A9E7A
/// </summary>
public partial class CombatUI : Control
{
    // UI element references (set in scene)
    private Label? _intentLabel;
    private Label? _turnLabel;
    private ProgressBar? _playerHpBar;
    private ProgressBar? _playerNeiliBar;
    private ProgressBar? _enemyHpBar;
    private Label? _playerHpText;
    private Label? _enemyHpText;
    private VBoxContainer? _moveButtons;
    private Button? _burstButton;
    private Label? _combatLog;
    private Label? _counterFlash;
    
    private CombatManager? _combatManager;
    
    public override void _Ready()
    {
        // Get references from scene tree
        _intentLabel = GetNodeOrNull<Label>("%IntentLabel");
        _turnLabel = GetNodeOrNull<Label>("%TurnLabel");
        _playerHpBar = GetNodeOrNull<ProgressBar>("%PlayerHpBar");
        _playerNeiliBar = GetNodeOrNull<ProgressBar>("%PlayerNeiliBar");
        _enemyHpBar = GetNodeOrNull<ProgressBar>("%EnemyHpBar");
        _playerHpText = GetNodeOrNull<Label>("%PlayerHpText");
        _enemyHpText = GetNodeOrNull<Label>("%EnemyHpText");
        _moveButtons = GetNodeOrNull<VBoxContainer>("%MoveButtons");
        _burstButton = GetNodeOrNull<Button>("%BurstButton");
        _combatLog = GetNodeOrNull<Label>("%CombatLog");
        _counterFlash = GetNodeOrNull<Label>("%CounterFlash");
        
        if (_burstButton != null)
        {
            _burstButton.Pressed += OnBurstPressed;
            _burstButton.Visible = false;
        }
        
        if (_counterFlash != null) _counterFlash.Visible = false;
        
        Visible = false;
    }
    
    public void BindCombatManager(CombatManager manager)
    {
        _combatManager = manager;
        manager.IntentRevealed += OnIntentRevealed;
        manager.WaitingForPlayerInput += OnWaitingForInput;
        manager.TurnAnimating += OnTurnAnimating;
        manager.BurstAvailable += OnBurstAvailable;
        manager.BurstExecuted += OnBurstExecuted;
        manager.CombatOver += OnCombatOver;
        manager.InvalidMoveSelected += OnInvalidMoveSelected;
        
        Visible = true;
        SetupMoveButtons();
        UpdateBars();
    }
    
    private void SetupMoveButtons()
    {
        if (_moveButtons == null || _combatManager == null) return;
        
        // Clear existing
        foreach (var child in _moveButtons.GetChildren())
        {
            child.QueueFree();
        }
        
        var moves = _combatManager.GetPlayerMoves();
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            var btn = new Button();
            string typeIcon = move.Type switch
            {
                MoveType.Gang => "【刚】",
                MoveType.Rou => "【柔】",
                MoveType.Qiao => "【巧】",
                _ => ""
            };
            btn.Text = $"{typeIcon} {move.Name} (内息:{move.NeiliCost} 伤害:{move.BaseDamage})";
            btn.CustomMinimumSize = new Vector2(300, 48);
            int moveIndex = i; // Capture for closure
            btn.Pressed += () => OnMoveSelected(moveIndex);
            _moveButtons.AddChild(btn);
        }
        
        RefreshMoveButtonStates();
    }
    
    private void OnIntentRevealed(string intentType)
    {
        if (_intentLabel != null)
        {
            string color = intentType switch
            {
                "刚" => "#C73E3E", // 朱砂红
                "柔" => "#3A5B8C", // 青山蓝
                "巧" => "#4A9E7A", // 翠生
                _ => "#F5F0E8"
            };
            _intentLabel.Text = $"[color={color}]敌方意图: {intentType}[/color]";
        }
    }
    
    private void OnWaitingForInput()
    {
        if (_turnLabel != null)
            _turnLabel.Text = $"第 {_combatManager?.TurnCount ?? 0} 回合 — 选择招式";
        
        RefreshMoveButtonStates();
        UpdateBars();
    }
    
    private void RefreshMoveButtonStates()
    {
        if (_moveButtons == null || _combatManager == null) return;
        
        int i = 0;
        var moves = _combatManager.GetPlayerMoves();
        var player = _combatManager.GetPlayer();
        foreach (var child in _moveButtons.GetChildren())
        {
            if (child is Button btn)
            {
                var move = moves[i];
                string typeIcon = move.Type switch
                {
                    MoveType.Gang => "【刚】",
                    MoveType.Rou => "【柔】",
                    MoveType.Qiao => "【巧】",
                    _ => ""
                };
                int cd = _combatManager.GetMoveCooldown(i);
                string statusText = "";
                bool canUse = _combatManager.CanSelectMove(i);
                btn.Disabled = !canUse;
                
                if (cd > 0)
                {
                    statusText = $" [冷却{cd}]";
                    btn.Modulate = new Color(0.4f, 0.4f, 0.5f, 0.7f);
                    btn.TooltipText = $"冷却中 — 还需 {cd} 回合";
                }
                else if (move.NeiliCost > player.CurrentNeili)
                {
                    statusText = $" [内息不足]";
                    btn.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                    btn.TooltipText = $"内息不足 — 需要 {move.NeiliCost} 内息";
                }
                else
                {
                    btn.Modulate = new Color(1, 1, 1, 1);
                    btn.TooltipText = move.Description;
                }
                
                btn.Text = $"{typeIcon} {move.Name} (内息:{move.NeiliCost} 伤害:{move.BaseDamage}){statusText}";
            }
            i++;
        }
    }
    
    private void OnInvalidMoveSelected(string message)
    {
        if (_combatLog != null)
        {
            _combatLog.Text = $"⚠ {message}";
        }
        
        var tween = CreateTween();
        tween.TweenInterval(1.5);
        tween.TweenCallback(Callable.From(() =>
        {
            if (_combatLog != null && _combatLog.Text.StartsWith("⚠"))
            {
                _combatLog.Text = "";
            }
        }));
    }
    
    private void OnMoveSelected(int index)
    {
        if (_combatManager == null) return;
        
        // Disable buttons during resolution
        if (_moveButtons != null)
        {
            foreach (var child in _moveButtons.GetChildren())
            {
                if (child is Button btn) btn.Disabled = true;
            }
        }
        
        _combatManager.PlayerSelectMove(index);
    }
    
    private void OnTurnAnimating(string playerMove, string enemyMove, int playerDmg, int enemyDmg, bool counter)
    {
        if (_combatLog != null)
        {
            string counterText = counter ? " [克制!]" : "";
            _combatLog.Text = $"你使出 {playerMove}{counterText} → 敌方 -{playerDmg}\n敌方 {enemyMove} → 你 -{enemyDmg}";
        }
        
        if (counter && _counterFlash != null)
        {
            _counterFlash.Text = "克制！";
            _counterFlash.Visible = true;
            // Auto-hide after 1 second via tween
            var tween = CreateTween();
            tween.TweenInterval(1.0);
            tween.TweenCallback(Callable.From(() => { if (_counterFlash != null) _counterFlash.Visible = false; }));
        }
        
        UpdateBars();
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
        if (_combatLog != null) _combatLog.Text = $"=== 一击决胜！=== 造成 {damage} 伤害！";
        if (_counterFlash != null)
        {
            _counterFlash.Text = "一击决胜！";
            _counterFlash.Visible = true;
        }
        UpdateBars();
    }
    
    private void OnBurstPressed()
    {
        _combatManager?.PlayerActivateBurst();
    }
    
    private void OnCombatOver(bool playerWon)
    {
        if (_moveButtons != null)
        {
            foreach (var child in _moveButtons.GetChildren())
            {
                if (child is Button btn) btn.Disabled = true;
            }
        }
        if (_burstButton != null) _burstButton.Visible = false;
        
        if (_combatLog != null)
        {
            _combatLog.Text = playerWon ? "你赢了。" : "你倒下了……";
        }
        
        // Brief pause then hide to avoid overlap with post-combat dialogue
        var tween = CreateTween();
        tween.TweenInterval(1.0);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => { Visible = false; Modulate = new Color(1, 1, 1, 1); }));
    }
    
    private void UpdateBars()
    {
        if (_combatManager == null) return;
        var player = _combatManager.GetPlayer();
        var enemy = _combatManager.GetEnemy();
        
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
        if (_playerHpText != null)
            _playerHpText.Text = $"{player.Name} HP:{player.CurrentHp}/{player.MaxHp} 内息:{player.CurrentNeili}/{player.MaxNeili}";
        if (_enemyHpText != null)
            _enemyHpText.Text = $"{enemy.Name} HP:{enemy.CurrentHp}/{enemy.MaxHp} 破绽:{enemy.CurrentStagger}/{enemy.StaggerCap}";
    }
}
