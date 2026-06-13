using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// AI 决策输出结果。
/// </summary>
public sealed class AIDecision
{
    /// <summary>
    /// 行动类型。
    /// </summary>
    public AIActionType ActionType { get; init; }

    /// <summary>
    /// 选定的体系（调息时为 null）。
    /// </summary>
    public MoveType? SelectedType { get; init; }

    /// <summary>
    /// 选定的招式（调息/蓄力动作时为 null）。
    /// </summary>
    public AIMoveEntry? SelectedMove { get; init; }

    /// <summary>
    /// 选定的目标 ID。
    /// </summary>
    public string? TargetId { get; init; }

    /// <summary>
    /// 是否使用了优先攻击。
    /// </summary>
    public bool IsPriorityStrike { get; init; }

    /// <summary>
    /// 是否触发了反读（用于 UI tell 信号）。
    /// </summary>
    public bool CounterReadTriggered { get; init; }

    /// <summary>
    /// 蓄力预告的招式名（预告回合输出）。
    /// </summary>
    public string? ChargeAnnounceName { get; init; }
}

public enum AIActionType
{
    Attack,
    Meditate,
    ChargeAnnounce // 预告回合（本回合执行蓄力动作）
}

/// <summary>
/// AI 决策上下文（由战斗系统提供）。
/// </summary>
public sealed class AIBattleContext
{
    public string SelfId { get; init; } = string.Empty;
    public float HpRatio { get; init; } = 1.0f;
    public int CurrentNeixi { get; init; }
    public int CurrentStagger { get; init; }
    public int CurrentRound { get; init; } = 1;
    public MoveType? LastRoundType { get; init; }
    public int ConsecutiveSameTypeCount { get; init; }
    public int ConsecutiveMeditationCount { get; init; }
    public bool WasCounteredLastRound { get; init; }
    public int SelfStagger { get; init; }
    public int TargetStagger { get; init; }
    public IReadOnlyList<AIMoveEntry> AvailableMoves { get; init; } = Array.Empty<AIMoveEntry>();
    public IReadOnlyList<TargetCandidate> Targets { get; init; } = Array.Empty<TargetCandidate>();
    public IReadOnlySet<string> AlreadyTargetedIds { get; init; } = new HashSet<string>();
}

/// <summary>
/// EnemyBrain：AI 完整决策管线。
/// 整合所有子模块（ai-001 ~ ai-007）为统一管线。
/// GDD §Core Rules 1: 管线求值顺序 ⓪→①→②③④→⑤→⑥→⑦→⑧⑨。
/// </summary>
public sealed class EnemyBrain
{
    private readonly PersonalityTemplate? _personality;
    private readonly AIStateMachine _stateMachine;
    private readonly SignatureTracker? _signatureTracker;
    private readonly CounterReadSystem _counterRead;
    private readonly BossPhaseManager? _bossManager;
    private readonly IAIRandomSource _random;

    /// <summary>
    /// 普通敌人构造。
    /// </summary>
    public EnemyBrain(
        PersonalityTemplate personality,
        SignaturePattern? signaturePattern,
        IAIRandomSource random)
    {
        _personality = personality;
        _stateMachine = new AIStateMachine();
        _signatureTracker = signaturePattern != null ? new SignatureTracker(signaturePattern) : null;
        _counterRead = new CounterReadSystem();
        _bossManager = null;
        _random = random;
    }

    /// <summary>
    /// Boss 构造。
    /// </summary>
    public EnemyBrain(
        BossPhaseScript bossScript,
        SignaturePattern? signaturePattern,
        IAIRandomSource random)
    {
        _personality = null;
        _stateMachine = new AIStateMachine();
        _signatureTracker = signaturePattern != null ? new SignatureTracker(signaturePattern) : null;
        _counterRead = new CounterReadSystem();
        _bossManager = new BossPhaseManager(bossScript);
        _random = random;
    }

