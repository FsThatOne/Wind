// VERTICAL SLICE - Boss Battle using Foundation EnemyBrain
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FengZhi.Foundation.Combat.AI;
using FoundationMoveType = FengZhi.Foundation.CharacterData.MoveType;

namespace FengzhiSlice;

/// <summary>
/// Boss combat manager that bridges Foundation's EnemyBrain to the Godot UI.
/// Handles the full turn flow:
/// 1. Boss AI decides (via EnemyBrain pipeline)
/// 2. Show intent / charge announcement
/// 3. Player selects move
/// 4. Resolve damage with counters
/// </summary>
public partial class BossCombatManager : Node
{
    [Signal] public delegate void IntentRevealedEventHandler(string enemyIntentType);
    [Signal] public delegate void WaitingForPlayerInputEventHandler();
    [Signal] public delegate void TurnAnimatingEventHandler(string playerMove, string enemyMove, int playerDmg, int enemyDmg, bool counter);
    [Signal] public delegate void BurstAvailableEventHandler();
    [Signal] public delegate void BurstExecutedEventHandler(int damage);
    [Signal] public delegate void CombatOverEventHandler(bool playerWon);
    [Signal] public delegate void InvalidMoveSelectedEventHandler(string message);
    [Signal] public delegate void BossPhaseChangedEventHandler(string phaseName);
    [Signal] public delegate void BossChargeAnnouncedEventHandler(string moveName);
    [Signal] public delegate void BossMeditatedEventHandler();
    [Signal] public delegate void CounterReadTriggeredEventHandler();

    private CharacterData _player = null!;
    private CharacterData _enemy = null!;
    private MartialMove[] _playerMoves = null!;
    private Dictionary<MoveType, MartialMove[]> _bossMovesByType = null!;
    private EnemyBrain _brain = null!;
    private AIDecision? _currentDecision;
    private int _turnCount;
    private bool _playerUsedBurst;
    private bool _isResolvingTurn;
    private int[] _playerMoveCooldowns = null!;

    // Track consecutive type for AI context
    private MoveType? _bossLastType;
    private int _bossConsecutiveType;
    private int _bossMeditationCount;
    private FoundationMoveType? _playerLastType;

    public int TurnCount => _turnCount;
    public bool IsOver { get; private set; }

    public void Initialize()
    {
        _player = BossData.CreateBossPlayer();
        _enemy = BossData.CreateBoss();
        _playerMoves = BossData.GetPlayerBossFightMoves();
        _bossMovesByType = BossData.GetBossMovesByType();
        _playerMoveCooldowns = new int[_playerMoves.Length];

        // Create EnemyBrain with standard 4-phase boss script
        _brain = new EnemyBrain(
            BossPhaseScript.StandardFourPhase,
            signaturePattern: null,
            new DefaultAIRandom());

        _turnCount = 0;
        _playerUsedBurst = false;
        IsOver = false;
        _isResolvingTurn = false;
        _bossLastType = null;
        _bossConsecutiveType = 0;
        _bossMeditationCount = 0;
        _playerLastType = null;
    }

    public void BeginCombat()
    {
        StartNewTurn();
    }

