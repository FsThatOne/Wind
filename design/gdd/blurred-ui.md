# Blurred UI (朦胧化 UI)

> **Status**: Designed
> **Author**: user + agents
> **Last Updated**: 2026-06-05
> **Implements Pillar**: Pillar 3 (武侠味先于游戏味)

## Overview

**朦胧化 UI** 是一套数据表达翻译层，负责将角色属性、心境状态、感情关系等后端数值转化为文学性描述、色调变化和意象化视觉线索——让玩家在战斗之外"读到"的不是数字，而是武侠小说般的意境。

核心原则遵循"**战斗内 / 战斗外分治**"：战斗场景保留完整数值 UI（HP、伤害、心法效果、buff 时长）以服务战术决策；而一切非战斗场景中，成长数据以境界文学呈现、人际关系以隐式叙事线索呈现、心境以视觉色调与内心独白呈现，所有精确数值对玩家不可见。

系统不产生新的 gameplay 数据，它是单向消费者——从角色属性、心境双轴、感情系统读取状态，输出为"玩家能看到什么、以什么形式看到"的规则集合。

**若无此系统**：游戏退化为标准数据驱动 RPG，丧失"江湖是诗"的核心体验差异。

> **设计哲学**: 战斗是棋局，让棋手看到棋盘；江湖是诗，让游人忘掉数字。

## Player Fantasy

**核心幻想**：我不是在"玩一个有属性面板的 RPG"——我是在**读一本以我为主角的武侠小说**。

当我打开人物界面，看到的不是"攻击力 47 / 防御力 32"，而是"**剑意渐凝，已入炉火纯青之境**"。当我与某位 NPC 的关系恶化，游戏不会跳出"-15 好感度"，而是在下一次相遇时她的语气变了、书信不再寄来、茶馆里有人说"她好像不太想提起你"。

**情感锚点**：
- **"我在经历一个故事"而非"我在操作一套系统"** —— 朦胧化 UI 制造的是一种叙事距离感：玩家通过文字、色调和暗示来感知自己角色的成长与处境，如同翻阅一本书时从字里行间捕捉人物的情绪变化。
- **"悟而非算"** —— 功力以境界名呈现，心境以色调和内心独白呈现，好感度以叙事事件呈现。玩家通过感知和推测来理解状态，而非通过精确数值计算最优解。

**参考体验**：
- 《Disco Elysium》—— 内心声音作为状态指示器（Electrochemistry、Empathy 等技能以内心独白暴露角色状态）
- 《太吾绘卷》—— 以"入门/登堂/炉火纯青/出神入化"等境界名代替精确数值的直觉感
- 武侠小说阅读体验 —— 读者永远不知道"郭靖的内力值是 87"，但通过降龙十八掌的威力、战斗描写的措辞变化，读者能**感知**实力差距

**Pillar 对齐**：此系统是 Pillar 3（武侠味先于游戏味）的核心执行者——它直接决定了玩家每分钟的信息接收方式是"游戏 UI"还是"江湖叙事"。

## Detailed Design

### Core Rules

**Rule 1 — 分治总则**

| 上下文 | 显示模式 | 原则 |
|--------|---------|------|
| 战斗场景 | **Clear** | 全数值化：HP、伤害值、心法加成%、buff 时长（秒）、行动条百分比 |
| 非战斗场景 | **Blurred** | 全文学化：境界名、色调暗示、叙事事件、内心独白；精确数值完全不可见 |

分治判定以 `is_in_combat` flag 为唯一依据。切换时瞬间完成（无渐变过渡）。

---

**Rule 2 — 翻译管道（Data Channels）**

系统消费 3 个上游数据源，各有独立翻译通道：

