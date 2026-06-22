# ADR-0015: Romance System — Comet Model & Milestone-Floor Architecture

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Relationship State, Rule Evaluation, Narrative Branching |
| **Knowledge Risk** | **LOW** — 纯逻辑系统，不依赖引擎特定 API；数据寄存于 NPC State 系统 |
| **References Consulted** | `design/gdd/romance-system.md`, ADR-0001 (EventBus), ADR-0003 (Data Schema), ADR-0008 (FSM/NPC State) |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) 验证 NPC State 系统可在对话进行中排队态度变更; 2) 验证 force_break 在任何里程碑组合下的覆写正确性 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 态度变化/里程碑事件发布), ADR-0008 (FSM/NPC State — 态度档位存储/生命状态查询), ADR-0005 (Dialogue — 对话选择触发态度变化), ADR-0014 (Living Jianghu — 传闻/书信投递) |
| **Enables** | 结局变体选取 (16 种), 误会系统情感地板查询, 朦胧化 UI 关系暗示, 成就系统彗星统计 |
| **Blocks** | Sprint 5+ (结缘/诀别演出, 终幕结局分支) |
| **Ordering Note** | ADR-0008 (NPC State) 必须就绪以提供态度读写; ADR-0014 (活江湖层) 就绪后对接传闻/书信投递通道 |

## Context

### Problem Statement

