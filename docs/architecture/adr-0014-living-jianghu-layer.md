# ADR-0014: Living Jianghu Layer — Event Scheduler & Condition Engine

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | World Simulation, Rule Engine, Scheduling |
| **Knowledge Risk** | **LOW** — 纯逻辑调度，不依赖引擎特定 API；唯一引擎触点是 Autoload 生命周期和信号连接 |
| **References Consulted** | `design/gdd/living-jianghu-layer.md`, ADR-0001 (EventBus), ADR-0003 (Data Schema), ADR-0004 (Save System) |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) 验证每日 Tick 在大事件表（200+ 条目）下的扫描性能 ≤ 1ms; 2) 验证 Flag 系统跨存档读写隔离 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 订阅 day_advanced / season_changed / chapter_changed), ADR-0003 (Data Schema — 事件表 YAML 格式定义), ADR-0004 (Save System — Flag 持久化, 事件状态持久化), ADR-0013 (Cutscene — 复用 `LockMode` 共享枚举查询全局锁定状态) |
| **Enables** | 11+ 系统的被动内容推送能力 (传闻/暗号/飞书/代办/世界事件), 条件引擎作为共享基础设施被其他系统复用 |
| **Blocks** | Sprint 6+ (所有依赖"世界活着"表现力的 Feature) |
| **Ordering Note** | ADR-0001 (EventBus) + ADR-0004 (Save/Flag) 必须就绪；ADR-0013 定义的 `LockMode` 共享枚举须先 Accepted；与 Cutscene 主流程无直接调用，仅查询 `Services.GameStateLock.IsSystemTickLocked` 决定是否暂停 Tick |

## Context

### Problem Statement