    private void StartNewTurn()
    {
        if (IsOver) return;

        _turnCount++;
        GD.Print($"[BossBattle] === 第 {_turnCount} 回合 ===");

        // Decrement player cooldowns
        DecrementCooldowns(_playerMoveCooldowns);

        // Stagger decay
        _enemy.DecayStagger();

        // Advance boss round counter
        _brain.AdvanceRound();

        // Build AI context
        var ctx = BuildBattleContext();

        // Boss AI decides
        _currentDecision = _brain.Decide(ctx);

        GD.Print($"[BossBattle] Boss决策: {_currentDecision.ActionType}" +
                 (_currentDecision.SelectedType != null ? $" 体系:{_currentDecision.SelectedType}" : "") +
                 (_currentDecision.CounterReadTriggered ? " [反读!]" : "") +
                 (_currentDecision.IsPriorityStrike ? " [优先攻击!]" : ""));

        // Handle different decision types
        switch (_currentDecision.ActionType)
        {
            case AIActionType.ChargeAnnounce:
                string chargeName = _currentDecision.ChargeAnnounceName ?? "蓄力";
                EmitSignal(SignalName.BossChargeAnnounced, chargeName);
                EmitSignal(SignalName.IntentRevealed, $"蓄力: {chargeName}");
                EmitSignal(SignalName.WaitingForPlayerInput);
                break;

            case AIActionType.Meditate:
                _bossMeditationCount++;
                EmitSignal(SignalName.BossMeditated);
                EmitSignal(SignalName.IntentRevealed, "调息");
                EmitSignal(SignalName.WaitingForPlayerInput);
                break;

            case AIActionType.Attack:
                _bossMeditationCount = 0;
                string intentType = MapTypeToString(_currentDecision.SelectedType);
                if (_currentDecision.CounterReadTriggered)
                {
                    EmitSignal(SignalName.CounterReadTriggered);
                }
                EmitSignal(SignalName.IntentRevealed, intentType);
                break;
        }

        bool burstAvailable = CanPlayerBurst();
        if (burstAvailable)
        {
            EmitSignal(SignalName.BurstAvailable);
        }

        if (_currentDecision.ActionType == AIActionType.Attack)
        {
            EmitSignal(SignalName.WaitingForPlayerInput);
        }
    }

    private AIBattleContext BuildBattleContext()
    {
        // Build available moves list for the brain
        var availableMoves = new List<AIMoveEntry>();
        foreach (var kvp in _bossMovesByType)
        {
            foreach (var move in kvp.Value)
            {
                availableMoves.Add(new AIMoveEntry
                {
                    Id = move.Id,
                    Name = move.Name,
                    Type = ToFoundationType(move.Type),
                    NeixiCost = move.NeiliCost,
                    DamageMultiplier = move.BaseDamage / 20f
                });
            }
        }

        FoundationMoveType? lastType = _bossLastType != null
            ? ToFoundationType(_bossLastType.Value)
            : null;

        return new AIBattleContext
        {
            SelfId = "boss",
            HpRatio = _enemy.HpRatio,
            CurrentNeixi = _enemy.CurrentNeili,
            CurrentStagger = _enemy.CurrentStagger,
            CurrentRound = _turnCount,
            LastRoundType = lastType,
            ConsecutiveSameTypeCount = _bossConsecutiveType,
            ConsecutiveMeditationCount = _bossMeditationCount,
            WasCounteredLastRound = false,
            SelfStagger = _enemy.CurrentStagger,
            TargetStagger = _player.CurrentStagger,
            AvailableMoves = availableMoves,
            Targets = new List<TargetCandidate>
            {
                new() { Id = "player", HpRatio = _player.HpRatio,
                         CurrentStagger = _player.CurrentStagger,
                         StaggerThreshold = _player.StaggerCap }
            },
            AlreadyTargetedIds = new HashSet<string>()
        };
    }

    public void PlayerSelectMove(int moveIndex)
    {
        if (IsOver || _isResolvingTurn) return;
        if (moveIndex < 0 || moveIndex >= _playerMoves.Length) return;

        var playerMove = _playerMoves[moveIndex];

        if (_playerMoveCooldowns[moveIndex] > 0)
        {
            EmitSignal(SignalName.InvalidMoveSelected,
                $"{playerMove.Name} 冷却中 — 还需 {_playerMoveCooldowns[moveIndex]} 回合");
            return;
        }

        if (playerMove.NeiliCost > _player.CurrentNeili)
        {
            EmitSignal(SignalName.InvalidMoveSelected,
                $"内息不足 — {playerMove.Name} 需要 {playerMove.NeiliCost} 内息");
            return;
        }

        ResolveTurn(moveIndex);
    }

    public bool CanSelectMove(int moveIndex)
    {
        if (moveIndex < 0 || moveIndex >= _playerMoves.Length) return false;
        if (_playerMoveCooldowns[moveIndex] > 0) return false;
        return _playerMoves[moveIndex].NeiliCost <= _player.CurrentNeili;
    }

