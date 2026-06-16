using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// Boss 阶段定义。
/// </summary>
public sealed class BossPhaseConfig
{
    public int PhaseIndex { get; init; }
    public string Label { get; init; } = string.Empty;
    public float HpThreshold { get; init; } // HP ≤ 此值转入本阶段
    public int WeightGang { get; init; }
    public int WeightRou { get; init; }
    public int WeightQiao { get; init; }
    public MoveType WeaknessType { get; init; } = MoveType.Rou;
    public float CounterReadChance { get; init; }
    public int StaggerThreshold { get; init; } = 5;
    public bool IgnoreConsecutivePenalty { get; init; }
    public int PriorityStrikeCooldown { get; init; } // 0 = 不可用
    public int TypeLockCooldown { get; init; } // 0 = 不可用
    public int TypeLockDuration { get; init; } = 2;
    public int TypeLockBias { get; init; } = 65;
    public int ChargeFrequency { get; init; } // 0 = 不可用

    public TypeWeights GetBaseWeights() => new() { Gang = WeightGang, Rou = WeightRou, Qiao = WeightQiao };
}

/// <summary>
/// Boss 阶段脚本（所有阶段的集合定义）。
/// </summary>
public sealed class BossPhaseScript
{
    public IReadOnlyList<BossPhaseConfig> Phases { get; }

    public BossPhaseScript(IReadOnlyList<BossPhaseConfig> phases)
    {
        Phases = phases;
    }

    /// <summary>
    /// 根据 HP 比例获取当前阶段。阶段按 HP 阈值从高到低排序。
    /// </summary>
    public BossPhaseConfig GetPhaseForHp(float hpRatio)
    {
        for (int i = Phases.Count - 1; i >= 0; i--)
        {
            if (hpRatio <= Phases[i].HpThreshold)
                return Phases[i];
        }
        return Phases[0]; // fallback 第一阶段
    }

    /// <summary>
    /// 标准 4 阶段 Boss 模板（用于测试和默认值）。
    /// </summary>
    public static BossPhaseScript StandardFourPhase { get; } = new(new[]
    {
        new BossPhaseConfig
        {
            PhaseIndex = 0, Label = "试探", HpThreshold = 1.0f,
            WeightGang = 40, WeightRou = 25, WeightQiao = 35,
            CounterReadChance = 0f, StaggerThreshold = 5,
            PriorityStrikeCooldown = 0, TypeLockCooldown = 0, ChargeFrequency = 0
        },
        new BossPhaseConfig
        {
            PhaseIndex = 1, Label = "认真", HpThreshold = 0.6f,
            WeightGang = 25, WeightRou = 25, WeightQiao = 50,
            CounterReadChance = 0.4f, StaggerThreshold = 5,
            PriorityStrikeCooldown = 3, TypeLockCooldown = 0, ChargeFrequency = 4
        },
        new BossPhaseConfig
        {
            PhaseIndex = 2, Label = "穷途", HpThreshold = 0.3f,
            WeightGang = 50, WeightRou = 20, WeightQiao = 30,
            CounterReadChance = 0.3f, StaggerThreshold = 7,
            PriorityStrikeCooldown = 2, TypeLockCooldown = 2, TypeLockDuration = 2,
            ChargeFrequency = 2
        },
        new BossPhaseConfig
        {
            PhaseIndex = 3, Label = "绝境", HpThreshold = 0.15f,
            WeightGang = 70, WeightRou = 10, WeightQiao = 20,
            CounterReadChance = 0.25f, StaggerThreshold = 7,
            IgnoreConsecutivePenalty = true,
            PriorityStrikeCooldown = 1, TypeLockCooldown = 0, ChargeFrequency = 0
        }
    });
}

/// <summary>
/// Boss 阶段状态机 —— 管理阶段转换和阶段特殊机制状态。
/// </summary>
public sealed class BossPhaseManager
{
    private readonly BossPhaseScript _script;
    private int _currentPhaseIndex;
    private int _roundsSinceTypeLock;
    private int _typeLockRemainingRounds;
    private MoveType? _lockedType;
    private int _roundsSinceCharge;
    private AIMoveEntry? _pendingCharge;
    private int _roundsSincePriorityStrike;

    public BossPhaseConfig CurrentPhase => _script.Phases[_currentPhaseIndex];
    public bool IsTypeLockActive => _typeLockRemainingRounds > 0;
    public MoveType? LockedType => _lockedType;
    public AIMoveEntry? PendingCharge => _pendingCharge;
    public int CurrentPhaseIndex => _currentPhaseIndex;

    public BossPhaseManager(BossPhaseScript script)
    {
        _script = script;
        _currentPhaseIndex = 0;
        _roundsSinceTypeLock = 0;
        _roundsSinceCharge = 0;
        _roundsSincePriorityStrike = 0;
    }