    public AIStateMachine StateMachine => _stateMachine;
    public CounterReadSystem CounterRead => _counterRead;
    public BossPhaseManager? BossManager => _bossManager;
    public SignatureTracker? SignatureTracker => _signatureTracker;

    /// <summary>
    /// 执行完整决策管线。
    /// </summary>
    public AIDecision Decide(AIBattleContext ctx)
    {
        // ⓪ 蓄力检查（仅 Boss）
        if (_bossManager != null)
        {
            var pendingCharge = _bossManager.ConsumePendingCharge();
            if (pendingCharge != null)
            {
                return new AIDecision
                {
                    ActionType = AIActionType.Attack,
                    SelectedType = pendingCharge.Type,
                    SelectedMove = pendingCharge,
                    TargetId = SelectTarget(ctx),
                    IsPriorityStrike = CheckPriorityStrike()
                };
            }
        }

        // ① 调息前置检查
        if (MeditationDecision.ShouldMeditate(
            ctx.CurrentNeixi, _random, ctx.ConsecutiveMeditationCount))
        {
            return new AIDecision { ActionType = AIActionType.Meditate };
        }

        // Phase A: ②③④ 计算权重并选定体系
        var rawWeights = ComputeRawWeights(ctx);
        var probabilities = TypeSelectionEngine.ComputeProbabilities(rawWeights);
        var selectedType = TypeSelectionEngine.SelectType(probabilities, _random);

        // Boss 蓄力预告检查
        if (_bossManager != null && _bossManager.ShouldCharge())
        {
            // 预告回合：用正常管线选定招式但不立即执行
            var chargeMove = SelectMoveForType(ctx, selectedType);
            if (chargeMove != null)
            {
                _bossManager.SetPendingCharge(chargeMove);
                return new AIDecision
                {
                    ActionType = AIActionType.ChargeAnnounce,
                    SelectedType = chargeMove.Type,
                    ChargeAnnounceName = chargeMove.Name
                };
            }
        }

        // Phase B: ⑤ 反读否决
        bool counterReadTriggered = false;
        float counterReadChance = GetCounterReadChance();
        if (counterReadChance > 0)
        {
            var counterType = _counterRead.TryCounterRead(counterReadChance, _random);
            if (counterType != null)
            {
                selectedType = counterType.Value;
                counterReadTriggered = true;
            }
        }

        // ⑥ 目标选择
        string? targetId = SelectTarget(ctx);

        // ⑦ 体系内选招
        var move = SelectMoveForType(ctx, selectedType);

        // 更新签名追踪
        _signatureTracker?.RecordChoice(selectedType);

        return new AIDecision
        {
            ActionType = AIActionType.Attack,
            SelectedType = selectedType,
            SelectedMove = move,
            TargetId = targetId,
            IsPriorityStrike = CheckPriorityStrike(),
            CounterReadTriggered = counterReadTriggered
        };
    }

    /// <summary>
    /// 记录玩家出招（供反读系统使用）。
    /// </summary>
    public void RecordPlayerAction(string playerId, MoveType type)
    {
        _counterRead.RecordPlayerAction(playerId, type);
    }

    /// <summary>
    /// 记录被克制（供状态机使用）。
    /// </summary>
    public void RecordCountered()
    {
        _stateMachine.RecordCountered();
    }

    /// <summary>
    /// 更新 HP（供状态机/Boss 阶段使用）。
    /// </summary>
    public bool UpdateHp(float hpRatio)
    {
        _stateMachine.UpdateHP(hpRatio);
        if (_bossManager != null)
            return _bossManager.CheckPhaseTransition(hpRatio);
        return false;
    }

    /// <summary>
    /// 推进回合（Boss 用）。
    /// </summary>
    public void AdvanceRound()
    {
        _bossManager?.AdvanceRound();
    }

