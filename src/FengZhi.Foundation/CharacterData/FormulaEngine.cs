namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 角色属性公式引擎。所有方法为纯函数、零副作用。
/// 对应 GDD character-attributes.md §Formulas F1-F8 + TotalPower。
/// </summary>
public static class FormulaEngine
{
    // ─── F1. 体系攻击力 ─────────────────────────────────────

    /// <summary>
    /// F1: attack = base_attack + primary_attr × scaling_factor + modifier_sum
    /// </summary>
    public static int AttackForType(int baseAttack, int primaryAttr, float scalingFactor, int modifierSum)
    {
        return (int)Math.Round(baseAttack + primaryAttr * scalingFactor + modifierSum);
    }

    /// <summary>
    /// F1 便捷重载：根据 MoveType 从五维中选取主属性。
    /// </summary>
    public static int AttackForType(MoveType type, int baseAttack, float scalingFactor, int modifierSum,
        int strength, int agility, int innerPower)
    {
        int primaryAttr = type switch
        {
            MoveType.Gang => strength,
            MoveType.Qiao => agility,
            MoveType.Rou => innerPower,
            _ => strength
        };
        return AttackForType(baseAttack, primaryAttr, scalingFactor, modifierSum);
    }

    // ─── F2. 防御力 ─────────────────────────────────────────

    /// <summary>
    /// F2: defense = constitution × 0.6 + strength × 0.2 + modifier_sum
    /// </summary>
    public static int Defense(int constitution, int strength, int modifierSum)
    {
        return (int)Math.Round(constitution * 0.6 + strength * 0.2 + modifierSum);
    }

    // ─── F4. HP 上限 ────────────────────────────────────────

    /// <summary>
    /// F4: max_hp = base_hp + constitution × 4 + modifier_sum
    /// </summary>
    public static int MaxHp(int baseHp, int constitution, int modifierSum)
    {
        return baseHp + constitution * 4 + modifierSum;
    }

    // ─── F5. 内息上限 ───────────────────────────────────────

    /// <summary>
    /// F5: max_neixi = base_neixi + inner_power × 2 + modifier_sum
    /// </summary>
    public static int MaxNeiXi(int baseNeiXi, int innerPower, int modifierSum)
    {
        return baseNeiXi + innerPower * 2 + modifierSum;
    }

    // ─── F6. 内息回复 ───────────────────────────────────────

    /// <summary>
    /// F6: neixi_recovery = max(2, 2 + inner_power × 0.2 + modifier_sum)
    /// 下限硬锁 2。
    /// </summary>
    public static int NeiXiRecovery(int innerPower, float modifierSum)
    {
        int raw = (int)Math.Round(2 + innerPower * 0.2 + modifierSum);
        return Math.Max(2, raw);
    }

    // ─── F7. 暴击率 ─────────────────────────────────────────

    /// <summary>
    /// F7: crit_rate = min(30%, agility × 0.5% + modifier_sum)
    /// 返回值为小数形式 (如 0.10 = 10%)。上限 0.30。
    /// </summary>
    public static float CritRate(int agility, float modifierSum)
    {
        float raw = agility * 0.005f + modifierSum;
        return Math.Min(0.30f, raw);
    }

    // ─── F8. 相对强度比较 ───────────────────────────────────

    /// <summary>
    /// F8: ratio = selfPower / targetPower → PowerLevel
    /// </summary>
    public static PowerLevel ComparePower(int selfPower, int targetPower)
    {
        if (targetPower <= 0) return PowerLevel.FarStronger;
        if (selfPower <= 0) return PowerLevel.FarWeaker;

        float ratio = (float)selfPower / targetPower;

        return ratio switch
        {
            < 0.5f => PowerLevel.FarWeaker,
            < 0.8f => PowerLevel.Weaker,
            < 1.2f => PowerLevel.Comparable,
            < 2.0f => PowerLevel.Stronger,
            _ => PowerLevel.FarStronger
        };
    }

    // ─── TotalPower ─────────────────────────────────────────

    /// <summary>
    /// 功力总值 = 五维属性之和。
    /// </summary>
    public static int TotalPower(int strength, int agility, int innerPower, int insight, int constitution)
    {
        return strength + agility + innerPower + insight + constitution;
    }
}
