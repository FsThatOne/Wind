using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>缺席检测器 — 每日 tick 检查玩家是否离开 NPC 区域过久，含级联熔断器。</summary>
public sealed class AbsenceDetector
{
    private readonly IScenePresenceQuery _presenceQuery;
    private readonly MisunderstandingConfig _config;
    private readonly Dictionary<string, int> _absentDays = new();
    private readonly Dictionary<string, int> _immunityDays = new();

    public AbsenceDetector(IScenePresenceQuery presenceQuery, MisunderstandingConfig config)
    {
        _presenceQuery = presenceQuery;
        _config = config;
    }

    /// <summary>注册战败保护期（级联熔断器）。</summary>
    public void ApplyDefeatImmunity(string npcId, int days = 2)
    {
        _immunityDays[npcId] = days;
    }

    /// <summary>每日 tick — 检测缺席并返回需要触发缺席误解的 NPC 列表。</summary>
    public List<string> OnDayAdvanced(IEnumerable<string> trackedNpcs)
    {
        var triggered = new List<string>();

        TickImmunity();

        foreach (var npcId in trackedNpcs)
        {
            if (_immunityDays.ContainsKey(npcId))
                continue;

            if (_presenceQuery.IsPlayerColocatedWith(npcId))
            {
                _absentDays.Remove(npcId);
                continue;
            }

            var days = _absentDays.GetValueOrDefault(npcId) + 1;
            _absentDays[npcId] = days;

            if (days >= _config.AbsenceTriggerThresholdDays)
            {
                triggered.Add(npcId);
                _absentDays.Remove(npcId);
            }
        }

        return triggered;
    }

    private void TickImmunity()
    {
        var expired = new List<string>();
        foreach (var (npcId, days) in _immunityDays)
        {
            if (days <= 1)
                expired.Add(npcId);
            else
                _immunityDays[npcId] = days - 1;
        }
        foreach (var npcId in expired)
            _immunityDays.Remove(npcId);
    }

    public int GetAbsentDays(string npcId) => _absentDays.GetValueOrDefault(npcId);
    public bool HasImmunity(string npcId) => _immunityDays.ContainsKey(npcId);
}
