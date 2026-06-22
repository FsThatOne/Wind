# ADR-0017: Epiphany Breakthrough — Trigger Framework & Reward Pipeline

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Character Progression, Combat Integration, State Machine |
| **Knowledge Risk** | **LOW** — 纯逻辑系统，概率计算与状态管理不依赖引擎特定 API |
| **References Consulted** | `design/gdd/epiphany-breakthrough.md`, ADR-0001 (EventBus), ADR-0003 (Data Schema), ADR-0004 (Save), ADR-0008 (FSM) |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) 验证凝神状态与战斗系统暂停/恢复的交互无死锁; 2) 验证境界突破串联演出时序不冲突 ADR-0013 CutsceneQueue |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — OnRoundEnd/顿悟触发事件发布), ADR-0003 (Data Schema — 顿悟事件 YAML 配置结构), ADR-0004 (Save — 顿悟状态/skip_count/配额持久化), ADR-0008 (FSM — 角色状态凝神/悟后buff), ADR-0013 (Cutscene — 顿悟与境界突破串联演出，复用 `LockMode` 共享枚举), ADR-0014 (Living Jianghu — ConditionEvaluator 共享条件引擎) |
| **Enables** | 队伍管理读取同伴顿悟奖励池 (ADR-0016), 武学组合解锁新招式, 角色属性境界突破 |
| **Blocks** | Sprint 6+ (战斗绝境触发、冥想场景交互) |
| **Ordering Note** | ADR-0001 (EventBus) 和 ADR-0008 (FSM) 必须就绪; ADR-0013 (CutsceneSystem) 和 ADR-0014 (ConditionEvaluator) 就绪后对接演出与条件判定 |

## Context

GDD #17 定义了一个泛场景触发框架，通过三种路径（战斗绝境、叙事脚本、呼吸期冥想）触发不可逆的永久成长事件。每次顿悟涉及：
- 6+ 状态生命周期管理
- 战斗中的概率判定 + 凝神风险抉择
- 章节门限防止过度触发
- 奖励发放后的境界突破联动

需要解决的架构问题：
1. 如何让三种路径共享同一套事件表与状态机？
2. 如何让凝神状态与战斗轮次系统正确交互而不产生死锁？
3. 如何在概率对玩家完全隐藏的前提下实现可调参的触发机制？
4. 如何让境界突破演出与顿悟演出正确串联？

## Decision

### D1. EpiphanyRegistry — 事件表 + 状态机

所有顿悟事件统一注册，生命周期由 8 状态 FSM 管理：

```csharp
public class EpiphanyRegistry
{
    private readonly Dictionary<string, EpiphanyInstance> _instances = new();

    public void LoadFromConfig(EpiphanyConfig[] configs)
    {
        foreach (var cfg in configs)
            _instances[cfg.Id] = new EpiphanyInstance(cfg, EpiphanyState.Locked);
    }

    public void RefreshAvailability()
    {
        foreach (var inst in _instances.Values)
        {
            if (inst.State != EpiphanyState.Locked) continue;
            if (_conditionEvaluator.EvaluateAll(inst.Config.Preconditions))
                inst.TransitionTo(EpiphanyState.Available);
        }
    }

    public EpiphanyInstance? GetHighestPriority(EpiphanyType type)
    {
        return _instances.Values
            .Where(i => i.State == EpiphanyState.Available && i.Config.Type == type)
            .OrderByDescending(i => i.Config.Priority)
            .FirstOrDefault();
    }
}

public enum EpiphanyState
{
    Locked, Available, Triggered, Choosing, Focusing, Completed, Skipped, Failed
}

public enum EpiphanyType { Combat, Narrative, Meditation }

public class EpiphanyInstance
{
    public EpiphanyConfig Config { get; }
    public EpiphanyState State { get; private set; }
    public int SkipCount { get; set; }

    public void TransitionTo(EpiphanyState newState)
    {
        // 验证合法转移
        State = newState;
        Services.EventBus.Publish(new EpiphanyStateChangedEvent(Config.Id, newState));
    }
}
```

### D2. EpiphanyEvaluator — 三路径触发判定

