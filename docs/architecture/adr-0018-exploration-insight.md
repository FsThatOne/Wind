# ADR-0018: Exploration & Insight — Scene Insight Node Architecture

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | Scene Interaction, Spatial Detection, Discovery Pipeline |
| **Knowledge Risk** | **LOW** — 使用 Godot Area2D 触发器和标准距离查询，无实验性 API |
| **References Consulted** | `design/gdd/exploration-insight.md`, ADR-0001 (EventBus), ADR-0003 (Data Schema), ADR-0004 (Save), ADR-0006 (Scene Loading), ADR-0014 (ConditionEvaluator) |
| **Post-Cutoff APIs Used** | 无 |
| **Verification Required** | 1) 验证 Area2D 触发器在场景切换时的正确清理; 2) 验证多节点排队的协程调度不与战斗/对话锁冲突 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 发现事件发布), ADR-0003 (Data Schema — InsightNode 资源定义), ADR-0004 (Save — 已发现节点持久化), ADR-0006 (Scene Loading — 场景加载时注册节点), ADR-0013 (Cutscene — 复用 `LockMode` 共享枚举查询全局锁定状态), ADR-0014 (ConditionEvaluator — prerequisite 评估) |
| **Enables** | 主线叙事推进 quest_flag, 物品系统接收 Loot, 武学组合接收残卷, 暗号簿登记, 朦胧化 UI 触发水墨晕染 |
| **Blocks** | Sprint 6+ (场景内洞察交互、奇遇与洞察融合) |
| **Ordering Note** | ADR-0006 (Scene Loading) 必须就绪以提供场景生命周期; ADR-0013 定义的 `LockMode` 共享枚举须先 Accepted (供 ExplorationLockGuard 监听); ADR-0014 (ConditionEvaluator) 提供前置条件评估能力 |

## Context

GDD #19 定义了场景层的洞察发现机制。它是对话系统（已有对话内洞察）和战斗系统（已有意图洞察概率）的**场景层补充**——让 insight 属性在世界探索中同样有意义。

需要解决的架构问题：
1. 如何在场景内高效检测玩家进入 InsightNode 的 detection_radius？
2. 如何让 6 种 discovery_type 共享同一发现管道但又能正确分派到不同子系统？
3. 多节点同时在范围内时如何按距离 stagger 触发，避免信息轰炸？
4. 如何让"洞察揭示历史"在存档/加载/场景切换中保持一致？

## Decision

### D1. InsightNode — Resource 数据结构

```csharp
[GlobalClass]
public partial class InsightNode : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string SceneId { get; set; } = "";
    [Export] public Vector2 Position { get; set; }
    [Export] public float DetectionRadius { get; set; } = 1.5f;
    [Export] public int InsightThreshold { get; set; } = 10;
    [Export] public DiscoveryType DiscoveryType { get; set; }
    [Export] public DiscoveryReward Reward { get; set; } = new();
    [Export] public string NarrativeContext { get; set; } = "";
    [Export] public Condition[] Prerequisite { get; set; } = Array.Empty<Condition>();
    [Export] public bool OneTime { get; set; } = true;
}

public enum DiscoveryType
{
    Clue, Loot, MartialFragment, CodePhrase, SideQuestEntry, EnvironmentDetail
}

[GlobalClass]
public partial class DiscoveryReward : Resource
{
    [Export] public string FlagId { get; set; } = "";       // Clue
    [Export] public Variant FlagValue { get; set; }
    [Export] public string ItemId { get; set; } = "";       // Loot
    [Export] public int Quantity { get; set; } = 1;
    [Export] public string MartialId { get; set; } = "";    // MartialFragment
    [Export] public string PhraseId { get; set; } = "";     // CodePhrase
    [Export] public string QuestNodeId { get; set; } = "";  // SideQuestEntry
}

public enum DiscoveryState { Undiscovered, Detected, Ignored, Investigated }
```

### D2. InsightNodeRegistry — 场景节点注册表

