using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 玩家出招历史记录（单个角色）。
/// </summary>
public sealed class PlayerActionRecord
{
    public string PlayerId { get; init; } = string.Empty;
    public MoveType Type { get; init; }
    public int Round { get; init; }
}

/// <summary>
/// 反读系统：追踪玩家行为并在 Phase B 判定是否否决 Phase A 结果。
/// GDD §Core Rules 4 + §Formulas F2。
/// </summary>
public sealed class CounterReadSystem
{
    public const int DefaultHistoryWindow = 3;
    public const float DefaultCooldownPenalty = 0.5f;
    public const int ConsecutiveThreshold = 2;

    private readonly int _historyWindow;
    private readonly float _cooldownPenalty;

    // 每个玩家角色独立维护历史
    private readonly Dictionary<string, List<MoveType>> _playerHistories = new();

    /// <summary>
    /// 上回合是否已触发反读（用于冷却计算）。
    /// </summary>
    public bool LastRoundTriggered { get; private set; }

    public CounterReadSystem(int historyWindow = DefaultHistoryWindow, float cooldownPenalty = DefaultCooldownPenalty)
    {
        _historyWindow = historyWindow;
        _cooldownPenalty = cooldownPenalty;
    }

    /// <summary>
    /// 记录玩家角色的出招体系。
    /// </summary>
    public void RecordPlayerAction(string playerId, MoveType type)
    {
        if (!_playerHistories.TryGetValue(playerId, out var history))
        {
            history = new List<MoveType>();
            _playerHistories[playerId] = history;
        }
        history.Add(type);
        // 只保留窗口内
        if (history.Count > _historyWindow)
            history.RemoveAt(0);
    }

    /// <summary>
    /// Phase B 反读判定。
    /// 如果触发，返回应使用的克制体系；否则返回 null。
    /// </summary>
    /// <param name="counterReadChance">敌人的反读基础概率</param>
    /// <param name="random">随机源</param>
    /// <returns>克制体系或 null</returns>
    public MoveType? TryCounterRead(float counterReadChance, IAIRandomSource random)
    {
        if (counterReadChance <= 0f)
            return null;

        // 找到连续同体系回合数最高的玩家角色
        string? bestPlayerId = null;
        int bestConsecutive = 0;
        MoveType bestType = MoveType.Gang;

        foreach (var (playerId, history) in _playerHistories)
        {
            int consecutive = GetConsecutiveCount(history);
            if (consecutive > bestConsecutive)
            {
                bestConsecutive = consecutive;
                bestPlayerId = playerId;
                bestType = history[^1]; // 最近使用的体系
            }
        }

        // 未达到连续阈值，不触发
        if (bestConsecutive < ConsecutiveThreshold)
            return null;

        // F2: trigger_chance = min(1.0, counter_read_chance × (1 - cooldown_penalty))
        float cooldown = LastRoundTriggered ? _cooldownPenalty : 0f;
        float triggerChance = MathF.Min(1.0f, counterReadChance * (1f - cooldown));

        if (random.NextFloat() < triggerChance)
        {
            LastRoundTriggered = true;
            return GetCounterType(bestType);
        }

        LastRoundTriggered = false;
        return null;
    }

    /// <summary>
    /// 在回合结束时调用，如果本回合未触发反读则重置冷却。
    /// </summary>
    public void EndRound(bool triggered)
    {
        LastRoundTriggered = triggered;
    }

    /// <summary>
    /// 计算某玩家历史中从末尾开始的连续同体系回合数。
    /// </summary>
    public static int GetConsecutiveCount(IReadOnlyList<MoveType> history)
    {
        if (history.Count == 0) return 0;

        var lastType = history[^1];
        int count = 0;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i] == lastType)
                count++;
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// 获取克制指定体系的体系类型。
    /// 克制关系：Gang > Qiao > Rou > Gang
    /// 克制某体系 = 用什么打它有优势
    /// </summary>
    public static MoveType GetCounterType(MoveType targetType) => targetType switch
    {
        MoveType.Gang => MoveType.Rou,   // 柔克刚
        MoveType.Qiao => MoveType.Gang,  // 刚克巧
        MoveType.Rou => MoveType.Qiao,   // 巧克柔
        _ => MoveType.Gang
    };

    /// <summary>
    /// 获取所有玩家历史（用于调试/测试）。
    /// </summary>
    public IReadOnlyDictionary<string, List<MoveType>> GetPlayerHistories() => _playerHistories;

    /// <summary>
    /// 重置所有状态。
    /// </summary>
    public void Reset()
    {
        _playerHistories.Clear();
        LastRoundTriggered = false;
    }
}
