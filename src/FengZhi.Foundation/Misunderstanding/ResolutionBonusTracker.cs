using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>弹回加成追踪器 — 管理澄清后的临时态度加成及自动过期。</summary>
public sealed class ResolutionBonusTracker
{
    private readonly INpcStateWriter _writer;
    private readonly MisunderstandingConfig _config;
    private readonly Dictionary<string, int> _remainingDays = new();

    public ResolutionBonusTracker(INpcStateWriter writer, MisunderstandingConfig config)
    {
        _writer = writer;
        _config = config;
    }

    /// <summary>施加弹回加成（重置计时，不叠加数值）。</summary>
    public void ApplyBonus(string npcId)
    {
        _remainingDays[npcId] = _config.ResolveBonusDurationDays;
        _writer.ApplyTemporaryBonus(npcId, _config.ResolveAttitudeBonus, _config.ResolveBonusDurationDays);
    }

    /// <summary>每日 tick — 递减并自动清除到期 bonus。</summary>
    public void OnDayAdvanced(IEnumerable<string> npcIds)
    {
        var expired = new List<string>();
        foreach (var npcId in npcIds)
        {
            if (!_remainingDays.TryGetValue(npcId, out var days))
                continue;
            days--;
            if (days <= 0)
            {
                expired.Add(npcId);
            }
            else
            {
                _remainingDays[npcId] = days;
            }
        }

        foreach (var npcId in expired)
        {
            _remainingDays.Remove(npcId);
            _writer.ApplyTemporaryBonus(npcId, 0, 0);
        }
    }

    public bool HasBonus(string npcId) => _remainingDays.ContainsKey(npcId);
    public int GetRemainingDays(string npcId) => _remainingDays.GetValueOrDefault(npcId);
}
