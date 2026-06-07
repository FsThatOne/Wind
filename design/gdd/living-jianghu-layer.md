# 活江湖层 (Living Jianghu Layer)

> **Status**: Designed
> **Author**: user + agents
> **Last Updated**: 2026-06-06
> **Implements Pillar**: Pillar 1 (江湖是活的，不止围绕你而转), Pillar 4 (做减法)

## Overview

活江湖层是《风止》的世界动态调度系统 —— 它让江湖不只是玩家脚下的地图，而是一张持续运转的事件网络。

作为基础设施，它是一台**事件表 + 规则引擎**：按游戏日 tick 检查触发条件、调度延迟事件、决定哪些传闻到达玩家耳中、哪些世界变化静默发生。它不穷举所有组合，而是通过"条件规则 + 优先级 + 容量上限"的调度模式，用有限的配置产出丰富但可控的世界动态。

作为玩家体验，它让你在江湖行走时不断遇到**世界在你不在场时发生的事**：客栈里的说书人带来远方门派覆灭的消息、一封同伴飞书汇报代办结果、一个不知名旅人提及你上次出手的后果、某条暗号在你到达之前已被改写。玩家不会意识到"活江湖层"的存在，但会感受到"这个世界有它自己的节奏"。

核心职责：
1. **世界事件调度** — 按游戏日/时辰/章节/季节条件，激活、推进和结算世界事件
2. **传闻网络** — 生成、传播和过期传闻，作为玩家获取世界信息的主要被动渠道
3. **暗号流转** — 管理江湖暗号的传递、被识破和演变（与主线揭秘节奏同步）
4. **同伴代办** — 在玩家缺席时由 NPC 代为处理特定支线钩子，反馈结果
5. **呼吸期填充** — 在主线暂停推进时集中呈现活江湖内容，让呼吸期有事可做
6. **Content Overflow 执行** — 实现"未玩到的内容不消失，而是由世界自行演进"的设计哲学

## Player Fantasy

**"我走在江湖里，而江湖也在我身后走。"**

活江湖层服务三层叠加的情感体验：

**第一层：世界在我不在时也在动**

你从靖川出发前往姑苏，花了三天赶路。到达时，客栈说书人正在讲"昨夜沉渊阁少主在钱塘一剑败退三名游侠"的故事。你没有去钱塘，但钱塘发生了事。这不是随机生成的填充内容，而是有因有果的世界运转：你在第一章帮了那三名游侠，他们有了信心去挑战沉渊阁——然后败了。

这让玩家相信：**江湖不是为我准备的舞台，而是我碰巧走进的一条大河。**

**第二层：传闻引我去看看**

每次进入客栈、偶遇旅人、收到飞书，都可能带来一条与你当前处境相关的传闻。传闻不是任务提示——它没有箭头、没有"前往XX"的按钮——但它让你产生好奇：那个地方发生了什么？和我有关系吗？值不值得绕一天的路去看看？

这让"自由探索"不是漫无目的地闲逛，而是被**信息密度自然引导**的旅途。

**第三层：错过有后果，但后果不是惩罚**

你没有去救那个被围困的旧友。三天后，同伴飞书告诉你：他替你去了，旧友被救了，但代价是同伴受了伤，下次重逢时他还在疗养。如果你亲自去，结果可能不同。

这不是"错过就失败"的FOMO设计，而是"每条路都有它的故事"：你走的路有你的收获，你没走的路由江湖自行演完——但演法不同，这就是选择的重量。

**支柱对齐：**
- **Pillar 1**（江湖是活的）— 活江湖层直接实现此支柱，是其在系统层面的核心载体
- **Pillar 4**（做减法）— 不用穷举所有可能性来制造丰富感，而是用"少量高质量事件 + 延迟回响"来营造世界在运转的错觉。控制信息密度上限，防止变成"消息泛滥"

## Detailed Design

### Core Rules

**1. 世界事件表结构**

每个活江湖事件定义为一条配置记录：

