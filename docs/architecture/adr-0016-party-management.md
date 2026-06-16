# ADR-0016: Party Management — Unified Growth & Deployment Architecture

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | Character Data, Deployment Logic, Growth Settlement |
| **Knowledge Risk** | **LOW** — 纯逻辑/数据系统，不依赖引擎特定渲染或实验性 API |
| **References Consulted** | `design/gdd/party-management.md`, ADR-0001, ADR-0003, ADR-0004, ADR-0008, ADR-0014 |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) 验证 GrowthSettlementEngine 与 Save System 的序列化兼容; 2) 验证 CatchupCalculator 在极端 party_average 下不溢出 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 成长事件发布/部署变更通知), ADR-0003 (Data Schema — PlayableCharacter 结构定义), ADR-0004 (Save — 队伍状态持久化), ADR-0008 (FSM/NPC State — 同伴可用性/离队状态查询), ADR-0014 (Living Jianghu — 代办结果投递/离队历练触发) |
| **Enables** | 战斗系统读取上阵名单, 顿悟系统查询同伴奖励池, 装备系统绑定校验, 存档系统队伍快照 |
| **Blocks** | Sprint 6+ (队伍 UI 面板, 代办成长结算) |
| **Ordering Note** | ADR-0003 (Data Schema) 和 ADR-0008 (NPC State) 必须就绪以提供角色结构和状态查询 |

## Context

GDD #25 规定：主角与同伴使用完全相同的可玩角色结构，成长不依赖刷怪，而是通过 6 种叙事驱动来源（章节基线、关键战斗心得、顿悟、个人旅程、末尾追赶、装备/武学）获得永久属性。

需要解决的架构问题：
1. 如何保证统一结构在扩展新同伴时零代码修改？
2. 如何在多成长管道下防止叠加超限？
3. 如何让部署校验支持剧情锁定、离队、重伤等多重约束？
4. 如何将离队历练的成长结算与活江湖层的触发逻辑解耦？

## Decision

### D1. PlayableCharacter — 统一 Resource 结构

```csharp
[GlobalClass]
public partial class PlayableCharacter : Resource
{
    // Identity
    [Export] public string CharacterId { get; set; } = "";
    [Export] public bool IsProtagonist { get; set; }

    // 五维属性
    [Export] public int Strength { get; set; }
    [Export] public int InnerForce { get; set; }
    [Export] public int Agility { get; set; }
    [Export] public int Insight { get; set; }
    [Export] public int Constitution { get; set; }

    // 资源属性
    [Export] public int MaxHp { get; set; }
    [Export] public int MaxQi { get; set; }
    [Export] public int MaxOpenings { get; set; }

    // 槽位 (引用 ID)
    [Export] public string[] EquipmentSlots { get; set; } = new string[5]; // 主手,身甲,足具,饰品1,饰品2
    [Export] public string[] MartialArtsSlots { get; set; } = new string[6];
    [Export] public string InternalArtSlot { get; set; } = "";
    [Export] public string MovementArtSlot { get; set; } = "";

    // 成长追踪
    [Export] public int TotalPower { get; set; }
    [Export] public int CatchupUsedThisChapter { get; set; }
    [Export] public int DelegateGrowthThisChapter { get; set; }

    // 状态
    [Export] public PartyMemberState CurrentState { get; set; } = PartyMemberState.Available;
}

public enum PartyMemberState
{
    NotRecruited,   // 未加入
    Available,      // 可同行/可上阵
    Deployed,       // 当前上阵
    AwayTraining,   // 离队历练
    OnDelegate,     // 代办中
    Locked,         // 剧情锁定/不可用
    Injured,        // 重伤
    Departed        // 永久离队
}
```

### D2. DeploymentManager — 上阵管理

