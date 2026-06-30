# Control Manifest

> **Engine**: Godot 4.7-stable (C# / .NET 8+)
> **Last Updated**: 2026-06-30
> **Manifest Version**: 2026-06-30
> **ADRs Covered**: ADR-0001 ~ ADR-0024 (22 Accepted, 1 Superseded, 1 Partial)
> **Status**: Active — regenerate with `/create-control-manifest update` when ADRs change

`Manifest Version` is the date this manifest was generated. Story files embed this date when created. `/story-readiness` compares a story's embedded version to this field to detect stories written against stale rules.

本清单是程序员快速查询手册，从所有 Accepted ADR、technical-preferences.md 与 engine-reference 文档中提取。**为什么** 这样规定请查阅来源 ADR；本清单只回答 **做什么 / 不做什么**。

---

## Platform Layer Rules

*适用范围: 引擎初始化、操作系统集成、加密、平台抽象、输入设备*

### Required Patterns
- **加密**: 必须使用 .NET 8 `System.Security.Cryptography` AES-256-CBC + PKCS7 — source: ADR-0004
- **加密 IV**: 每次加密必须随机生成 16 bytes IV 存于密文头部 — source: ADR-0004
- **完整性校验**: 必须附加 HMAC-SHA256（先加密后签名） — source: ADR-0004
- **存档文件头**: 必须含 magic `"FZHI"` + `schema_version` + `flags` — source: ADR-0004
- **存档异步**: `SaveGame` 必须在后台 Task 写入，不阻塞主线程 — source: ADR-0004
- **可存档接口**: 系统必须实现 `ISaveable`（SaveKey / Serialize / Deserialize） — source: ADR-0004
- **版本迁移**: 必须链式 v1→v2→v3，使用 `IMigration` + `MigrationChain`，不跳版本 — source: ADR-0004
- **迁移备份**: 迁移前必须备份原文件 (.bak)，迁移超时 500ms 自动中断 — source: ADR-0004
- **DEBUG 旁路**: DEBUG 构建必须额外写明文 `.dev.json`（用 `#if DEBUG`） — source: ADR-0004
- **自动存档**: 触发延迟 500ms；战斗/过场/锁定对话/存档进行中禁止触发 — source: ADR-0004
- **存档槽位**: 必须 10 手动 + 1 自动；失败重试 3 次（间隔 100ms） — source: ADR-0004
- **输入抽象**: v1.0 必须使用 Godot 内置 InputMap + 固定 Action 映射 — source: ADR-0007
- **输入查询**: 必须通过 `Input.IsActionPressed("action_name")` — source: ADR-0007
- **输入设备检测**: 必须复用 ADR-0002 `InputModeDetector` — source: ADR-0007
- **Remap 占位**: v1.0 `ApplyInputRemap` 必须为 no-op (`throw NotImplementedException`) — source: ADR-0007
- **决胜锁定**: 一击决胜期间必须通过 `CombatSystem.IsInCinematicLock()` 屏蔽暂停 — source: ADR-0007

### Forbidden Approaches
- **Never** 用 GDExtension C++ 加密库 — .NET 8 已足够快，不值得引入构建复杂度 — source: ADR-0004
- **Never** 引入 BouncyCastle — `System.Security.Cryptography` 已覆盖 AES-256 — source: ADR-0004
- **Never** 用 XOR + base64 简单混淆 — 安全性极低且无完整性校验 — source: ADR-0004
- **Never** 跳版本迁移 — source: ADR-0004
- **Never** 在 v1.0 实现运行时 rebinding — GDD 明确不支持 — source: ADR-0007

### Performance Guardrails
- **AES-256 加密**: 100KB ≈ 0.5ms（AES-NI 硬件加速） — source: ADR-0004
- **SaveGame**: 500KB 存档 < 50ms（含加密） — source: ADR-0004
- **LoadGame**: 500KB 存档 < 200ms（含解密+迁移） — source: ADR-0004
- **存档双缓冲**: 序列化峰值 ~1MB — source: ADR-0004

### Engine API Constraints
- **Post-Cutoff**: `FileAccess.store_*` 4.4+ 返回 bool，须处理返回值 — source: ADR-0004
- **Verification Required**: `FileAccess.StoreBuffer()` 在 C# 绑定中的返回类型 — source: ADR-0004
- **Verification Required**: Steam Deck 手柄在 SDL3 (4.5+) 下正确映射所有按键 — source: ADR-0007

---

## Foundation Layer Rules

*适用范围: 事件总线、场景加载、数据配置、状态机、TileMap、共享枚举*

### Required Patterns
- **跨层通知**: 必须走 `EventBus.Publish<T>()`，下层不得引用上层 — source: ADR-0001
- **同场景通信**: 父子/兄弟节点必须使用 Godot `[Signal]` delegate — source: ADR-0001
- **同层通信**: 同层模块内部必须使用直接方法调用 — source: ADR-0001
- **事件类型**: 所有事件必须为 record 类型继承自 `GameEvent`，不可变 — source: ADR-0001
- **订阅清理**: 订阅 Node 必须在 `_ExitTree()` 中调用 `ClearAllFor(this)` — source: ADR-0001
- **事件命名**: 必须为 `{Domain}{Action}Event`，单文件单类型 — source: ADR-0001
- **事件路径**: 必须放在 `src/FengZhi.Shared/EventBus/Events/{Layer}/` — source: ADR-0001
- **存档订阅优先级**: 存档系统订阅必须使用最低 priority 以最后处理 — source: ADR-0001
- **配置格式**: 所有静态配置必须使用 YAML 1.2，扩展名 `.yaml`，UTF-8 BOM-free — source: ADR-0003
- **YAML 解析**: 必须使用 YamlDotNet (NuGet) — source: ADR-0003
- **配置路径**: 必须为 `assets/data/{domain}/{table}.yaml` — source: ADR-0003
- **配置加载**: 启动时一次性加载到 `DataRegistry`，运行时只读 — source: ADR-0003
- **配置查询**: 必须通过 `IDataTable<T>` 接口（Get/GetAll/Has/Query） — source: ADR-0003
- **快速失败**: YAML 格式错误必须在启动时立即报告文件名+行号 — source: ADR-0003
- **场景加载策略**: 当前场景必须完全加载并实例化 — source: ADR-0006
- **场景预加载**: 相邻场景必须异步预加载为 `PackedScene`（不实例化） — source: ADR-0006
- **场景缓存**: 必须使用 LRU 缓存，最多 5 个 PackedScene — source: ADR-0006
- **场景过渡 FSM**: 必须遵循 4 状态机 Active→TransitionOut→Loading→TransitionIn→Active — source: ADR-0006
- **过渡锁输入**: TransitionOut/Loading 状态必须锁定输入 — source: ADR-0006
- **场景图数据**: 场景连通图必须由 `connections.yaml` 数据驱动 — source: ADR-0006
- **预加载重试**: 失败必须重试 3 次（间隔 500ms），全失败回退 + Toast — source: ADR-0006
- **场景容错**: 必须不崩溃且不丢失游戏状态 — source: ADR-0006
- **FSM 实现**: 必须使用泛型 C# 基类 `FiniteStateMachine<TState, TTrigger>` — source: ADR-0008
- **FSM 类型**: TState 和 TTrigger 必须为 enum（编译期约束） — source: ADR-0008
- **FSM 回调**: 必须支持 OnEnter/OnExit 回调和 StateChanged 事件 — source: ADR-0008
- **FSM 容错**: 无效 trigger 必须返回 false 且不改变状态 — source: ADR-0008
- **TileMapLayer 分层**: 每场景必须使用多 TileMapLayer，固定层级 Ground(0)/Terrain(1)/Structures(2)/Overlay(3)/Collision — source: ADR-0010
- **地图地块尺寸**: 所有地图场景必须使用丹房同规格完整等距菱形 tile `128×64`；TMX `tilewidth/tileheight`、Godot TileSet `tile_size`、`IsoProjection` 和运行时移动步长必须一致 — source: ADR-0022
- **地块碰撞形状**: 与地砖表层对齐的物理、占地和交互 `Area2D` 碰撞必须使用 `CollisionPolygon2D` 45 度菱形 `(-64,0),(0,-32),(64,0),(0,32)`，节点局部偏移统一为 `Vector2(22, 5)`；禁止用矩形模拟地砖碰撞 — source: ADR-0022
- **光照变体**: 必须通过 `CanvasModulate` + shader 色调偏移实现，不实例化多套 tileset — source: ADR-0010
- **物理碰撞层**: 必须为 Layer1 地形 / Layer2 交互 / Layer3 水域 — source: ADR-0010
- **场景模板**: 必须遵循 SceneRoot/TileMapLayer×5/CanvasModulate/SpawnPoints/NPCs/Interactables — source: ADR-0010
- **Tile 旋转**: Scene tile rotation 仅用于装饰，不用于碰撞相关 tiles — source: ADR-0010

### Forbidden Approaches
- **Never** 在单个 Autoload 上声明所有 [Signal] delegate — 75+ 信号集中违反单一职责且参数受 Variant 限制 — source: ADR-0001
- **Never** 使用 C# `static event` — 无生命周期管理，长时间 RPG 内存泄漏不可接受 — source: ADR-0001
- **Never** 引入 MediatR 等第三方 message bus — 违反最小依赖原则且不感知 Godot 生命周期 — source: ADR-0001
- **Never** 用 string-based 事件名连接，必须类型安全 — source: ADR-0001
- **Never** 使用 JSON 作为配置 — 不支持注释，配置表需注释解释字段 — source: ADR-0003
- **Never** 使用 Godot Resource (.tres/.res) 表格批量数据 — Git diff 体验差且 .tres 冗长 — source: ADR-0003
- **Never** 使用 TOML — 嵌套表达力不如 YAML，C# 生态不成熟 — source: ADR-0003
- **Never** 启动时全量预加载所有场景 — 启动时间和内存不可接受 — source: ADR-0006
- **Never** 纯按需加载无预加载 — 切换 100-500ms 延迟破坏沉浸感 — source: ADR-0006
- **Never** 用 Godot `AnimationTree`/`StateMachine` 节点做逻辑状态 — 服务于动画状态，不支持强类型 Trigger，难以单元测试 — source: ADR-0008
- **Never** 为每光照变体实例化独立 tileset — 4× 资产量（216 套）不可接受 — source: ADR-0010
- **Never** 新增 `64×32`、`96×48` 或其它非 `128×64` 的地图场景地块；视觉缩放必须通过相机、美术重制或贴图细节处理，不改变逻辑 tile 尺寸 — source: ADR-0022

### Performance Guardrails
- **EventBus**: Publish O(1) Dictionary 查找 + 线性遍历，~10 订阅者可忽略 — source: ADR-0001
- **EventBus**: 25 系统 × 3 事件 ≈ 75 entries，内存 < 10KB — source: ADR-0001
- **EventBus**: 1000 次 Publish < 1ms（10 订阅者） — source: ADR-0001
- **YAML 启动**: ~500KB 解析 < 100ms — source: ADR-0003
- **YAML 内存**: 反序列化后 ~2-5MB 常驻 — source: ADR-0003
- **DataRegistry**: `GetTable<T>().Get(id)` 必须 O(1) — source: ADR-0003
- **场景缓存**: 当前 + 5 PackedScene ≈ 50-100MB — source: ADR-0006
- **场景命中切换**: < 50ms — source: ADR-0006
- **场景未命中**: 300-1000ms（黑屏遮盖） — source: ADR-0006
- **场景加载**: 异步在后台线程，不阻塞主线程 — source: ADR-0006
- **TileMapLayer**: 5 层渲染 < 1ms/frame — source: ADR-0010
- **FSM**: 纯 C# 实现，无引擎开销 — source: ADR-0008

### Engine API Constraints
- **Verification Required**: `[Signal]` delegate 与 generic EventBus Autoload 的兼容性 — source: ADR-0001
- **Verification Required**: 场景卸载时事件订阅的 GC 行为 — source: ADR-0001
- **Verification Required**: C# 中 `ResourceLoader.LoadThreadedRequest`/`GetStatus` 绑定正确性 — source: ADR-0006
- **Post-Cutoff**: `TileMapLayer` (4.3 替代 TileMap)、scene tile rotation (4.6) — source: ADR-0010
- **Verification Required**: C# 中 `TileMapLayer` rotation 属性对 scene tiles 行为 — source: ADR-0010

---

## Core Layer Rules

*适用范围: 对话系统、演出/CG 系统、核心叙事 / 战斗（部分）*

### Required Patterns
- **对话格式**: 必须使用 YAML 图节点格式（自研 schema），与 ADR-0003 一致 — source: ADR-0005
- **对话头字段**: 必须含 `id` / `version` / `entry_node` / `nodes` — source: ADR-0005
- **对话节点类型**: 必须使用 7 种之一 — speech / choice / inner_monologue / narration / letter / insight_prompt / code_phrase — source: ADR-0005
- **对话条件**: 必须为三元组 `{source, op, value}`；AND 默认，OR 用 `conditions_any_of` — source: ADR-0005
- **对话路径**: 必须按章节分目录 `assets/data/dialogues/{chapter}/` — source: ADR-0005
- **死循环保护**: 单次对话最多 500 节点访问 — source: ADR-0005
- **对话 CI 校验**: 节点 id 唯一性、next 引用有效性、孤立节点检测 — source: ADR-0005
- **演出入口**: 必须使用 `CutsceneService` Autoload 作为全局入口 — source: ADR-0013
- **演出调用**: 必须通过 `PlayCutscene(id)` 或 `PlayCutsceneChain(ids)` — source: ADR-0013
- **演出 FSM**: 必须使用 6 状态 Idle→Loading→Playing→Skipping→CompletingEffects→Completed — source: ADR-0013
- **演出跳过**: 必须经 Skipping → CompletingEffects 路径，`on_complete` 必须完整执行 — source: ADR-0013
- **GameStateLock**: 必须使用三级 LockMode（None / Partial / Full） — source: ADR-0013
- **LockMode 共享**: enum 必须共享于 `Game.Foundation.GameState`，修改须同步 ADR-0014/0017/0018 — source: ADR-0013
- **FULL 锁定**: 期间必须禁止存档 / 系统 tick / 输入 — source: ADR-0013
- **资源预加载**: 必须 3.0s 超时降级为占位画面 — source: ADR-0013
- **PARALLEL 步骤**: 必须用 `Task.WhenAll` 并行 — source: ADR-0013
- **串联过渡**: 段间必须有 `transition_gap`（默认 500ms 黑屏） — source: ADR-0013
- **HUD 隐藏**: 必须通过 `HudVisibilityRequestEvent` 事件统一 — source: ADR-0013
- **慢动作**: SLOW_MOTION 步骤必须通过 `TimeScaleController.Request(priority:50)` — source: ADR-0013
- **跳过手感**: 长按 1.0s 触发；首次观看必须不可跳过（查 cutscene_viewed 记录） — source: ADR-0013
- **CanvasLayer 分配**: Cutscene 90+：底层 90 / 画面 91 / 文字 92 / 特效 93 / Skip UI 95 — source: ADR-0013
- **演出排队**: FIFO；Tier 4 微演出在队列前方有高 Tier 时必须 PruneStale 丢弃 — source: ADR-0013

### Forbidden Approaches
- **Never** 使用 Ink — C# 运行时需额外维护，条件系统与 Mindset/NPC/Item 查询不兼容 — source: ADR-0005
- **Never** 使用 Yarn Spinner — Godot 移植质量不确定，不支持图回环 — source: ADR-0005
- **Never** 用 `AnimationPlayer` Timeline 统一编排演出 — 不支持条件分支/WAIT_INPUT/动态资源加载 — source: ADR-0013
- **Never** 用纯事件驱动无状态机 — 跳过时无法安全中断中间步骤 — source: ADR-0013
- **Never** 用 InputMap 覆盖实现锁定 — 会连跳过本身也禁掉 — source: ADR-0013
- **Never** 同时播放两段 Tier 1-2 演出（互斥） — source: ADR-0013
- **Never** 在 FULL 锁定期间打开菜单（设计约束） — source: ADR-0013

### Performance Guardrails
- **对话解析**: 单文件 < 5ms — source: ADR-0005
- **对话内存**: 当前章节常驻 ~100KB — source: ADR-0005
- **CG 资源**: 单张 Tier 1 全屏插画 ~2MB — source: ADR-0013
- **演出递归**: 串联深度最多 2-3 段 — source: ADR-0013

### Engine API Constraints
- **Post-Cutoff**: `ResourceLoader.LoadThreadedRequest` (4.x async)、SceneTreeTween process mode — source: ADR-0013
- **Verification Required**: `ResourceLoader.LoadThreadedRequest` 在 FULL 锁定下行为 — source: ADR-0013
- **Verification Required**: CanvasLayer(100) 遮罩下方 UI 输入屏蔽效果 — source: ADR-0013
- **Verification Required**: `SceneTreeTween` 在 `Engine.TimeScale=0` 时 `SetProcessMode(ALWAYS)` 跳过恢复动画 — source: ADR-0013

---

## Feature Layer Rules

*适用范围: 活江湖层、感情系统、队伍管理、顿悟突破、探索/洞察*

### Required Patterns
- **活江湖架构**: 必须使用 `ConditionEvaluator` + `JianghuScheduler` + `EventRegistry` + `DeliveryQueue` 四组件 — source: ADR-0014
- **ConditionEvaluator 位置**: 必须位于 Foundation 层供跨系统复用 — source: ADR-0014
- **9 种 precondition**: flag/not_flag/chapter/chapter_range/day_elapsed_since/npc_state/season/in_breathing/player_region/mindset_zone — source: ADR-0014
- **每日 Tick**: 必须严格按 GDD 8 步流程 — source: ADR-0014
- **得分公式**: `priority + tagScore + min(BacklogDays × 5, 25)` — source: ADR-0014
- **每日上限**: `daily_event_cap=3`，呼吸期 ×2=6；同类型每日 ≤ 2 — source: ADR-0014
- **同日连锁禁止**: on_trigger 产生的新事件必须下一日才生效 — source: ADR-0014
- **on_trigger 限制**: 单事件最多 3 个 npc_state_change；attitude 偏移 [-2, +2] — source: ADR-0014
- **传闻延迟**: `delay = base_delay(1) + region_distance × distance_factor(1.0)` — source: ADR-0014
- **章节切换**: 必须 `CleanupOnChapterChange` 清理过期 pending 事件 — source: ADR-0014
- **存档隔离**: 只持久化运行时状态，不持久化 Script 定义 — source: ADR-0014
- **Tick 锁查询**: 必须查询 `Services.GameStateLock.IsSystemTickLocked`，FULL 时跳过 — source: ADR-0014
- **Flag 前缀强制**: 所有 `SetFlag(key)` 必须以注册前缀开头，未带前缀 Debug 触发 assert — source: ADR-0014
- **Flag 前缀分配**: jianghu_/narrative_/romance_/epiphany_/insight_/tutorial_/combat_/party_/mindset_/system_ — source: ADR-0014
- **Flag 不可变**: 一旦命名进入存档不可重命名；写仅限拥有者 ADR；任意系统可读 — source: ADR-0014
- **感情数据归属**: 数据必须寄存于 NPC State 系统，Romance Service 仅拥有规则 — source: ADR-0015
- **感情架构**: `RomanceService` + `MilestoneRegistry` + `CometPresenceTracker` + `EndingResolver` — source: ADR-0015
- **态度拦截器**: 所有变更必须经 `OnAttitudeChangeRequest` 拦截器应用地板钳位 — source: ADR-0015
- **地板表**: M_BREAK=-4 / M_BOND=3 / M_HEART=2 / M_CRISIS=2 / M_TRUST=1 / M_ACQUAINTED=0 / 无里程碑=-4 — source: ADR-0015
- **里程碑顺序**: Acquainted→Trust→Crisis→Heart→Bond，不可跳级 — source: ADR-0015
- **解锁门槛**: Trust≥1 / Crisis≥1 / Heart≥2 / Bond≥2 — source: ADR-0015
- **结缘互斥**: `bonded_heroine` 全局唯一 — source: ADR-0015
- **结缘流程**: 必须三步 TryBond → AwaitingChoice → ConfirmBond — source: ADR-0015
- **force_break**: 必须无视地板，直接 M_BREAK + attitude=-4 — source: ADR-0015
- **拒绝结缘**: 必须设 `romance_bond_declined_{npcId}` flag 防重触发 — source: ADR-0015
- **传闻概率**: `base(0.3) + same_region(+0.4) + absence>7天(+0.2)`，Clamp(0, 0.8) — source: ADR-0015
- **结局选取**: moralityTier ≤ -30 走魔道；否则按心境 zone × {companion/farewell/solo} × narrator_tone — source: ADR-0015
- **Romance Flag 前缀**: 必须为 `romance_` — source: ADR-0015
- **周目隔离**: 感情数据不跨周目继承 — source: ADR-0015
- **审计日志**: 被地板吞掉的 delta 必须日志记录 — source: ADR-0015
- **统一角色结构**: 主角与同伴必须使用 `PlayableCharacter` Resource [GlobalClass] — source: ADR-0016
- **五维属性**: Strength/InnerForce/Agility/Insight/Constitution — source: ADR-0016
- **角色槽位**: 装备 5 / 武学 6 / 内功 1 / 轻功 1 — source: ADR-0016
- **PartyMemberState**: 必须为 8 状态枚举 NotRecruited/Available/Deployed/AwayTraining/OnDelegate/Locked/Injured/Departed — source: ADR-0016
- **上阵上限**: `MaxDeployed=5` — source: ADR-0016
- **主角约束**: 主角不可下阵 — source: ADR-0016
- **部署锁定**: 必须用 `DeploymentLock` 栈，支持嵌套 — source: ADR-0016
- **6 种成长来源**: ChapterBaseline/BattleInsight/Observation/Epiphany/PersonalJourney/Catchup — source: ADR-0016
- **观战率**: `ObservationGrowthRate=0.35` — source: ADR-0016
- **末尾追赶**: `target = max(chapter_baseline, party_average × 0.85)` — source: ADR-0016
- **追赶上限**: `CatchupChapterCap=8` — source: ADR-0016
- **代办上限**: `DelegateGrowthCapPerChapter=4`，必须计入追赶 cap — source: ADR-0016
- **装备唯一**: 实例必须唯一绑定一个角色（`EquipmentRegistry.instanceToOwner`） — source: ADR-0016
- **章节切换**: 必须重置 CatchupUsedThisChapter / DelegateGrowthThisChapter 并 SettleChapterBaseline — source: ADR-0016
- **代办通知**: 活江湖必须通过 `DelegateCompletedEvent` 通知，Party 仅做结算 — source: ADR-0016
- **顿悟解耦**: 通过 EventBus 通知，Party 不干预奖励池选取 — source: ADR-0016
- **Party Flag 前缀**: 必须为 `party_` — source: ADR-0016
- **顿悟架构**: `EpiphanyRegistry` + `Evaluator` + `FocusingController` + `ChapterCapTracker` + `RewardDispatcher` + `SkipHandler` — source: ADR-0017
- **顿悟 8 状态 FSM**: Locked/Available/Triggered/Choosing/Focusing/Completed/Skipped/Failed — source: ADR-0017
- **三种触发路径**: Combat/Narrative/Meditation 共享同一事件表与状态机 — source: ADR-0017
- **战斗概率公式**: `max(0.30 × (1 - SkipCount × 0.15), 0) + (1 - hpRatio) × 0.40`，Clamp(0, 0.70) — source: ADR-0017
- **单战触发**: 单场战斗最多触发一次（`_triggeredThisBattle`） — source: ADR-0017
- **章节配额**: 序章=1 / 第1章=3 / 第2章=4 / 第3章=5 / 终章=5 — source: ADR-0017
- **叙事顿悟**: 不受章节配额限制；不扣 ConsumeSlot — source: ADR-0017
- **冥想顿悟**: 必须 100% 触发；`MeditationEpiphanyCap=4`（跨章累计） — source: ADR-0017
- **凝神时长**: 必须持续 `EpiphanyFocusTurns=3` 自然行动轮 — source: ADR-0017
- **凝神死亡**: `death_protection=false` → Failed（不递增 SkipCount，不消耗章节配额） — source: ADR-0017
- **悟后 buff**: 必须 `ApplyPostEpiphanyBuff(×1.10 全属性，本战剩余)` + `RefreshAllCooldowns` — source: ADR-0017
- **跳过上限**: `SkipLimit=3`；超过 → Skipped 终态永不再出现 — source: ADR-0017
- **跳过补偿**: 必须发放 skip_reward (HP/Qi 恢复 + 清破绽 + 下次必爆 + 减伤) — source: ADR-0017
- **境界突破**: 必须用 `PlayCutsceneChain([顿悟, 突破], transitionGapMs=500)` 串联 — source: ADR-0017
- **多事件优先级**: 必须用 `GetHighestPriority` 选取 — source: ADR-0017
- **顿悟条件**: preconditions 必须复用 ADR-0014 ConditionEvaluator — source: ADR-0017
- **顿悟 Flag**: 必须设 `epiphany_{cfgId}_done` flag — source: ADR-0017
- **概率隐藏**: 计算完全在服务端，UI 不暴露概率 — source: ADR-0017
- **洞察架构**: `InsightNode` Resource + `InsightNodeRegistry` + `ProximityDetector` + `DiscoveryDispatcher` — source: ADR-0018
- **InsightNode 字段**: SceneId/Position/DetectionRadius/InsightThreshold/DiscoveryType/Reward/Prerequisite/OneTime — source: ADR-0018
- **DiscoveryType 6 种**: Clue/Loot/MartialFragment/CodePhrase/SideQuestEntry/EnvironmentDetail — source: ADR-0018
- **洞察 4 状态**: Undiscovered → Detected → (Ignored) → Investigated — source: ADR-0018
- **距离查询**: 必须用 `ProximityDetector._PhysicsProcess`，不为每节点创 Area2D — source: ADR-0018
- **多节点 stagger**: 范围内多个节点必须按距离近→远 stagger 1.5s 触发 — source: ADR-0018
- **检定**: 纯布尔 `playerInsight ≥ InsightThreshold`；失败完全无提示 — source: ADR-0018
- **Cue Linger**: 5s 后自动转 Ignored — source: ADR-0018
- **离开范围**: Detected/Ignored 必须回退为 Undiscovered — source: ADR-0018
- **prerequisite 复用**: 必须复用 ADR-0014 ConditionEvaluator 实时评估 — source: ADR-0018
- **锁定暂停**: LockMode ≥ Partial 期间必须暂停 ProximityDetector 并隐藏所有 cues — source: ADR-0018
- **场景钩子**: 必须在场景加载/卸载调用 OnSceneLoaded/OnSceneUnloaded — source: ADR-0018
- **OneTime**: 已 Investigated 的节点场景加载时跳过注册 — source: ADR-0018
- **存档隔离**: 仅持久化 Investigated 终态；DETECTED/IGNORED 瞬态自动重置 — source: ADR-0018
- **Investigate 输出**: 必须播放 NarrativeContext 独白（Inner Monologue 节点）+ 按 type 分派奖励 — source: ADR-0018
- **Insight Flag 前缀**: 必须为 `insight_` — source: ADR-0018
- **trigger_type 隔离**: 仅处理 `trigger_type=INSIGHT` 的奇遇 — source: ADR-0018

### Forbidden Approaches
- **Never** 用 Godot Timer 节点定时调度 — 200+ Timer 管理开销，与游戏日不同步，存档恢复困难 — source: ADR-0014
- **Never** 在活江湖私有内嵌 ConditionEvaluator — 探索/教学需复用，违反 DRY — source: ADR-0014
- **Never** 用纯事件驱动反应式调度 — day_elapsed_since 依赖时间推进，同日连锁难保障 — source: ADR-0014
- **Never** 运行时动态生成事件定义（YAML 静态） — source: ADR-0014
- **Never** 态度与里程碑合一为单一进度条 — 违反"态度可波动但里程碑不可逆"地板语义 — source: ADR-0015
- **Never** Romance Service 拥有数据 — 对话/误会等需查 NPC State，分散查询路径产生歧义 — source: ADR-0015
- **Never** 允许同时与多位女主结缘 — 16 种结局变体基于互斥假设设计 — source: ADR-0015
- **Never** 暴露任何数值给玩家（朦胧化） — source: ADR-0015
- **Never** 使用经验值 + 等级制 — 违反"成长不靠刷怪"且产生板凳掉级恐慌 — source: ADR-0016
- **Never** 分离主角与同伴数据结构 — GDD CR-1 强制统一 — source: ADR-0016
- **Never** 自动追赶无上限 — 消除上阵选择意义 — source: ADR-0016
- **Never** 代办成长独立 cap — 与观战/追赶分开会多路叠加超限 — source: ADR-0016
- **Never** 100% 确定触发顿悟 — 丧失稀有感与"绝境灵光"意外体验 — source: ADR-0017
- **Never** 凝神期间免死 — 消除风险-收益抉择 — source: ADR-0017
- **Never** 让玩家自选顿悟奖励 — 违反朦胧化原则 — source: ADR-0017
- **Never** 顿悟次数无上限 — 破坏成长节奏 — source: ADR-0017
- **Never** 用概率检定洞察 — GDD F-1 明确纯布尔判定 — source: ADR-0018
- **Never** HUD 雷达点标记可发现物 — 违反朦胧化原则 — source: ADR-0018
- **Never** 为每个 InsightNode 创独立 Area2D — 节点数量爆炸 — source: ADR-0018
- **Never** 不使用 stagger 让多节点同时弹出 — 信息轰炸 — source: ADR-0018
- **Never** 持久化 IGNORED 状态 — 玩家忽略后再次接近应可重新提示 — source: ADR-0018

### Performance Guardrails
- **JianghuScheduler**: 每日 Tick 扫描 200 条事件 ≤ 1ms — source: ADR-0014
- **ConditionEvaluator**: 单条件评估 ≤ 5μs（缓存查询，无 IO） — source: ADR-0014
- **活江湖存档**: 序列化 ≤ 5ms（运行时状态 ~2KB） — source: ADR-0014
- **ProximityDetector**: 每帧遍历活动节点；场景节点 ≤ 6 影响可忽略 — source: ADR-0018
- **洞察时序**: MultiNodeStaggerSec=1.5s；CueLingerSec=5s — source: ADR-0018

### Engine API Constraints
- **Verification Required**: 每日 Tick 在 200+ 条目下扫描性能 ≤ 1ms — source: ADR-0014
- **Verification Required**: Flag 系统跨存档读写隔离 — source: ADR-0014
- **Verification Required**: NPC State 系统可在对话进行中排队态度变更 — source: ADR-0015
- **Verification Required**: force_break 在任何里程碑组合下覆写正确 — source: ADR-0015
- **Verification Required**: GrowthSettlementEngine 与 Save System 序列化兼容 — source: ADR-0016
- **Verification Required**: CatchupCalculator 在极端 party_average 下不溢出 — source: ADR-0016
- **Verification Required**: 凝神状态与战斗系统暂停/恢复交互无死锁 — source: ADR-0017
- **Verification Required**: 境界突破串联演出时序不冲突 ADR-0013 CutsceneQueue — source: ADR-0017
- **Verification Required**: Area2D 触发器在场景切换时正确清理 — source: ADR-0018
- **Verification Required**: 多节点排队协程调度不与战斗/对话锁冲突 — source: ADR-0018

---

## Presentation Layer Rules

*适用范围: UI 框架、动态音乐、战斗 UI 动画、误会信号渲染*

### Required Patterns
- **UI 基础**: 所有 UI 必须基于 Godot Control 节点树 + 薄抽象层 — source: ADR-0002
- **UI 基类**: 所有 UI 面板必须继承 `BaseUiPanel` — source: ADR-0002
- **可交互焦点**: 所有可交互 Control 必须设 `focus_mode = FOCUS_ALL` — source: ADR-0002
- **焦点管理**: 必须通过 `FocusManager.PushFocus`/`PopFocus` 栈管理 — source: ADR-0002
- **UI 刷新**: 必须使用脏标记模式（_dirty + _Process），禁止每帧无条件刷新 — source: ADR-0002
- **Blur 输入**: BlurredUi shader 必须设 `mouse_filter = PASS` 不阻断输入 — source: ADR-0002
- **CanvasLayer 分配**: WorldUi 10 / Hud 20 / Menu 30 / Dialogue 40 / Tutorial 50 / Notification 60 — source: ADR-0002
- **焦点栈深度**: 最大 8，超出强制清栈 — source: ADR-0002
- **音乐架构**: 必须使用自研 C# 音频状态机 + Godot AudioServer 总线 — source: ADR-0009
- **音频总线层级**: Master → BGM/Ambient/SFX → 子总线 — source: ADR-0009
- **音乐 FSM**: 必须复用 ADR-0008 泛型 FSM (6 状态 Exploration/Combat/Cutscene/Dialogue/Menu/Silence) — source: ADR-0009
- **BGM Override**: 栈深度 3，溢出替换栈顶 — source: ADR-0009
- **Crossfade 曲线**: 必须等功率 fade_out=cos(t·π/2)、fade_in=sin(t·π/2) — source: ADR-0009
- **战斗段切换**: 必须等待小节线对齐（`bar_length_ms`） — source: ADR-0009
- **SFX 并发**: 8 路；同 SFX 50ms cooldown；同源最多 2 路 — source: ADR-0009
- **SFX 优先级**: P0/P1 不可淘汰，P2-P4 8 路满时按优先级淘汰 — source: ADR-0009
- **Motif 处理**: 女主 Motif 用独立 overlay player + BGM duck 至 30%（不入栈） — source: ADR-0009
- **战斗动画编排**: 必须用 `CombatAnimationDirector` 状态机 + 命令队列 — source: ADR-0011
- **TimeScale 控制**: 必须通过 `TimeScaleController` 优先级栈（暂停=100 > 演出=50 > 默认=0） — source: ADR-0011
- **Tween 防自锁**: 修改 `Engine.TimeScale` 的 Tween 必须 `SetProcessMode(Tween.TweenProcessMode.Always)` — source: ADR-0011
- **Camera 仲裁**: 必须通过 `CameraRequestBus` 优先级请求（Default=0/Decisive=50/Cutscene=80） — source: ADR-0011
- **伤害数字池**: 必须使用 `DamageNumberPool`（预分配 12，零 GC） — source: ADR-0011
- **池化焦点**: 池化 Control 节点必须设 `focus_mode=NONE` 且回收时 `ReleaseFocus()` — source: ADR-0011
- **Camera 演出**: 期间必须禁用 `position_smoothing_enabled`，归位时重启 — source: ADR-0011
- **动画工具选择**: 程序化参数动画用 SceneTreeTween；体系专属/循环动画用 AnimationPlayer — source: ADR-0011
- **决胜 7 Phase**: 预结算 → 慢入 → 推镜 → 招式 → 数字 → 慢出 → 归位 — source: ADR-0011
- **并行动画**: 必须用 `ParallelCommand` 包装，单队列槽 — source: ADR-0011
- **ICombatService 稳定**: 接口签名一旦 Accepted 不可在 Sprint 3 实现期内修改 — source: ADR-0011
- **战斗内部隔离**: `CombatAnimationDirector`/`TimeScaleController` 不暴露给 Core 以外模块 — source: ADR-0011
- **误会三通道**: 必须使用 `MisunderstandingSignalRouter` 三通道（Dialogue/Panel/Ambience） — source: ADR-0012
- **HINTED 处理**: 必须借用 Blurred UI CH-3 `pending_reveals` 延迟队列 — source: ADR-0012
- **PERCEIVED/URGENT**: 必须绕过 CH-3 延迟立即生效 — source: ADR-0012
- **DialogueChannel**: 零 UI 节点开销，仅查表切换 AddressMode/ToneVariant — source: ADR-0012
- **云雾 Shader**: 必须 `ShaderMaterial` 实现脉动；PERCEIVED 周期 2s、URGENT 1s — source: ADR-0012
- **URGENT 氛围**: 必须激活 CanvasModulate 微蓝冷调 0.92 + 低频 drone — source: ADR-0012
- **force_break 演出**: 必须发布 `CutsceneRequestEvent(immediate: true)` 绕过延迟 — source: ADR-0012
- **色调叠加**: 必须乘法 `final = mindset_tint × urgency_tint` — source: ADR-0012
- **手柄聚焦**: 关系面板手柄聚焦必须可显示"心有疑云" tooltip — source: ADR-0012
- **误会 SFX**: 4 种必须通过 `IAudioDirector.PlaySfx` 播放（hinted/urgency_tick/resolve/permanent） — source: ADR-0012

### Forbidden Approaches
- **Never** 引入第三方 UI 框架（GodotUIFramework/ImGui） — 像素 UI 不需要框架且增加 HIGH RISK 域不确定性 — source: ADR-0002
- **Never** 用 CanvasItem `draw_*` 自研 UI 系统 — 重新造轮子且失去 AccessKit 集成 — source: ADR-0002
- **Never** 在每个 UI 场景中分散写 dual-focus 适配 — 重复代码不可维护 — source: ADR-0002
- **Never** 使用 hover-only 交互 — source: ADR-0002
- **Never** 使用 `AudioStreamInteractive` — Godot 无此功能且无法提供小节线对齐切换精度 — source: ADR-0009
- **Never** 用 AnimationPlayer 统一所有战斗动画 — 70% 程序化参数驱动不适合 — source: ADR-0011
- **Never** 自研 Coroutine 动画系统 — SceneTreeTween 已支持 SetProcessMode(ALWAYS) — source: ADR-0011
- **Never** 在事件回调中直接 `new Tween` 无状态机编排 — 并发冲突无法管理，Camera 无仲裁 — source: ADR-0011
- **Never** 引入第三方动画库 — source: ADR-0011
- **Never** 创建专用"误会追踪" HUD 面板 — 违反 GDD #18 不显示误会日志 — source: ADR-0012
- **Never** 用 Toast 通知误会变化 — 过于直白破坏氛围感 — source: ADR-0012
- **Never** 暴露任何数值（倒计时/严重度/条件列表） — source: ADR-0012
- **Never** 用纯对话层无面板标记 — GDD 明确要求 PERCEIVED 显示标记 — source: ADR-0012
- **Never** 用高饱和色或警告图标做关系面板标记 — source: ADR-0012

### Performance Guardrails
- **InputModeDetector**: O(1)，可忽略 — source: ADR-0002
- **FocusStack**: 最大 8 层，可忽略 — source: ADR-0002
- **战斗 HUD**: 刷新不超过 1ms/frame — source: ADR-0002
- **音频资产**: 153 条管理 — source: ADR-0009
- **小节线**: 每首曲子需配置 `bar_length_ms` — source: ADR-0009
- **战斗命令队列**: Dequeue O(1)；Tween 每帧 1 lerp — source: ADR-0011
- **DamageNumberPool**: 预分配 12 Label ≈ 24KB 常驻 — source: ADR-0011
- **战斗零 GC**: alloc/free 必须为零 — source: ADR-0011
- **并发伤害数字**: 单回合最多 6 个 — source: ADR-0011
- **DialogueSignalChannel**: 查表 O(1) — source: ADR-0012
- **云雾 Shader**: 每帧 1 次 sin() 计算，可忽略 — source: ADR-0012
- **核心 NPC**: 最多 5 个同时有 CloudMarkerState — source: ADR-0012
- **云雾运行**: 仅在关系面板打开时运行 — source: ADR-0012

### Engine API Constraints
- **Post-Cutoff**: Dual-focus system (4.6) — source: ADR-0002
- **Post-Cutoff**: FoldableContainer (4.5)、Recursive Control disable (4.5)、AccessKit (4.5) — source: ADR-0002
- **Verification Required**: `grab_focus()` 不影响鼠标悬停高亮 — source: ADR-0002
- **Verification Required**: 键盘焦点和鼠标焦点可同时存在于不同 Control — source: ADR-0002
- **Verification Required**: 朦胧化 shader 在 dual-focus 下不干扰输入响应 — source: ADR-0002
- **Post-Cutoff**: Dual-focus、SceneTreeTween process_mode — source: ADR-0011
- **Verification Required**: `Engine.TimeScale=0.2` 时 SceneTreeTween `SetProcessMode(ALWAYS)` 是否忽略 TimeScale — source: ADR-0011
- **Verification Required**: Camera2D smoothing 在低 TimeScale 下平滑行为 — source: ADR-0011
- **Verification Required**: 对象池 Control 节点 reparent 时焦点不泄漏 — source: ADR-0011
- **Post-Cutoff**: Dual-focus、Recursive Control disable — source: ADR-0012
- **Verification Required**: ShaderMaterial uniform 动态更新在 Control 节点上的每帧性能 — source: ADR-0012
- **Verification Required**: CanvasModulate 与朦胧化 UI 色调偏移交互 — source: ADR-0012
- **Verification Required**: `AudioStreamPlayer` 低频循环在场景切换时正确释放 — source: ADR-0012

---

## Rendering & Animation Direction Rules

*适用范围: 场景渲染路线、光影氛围、角色资产双轨制、动画 Port、对话 Pipeline — source: ADR-0020, ADR-0021, ADR-0022, ADR-0023, ADR-0024*

### Required Patterns
- **渲染路线**: 纯 2D 武侠战棋（《大侠立志传》风格）；场景 = 手绘 tileset + 分层 2D 背景 — source: ADR-0020
- **场景构建**: `Node2D` + 多 `TileMapLayer`（沿用 ADR-0010 五层）+ `Background` 节点（Sprite2D，z-index 低于 Ground） — source: ADR-0020
- **色调氛围**: 仅 `CanvasModulate` 做昼夜/章节/心境整体偏色 — source: ADR-0020
- **VFX 工具集**: sprite sheet + GPUParticles2D + screen overlay + 纯 2D shader（水墨/纸纹） — source: ADR-0020
- **战棋可读性优先级**: 移动格>角色阵营>遮挡关系>招式 VFX>氛围，任何美术服从此顺序 — source: ADR-0020
- **角色双轨**: 探索/战斗用 Q 版 sprite(3-4 头身)；对话/演出用立绘(6.5-7.5 头身) — source: ADR-0020
- **双轨隔离**: 场景中不出现立绘比例角色；立绘镜头中不出现 Q 版角色 — source: ADR-0020
- **动画 Port**: 必须使用 `ICharacterAnimator` 单一接口 + 多 Adapter 模式 — source: ADR-0021
- **Port 位置**: `src/FengZhi.Foundation/Animation/ICharacterAnimator.cs` — source: ADR-0021
- **AnimState 粗分类**: Idle/Walk/Run/Attack/Cast/Block/Hurt/Stagger/Die/Victory/Defeat/Custom — source: ADR-0021
- **Facing**: `Left / Right` 2 向（探索 iso 场景由 ADR-0022 IIso4Animator 子接口扩展为 4 斜向） — source: ADR-0021
- **Iso 投影**: 等距菱形 tile `128×64`，4 斜方向 NE/SE/SW/NW — source: ADR-0022
- **Iso4 子接口**: `IIso4Animator : ICharacterAnimator` 新增 `SetIsoFacing(IsoDirection)` — source: ADR-0022
- **对话 Pipeline**: `.dlg` (作者层) → `dialogue_compiler.py` → `.yaml` (存储层) → `DialogueRuntime` (运行时) — source: ADR-0023
- **Provider 通用化**: `SceneConditionValueProvider` scene-agnostic；条件走 `mindset.*` / `flag.*` 通用域 — source: ADR-0023
- **.dlg 幂等**: 相同 .dlg 输入产出相同 yaml（字段顺序/转义稳定），直接入 git — source: ADR-0023
- **过渡期 tileset**: 使用 `jiangnan_riverside` 平面 tileset 占位 chapter_00 explore — source: ADR-0024

### Forbidden Approaches
- **Never** 使用伪 2.5D / HD-2D / 准 HD-2D 任何变体 — source: ADR-0020
- **Never** 使用 PointLight2D / DirectionalLight2D 作为常规氛围手段（特例需 PR 审批） — source: ADR-0020
- **Never** 使用多层视差背景 / ParallaxLayer 模拟空间纵深 — source: ADR-0020
- **Never** 建立复杂后处理 compositor 管线（不做景深/bloom/体积光） — source: ADR-0020
- **Never** 做 3D 场景建模、PBR 材质、法线贴图 — source: ADR-0020
- **Never** 业务代码直接持有 `AnimatedSprite2D` — 必须走 `ICharacterAnimator` Port — source: ADR-0021
- **Never** 在 Port 接口中传 `string animationName`（走 `PlayCustom` 专用入口） — source: ADR-0021
- **Never** 使用 jiangnan-iso-v1 资产（已删除） — source: ADR-0024
- **Never** 将过渡期平面 tileset 进 art-bible 或复用到战斗场景 — source: ADR-0024
- **Never** 在 `S8-tileset-iso-redesign` 拍板前启动 6 个依赖 story — source: ADR-0024

### Performance Guardrails
- **背景层**: 远景 1 张静态 Sprite2D，不做运行时视差 — source: ADR-0020
- **角色 sprite**: Q 版 sprite sheet ≤ 64 frames per direction — source: ADR-0020
- **AnimatedSprite2DAnimator**: `Play()` O(1) 映射 + 1 signal 连接 — source: ADR-0021

### Engine API Constraints
- **Post-Cutoff**: `TileMapLayer` scene tile rotation (4.6)，`CanvasModulate` + shader 交互 — source: ADR-0020
- **Verification Required**: `IIso4Animator` 4 方向 sprite sheet 帧切换在 60fps 无跳帧 — source: ADR-0022
- **Verification Required**: `dialogue_compiler.py` 输出与 YamlDotNet 反序列化兼容 — source: ADR-0023

---

## Global Rules (All Layers)

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase + `partial` | `public partial class PlayerController` |
| Public 字段/属性 | PascalCase | `MoveSpeed`, `JumpVelocity` |
| Private 字段 | `_camelCase` | `_currentHealth`, `_isGrounded` |
| Signals/Events | PascalCase + `EventHandler` 后缀 | `HealthChangedEventHandler` |
| 文件名 | PascalCase 匹配类名 | `PlayerController.cs` |
| 场景 / Prefab | PascalCase 匹配根节点 | `PlayerController.tscn` |
| 常量 | PascalCase | `MaxHealth`, `DefaultMoveSpeed` |

Source: `.claude/docs/technical-preferences.md`

### Performance Budgets

| Target | Value | Source |
|---|---|---|
| Target Framerate | 60 FPS (PC) / 30 FPS (Steam Deck minimum) | technical-preferences.md |
| Frame Budget | 16.67ms (PC) / 33.33ms (Steam Deck) | technical-preferences.md |
| Draw Calls | ≤ 200 per frame | technical-preferences.md |
| Memory Ceiling | 512 MB | technical-preferences.md |
| Concurrent UI Tweens | ≤ 8 同屏并发 | technical-preferences.md |
| Scene Load Time | ≤ 2s (异步预加载) | technical-preferences.md |

### Approved Libraries / Addons

- **YamlDotNet** (NuGet) — YAML 1.2 解析/序列化，配置数据加载 — source: ADR-0003 + technical-preferences.md

### Forbidden APIs (Godot 4.7-stable)

> Source: `docs/engine-reference/godot/deprecated-apis.md`

#### Nodes & Classes
- `TileMap` → 用 `TileMapLayer`（since 4.3）
- `VisibilityNotifier2D` → `VisibleOnScreenNotifier2D`（since 4.0）
- `VisibilityNotifier3D` → `VisibleOnScreenNotifier3D`（since 4.0）
- `YSort` → `Node2D.y_sort_enabled`（since 4.0）
- `Navigation2D` / `Navigation3D` → `NavigationServer2D` / `NavigationServer3D`
- `EditorSceneFormatImporterFBX` → `EditorSceneFormatImporterFBX2GLTF`（since 4.3）

#### Methods & Properties
- `yield()` → `await signal`（since 4.0）
- `connect("signal", obj, "method")` → `signal.connect(callable)`（since 4.0）
- `instance()` → `instantiate()`（since 4.0）
- `get_world()` → `get_world_3d()`（since 4.0）
- `OS.get_ticks_msec()` → `Time.get_ticks_msec()`（since 4.0）
- `duplicate()` 嵌套资源 → `duplicate_deep()`（since 4.5）
- `Skeleton3D.bone_pose_updated` → `skeleton_updated`（since 4.3）
- `AnimationPlayer.method_call_mode` → `AnimationMixer.callback_mode_method`（since 4.3）
- `AnimationPlayer.playback_active` → `AnimationMixer.active`（since 4.3）

#### Patterns
- String-based `connect()` → 必须类型安全 signal 连接
- `$NodePath` in `_process()` → `@onready var` 缓存引用
- 未类型化 Array/Dictionary → `Array[Type]`、typed variables
- `Texture2D` shader 参数 → `Texture` 基类
- 手动 post-process viewport chains → `Compositor` + `CompositorEffect`
- GodotPhysics3D 新项目 → Jolt Physics 3D（4.6 默认）

### Cross-Cutting Constraints

> 跨 ADR 共享的规范契约 (3 项)

1. **`LockMode` 共享枚举** — 定义于 ADR-0013 §GameStateLock；引用方 ADR-0014 / ADR-0017 / ADR-0018；命名空间 `Game.Foundation.GameState.LockMode`；修改须同步所有引用方
2. **`ICombatService` Facade** — 定义于 ADR-0011 §ICombatService 接口契约；调用方 ADR-0013 (Cutscene SuspendLogic) / ADR-0017 (Epiphany 状态查询)；接口签名 Accepted 后 Sprint 3 不可修改
3. **Flag Namespace Registry** — 定义于 ADR-0014 §Flag Namespace Registry；10 个前缀正式分配：`jianghu_` / `narrative_` / `romance_` / `epiphany_` / `insight_` / `tutorial_` / `combat_` / `party_` / `mindset_` / `system_`；强前缀强制；Flag 一旦命名进入存档不可重命名；写仅限拥有者 ADR

### Engine Specialists Routing

| File Type | Specialist |
|---|---|
| `.cs` | godot-csharp-specialist |
| `.gdshader` / VisualShader | godot-shader-specialist |
| Control 节点 / CanvasLayer | godot-specialist |
| `.tscn` / `.tres` | godot-specialist |
| `.csproj` / NuGet | godot-csharp-specialist |
| `.gdextension` / C++ | godot-gdextension-specialist |
| 通用架构 review | godot-specialist |

Source: `.claude/docs/technical-preferences.md`

### C# Godot Idiom 必读

- 所有 C# 类必须声明为 `partial`（Godot 源生成器在 build 时注入）
- Signal 使用 delegate：`[Signal] public delegate void HealthChangedEventHandler(int newValue);` 连接 `HealthChanged += OnHealthChanged;`
- Export 属性：`[Export] public float MoveSpeed { get; set; } = 5.0f;`
- .NET 版本：项目使用 .NET 8 SDK
- `[Obsolete]` 标注：Godot 4.5+ 在 C# bindings 上已添加，IDE 直接提示

Source: `docs/engine-reference/godot/VERSION.md`

---

## 维护协议

- **触发再生**: 当任何 ADR 进入 Accepted、被修订或被 Superseded 时，运行 `/create-control-manifest` 重新生成
- **Story 兼容性**: Story 文件 front-matter 嵌入 `manifest_version`；`/story-readiness` 比对此字段，stale 时阻止开发
- **变更通知**: 新增 Forbidden 条目时必须通知所有正在 in-flight 的 story 作者
- **来源完整性**: 任何无 ADR / preference / engine reference 来源的规则，禁止加入本清单
