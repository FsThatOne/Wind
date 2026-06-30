using System;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会 mod 计算器 — 负责重算并写出 misunderstanding_mod。</summary>
public sealed class MisunderstandingModCalculator
{
    private readonly IMisunderstandingRegistry _registry;
    private readonly INpcStateWriter _writer;

    public MisunderstandingModCalculator(IMisunderstandingRegistry registry, INpcStateWriter writer)
    {
        _registry = registry;
        _writer = writer;
    }

    /// <summary>重算指定 NPC 的 misunderstanding_mod 并写出。幂等。</summary>
    public int RecomputeAndWrite(string npcId)
    {
        var mod = ComputeMod(npcId);
        _writer.SetMisunderstandingMod(npcId, mod);
        return mod;
    }

    /// <summary>纯计算，不写出。</summary>
    public int ComputeMod(string npcId)
    {
        var active = _registry.GetActive(npcId);
        if (active.Count == 0)
            return 0;

        int maxMod = 0;
        foreach (var inst in active)
        {
            var m = MisunderstandingInstance.SeverityToMod(inst.Severity);
            maxMod = Math.Min(maxMod, m);
        }

        return Math.Clamp(maxMod, -2, 0);
    }
}
