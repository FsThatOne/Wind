# 探索 / 洞察系统 GDD

> **System**: #19 探索 / 洞察
> **Status**: Designed
> **Priority**: Alpha
> **Depends On**: #12 地图/场景管理, #9 主线叙事, #1 角色属性, #5 对话系统
> **Author**: game-designer, level-designer
> **Created**: 2026-06-07

---

## Overview

探索/洞察系统管理玩家在场景中的**主动发现行为**和**被动感知能力**。它是对话系统（已定义对话内洞察机制）和战斗系统（已定义意图洞察概率）的**场景层补充**——让洞察属性在探索世界时同样有意义。

核心设计原则：
- **探索是叙事的延伸，不是刷格子**：每个可发现物都有叙事目的
- **洞察是角色能力的自然表达**：高洞察的角色能感知到低洞察角色错过的细节
- **错过不惩罚，发现有惊喜**：关键剧情线索不依赖高洞察（主线不需要高洞察门槛），洞察奖励的是**丰富度**而非**通关能力**

---

## Player Fantasy

**"我走进废弃的驿站，别人只看到灰尘和断壁，但我注意到墙角石砖的缝隙——有人刻意松动过。推开后是一封旧信，字迹模糊但署名是……"**

探索感的核心不是"捡物品"，而是"我比别人看得更深"。高洞察角色像福尔摩斯——同一个场景，他看到的世界比别人多一层。

---

## Detailed Design

### Core Rules

**1. 场景交互模式**

场景中的可交互对象分为三类：

| 类型 | 可见性 | 交互方式 | 示例 |
|------|--------|---------|------|
| **明示交互物** | 始终可见，有交互图标 | 走近后按交互键 | 门、NPC、告示牌、宝箱 |
| **环境细节** | 始终可见，无交互图标 | 走近后出现交互提示 | 书架、花瓶、墙画、桌上信件 |
| **洞察触发点** | 默认不可见 | 角色接近时，若 `insight >= threshold`，出现水墨晕染提示 → 玩家选择追查或忽略 | 松动的墙砖、异常的脚印、隐藏的刻痕 |

**2. 洞察触发点（Insight Node）**

场景中的隐藏信息节点，是本系统的核心数据单元。

```
InsightNode {
    id: string                  // 唯一标识
    scene_id: string            // 所在场景
    position: Vector2           // 场景内坐标
    detection_radius: float     // 感知半径（角色进入此范围触发检定）
    insight_threshold: int      // 洞察门槛（5-40，同 dialogue-system 标准）
    discovery_type: enum        // 发现类型（见下表）
    reward: DiscoveryReward     // 发现内容
    narrative_context: string   // 叙事描述（主角独白/旁白）
    prerequisite: Condition[]   // 前置条件（可选），格式复用 dialogue-system.md §7 条件系统
    one_time: bool              // 是否一次性（默认 true）
    discovered: bool            // 是否已发现（持久化）
}
```

> **Condition 格式引用**：`Condition[]` 复用 dialogue-system.md §7 定义的条件类型（`flag_check`, `chapter_check`, `item_check`, `insight_check`, `mindset_check`, `relationship_check`, `power_check`, `code_phrase_check`），支持 AND/OR 组合。典型用例：`[{ "type": "chapter_check", "chapter_id": "ch1", "operator": ">=" }]` 表示需到达第一章。

> **DiscoveryReward 数据结构**（tagged-union，按 `discovery_type` 分派）：
>
> ```
> DiscoveryReward = {
>     type: discovery_type,       // 与 InsightNode.discovery_type 一致
>     // —— 以下字段按 type 选择性填充 ——
>     // Clue
>     flag_id?: string,           // 设置的 quest_flag 标识
>     flag_value?: any,           // quest_flag 值
>     // Loot
>     item_id?: string,           // 物品标识
>     quantity?: int,             // 数量，默认 1
>     // MartialFragment
>     martial_id?: string,        // 武学残卷标识，触发武学获取流程
>     // CodePhrase
>     phrase_id?: string,         // 暗号标识，写入暗号簿
>     // SideQuestEntry
>     quest_node_id?: string,     // 解锁的支线节点标识
>     // EnvironmentDetail
>     // （无额外字段——仅触发 narrative_context 独白）
> }
> ```
>
> 所有类型均附带 `narrative_context` 独白（定义在 InsightNode 上）。