```csharp
public class DeploymentManager
{
    private const int MaxDeployed = 5;
    private readonly List<string> _deployedIds = new();
    private readonly Stack<DeploymentLock> _lockStack = new();

    public bool CanDeploy(string characterId)
    {
        var pc = _roster.Get(characterId);
        if (pc == null) return false;
        if (pc.CurrentState is PartyMemberState.AwayTraining
            or PartyMemberState.OnDelegate
            or PartyMemberState.Locked
            or PartyMemberState.Injured
            or PartyMemberState.Departed
            or PartyMemberState.NotRecruited) return false;
        if (_deployedIds.Count >= MaxDeployed) return false;
        return true;
    }

    public bool CanUndeploy(string characterId)
    {
        var pc = _roster.Get(characterId);
        if (pc == null || pc.IsProtagonist) return false; // 主角不可下阵
        if (_lockStack.Count > 0 && _lockStack.Peek().LockedIds.Contains(characterId))
            return false; // 剧情锁定
        return true;
    }

    public void PushLock(DeploymentLock lockDef)
    {
        _lockStack.Push(lockDef);
        foreach (var id in lockDef.LockedIds)
            if (!_deployedIds.Contains(id)) ForceDeployInternal(id);
    }

    public void PopLock() => _lockStack.Pop();
}

public record DeploymentLock(string Reason, HashSet<string> LockedIds);
```

### D3. GrowthSettlementEngine — 统一成长管道

```csharp
public class GrowthSettlementEngine
{
    private const float ObservationGrowthRate = 0.35f;
    private const int CatchupChapterCap = 8;
    private const int DelegateGrowthCapPerChapter = 4;

    public void SettleBattleGrowth(BattleResult result)
    {
        if (!result.IsGrowthEligible) return;

        foreach (var id in result.DeployedIds)
            ApplyGrowthNode(id, GrowthSource.BattleInsight, result.ConfiguredGain);

        foreach (var id in GetBenchObservers())
            ApplyGrowthNode(id, GrowthSource.Observation,
                Mathf.FloorToInt(result.ConfiguredGain * ObservationGrowthRate));
    }

    public void SettleChapterBaseline(int chapter)
    {
        int baseline = _chapterData.GetBaselinePower(chapter);
        foreach (var pc in _roster.GetAllRecruited())
        {
            if (pc.TotalPower < baseline)
                ApplyGrowthNode(pc.CharacterId, GrowthSource.ChapterBaseline,
                    baseline - pc.TotalPower);
        }
    }

    public void SettleCatchup(string characterId)
    {
        var pc = _roster.Get(characterId);
        int target = _catchup.CalcTarget();
        if (pc.TotalPower >= target) return;

        int gap = target - pc.TotalPower;
        int remaining = CatchupChapterCap - pc.CatchupUsedThisChapter;
        int gain = Math.Min(gap, remaining);
        if (gain <= 0) return;

        ApplyGrowthNode(characterId, GrowthSource.Catchup, gain);
        pc.CatchupUsedThisChapter += gain;
    }

    public void SettleDelegateResult(DelegateResult result)
    {
        var pc = _roster.Get(result.CharacterId);
        int remaining = DelegateGrowthCapPerChapter - pc.DelegateGrowthThisChapter;
        int gain = Math.Min(result.GrowthValue, remaining);
        if (gain <= 0) return;

        ApplyGrowthNode(result.CharacterId, GrowthSource.PersonalJourney, gain);
        pc.DelegateGrowthThisChapter += gain;
        // 代办成长计入追赶上限
        pc.CatchupUsedThisChapter += gain;
    }

    private void ApplyGrowthNode(string characterId, GrowthSource source, int value)
    {
        _roster.Get(characterId).TotalPower += value;
        Services.EventBus.Publish(new GrowthAppliedEvent(characterId, source, value));
        Services.Save.MarkDirty(SaveDomain.Party);
    }
}

public enum GrowthSource
{
    ChapterBaseline, BattleInsight, Observation, Epiphany, PersonalJourney, Catchup
}
```

### D4. CatchupCalculator — 末尾追赶

```csharp
public class CatchupCalculator
{
    private const float CatchupFloorRatio = 0.85f;

    public int CalcTarget()
    {
        int chapterBaseline = _chapterData.GetBaselinePower(_narrative.GetCurrentChapter());
        float partyAverage = _roster.GetAllRecruited()
            .Where(pc => pc.CurrentState != PartyMemberState.Departed)
            .Average(pc => pc.TotalPower);
        int floorFromAverage = Mathf.FloorToInt(partyAverage * CatchupFloorRatio);
        return Math.Max(chapterBaseline, floorFromAverage);
    }
}
```

### D5. EquipmentRegistry — 装备实例唯一绑定

