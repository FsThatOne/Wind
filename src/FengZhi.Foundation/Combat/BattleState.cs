namespace FengZhi.Foundation.Combat;

/// <summary>
/// 战斗实例的阶段枚举。按 GDD §Core Rules 2 的回合流程定义。
/// </summary>
public enum BattlePhase
{
    /// <summary>战斗初始化：加载参战角色、设置资源初始值</summary>
    Initializing,

    /// <summary>回合开始：内息回复、破绽衰减、持续效果检查</summary>
    RoundStart,

    /// <summary>意图公开：敌方体系类型亮出</summary>
    IntentReveal,

    /// <summary>玩家决策：选择行动</summary>
    PlayerDecision,

    /// <summary>同时结算：所有行动逻辑结算</summary>
    Resolution,

    /// <summary>回合结束：检查胜负条件</summary>
    RoundEnd,

    /// <summary>战斗结束</summary>
    BattleOver
}

/// <summary>
/// 战斗结果类型。
/// </summary>
public enum BattleResult
{
    /// <summary>仍在进行</summary>
    InProgress,

    /// <summary>玩家胜利（敌方全灭）</summary>
    Victory,

    /// <summary>玩家失败（己方全灭）</summary>
    Defeat,

    /// <summary>惜败（双方同时归零）</summary>
    NarrowDefeat,

    /// <summary>平局（达到最大回合数）</summary>
    Draw
}

/// <summary>
/// 战斗实例。管理回合流程状态机。
/// 外部通过 AdvancePhase() 驱动，适配同步测试和异步 UI。
/// </summary>
public sealed class BattleInstance
{
    private readonly List<BattleCombatant> _playerParty;
    private readonly List<BattleCombatant> _enemyGroup;
    private readonly int _maxRounds;

    public BattlePhase CurrentPhase { get; private set; }
    public int CurrentRound { get; private set; }
    public int MaxRounds => _maxRounds;
    public BattleResult Result { get; private set; }

    public IReadOnlyList<BattleCombatant> PlayerParty => _playerParty;
    public IReadOnlyList<BattleCombatant> EnemyGroup => _enemyGroup;

    public BattleInstance(
        IEnumerable<BattleCombatant> playerParty,
        IEnumerable<BattleCombatant> enemyGroup,
        int maxRounds = 15)
    {
        _playerParty = new List<BattleCombatant>(playerParty);
        _enemyGroup = new List<BattleCombatant>(enemyGroup);
        _maxRounds = maxRounds;
        CurrentPhase = BattlePhase.Initializing;
        CurrentRound = 0;
        Result = BattleResult.InProgress;
    }

    /// <summary>
    /// 推进到下一阶段。返回推进后的阶段。
    /// 如果已处于 BattleOver 则不再推进。
    /// </summary>
    public BattlePhase AdvancePhase()
    {
        if (CurrentPhase == BattlePhase.BattleOver)
            return BattlePhase.BattleOver;

        CurrentPhase = CurrentPhase switch
        {
            BattlePhase.Initializing => BeginFirstRound(),
            BattlePhase.RoundStart => BattlePhase.IntentReveal,
            BattlePhase.IntentReveal => BattlePhase.PlayerDecision,
            BattlePhase.PlayerDecision => BattlePhase.Resolution,
            BattlePhase.Resolution => BattlePhase.RoundEnd,
            BattlePhase.RoundEnd => ProcessRoundEnd(),
            _ => BattlePhase.BattleOver
        };

        return CurrentPhase;
    }

    private BattlePhase BeginFirstRound()
    {
        CurrentRound = 1;
        ApplyRoundStartEffects();
        return BattlePhase.RoundStart;
    }

    private BattlePhase ProcessRoundEnd()
    {
        // Check battle end conditions
        var result = CheckBattleEndCondition();
        if (result != BattleResult.InProgress)
        {
            Result = result;
            return BattlePhase.BattleOver;
        }

        // Max rounds check
        if (CurrentRound >= _maxRounds)
        {
            Result = BattleResult.Draw;
            return BattlePhase.BattleOver;
        }

        // Start next round
        CurrentRound++;
        ApplyRoundStartEffects();
        return BattlePhase.RoundStart;
    }

    private void ApplyRoundStartEffects()
    {
        var allCombatants = _playerParty.Concat(_enemyGroup);
        foreach (var combatant in allCombatants)
        {
            if (!combatant.IsAlive) continue;
            combatant.RecoverNeixi(combatant.NeixiRecovery);
            combatant.DecayStagger();
        }
    }

    private BattleResult CheckBattleEndCondition()
    {
        bool allPlayersDead = _playerParty.All(c => !c.IsAlive);
        bool allEnemiesDead = _enemyGroup.All(c => !c.IsAlive);

        if (allPlayersDead && allEnemiesDead)
            return BattleResult.NarrowDefeat;
        if (allEnemiesDead)
            return BattleResult.Victory;
        if (allPlayersDead)
            return BattleResult.Defeat;

        return BattleResult.InProgress;
    }
}