```yaml
event:
  id: "rumor_chenyuan_defeats_wanderers"
  type: rumor | code | letter | delegation | world_event
  chapter_range: [1, 2]        # 仅在第1-2章有效
  preconditions:
    - flag: "helped_three_wanderers"
    - day_elapsed_since: { flag: "helped_three_wanderers", min: 3 }
    - chapter: 1
    - not_flag: "rumor_chenyuan_already_delivered"
  priority: 50                 # 0-100, 越高越优先被选中
  cooldown: 0                  # 触发后多少天内不重复
  delivery_method: storyteller | traveler | letter | companion_report | ambient
  content_key: "rumor_chenyuan_defeats_wanderers_text"
  on_trigger:
    - set_flag: "rumor_chenyuan_already_delivered"
    - npc_state_change: { npc: "three_wanderers", attitude: -1 }
  expire_day: null             # null=永不过期; 数字=到期自动结算
  breathing_only: false        # true=仅呼吸期激活
  tags: [jiangnan, chenyuan, consequence]
```

**2. 每日 Tick 循环**

每次 `day_advanced` 事件触发时，活江湖层执行以下流程：

```
1. 收集当前状态快照（章节、游戏日、季节、NPC 状态、flag 集合、呼吸期状态）
2. 扫描事件表，筛选所有 preconditions 满足的事件 → 候选池
3. 若当前在呼吸期且事件标记 breathing_only=true → 加入候选池
4. 按 priority 降序排列候选池
5. 取前 N 个事件（N = daily_event_cap，默认 3；呼吸期内 N = daily_event_cap × breathing_multiplier）
6. 对选中事件执行 on_trigger 副作用
7. 将事件推入「待呈现队列」（由呈现层在合适时机展示给玩家）
8. 检查过期事件（当前日 ≥ expire_day）→ 执行 on_expire 副作用并标记为 expired
```

**3. 条件系统**

Precondition 支持以下原语，可组合（AND 关系）：

| 条件类型 | 语法 | 说明 |
|---------|------|------|
| flag 存在 | `flag: "xxx"` | 全局 flag 已设置 |
| flag 不存在 | `not_flag: "xxx"` | 全局 flag 未设置 |
| 章节范围 | `chapter: N` 或 `chapter_range: [min, max]` | 当前章节匹配 |
| 天数经过 | `day_elapsed_since: { flag, min }` | 自某 flag 设置以来过了至少 min 天 |
| NPC 状态 | `npc_state: { npc, axis, value }` | 查询 NPC 的特定轴是否匹配 |
| 季节 | `season: spring/summer/autumn/winter` | 当前季节匹配 |
| 呼吸期 | `in_breathing: true/false` | 是否在呼吸期 |
| 区域 | `player_region: "jiangnan"` | 玩家当前所在大区 |
| 心境 | `mindset_zone: "xxx"` | 心境双轴当前区域 |

**4. 优先级与容量控制**

- **每日事件容量**：默认 `daily_event_cap = 3`
- **呼吸期倍率**：`breathing_multiplier = 2`（呼吸期内每日容量 = 6）
- **优先级机制**：候选事件按 priority 降序排列，同 priority 按 tag 相关性排序（与玩家当前区域/章节相关的 tag 加权）
- **防重复**：每个事件 id 只能触发一次（除非配置 `repeatable: true` + `cooldown`）
- **类型均衡**：同一天内同 type 的事件最多 2 个，防止某类事件垄断

**5. 五种事件类型的特化规则**

| 类型 | 呈现方式 | 特化规则 |
|------|---------|---------|
| **传闻 (rumor)** | 说书人/旅人/客栈闲谈 | 有传播延迟（事件发生后 1-5 天才到达玩家区域）；远距离传闻延迟更长 |
| **暗号 (code)** | NPC 对话中嵌入/环境暗号 | 与主线揭秘节奏绑定（`chapter_range` 严格控制）；被识破后触发后续暗号变更事件 |
| **飞书 (letter)** | 信件到达通知 | 使用 NPC 状态管理的飞书信使接口；到达延迟由发信人距离决定 |
| **同伴代办 (delegation)** | 同伴飞书汇报 | 仅当同伴处于"离队远行"状态且支线标记 `delegate_allowed` 时可触发；结果从预配置的 3-4 种中按条件选取 |
| **世界事件 (world_event)** | 多渠道（传闻+环境变化+NPC态度变化） | 可产生连锁事件（`on_trigger` 中注册新延迟事件）；影响范围大，通常改变多个 NPC 状态 |