```csharp
public class EquipmentRegistry
{
    private readonly Dictionary<string, string> _instanceToOwner = new(); // instanceId → characterId

    public bool TryEquip(string instanceId, string characterId, int slotIndex)
    {
        if (_instanceToOwner.TryGetValue(instanceId, out var currentOwner))
        {
            if (currentOwner == characterId) return true; // 已装备
            return false; // 需先卸下
        }
        _instanceToOwner[instanceId] = characterId;
        _roster.Get(characterId).EquipmentSlots[slotIndex] = instanceId;
        return true;
    }

    public void Unequip(string instanceId)
    {
        if (!_instanceToOwner.TryGetValue(instanceId, out var owner)) return;
        var pc = _roster.Get(owner);
        int idx = Array.IndexOf(pc.EquipmentSlots, instanceId);
        if (idx >= 0) pc.EquipmentSlots[idx] = "";
        _instanceToOwner.Remove(instanceId);
    }
}
```

### D6. 与活江湖层的边界契约

```csharp
// 活江湖层 (ADR-0014) 发布代办结果事件
public record DelegateCompletedEvent(string CharacterId, DelegateResult Result);

// PartyManagement 订阅并仅负责成长结算
Services.EventBus.Subscribe<DelegateCompletedEvent>(e =>
    _growthEngine.SettleDelegateResult(e.Result));

// 活江湖层不直接写入角色属性
// 顿悟系统通过 EventBus 通知，PartyManagement 不干预奖励池选取
```

### D7. 章节重置

```csharp
public void OnChapterAdvance(int newChapter)
{
    foreach (var pc in _roster.GetAllRecruited())
    {
        pc.CatchupUsedThisChapter = 0;
        pc.DelegateGrowthThisChapter = 0;
    }
    SettleChapterBaseline(newChapter);
}
```

## Consequences

### Positive
- **零代码扩展新同伴**: 新角色仅需 PlayableCharacter Resource 配置，无需新类
- **成长不可叠加超限**: 所有板凳成长共享 chapter cap，防止"代办+追赶+观战"三路叠加
- **部署规则可堆叠**: DeploymentLock 栈允许多重剧情锁定嵌套
- **与活江湖层解耦**: Party 只管结算，触发和分支由 ADR-0014 侧管理

### Negative
- **TotalPower 单维聚合**: 为追赶计算需要的标量，牺牲了多维度独立追踪的精度
- **观战心得均分**: 当前设计不区分观战角色是否"适合"本场战斗

### Risks
- **Catchup 参数敏感**: `catchup_floor_ratio` 过高会让轮换无代价，过低会让角色永久掉队
- **代办并发管理**: `max_concurrent_away_training=2` 需要 UI 清晰表达，否则玩家困惑

## Alternatives Considered

| 方案 | 理由 |
|------|------|
| 经验值 + 等级制 | 违反"成长不靠刷怪"核心设计，且产生板凳掉级恐慌 |
| 分离主角/同伴数据结构 | 增加维护成本，且 GDD CR-1 明确要求统一结构 |
| 自动追赶无上限 | 消除上阵意义，使玩家养成选择退化 |
| 代办成长独立 cap | 与观战/追赶分开计算，可能产生多路叠加超限 |

## Compliance

| GDD Section | ADR Coverage |
|-------------|-------------|
| CR-1 统一结构 | D1 PlayableCharacter Resource |
| CR-2 上阵换人 | D2 DeploymentManager |
| CR-3 成长不靠刷怪 | D3 GrowthSettlementEngine (6 来源) |
| CR-4 观战心得 | D3.SettleBattleGrowth (ObservationGrowthRate=0.35) |
| CR-5 末尾追赶 | D4 CatchupCalculator (floor_ratio=0.85, cap=8) |
| CR-6 离队历练/代办 | D3.SettleDelegateResult + D6 边界契约 |
| CR-7 战斗外顿悟 | EventBus 订阅，不干预奖励池 |
| CR-8 自然上阵机会 | D2.PushLock (内容层配置) |
| F1 关键战斗心得 | D3 公式实现 |
| F2 末尾追赶公式 | D4 公式实现 |
| F3 代办能力评估 | 由 NPC State 侧实现，Party 不重复 |
| Edge Cases (8 条) | D2 校验 + D3 cap 钳位 + D5 唯一绑定 |