```csharp
public class EpiphanyEvaluator
{
    private const float EpiphanyBaseChance = 0.30f;
    private const float DesperationScaling = 0.40f;
    private const float ChanceCap = 0.70f;
    private const float SkipChanceDecay = 0.15f;
    private const int MeditationEpiphanyCap = 4;
    private bool _triggeredThisBattle;

    // === 战斗路径 ===
    public void OnRoundEnd(RoundEndEvent e)
    {
        if (_capTracker.IsChapterExhausted()) return;
        if (_triggeredThisBattle) return;

        var candidate = _registry.GetHighestPriority(EpiphanyType.Combat);
        if (candidate == null) return;
        if (!EvaluateCombatConditions(candidate, e)) return;

        float chance = CalcFinalChance(candidate, e.ActorHpRatio);
        if (_rng.NextFloat() > chance) return;

        _triggeredThisBattle = true;
        candidate.TransitionTo(EpiphanyState.Triggered);
        candidate.TransitionTo(EpiphanyState.Choosing);
        Services.EventBus.Publish(new EpiphanyChoiceRequestEvent(candidate.Config.Id));
    }

    private float CalcFinalChance(EpiphanyInstance inst, float hpRatio)
    {
        // 跳过衰减：每次跳过后 base_chance 降低
        float decayFactor = Math.Max(1f - inst.SkipCount * SkipChanceDecay, 0f);
        float adjustedBase = EpiphanyBaseChance * decayFactor;
        float desperationBonus = (1f - hpRatio) * DesperationScaling;
        return Math.Min(adjustedBase + desperationBonus, ChanceCap);
    }

    // === 叙事路径 ===
    public void TriggerNarrativeEpiphany(string nodeId)
    {
        var candidate = _registry.FindByNarrativeNode(nodeId);
        if (candidate == null || candidate.State != EpiphanyState.Available) return;
        // 叙事顿悟不受章节配额限制
        candidate.TransitionTo(EpiphanyState.Triggered);
        candidate.TransitionTo(EpiphanyState.Completed);
        _rewardDispatcher.Dispatch(candidate);
    }

    // === 冥想路径 ===
    public void OnMeditationPerformed()
    {
        _meditationDays++;
        if (_meditationEpiphanyCount >= MeditationEpiphanyCap) return;

        var candidate = _registry.GetHighestPriority(EpiphanyType.Meditation);
        if (candidate == null) return;
        if (!EvaluateMeditationConditions(candidate)) return;

        // 冥想满足条件 = 100% 触发
        candidate.TransitionTo(EpiphanyState.Triggered);
        candidate.TransitionTo(EpiphanyState.Completed);
        _rewardDispatcher.Dispatch(candidate);
        _meditationEpiphanyCount++;
    }

    public void OnBattleStart() => _triggeredThisBattle = false;
}
```

### D3. FocusingController — 凝神状态管理

```csharp
public class FocusingController
{
    private const int EpiphanyFocusTurns = 3;
    private const float BuffMultiplier = 1.10f;
    private EpiphanyInstance? _activeEpiphany;
    private int _remainingTurns;

    public void BeginFocusing(EpiphanyInstance epiphany)
    {
        _activeEpiphany = epiphany;
        _remainingTurns = EpiphanyFocusTurns;
        epiphany.TransitionTo(EpiphanyState.Focusing);
        Services.EventBus.Publish(new CharacterStateChangedEvent(
            epiphany.Config.CharacterId, CharacterCombatState.Focusing));
    }

    // 该角色的自然行动轮到来时调用
    public void OnCharacterTurnStart(string characterId)
    {
        if (_activeEpiphany == null) return;
        if (_activeEpiphany.Config.CharacterId != characterId) return;

        _remainingTurns--;
        if (_remainingTurns <= 0) CompleteFocusing();
    }

    public void OnCharacterDeath(string characterId)
    {
        if (_activeEpiphany == null) return;
        if (_activeEpiphany.Config.CharacterId != characterId) return;

        if (!_activeEpiphany.Config.DeathProtection)
        {
            _activeEpiphany.TransitionTo(EpiphanyState.Failed);
            _activeEpiphany = null;
            // 凝神失败不递增 skip_count，不消耗章节配额
        }
        // death_protection=true 时忽略死亡，由教学/脚本保护
    }

    private void CompleteFocusing()
    {
        _activeEpiphany!.TransitionTo(EpiphanyState.Completed);
        _rewardDispatcher.Dispatch(_activeEpiphany);
        ApplyPostEpiphanyBuff(_activeEpiphany.Config.CharacterId);
        RefreshCooldowns(_activeEpiphany.Config.CharacterId);
        _activeEpiphany = null;
    }

    private void ApplyPostEpiphanyBuff(string characterId)
    {
        // 全属性 ×1.10 持续本战剩余
        Services.EventBus.Publish(new ApplyBuffEvent(characterId,
            BuffType.AllAttributes, BuffMultiplier, BuffDuration.UntilBattleEnd));
    }

    private void RefreshCooldowns(string characterId)
    {
        // 刷新该角色普通招式 + 内功主动 + 轻功主动冷却
        Services.Combat.RefreshAllCooldowns(characterId);
    }
}
```

