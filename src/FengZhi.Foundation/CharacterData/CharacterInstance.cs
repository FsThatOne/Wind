namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 角色运行时实例。持有 CharacterAttributes + ModifierStack，
/// 暴露 GDD 定义的全部下游查询接口。
/// </summary>
public sealed class CharacterInstance
{
    /// <summary>运行时唯一 ID: "{type}_{templateId}_{guid_short}"</summary>
    public string RuntimeId { get; }

    /// <summary>角色属性数据</summary>
    public CharacterAttributes Attributes { get; }

    /// <summary>属性修改器栈</summary>
    public ModifierStack Modifiers { get; }

    /// <summary>来源模板 ID</summary>
    public string TemplateId => Attributes.TemplateId;

    /// <summary>角色类型</summary>
    public CharacterType Type => Attributes.Type;

    /// <summary>是否存活</summary>
    public bool IsAlive => Attributes.CurrentHp > 0;

    public CharacterInstance(string runtimeId, CharacterAttributes attributes, ModifierStack modifiers)
    {
        RuntimeId = runtimeId;
        Attributes = attributes;
        Modifiers = modifiers;
    }

    // ─── 下游查询接口（含修改器） ──────────────────────────

    /// <summary>F1: 按体系计算攻击力（含修改器）</summary>
    public int GetAttackForType(MoveType type)
    {
        int modSum = Modifiers.GetSum(AttributeType.Attack);
        return FormulaEngine.AttackForType(type, Attributes.BaseAttack, Attributes.ScalingFactor, modSum,
            Attributes.Strength, Attributes.Agility, Attributes.InnerPower);
    }

    /// <summary>F2: 防御力（含修改器）</summary>
    public int GetDefense()
    {
        int modSum = Modifiers.GetSum(AttributeType.Defense);
        return FormulaEngine.Defense(Attributes.Constitution, Attributes.Strength, modSum);
    }

    /// <summary>F3: 速度（含修改器）</summary>
    public int GetSpeed()
    {
        int modSum = Modifiers.GetSum(AttributeType.Speed);
        return FormulaEngine.Speed(Attributes.Agility, Attributes.Insight, modSum);
    }

    /// <summary>F7: 暴击率（含修改器）</summary>
    public float GetCritRate()
    {
        float modSum = Modifiers.GetSum(AttributeType.CritRate) * 0.01f;
        return FormulaEngine.CritRate(Attributes.Agility, modSum);
    }

    /// <summary>F4: HP 上限（含修改器）</summary>
    public int GetMaxHp()
    {
        int modSum = Modifiers.GetSum(AttributeType.MaxHp);
        return FormulaEngine.MaxHp(Attributes.MaxHp, Attributes.Constitution, modSum);
    }

    /// <summary>F5: 内息上限（含修改器）</summary>
    public int GetMaxNeiXi()
    {
        int modSum = Modifiers.GetSum(AttributeType.MaxNeiXi);
        return FormulaEngine.MaxNeiXi(Attributes.MaxNeiXi, Attributes.InnerPower, modSum);
    }

    /// <summary>F6: 内息回复（含修改器）</summary>
    public int GetNeiXiRecovery()
    {
        float modSum = Modifiers.GetSum(AttributeType.NeiXiRecovery);
        return FormulaEngine.NeiXiRecovery(Attributes.InnerPower, modSum);
    }

    /// <summary>功力总值（五维之和）</summary>
    public int GetTotalPower()
    {
        return FormulaEngine.TotalPower(
            Attributes.Strength, Attributes.Agility, Attributes.InnerPower,
            Attributes.Insight, Attributes.Constitution);
    }
}
