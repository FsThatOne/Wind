# 误会系统 (Misunderstanding System)

> **Status**: Designed
> **Author**: user + agents
> **Last Updated**: 2026-06-07
> **Implements Pillar**: Pillar 1 (江湖是活的), Pillar 2 (每个选择必须有重量), Pillar 4 (情感深度优先于内容广度)

## Overview

误会系统是《风止》的**叙事信息差管理层**——它追踪、演化和呈现主角与 NPC 之间因信息不对称而产生的误解，并为每个误会设定透明度信号、恶化条件和澄清路径。

作为数据层，它是一台**误会状态机 + 条件规则引擎**：每个活跃误会是一条实例记录，持有来源、严重度、透明度等级和剩余澄清窗口。系统监听来自活江湖层、对话系统和主线叙事的事件，在满足条件时注册新误会、升级严重度或解除误会，并将结果写入 NPC 状态管理的 `misunderstanding_mod`（-2~0）影响态度档位。

作为玩家体验，它让你在江湖行走时感受到"有什么不对了"：某个曾经亲近的同伴忽然措辞疏远、客栈传闻里有人在背后议论你"负义"、一封来信的语气突然冷了。你能**察觉误会存在**——这是"误会透明度"原则——但你不确定误会从何而来、该如何解开。解法不是一个明确的"去找 XX 对话"提示，而是需要你在后续旅程中留意线索、做出正确选择，或在特定窗口内完成澄清行动。错过窗口，误会定型，成为你和那个人之间"再也回不去的一段路"。

核心职责：
1. **误会注册** — 监听事件触发条件，创建误会实例并设定初始严重度
2. **透明度信号** — 通过对话语气变化、传闻、暗号等间接方式让玩家感知误会存在（不直接告知具体内容）
3. **误会演化** — 随时间或事件恶化/缓和误会严重度
4. **澄清路径** — 定义每个误会的解除条件和窗口期
5. **态度写入** — 根据当前活跃误会严重度计算 `misunderstanding_mod` 并写入 NPC 状态
6. **诀别触发** — 极端未澄清误会可调用感情系统 `force_break()` 造成不可逆决裂

## Player Fantasy

**"我明明没有做错，但她以为我负了她。"**

误会系统服务的核心情感，是武侠小说中最令读者揪心的一类场景：令狐冲与岳灵珊因信息差渐行渐远，杨过与郭芙的恩怨因一次误判定型终生。玩家体验的不是"系统判定我失败了"，而是**"我和这个我在乎的人之间，有一堵看不见的墙正在长高"**。

**感知层：隐隐的不对劲**

你从塞北归来，去找白苓。她说话时用了"阁下"而不是以前的"停云"——措辞回退了一级。你知道有什么不对了，但不知道是什么。传闻里有人提到你"在绝江渡见死不救"，你皱眉：那明明不是事实。但你还来不及解释，她已经转身离开了。

这种"我知道不对了、但我不知道误会从何而来"的焦虑感，就是误会系统想要制造的核心张力。

**抉择层：救得了吗？值得吗？**

每个误会都有窗口期。你可以中断当前旅程，回头去找线索、找证人、赶赴某个特定的地点完成澄清行动——但这意味着放弃另一条当前可追的支线或时限内容。Pillar 2 说"每个选择有重量"，误会系统把这个重量施加在**修复关系**和**推进其他目标**之间的张力上。

**结果层：释怀或遗憾**

成功澄清时，你收获的是一段"风雨后更深的信任"——里程碑不会被误会击穿（感情系统的地板机制），但态度会在解除后弹回甚至超过误会前。
错过窗口时，误会定型。态度永久下移一级，对话路径永远改写。如果是极端误会未解，甚至触发诀别——这个人从此拔剑相向。这种"再也回不去"的重量，正是驱动玩家在二周目时"这次我一定要早点去解释"的动力。

**支柱对齐：**
- **Pillar 1** — 误会因信息不对称自然产生：玩家不在场时，江湖中的传闻可能歪曲事实，NPC 只听到片面消息
- **Pillar 2** — 错过澄清窗口是有重量的选择
- **Pillar 4** — 每个误会都是精心设计的叙事事件，不是随机生成的负面状态；少而深

