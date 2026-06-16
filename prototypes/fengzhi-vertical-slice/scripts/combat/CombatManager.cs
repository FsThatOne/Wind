// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;
using System;

namespace FengzhiSlice;

/// <summary>
/// Burst+Read combat manager. Handles turn flow:
/// 1. Enemy reveals intent (type only: 刚/柔/巧)
/// 2. Player selects move
/// 3. Simultaneous resolution with counter multipliers
/// 4. Check for Burst (一击决胜) trigger conditions
/// 5. Repeat until one side falls
/// </summary>
public partial class CombatManager : Node
{
    [Signal] public delegate void IntentRevealedEventHandler(string enemyIntentType);
    [Signal] public delegate void WaitingForPlayerInputEventHandler();
    [Signal] public delegate void TurnAnimatingEventHandler(string playerMove, string enemyMove, int playerDmg, int enemyDmg, bool counter);
    [Signal] public delegate void BurstAvailableEventHandler();
    [Signal] public delegate void BurstExecutedEventHandler(int damage);
    [Signal] public delegate void CombatOverEventHandler(bool playerWon);
    [Signal] public delegate void InvalidMoveSelectedEventHandler(string message);
    
    private CharacterData _player = null!;
    private CharacterData _enemy = null!;
    private MartialMove[] _playerMoves = null!;
    private MartialMove[] _enemyMoves = null!;
    private EnemyAI _enemyAI = null!;
    private MartialMove? _enemyChosenMove;
    private int _turnCount;
    private bool _playerUsedBurst;
    private bool _isResolvingTurn;
    private int[] _playerMoveCooldowns = null!; // remaining cooldown for each player move
    
    public int TurnCount => _turnCount;
    public bool IsOver { get; private set; }
    
    public void Initialize(CharacterData player, CharacterData enemy)
    {
        _player = player;
        _enemy = enemy;
        _playerMoves = MartialMove.GetFengzhiRulerSet();
        _enemyMoves = MartialMove.GetBanditSaberSet();
        _enemyAI = new EnemyAI(_enemy, _enemyMoves);
        _turnCount = 0;
        _playerUsedBurst = false;
        IsOver = false;
        _isResolvingTurn = false;
        
        _playerMoveCooldowns = new int[_playerMoves.Length];
    }
    
    /// <summary>
    /// Call after UI is bound to start the first turn.
    /// </summary>
    public void BeginCombat()
    {
        StartNewTurn();
    }
    
    private void StartNewTurn()
    {
        if (IsOver) return;
        
        _turnCount++;
        GD.Print($"[Combat] === Turn {_turnCount} ===");
        
        // Decrement player cooldowns at start of turn (enemy AI manages its own)
        DecrementCooldowns(_playerMoveCooldowns);
        _enemyAI.TickCooldowns();
        
        // Stagger decay (per prototype finding #3)
        _enemy.DecayStagger();
        
        // Enemy AI selects move (AI now manages its own cooldowns internally)
        _enemyChosenMove = _enemyAI.SelectMove(_player);
        
        string intentType = _enemyChosenMove.Type switch
        {
            MoveType.Gang => "刚",
            MoveType.Rou => "柔",
            MoveType.Qiao => "巧",
            _ => "?"
        };
        
        GD.Print($"[Combat] Enemy intent: {intentType} ({_enemyChosenMove.Name})");
        EmitSignal(SignalName.IntentRevealed, intentType);
        
        bool burstAvailable = CanPlayerBurst();
        if (burstAvailable)
        {
            EmitSignal(SignalName.BurstAvailable);
        }
        
        EmitSignal(SignalName.WaitingForPlayerInput);
    }
    