**6. 传闻传播延迟**

传闻不是即时到达玩家的。每条传闻有一个「源发区域」，从源发到玩家当前区域的传播天数：

```
传播延迟 = base_delay + region_distance × distance_factor
```

- `base_delay`: 1 天（最低延迟）
- `region_distance`: 区域间的逻辑距离（0=同区, 1=相邻, 2=隔一区, 3=跨国）
- `distance_factor`: 1 天/距离单位

效果：同区传闻次日到达，跨区 2-3 天，跨国 4 天。

**7. 呼吸期填充策略**

呼吸期是活江湖层最活跃的时段：
- 容量加倍（3→6 事件/日）
- 标记 `breathing_only: true` 的事件仅在此时激活（通常是支线钩子、奇遇引导）
- 传闻"积压释放"：如果前几天有因容量限制未呈现的传闻，呼吸期开始时优先释放

### States and Transitions

每个事件实例的生命周期：

```
inactive → pending → triggered → delivered → expired/consumed
```

| 状态 | 含义 | 转移条件 |
|------|------|---------|
| inactive | 事件定义存在但条件未满足 | preconditions 全部满足 → pending |
| pending | 条件满足，在候选池中等待选中 | 被每日 tick 选中 → triggered |
| triggered | 已执行 on_trigger 副作用，进入待呈现队列 | 呈现给玩家 → delivered |
| delivered | 玩家已接收到此事件内容 | 终态（或到期 → expired） |
| expired | 未被选中就到了 expire_day | 执行 on_expire 副作用 → 终态 |

### Interactions with Other Systems

| 系统 | 方向 | 接口 |
|------|------|------|
| **自然日+体力** | ← 监听 | `day_advanced` 事件触发每日 tick；`season_changed` 更新季节条件 |
| **NPC 状态管理** | ↔ 双向 | 查询 NPC 状态用于 preconditions；`on_trigger` 中发送 `npc_state_change` |
| **主线叙事** | ← 查询 | 查询当前章节、呼吸期状态；不主动修改主线进度 |
| **对话系统** | → 推送 | 传闻/暗号内容推送到对话系统的"环境对话"池 |
| **感情系统** | → 推送 | 飞书/同伴代办结果可触发感情系统的关系变化 |
| **心境双轴** | ← 查询 | precondition 中可查询心境区域 |
| **地图/场景管理** | ← 查询 | 查询玩家当前区域用于 preconditions 和传闻延迟计算 |
| **误会系统 (下游)** | → 推送 | 世界事件和传闻可触发误会条件 |

## Formulas

### F-1: 传闻传播延迟 (rumor_propagation_delay)

| 变量 | 定义 | 取值范围 | 默认值 |
|------|------|---------|--------|
| base_delay | 最低传播天数 | ≥1 | 1 |
| region_distance | 源发区域到玩家区域的逻辑距离 | 0-3 | — |
| distance_factor | 每单位距离增加的天数 | >0 | 1.0 |

**表达式：**

```
propagation_delay = base_delay + region_distance × distance_factor
```

**输出范围：** [1, 4] 天

| 距离场景 | region_distance | 结果 |
|---------|----------------|------|
| 同区 | 0 | 1 天 |
| 相邻区域 | 1 | 2 天 |
| 隔一区 | 2 | 3 天 |
| 跨国 | 3 | 4 天 |

---

### F-2: 标签相关性得分 (tag_relevance_score)

当多个候选事件 priority 相同时，使用标签相关性作为次要排序键。