## Detailed Design

### Core Rules

**1. 误会实例 (Misunderstanding Instance)**

每个活跃误会是一条结构化记录：

```
MisunderstandingInstance:
  id:             string          # 唯一标识 "mis_bailing_juejiang"
  target_npc:     npc_id          # 被误会影响的 NPC
  source_type:    enum            # JIANGHU_EVENT | DIALOGUE_CHOICE | ABSENCE
  severity:       enum            # MINOR(微误) | MODERATE(中误) | SEVERE(重误)
  transparency:   enum            # HIDDEN → HINTED → PERCEIVED
  initial_window: int             # 创建时的原始窗口天数（用于透明度半窗口计算）
  window_remaining: int           # 剩余可主动澄清的游戏天数 (-1 = 已关闭)
  escalation_count: int           # 累计恶化次数
  created_chapter: int
  created_day:    int             # 创建时的游戏天数（用于透明度天数计算）
  resolution_conditions: []       # 澄清条件列表
```

**2. 三大触发来源**

| 来源 | 触发时机 | 示例 |
|------|----------|------|
| **活江湖层事件** | 活江湖层 tick 产生传闻/世界事件，命中已注册的 `misunderstanding_trigger` | 绝江渡传闻"停云见死不救"→ 白苓注册了对此敏感的 trigger |
| **对话选择** | 玩家在对话中选择了特定选项，该选项标记 `mis_trigger: {npc, severity}` | 向明德堂弟子说出白苓行踪 → 白苓误以为你出卖了她 |
| **缺席误解** | 玩家在限时事件中未出现在特定地点/未完成特定行动 | 白苓等你三天未至 → 产生"被抛弃"的误读 |

**3. 三级严重度**

| 等级 | 名称 | `misunderstanding_mod` 贡献 | 效果 | 窗口期(默认) |
|------|------|---------------------------|------|-------------|
| 1 | 微误 (MINOR) | -1 | 措辞回退一级，部分对话选项锁定 | 7 天 |
| 2 | 中误 (MODERATE) | -2 | 态度档位明显下降，支线锁定 | 5 天 |
| 3 | 重误 (SEVERE) | -2 + 诀别倒计时启动 | 触发 `force_break()` 倒计时（3天未澄清 → 诀别） | 3 天(诀别倒计时) |

> 约束：同一 NPC 同时存在多条误会时，`misunderstanding_mod = max(所有活跃误会的贡献, -2)`，不叠加超出 NPC State 定义的 [-2, 0] 范围。

**4. 透明度信号规则**

误会创建后不立即告知玩家具体内容，而是通过三阶段透明度逐步暴露：

| 阶段 | 信号形式 | 触发条件 |
|------|----------|----------|
| HIDDEN | 无直接信号，仅 NPC 态度静默下降 | 刚创建的前 1 天 |
| HINTED | 间接信号：NPC 称呼回退、传闻提及、第三方暗示 | 创建后 1 天自动升级，或玩家与该 NPC 交互时 |
| PERCEIVED | 玩家明确知道"此人对我有误会"（UI 关系面板标记为"心有疑云"） | 窗口期过半，或玩家主动调查 |
| URGENT | 关系面板标记脉动 + 朦胧化 UI 推送"此人的疑虑正在加深"类文学信号 | `window_remaining ≤ 3` 且已在 PERCEIVED 阶段 |

**5. 混合制澄清机制**

```
澄清路径 = 窗口期内主动澄清 ∪ 窗口关闭后等待特定剧情节点

IF window_remaining > 0:
    玩家可通过满足 resolution_conditions 主动解除
    解除后: severity → RESOLVED, misunderstanding_mod 归零, 态度弹回

IF window_remaining == 0 AND severity < SEVERE:
    误会定型 (PERMANENT)
    misunderstanding_mod 永久保留
    仅在后续特定剧情节点（编剧手工标记 unlock_permanent_mis）时可再次解除

IF window_remaining == 0 AND severity == SEVERE:
    调用 force_break(target_npc) → 诀别，不可逆
```

### States and Transitions

