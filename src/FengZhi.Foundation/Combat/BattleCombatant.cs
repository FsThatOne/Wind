using FengZhi.Foundation.Animation;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.Board;

namespace FengZhi.Foundation.Combat;

/// <summary>
/// 战斗中角色的运行时数据模型。POCO 结构，不继承 Node。
/// 封装 HP/内息/破绽/攻防速暴 等资源及其修改方法。
/// </summary>
public sealed class BattleCombatant
{
    public const int DefaultStaggerThreshold = 5;

    public string Id { get; }
    public string Name { get; }

    // --- Resources ---
    public int MaxHP { get; }
    public int HP { get; private set; }

    public int MaxNeixi { get; }
    public int Neixi { get; private set; }

    public int Stagger { get; private set; }
    public int StaggerThreshold { get; }

    // --- Stats ---
    public int AttackGang { get; }
    public int AttackRou { get; }
    public int AttackQiao { get; }
    public int Defense { get; }
    public int Speed { get; }
    public int Agility { get; }
    public float CritRate { get; }
    public int InsightStat { get; }
    public int NeixiRecovery { get; }

    // --- Xingqi ---
    public int Xingqi { get; private set; }

    // --- Board ---
    public GridPosition Position { get; private set; }
    public Iso4Direction Facing { get; private set; }
    public int MoveRange { get; }

    public BattleCombatant(
        string id,
        string name,
        int maxHP,
        int maxNeixi,
        int attackGang,
        int attackRou,
        int attackQiao,
        int defense,
        int speed,
        float critRate,
        int insightStat,
        int neixiRecovery,
        int staggerThreshold = DefaultStaggerThreshold,
        int agility = -1,
        GridPosition initialPosition = default,
        Iso4Direction initialFacing = Iso4Direction.SE,
        int moveRange = 3)
    {
        Id = id;
        Name = name;
        MaxHP = maxHP;
        HP = maxHP;
        MaxNeixi = maxNeixi;
        Neixi = maxNeixi;
        Stagger = 0;
        StaggerThreshold = staggerThreshold;
        AttackGang = attackGang;
        AttackRou = attackRou;
        AttackQiao = attackQiao;
        Defense = defense;
        Speed = speed;
        Agility = agility >= 0 ? agility : speed;
        CritRate = critRate;
        InsightStat = insightStat;
        NeixiRecovery = neixiRecovery;
        Position = initialPosition;
        Facing = initialFacing;
        MoveRange = moveRange;
    }

    // --- Status Queries ---

    public bool IsAlive => HP > 0;
    public bool IsStaggerExposed => Stagger >= StaggerThreshold;
    public bool CanAfford(int neixiCost) => Neixi >= neixiCost;

    /// <summary>
    /// 获取对应体系的攻击力。
    /// </summary>
    public int GetAttackForType(MoveType type) => type switch
    {
        MoveType.Gang => AttackGang,
        MoveType.Rou => AttackRou,
        MoveType.Qiao => AttackQiao,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // --- Resource Modification ---

    /// <summary>
    /// 受到伤害。HP 钳位到 0。
    /// </summary>
    public void ApplyDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "伤害不能为负");
        HP = Math.Max(0, HP - amount);
    }

    /// <summary>
    /// 消耗内息。内息不足时返回 false 且不扣除。
    /// </summary>
    public bool SpendNeixi(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "消耗不能为负");
        if (Neixi < amount) return false;
        Neixi -= amount;
        return true;
    }

    /// <summary>
    /// 回复内息。钳位到 MaxNeixi。
    /// </summary>
    public void RecoverNeixi(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "回复不能为负");
        Neixi = Math.Min(MaxNeixi, Neixi + amount);
    }

    /// <summary>
    /// 累积破绽。
    /// </summary>
    public void AddStagger(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "破绽增加不能为负");
        Stagger += amount;
    }

    /// <summary>
    /// 破绽衰减 1，最低到 0。
    /// </summary>
    public void DecayStagger()
    {
        Stagger = Math.Max(0, Stagger - 1);
    }

    /// <summary>
    /// 清空破绽（一击决胜后）。
    /// </summary>
    public void ClearStagger()
    {
        Stagger = 0;
    }

    // --- Xingqi Operations ---

    public void AdvanceXingqi(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Xingqi += amount;
    }

    public void ResetXingqi(int retainedAmount = 0)
    {
        Xingqi = Math.Max(0, retainedAmount);
    }

    // --- Board Operations ---

    public void MoveTo(GridPosition newPosition)
    {
        Position = newPosition;
    }

    public void SetFacing(Iso4Direction facing)
    {
        Facing = facing;
    }
}