| 变量 | 定义 | 取值范围 | 默认值 |
|------|------|---------|--------|
| matching_region_tags | 事件 tags 中与玩家当前区域匹配的数量 | 0-N | — |
| matching_chapter_tags | 事件 tags 中与当前章节相关的数量 | 0-N | — |
| region_weight | 区域匹配权重 | — | 2 |
| chapter_weight | 章节匹配权重 | — | 1 |

**表达式：**

```
tag_relevance_score = matching_region_tags × region_weight + matching_chapter_tags × chapter_weight
```

**输出范围：** [0, 10]（软上限，由标签数量自然约束）

---

### F-3: 事件选择得分 (event_selection_score)

每日 Tick 中对候选池进行最终排序的综合得分。

| 变量 | 定义 | 取值范围 | 默认值 |
|------|------|---------|--------|
| priority | 事件配置中的静态优先级 | 0-100 | — |
| tag_relevance_score | F-2 计算结果 | 0-10 | — |
| backlog_bonus | 因容量限制未被选中的累积天数 × 5 | 0-25 | — |

**表达式：**

```
selection_score = priority + tag_relevance_score + backlog_bonus
```

**输出范围：** [0, 135]

**说明：**
- `backlog_bonus` 上限 = 5天 × 5分 = 25，防止低优先级事件无限累积后突然高权
- 事件首次进入 pending 时 backlog = 0；每过一天未被选中 +5，上限 25

---

### F-4: 有效每日容量 (effective_daily_cap)

| 变量 | 定义 | 取值范围 | 默认值 |
|------|------|---------|--------|
| daily_event_cap | 基础每日事件上限 | ≥1 | 3 |
| breathing_multiplier | 呼吸期倍率 | ≥1 | 2 |
| is_breathing | 当前是否在呼吸期 | 0/1 | — |

**表达式：**

```
effective_cap = daily_event_cap × (1 + (breathing_multiplier - 1) × is_breathing)
```

**输出范围：** [3, 6]

| 场景 | 结果 |
|------|------|
| 正常推进期 | 3 |
| 呼吸期 | 6 |

## Edge Cases

### E-1: 候选池为空

**场景：** 某日 tick 时没有任何事件的 preconditions 满足。
**处理：** 跳过该日调度，不强制填充。沉默日是合理的——并非每天都有江湖大事。

### E-2: 候选池小于 effective_cap

**场景：** 满足条件的事件数量 < 每日容量上限。
**处理：** 全部选中，不补齐。宁可少而精，不人为凑数。

### E-3: 同优先级 + 同标签相关性

**场景：** selection_score 完全相同的多个候选事件竞争最后一个席位。
**处理：** 稳定随机——使用 `event_id` 的哈希值作为最终 tiebreaker，确保同一天多次加载时结果一致。

### E-4: 传闻源发时玩家正在跨区旅行

**场景：** 传闻延迟计算时玩家处于移动状态（区域尚未确定）。
**处理：** 使用玩家出发区域作为计算基准。传闻到达时若玩家已到新区域，仍正常呈现（旅人带来的消息可以在路上追上你）。

### E-5: 呼吸期突然结束（主线推进）

**场景：** 玩家在呼吸期中触发主线推进，breathing 状态变为 false。
**处理：** 当日已选中但未呈现的事件保留在待呈现队列中（不丢弃）。次日容量恢复正常值(3)。标记 `breathing_only: true` 的 pending 事件回退至 inactive（条件不再满足）。

### E-6: 事件过期与触发同日竞争

**场景：** 某事件今天既满足 expire_day 又被选中。
**处理：** 触发优先于过期。如果事件被选入当日队列，正常执行 on_trigger；只有未被选中的过期事件才执行 on_expire。

### E-7: on_trigger 产生的连锁事件立即满足条件

**场景：** 事件 A 的 on_trigger 设置了某个 flag，恰好使事件 B 的 preconditions 满足。
**处理：** 事件 B 在下一日 tick 才进入候选池（同日 tick 已完成扫描）。无同日连锁，防止雪崩。

### E-8: 同伴代办但同伴已阵亡/离队