    private TypeWeights ComputeRawWeights(AIBattleContext ctx)
    {
        // 基础权重
        TypeWeights baseWeights;
        bool ignoreConsecutivePenalty = false;

        if (_bossManager != null)
        {
            if (_bossManager.IsTypeLockActive)
            {
                baseWeights = _bossManager.ComputeLockedWeights();
                ignoreConsecutivePenalty = true; // 锁定期间暂停连续惩罚
            }
            else
            {
                var phase = _bossManager.CurrentPhase;
                baseWeights = phase.GetBaseWeights();
                ignoreConsecutivePenalty = phase.IgnoreConsecutivePenalty;
            }

            // Boss 体系锁定触发检查
            if (_bossManager.ShouldTypeLock())
            {
                // 锁定当前阶段权重最高的体系
                var phase = _bossManager.CurrentPhase;
                MoveType lockType = MoveType.Gang;
                int max = phase.WeightGang;
                if (phase.WeightRou > max) { lockType = MoveType.Rou; max = phase.WeightRou; }
                if (phase.WeightQiao > max) { lockType = MoveType.Qiao; }
                _bossManager.ActivateTypeLock(lockType);
                baseWeights = _bossManager.ComputeLockedWeights();
                ignoreConsecutivePenalty = true;
            }
        }
        else
        {
            baseWeights = new TypeWeights
            {
                Gang = _personality!.WeightGang,
                Rou = _personality.WeightRou,
                Qiao = _personality.WeightQiao
            };
        }

        // 状态修正
        var stateMod = _stateMachine.GetStateModifier(ctx.LastRoundType);

        // 情境修正
        var sitMod = SituationalModifiers.Compute(
            ctx.SelfStagger, ctx.TargetStagger,
            ctx.WasCounteredLastRound, ctx.LastRoundType);

        // 连续惩罚
        TypeWeights consPenalty = default;
        if (!ignoreConsecutivePenalty && ctx.LastRoundType.HasValue)
        {
            consPenalty = ConsecutivePenalty.Compute(ctx.ConsecutiveSameTypeCount, ctx.LastRoundType.Value);
        }

        // 招式预兆加成
        TypeWeights sigBonus = default;
        if (_signatureTracker != null)
        {
            sigBonus = _signatureTracker.GetSignatureBonus(ctx.CurrentRound);
        }

        // 聚合：base + state + situational + consecutive + signature
        return new TypeWeights
        {
            Gang = baseWeights.Gang + stateMod.Gang + sitMod.Gang + consPenalty.Gang + sigBonus.Gang,
            Rou = baseWeights.Rou + stateMod.Rou + sitMod.Rou + consPenalty.Rou + sigBonus.Rou,
            Qiao = baseWeights.Qiao + stateMod.Qiao + sitMod.Qiao + consPenalty.Qiao + sigBonus.Qiao
        };
    }

    private AIMoveEntry? SelectMoveForType(AIBattleContext ctx, MoveType type)
    {
        return MoveSelector.SelectMove(ctx.AvailableMoves, type, ctx.CurrentNeixi, _random);
    }

    private string? SelectTarget(AIBattleContext ctx)
    {
        if (ctx.Targets.Count == 0) return null;
        var target = TargetSelector.SelectTarget(ctx.Targets, ctx.AlreadyTargetedIds, _random);
        return target?.Id;
    }

    private float GetCounterReadChance()
    {
        if (_bossManager != null)
        {
            var phase = _bossManager.CurrentPhase;
            // 锁定期间反读概率减半
            float chance = phase.CounterReadChance;
            if (_bossManager.IsTypeLockActive)
                chance *= 0.5f;
            return chance;
        }
        else
        {
            return _personality!.CounterReadEnabled ? _personality.CounterReadChance : 0f;
        }
    }

    private bool CheckPriorityStrike()
    {
        if (_bossManager == null) return false;
        if (_bossManager.CanPriorityStrike())
        {
            _bossManager.RecordPriorityStrike();
            return true;
        }
        return false;
    }
}