```
状态机:

  [DORMANT] ──触发条件满足──→ [ACTIVE]
                                 │
                    ┌─────────────┼─────────────┐
                    │ 恶化事件     │ 窗口归零      │ 澄清成功
                    ▼             ▼              ▼
              [ESCALATED]    [PERMANENT]    [RESOLVED]
                    │             │
                    │ severity→SEVERE且窗口归零
                    ▼             ▼
                 [BROKEN] ← force_break()
```

| 状态 | 含义 | 可转移到 |
|------|------|----------|
| DORMANT | 触发条件已注册但未满足 | ACTIVE |
| ACTIVE | 误会生效中，窗口期内 | ESCALATED, RESOLVED, PERMANENT |
| ESCALATED | 恶化（severity +1）后仍在窗口内 | RESOLVED, PERMANENT, BROKEN |
| RESOLVED | 成功澄清 | (终态) |
| PERMANENT | 窗口关闭，误会定型 | RESOLVED(仅限特定剧情节点), BROKEN |
| BROKEN | 诀别已触发 | (终态，不可逆) |

**恶化条件：** 同一误会在 ACTIVE 期间若再次被相关事件命中（如更多负面传闻），`escalation_count++`，severity 向上提升一级（MINOR→MODERATE, MODERATE→SEVERE）。

### Interactions with Other Systems

| 系统 | 方向 | 接口 | 说明 |
|------|------|------|------|
| **活江湖层** | 消费 | `register_misunderstanding_trigger(trigger_config)` | 注册"什么世界事件会触发此误会" |
| **活江湖层** | 监听 | `on_world_event(event)` | 活江湖层 tick 产生事件后广播 |
| **NPC 状态管理** | 写入 | `set_misunderstanding_mod(npc_id, value)` | 写入 [-2, 0] 范围的修正值 |
| **NPC 状态管理** | 读取 | `get_npc_state(npc_id)` | 读取当前态度、关系阶段用于计算 |
| **感情系统** | 读取 | `get_milestone_flags(npc_id)` | 判断里程碑地板，确保普通误会不击穿地板 |
| **感情系统** | 调用 | `force_break(npc_id)` | 重误未澄清时触发诀别 |
| **对话系统** | 监听 | `on_dialogue_choice(choice)` | 监听带 `mis_trigger` 标记的对话选项 |
| **对话系统** | 写入 | `inject_transparency_signal(npc_id, signal_type)` | 注入称呼回退/语气变化等透明度信号 |
| **模糊 UI** | 通知 | `notify_attitude_change(npc_id, reason)` | 触发"心有疑云"等模糊 UI 反馈 |

**地板保护规则：**

```
before_apply_misunderstanding_mod(npc_id, mod):
    floor = romance.get_milestone_floor(npc_id)
    current_score = npc_state.get_attitude_score(npc_id)
    effective_mod = max(mod, floor - current_score)  # 不击穿地板
    # 例外：SEVERE 触发 force_break 时无视地板
```

**地板保护下的独立效果层**（W3 解决）：

当 `effective_mod` 被地板钳位为 0（即态度已在地板值、无法再降）时，误会**不会被忽略**——它通过独立的对话效果层继续产生影响：

| 地板保护状态 | 态度档位显示 | 对话效果（独立于态度） |
|---|---|---|
| mod 被钳位 | 保持地板标签（如"推心置腹"） | 措辞回退一级 + 部分对话选项锁定（与正常 MINOR/MODERATE 效果一致） |
| mod 未被钳位 | 正常降档 | 同上 + 态度档位下移 |

> **设计原理**：地板保护的是"关系不会因普通误会倒退到历史以下"的长期承诺，但短期的"语气变冷、部分话题不愿聊"仍需体现——否则误会系统对深度关系 NPC 完全失效。

**级联熔断器**（S5 / Scenario 5 解决）：

```
on_battle_end(result):
    if result == DEFEAT:
        set_protection("absence_immunity", duration=2)  # 战败后 2 天保护期

on_absence_check(npc_id):
    if has_protection("absence_immunity"):
        skip_trigger()  # 不触发缺席误解
        return
    # ... 正常缺席判定逻辑
```

> **核心原则**：**不可控的失败（战力不足导致的战败）不应触发仅属于可控选择（主动缺席）的惩罚**。保护期 2 天给予玩家恢复和赶赴澄清的时间窗口。此规则仅豁免"缺席误解"触发器，不豁免其他类型的误会。