```csharp
public class InsightNodeRegistry
{
    private readonly Dictionary<string, InsightNode> _allNodes = new();
    private readonly Dictionary<string, DiscoveryState> _states = new(); // 来自存档
    private readonly List<InsightNode> _activeSceneNodes = new();

    public void OnSceneLoaded(string sceneId)
    {
        _activeSceneNodes.Clear();
        foreach (var node in _allNodes.Values.Where(n => n.SceneId == sceneId))
        {
            // 已 Investigated 的 OneTime 节点跳过
            if (_states.GetValueOrDefault(node.Id) == DiscoveryState.Investigated && node.OneTime)
                continue;
            _activeSceneNodes.Add(node);
        }
        Services.EventBus.Publish(new InsightSceneActivatedEvent(sceneId, _activeSceneNodes.Count));
    }

    public void OnSceneUnloaded()
    {
        // 重置瞬态状态：DETECTED/IGNORED → UNDISCOVERED
        foreach (var node in _activeSceneNodes)
        {
            var state = _states.GetValueOrDefault(node.Id, DiscoveryState.Undiscovered);
            if (state == DiscoveryState.Detected || state == DiscoveryState.Ignored)
                _states[node.Id] = DiscoveryState.Undiscovered;
        }
        _activeSceneNodes.Clear();
    }

    public IReadOnlyList<InsightNode> GetActiveNodes() => _activeSceneNodes;

    public DiscoveryState GetState(string nodeId)
        => _states.GetValueOrDefault(nodeId, DiscoveryState.Undiscovered);

    public void SetState(string nodeId, DiscoveryState state)
    {
        _states[nodeId] = state;
        if (state == DiscoveryState.Investigated)
            Services.Save.MarkDirty(SaveDomain.Exploration);
    }
}
```

### D3. ProximityDetector — 距离感知与排队

```csharp
public partial class ProximityDetector : Node
{
    private const float MultiNodeStaggerSec = 1.5f;
    private const float CueLingerSec = 5f;
    private readonly Queue<InsightNode> _pendingTriggers = new();
    private float _staggerTimer;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 playerPos = Services.Player.GetPosition();
        var inRange = _registry.GetActiveNodes()
            .Where(n => n.Position.DistanceTo(playerPos) <= n.DetectionRadius)
            .Where(n => _registry.GetState(n.Id) == DiscoveryState.Undiscovered)
            .OrderBy(n => n.Position.DistanceTo(playerPos)) // 按距离近→远
            .ToList();

        // 入队新进入范围的节点
        foreach (var node in inRange)
        {
            if (!_pendingTriggers.Contains(node) && !_currentlyDetected.Contains(node))
                _pendingTriggers.Enqueue(node);
        }

        // 处理离开范围的 Detected/Ignored 节点
        foreach (var node in _currentlyDetected.ToList())
        {
            if (node.Position.DistanceTo(playerPos) > node.DetectionRadius)
            {
                _registry.SetState(node.Id, DiscoveryState.Undiscovered);
                _currentlyDetected.Remove(node);
                Services.EventBus.Publish(new InsightCueHiddenEvent(node.Id));
            }
        }

        // Stagger 节流
        _staggerTimer += (float)delta;
        if (_staggerTimer >= MultiNodeStaggerSec && _pendingTriggers.Count > 0)
        {
            _staggerTimer = 0f;
            ProcessNextTrigger(_pendingTriggers.Dequeue());
        }
    }

    private void ProcessNextTrigger(InsightNode node)
    {
        // 1. 前置条件
        if (!_conditionEvaluator.EvaluateAll(node.Prerequisite)) return;

        // 2. 洞察检定 — 布尔判定，无概率
        int playerInsight = Services.CharacterAttributes.GetInsight();
        if (playerInsight < node.InsightThreshold) return; // 失败：完全无提示

        // 3. 成功 → DETECTED + 视觉提示
        _registry.SetState(node.Id, DiscoveryState.Detected);
        _currentlyDetected.Add(node);
        Services.EventBus.Publish(new InsightCueShownEvent(node.Id, node.Position));

        // 4. 启动 linger 计时器
        StartLingerTimer(node);
    }

    private async void StartLingerTimer(InsightNode node)
    {
        await Task.Delay(TimeSpan.FromSeconds(CueLingerSec));
        if (_registry.GetState(node.Id) == DiscoveryState.Detected)
        {
            _registry.SetState(node.Id, DiscoveryState.Ignored);
            Services.EventBus.Publish(new InsightCueHiddenEvent(node.Id));
        }
    }
}
```