    public int GetMoveCooldown(int moveIndex)
    {
        if (moveIndex < 0 || moveIndex >= _playerMoveCooldowns.Length) return 0;
        return _playerMoveCooldowns[moveIndex];
    }

    public void PlayerActivateBurst()
    {
        if (IsOver || _playerUsedBurst || !CanPlayerBurst()) return;

        _playerUsedBurst = true;
        int burstDamage = 80;
        _player.SpendNeili(3);
        _enemy.TakeDamage(burstDamage);

        GD.Print($"[BossBattle] === 一击决胜! === 伤害: {burstDamage}");
        EmitSignal(SignalName.BurstExecuted, burstDamage);

        _brain.UpdateHp(_enemy.HpRatio);
        CheckCombatEnd();
        if (!IsOver) StartNewTurn();
    }

    private void ResolveTurn(int playerMoveIndex)
    {
        _isResolvingTurn = true;
        var playerMove = _playerMoves[playerMoveIndex];

        // Record player action for counter-read system
        _brain.RecordPlayerAction("player", ToFoundationType(playerMove.Type));
        _playerLastType = ToFoundationType(playerMove.Type);

        // Get boss move from decision
        MartialMove bossMove = GetBossMoveFromDecision();

        // Player spends neili
        _player.SpendNeili(playerMove.NeiliCost);

        // Calculate damage
        int playerDamage;
        int bossDamage;
        bool wasCounter;

        if (_currentDecision!.ActionType == AIActionType.Meditate)
        {
            // Boss meditates - recovers neili, doesn't attack
            _enemy.GainNeili(2);
            playerDamage = (int)(playerMove.BaseDamage * 1.0f); // Player attacks uncontested
            bossDamage = 0;
            wasCounter = false;
            _enemy.TakeDamage(playerDamage);
        }
        else if (_currentDecision.ActionType == AIActionType.ChargeAnnounce)
        {
            // Boss charging - vulnerable
            playerDamage = (int)(playerMove.BaseDamage * 1.2f); // Bonus for hitting during charge
            bossDamage = 0;
            wasCounter = false;
            _enemy.TakeDamage(playerDamage);
        }
        else
        {
            // Normal combat resolution
            _enemy.SpendNeili(bossMove.NeiliCost);

            float playerMultiplier = MartialMove.GetCounterMultiplier(playerMove.Type, bossMove.Type);
            float bossMultiplier = MartialMove.GetCounterMultiplier(bossMove.Type, playerMove.Type);

            // Priority strike bonus
            if (_currentDecision.IsPriorityStrike)
                bossMultiplier *= 1.3f;

            playerDamage = (int)(playerMove.BaseDamage * playerMultiplier);
            bossDamage = (int)(bossMove.BaseDamage * bossMultiplier);

            wasCounter = playerMultiplier > 1.0f;

            _enemy.TakeDamage(playerDamage);
            _player.TakeDamage(bossDamage);

            // Stagger
            if (playerMove.StaggerValue > 0)
                _enemy.AddStagger(playerMove.StaggerValue);

            // Counter tracking
            if (wasCounter)
                _brain.RecordCountered();

            // Neili recovery from Rou defense
            if (playerMove.GrantsNeili) _player.GainNeili(1);
            if (bossMove.GrantsNeili) _enemy.GainNeili(1);
        }

        // Track boss consecutive type
        if (_currentDecision.SelectedType != null)
        {
            var decisionSliceType = FromFoundationType(_currentDecision.SelectedType.Value);
            if (_bossLastType == decisionSliceType)
                _bossConsecutiveType++;
            else
                _bossConsecutiveType = 1;
            _bossLastType = decisionSliceType;
        }

        // Set player cooldown
        if (playerMove.CooldownTurns > 0)
            _playerMoveCooldowns[playerMoveIndex] = playerMove.CooldownTurns + 1;

        // Update boss HP for phase transitions
        bool phaseChanged = _brain.UpdateHp(_enemy.HpRatio);
        if (phaseChanged)
        {
            string phaseName = GetCurrentPhaseName();
            GD.Print($"[BossBattle] *** 阶段转换: {phaseName} ***");
            EmitSignal(SignalName.BossPhaseChanged, phaseName);
        }

        string bossMoveName = _currentDecision.ActionType switch
        {
            AIActionType.Meditate => "调息",
            AIActionType.ChargeAnnounce => "蓄力中...",
            _ => bossMove.Name
        };

        GD.Print($"[BossBattle] {playerMove.Name} → Boss -{playerDamage} | {bossMoveName} → 玩家 -{bossDamage}" +
                 (wasCounter ? " [克制!]" : ""));

        EmitSignal(SignalName.TurnAnimating, playerMove.Name, bossMoveName, playerDamage, bossDamage, wasCounter);

        _isResolvingTurn = false;
        CheckCombatEnd();
        if (!IsOver) StartNewTurn();
    }

