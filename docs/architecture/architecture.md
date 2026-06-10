# 《风止》Master Architecture Document

> **Status**: Draft — Pending TD Sign-off
> **Engine**: Godot 4.6.3 (C# / .NET 8+)
> **Created**: 2026-06-08
> **Source**: 25 GDDs × 575+ Technical Requirements

---

## 1. Architecture Overview

《风止》采用 **5 层分层架构**，严格单向依赖，层间通过事件总线解耦：

```
┌─────────────────────────────────────────────────┐
│            Presentation Layer                    │
│  CombatUi · BlurredUi · Cutscene · Audio · Tutorial  │
├─────────────────────────────────────────────────┤
│              Feature Layer                       │
│  Romance · Jianghu · Epiphany · Misunderstanding │
│  Exploration · Party                            │
├─────────────────────────────────────────────────┤
│               Core Layer                        │
│  Combat · MartialArts · EnemyAi · Dialogue      │
│  Mindset · Narrative · Items                    │
├─────────────────────────────────────────────────┤
│            Foundation Layer                      │
│  CharacterData · NpcState · SceneManagement     │
│  TimeSystem                                     │
├─────────────────────────────────────────────────┤
│             Platform Layer                       │
│  Save · Settings · Steam                        │
├─────────────────────────────────────────────────┤
│          Shared (cross-cutting)                  │
│  EventBus · DataRegistry · StateMachine · Extensions │
└─────────────────────────────────────────────────┘
```

**依赖规则**：
- 上层 → 下层：允许直接引用
- 下层 → 上层：禁止，必须通过 EventBus 事件通知
- 同层：允许同层模块间引用
- Shared：所有层均可引用

---

## 2. Engine Risk Assessment

| 域 | 风险等级 | 影响模块 | 原因 |
|---|---------|---------|------|
| UI dual-focus system | **HIGH** | CombatUi, BlurredUi | Godot 4.6 post-cutoff，行为未经验证 |
| C# binding patterns | **HIGH** | 全部 .cs | 4.4+ Signal/Export 变更 |
| Input SDL3 | **MEDIUM** | Settings, 全局 | SDL3 替换 SDL2，rebinding API 变更 |
| TileMapLayer | **MEDIUM** | SceneManagement | rotation/transform API 可能变更 |
| Audio | LOW | Audio | 无破坏性变更 |
| 2D Physics | LOW | Combat, Exploration | 无破坏性变更 |
| Navigation 2D | LOW | Jianghu, Exploration | 独立 NavigationServer2D，稳定 |
| Animation | LOW | Cutscene, CombatUi | 无破坏性变更 |

> **缓解策略**：HIGH RISK 域在实现前必须先建 spike/prototype 验证 API 行为，并记录为 ADR。

---

## 3. System Layer Mapping

### 3.1 Platform Layer (引擎抽象、平台 API、IO)

| # | 系统 | 模块 | 职责 |
|---|------|------|------|
| 8 | 存档系统 | `Platform/Save/` | 文件 IO、AES-256 加密、版本迁移链、槽位管理 |
| 23 | 设置/选项 | `Platform/Settings/` | 配置持久化、输入重映射、音量/画面选项 |
| 24 | 成就/Steam | `Platform/Steam/` | Steamworks API 封装、成就解锁、统计 |

### 3.2 Foundation Layer (数据模型、状态管理、场景基础设施)

| # | 系统 | 模块 | 职责 |
|---|------|------|------|
| 1 | 角色属性/功力 | `Foundation/CharacterData/` | 属性数据模型、功力曲线、公式引擎 (F1-F9) |
| 10 | NPC 状态管理 | `Foundation/NpcState/` | 6 状态 FSM、态度值、独立旅程状态 |
| 12 | 地图/场景管理 | `Foundation/SceneManagement/` | 异步加载、18 场景跳转、色调控制、解锁 |
| 11 | 自然日+体力 | `Foundation/TimeSystem/` | 日历推进、时段、体力池、旅行耗时 |

### 3.3 Core Layer (核心玩法逻辑)

| # | 系统 | 模块 | 职责 |
|---|------|------|------|
| 2 | 回合制战斗 | `Core/Combat/` | Burst+Read 判定、回合管理、8 动作类型 |
| 3 | 武学组合 | `Core/MartialArts/` | 招式数据、刚/柔/巧克制、连招规则 |
| 4 | 敌方 AI | `Core/EnemyAi/` | 意图系统、F8/F9 公式决策、多阶段 Boss |
| 5 | 对话系统 | `Core/Dialogue/` | 节点图、条件分支、变量绑定 |
| 6 | 心境双轴 | `Core/Mindset/` | 善恶/刚柔轴、9 区域判定、位移计算 |
| 9 | 主线叙事 | `Core/Narrative/` | 章节推进、19 叙事节点、结局收敛 |
| 15 | 物品/道具 | `Core/Items/` | 物品数据、背包、装备、效果系统 |

### 3.4 Feature Layer (高层组合逻辑)

| # | 系统 | 模块 | 职责 |
|---|------|------|------|
| 13 | 感情系统 | `Feature/Romance/` | 彗星模型、亲密度、5 NPC 关系线 |
| 16 | 活江湖层 | `Feature/Jianghu/` | 传闻传播、暗号轮换、NPC 日程、代办 |
| 17 | 顿悟突破 | `Feature/Epiphany/` | 回合末触发、凝神免死、排他二选一 |
| 18 | 误会系统 | `Feature/Misunderstanding/` | 6 状态机、三阶段透明度、释怀路径 |
| 19 | 探索/洞察 | `Feature/Exploration/` | 线索发现、洞察力阈值、场景交互 |
| 25 | 队伍管理 | `Feature/Party/` | 5 人编队、同伴成长、板凳追赶 |

### 3.5 Presentation Layer (UI、视听、教学)

| # | 系统 | 模块 | 职责 | 引擎风险 |
|---|------|------|------|---------|
| 7 | 战斗 UI | `Presentation/CombatUi/` | HUD、意图指示器、Burst 动画 | **HIGH** |
| 14 | 朦胧化 UI | `Presentation/BlurredUi/` | 动态模糊、信息差、色调联动 | **HIGH** |
| 20 | CG/演出 | `Presentation/Cutscene/` | 时间轴、相机、特效编排 | LOW |
| 21 | 音乐/音效 | `Presentation/Audio/` | 动态音乐、环境音、情绪锚点 | LOW |
| 22 | 教学/引导 | `Presentation/Tutorial/` | 触发器、覆盖 UI、首访检测 | **HIGH** |

---

## 4. Namespace & Directory Structure

```
src/
├── FengZhi.Platform/
│   ├── Save/
│   │   ├── ISaveManager.cs
│   │   ├── SaveManager.cs
│   │   ├── SaveSnapshot.cs
│   │   ├── EncryptionService.cs
│   │   └── MigrationChain/
│   ├── Settings/
│   │   ├── ISettingsProvider.cs
│   │   └── SettingsProvider.cs
│   └── Steam/
│       ├── ISteamBridge.cs
│       └── SteamBridge.cs
│
├── FengZhi.Foundation/
│   ├── CharacterData/
│   │   ├── ICharacterRegistry.cs
│   │   ├── CharacterRegistry.cs
│   │   ├── FormulaEngine.cs       # F1-F9 公式实现
│   │   └── Models/
│   ├── NpcState/
│   │   ├── INpcStateManager.cs
│   │   ├── NpcStateManager.cs
│   │   └── NpcFsm.cs
│   ├── SceneManagement/
│   │   ├── ISceneDirector.cs
│   │   ├── SceneDirector.cs
│   │   └── SceneTransitionData.cs
│   └── TimeSystem/
│       ├── ITimeManager.cs
│       ├── TimeManager.cs
│       └── StaminaPool.cs
│
├── FengZhi.Core/
│   ├── Combat/
│   │   ├── ICombatSystem.cs
│   │   ├── CombatSystem.cs
│   │   ├── TurnResolver.cs
│   │   ├── BurstJudge.cs
│   │   └── DamageCalculator.cs
│   ├── MartialArts/
│   │   ├── IMartialArtsRegistry.cs
│   │   ├── MartialArtsRegistry.cs
│   │   └── CounterMatrix.cs
│   ├── EnemyAi/
│   │   ├── IEnemyAiEngine.cs
│   │   ├── EnemyAiEngine.cs
│   │   └── IntentResolver.cs
│   ├── Dialogue/
│   │   ├── IDialogueRunner.cs
│   │   ├── DialogueRunner.cs
│   │   └── ConditionEvaluator.cs
│   ├── Mindset/
│   │   ├── IMindsetSystem.cs
│   │   ├── MindsetSystem.cs
│   │   └── RegionCalculator.cs
│   ├── Narrative/
│   │   ├── INarrativeDirector.cs
│   │   ├── NarrativeDirector.cs
│   │   └── EndingConvergence.cs
│   └── Items/
│       ├── IInventory.cs
│       ├── Inventory.cs
│       └── ItemEffectProcessor.cs
│
├── FengZhi.Feature/
│   ├── Romance/
│   │   ├── IRomanceSystem.cs
│   │   ├── RomanceSystem.cs
│   │   └── CometModel.cs
│   ├── Jianghu/
│   │   ├── IJianghuLayer.cs
│   │   ├── JianghuLayer.cs
│   │   ├── RumorEngine.cs
│   │   └── PassphraseManager.cs
│   ├── Epiphany/
│   │   ├── IEpiphanySystem.cs
│   │   ├── EpiphanySystem.cs
│   │   └── BreakthroughResolver.cs
│   ├── Misunderstanding/
│   │   ├── IMisunderstandingSystem.cs
│   │   ├── MisunderstandingSystem.cs
│   │   └── TransparencyManager.cs
│   ├── Exploration/
│   │   ├── IExplorationSystem.cs
│   │   ├── ExplorationSystem.cs
│   │   └── ClueRegistry.cs
│   └── Party/
│       ├── IPartyManager.cs
│       ├── PartyManager.cs
│       └── CatchUpCalculator.cs
│
├── FengZhi.Presentation/
│   ├── CombatUi/
│   │   ├── ICombatUiController.cs
│   │   ├── CombatHud.cs
│   │   └── IntentIndicator.cs
│   ├── BlurredUi/
│   │   ├── IBlurredUiController.cs
│   │   ├── BlurredUiController.cs
│   │   └── ToneTransition.cs
│   ├── Cutscene/
│   │   ├── ICutscenePlayer.cs
│   │   ├── CutscenePlayer.cs
│   │   └── TimelineDirector.cs
│   ├── Audio/
│   │   ├── IAudioDirector.cs
│   │   ├── AudioDirector.cs
│   │   └── DynamicMusicFsm.cs
│   └── Tutorial/
│       ├── ITutorialSystem.cs
│       ├── TutorialSystem.cs
│       └── TriggerRegistry.cs
│
└── FengZhi.Shared/
    ├── EventBus/
    │   ├── IEventBus.cs
    │   ├── EventBus.cs
    │   └── GameEvent.cs
    ├── DataRegistry/
    │   ├── IDataTable.cs
    │   └── YamlDataLoader.cs
    ├── StateMachine/
    │   ├── IStateMachine.cs
    │   └── FiniteStateMachine.cs
    ├── Contracts/
    │   ├── ISaveable.cs
    │   └── SaveSnapshot.cs
    └── Extensions/
        └── NodeExtensions.cs
```

---

## 5. Cross-Cutting Concerns

### 5.1 Event Bus (信号路由)

```csharp
// 全局单例 Autoload
public partial class EventBus : Node
{
    public void Publish<T>(T evt) where T : GameEvent;
    public void Subscribe<T>(Action<T> handler) where T : GameEvent;
    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent;
}
```

所有层间通信通过 EventBus 解耦。事件类型定义在 `Shared/EventBus/Events/` 下按域分文件。

### 5.2 Data Registry (数据驱动配置)

- 配置文件路径：`assets/data/{domain}/{table}.yaml`
- 启动时由 `YamlDataLoader` 加载到内存
- 各系统通过 `IDataTable<T>` 接口查询
- 运行时只读，不可修改

### 5.3 State Machine (通用 FSM)

复用于：
- NPC 态度状态机 (Foundation/NpcState)
- 误会 6 状态机 (Feature/Misunderstanding)
- 战斗阶段管理 (Core/Combat)
- 动态音乐状态 (Presentation/Audio)

### 5.4 Save System (存档契约)

```csharp
public interface ISaveable
{
    string SaveKey { get; }
    SaveSnapshot Serialize();
    void Deserialize(SaveSnapshot snapshot, int version);
}
```

- 加密：AES-256-CBC
- 版本迁移：链式 `IMigration` 接口
- 自动存档触发点：场景切换、叙事节点完成、战斗结束

---

## 6. Key Data Flows

### DF-1: 战斗回合循环

```
玩家输入 (选招/Read)
  → Core/Combat.SubmitAction()
  → Core/MartialArts.CheckCounter() + Foundation/CharacterData.CalculateDamage()
  → Core/Combat.ResolveTurn()
  → EventBus: TurnResolvedEvent
    ├→ Presentation/CombatUi.AnimateAction()
    ├→ Feature/Epiphany.CheckTriggerConditions() [回合末]
    ├→ Core/EnemyAi.DecideIntent() [引用 F8/F9]
    └→ Presentation/Audio.SetCombatMusicState()
```

### DF-2: 对话/叙事分支

```
玩家选择 → Core/Dialogue.SelectOption()
  → ConditionEvaluator 查询 Mindset/NpcState/Items
  → Core/Narrative.AdvanceToNode()
  → EventBus: NarrativeNodeEvent
    ├→ Core/Mindset.ShiftMindset()
    ├→ Feature/Romance.TriggerResonance()
    ├→ Feature/Misunderstanding (触发/释怀检测)
    ├→ Feature/Jianghu (传闻生成)
    └→ Platform/Save (自动存档)
```

### DF-3: 自然日推进

```
Foundation/TimeSystem.AdvanceDay()
  → EventBus: DayAdvancedEvent
    ├→ Foundation/CharacterData (体力恢复)
    ├→ Foundation/NpcState (日程推进、态度衰减)
    ├→ Feature/Jianghu (传闻传播、暗号轮换)
    ├→ Feature/Romance (彗星距离衰减)
    ├→ Feature/Party (同伴旅程步进)
    ├→ Feature/Misunderstanding (时限检查)
    └→ Foundation/SceneManagement (时段光照更新)
```

### DF-4: 存档/读档

```
写入: Platform/Save 收集各模块 ISaveable.Serialize()
     → JSON 合并 → AES-256 加密 → 写入文件

读取: 文件 → 解密 → 版本检测 → MigrationChain
     → 分发到各模块 .Deserialize()
     → EventBus: SaveLoadedEvent
     → SceneManagement 恢复场景
```

### DF-5: NPC 状态传播

```
任意触发 → Foundation/NpcState.ShiftAttitude()
  → EventBus: NpcAttitudeChangedEvent
    ├→ Core/Dialogue (解锁/锁定分支)
    ├→ Feature/Romance (参数调整)
    ├→ Feature/Jianghu (传闻变化)
    ├→ Feature/Misunderstanding (阈值检测)
    └→ Presentation/BlurredUi (模糊度更新)
```

### DF-6: 心境演变

```
触发源 → Core/Mindset.ShiftMindset()
  → 区域判定 (9宫格)
  → EventBus: MindsetShiftedEvent
    ├→ Presentation/BlurredUi (色调联动)
    ├→ Core/Narrative (结局路径更新)
    ├→ Feature/Romance (共鸣/冲突)
    └→ Feature/Epiphany (前置条件刷新)
```

### DF-7: 场景切换

```
触发 → Foundation/SceneManagement.TransitionTo()
  → 异步加载 → EventBus: SceneTransitionEvent
    ├→ Foundation/TimeSystem (旅行耗时)
    ├→ Presentation/Audio (场景音乐切换)
    ├→ Feature/Jianghu (NPC 位置刷新)
    ├→ Presentation/Tutorial (首访检测)
    └→ Platform/Save (自动存档)
```

---

## 7. API Boundaries

### 7.1 Shared Contracts

```csharp
public interface ISaveable
{
    string SaveKey { get; }
    SaveSnapshot Serialize();
    void Deserialize(SaveSnapshot snapshot, int version);
}

public abstract record GameEvent(double Timestamp);

public interface IDataTable<T> where T : class
{
    T Get(string id);
    IReadOnlyList<T> GetAll();
    bool Has(string id);
}
```

### 7.2 Platform Layer Interfaces

| Interface | Methods |
|-----------|---------|
| `ISaveManager` | `SaveGame(slot)`, `LoadGame(slot)`, `ListSlots()`, `DeleteSlot(slot)`, `GetMetadata(slot)` |
| `ISettingsProvider` | `Get<T>(key)`, `Set<T>(key, val)`, `ResetToDefault()`, `ApplyInputRemap(action, key)` |
| `ISteamBridge` | `UnlockAchievement(id)`, `SetStat(key, val)`, `IsAchievementUnlocked(id)` |

### 7.3 Foundation Layer Interfaces

| Interface | Methods | Events |
|-----------|---------|--------|
| `ICharacterRegistry` | `GetAttribute(charId, attr)`, `ModifyAttribute(charId, attr, delta)`, `CalculateDamage(attacker, defender, move)` | `AttributeChangedEvent` |
| `INpcStateManager` | `GetAttitude(npcId, towardId)`, `ShiftAttitude(npcId, towardId, delta)`, `GetFsmState(npcId)`, `TransitionState(npcId, trigger)` | `NpcAttitudeChangedEvent` |
| `ISceneDirector` | `TransitionTo(sceneId, spawnPoint)`, `GetCurrentScene()`, `IsSceneUnlocked(sceneId)` | `SceneTransitionEvent` |
| `ITimeManager` | `GetCurrentDay()`, `GetTimeSlot()`, `AdvanceDay()`, `ConsumeStamina(amount)`, `GetStamina()` | `DayAdvancedEvent`, `StaminaDepletedEvent` |

### 7.4 Core Layer Interfaces

| Interface | Methods | Events |
|-----------|---------|--------|
| `ICombatSystem` | `StartBattle(config)`, `SubmitAction(actorId, action)`, `ResolveTurn()`, `GetBattleState()`, `EndBattle()` | `TurnResolvedEvent`, `BattleEndedEvent` |
| `IMartialArtsRegistry` | `GetMove(moveId)`, `GetEquippedMoves(charId)`, `EquipMove(charId, moveId, slot)`, `CheckCounter(moveA, moveB)` | — |
| `IEnemyAiEngine` | `DecideIntent(enemyId, battleState)`, `GetDisplayedIntent(enemyId)` | `IntentDeclaredEvent` |
| `IDialogueRunner` | `StartDialogue(dialogueId)`, `SelectOption(optionIdx)`, `GetCurrentNode()` | `DialogueCompletedEvent` |
| `IMindsetSystem` | `GetAxis(axis)`, `GetRegion()`, `ShiftMindset(axis, delta, source)` | `MindsetShiftedEvent` |
| `INarrativeDirector` | `GetCurrentChapter()`, `AdvanceToNode(nodeId)`, `IsNodeCompleted(nodeId)`, `GetEndingPath()` | `NarrativeNodeEvent` |
| `IInventory` | `AddItem(itemId, qty)`, `RemoveItem(itemId, qty)`, `HasItem(itemId, qty)`, `GetEquipment(charId)` | `ItemAcquiredEvent` |

### 7.5 Feature Layer Interfaces

| Interface | Methods | Events |
|-----------|---------|--------|
| `IRomanceSystem` | `GetIntimacy(npcId)`, `GetCometPhase(npcId)`, `TriggerResonance(npcId, type)` | `IntimacyChangedEvent` |
| `IJianghuLayer` | `GetActiveRumors(sceneId)`, `PropagateRumor(rumorId)`, `CheckPassphrase(phrase, npcId)` | `RumorSpreadEvent` |
| `IEpiphanySystem` | `CheckTriggerConditions(charId)`, `PresentChoice(charId, options)`, `ApplyBreakthrough(charId, choice)` | `EpiphanyTriggeredEvent` |
| `IMisunderstandingSystem` | `GetState(misId)`, `GetTransparency(misId)`, `TriggerMisunderstanding(config)`, `AttemptResolve(misId, action)` | `MisunderstandingStateChangedEvent` |
| `IExplorationSystem` | `GetCluesInScene(sceneId)`, `DiscoverClue(clueId)`, `GetInsightLevel()` | `ClueDiscoveredEvent` |
| `IPartyManager` | `GetActiveParty()`, `SwapMember(inId, outId)`, `GetCompanionJourney(npcId)`, `ApplyCatchUp(npcId)` | `PartyChangedEvent` |

### 7.6 Presentation Layer Interfaces

| Interface | Methods |
|-----------|---------|
| `ICombatUiController` | `ShowHud(battleState)`, `AnimateAction(result)`, `ShowIntentIndicator(intent)`, `HideHud()` |
| `IBlurredUiController` | `SetBlurLevel(target, amount)`, `SetInfoVisibility(element, transparency)`, `TransitionTone(region)` |
| `ICutscenePlayer` | `PlayCutscene(cutsceneId)`, `SkipCutscene()`, `IsCutscenePlaying()` |
| `IAudioDirector` | `PlayBgm(trackId, crossfade)`, `PlaySfx(sfxId)`, `SetCombatMusicState(state)`, `SetAmbienceForScene(sceneId)` |
| `ITutorialSystem` | `TryTrigger(triggerId)`, `CompleteTutorial(tutorialId)`, `IsTutorialCompleted(tutorialId)` |

---

## 8. Dependency Injection Strategy

### Godot Autoload Singletons

生命周期等于应用生命周期的全局服务：

| Autoload | Interface | 理由 |
|----------|-----------|------|
| `EventBusAutoload` | `IEventBus` | 全局事件路由 |
| `SaveManagerAutoload` | `ISaveManager` | 任何时刻可存档 |
| `TimeManagerAutoload` | `ITimeManager` | 日历全局推进 |
| `SceneDirectorAutoload` | `ISceneDirector` | 场景切换不随场景销毁 |
| `AudioDirectorAutoload` | `IAudioDirector` | 音乐跨场景播放 |
| `SettingsAutoload` | `ISettingsProvider` | 设置全局可读 |

### Scene-Scoped Services

绑定到特定场景生命周期：

| 场景类型 | 服务 | 理由 |
|---------|------|------|
| 战斗场景 | `ICombatSystem`, `IEnemyAiEngine`, `ICombatUiController` | 战斗结束即销毁 |
| 对话场景 | `IDialogueRunner` | 对话结束即销毁 |
| 演出场景 | `ICutscenePlayer` | 播放完即销毁 |
| 世界场景 | `IExplorationSystem` | 场景特定线索 |

### Service Locator Pattern

```csharp
// 用于 Autoload 间互相引用
public static class Services
{
    public static IEventBus EventBus => GetNode<EventBusAutoload>("/root/EventBus");
    public static ISaveManager Save => GetNode<SaveManagerAutoload>("/root/SaveManager");
    // ...
}
```

---

## 9. Data Architecture

### 9.1 Configuration Data (只读)

```
assets/data/
├── characters/         # 角色属性基础值、成长曲线
├── martial-arts/       # 招式数据、克制矩阵
├── enemies/            # 敌人模板、AI 行为表
├── items/              # 物品数据、效果定义
├── dialogues/          # 对话节点图 (YAML)
├── narrative/          # 章节结构、叙事节点
├── scenes/             # 场景元数据、传送点
├── rumors/             # 传闻模板、传播规则
├── tutorials/          # 教学触发条件
└── audio/              # 音乐映射表、动态规则
```

格式：YAML（人类可读、Git 友好）
加载：启动时一次性加载到 `DataRegistry`

### 9.2 Runtime State (可变)

各系统内部维护运行时状态，通过 `ISaveable` 接口暴露序列化能力。

### 9.3 Save File Schema

```json
{
  "version": 3,
  "timestamp": "2026-06-08T12:00:00Z",
  "slot": 1,
  "playtime_seconds": 3600,
  "data": {
    "character_data": { ... },
    "npc_state": { ... },
    "time_system": { ... },
    "mindset": { ... },
    "narrative": { ... },
    "romance": { ... },
    "jianghu": { ... },
    "misunderstanding": { ... },
    "party": { ... },
    "items": { ... },
    "exploration": { ... },
    "epiphany": { ... },
    "tutorial": { ... },
    "achievement": { ... }
  }
}
```

---

## 10. Performance Considerations

| 关注点 | 策略 |
|--------|------|
| 场景加载 | 异步 `ResourceLoader` + 加载屏遮罩 |
| 事件风暴 | DayAdvancedEvent 订阅者使用延迟队列，分帧处理 |
| 数据查询 | DataRegistry 启动时建索引，O(1) 查询 |
| 存档写入 | 后台线程序列化+加密，不阻塞主线程 |
| NPC 状态更新 | 仅更新当前场景可见 NPC，远程 NPC 惰性计算 |
| UI 刷新 | 脏标记模式，仅数据变化时重绘 |

---

## 11. Testing Strategy

| 层 | 测试类型 | 框架 | 覆盖重点 |
|---|---------|------|---------|
| Platform | 单元测试 | GdUnit4 | 加密/解密、版本迁移、槽位 CRUD |
| Foundation | 单元测试 | GdUnit4 | 公式计算 (F1-F9)、FSM 转换、体力耗回 |
| Core | 单元+集成 | GdUnit4 | 战斗判定、克制计算、条件求值、心境位移 |
| Feature | 集成测试 | GdUnit4 | 彗星模型、误会状态链、传闻传播 |
| Presentation | 手动测试 | — | UI 不强制自动化，手柄导航人工验证 |
| 跨层 | 冒烟测试 | GdUnit4 | 完整战斗→存档→读档→验证状态 |

---

## 12. ADR Roadmap (缺失决策清单)

以下架构决策需在实现前通过 `/architecture-decision` 创建 ADR：

| ADR ID | 标题 | 优先级 | 涉及模块 | 风险 |
|--------|------|--------|---------|------|
| ADR-001 | EventBus 实现方案：Godot Signals vs C# Events vs 自定义 | **P0** | Shared/EventBus | 全局影响 |
| ADR-002 | UI 框架选型：Godot Control vs 第三方 (dual-focus 适配) | **P0** | Presentation/* | HIGH RISK |
| ADR-003 | 数据配置格式：YAML vs JSON vs Godot Resource | P1 | Shared/DataRegistry | 工具链影响 |
| ADR-004 | 存档加密方案：AES-256 实现 (C# vs GDExtension) | P1 | Platform/Save | 安全性 |
| ADR-005 | 对话系统格式：自研节点图 vs Ink vs Yarn Spinner | P1 | Core/Dialogue | 叙事工具链 |
| ADR-006 | 场景加载策略：PackedScene vs 动态实例化 | P1 | Foundation/SceneManagement | 性能 |
| ADR-007 | 输入系统适配：SDL3 rebinding 方案 | P1 | Platform/Settings | MEDIUM RISK |
| ADR-008 | FSM 实现方案：泛型 FSM vs Godot StateMachine node | P2 | Shared/StateMachine | 复用性 |
| ADR-009 | 动态音乐方案：AudioStreamInteractive vs 自研状态机 | P2 | Presentation/Audio | 复杂度 |
| ADR-010 | TileMapLayer 使用模式：4.6 API 验证 | P2 | Foundation/SceneManagement | MEDIUM RISK |

> **执行顺序**：P0 必须在第一个 Sprint 开始前完成；P1 在对应模块实现前完成；P2 可延迟到具体开发时。

---

## 13. Implementation Priority (Sprint 建议)

基于 MVP 优先级和依赖关系，建议实现顺序：

### Sprint 1: 基础骨架
- Shared/EventBus + DataRegistry + StateMachine
- Platform/Save (基础存档读写)
- Foundation/CharacterData (属性模型 + F1-F3 公式)

### Sprint 2: 战斗核心
- Core/Combat (Burst+Read 基础流程)
- Core/MartialArts (招式数据 + 克制)
- Foundation/TimeSystem (日历基础)

### Sprint 3: 战斗完善
- Core/EnemyAi (意图系统)
- Presentation/CombatUi (基础 HUD)
- Presentation/Audio (战斗音乐状态机)

### Sprint 4: 叙事基础
- Core/Dialogue (对话运行器)
- Core/Mindset (双轴 + 区域)
- Foundation/NpcState (态度 FSM)

### Sprint 5: 叙事+场景
- Core/Narrative (章节推进)
- Foundation/SceneManagement (异步加载)
- Core/Items (物品基础)

### Sprint 6+: Feature 层逐步展开
- Feature/Romance → Feature/Jianghu → Feature/Epiphany → ...

---

## 14. Conventions & Constraints

### 编码规范 (摘自 technical-preferences.md)

- Classes: PascalCase, 必须 `partial`
- Public: PascalCase / Private: `_camelCase`
- Signals: PascalCase + `EventHandler` 后缀
- Files: PascalCase 匹配类名
- 测试框架: GdUnit4

### 架构约束

1. **禁止反向依赖**：下层模块不得引用上层模块
2. **禁止跨层直接调用**：非相邻层通信必须经过中间层或 EventBus
3. **接口优先**：所有跨模块引用通过接口，不依赖具体实现
4. **数据只读**：DataRegistry 加载的配置数据运行时不可修改
5. **存档契约**：任何持久化状态的系统必须实现 `ISaveable`
6. **事件不可变**：`GameEvent` 为 record 类型，发布后不可修改

### 平台约束

- 目标：PC (Steam) + Steam Deck
- 渲染器：Compatibility (GL ES 3.0)
- 输入：键鼠 + 手柄全支持，禁止 hover-only 交互
- 语言：简体中文（主），预留本地化接口

---

## 15. Appendix: TR Coverage Summary

本架构覆盖 575+ 条技术需求（TR），分布如下：

| 层 | TR 数量 | 来源 GDD |
|---|--------|---------|
| Foundation | ~135 | #1 角色属性, #5 对话, #10 NPC 状态, #8 存档, #12 地图 |
| Core | ~146 | #2 战斗, #3 武学, #4 AI, #6 心境, #9 叙事, #11 日历 |
| Feature | ~154 | #13 感情, #16 江湖, #17 顿悟, #18 误会, #15 物品, #19 探索, #25 队伍 |
| Presentation+Polish | ~140 | #7 战斗UI, #14 朦胧UI, #20 CG, #21 音乐, #22 教学, #23 设置, #24 成就 |

> 完整 TR-ID 注册表将在 `/architecture-review` 执行时填充到 `docs/architecture/tr-registry.yaml`。

---

*Document generated by create-architecture skill. Next step: run `/architecture-decision` for P0 ADRs (ADR-001, ADR-002).*
