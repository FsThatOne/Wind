using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.Board;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 追踪型 AI：移动阶段向最近敌人逼近，行动阶段使用预设招式。
/// 追踪策略：在可达范围中选择距目标曼哈顿距离最小的格子。
/// </summary>
public sealed class ChasingAI : IBattleAI
{
    private readonly string _targetId;
    private readonly Queue<BattleAction> _actionScript;

    public ChasingAI(string targetId, IEnumerable<BattleAction> actionScript)
    {
        _targetId = targetId;
        _actionScript = new Queue<BattleAction>(actionScript);
    }

    public GridPosition? DecideMovement(BattleCombatant actor, BattleGrid grid, IReadOnlySet<GridPosition> reachable)
    {
        var targetPos = grid.FindCombatant(_targetId);
        if (targetPos == null) return null;

        var target = targetPos.Value;
        int currentDist = actor.Position.ManhattanDistance(target);

        GridPosition best = actor.Position;
        int bestDist = currentDist;

        foreach (var cell in reachable)
        {
            int dist = cell.ManhattanDistance(target);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = cell;
            }
        }

        return best == actor.Position ? null : best;
    }

    public BattleAction DecideAction(BattleCombatant actor, BattleInstance battle)
    {
        if (_actionScript.Count > 0)
            return _actionScript.Dequeue();

        return new BattleAction { ActorId = actor.Id, Type = ActionType.Breathe };
    }
}
