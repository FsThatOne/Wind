namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 角色属性核心数据模型。
/// 三层结构：资源属性（HP/内息/破绽）、基础属性（五维）、派生属性（计算值）。
/// </summary>
public class CharacterAttributes
{
    // ─── 常量 ───────────────────────────────────────────────
    public const int AttributeMin = 1;
    public const int AttributeCap = 50;

    // ─── 资源属性 ───────────────────────────────────────────
    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; private set; } = 100;

    public int MaxNeiXi { get; set; } = 20;
    public int CurrentNeiXi { get; private set; } = 20;

    public int StaggerThreshold { get; set; } = 5;
    public int CurrentStagger { get; private set; }

    // ─── 基础属性（五维） ───────────────────────────────────
    private int _strength = 10;
    private int _agility = 10;
    private int _innerPower = 10;
    private int _insight = 10;
    private int _constitution = 10;

    /// <summary>力量 — 刚系攻击主属性</summary>
    public int Strength
    {
        get => _strength;
        set => _strength = Clamp(value);
    }

    /// <summary>敏捷 — 巧系攻击主属性、速度、暴击</summary>
    public int Agility
    {
        get => _agility;
        set => _agility = Clamp(value);
    }

    /// <summary>内力 — 柔系攻击主属性、内息</summary>
    public int InnerPower
    {
        get => _innerPower;
        set => _innerPower = Clamp(value);
    }

    /// <summary>洞察 — 速度副属性、特殊判定</summary>
    public int Insight
    {
        get => _insight;
        set => _insight = Clamp(value);
    }

    /// <summary>体魄 — 防御、HP上限、体力</summary>
    public int Constitution
    {
        get => _constitution;
        set => _constitution = Clamp(value);
    }

    // ─── 派生属性（FormulaEngine 驱动） ─────────────────────

    /// <summary>模板基础攻击力（由 YAML 配置）</summary>
    public int BaseAttack { get; set; } = 10;

    /// <summary>攻击力缩放系数（默认 1.0，心法可调）</summary>
    public float ScalingFactor { get; set; } = 1.0f;

    /// <summary>功力总值 = 五维之和</summary>
    public int TotalPower => FormulaEngine.TotalPower(Strength, Agility, InnerPower, Insight, Constitution);

    /// <summary>F4: HP上限</summary>
    public int ComputedMaxHp => FormulaEngine.MaxHp(MaxHp, Constitution, 0);

    /// <summary>F5: 内息上限</summary>
    public int ComputedMaxNeiXi => FormulaEngine.MaxNeiXi(MaxNeiXi, InnerPower, 0);

    /// <summary>F2: 防御力</summary>
    public int ComputedDefense => FormulaEngine.Defense(Constitution, Strength, 0);

    /// <summary>F7: 暴击率 (0.0~0.30)</summary>
    public float ComputedCritRate => FormulaEngine.CritRate(Agility, 0f);

    /// <summary>F6: 每回合内息回复</summary>
    public int ComputedNeiXiRecovery => FormulaEngine.NeiXiRecovery(InnerPower, 0f);

    /// <summary>F1: 根据招式体系获取攻击力</summary>
    public int GetAttackForType(MoveType type) =>
        FormulaEngine.AttackForType(type, BaseAttack, ScalingFactor, 0, Strength, Agility, InnerPower);

    // ─── 角色元数据 ─────────────────────────────────────────
    public CharacterType Type { get; set; } = CharacterType.Player;
    public string TemplateId { get; set; } = string.Empty;

    // ─── 资源属性操作 ───────────────────────────────────────

    /// <summary>
    /// 施加伤害。HP 钳位到 0（不为负）。
    /// </summary>
    public void ApplyDamage(int amount)
    {
        if (amount < 0) return;
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    /// <summary>
    /// 消耗内息。内息不足时返回 false 且不扣减。
    /// </summary>
    public bool SpendNeiXi(int amount)
    {
        if (amount < 0) return false;
        if (CurrentNeiXi < amount) return false;
        CurrentNeiXi -= amount;
        return true;
    }

    /// <summary>
    /// 累积破绽。
    /// </summary>
    public void AddStagger(int amount)
    {
        if (amount < 0) return;
        CurrentStagger += amount;
    }

    /// <summary>
    /// 破绽每回合衰减 1，钳位到 0。
    /// </summary>
    public void DecayStagger()
    {
        CurrentStagger = Math.Max(0, CurrentStagger - 1);
    }

    /// <summary>
    /// 战斗开始时重置：HP/内息设为最大值，破绽归零。
    /// </summary>
    public void ResetForCombat()
    {
        CurrentHp = MaxHp;
        CurrentNeiXi = MaxNeiXi;
        CurrentStagger = 0;
    }

    /// <summary>
    /// 回复内息（每回合）。amount 钳位到不超过上限。
    /// </summary>
    public void RecoverNeiXi(int amount)
    {
        if (amount < 0) return;
        CurrentNeiXi = Math.Min(MaxNeiXi, CurrentNeiXi + amount);
    }

    /// <summary>
    /// 直接设置当前 HP（用于加载存档等）。钳位到 [0, MaxHp]。
    /// </summary>
    public void SetCurrentHp(int value)
    {
        CurrentHp = Math.Clamp(value, 0, MaxHp);
    }

    /// <summary>
    /// 直接设置当前内息（用于加载存档等）。钳位到 [0, MaxNeiXi]。
    /// </summary>
    public void SetCurrentNeiXi(int value)
    {
        CurrentNeiXi = Math.Clamp(value, 0, MaxNeiXi);
    }

    /// <summary>
    /// 检查破绽是否达到阈值。
    /// </summary>
    public bool IsStaggered => CurrentStagger >= StaggerThreshold;

    // ─── 私有工具 ───────────────────────────────────────────

    private static int Clamp(int value)
    {
        return Math.Clamp(value, AttributeMin, AttributeCap);
    }
}
