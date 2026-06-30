using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会注册表接口 — 管理误会实例的创建和查询。</summary>
public interface IMisunderstandingRegistry
{
    /// <summary>尝试注册一条新误会。若 NPC 已 BROKEN 或超出上限则返回 false。</summary>
    bool TryRegister(MisunderstandingInstance instance);

    /// <summary>获取指定 NPC 的所有活跃误会（Active/Escalated/Permanent）。</summary>
    IReadOnlyList<MisunderstandingInstance> GetActive(string npcId);

    /// <summary>获取所有已注册误会（含 Dormant/Resolved/Broken）。</summary>
    IReadOnlyList<MisunderstandingInstance> GetAll(string npcId);

    /// <summary>按 ID 查找误会。</summary>
    bool TryGet(string instanceId, [NotNullWhen(true)] out MisunderstandingInstance? instance);

    /// <summary>判断 NPC 是否有任何活跃误会。</summary>
    bool HasActive(string npcId);

    /// <summary>判断 NPC 是否处于 BROKEN 状态（任一误会为 Broken）。</summary>
    bool IsBroken(string npcId);
}

/// <summary>误会注册表的内存实现。</summary>
public sealed class MisunderstandingRegistry : IMisunderstandingRegistry
{
    private readonly Dictionary<string, MisunderstandingInstance> _byId = new();
    private readonly Dictionary<string, List<MisunderstandingInstance>> _byNpc = new();
    private readonly int _maxActivePerNpc;

    public MisunderstandingRegistry(int maxActivePerNpc = 3)
    {
        _maxActivePerNpc = maxActivePerNpc;
    }

    public bool TryRegister(MisunderstandingInstance instance)
    {
        if (IsBroken(instance.TargetNpc))
            return false;

        var active = GetActive(instance.TargetNpc);
        if (active.Count >= _maxActivePerNpc)
            return false;

        _byId[instance.Id] = instance;

        if (!_byNpc.TryGetValue(instance.TargetNpc, out var list))
        {
            list = new List<MisunderstandingInstance>();
            _byNpc[instance.TargetNpc] = list;
        }
        list.Add(instance);
        return true;
    }

    public IReadOnlyList<MisunderstandingInstance> GetActive(string npcId)
    {
        if (!_byNpc.TryGetValue(npcId, out var list))
            return [];

        var result = new List<MisunderstandingInstance>();
        foreach (var inst in list)
        {
            if (inst.State is MisunderstandingState.Active or MisunderstandingState.Escalated or MisunderstandingState.Permanent)
                result.Add(inst);
        }
        return result;
    }

    public IReadOnlyList<MisunderstandingInstance> GetAll(string npcId)
    {
        if (!_byNpc.TryGetValue(npcId, out var list))
            return [];
        return list;
    }

    public bool TryGet(string instanceId, [NotNullWhen(true)] out MisunderstandingInstance? instance)
    {
        return _byId.TryGetValue(instanceId, out instance);
    }

    public bool HasActive(string npcId)
    {
        return GetActive(npcId).Count > 0;
    }

    public bool IsBroken(string npcId)
    {
        if (!_byNpc.TryGetValue(npcId, out var list))
            return false;

        foreach (var inst in list)
        {
            if (inst.State == MisunderstandingState.Broken)
                return true;
        }
        return false;
    }
}