### D4. DiscoveryDispatcher — 6 种类型分派

```csharp
public class DiscoveryDispatcher
{
    public void OnPlayerInvestigate(string nodeId)
    {
        var node = _registry.GetNode(nodeId);
        if (_registry.GetState(nodeId) != DiscoveryState.Detected) return;

        // 1. 播放独白（Inner Monologue 节点）
        if (!string.IsNullOrEmpty(node.NarrativeContext))
            Services.Dialogue.PlayInnerMonologue(node.NarrativeContext);

        // 2. 按 type 分派奖励
        DispatchReward(node);

        // 3. 标记为已查
        _registry.SetState(nodeId, DiscoveryState.Investigated);

        // 4. 通知对话系统解锁洞察选择
        Services.EventBus.Publish(new InsightDiscoveredEvent(nodeId));
    }

    private void DispatchReward(InsightNode node)
    {
        var r = node.Reward;
        switch (node.DiscoveryType)
        {
            case DiscoveryType.Clue:
                Services.Flags.SetFlag(r.FlagId, r.FlagValue);
                Services.Narrative.OnClueDiscovered(r.FlagId);
                break;
            case DiscoveryType.Loot:
                Services.Inventory.GrantItem(r.ItemId, r.Quantity);
                break;
            case DiscoveryType.MartialFragment:
                Services.MartialArts.AcquireFragment(r.MartialId);
                break;
            case DiscoveryType.CodePhrase:
                Services.CodePhraseBook.Learn(r.PhraseId);
                break;
            case DiscoveryType.SideQuestEntry:
                Services.Narrative.UnlockNode(r.QuestNodeId);
                break;
            case DiscoveryType.EnvironmentDetail:
                // 仅独白，无额外奖励
                break;
        }
    }

    public void OnPlayerIgnore(string nodeId)
    {
        // 玩家主动忽略 → IGNORED；离开范围后回退为 UNDISCOVERED
        _registry.SetState(nodeId, DiscoveryState.Ignored);
        Services.EventBus.Publish(new InsightCueHiddenEvent(nodeId));
    }
}
```

### D5. 战斗/对话期间的暂停协议

```csharp
public class ExplorationLockGuard
{
    public void OnGameStateLockAcquired(LockMode mode)
    {
        // 进入战斗/对话/演出时，暂停所有洞察提示
        if (mode >= LockMode.Partial)
        {
            _proximityDetector.SetActive(false);
            HideAllActiveCues();
        }
    }

    public void OnGameStateLockReleased()
    {
        _proximityDetector.SetActive(true);
        // 恢复后重新检测：仍在范围且条件满足的节点会重新触发
    }
}
```

### D6. 与奇遇系统的边界契约

```csharp
// map-scene-management 拥有奇遇位置和触发条件
// 探索/洞察系统仅负责 trigger_type=INSIGHT 的奇遇检定

public class EncounterInsightHook
{
    public bool TryTriggerInsightEncounter(string encounterId, int threshold)
    {
        int playerInsight = Services.CharacterAttributes.GetInsight();
        return playerInsight >= threshold;
    }
}
// 非洞察型奇遇（路遇劫匪/偶遇高人等）不经过本系统
```

### D7. 存档契约