**3. 发现类型**

| 发现类型 | 描述 | 奖励形式 | 示例 |
|---------|------|---------|------|
| **Clue** | 线索/证据 | 触发 `quest_flag` + 主角独白 | 发现暗格被动过的痕迹 |
| **Loot** | 物品拾取 | `GrantItem(item_id, qty)` | 隐藏的草药、矿石、旧信 |
| **MartialFragment** | 武学残卷 | 触发武学获取流程 | 石壁上的残留招式刻痕 |
| **CodePhrase** | 暗号线索 | `code_phrase_learned(phrase_id)` | 客栈梁柱上的隐秘刻记 |
| **SideQuestEntry** | 支线入口 | 解锁支线节点 | 发现异常现场 → 触发调查支线 |
| **EnvironmentDetail** | 纯叙事细节 | 主角独白，无实际奖励 | 感受到此地曾发生过的事（氛围补充） |

**4. 洞察检定流程（场景内）**

```
当角色进入 InsightNode 的 detection_radius 时：
1. 检查 prerequisite — 不满足则跳过
2. 检查 discovered — 已发现则跳过
3. 检定：player.insight >= node.insight_threshold
4. 成功 → 播放水墨晕染视觉提示（同 dialogue-system InsightPrompt 风格）
5. 玩家选择"追查"→ 触发 narrative_context 独白 → 发放 reward → 标记 discovered = true
6. 玩家选择"忽略"→ 提示消失，下次接近时再次触发
7. 失败 → 无任何提示（角色完全不知道有东西在此）
```

**5. 奇遇与洞察的交叉**

地图/场景管理系统已定义了**隐藏触发点**和**奇遇事件**（map-scene-management.md #246, #312）。本系统与之的关系：

- **奇遇触发点**由 map-scene-management 管理位置和触发条件
- **洞察型奇遇**：部分奇遇的 `trigger_type = INSIGHT`，需通过洞察检定才能触发
- **非洞察型奇遇**：随机事件型，不依赖洞察属性（如路遇劫匪、偶遇高人）
- 本系统仅负责洞察型奇遇的检定逻辑，奇遇内容和结果由 map-scene-management 或主线叙事定义

**6. 探索不消耗体力**

已在 natural-day-stamina.md 中明确：场景内探索消耗 0 体力。探索是呼吸期的核心活动之一，不应受体力限制。

### States and Transitions

**洞察发现状态机**

```
UNDISCOVERED → DETECTED → INVESTIGATED
                ↓
              IGNORED (暂时) → DETECTED (再次接近)
```

| 状态 | 含义 |
|------|------|
| **UNDISCOVERED** | 默认状态，角色未曾满足检定条件 |
| **DETECTED** | 角色满足检定，水墨提示出现，等待玩家操作 |
| **IGNORED** | 玩家选择忽略，提示消失；离开 detection_radius 后状态回退为 UNDISCOVERED，下次可再触发 |
| **INVESTIGATED** | 玩家选择追查，奖励已发放，标记为永久完成 |

### Interactions with Other Systems