**场景：** delegation 事件触发时，目标同伴已不在"离队远行"状态。
**处理：** 事件不被选中（precondition `npc_state: companion_away` 不满足）。如果同伴状态在 tick 后才变化（如同日死亡），已触发的代办按"失败"路径结算。

### E-9: 单日类型均衡限制导致高优先级事件被跳过

**场景：** 3 个最高优先级事件均为 rumor 类型，但类型均衡规则限制同类最多 2 个。
**处理：** 第 3 个 rumor 被跳过，由下一个非 rumor 类型事件替补。被跳过的 rumor 获得 backlog_bonus 累积。

### E-10: 章节切换时 chapter_range 过期事件

**场景：** 玩家推进到第 3 章，大量 chapter_range=[1,2] 的事件变为无效。
**处理：** 章节切换时执行一次性清理：所有 chapter_range 不再覆盖当前章节的 pending 事件直接进入 expired（执行 on_expire），inactive 事件保持 inactive 不做处理。

## Dependencies

### 硬依赖（必须先实现）

| 系统 | 需要的接口/功能 | 状态 |
|------|----------------|------|
| **自然日+体力** | `day_advanced` 事件、`season_changed` 事件、`register_delayed_event()` | Designed ✓ |
| **NPC 状态管理** | 查询 NPC 轴值 (`get_npc_axis`)、飞书信使接口、同伴"离队远行"状态 | Designed ✓ |
| **主线叙事** | 查询当前章节 (`get_current_chapter`)、呼吸期状态 (`is_breathing`) | Designed ✓ |
| **Flag 系统** | 全局 flag 读写 (`has_flag`, `set_flag`) | 未独立 GDD — 内嵌于存档系统 |

### 软依赖（可并行开发，后期对接）

| 系统 | 需要的接口/功能 | 状态 |
|------|----------------|------|
| **对话系统** | 环境对话池推送接口 (`push_ambient_dialogue`) | Designed ✓ |
| **感情系统** | 关系变化触发 (`trigger_relationship_change`) | In Design |
| **心境双轴** | 查询心境区域 (`get_mindset_zone`) | Designed ✓ |
| **地图/场景管理** | 查询玩家区域 (`get_player_region`)、区域距离表 | Designed ✓ |
| **误会系统** | 触发误会条件 (`register_misunderstanding_trigger`) | Not Started |

### 实现顺序建议

1. Flag 系统接口确认（可在存档系统 GDD 中明确）
2. 活江湖层核心 tick 循环 + 条件引擎
3. 对接 day_advanced / NPC 状态 / 章节查询
4. 事件呈现层（对接对话系统、飞书信使）
5. 感情系统 / 误会系统对接（后期联调）

## Tuning Knobs

| 参数名 | 默认值 | 范围 | 影响 |
|--------|--------|------|------|
| daily_event_cap | 3 | 1-6 | 每日最大事件触发数。过低→世界沉寂；过高→信息过载 |
| breathing_multiplier | 2 | 1-4 | 呼吸期容量倍率。决定呼吸期节奏密度 |
| max_type_per_day | 2 | 1-4 | 同类型事件每日上限。防止传闻霸屏 |
| rumor_base_delay | 1 | 0-3 | 传闻最低传播延迟(天) |
| rumor_distance_factor | 1.0 | 0.5-2.0 | 每单位区域距离增加的传播天数 |
| backlog_bonus_per_day | 5 | 0-10 | 未被选中事件每日积压加分 |
| backlog_bonus_cap | 25 | 10-50 | 积压加分上限 |
| tag_region_weight | 2 | 1-5 | 区域 tag 匹配权重 |
| tag_chapter_weight | 1 | 1-3 | 章节 tag 匹配权重 |
| expire_grace_days | 0 | 0-3 | 过期日之后额外保留天数（0=精确过期） |
| breathing_backlog_release | true | bool | 呼吸期开始时是否批量释放积压事件 |
| chapter_transition_cleanup | true | bool | 章节切换时是否自动清理过期事件 |

