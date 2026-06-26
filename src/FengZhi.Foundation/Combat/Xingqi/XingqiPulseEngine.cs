namespace FengZhi.Foundation.Combat.Xingqi;

/// <summary>
/// 行气脉冲引擎。负责推进所有角色的行气，并返回就绪行动队列（GDD F2 排序）。
/// </summary>
public sealed class XingqiPulseEngine
{
    private readonly XingqiConfig _config;

    public XingqiPulseEngine(XingqiConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 执行一个脉冲：所有存活角色增加行气。
    /// 返回达到阈值的角色 ID 列表（按 F2 排序规则排好）。
    /// </summary>
    public IReadOnlyList<string> AdvancePulse(
        IReadOnlyList<BattleCombatant> combatants,
        Dictionary<string, XingqiGauge> gauges,
        IReadOnlySet<string> playerSideIds)
    {
        foreach (var c in combatants)
        {
            if (!c.IsAlive) continue;
            if (!gauges.TryGetValue(c.Id, out var gauge)) continue;

            int gain = XingqiFormulaEngine.ComputeGain(_config, c.Agility);
            gauge.Advance(gain);
        }

        var readyList = new List<(string Id, int Xingqi, bool IsPlayer, int Agility)>();
        foreach (var c in combatants)
        {
            if (!c.IsAlive) continue;
            if (!gauges.TryGetValue(c.Id, out var gauge)) continue;
            if (!gauge.IsReady(_config.XingqiThreshold)) continue;

            readyList.Add((c.Id, gauge.CurrentValue, playerSideIds.Contains(c.Id), c.Agility));
        }

        readyList.Sort((a, b) =>
        {
            int cmp = b.Xingqi.CompareTo(a.Xingqi);
            if (cmp != 0) return cmp;

            // 玩家方优先
            int pa = a.IsPlayer ? 0 : 1;
            int pb = b.IsPlayer ? 0 : 1;
            cmp = pa.CompareTo(pb);
            if (cmp != 0) return cmp;

            cmp = b.Agility.CompareTo(a.Agility);
            if (cmp != 0) return cmp;

            return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        });

        return readyList.Select(r => r.Id).ToList();
    }
}