| 系统 | 方向 | 接口 |
|------|------|------|
| **角色属性** | 属性 → 探索 | 查询 `player.insight` 用于场景洞察检定 |
| **地图/场景管理** | 双向 | 场景加载时读取该场景的 InsightNode 列表；洞察型奇遇的检定委托本系统 |
| **对话系统** | 探索 → 对话 | 发现后触发 `InnerMonologue` 节点展示叙事描述；通过 `insight_discovered(insight_id)` 事件通知对话系统解锁洞察选择 |
| **物品/道具** | 探索 → 物品 | Loot 类型发现调用 `GrantItem(item_id, qty)` |
| **武学组合** | 探索 → 武学 | MartialFragment 类型发现触发武学残卷获取流程 |
| **主线叙事** | 双向 | Clue 类型发现推送 `quest_flag`；主线推进可解锁/锁定特定 InsightNode 的 prerequisite |
| **存档系统** | 双向 | 持久化所有 InsightNode 的 `discovered` 状态（即"洞察揭示历史"，save-system.md #89） |
| **朦胧化 UI** | 探索 → UI | 洞察提示视觉效果使用朦胧化 UI 的水墨晕染风格，场景上下文为 `EXPLORATION` |

---

## Formulas

### F1. 场景洞察检定

```
insight_triggered = player.insight >= node.insight_threshold
```

- `player.insight`: int, 范围 5-50（角色属性五维之一）
- `node.insight_threshold`: int, 范围 5-40（由内容设计时设定）
- 纯布尔判定，无概率成分，与 dialogue-system 洞察检定规则完全一致

### F2. 场景洞察密度建议

```
insight_nodes_per_scene ≤ scene_exploration_budget
```

| 场景规模 | exploration_budget | 说明 |
|---------|-------------------|------|
| 小型（客栈内/民居） | 0-2 | 大多数小场景无洞察节点 |
| 中型（城镇街区） | 2-4 | 关键场景可放 1-2 个 |
| 大型（区域地图） | 3-6 | 不超过 6 个，避免"扫地式探索" |

> 原则：宁少不滥。每个洞察节点必须有叙事目的（Clue/SideQuestEntry/CodePhrase），纯 Loot 节点应罕见。

### F3. 洞察门槛分级建议

| 门槛范围 | 难度 | 适用场景 |
|---------|------|---------|
| 5-10 | 低 | 大多数角色都能发现；用于氛围细节和非关键 Loot |
| 11-20 | 中 | 需要有意点洞察属性的角色；用于线索和暗号 |
| 21-30 | 高 | 需要专注洞察流派的角色；用于重要支线入口和稀有武学残卷 |
| 31-40 | 极高 | 仅最强洞察角色可触发；用于隐藏结局线索和传说级发现 |

---

## Edge Cases

| ID | 场景 | 处理 |
|----|------|------|
| E1 | 角色洞察提升后重访已失败的场景 | 重新进入 detection_radius 时重新检定，此次可能成功 |
| E2 | 场景内多个洞察节点同时在 detection_radius 内 | 按距离近→远依次触发，避免信息轰炸 |
| E3 | 洞察节点的 prerequisite 在场景内变化（如对话解锁） | 实时检查，场景内即时生效 |
| E4 | 玩家在洞察提示出现期间进入战斗/对话 | 提示暂时消失，事件结束后若仍在范围内则重新出现 |
| E5 | 一个 InsightNode 关联的支线已被其他途径完成 | 检查 quest_flag，若已完成则标记 discovered 并跳过 |
| E6 | 存档加载后场景中的洞察节点状态 | 从存档恢复 discovered 状态；DETECTED/IGNORED 为瞬态，加载后回退为 UNDISCOVERED |
| E7 | 主角洞察为 5（初始最低），所有 threshold=5 的节点是否太容易 | threshold=5 的节点定位为"所有人都应该发现"的氛围补充，不包含关键奖励 |

---

## Dependencies

| 依赖系统 | 依赖类型 | 说明 |
|---------|---------|------|
| **#12 地图/场景管理** | 硬依赖 | InsightNode 的位置管理和场景加载 |
| **#9 主线叙事** | 硬依赖 | Clue/SideQuestEntry 的 quest_flag 定义 |
| **#1 角色属性** | 硬依赖 | 洞察属性值查询 |
| **#5 对话系统** | 软依赖 | InnerMonologue 展示和 insight_discovered 事件 |
| **#15 物品/道具** | 软依赖 | Loot 类发现的 GrantItem |
| **#3 武学组合** | 软依赖 | MartialFragment 类发现的残卷获取 |
| **#8 存档系统** | 软依赖 | 洞察揭示历史持久化 |
| **#14 朦胧化 UI** | 软依赖 | 水墨晕染视觉风格 |