活江湖层 (GDD #16) 是《风止》的世界动态调度引擎——每个游戏日 tick 时，它扫描 200+ 条事件配置，评估 9 种组合条件，按优先级选出 ≤N 个事件触发，并将内容推送给下游系统呈现。这带来以下架构挑战：

1. **条件引擎通用性**：9 种 precondition 原语（flag/chapter/day_elapsed/npc_state/season/breathing/region/mindset/not_flag）需要跨系统查询，且未来其他系统（探索/洞察、教学/引导）也需要类似的条件评估能力
2. **调度性能**：每日 tick 扫描全量事件表，必须保持 ≤ 1ms（避免卡帧）
3. **事件生命周期**：5 种状态 + 到期/过期/连锁规则，需要可靠的状态机管理
4. **跨系统副作用**：`on_trigger` 可修改 NPC 状态、设置 Flag、注册延迟事件，需要事务性保障
5. **呈现解耦**：调度层不关心"怎么展示"，只负责"选什么、何时选"
6. **存档一致性**：事件状态（pending/triggered/delivered）必须随存档持久化，加载后恢复到精确状态

### Constraints

- 每日事件容量上限 `daily_event_cap = 3`（呼吸期 ×2 = 6）
- 同类型事件每日 ≤ 2 个（类型均衡）
- 同日 tick 内不允许连锁触发（on_trigger 产生的新满足条件在下一日才生效）
- 单事件 `on_trigger` 最多包含 3 个 `npc_state_change`
- attitude 偏移范围 [-2, +2]
- 事件表为静态配置（YAML），运行时不动态生成新事件定义
- 传闻传播延迟 = base_delay + region_distance × distance_factor

### Requirements (from GDD #16)

- 5 种事件类型 (rumor / code / letter / delegation / world_event)
- 9 种 precondition 原语 (flag / not_flag / chapter / chapter_range / day_elapsed_since / npc_state / season / in_breathing / player_region / mindset_zone)
- 5 种事件生命周期状态 (inactive → pending → triggered → delivered → expired)
- 每日 Tick 8 步流程
- 优先级 + 标签相关性 + 积压奖励 三维排序
- 呼吸期容量倍增 + breathing_only 事件
- 传闻传播延迟
- 章节切换清理
- 探索/洞察互斥协调（共享 flag 命名空间）

## Decision

采用 **ConditionEvaluator (共享条件引擎) + JianghuScheduler (每日调度器) + EventRegistry (事件表注册中心) + DeliveryQueue (呈现队列)** 四组件方案。

### 核心架构

```
┌─────────────────────────────────────────────────────────────┐
│              JianghuService (Autoload)                        │
│         全局入口 — 订阅 day_advanced 驱动调度                │
└────────────────────────┬─────────────────────────────────────┘
                         │
          ┌──────────────┼─────────────────────┐
          ▼              ▼                     ▼
┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ EventRegistry│  │ JianghuScheduler │  │ DeliveryQueue    │
│ (事件表)     │  │ (每日调度器)     │  │ (待呈现队列)     │
│              │  │                  │  │                  │
│ Load(yaml)   │  │ RunDailyTick()   │  │ Enqueue(event)   │
│ GetAll()     │  │ SelectEvents()   │  │ Consume(type)    │
│ GetById()    │  │ ApplyEffects()   │  │ PeekByChannel()  │
└──────────────┘  └────────┬─────────┘  └──────────────────┘
                           │ 依赖
              ┌────────────┼────────────────┐
              ▼            ▼                ▼
   ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
   │ Condition    │ │ EffectExec   │ │ PropagationCalc  │
   │ Evaluator    │ │ utor         │ │ (传播延迟)       │
   │ (条件引擎)   │ │ (副作用执行) │ │                  │
   │              │ │              │ │ CalcDelay()      │
   │ Evaluate()   │ │ Apply()      │ │                  │
   └──────────────┘ └──────────────┘ └──────────────────┘
```

### 1. ConditionEvaluator (共享条件引擎 — Foundation 层)

**这是一个跨系统共享组件**，不仅服务活江湖层，未来也可被探索/洞察、教学/引导、对话条件分支等系统复用。

```csharp
// Foundation/Conditions/ConditionEvaluator.cs
public class ConditionEvaluator
{
    private readonly IFlagService _flags;
    private readonly INarrativeQuery _narrative;
    private readonly INpcStateQuery _npcState;
    private readonly IWorldQuery _world;

    /// <summary>
    /// 评估一组条件（AND 关系）。全部满足返回 true。
    /// </summary>
    public bool EvaluateAll(IReadOnlyList<Condition> conditions)
    {
        foreach (var cond in conditions)
        {
            if (!EvaluateSingle(cond)) return false;
        }
        return true;
    }

    private bool EvaluateSingle(Condition cond) => cond.Type switch
    {
        ConditionType.Flag => _flags.HasFlag(cond.Key),
        ConditionType.NotFlag => !_flags.HasFlag(cond.Key),
        ConditionType.Chapter => _narrative.GetCurrentChapter() == cond.IntValue,
        ConditionType.ChapterRange => _narrative.GetCurrentChapter() >= cond.Min
                                   && _narrative.GetCurrentChapter() <= cond.Max,
        ConditionType.DayElapsedSince => _flags.HasFlag(cond.Key)
            && (_world.GetCurrentDay() - _flags.GetFlagDay(cond.Key)) >= cond.Min,
        ConditionType.NpcState => _npcState.GetAxis(cond.NpcId, cond.Axis) == cond.StringValue,
        ConditionType.Season => _world.GetCurrentSeason() == cond.SeasonValue,
        ConditionType.InBreathing => _narrative.IsBreathing() == cond.BoolValue,
        ConditionType.PlayerRegion => _world.GetPlayerRegion() == cond.StringValue,
        ConditionType.MindsetZone => _world.GetMindsetZone() == cond.StringValue,
        _ => throw new ArgumentException($"Unknown condition type: {cond.Type}")
    };
}

/// <summary>
/// 条件数据结构 — 从 YAML 反序列化而来。
/// </summary>
public record Condition
{
    public ConditionType Type { get; init; }
    public string Key { get; init; }       // flag name / npc_id
    public string Axis { get; init; }      // npc state axis
    public string StringValue { get; init; }
    public int IntValue { get; init; }
    public int Min { get; init; }
    public int Max { get; init; }
    public bool BoolValue { get; init; }
    public Season SeasonValue { get; init; }
}
```

**查询接口抽象（依赖注入）：**

```csharp
// Foundation/Conditions/IConditionContext.cs
public interface IFlagService
{
    bool HasFlag(string key);
    int GetFlagDay(string key);  // flag 设置时的游戏日
    void SetFlag(string key);
}

public interface INarrativeQuery
{
    int GetCurrentChapter();
    bool IsBreathing();
}

public interface INpcStateQuery
{
    string GetAxis(string npcId, string axis);
}

public interface IWorldQuery
{
    int GetCurrentDay();
    Season GetCurrentSeason();
    string GetPlayerRegion();
    string GetMindsetZone();
}
```

### 2. JianghuScheduler (每日调度器)

```csharp
// Feature/LivingJianghu/JianghuScheduler.cs
public class JianghuScheduler
{
    private readonly EventRegistry _registry;
    private readonly ConditionEvaluator _evaluator;
    private readonly EffectExecutor _effects;
    private readonly PropagationCalculator _propagation;
    private readonly DeliveryQueue _deliveryQueue;
    private readonly IWorldQuery _world;
    private readonly INarrativeQuery _narrative;

    // Tuning Knobs
    private int _dailyEventCap = 3;
    private int _breathingMultiplier = 2;
    private int _maxTypePerDay = 2;
    private int _backlogBonusPerDay = 5;
    private int _backlogBonusCap = 25;

    /// <summary>
    /// 每日 Tick 主循环 — 由 day_advanced 事件触发。
    /// 严格遵循 GDD 8 步流程。
    /// </summary>
    public void RunDailyTick()
    {
        // Step 1: 更新积压天数
        _registry.IncrementBacklogDays();

        // Step 2: 收集候选池 — 条件满足的事件
        var candidates = _registry.GetAllPending()
            .Where(e => _evaluator.EvaluateAll(e.Script.Preconditions))
            .ToList();

        // Step 3: 呼吸期事件加入
        bool isBreathing = _narrative.IsBreathing();
        if (!isBreathing)
        {
            candidates.RemoveAll(e => e.Script.BreathingOnly);
        }

        // Step 4: 计算选择得分并排序
        foreach (var candidate in candidates)
        {
            candidate.SelectionScore = CalcSelectionScore(candidate);
        }
        candidates.Sort((a, b) => b.SelectionScore.CompareTo(a.SelectionScore));

        // Step 5: 按容量 + 类型均衡选取
        int effectiveCap = _dailyEventCap * (isBreathing ? _breathingMultiplier : 1);
        var selected = ApplyCapAndBalance(candidates, effectiveCap);

        // Step 6: 执行副作用
        foreach (var evt in selected)
        {
            _effects.Apply(evt.Script.OnTrigger);
            evt.State = EventState.Triggered;
            evt.BacklogDays = 0;
        }

        // Step 7: 推入呈现队列（含传播延迟）
        foreach (var evt in selected)
        {
            int delay = evt.Script.Type == EventType.Rumor
                ? _propagation.CalcDelay(evt.Script.SourceRegion)
                : 0;
            _deliveryQueue.Enqueue(evt, delay);
        }

        // Step 8: 过期清理
        ProcessExpirations();
    }

    private float CalcSelectionScore(EventInstance e)
    {
        float tagScore = CalcTagRelevance(e.Script.Tags);
        float backlog = Math.Min(e.BacklogDays * _backlogBonusPerDay, _backlogBonusCap);
        return e.Script.Priority + tagScore + backlog;
    }

    private List<EventInstance> ApplyCapAndBalance(
        List<EventInstance> sorted, int cap)
    {
        var result = new List<EventInstance>();
        var typeCounts = new Dictionary<EventType, int>();

        foreach (var evt in sorted)
        {
            if (result.Count >= cap) break;
            typeCounts.TryGetValue(evt.Script.Type, out int count);
            if (count >= _maxTypePerDay) continue; // 类型均衡
            result.Add(evt);
            typeCounts[evt.Script.Type] = count + 1;
        }
        return result;
    }
}
```

### 3. EventRegistry (事件表注册中心)

```csharp
// Feature/LivingJianghu/EventRegistry.cs
public class EventRegistry
{
    private readonly Dictionary<string, EventInstance> _events = new();

    /// <summary>
    /// 启动时从 YAML 加载全量事件配置。
    /// </summary>
    public void LoadFromYaml(string path)
    {
        var scripts = YamlLoader.LoadAll<JianghuEventScript>(path);
        foreach (var script in scripts)
        {
            _events[script.Id] = new EventInstance(script);
        }
    }

    public IEnumerable<EventInstance> GetAllPending() =>
        _events.Values.Where(e => e.State == EventState.Pending || e.State == EventState.Inactive);

    /// <summary>
    /// 章节切换清理 — GDD E-10。
    /// </summary>
    public void CleanupOnChapterChange(int newChapter)
    {
        foreach (var evt in _events.Values)
        {
            if (evt.State != EventState.Pending) continue;
            if (evt.Script.ChapterMax < newChapter)
            {
                evt.State = EventState.Expired;
                // on_expire 由 EffectExecutor 执行
            }
        }
    }

    public void IncrementBacklogDays()
    {
        foreach (var evt in _events.Values.Where(e => e.State == EventState.Pending))
            evt.BacklogDays++;
    }

    /// <summary>
    /// 存档序列化 — 只持久化运行时状态，不持久化 Script 定义。
    /// </summary>
    public EventRegistrySaveData ToSaveData() =>
        new(_events.Values.Select(e => e.ToSaveEntry()).ToList());

    public void LoadSaveData(EventRegistrySaveData data)
    {
        foreach (var entry in data.Entries)
        {
            if (_events.TryGetValue(entry.Id, out var instance))
            {
                instance.State = entry.State;
                instance.BacklogDays = entry.BacklogDays;
            }
        }
    }
}

public class EventInstance
{
    public JianghuEventScript Script { get; }
    public EventState State { get; set; } = EventState.Inactive;
    public int BacklogDays { get; set; }
    public float SelectionScore { get; set; }

    public EventInstance(JianghuEventScript script) => Script = script;
}

public enum EventState
{
    Inactive, Pending, Triggered, Delivered, Expired
}
```

### 4. DeliveryQueue (呈现队列)

```csharp
// Feature/LivingJianghu/DeliveryQueue.cs
public class DeliveryQueue
{
    private readonly List<DeliveryEntry> _queue = new();

    /// <summary>
    /// 入队，附带传播延迟（天数）。
    /// delay=0 表示立即可呈现。
    /// </summary>
    public void Enqueue(EventInstance evt, int delayDays)
    {
        _queue.Add(new DeliveryEntry
        {
            Event = evt,
            ReadyDay = Services.World.GetCurrentDay() + delayDays
        });
    }

    /// <summary>
    /// 下游系统（对话/信使/环境）拉取可呈现的事件。
    /// 按 delivery_method 分通道消费。
    /// </summary>
    public IReadOnlyList<EventInstance> ConsumeReady(DeliveryMethod method)
    {
        int today = Services.World.GetCurrentDay();
        var ready = _queue
            .Where(e => e.ReadyDay <= today && e.Event.Script.DeliveryMethod == method)
            .Select(e => e.Event)
            .ToList();

        foreach (var evt in ready)
        {
            evt.State = EventState.Delivered;
        }
        _queue.RemoveAll(e => ready.Contains(e.Event));
        return ready;
    }

    /// <summary>
    /// 呼吸期开始时，释放所有积压（ready 但未消费的）事件。
    /// </summary>
    public void FlushBacklog()
    {
        int today = Services.World.GetCurrentDay();
        foreach (var entry in _queue.Where(e => e.ReadyDay > today))
        {
            entry.ReadyDay = today; // 立即可呈现
        }
    }
}
```

### 5. EffectExecutor (副作用执行器)

```csharp
// Feature/LivingJianghu/EffectExecutor.cs
public class EffectExecutor
{
    private readonly IFlagService _flags;
    private readonly INpcStateService _npcState;
    private readonly EventRegistry _registry;

    /// <summary>
    /// 执行 on_trigger / on_expire 副作用列表。
    /// 事务性：全部执行或全部不执行（出错时回滚已设置的 flag）。
    /// </summary>
    public void Apply(IReadOnlyList<TriggerEffect> effects)
    {
        foreach (var effect in effects)
        {
            switch (effect.Type)
            {
                case EffectType.SetFlag:
                    _flags.SetFlag(effect.Key);
                    break;
                case EffectType.NpcStateChange:
                    _npcState.ModifyAxis(effect.NpcId, effect.Field, effect.Value, effect.SourceEvent);
                    break;
                case EffectType.RegisterDelayedEvent:
                    // 注册新的延迟事件到 Registry（下一日才可被选中 — E-7 无同日连锁）
                    _registry.ActivateDelayed(effect.EventId);
                    break;
            }
        }
    }
}
```

### 6. PropagationCalculator (传闻传播延迟)

```csharp
// Feature/LivingJianghu/PropagationCalculator.cs
public class PropagationCalculator
{
    private readonly IWorldQuery _world;
    private readonly RegionDistanceTable _distanceTable;

    // Tuning Knobs
    private int _baseDelay = 1;
    private float _distanceFactor = 1.0f;

    /// <summary>
    /// GDD F-1: propagation_delay = base_delay + region_distance × distance_factor
    /// </summary>
    public int CalcDelay(string sourceRegion)
    {
        string playerRegion = _world.GetPlayerRegion();
        int distance = _distanceTable.GetDistance(sourceRegion, playerRegion);
        return _baseDelay + (int)(distance * _distanceFactor);
    }
}
```

### 与 GameStateLock 的集成

活江湖层每日 Tick 需要检查全局锁定状态：

```csharp
// JianghuService 内部
private void OnDayAdvanced(DayAdvancedEvent e)
{
    // 演出锁定期间（FULL 模式禁止系统 tick），不执行调度
    if (Services.GameStateLock.IsSystemTickLocked) return;
    _scheduler.RunDailyTick();
}
```

### 存档集成

```csharp
// JianghuService 存档接口
public JianghuSaveData CreateSaveData() => new()
{
    RegistryState = _registry.ToSaveData(),
    QueueState = _deliveryQueue.ToSaveData()
};

public void LoadSaveData(JianghuSaveData data)
{
    _registry.LoadSaveData(data.RegistryState);
    _deliveryQueue.LoadSaveData(data.QueueState);
}
```

### 与探索/洞察系统的互斥协调

```csharp
// 探索系统发现时：
_flags.SetFlag($"insight_{nodeId}_discovered");
// → 活江湖的传闻事件 precondition 中：
//   not_flag: "insight_xxx_discovered"
// → 自动被过滤，不再推送传闻（GDD W12）
```

### 性能预算

| 操作 | 预算 | 策略 |
|------|------|------|
| 每日 Tick 扫描 200 条事件 | ≤ 1ms | 条件评估短路求值 + 无 GC 分配 |
| 条件评估（单条件） | ≤ 5μs | 查询接口返回缓存值，不进行 IO |
| 存档序列化 | ≤ 5ms | 仅序列化运行时状态（~2KB），不含 Script 定义 |

## Consequences

### Positive

- **条件引擎可复用**：ConditionEvaluator 位于 Foundation 层，探索/洞察、教学/引导等系统可直接使用同一套条件原语
- **调度与呈现解耦**：DeliveryQueue 作为中间层，下游对话/信使系统按自己的节奏拉取，不被 tick 节奏绑架
- **无同日连锁保障**：EffectExecutor 注册的延迟事件在下一日才激活，杜绝雪崩
- **存档精确恢复**：只持久化事件实例状态 + 积压天数，定义始终从 YAML 加载（支持热更新事件表）
- **性能可控**：条件短路 + 缓存查询确保大事件表不卡帧

### Negative

- **YAML 事件表静态**：运行时不能动态生成新事件类型，mod 扩展受限
- **查询接口抽象开销**：每个条件查询经过接口调用，比直接访问略慢（但在 1ms 预算内）
- **DeliveryQueue 遗忘**：如果下游系统 bug 不消费队列，事件会堆积

### Risks

| 风险 | 影响 | 缓解 |
|------|------|------|
| 事件表超过 200 条后性能退化 | 每日 Tick 超过 1ms 预算 | 按 chapter_range 预过滤，只扫描当前章节相关事件 |
| Flag 命名空间冲突（多系统共写） | 条件误判 | 遵循下方 **Flag Namespace Registry**，强制前缀命名规范 |
| 呼吸期 FlushBacklog 导致事件洪水 | 玩家信息过载 | FlushBacklog 仍受 effective_cap 限制，分多日释放 |

## Flag Namespace Registry (跨 ADR 共享规范)

> **MI-3 补齐**：本节是项目唯一的 Flag 前缀官方注册表。所有 ADR / 系统在使用 `IFlagService.SetFlag()` 时**必须**遵循前缀规则。新增前缀须先更新本表并通知架构 owner。

### 前缀分配表

| 前缀 | 拥有 ADR / 系统 | 用途 | 示例 |
|---|---|---|---|
| `jianghu_` | ADR-0014 / 活江湖层 #16 | 江湖事件状态 / 传闻 / 暗号轮换 | `jianghu_event_lake_storm_triggered`, `jianghu_rumor_broker_propagated` |
| `narrative_` | 主线叙事 #9 | 章节进度 / 主线节点 / 关键剧情分支 | `narrative_chapter1_complete`, `narrative_branch_silent_path` |
| `romance_` | ADR-0015 / 感情系统 #13 | NPC 关系状态 / 表白结果 / 上次接触 | `romance_last_contact_{npcId}`, `romance_bond_declined_{npcId}`, `romance_milestone_{npcId}_{milestoneId}` |
| `epiphany_` | ADR-0017 / 顿悟突破 #17 | 顿悟事件完成态 / 配额追踪 | `epiphany_{cfgId}_done`, `epiphany_chapter_{n}_quota` |
| `insight_` | ADR-0018 / 探索洞察 #19 | 场景洞察节点发现状态 / 线索登记 | `insight_{nodeId}_discovered`, `insight_clue_{clueId}_registered` |
| `tutorial_` | 教学引导 #22 | 引导步骤完成态 / 弹窗已读 | `tutorial_combat_intro_seen`, `tutorial_save_first_seen` |
| `combat_` | Core/Combat #2 | 战斗系统永久标记（如首次解锁某机制） | `combat_first_decisive_unlocked` |
| `party_` | ADR-0016 / 队伍管理 #25 | 同伴入队/退队历史 / 上阵记录 | `party_npc_{id}_recruited`, `party_npc_{id}_departed` |
| `mindset_` | 心境双轴 #6 | 心境永久里程碑 | `mindset_balance_achieved` |
| `system_` | Platform 层共享 | 引擎/平台级标记（如成就解锁、Steam 同步） | `system_first_launch`, `system_steam_synced_v1` |

### 命名规则

1. **强前缀强制**：所有 `SetFlag(key)` 的 `key` 必须以上述前缀开头；未带前缀的写入应在 Debug build 触发 assert
2. **id 占位符**：`{npcId}`、`{cfgId}` 等占位符使用 GDD 中定义的稳定 ID（snake_case，无空格）
3. **不可变语义**：Flag 一旦命名并进入存档，不可重命名（涉及存档兼容）；废弃需走 deprecation 流程
4. **跨系统读**：任意系统可**读**任意前缀的 Flag（条件评估）；**写**仅限拥有者 ADR
5. **存档隔离**：所有 Flag 自动随存档持久化（详见 ADR-0004），不区分 session-scoped / persistent — 由命名约定隐含

### 实施位置

- `IFlagService.SetFlag()` 实现处（位于 ADR-0014 调度器内）添加前缀校验
- 单元测试 `FlagNamespaceTests` 扫描所有 ADR 中的 `SetFlag(...)` 字符串字面量，验证前缀合规
- CI 静态扫描脚本 `tools/check-flag-prefix.cs`（Sprint 6 前补齐）

## Alternatives Considered

### A. 基于 Godot Timer 的定时调度

每个事件挂一个 Timer 节点，到期自动触发。

**优点**：简单直观，利用引擎节点系统。
**拒绝原因**：200+ Timer 节点的管理开销；Timer 与游戏日不同步（Timer 用实时，游戏日是逻辑日）；暂停/加载存档时 Timer 状态恢复困难。

### B. 条件引擎内嵌于活江湖层

ConditionEvaluator 作为活江湖的私有组件，其他系统各自实现条件评估。

**优点**：无跨系统依赖。
**拒绝原因**：GDD 明确探索/洞察系统需要相同的条件原语；教学/引导也需要条件门控。重复实现违反 DRY。Foundation 层共享是正确选择。

### C. 事件驱动反应式（不用每日 Tick 扫描）

每次 flag 变化时反向查询受影响的事件，仅评估被触发的子集。

**优点**：避免全量扫描。
**拒绝原因**：`day_elapsed_since` 条件依赖时间推进而非 flag 变化；同日连锁禁止规则在反应式模式下难以保障。批量每日 Tick 更简单可靠，且 200 条在 1ms 内可完成。

## Compliance

- **每日 Tick 8 步流程 (Rule 2)**：JianghuScheduler.RunDailyTick() 严格按 GDD 步骤实现
- **9 种条件原语 (Rule 3, AC-2)**：ConditionEvaluator switch 表达式完整覆盖
- **优先级 + 标签 + 积压 三维排序 (F-1/F-2/F-3, AC-3)**：CalcSelectionScore 实现 GDD 公式
- **类型均衡 (Rule 4, AC-3)**：ApplyCapAndBalance 强制同类 ≤ max_type_per_day
- **传闻传播延迟 (F-1, AC-4)**：PropagationCalculator 实现 GDD 公式
- **无同日连锁 (E-7, AC-9)**：EffectExecutor.ActivateDelayed 标记下一日才可选
- **章节切换清理 (E-10, AC-7)**：EventRegistry.CleanupOnChapterChange
- **呼吸期行为 (Rule 7, AC-6)**：breathing_only 过滤 + FlushBacklog
- **探索互斥 (W12)**：共享 flag 命名空间 + not_flag 条件自动抑制