```csharp
[Serializable]
public class ExplorationSaveData
{
    // 仅持久化 Investigated 终态；DETECTED/IGNORED 为瞬态
    public Dictionary<string, DiscoveryState> InvestigatedNodes { get; set; } = new();
}

public class ExplorationSerializer
{
    public ExplorationSaveData Capture()
    {
        return new ExplorationSaveData
        {
            InvestigatedNodes = _registry.GetAllStates()
                .Where(kv => kv.Value == DiscoveryState.Investigated)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    public void Restore(ExplorationSaveData data)
    {
        foreach (var kv in data.InvestigatedNodes)
            _registry.SetState(kv.Key, kv.Value);
    }
}
```

## Consequences

### Positive
- **统一发现管道**: 6 种 DiscoveryType 共享同一检定 + 分派流程，新增类型只需扩展 switch
- **零概率干扰**: 布尔门槛与对话/战斗洞察检定规则完全一致，玩家心智模型统一
- **场景生命周期对齐**: 通过 ADR-0006 Scene Loading 钩子注册/清理，无内存泄漏
- **存档极简**: 仅持久化 Investigated 终态，DETECTED/IGNORED 为瞬态自动重置
- **复用 ConditionEvaluator**: 前置条件评估直接复用 ADR-0014 共享引擎，零重复

### Negative
- **每帧距离查询**: ProximityDetector._PhysicsProcess 每帧遍历活动节点；场景节点 ≤ 6 时影响可忽略
- **stagger 延迟感知**: 多节点同时在范围内时第二个节点延迟 1.5s 才出现，可能让玩家以为只有一个

### Risks
- **场景边界泄漏**: 切换场景时若未正确调用 OnSceneUnloaded，瞬态可能错误持久化
- **奇遇耦合**: 与 map-scene-management 的奇遇接口需在 ADR-0006 / 后续奇遇 ADR 中明确契约

## Alternatives Considered

| 方案 | 理由 |
|------|------|
| 概率检定（如 dialogue 战斗洞察） | GDD F-1 明确规定纯布尔判定，与对话系统场景层规则一致 |
| HUD 雷达点标记可发现物 | 违反朦胧化原则，破坏"我比别人看得更深"的探索感 |
| 每个 InsightNode 一个 Area2D 节点 | 场景节点数量爆炸，且 _PhysicsProcess 距离查询已足够高效 |
| 不使用 stagger，多节点同时弹出 | 信息轰炸，违反 GDD E2 处理 |
| 持久化 IGNORED 状态 | 玩家"忽略"后再次接近应可重新提示，持久化会破坏此体验 |

## Compliance

| GDD Section | ADR Coverage |
|-------------|-------------|
| 三类交互（明示/环境/洞察） | D1 仅管理 InsightNode（洞察类）;明示/环境由场景层处理 |
| InsightNode 数据结构 | D1 InsightNode Resource |
| 6 种 DiscoveryType | D4 DiscoveryDispatcher.DispatchReward |
| 洞察检定流程 | D3 ProximityDetector.ProcessNextTrigger |
| 状态机 (UNDISCOVERED→DETECTED→IGNORED→INVESTIGATED) | D2 InsightNodeRegistry.SetState |
| F-1 布尔判定 | D3 (playerInsight >= threshold) |
| F-2 场景节点上限 | 内容设计约束，运行时不强制 |
| F-3 门槛分级 | 内容配置；架构不约束门槛值 |
| 多节点 stagger E2 | D3 MultiNodeStaggerSec=1.5f |
| 重访已失败场景 E1 | D3 离开范围后回退 Undiscovered，重新检定 |
| prerequisite 实时变化 E3 | D3 每次进入触发时评估 ConditionEvaluator |
| 战斗/对话期间提示 E4 | D5 ExplorationLockGuard |
| 存档加载瞬态重置 E6 | D7 仅持久化 Investigated |
| 奇遇接口 | D6 EncounterInsightHook |
| linger 自动消失 AC9 | D3 StartLingerTimer (CueLingerSec=5s) |