    private static void DecrementCooldowns(int[] cooldowns)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0) cooldowns[i]--;
        }
    }
    
    /// <summary>
    /// Called by UI when player selects a move.
    /// </summary>
    public void PlayerSelectMove(int moveIndex)
    {
        if (IsOver || _isResolvingTurn) return;
        if (moveIndex < 0 || moveIndex >= _playerMoves.Length) return;
        
        var playerMove = _playerMoves[moveIndex];
        
        // Check cooldown
        if (_playerMoveCooldowns[moveIndex] > 0)
        {
            EmitSignal(SignalName.InvalidMoveSelected, $"{playerMove.Name} 冷却中 — 还需 {_playerMoveCooldowns[moveIndex]} 回合");
            return;
        }
        
        // Check neili cost
        if (playerMove.NeiliCost > _player.CurrentNeili)
        {
            GD.Print($"[Combat] Not enough neili for {playerMove.Name}");
            EmitSignal(SignalName.InvalidMoveSelected, $"内息不足 — {playerMove.Name} 需要 {playerMove.NeiliCost} 内息");
            return;
        }
        
        ResolveTurn(moveIndex);
    }
    
    /// <summary>
    /// Check whether a move can be selected with current neili + cooldown.
    /// </summary>
    public bool CanSelectMove(int moveIndex)
    {
        if (moveIndex < 0 || moveIndex >= _playerMoves.Length) return false;
        if (_playerMoveCooldowns[moveIndex] > 0) return false;
        return _playerMoves[moveIndex].NeiliCost <= _player.CurrentNeili;
    }
    
    /// <summary>
    /// Get remaining cooldown for a player move (0 = ready).
    /// </summary>
    public int GetMoveCooldown(int moveIndex)
    {
        if (moveIndex < 0 || moveIndex >= _playerMoves.Length) return 0;
        return _playerMoveCooldowns[moveIndex];
    }
    
    /// <summary>
    /// Player activates Burst (一击决胜). Costs 3 total neili.
    /// </summary>
    public void PlayerActivateBurst()
    {
        if (IsOver || _playerUsedBurst || !CanPlayerBurst()) return;
        
        _playerUsedBurst = true;
        
        // Burst does massive damage — per prototype: 3 neili total cost
        int burstDamage = 60; // Enough to kill most enemies
        _player.SpendNeili(3);
        _enemy.TakeDamage(burstDamage);
        
        GD.Print($"[Combat] === 一击决胜! === Damage: {burstDamage}");
        EmitSignal(SignalName.BurstExecuted, burstDamage);
        
        CheckCombatEnd();
        if (!IsOver) StartNewTurn();
    }
    
    private void ResolveTurn(int playerMoveIndex)
    {
        _isResolvingTurn = true;
        var playerMove = _playerMoves[playerMoveIndex];
        var enemyMove = _enemyChosenMove!;
        
        // Spend neili
        _player.SpendNeili(playerMove.NeiliCost);
        _enemy.SpendNeili(enemyMove.NeiliCost);
        
        // Calculate damage with counter system (1.3x / 0.7x per prototype finding)
        float playerMultiplier = MartialMove.GetCounterMultiplier(playerMove.Type, enemyMove.Type);
        float enemyMultiplier = MartialMove.GetCounterMultiplier(enemyMove.Type, playerMove.Type);
        
        int playerDamage = (int)(playerMove.BaseDamage * playerMultiplier);
        int enemyDamage = (int)(enemyMove.BaseDamage * enemyMultiplier);
        
        bool wasCounter = playerMultiplier > 1.0f;
        
        // Apply damage (simultaneous resolution)
        _enemy.TakeDamage(playerDamage);
        _player.TakeDamage(enemyDamage);
        
        // Apply stagger
        if (playerMove.StaggerValue > 0) _enemy.AddStagger(playerMove.StaggerValue);
        
        // Rou defense grants neili
        if (playerMove.GrantsNeili) _player.GainNeili(1);
        if (enemyMove.GrantsNeili) _enemy.GainNeili(1);
        
        GD.Print($"[Combat] {playerMove.Name}({playerMultiplier:F1}x) → Enemy -{playerDamage}HP | {enemyMove.Name}({enemyMultiplier:F1}x) → Player -{enemyDamage}HP");
        
        // Set player move cooldown after successful use (enemy AI manages its own cooldowns)
        // +1 compensates for the immediate decrement in StartNewTurn that follows
        if (playerMove.CooldownTurns > 0)
        {
            _playerMoveCooldowns[playerMoveIndex] = playerMove.CooldownTurns + 1;
        }
        
        // Publish turn result
        var result = new TurnResult(playerMove.Name, enemyMove.Name, playerDamage, enemyDamage, wasCounter);
        EventBus.PublishTurnResolved(result);
        EmitSignal(SignalName.TurnAnimating, playerMove.Name, enemyMove.Name, playerDamage, enemyDamage, wasCounter);
        
        _isResolvingTurn = false;
        
        CheckCombatEnd();
        if (!IsOver) StartNewTurn();
    }
    
    private bool CanPlayerBurst()
    {
        if (_playerUsedBurst) return false;
        if (_player.CurrentNeili < 3) return false;
        
        // Burst conditions (per validated prototype):
        // 1. Enemy stagger at cap
        // 2. Enemy HP < 30%
        // 3. Player full HP + successful counter
        return _enemy.IsStaggered || _enemy.HpRatio < 0.3f || _player.HpRatio >= 1.0f;
    }
    
    private void CheckCombatEnd()
    {
        if (!_enemy.IsAlive)
        {
            IsOver = true;
            GD.Print("[Combat] === Player WINS ===");
            EmitSignal(SignalName.CombatOver, true);
            EventBus.PublishCombatEnded(new CombatResult(true, _turnCount, _playerUsedBurst));
        }
        else if (!_player.IsAlive)
        {
            IsOver = true;
            GD.Print("[Combat] === Player LOSES ===");
            EmitSignal(SignalName.CombatOver, false);
            EventBus.PublishCombatEnded(new CombatResult(false, _turnCount, _playerUsedBurst));
        }
    }
    
    public MartialMove[] GetPlayerMoves() => _playerMoves;
    public CharacterData GetPlayer() => _player;
    public CharacterData GetEnemy() => _enemy;
}