### D4. ChapterCapTracker — 章节门限

```csharp
public class ChapterCapTracker
{
    // 序章=1, 第1章=3, 第2章=4, 第3章=5, 终章=5
    private readonly int[] _chapterCaps = { 1, 3, 4, 5, 5 };
    private int _usedThisChapter;

    public bool IsChapterExhausted()
    {
        int currentChapter = Services.Narrative.GetCurrentChapter();
        int cap = currentChapter < _chapterCaps.Length
            ? _chapterCaps[currentChapter]
            : _chapterCaps[^1];
        return _usedThisChapter >= cap;
    }

    public void ConsumeSlot(int cost = 1) => _usedThisChapter += cost;

    public void OnChapterAdvance() => _usedThisChapter = 0;
}
```

### D5. EpiphanyRewardDispatcher — 奖励发放管道

```csharp
public class EpiphanyRewardDispatcher
{
    public void Dispatch(EpiphanyInstance epiphany)
    {
        var cfg = epiphany.Config;
        var characterId = cfg.CharacterId;

        // 1. 属性增长
        if (cfg.Rewards.Attributes != null)
            Services.CharacterAttributes.ApplyPermanentModifier(characterId, cfg.Rewards.Attributes);

        // 2. 解锁内容
        if (!string.IsNullOrEmpty(cfg.Rewards.UnlockMove))
            Services.MartialArts.UnlockMove(characterId, cfg.Rewards.UnlockMove);
        if (!string.IsNullOrEmpty(cfg.Rewards.NarrativeOption))
            Services.Narrative.UnlockOption(cfg.Rewards.NarrativeOption);

        // 3. 章节配额扣减（叙事顿悟跳过此步）
        if (cfg.Type != EpiphanyType.Narrative)
            _capTracker.ConsumeSlot(cfg.ChapterCapCost);

        // 4. 设置完成 flag
        // Flag 前缀遵循 ADR-0014 Flag Namespace Registry: `epiphany_` 前缀
        Services.Flags.SetFlag($"epiphany_{cfg.Id}_done");

        // 5. 境界突破联动 — 串联演出
        int newTotalPower = Services.CharacterAttributes.GetTotalPower(characterId);
        if (Services.CharacterAttributes.CheckRealmBreakthrough(characterId, newTotalPower))
        {
            Services.Cutscene.PlayCutsceneChain(
                new[] { cfg.CutsceneKey, $"realm_breakthrough_{characterId}" },
                transitionGapMs: 500);
        }
        else
        {
            Services.Cutscene.PlayCutscene(cfg.CutsceneKey);
        }

        Services.EventBus.Publish(new EpiphanyCompletedEvent(cfg.Id, characterId));
        Services.Save.MarkDirty(SaveDomain.Epiphany);
    }
}
```

### D6. 稳妥取胜处理

