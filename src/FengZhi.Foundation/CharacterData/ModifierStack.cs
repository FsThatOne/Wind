namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 属性修改器栈。管理三层修改器的添加、移除、计算和生命周期。
/// 核心规则：同源不叠加（取最高值），不同源正常叠加。
/// </summary>
public sealed class ModifierStack
{
    private readonly List<AttributeModifier> _modifiers = new();

    /// <summary>当前所有修改器数量</summary>
    public int Count => _modifiers.Count;

    /// <summary>
    /// 添加修改器。同源同属性同层级时，仅保留最高值。
    /// </summary>
    public void Add(AttributeModifier modifier)
    {
        // 同源检查：同一 Source + 同一 Attribute + 同一 Layer → 只保留最高值
        var existing = _modifiers.Find(m =>
            m.Source == modifier.Source &&
            m.Attribute == modifier.Attribute &&
            m.Layer == modifier.Layer);

        if (existing != null)
        {
            if (modifier.Value > existing.Value)
            {
                _modifiers.Remove(existing);
                _modifiers.Add(modifier);
            }
            // 否则忽略（现有值更高）
            return;
        }

        _modifiers.Add(modifier);
    }

    /// <summary>
    /// 按来源移除所有修改器。
    /// </summary>
    public void Remove(string source)
    {
        _modifiers.RemoveAll(m => m.Source == source);
    }

    /// <summary>
    /// 按来源和属性移除特定修改器。
    /// </summary>
    public void Remove(string source, AttributeType attribute)
    {
        _modifiers.RemoveAll(m => m.Source == source && m.Attribute == attribute);
    }

    /// <summary>
    /// 计算指定属性的修改器总和。
    /// </summary>
    public int GetSum(AttributeType attribute)
    {
        int sum = 0;
        foreach (var mod in _modifiers)
        {
            if (mod.Attribute == attribute)
                sum += mod.Value;
        }
        return sum;
    }

    /// <summary>
    /// 回合推进：临时层修改器 duration 递减，到期（=0）移除。
    /// duration=-1 的临时修改器不受影响（需手动移除或 ClearTemporary）。
    /// </summary>
    public void TickTurn()
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            var mod = _modifiers[i];
            if (mod.Layer != ModifierLayer.Temporary) continue;
            if (mod.Duration <= 0) continue; // -1=无限，0=已到期（不应存在）

            mod.Duration--;
            if (mod.Duration <= 0)
            {
                _modifiers.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 清除所有临时层修改器（战斗结束时调用）。
    /// </summary>
    public void ClearTemporary()
    {
        _modifiers.RemoveAll(m => m.Layer == ModifierLayer.Temporary);
    }

    /// <summary>
    /// 清除所有修改器（慎用）。
    /// </summary>
    public void ClearAll()
    {
        _modifiers.Clear();
    }

    /// <summary>
    /// 获取指定层的所有修改器（只读）。
    /// </summary>
    public IReadOnlyList<AttributeModifier> GetByLayer(ModifierLayer layer)
    {
        return _modifiers.Where(m => m.Layer == layer).ToList();
    }

    /// <summary>
    /// 获取指定来源的所有修改器（只读）。
    /// </summary>
    public IReadOnlyList<AttributeModifier> GetBySource(string source)
    {
        return _modifiers.Where(m => m.Source == source).ToList();
    }

    /// <summary>
    /// 检查是否存在指定来源的修改器。
    /// </summary>
    public bool HasSource(string source)
    {
        return _modifiers.Any(m => m.Source == source);
    }
}