## Formulas

### F1. misunderstanding_mod 计算

```
# 每次误会状态变化后重算
func compute_misunderstanding_mod(npc_id) -> int:
    active_instances = get_active_instances(npc_id)  # ACTIVE + ESCALATED + PERMANENT
    if active_instances.is_empty():
        return 0
    
    max_severity_mod = max(inst.severity_to_mod() for inst in active_instances)
    return clamp(max_severity_mod, -2, 0)

# severity_to_mod 映射
MINOR    → -1
MODERATE → -2
SEVERE   → -2  (额外效果由诀别倒计时处理，mod 仍为 -2)
```

### F2. 窗口期递减

```
# 每日 tick (活江湖层 day_advanced 事件后)
func on_day_advanced():
    for inst in all_active_instances():
        if inst.window_remaining > 0:
            inst.window_remaining -= 1
        if inst.window_remaining == 0:
            if inst.severity == SEVERE:
                romance.force_break(inst.target_npc)
                inst.state = BROKEN
            else:
                inst.state = PERMANENT
            recompute_mod(inst.target_npc)
```

### F3. 透明度自动升级

```
func check_transparency_upgrade(inst):
    days_elapsed = current_day - inst.created_day
    
    if inst.transparency == HIDDEN and days_elapsed >= 1:
        inst.transparency = HINTED
        dialogue.inject_transparency_signal(inst.target_npc, "hint")
    
    if inst.transparency == HINTED:
        half_window = inst.initial_window / 2
        if days_elapsed >= half_window:
            inst.transparency = PERCEIVED
            blurred_ui.notify_attitude_change(inst.target_npc, "misunderstanding")
    
    if inst.transparency == PERCEIVED and inst.window_remaining <= 3:
        inst.transparency = URGENT
        blurred_ui.pulse_relationship_indicator(inst.target_npc)
        blurred_ui.push_literary_signal(inst.target_npc, "urgency")
        # 推送朦胧化文学信号，如"你隐约觉得，若再不做些什么，某种东西就要碎了"
```

> **W17 设计意图**：URGENT 阶段确保玩家在误会窗口即将关闭时获得更强的感知信号。这不是"倒计时数字"，而是朦胧化 UI 风格内的"氛围加深"——关系面板标记脉动加快、进入相关区域时环境音调变暗。玩家不知道"还剩 3 天"，但能感受到"再不去做点什么就来不及了"。

### F4. 恶化判定

```
func on_escalation_event(inst):
    inst.escalation_count += 1
    if inst.severity == MINOR:
        inst.severity = MODERATE
    elif inst.severity == MODERATE:
        inst.severity = SEVERE
        inst.window_remaining = min(inst.window_remaining, 3)  # 诀别倒计时上限
    recompute_mod(inst.target_npc)
```

### F5. 澄清弹回

```
func resolve_misunderstanding(inst):
    inst.state = RESOLVED
    set_misunderstanding_mod(inst.target_npc, compute_misunderstanding_mod(inst.target_npc))
    # 弹回加成：澄清后短暂态度+1（持续至下次态度事件）
    npc_state.apply_temporary_bonus(inst.target_npc, +1, duration=3_days)
```

## Edge Cases