**调参建议：**
- 首轮 playtest 重点观察：玩家是否觉得"太吵"（降低 daily_event_cap）还是"太安静"（提高）
- 传闻延迟过长会让玩家忘记上下文；过短则失去"传播感"
- backlog_bonus 过高会导致旧事件突然大量涌入

## Visual/Audio Requirements

活江湖层本身是纯逻辑调度系统，不直接控制视觉/音频表现。呈现由下游系统负责：

- **传闻呈现**：对话系统负责（说书人动画、旅人对话 UI）
- **飞书到达**：NPC 状态管理的信使系统控制到达动画和音效
- **世界事件环境变化**：地图/场景管理系统执行（如门派废墟状态切换）

**本系统唯一的感官需求：**
- 事件到达时需要一个轻量级的"世界脉搏"提示音（非 UI 弹窗，而是类似微风的环境音变化），让玩家潜意识感知"有什么在流动"
- 具体音效设计由音频 GDD 定义，本系统仅负责触发 `world_pulse` 信号

## UI Requirements

活江湖层不拥有独立 UI 面板。事件内容通过以下已有 UI 通道呈现：

| 事件类型 | UI 通道 | 负责系统 |
|---------|---------|---------|
| 传闻 | 对话气泡 / 说书人场景 | 对话系统 |
| 暗号 | 对话中高亮文本 / 环境交互物 | 对话系统 / 场景管理 |
| 飞书 | 信件 UI（已有） | NPC 状态管理 |
| 同伴代办 | 同伴飞书汇报（信件 UI 复用） | NPC 状态管理 |
| 世界事件 | 无专属 UI，通过传闻 + 环境变化间接传达 | — |

**调试/开发 UI（仅 Debug 模式）：**
- 事件表实时状态面板：显示所有 pending/triggered 事件及其 selection_score
- 每日 tick 日志：记录候选池大小、选中事件、被跳过事件及原因

## Acceptance Criteria

### AC-1: 每日 Tick 基础循环
- [ ] `day_advanced` 事件触发时，系统扫描事件表并产出 ≤ daily_event_cap 个事件
- [ ] 不满足 preconditions 的事件不会出现在候选池中
- [ ] 呼吸期内 effective_cap 正确翻倍

### AC-2: 条件系统
- [ ] 所有 9 种 precondition 原语均可独立验证
- [ ] 多条件 AND 组合正确工作
- [ ] `day_elapsed_since` 正确计算自 flag 设置以来的天数

### AC-3: 优先级与选择
- [ ] 高 priority 事件优先于低 priority 被选中
- [ ] 同 priority 时 tag_relevance_score 高者优先
- [ ] backlog_bonus 随天数累积且不超过 cap
- [ ] 类型均衡规则生效（同类 ≤ max_type_per_day）

### AC-4: 传闻传播延迟
- [ ] 同区传闻延迟 = base_delay (1天)
- [ ] 跨国传闻延迟 = 4 天
- [ ] 延迟期间事件处于 pending 状态，不呈现给玩家

### AC-5: 事件生命周期
- [ ] 状态按 inactive → pending → triggered → delivered → expired 流转
- [ ] 到期事件正确执行 on_expire 副作用
- [ ] 触发事件正确执行 on_trigger 副作用（set_flag、npc_state_change）

### AC-6: 呼吸期行为
- [ ] breathing_only 事件仅在呼吸期激活
- [ ] 呼吸期结束时 breathing_only 的 pending 事件回退至 inactive
- [ ] 积压释放在呼吸期开始首日生效

### AC-7: 章节切换清理
- [ ] 切换章节时，chapter_range 不匹配的 pending 事件标记为 expired
- [ ] expired 事件的 on_expire 副作用被执行

### AC-8: 同伴代办
- [ ] 仅当同伴处于"离队远行"状态且事件标记 delegate_allowed 时可触发
- [ ] 代办结果从预配置的选项中正确选取

### AC-9: 无同日连锁
- [ ] on_trigger 新注册的事件不会在同一日 tick 中被选中

## Open Questions

> 当前无未解决的设计问题。所有核心决策已在本 GDD 中确定。
> 如后续实现中发现新问题，在此追加并标记日期。