    /// <summary>
    /// 检查并执行阶段转换。返回是否发生了转换。
    /// </summary>
    public bool CheckPhaseTransition(float hpRatio)
    {
        var newPhase = _script.GetPhaseForHp(hpRatio);
        if (newPhase.PhaseIndex > _currentPhaseIndex)
        {
            _currentPhaseIndex = newPhase.PhaseIndex;
            // 阶段转换：清除所有临时状态
            _typeLockRemainingRounds = 0;
            _lockedType = null;
            _roundsSinceTypeLock = 0;
            _pendingCharge = null;
            _roundsSinceCharge = 0;
            _roundsSincePriorityStrike = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 每回合开始调用：推进回合计数器，检查蓄力/锁定/优先攻击触发。
    /// </summary>
    public void AdvanceRound()
    {
        _roundsSinceTypeLock++;
        _roundsSinceCharge++;
        _roundsSincePriorityStrike++;

        // 体系锁定递减
        if (_typeLockRemainingRounds > 0)
        {
            _typeLockRemainingRounds--;
            if (_typeLockRemainingRounds == 0)
            {
                _lockedType = null;
                _roundsSinceTypeLock = 0;
            }
        }
    }

    /// <summary>
    /// 检查是否应触发蓄力预告。
    /// </summary>
    public bool ShouldCharge()
    {
        var phase = CurrentPhase;
        if (phase.ChargeFrequency <= 0) return false;
        return _pendingCharge == null && _roundsSinceCharge >= phase.ChargeFrequency;
    }

    /// <summary>
    /// 设置蓄力承诺。
    /// </summary>
    public void SetPendingCharge(AIMoveEntry move)
    {
        _pendingCharge = move;
        _roundsSinceCharge = 0;
    }

    /// <summary>
    /// 消费蓄力承诺（执行已承诺招式）。
    /// </summary>
    public AIMoveEntry? ConsumePendingCharge()
    {
        var charge = _pendingCharge;
        _pendingCharge = null;
        return charge;
    }

    /// <summary>
    /// 检查是否应触发体系锁定。
    /// </summary>
    public bool ShouldTypeLock()
    {
        var phase = CurrentPhase;
        if (phase.TypeLockCooldown <= 0) return false;
        return !IsTypeLockActive && _roundsSinceTypeLock >= phase.TypeLockCooldown;
    }

    /// <summary>
    /// 激活体系锁定。
    /// </summary>
    public void ActivateTypeLock(MoveType type)
    {
        _lockedType = type;
        _typeLockRemainingRounds = CurrentPhase.TypeLockDuration;
    }

    /// <summary>
    /// 检查是否可以使用优先攻击。
    /// </summary>
    public bool CanPriorityStrike()
    {
        var phase = CurrentPhase;
        if (phase.PriorityStrikeCooldown <= 0) return false;
        return _roundsSincePriorityStrike >= phase.PriorityStrikeCooldown;
    }

    /// <summary>
    /// 记录使用了优先攻击。
    /// </summary>
    public void RecordPriorityStrike()
    {
        _roundsSincePriorityStrike = 0;
    }

    /// <summary>
    /// 计算体系锁定期间的权重。
    /// GDD: locked_type = type_lock_bias, 其余按原比例分配。
    /// </summary>
    public TypeWeights ComputeLockedWeights()
    {
        if (!IsTypeLockActive || _lockedType == null)
            return CurrentPhase.GetBaseWeights();

        var phase = CurrentPhase;
        int bias = phase.TypeLockBias;
        int remaining = 100 - bias;

        int baseOther1, baseOther2;
        MoveType other1, other2;

        switch (_lockedType.Value)
        {
            case MoveType.Gang:
                other1 = MoveType.Rou; baseOther1 = phase.WeightRou;
                other2 = MoveType.Qiao; baseOther2 = phase.WeightQiao;
                break;
            case MoveType.Rou:
                other1 = MoveType.Gang; baseOther1 = phase.WeightGang;
                other2 = MoveType.Qiao; baseOther2 = phase.WeightQiao;
                break;
            default: // Qiao
                other1 = MoveType.Gang; baseOther1 = phase.WeightGang;
                other2 = MoveType.Rou; baseOther2 = phase.WeightRou;
                break;
        }

        int totalOther = baseOther1 + baseOther2;
        int w1 = totalOther > 0 ? (int)(remaining * ((float)baseOther1 / totalOther)) : remaining / 2;
        int w2 = remaining - w1;

        int gang = 0, rou = 0, qiao = 0;
        switch (_lockedType.Value)
        {
            case MoveType.Gang: gang = bias; rou = w1; qiao = w2; break;
            case MoveType.Rou: gang = w1; rou = bias; qiao = w2; break;
            case MoveType.Qiao: gang = w1; rou = w2; qiao = bias; break;
        }

        return new TypeWeights { Gang = gang, Rou = rou, Qiao = qiao };
    }
}