| ID | 场景 | 处理规则 |
|----|------|----------|
| E1 | 同一 NPC 同时存在 MINOR + MODERATE 两条误会 | `misunderstanding_mod = max(-1, -2) = -2`；两条独立追踪窗口和澄清条件，解除一条后重算 mod |
| E2 | 误会窗口期最后一天，玩家触发了恶化事件 | 先执行恶化（severity +1），再检查窗口归零逻辑；若恶化到 SEVERE 则立即进入 3 天诀别倒计时（窗口重设为 3） |
| E3 | 澄清行动与恶化事件在同一天触发 | 澄清优先：若当日同时满足 `resolution_conditions` 和恶化事件，先结算澄清，误会转 RESOLVED，恶化事件无效 |
| E4 | `force_break()` 与正面里程碑在同一 tick | `force_break()` 优先级最高（与感情系统 D6 一致），覆写一切 |
| E5 | 误会目标 NPC 已处于 M_BREAK 状态（之前因其他原因诀别） | 不再注册新误会——已经是终态，新的负面信息无意义 |
| E6 | 缺席误解触发时玩家正在与该 NPC 同场景 | 不触发——缺席误解的前置条件包含"玩家不在 NPC 所在区域"，同场景下自动跳过 |
| E7 | PERMANENT 误会的特定剧情解锁节点恰好也是另一条新误会的触发节点 | 先结算旧误会解除（PERMANENT → RESOLVED），再注册新误会；两者在同一 tick 内顺序执行，mod 先归零再重算 |
| E8 | 存档/读档时窗口期跨越多天 | 读档后一次性递减所有欠缺的天数，逐天触发窗口检查（含可能的定型/诀别），确保不因存档逃避窗口倒计时 |
| E9 | 同一对话选择同时标记了对两个不同 NPC 的 `mis_trigger` | 为每个 NPC 独立创建误会实例，各自走独立的状态机 |
| E10 | 玩家尝试在 HIDDEN 阶段主动澄清（尚未感知误会） | 若玩家恰好完成了 `resolution_conditions`，仍然结算成功——系统不要求玩家"知道"误会才能解除它，只要求满足条件 |

## Dependencies

### 消费（本系统依赖）

