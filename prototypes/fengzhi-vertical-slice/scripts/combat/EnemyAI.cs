// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using System;

namespace FengzhiSlice;

/// <summary>
/// Basic enemy AI for vertical slice.
/// Selects moves with weighted randomness based on situation.
/// Intent type is revealed honestly (per validated design decision).
/// </summary>
public class EnemyAI
{
    private readonly CharacterData _self;
    private readonly MartialMove[] _moves;
    private readonly int[] _cooldowns;
    private readonly Random _rng = new();
    
    public EnemyAI(CharacterData self, MartialMove[] moves)
    {
        _self = self;
        _moves = moves;
        _cooldowns = new int[moves.Length];
    }
    
    /// <summary>
    /// Called at the start of each turn to decrement cooldowns.
    /// </summary>
    public void TickCooldowns()
    {
        for (int i = 0; i < _cooldowns.Length; i++)
        {
            if (_cooldowns[i] > 0) _cooldowns[i]--;
        }
    }
    
    /// <summary>
    /// Called when this AI uses a move — sets the cooldown.
    /// </summary>
    public void OnMoveUsed(int moveIndex)
    {
        if (moveIndex >= 0 && moveIndex < _cooldowns.Length && moveIndex < _moves.Length)
        {
            _cooldowns[moveIndex] = _moves[moveIndex].CooldownTurns;
        }
    }
    
    public MartialMove SelectMove(CharacterData player)
    {
        float[] weights = new float[_moves.Length];
        
        for (int i = 0; i < _moves.Length; i++)
        {
            var move = _moves[i];
            
            // Can't afford or on cooldown
            if (move.NeiliCost > _self.CurrentNeili || _cooldowns[i] > 0)
            {
                weights[i] = 0f;
                continue;
            }
            
            float baseWeight = 1.0f;
            
            if (_self.HpRatio < 0.4f && move.Type == MoveType.Rou)
                baseWeight += 2.0f;
            
            if (player.CurrentStagger >= player.StaggerCap - 1 && move.Type == MoveType.Gang)
                baseWeight += 1.5f;
            
            if (_self.CurrentNeili >= 2 && move.Category == MoveCategory.Attack)
                baseWeight += 0.5f;
            
            weights[i] = baseWeight;
        }
        
        float totalWeight = 0f;
        foreach (var w in weights) totalWeight += w;
        
        if (totalWeight <= 0f)
        {
            for (int i = 0; i < _moves.Length; i++)
            {
                if (_moves[i].NeiliCost <= _self.CurrentNeili && _cooldowns[i] == 0) return _moves[i];
            }
            return _moves[0];
        }
        
        float roll = (float)_rng.NextDouble() * totalWeight;
        float cumulative = 0f;
        
        for (int i = 0; i < _moves.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                OnMoveUsed(i);
                return _moves[i];
            }
        }
        
        OnMoveUsed(0);
        return _moves[0];
    }
}