    private MartialMove GetBossMoveFromDecision()
    {
        if (_currentDecision == null || _currentDecision.ActionType != AIActionType.Attack)
        {
            // Fallback for meditate/charge - return a dummy
            return new MartialMove { Name = "调息", BaseDamage = 0, NeiliCost = 0 };
        }

        // Map Foundation's selected move back to slice MartialMove
        var selectedType = _currentDecision.SelectedType ?? FoundationMoveType.Gang;
        var sliceType = FromFoundationType(selectedType);

        if (_bossMovesByType.TryGetValue(sliceType, out var moves) && moves.Length > 0)
        {
            // If brain selected a specific move by name, find it
            if (_currentDecision.SelectedMove != null)
            {
                var found = moves.FirstOrDefault(m => m.Id == _currentDecision.SelectedMove.Id);
                if (found != null) return found;
            }

            // Otherwise pick first affordable move of the type
            foreach (var m in moves)
            {
                if (m.NeiliCost <= _enemy.CurrentNeili) return m;
            }
            return moves[0];
        }

        // Ultimate fallback
        return _bossMovesByType[MoveType.Gang][0];
    }

    private bool CanPlayerBurst()
    {
        if (_playerUsedBurst) return false;
        if (_player.CurrentNeili < 3) return false;
        return _enemy.IsStaggered || _enemy.HpRatio < 0.25f;
    }

    private void CheckCombatEnd()
    {
        if (!_enemy.IsAlive)
        {
            IsOver = true;
            GD.Print("[BossBattle] === 玩家胜利 ===");
            EmitSignal(SignalName.CombatOver, true);
        }
        else if (!_player.IsAlive)
        {
            IsOver = true;
            GD.Print("[BossBattle] === 玩家落败 ===");
            EmitSignal(SignalName.CombatOver, false);
        }
    }

    private string GetCurrentPhaseName()
    {
        if (_brain.BossManager == null) return "未知";
        var phase = _brain.BossManager.CurrentPhase;
        return phase.Label ?? $"阶段{phase.PhaseIndex + 1}";
    }

    public MartialMove[] GetPlayerMoves() => _playerMoves;
    public CharacterData GetPlayer() => _player;
    public CharacterData GetEnemy() => _enemy;

    private static string MapTypeToString(FoundationMoveType? type) => type switch
    {
        FoundationMoveType.Gang => "刚",
        FoundationMoveType.Rou => "柔",
        FoundationMoveType.Qiao => "巧",
        _ => "?"
    };

    private static FoundationMoveType ToFoundationType(MoveType t) => t switch
    {
        MoveType.Gang => FoundationMoveType.Gang,
        MoveType.Rou => FoundationMoveType.Rou,
        MoveType.Qiao => FoundationMoveType.Qiao,
        _ => FoundationMoveType.Gang
    };

    private static MoveType FromFoundationType(FoundationMoveType t) => t switch
    {
        FoundationMoveType.Gang => MoveType.Gang,
        FoundationMoveType.Rou => MoveType.Rou,
        FoundationMoveType.Qiao => MoveType.Qiao,
        _ => MoveType.Gang
    };

    private static void DecrementCooldowns(int[] cooldowns)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0) cooldowns[i]--;
        }
    }
}