| 系统 | 接口 | 用途 |
|------|------|------|
| 活江湖层 (#16) | `register_misunderstanding_trigger(trigger_config)` | 注册世界事件触发误会的条件 |
| 活江湖层 (#16) | `on_world_event(event)` 广播 | 监听世界事件/传闻，判断是否命中触发条件 |
| 活江湖层 (#16) | `on_day_advanced()` 广播 | 每日 tick 驱动窗口递减和透明度升级 |
| NPC 状态管理 (#10) | `get_npc_state(npc_id)` | 读取当前态度分数和关系阶段 |
| 感情系统 (#13) | `get_milestone_flags(npc_id)` | 判断里程碑状态，用于地板保护 |
| 感情系统 (#13) | `get_milestone_floor(npc_id)` | 获取态度地板值 |
| 感情系统 (#13) | `force_break(npc_id)` | 重误诀别时调用 |
| 对话系统 (#5) | `on_dialogue_choice(choice)` 广播 | 监听带 `mis_trigger` 标记的对话选项 |

### 提供（本系统暴露）

| 接口 | 消费者 | 用途 |
|------|--------|------|
| `get_active_misunderstandings(npc_id) -> [MisunderstandingInstance]` | 对话系统, 模糊 UI | 查询当前活跃误会列表，用于对话分支和 UI 提示 |
| `set_misunderstanding_mod(npc_id, value)` | NPC 状态管理 | 写入态度公式中的误会修正项 |
| `inject_transparency_signal(npc_id, signal_type)` | 对话系统 | 触发称呼回退/语气变化等透明度表现 |
| `notify_attitude_change(npc_id, reason)` | 模糊 UI | 触发"心有疑云"等模糊反馈 |
| `is_misunderstanding_active(npc_id) -> bool` | 叙事系统, 条件检查 | 快速查询某 NPC 是否有活跃误会 |

### 实现顺序建议

1. 误会实例数据结构 + 状态机核心
2. 活江湖层 trigger 注册与事件监听对接
3. 对话系统 `mis_trigger` 标记与监听对接
4. `misunderstanding_mod` 计算与 NPC 状态写入
5. 透明度信号层 + 模糊 UI 对接
6. 感情系统地板保护 + `force_break` 路径
7. 缺席误解检测（依赖场景/地图系统）

## Tuning Knobs

| 参数 | 默认值 | 范围 | 作用 | 影响 |
|------|--------|------|------|------|
| `minor_window_days` | 7 | 3–14 | 微误 (MINOR) 的默认澄清窗口天数 | 值越小，微误越紧迫 |
| `moderate_window_days` | 5 | 2–10 | 中误 (MODERATE) 的默认澄清窗口天数 | 值越小，容错越低 |
| `severe_countdown_days` | 3 | 1–5 | 重误 (SEVERE) 的诀别倒计时天数 | 值=1 时几乎无法挽救 |
| `hidden_duration_days` | 1 | 0–3 | HIDDEN 阶段持续天数（0=立即进入 HINTED） | 值越大，误会越隐蔽 |
| `perceived_threshold_ratio` | 0.5 | 0.3–0.8 | 窗口期过多少比例后自动升级到 PERCEIVED | 值越小，玩家越早获知 |
| `resolve_attitude_bonus` | +1 | 0–+2 | 成功澄清后的临时态度加成 | 值越大，澄清奖励越明显 |
| `resolve_bonus_duration_days` | 3 | 1–7 | 澄清弹回加成持续天数 | 短则奖励感弱，长则影响后续判定 |
| `max_active_per_npc` | 3 | 1–5 | 同一 NPC 同时最多活跃误会数量 | 防止叠加过多误会导致体验混乱 |
| `absence_trigger_threshold_days` | 3 | 1–7 | 缺席误解的触发阈值（玩家离开 NPC 区域超过此天数） | 值越小，缺席误解越敏感 |

## Visual/Audio Requirements

### 视觉需求

| 表现 | 触发条件 | 说明 |
|------|----------|------|
| **称呼回退** | 透明度 ≥ HINTED | NPC 对话中称谓从亲密回退到疏远（如"停云"→"阁下"），由对话系统根据 `transparency_signal` 切换称呼表 |
| **对话语气着色** | 透明度 ≥ HINTED | NPC 对话文本使用更冷淡/疏离的措辞变体（同一语义的冷版本和暖版本） |
| **关系面板标记** | 透明度 = PERCEIVED | 关系面板中该 NPC 条目出现"心有疑云"标记（模糊 UI 风格，不显示具体误会内容） |
| **诀别演出** | `force_break()` 触发 | 强制叙事演出（NPC 翻脸/拔剑/离去），具体内容由叙事事件配置（与模糊 UI GDD 一致） |

### 音频需求

| 音效 | 触发条件 | 说明 |
|------|----------|------|
| **关系变化暗示音** | 误会从 HIDDEN → HINTED | 短促的低沉弦乐或风声，暗示"有什么变了"，不引起警觉但留下潜意识印记 |
| **窗口紧迫音** | `window_remaining ≤ 2` 且透明度 = PERCEIVED | 每日 tick 时低频率播放一次提示音，暗示时间紧迫 |
| **澄清释怀音** | 误会 → RESOLVED | 清澈的琴弦回响，传达"冰释前嫌"的情感 |
| **定型遗憾音** | 误会 → PERMANENT | 低沉的余韵，传达"来不及了"的惋惜 |

> 注：诀别演出的音效由具体叙事事件配置（拔剑声/摔门声/脚步远去等），不在本系统统一定义。

## UI Requirements

误会系统遵循**模糊 UI**设计原则——不直接暴露系统内部数值，所有信息通过叙事化表现传递。

| UI 元素 | 位置 | 内容 | 模糊 UI 规则 |
|---------|------|------|-------------|
| **关系面板 — 心有疑云标记** | 角色关系列表 → NPC 条目旁 | 当 `transparency = PERCEIVED` 时显示一个水墨风"云雾"图标 | 不显示误会具体内容、来源或严重度；仅表达"此人对你有心结" |
| **关系面板 — 态度措辞** | 角色关系列表 → 态度描述 | 态度档位文字因误会而附加修饰（如"以礼相待 · 心有疑云"） | 不显示数值，仅追加定性描述 |
| **对话中 — 称呼变化** | 对话文本 | NPC 使用疏远称呼替代亲密称呼 | 无额外 UI 提示，靠文本本身传达 |
| **传闻面板** | 江湖传闻列表 | 与误会相关的传闻词条可见（如"有人说你在绝江渡见死不救"） | 传闻是活江湖层的一部分，误会系统不额外创建 UI，仅标记相关传闻为"与某人心结相关" |
| **诀别通知** | 全屏叙事演出 | `force_break` 触发时的强制演出画面 | 绕过模糊 UI 的延迟规则，立即呈现（与模糊 UI GDD 的 `force_break_bypass` 一致） |

**不做的事：**
- 不显示误会倒计时天数
- 不显示澄清条件列表
- 不显示严重度等级
- 不提供"误会日志"或"待澄清事项"面板

## Acceptance Criteria

| ID | 条件 | 验证方式 |
|----|------|----------|
| AC1 | 活江湖层事件命中 `misunderstanding_trigger` 时，正确创建 ACTIVE 状态的误会实例 | 单元测试：注册 trigger → 广播匹配事件 → 断言实例 state=ACTIVE, severity=配置值 |
| AC2 | 对话选择标记 `mis_trigger` 后，对应 NPC 生成误会实例 | 单元测试：模拟对话选择 → 断言实例创建且 source_type=DIALOGUE_CHOICE |
| AC3 | 缺席超过 `absence_trigger_threshold_days` 天后，自动触发缺席误解 | 单元测试：模拟玩家离开区域 N 天 → 断言实例创建且 source_type=ABSENCE |
| AC4 | `misunderstanding_mod` 计算取所有活跃误会的最大贡献值，结果 ∈ [-2, 0] | 单元测试：同 NPC 创建 MINOR+MODERATE → 断言 mod=-2 |
| AC5 | 窗口期每日递减，归零后 MINOR/MODERATE 转 PERMANENT | 单元测试：创建 window=3 的 MINOR → tick 3 天 → 断言 state=PERMANENT |
| AC6 | SEVERE 窗口归零时调用 `force_break()` | 单元测试：创建 SEVERE window=1 → tick 1 天 → 断言 force_break 被调用且 state=BROKEN |
| AC7 | 透明度按 HIDDEN→HINTED→PERCEIVED 正确升级 | 单元测试：创建实例 → tick 1 天 → 断言 HINTED → tick 至半窗口 → 断言 PERCEIVED |
| AC8 | 满足 `resolution_conditions` 时误会转 RESOLVED，mod 重算 | 单元测试：创建 ACTIVE MODERATE → 满足条件 → 断言 RESOLVED 且 mod=0 |
| AC9 | 澄清弹回加成 +1 持续 `resolve_bonus_duration_days` 天后消失 | 单元测试：解除误会 → 断言 bonus=+1 → tick 3 天 → 断言 bonus=0 |
| AC10 | 恶化事件正确提升 severity（MINOR→MODERATE→SEVERE） | 单元测试：MINOR 实例 → 触发恶化 → 断言 severity=MODERATE |
| AC11 | 同 NPC 活跃误会数不超过 `max_active_per_npc` | 单元测试：已有 3 条活跃 → 尝试创建第 4 条 → 断言拒绝或替换最轻误会 |
| AC12 | 地板保护：普通误会不击穿感情系统里程碑地板 | 集成测试：里程碑 floor=0 → 施加 MODERATE(mod=-2) → 断言 attitude 不低于 floor |
| AC13 | M_BREAK 状态下不注册新误会 | 单元测试：NPC 已 M_BREAK → 尝试注册 → 断言无新实例 |
| AC14 | 存档/读档后窗口期正确补算 | 单元测试：创建 window=5 → 模拟存档跳 3 天 → 读档 → 断言 window=2 |

## Open Questions

| ID | 问题 | 影响 | 建议处理时机 |
|----|------|------|-------------|
| OQ1 | 缺席误解是否需要与"日程/任务系统"联动？当前设计仅检测"玩家不在 NPC 区域"，但如果未来有任务系统提示"白苓在等你"，是否应将此提示纳入缺席触发的前置条件？ | 可能影响缺席误解的触发精度 | 任务系统 GDD 设计时 |
| OQ2 | `max_active_per_npc` 达到上限时，新误会应该被拒绝还是替换最轻的那条？ | 影响 AC11 的具体实现 | 实现阶段 |
| OQ3 | PERMANENT 状态误会的"特定剧情解锁节点"由谁标记？编剧在叙事数据中手工标 `unlock_permanent_mis` tag，还是系统自动在章节转折点解锁？ | 影响内容制作管线和工具需求 | 叙事系统/章节系统 GDD 设计时 |
| OQ4 | 澄清弹回加成（`resolve_attitude_bonus`）是否应计入 NPC 态度公式的正式项，还是作为临时 buff 处理？当前设计为临时 buff，但如果加入正式公式可能需要 NPC State GDD 修改 | 影响 NPC 态度公式结构 | 集成测试阶段 |