---

## Tuning Knobs

| 参数 | 默认值 | 范围 | 说明 |
|------|--------|------|------|
| `detection_radius_default` | 1.5 tiles | 0.5-3.0 | InsightNode 默认感知半径 |
| `insight_cue_fade_in` | 400ms | 200-800 | 水墨晕染提示淡入时长 |
| `insight_cue_linger` | 5s | 3-10 | 玩家不操作时提示保持时长，到期后自动 IGNORED |
| `multi_node_stagger` | 1.5s | 0.5-3.0 | 多节点同时在范围内时的依次触发间隔 |
| `max_nodes_per_scene` | 6 | 2-10 | 单场景 InsightNode 上限（内容设计约束，非运行时限制） |

---

## Visual/Audio Requirements

### 视觉需求

| 资产 | 规格 | 说明 |
|------|------|------|
| 洞察提示动画 | 水墨晕染效果，与对话系统 InsightPrompt 共用风格 | 场景内以光点/涟漪形式出现在触发点位置 |
| 追查交互 UI | 轻量级交互提示（类似"按键调查"） | 不打断探索流 |
| 发现演出 | 卷轴展开式动画（同 dialogue-system 洞察追查成功） | Clue/MartialFragment 用完整演出；Loot/EnvironmentDetail 用轻量提示 |

### 音效需求

| 触发点 | 描述 |
|--------|------|
| 洞察提示出现 | 微弱风铃/清脆水滴音（暗示"有什么在这里"） |
| 追查成功 | 卷轴展开音效（同对话系统） |
| 发现重要线索 | 独立 SE（区别于普通拾取） |

---

## UI Requirements

### 洞察提示（场景内）

- 水墨晕染效果出现在 InsightNode 的场景位置
- 玩家走近时显示交互键提示
- 不使用传统 HUD 标记（雷达点/感叹号），保持朦胧化风格

### 发现记录（菜单内）

- 不设独立的"收集品"界面
- Clue 类发现记录在主线叙事的证据链/线索簿中
- CodePhrase 类记录在暗号簿中
- MartialFragment 记录在武学界面
- Loot 直接进入背包
- EnvironmentDetail 不记录（纯一次性叙事体验）

---

## Acceptance Criteria

| AC | 描述 |
|----|------|
| AC1 | 角色 insight=15 进入包含 threshold=10 的 InsightNode 场景，走进 detection_radius 后出现水墨晕染提示，选择追查后获得奖励并标记为已发现 |
| AC2 | 角色 insight=8 进入同一场景，走过同一位置无任何提示 |
| AC3 | 角色升级洞察至 10 后重访该场景，此次可以发现 |
| AC4 | 同一场景有 3 个 InsightNode 在接近范围内，按距离依次触发，间隔 `multi_node_stagger` |
| AC5 | 发现 Clue 类节点后，主线叙事的对应 quest_flag 被正确设置 |
| AC6 | 发现 CodePhrase 类节点后，暗号簿中出现新暗号 |
| AC7 | 存档→加载后，已发现节点保持 INVESTIGATED 状态，不重复触发 |
| AC8 | 洞察提示出现期间进入战斗，战斗结束后返回场景，提示正确恢复 |
| AC9 | `insight_cue_linger` 超时后提示自动消失，玩家再次接近时重新出现 |

---

## Open Questions

| OQ | 问题 | 影响 |
|----|------|------|
| OQ1 | 是否需要"洞察等级"视觉反馈（如水墨晕染颜色/强度随门槛难度变化）？ | 视觉设计 |
| OQ2 | EnvironmentDetail 类发现是否应该有极少量心境微调（如发现悲惨遗迹 → 微调执念轴）？ | 与心境系统的交互深度 |
| OQ3 | 二周目是否解锁"洞察全开"模式（降低所有门槛为 0）以鼓励探索？ | 重玩性设计 |