感情系统 (GDD #13) 采用"彗星模型"管理主角与 4 位女主的关系——女主拥有独立旅程，与主角的关系通过阶段性里程碑推进。这带来以下架构挑战：

1. **双层状态协作**：态度档位（即时波动）与关系里程碑（不可逆 flag）必须互锁——里程碑设定态度下限（地板），态度达标才能解锁新里程碑（门槛）
2. **互斥约束**：结缘（M_BOND）全局互斥，同一周目最多绑定一人，需要跨 NPC 的原子性检查
3. **诀别覆写**：`force_break` 必须无条件覆写所有现有状态，是系统内唯一的"超级权限"操作
4. **彗星存在感**：4 种被动联系机制（暗号/书信/传闻/偶遇）需要与活江湖层、对话系统协调，但不直接拥有呈现能力
5. **结局交互**：终幕结局需要组合查询心境兼容性 + 结缘状态 + 善恶值，产出 16 种变体
6. **无数值暴露**：系统对玩家完全隐藏数值，只通过对话/演出/图鉴间接传达

### Constraints

- 态度档位存储于 NPC State 系统（8 级枚举：-4 到 +3）
- 里程碑为 bool flags，不可逆（除 M_BREAK 覆写）
- 结缘互斥：`bonded_heroine` 全局唯一
- 里程碑不可跳级（M_ACQUAINTED → M_TRUST → M_CRISIS → M_HEART → M_BOND）
- 地板钳位静默吞掉多余负面 delta（不报错、不通知玩家）
- 传闻触发概率上限 0.8（永远有 20% 不触发）
- 感情数据不跨周目继承

### Requirements (from GDD #13)

- 6 种里程碑 (M_ACQUAINTED → M_TRUST → M_CRISIS → M_HEART → M_BOND / M_BREAK)
- 态度地板映射表（里程碑 → 最低态度值）
- 结缘前置条件检查 (4 项)
- 结局变体选取公式 (5 zone × 3 variant + 魔道 = 16)
- 彗星存在感 4 机制 (暗号/书信/传闻/偶遇)
- force_break 诀别覆写
- 心境兼容性查询

## Decision

采用 **RomanceService (规则引擎) + MilestoneRegistry (里程碑管理) + CometPresenceTracker (存在感追踪) + EndingResolver (结局变体)** 四组件方案。数据寄存于 NPC State 系统，感情系统只负责规则逻辑。

### 核心架构

```
┌─────────────────────────────────────────────────────────────┐
│              RomanceService (Autoload)                        │
│      全局入口 — 态度变化拦截 / 里程碑解锁 / 结缘流程        │
└────────────────────────┬─────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────────────┐
          ▼              ▼                      ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ MilestoneRegistry│  │ CometPresence    │  │ EndingResolver   │
│ (里程碑管理)     │  │ Tracker          │  │ (结局变体)       │
│                  │  │ (存在感追踪)     │  │                  │
│ GetFloor()       │  │ OnSignDiscovered │  │ Resolve()        │
│ CanUnlock()      │  │ OnLetterReceived │  │ IsCompatible()   │
│ Unlock()         │  │ CalcRumorChance  │  │                  │
│ ForceBreak()     │  │                  │  │                  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ NPC State System     │
              │ (数据存储 — 态度 +   │
              │  里程碑 flags)       │
              └──────────────────────┘
```

### 1. RomanceService (规则引擎 — Autoload)

```csharp
// Feature/Romance/RomanceService.cs
public partial class RomanceService : Node
{
    private MilestoneRegistry _milestones;
    private CometPresenceTracker _comet;
    private EndingResolver _endings;
    private BondedHeroine _bondedHeroine = BondedHeroine.None;

    public BondedHeroine BondedHeroine => _bondedHeroine;

    public override void _Ready()
    {
        // 拦截所有态度变化，注入地板钳位逻辑
        Services.EventBus.Subscribe<AttitudeChangeRequestEvent>(OnAttitudeChangeRequest);
    }

    /// <summary>
    /// 态度变化拦截器 — 应用地板钳位规则。
    /// NPC State 系统在修改态度前先通过此接口校验。
    /// </summary>
    private void OnAttitudeChangeRequest(AttitudeChangeRequestEvent e)
    {
        if (e.Source.IsForceBreak)
        {
            // 诀别覆写：无视地板
            _milestones.ForceBreak(e.NpcId);
            Services.NpcState.SetAttitude(e.NpcId, AttitudeLevel.SwordDrawn); // -4
            Services.EventBus.Publish(new RomanceBreakEvent(e.NpcId));
            return;
        }

        int floor = _milestones.GetFloor(e.NpcId);
        int current = (int)Services.NpcState.GetAttitude(e.NpcId);
        int proposed = current + e.Delta;
        int clamped = Math.Max(proposed, floor);

        Services.NpcState.SetAttitude(e.NpcId, (AttitudeLevel)clamped);

        if (clamped != proposed)
        {
            // 被地板吞掉的 delta，记录供调试
            GD.Print($"[Romance] Floor clamp: {e.NpcId} delta={e.Delta}, proposed={proposed}, clamped={clamped}");
        }
    }

    /// <summary>
    /// 里程碑解锁入口 — 由叙事节点调用。
    /// </summary>
    public bool TryUnlockMilestone(string npcId, MilestoneType milestone)
    {
        if (!_milestones.CanUnlock(npcId, milestone)) return false;
        _milestones.Unlock(npcId, milestone);
        Services.EventBus.Publish(new MilestoneUnlockedEvent(npcId, milestone));
        return true;
    }

    /// <summary>
    /// 结缘流程入口 — GDD C3 逻辑。
    /// </summary>
    public BondResult TryBond(string npcId)
    {
        if (_bondedHeroine != BondedHeroine.None)
            return BondResult.AlreadyBonded;
        if (!_milestones.CanUnlock(npcId, MilestoneType.Bond))
            return BondResult.ConditionsNotMet;

        // 返回 Pending — 等待玩家选择确认后调用 ConfirmBond
        return BondResult.AwaitingChoice;
    }

    public void ConfirmBond(string npcId)
    {
        _milestones.Unlock(npcId, MilestoneType.Bond);
        _bondedHeroine = HeroineFromId(npcId);
        Services.EventBus.Publish(new BondConfirmedEvent(npcId));
    }

    public void DeclineBond(string npcId)
    {
        // 标记该节点已使用，不再重复触发
        // Flag 前缀遵循 ADR-0014 Flag Namespace Registry: `romance_` 前缀
        Services.Flags.SetFlag($"romance_bond_declined_{npcId}");
        Services.EventBus.Publish(new BondDeclinedEvent(npcId));
    }
}

public enum BondedHeroine { None, A, B, C, D }
public enum BondResult { AwaitingChoice, AlreadyBonded, ConditionsNotMet }
```

### 2. MilestoneRegistry (里程碑管理)

```csharp
// Feature/Romance/MilestoneRegistry.cs
public class MilestoneRegistry
{
    private readonly INpcStateService _npcState;

    /// <summary>
    /// 态度地板计算 — GDD C1。
    /// 从最高里程碑向下查询，返回对应地板值。
    /// </summary>
    public int GetFloor(string npcId)
    {
        var data = GetRomanceData(npcId);
        if (data.Broken) return -4;      // M_BREAK 覆写
        if (data.Bonded) return 3;       // M_BOND
        if (data.HeartOpened) return 2;  // M_HEART
        if (data.CrisisShared) return 2; // M_CRISIS
        if (data.TrustBuilt) return 1;   // M_TRUST
        if (data.Acquainted) return 0;   // M_ACQUAINTED
        return -4; // 无里程碑，无限制
    }

    /// <summary>
    /// 里程碑解锁前置检查 — GDD C2。
    /// 包含序列约束（不可跳级）+ 态度门槛。
    /// </summary>
    public bool CanUnlock(string npcId, MilestoneType milestone)
    {
        var data = GetRomanceData(npcId);
        int attitude = (int)_npcState.GetAttitude(npcId);

        return milestone switch
        {
            MilestoneType.Acquainted => true,
            MilestoneType.Trust => data.Acquainted && attitude >= 1,
            MilestoneType.Crisis => data.TrustBuilt && attitude >= 1,
            MilestoneType.Heart => data.CrisisShared && attitude >= 2,
            MilestoneType.Bond => data.HeartOpened && attitude >= 2,
            MilestoneType.Break => true, // force_break 不检查前置
            _ => false
        };
    }

    public void Unlock(string npcId, MilestoneType milestone)
    {
        var data = GetRomanceData(npcId);
        switch (milestone)
        {
            case MilestoneType.Acquainted: data.Acquainted = true; break;
            case MilestoneType.Trust: data.TrustBuilt = true; break;
            case MilestoneType.Crisis: data.CrisisShared = true; break;
            case MilestoneType.Heart: data.HeartOpened = true; break;
            case MilestoneType.Bond: data.Bonded = true; break;
            case MilestoneType.Break: data.Broken = true; break;
        }
        _npcState.SetRomanceData(npcId, data);
    }

    public void ForceBreak(string npcId)
    {
        var data = GetRomanceData(npcId);
        data.Broken = true;
        _npcState.SetRomanceData(npcId, data);
    }

    private RomanceData GetRomanceData(string npcId) =>
        _npcState.GetRomanceData(npcId);
}

public enum MilestoneType
{
    Acquainted, Trust, Crisis, Heart, Bond, Break
}
```

### 3. CometPresenceTracker (彗星存在感追踪)

```csharp
// Feature/Romance/CometPresenceTracker.cs
public class CometPresenceTracker
{
    private readonly IFlagService _flags;
    private readonly IWorldQuery _world;
    private readonly INpcStateQuery _npcState;

    // Tuning Knobs
    private float _rumorBaseChance = 0.3f;
    private float _sameRegionBonus = 0.4f;
    private float _absenceBonus = 0.2f;
    private int _absenceThresholdDays = 7;
    private float _rumorCap = 0.8f;

    /// <summary>
    /// 传闻触发概率计算 — GDD C5。
    /// 由活江湖层在区域进入时调用。
    /// </summary>
    public float CalcRumorChance(string npcId, string region)
    {
        float chance = _rumorBaseChance;

        // 女主当前在同一区域
        string heroineRegion = _npcState.GetAxis(npcId, "journey_region");
        if (heroineRegion == region)
            chance += _sameRegionBonus;

        // 长时间未联系
        int daysSinceContact = GetDaysSinceLastContact(npcId);
        if (daysSinceContact > _absenceThresholdDays)
            chance += _absenceBonus;

        return Math.Clamp(chance, 0f, _rumorCap);
    }

    /// <summary>
    /// 暗号发现 — 场景探索触发。
    /// </summary>
    public void OnSignDiscovered(string npcId)
    {
        var data = Services.NpcState.GetRomanceData(npcId);
        data.SignsDiscovered++;
        Services.NpcState.SetRomanceData(npcId, data);
        UpdateLastContactDay(npcId);
        Services.EventBus.Publish(new CometSignDiscoveredEvent(npcId));
    }

    /// <summary>
    /// 书信到达 — 活江湖层/NPC State 信使系统触发。
    /// </summary>
    public void OnLetterReceived(string npcId)
    {
        var data = Services.NpcState.GetRomanceData(npcId);
        data.LettersReceived++;
        Services.NpcState.SetRomanceData(npcId, data);
        UpdateLastContactDay(npcId);
    }

    private int GetDaysSinceLastContact(string npcId)
    {
        string flagKey = $"romance_last_contact_{npcId}";
        if (!_flags.HasFlag(flagKey)) return 999;
        return _world.GetCurrentDay() - _flags.GetFlagDay(flagKey);
    }

    private void UpdateLastContactDay(string npcId)
    {
        _flags.SetFlag($"romance_last_contact_{npcId}");
    }
}
```

### 4. EndingResolver (结局变体选取)

```csharp
// Feature/Romance/EndingResolver.cs
public class EndingResolver
{
    /// <summary>
    /// 终幕结局变体选取 — GDD C4。
    /// 输出 16 种变体之一。
    /// </summary>
    public EndingVariant Resolve(
        string mindsetZone,
        int moralityTier,
        BondedHeroine bonded)
    {
        // 魔道 override
        if (moralityTier <= -30)
            return new EndingVariant("demonic", "demonic", "dark");

        string script = mindsetZone; // 5 种核心脚本

        string variant;
        if (bonded != BondedHeroine.None)
        {
            bool compatible = IsZoneCompatible(bonded, mindsetZone);
            variant = compatible ? "companion" : "farewell";
        }
        else
        {
            variant = "solo";
        }

        string narratorTone = moralityTier switch
        {
            <= -15 => "dark",
            >= 15 => "light",
            _ => "neutral"
        };

        return new EndingVariant(script, variant, narratorTone);
    }

    /// <summary>
    /// 心境兼容性查询 — 每位女主有 1-2 个兼容方向。
    /// 具体映射由配置数据定义（OQ1 待确定）。
    /// </summary>
    public bool IsZoneCompatible(BondedHeroine heroine, string zone)
    {
        return _compatibilityTable.TryGetValue(heroine, out var zones)
            && zones.Contains(zone);
    }
}

public record EndingVariant(string Script, string Variant, string NarratorTone);
```

### 数据所有权与存储

```
┌─────────────────────────────────────────────────────┐
│  NPC State System (数据 Owner)                       │
│                                                      │
│  npc["heroine_a"].attitude = AttitudeLevel.Intimate  │
│  npc["heroine_a"].romance = RomanceData { ... }      │
│  npc["heroine_a"].journey_stage = "investigating"    │
└─────────────────────────────────────────────────────┘
         ↑ 读写                    ↑ 只读
┌──────────────────┐      ┌──────────────────┐
│ Romance Service  │      │ Dialogue System  │
│ (规则 Owner)     │      │ (消费者)         │
└──────────────────┘      └──────────────────┘
```

> **关键分离**：NPC State 拥有数据，Romance Service 拥有规则。其他系统（对话、误会、朦胧化 UI）只读取 NPC State 中的态度/里程碑值，不直接调用 Romance Service。

### 与活江湖层 (ADR-0014) 的集成

```csharp
// 活江湖层的传闻事件可触发 RomanceService：
// on_trigger: npc_state_change: { npc: "heroine_a", field: "attitude", value: +1 }
// → NPC State 收到变更请求 → 发布 AttitudeChangeRequestEvent
// → RomanceService 拦截并应用地板钳位
```

### 与 CG/演出系统 (ADR-0013) 的集成

```csharp
// 结缘/诀别触发演出：
public void ConfirmBond(string npcId)
{
    _milestones.Unlock(npcId, MilestoneType.Bond);
    _bondedHeroine = HeroineFromId(npcId);
    // 触发结缘演出
    Services.CutsceneService.PlayCutscene($"bond_{npcId}");
    Services.EventBus.Publish(new BondConfirmedEvent(npcId));
}
```

### 存档集成

```csharp
// RomanceService 存档数据（极简 — 大部分寄存于 NPC State）
public RomanceSaveData CreateSaveData() => new()
{
    BondedHeroine = _bondedHeroine
    // RomanceData (里程碑 flags) 随 NPC State 一起持久化
    // days_since_last_contact 通过 Flag 系统持久化
};
```

## Consequences

### Positive

- **数据规则分离**：NPC State 拥有数据，Romance Service 拥有规则，对话/误会等系统只需查 NPC State 即可获取关系信息
- **地板钳位可审计**：被吞掉的 delta 有日志记录，调试时可追踪"为什么态度没变"
- **结缘原子性**：TryBond → AwaitingChoice → ConfirmBond 三步流程确保玩家选择被尊重，不会出现竞态
- **结局公式清晰**：EndingResolver 纯函数，5×3+1 = 16 种变体可穷举测试
- **彗星解耦**：CometPresenceTracker 只做概率计算和统计，具体呈现由活江湖层/对话系统负责

### Negative

- **拦截模式耦合**：RomanceService 通过事件拦截 NPC State 的态度变更，若事件顺序出错可能导致地板失效
- **里程碑不可逆**：一旦误触发（如开发期 bug），只能通过存档回滚修复
- **4 位女主硬编码**：BondedHeroine 枚举写死 4 人，DLC 新增角色需改枚举

### Risks

| 风险 | 影响 | 缓解 |
|------|------|------|
| 事件拦截顺序错误导致地板钳位失效 | 态度跌破地板 | 在 NPC State 写入时增加二次校验断言 |
| force_break 在对话进行中触发 | NPC State 排队规则可能延迟覆写 | force_break 绕过排队，立即生效 |
| 心境兼容性配置未确定 (OQ1) | EndingResolver 无法完成映射 | 默认 fallback: 全不兼容 → 所有结缘走 farewell，待配置补齐后切换 |

## Alternatives Considered

### A. 态度与里程碑合一（单一进度条）

用连续数值表示关系进度，达到阈值自动解锁里程碑。

**优点**：简单直观。
**拒绝原因**：违反 GDD "态度可波动但里程碑不可逆" 的核心设计。单一进度条无法表达"共渡险境后即使吵架也不会回到陌路人"的地板语义。

### B. Romance Service 拥有数据（不寄存于 NPC State）

感情数据独立于 NPC State 系统。

**优点**：完全自治。
**拒绝原因**：对话系统、误会系统等需要频繁查询态度值。如果数据分散在两个系统，查询方需要知道"去 NPC State 查态度还是去 Romance Service 查态度"。统一寄存于 NPC State 消除此歧义。

### C. 结缘不互斥（后宫模式）

允许同时与多位女主结缘。

**优点**：玩家自由度更高。
**拒绝原因**：GDD 明确"得之我幸、失之我命"的设计哲学——选择的重量来自于互斥。且 16 种结局变体已基于互斥假设设计，解除互斥会导致组合爆炸。

## Compliance

- **双层协作 (B2, AC1)**：RomanceService 拦截态度变更 + MilestoneRegistry.GetFloor 实现地板钳位
- **里程碑不可跳级 (B1, AC4)**：CanUnlock 检查前置里程碑
- **结缘互斥 (B4, AC3)**：TryBond 检查 _bondedHeroine + M_HEART 前置
- **诀别覆写 (B2, AC2)**：force_break 路径无视地板，直接设 M_BREAK + 态度 -4
- **魔道 override (B5, AC5)**：EndingResolver 首先检查 moralityTier ≤ -30
- **传闻概率上限 (C5, AC6)**：CalcRumorChance 使用 Math.Clamp(..., 0, 0.8)
- **存档续算 (D8, AC7)**：days_since_last_contact 通过 Flag 系统 GetFlagDay 续算
- **拒绝结缘不重触发 (D3, AC9)**：DeclineBond 设置 bond_declined_{npcId} flag