| # | 数据源 | 消费的数据 | 翻译方式 | 表达形态 |
|---|--------|-----------|----------|---------|
| CH-1 | 角色属性 | `total_power` | 境界映射表（9级阈值查表） | 文字标签："初学乍练"…"返璞归真" |
| CH-2 | 心境双轴 | `get_mindset_zone()` → 9-value enum | 区域→色调表 + 文学模板库 | 屏幕色调偏移 + 内心独白文本 |
| CH-3 | 感情系统 | `attitude_tier` (8级) + `comet_events[]` | 态度→叙事触发规则 | 对话语气/环境叙事（书信、传闻、冷淡、缄默） |

---

**Rule 3 — 场景感知映射**

同一底层数据在不同 UI 上下文中以不同形式表达：

| 场景上下文 | CH-1（境界）表达 | CH-2（心境）表达 | CH-3（关系）表达 |
|-----------|---------------|---------------|---------------|
| 角色面板 | 境界文字 + 一句境界描述 | 主色调光晕 + 当前区域文学名 | 不显示（面板无关系信息） |
| 对话中 | — | 内心独白插入（Inner Monologue） | NPC 语气变化（由 attitude_tier 决定对话分支可见性） |
| 自由探索 | — | 场景色调整体偏移 | 环境叙事：茶馆传闻、路人窃语、书信到达 |
| 与他人对比 | 相对境界暗示："此人功力远在你之上" | — | — |
| 战斗结算 | 切换为精确数值：功力总值 | 切换为精确数值：心境加成% | — |

**场景上下文检测规则**：由当前活跃的 `UIContext` enum 决定（PANEL / DIALOGUE / EXPLORATION / COMPARISON / COMBAT_RESULT），各 Channel 注册自己响应哪些 context。

---

**Rule 4 — 事件驱动延迟揭示**

数据变化不立即反映在文学化展示中。更新时机：

| 数据变化 | 何时对玩家可见 | 设计理由 |
|---------|--------------|---------|
| `total_power` 跨越境界阈值 | 下次打开角色面板时，触发"破境"叙事文本 | 模拟顿悟感 |
| `total_power` 在同境界内增长 | **不可见**（只知所在，不知距离） | Rule 3 + 防止刷数心态 |
| `mindset_zone` 变化 | 下一次进入新场景/新对话时，色调渐变切换 | 模拟"回头看才意识到自己变了" |
| `attitude_tier` 变化 | 下次与该 NPC 相关事件触发时（对话/书信/传闻） | 关系变化是"被发现的"而非"被通知的" |
| 新 `comet_event` 到达 | 由事件系统调度，在下一个合适的叙事节点插入 | 不打断当前行为 |

**缓冲队列**：每个 Channel 维护一个 `pending_reveals[]` 队列。当对应 UIContext 激活时，按 FIFO 顺序弹出并执行表达。若积压超过 3 条同类揭示，合并为一条摘要式表达（防止信息洪水）。

---

### States and Transitions

| 状态 | 描述 | 进入条件 | 退出条件 |
|------|------|---------|---------|
| **BLURRED** | 所有非战斗 UI 以文学化呈现 | 游戏启动默认 / 战斗结束 | `is_in_combat` → true |
| **CLEAR** | 战斗 UI 全数值化 | 进入战斗 | 战斗结束 / 逃离战斗 |
| **TRANSITION_TO_CLEAR** | 瞬切+破境阈值播放 | 进入战斗且有 pending 破境 | 过渡动画完成 |
| **TRANSITION_TO_BLURRED** | 战斗结算数字→淡出→文学UI回归 | 战斗结束 | 淡出完成（≤1s） |

**非法状态**：BLURRED + 战斗中不可能共存；CLEAR + 非战斗场景不可能共存。如检测到非法组合则强制切换到正确状态并写入错误日志。

---

### Interactions with Other Systems