```csharp
public class SkipHandler
{
    private const int SkipLimit = 3;

    public void OnPlayerChooseSkip(string epiphanyId)
    {
        var inst = _registry.Get(epiphanyId);
        inst.SkipCount++;

        // 发放 skip_reward — 即时战斗增益
        var skipCfg = inst.Config.SkipReward;
        Services.Combat.RestoreHp(inst.Config.CharacterId, skipCfg.HpRestoreRatio);
        Services.Combat.RestoreQi(inst.Config.CharacterId, skipCfg.NeixiRestoreRatio);
        Services.Combat.ClearStagger(inst.Config.CharacterId);
        Services.Combat.GrantNextCrit(inst.Config.CharacterId);
        Services.Combat.ApplyDamageReduction(inst.Config.CharacterId, skipCfg.DamageReduction);

        // 状态转移
        if (inst.SkipCount >= SkipLimit)
            inst.TransitionTo(EpiphanyState.Skipped); // 终态，永不再出现
        else
            inst.TransitionTo(EpiphanyState.Available); // 回退，下次可再触发（概率衰减）
    }
}
```

### D7. 存档契约

```csharp
[Serializable]
public class EpiphanySaveData
{
    public Dictionary<string, EpiphanyState> States { get; set; } = new();
    public Dictionary<string, int> SkipCounts { get; set; } = new();
    public int UsedThisChapter { get; set; }
    public int MeditationDays { get; set; }
    public int MeditationEpiphanyCount { get; set; }
}
```

## Consequences

### Positive
- **三路径统一框架**: 战斗/叙事/冥想共享同一事件表和状态机，新路径只需新增 Evaluator 方法
- **朦胧化原则**: 概率计算完全在服务端，UI 仅展示结果，玩家无法逆推概率
- **与战斗系统松耦合**: FocusingController 通过 EventBus 监听轮次，不侵入战斗核心循环
- **演出串联**: 利用 ADR-0013 CutsceneQueue 的 chain 接口，顿悟+境界突破无缝衔接
- **共享条件引擎**: 复用 ADR-0014 ConditionEvaluator，preconditions 评估逻辑零重复

### Negative
- **单战斗单顿悟**: 一场战斗只触发一次，可能让多条件满足时浪费优先级较低的事件
- **凝神期间的战斗 AI 不感知**: 队友 AI 不会自动保护凝神角色，需要玩家手动操作

### Risks
- **概率参数调平**: base_chance/desperation_scaling/skip_decay 三参数耦合，需要 playtest 迭代
- **凝神死亡体验**: 普通战斗无免死保护，若 epiphany_focus_turns 过长可能让玩家永远选"稳妥"

## Alternatives Considered

| 方案 | 理由 |
|------|------|
| 100% 确定触发（满足条件即出现） | 丧失稀有感和"绝境灵光"的意外体验 |
| 凝神期间免死 | 消除风险-收益抉择的意义，"凝神"变成无脑最优解 |
| 玩家自选奖励 | 违反朦胧化原则，且让顿悟变成"商店购买"体验 |
| 顿悟次数无上限 | 破坏成长节奏，前期过度顿悟导致后期无提升空间 |

## Compliance

| GDD Section | ADR Coverage |
|-------------|-------------|
| 事件表结构 | D1 EpiphanyRegistry + EpiphanyConfig YAML 映射 |
| 三种触发路径 | D2 EpiphanyEvaluator (Combat/Narrative/Meditation) |
| 战斗绝境概率 F-1 | D2.CalcFinalChance (base+desperation+decay+cap) |
| 凝神 vs 稳妥 | D3 FocusingController + D6 SkipHandler |
| 章节门限 | D4 ChapterCapTracker |
| 奖励发放 + 境界联动 | D5 EpiphanyRewardDispatcher (串联演出) |
| 悟后 buff F-2 | D3.ApplyPostEpiphanyBuff (×1.10 本战) |
| 跳过衰减 W14 | D2.CalcFinalChance (skip_chance_decay=0.15) |
| 冥想约束 W15 | D2.OnMeditationPerformed (MeditationEpiphanyCap=4) |
| 凝神失败 E-1 | D3.OnCharacterDeath (death_protection 开关) |
| 多事件优先级 E-2 | D1.GetHighestPriority |
| 叙事不受配额 E-3 | D5 (Type != Narrative 时才扣配额) |
| 冥想跨章累计 E-4 | D7 EpiphanySaveData (MeditationDays 全局) |
| 跳过 skip_limit E-6 | D6 (SkipCount >= SkipLimit → Skipped 终态) |
| 境界阈值串联 E-7 | D5 PlayCutsceneChain (transitionGapMs=500) |
| 存档完整性 AC-9 | D7 EpiphanySaveData |