| 系统 | 方向 | 接口 | 说明 |
|------|------|------|------|
| 角色属性 | ← 消费 | `CharacterAttributes.total_power: int` | 读取功力总值，查表映射 |
| 角色属性 | ← 消费 | `CharacterAttributes.get_relative_strength(target): RelativeStrength` | 与NPC对比时调用 |
| 心境双轴 | ← 消费 | `MindsetSystem.get_mindset_zone(): MindsetZone` | 读取当前区域枚举 |
| 心境双轴 | ← 消费 | `MindsetSystem.get_echo_queue(): EchoEvent[]` | 获取待播放的内心独白 |
| 感情系统 | ← 消费 | `RomanceSystem.get_attitude_tier(npc_id): AttitudeTier` | 获取与特定NPC的态度档位 |
| 感情系统 | ← 消费 | `RomanceSystem.get_pending_comet_events(): CometEvent[]` | 获取待揭示的彗星事件 |
| 战斗 UI (#7) | → 交接 | `BlurredUI.on_combat_enter()` / `on_combat_exit()` | 模式切换信号 |
| 对话系统 (#5) | ← 消费 | `DialogueSystem.get_active_context(): UIContext` | 判断当前对话场景 |
| 存档系统 (#8) | → 提供 | `BlurredUI.get_save_state(): BlurredUISaveData` | 序列化 pending_reveals 队列 |

**所有交互均为只读消费或信号通知**——朦胧化 UI 不修改任何上游系统的数据。

## Formulas

### F1: 境界映射查表（引用自角色属性 GDD）

```
realm_name = REALM_TABLE[bisect_right(REALM_THRESHOLDS, total_power)]
```

**Variables:**
| Variable | Type | Range | Description |
|----------|------|-------|-------------|
| `total_power` | int | 0–999+ | 角色功力总值（来自 CharacterAttributes） |
| `REALM_THRESHOLDS` | int[] | [25, 35, 50, 70, 90, 115, 140, 170, 200] | 9级境界阈值（SSoT: character-attributes.md） |
| `REALM_TABLE` | string[] | 10 entries | 境界名数组（index 0 = 未入门，index 1–9 = 9 境界） |

**输出**：`realm_name: string` — 10 值之一（实际游戏中 power < 25 仅出现在极早期）：

| Tier | 名称 | total_power 区间 |
|------|------|-----------------|
| 0 | 未入门 | 0–24 |
| 1 | 初学乍练 | 25–34 |
| 2 | 初窥门径 | 35–49 |
| 3 | 登堂入室 | 50–69 |
| 4 | 融会贯通 | 70–89 |
| 5 | 驾轻就熟 | 90–114 |
| 6 | 炉火纯青 | 115–139 |
| 7 | 出神入化 | 140–169 |
| 8 | 登峰造极 | 170–199 |
| 9 | 返璞归真 | 200+ |

> ⚠️ 此映射表的 SSoT 为 `character-attributes.md`。本系统仅消费，不得修改阈值。

---

### F2: 心境色调偏移

```
tint_color = ZONE_TINT_TABLE[current_mindset_zone]
tint_intensity = base_intensity * (1.0 + zone_extremity * 0.3)
```

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `current_mindset_zone` | zone | MindsetZone | 9-value enum | 当前双轴区域（执念/释怀 × 入世/出世） |
| `base_intensity` | I₀ | float | 0.05–0.15 | 色调偏移基础强度（Tuning Knob） |
| `zone_extremity` | ext | float | 0.0–1.0 | 区域离中庸的距离（中庸=0, 四角=1） |

**ZONE_TINT_TABLE（待美术确认）：**

| Zone | 文学名 | 色调意象 | RGB 偏移方向 |
|------|--------|---------|-------------|
| 中庸 | 中庸 | 无偏移 / 自然色温 | (0, 0, 0) |
| 执念+入世 | 孤剑入世 | 暗铁红（复仇+涉世） | (+0.06, -0.02, -0.04) |
| 执念+中立 | 执念未定 | 深褐暖（执着但不极端） | (+0.03, -0.01, -0.02) |
| 执念+出世 | 风止尘湮 | 霜白冷（执念+避世=压抑冰冷） | (-0.02, -0.01, +0.04) |
| 中立+入世 | 入世未定 | 微暖（人间烟火） | (+0.02, +0.01, -0.01) |
| 中立+出世 | 出世未定 | 微冷（山林清气） | (-0.01, +0.01, +0.02) |
| 释怀+入世 | 白衣入世 | 明金暖（洒脱+入世=豁达） | (+0.04, +0.03, -0.01) |
| 释怀+中立 | 释怀未定 | 淡琥珀（放下但未选方向） | (+0.02, +0.02, 0) |
| 释怀+出世 | 大隐于市 | 烟青空灵（彻底超脱） | (-0.02, +0.03, +0.03) |

**关键约束**：善恶轴（morality）**不参与**色调偏移计算。善恶仅通过 World Echo / Narration Echo / Inner Monologue 文本内容（非视觉色调）体现。

**Output Range**: tint_intensity 在 0.05（中庸）至 0.195（四角极端）之间。

---

### F3: 缓冲队列合并规则

```
if pending_reveals[channel].count > MERGE_THRESHOLD:
    merged = summarize(pending_reveals[channel])
    pending_reveals[channel] = [merged]
```

**Variables:**
| Variable | Type | Range | Description |
|----------|------|-------|-------------|
| `MERGE_THRESHOLD` | int | 3 (default) | 触发合并的积压条数上限（Tuning Knob） |
| `channel` | enum | CH-1/CH-2/CH-3 | 当前 Channel |

**合并策略（per channel）**：
- CH-1（境界）: 只保留最终境界名（跳过中间变化）
- CH-2（心境）: 直接切到最新 zone（中间状态不回放）
- CH-3（关系）: 合并为一句摘要："最近发生了很多事…"

---

### F4: 相对境界判定

```
relative = sign(my_realm_tier - target_realm_tier)
```

| relative | 文学表达模板 |
|----------|------------|
| ≤ -3 | "此人深不可测，远非你所能窥" |
| -2 | "此人功力远在你之上" |
| -1 | "此人修为略胜于你" |
| 0 | "此人修为与你相当" |
| +1 | "此人修为不及你" |
| +2 | "此人远非你的对手" |
| ≥ +3 | "此人不过尔尔" |

## Edge Cases

- **If `total_power` 一次性跨越多个境界阈值**（如奇遇奖励）: 只显示最终到达的境界名，跳过中间境界。破境叙事文本使用"连破数境"变体模板而非逐级播放。

- **If 玩家在对话中触发 `mindset_shift` 导致 zone 切换**: 色调不在对话中途切换（避免干扰阅读）。zone 变化写入 pending_reveals[CH-2]，下次进入新场景时生效。

- **If 进入战斗时 pending_reveals 队列非空**: 所有 pending 揭示暂挂（frozen）。战斗结束回到 BLURRED 状态后，在 TRANSITION_TO_BLURRED 完成后按 FIFO 弹出。

- **If 战斗结算触发境界突破**: 例外于"事件驱动延迟"规则——结算画面直接展示破境叙事（因为结算本身就是一个 COMBAT_RESULT UIContext，属于 CH-1 的响应范围）。

- **If 两个 Channel 在同一 UIContext 激活时同时有 pending reveals**: 按 Channel 编号优先级依次播放：CH-1 > CH-2 > CH-3。每个 Channel 的揭示之间间隔 ≥0.5s，防止信息堆叠。

- **If `attitude_tier` 被 `force_break()` 覆写（决裂覆写）**: 无视正常的"下次相关事件时显示"规则——立即插入一段强制叙事演出（如 NPC 当面翻脸/拔剑/离去），不进 pending 队列。这是唯一绕过事件驱动延迟的路径。

- **If 存档/读档时 pending_reveals 队列有积压**: 队列内容随存档序列化。读档后恢复队列，下次对应 UIContext 激活时正常弹出。不丢弃任何未揭示的信息。

- **If `total_power` 恰好等于境界阈值**: 采用 `bisect_right` 语义——阈值本身属于新境界（即 power=20 时为 Tier 2"略窥门径"）。

- **If 玩家连续快速切换场景导致 CH-2 色调尚未渐变完成**: 中断当前渐变，直接跳到目标 zone 的色调值。不累加未完成的渐变（防止色调漂移）。

- **If 游戏处于 BLURRED 模式但 UI 需要显示精确数值**（如商店价格、物品数量）: 物品/经济数值不受朦胧化影响——朦胧化仅适用于角色成长/心境/关系三个 Channel 的数据。商店价格、背包数量等"世界客观事实"正常显示数字。

## Dependencies

### 上游依赖（本系统消费）

| 系统 | 硬/软 | 接口 | 用途 |
|------|-------|------|------|
| **角色属性 (#1)** | 硬 | `total_power`, `get_relative_strength()` | CH-1 境界文学化的唯一数据源 |
| **心境双轴 (#6)** | 硬 | `get_mindset_zone()`, `get_echo_queue()` | CH-2 色调偏移 + 内心独白的唯一数据源 |
| **感情系统 (#13)** | 硬 | `get_attitude_tier(npc_id)`, `get_pending_comet_events()` | CH-3 关系隐式表达的唯一数据源 |
| **对话系统 (#5)** | 软 | `get_active_context()` | 判断 UIContext（缺失时默认 EXPLORATION） |
| **存档系统 (#8)** | 软 | 序列化/反序列化钩子 | pending_reveals 持久化（缺失时重启丢失队列，不影响核心功能） |

### 下游被依赖（其他系统消费本系统）

| 系统 | 硬/软 | 接口 | 用途 |
|------|-------|------|------|
| **战斗 UI (#7)** | 软 | `on_combat_enter()` / `on_combat_exit()` 信号 | 通知模式切换（战斗 UI 自身可独立运作） |

### 双向一致性备注

- 角色属性 GDD 已定义 9 级境界映射表 → 本系统引用而非重定义
- 心境双轴 GDD 已声明"通过朦胧化 UI 的视觉线索（色调变化、文学描述）" → 本系统为其实现者
- 感情系统 GDD 已声明态度档位"完全隐式" → 本系统负责隐式表达机制
- 战斗 UI GDD 应声明"朦胧化 UI 不介入战斗 HUD" → 待验证

## Tuning Knobs

| Knob | 默认值 | 安全范围 | 过高风险 | 过低风险 | 交互影响 |
|------|--------|---------|---------|---------|---------|
| `base_tint_intensity` | 0.10 | 0.03–0.20 | 色调偏移过于明显，玩家察觉到"有颜色在变" | 色调差异太微弱，9 个 zone 视觉上无法区分 | 与 `zone_extremity_scale` 相乘 |
| `zone_extremity_scale` | 0.3 | 0.1–0.6 | 四角 zone 色调过强，中间 zone 无存在感 | 所有 zone 强度接近，位置差异模糊 | 与 `base_tint_intensity` 相乘 |
| `tint_transition_duration` | 2.0s | 0.5–5.0s | 切换太慢，玩家已离开场景仍在渐变 | 切换太快，突兀感明显 | — |
| `merge_threshold` | 3 | 2–6 | 队列过长才合并，导致信息洪水 | 太激进合并，玩家错过细节变化 | — |
| `reveal_interval` | 0.5s | 0.2–2.0s | 多条揭示间隔过长，节奏拖沓 | 间隔过短，信息叠加不可读 | 与 `merge_threshold` 配合 |
| `realm_description_variants` | 3 | 2–8 | 变体太多，玩家无法通过文字锚定状态 | 变体太少，感觉重复 | — |
| `relative_strength_threshold` | 境界差 ±1 | ±1–±2 | 阈值过大则"相当"范围过宽，比较失去意义 | 阈值过小则频繁出现差异文字 | — |
| `force_break_bypass` | true | bool | 关闭后决裂不立即演出，可能造成叙事断裂 | — | 感情系统的 `force_break()` |

**调优提示**：
- `base_tint_intensity` 和 `zone_extremity_scale` 必须联调——单独调高任一个都可能导致极端值超出玩家感知的"自然"范围
- `merge_threshold` 应与游戏推进速度匹配——如果一章内能触发 5+ 同类事件，threshold 需提高
- `reveal_interval` 的"最佳手感"需要 playtest 确认——文本类揭示和色调类揭示可能需要不同的间隔值

## Visual/Audio Requirements

### 视觉需求

**1. 色调偏移叠加层 (Tint Overlay)**
- 全屏半透明色彩叠加层，不影响 UI 文字可读性
- 9 种 zone 色调配色方案（参见 F2 ZONE_TINT_TABLE）
- 渐变切换动画：线性过渡，时长由 `tint_transition_duration` 控制
- 强度不超过 20% 不透明度（保证像素美术底色可见）

**2. 境界文字展示**
- 角色面板中境界名以书法风格字体呈现（区别于常规 UI 字体）
- "破境"触发时：文字以"墨迹晕染"动画展开（≤1.5s）
- 相对境界比较文字：以内心独白样式浮现（半透明底条 + 楷体）

**3. 内心独白 UI**
- 屏幕上方或下方浮现文字条（区别于对话框）
- 字体：模拟手写楷体 / 行书
- 出现方式：逐字渐显（非弹出），伴随轻微透明度波动
- 自动消失时长：基于文字长度，约 3–5s

**4. 环境叙事提示**
- 传闻/书信/窃语以"信纸"或"旁白"UI 元素出现在屏幕边缘
- 不遮挡核心画面，玩家可忽略或点击展开

### 音效需求

**5. 色调切换反馈**
- zone 渐变切换时：一声极轻微的环境音变化（如风声调性微调）
- 不需要明显 SFX，目标是"无意识感知"

**6. 破境反馈**
- 破境时："丹田气海涌动"的低沉冲击音 + 清越剑鸣
- 时长 ≤1s，与墨迹动画同步

**7. 决裂演出音效**
- `force_break()` 触发时的强制叙事演出：由具体演出内容决定（拔剑声/摔门声/脚步远去等），不在本系统统一定义——由叙事事件配置

## UI Requirements

### 组件清单

| 组件 | 位置 | 触发条件 | 交互方式 |
|------|------|---------|---------|
| **Tint Overlay** | 全屏最底 UI 层 | 始终激活（BLURRED 模式下） | 无交互，纯视觉 |
| **境界标签** | 角色面板 · 姓名下方 | 面板打开时 | 无交互，hover 显示境界描述 tooltip |
| **破境演出** | 屏幕中央 | pending 破境 + 面板打开 | 自动播放，点击任意处跳过 |
| **内心独白条** | 屏幕顶部/底部（可配置） | CH-2 pending reveal 弹出时 | 自动消失；点击加速消失 |
| **环境叙事卡** | 屏幕右侧边缘 | CH-3 pending reveal 弹出时 | 点击展开全文；滑动/忽略自动收起 |
| **相对境界提示** | NPC 头像旁 / 对话框内 | COMPARISON context 激活时 | 无交互，2s 后淡出 |

### 布局规则

- 内心独白条与环境叙事卡**不同时显示**（reveal_interval 保证间隔）
- 所有朦胧化 UI 组件的 z-order **低于**对话框和系统菜单
- 战斗模式下所有朦胧化组件**立即隐藏**（opacity → 0，不 destroy）
- 移动端/手柄适配：环境叙事卡增加"提示图标"模式（缩小为图标，点击展开）

### 无障碍考虑

- 色调偏移可在设置中关闭（fallback 为纯文字提示当前 zone 名）
- 内心独白支持字体大小调节（跟随全局 UI 缩放）
- 破境动画可设为"简洁模式"（跳过墨迹动画，直接显示文字）

## Acceptance Criteria

### 分治规则

- **GIVEN** 玩家处于非战斗场景, **WHEN** 查看角色面板, **THEN** 功力以境界文字显示（如"炉火纯青"），无数值可见。
- **GIVEN** 玩家进入战斗, **WHEN** 战斗 HUD 激活, **THEN** HP/伤害/心法效果以精确数值显示；朦胧化 UI 组件全部不可见。
- **GIVEN** 战斗结束, **WHEN** 结算画面消失, **THEN** ≤1s 内恢复朦胧化模式，色调叠加重新出现。

### 境界映射 (CH-1)

- **GIVEN** 角色 `total_power` = 180, **WHEN** 打开角色面板, **THEN** 显示"登峰造极"。
- **GIVEN** 角色从 `total_power` 49 → 55（跨越 Tier 2→3 阈值）, **WHEN** 下次打开面板, **THEN** 播放"破境"墨迹动画 + 显示"登堂入室"。
- **GIVEN** 角色从 `total_power` 40 → 120（跨越多个阈值）, **WHEN** 下次打开面板, **THEN** 播放"连破数境"变体，最终显示"炉火纯青"。

### 心境色调 (CH-2)

- **GIVEN** 玩家心境 zone 为"孤剑入世", **WHEN** 进入新场景, **THEN** 屏幕色调在 `tint_transition_duration` 内渐变为暗铁红偏移。
- **GIVEN** 玩家在对话中触发 zone 切换, **WHEN** 对话仍在进行, **THEN** 色调不变；对话结束进入下一场景时才切换。

### 关系隐式表达 (CH-3)

- **GIVEN** NPC-A 的 `attitude_tier` 从"友善"降为"疏远", **WHEN** 下次与 NPC-A 相关事件触发, **THEN** 通过环境叙事卡或对话语气变化体现（无数字提示）。
- **GIVEN** `force_break()` 被调用, **WHEN** 立即, **THEN** 强制叙事演出触发，不进入 pending 队列。

### 缓冲队列

- **GIVEN** CH-2 的 pending_reveals 积压 4 条（> MERGE_THRESHOLD=3）, **WHEN** 对应 UIContext 激活, **THEN** 只播放 1 条合并后的揭示（直接切到最新 zone）。
- **GIVEN** 玩家存档时 pending_reveals 有 2 条, **WHEN** 读档, **THEN** 2 条均恢复，下次 UIContext 激活时正常弹出。

### 相对境界

- **GIVEN** 玩家 Tier=4, 目标 NPC Tier=7, **WHEN** COMPARISON context 激活, **THEN** 显示"此人深不可测，远非你所能窥"。

### 性能预算

- Tint Overlay 渲染：≤ 0.5ms/frame（单个全屏 ColorRect）
- pending_reveals 队列查询：≤ 0.1ms/frame（无 UI 渲染时不 tick）

### 无障碍

- **GIVEN** 玩家在设置中关闭色调偏移, **WHEN** 进入任何场景, **THEN** 无色调叠加；zone 名以文字提示替代。

## Open Questions

| # | 问题 | 负责人 | 目标解决时间 |
|---|------|--------|------------|
| 1 | ZONE_TINT_TABLE 的 RGB 偏移值需美术实机调色确认 | Art Director | Art Bible 完成后 |
| 2 | 内心独白条位置（顶部 vs 底部）需 UX playtest 验证 | UX Designer | Vertical Slice |
| 3 | 环境叙事卡在不同分辨率下的响应式布局细节 | UI Programmer | 实现阶段 |
| 4 | 战斗 UI GDD 是否需要补充"朦胧化 UI 不介入战斗 HUD"的声明 | Designer | 下次 consistency-check |
| 5 | 破境动画的具体 VFX 参考（墨迹晕染 shader 可行性）| Technical Artist | 技术验证阶段 |
