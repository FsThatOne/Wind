# 行气战棋战斗（Xingqi Tactics）

> **Status**: Designed
> **Author**: user + game-designer + systems-designer
> **Last Updated**: 2026-06-16
> **Implements Pillar**: Pillar 1 — 一读定生死（核心战斗幻想）

## Overview

战斗系统是《风止》的核心战术体验。它不再采用旧版 Burst+Read 的“同步声明、同时结算、反制按钮”模型，而改为**行气驱动的轻量武侠战棋**：最多 5 名玩家方角色在棋盘上与敌人周旋，每个角色通过“行气条”积累出手机会，行气满后执行一次“移动 + 出招”。

新版战斗的核心循环是：**观气 → 取位 → 出招 → 破绽 → 决胜**。玩家观察敌人当前显露的内功气机，而不是读取下一招名称；根据刚 / 柔 / 巧三种内劲气质选择合适招式；通过移动、朝向、侧背击、招式范围和位移招制造“破绽大开”；最终在关键窗口发动一击决胜。

战斗保留三项核心资源：**气血、内息、破绽**。气血决定生死，内息限制招式与调息节奏，破绽表示敌我防守、气息和身形被打开的程度。刚 / 柔 / 巧不再是简单属性颜色，而是武学内劲气质：刚猛破巧、巧变破柔、柔化破刚。克制不再通过独立“反制”按钮触发，而是在合适身位与招式命中时自然转化为伤害、破绽累积或行气干扰收益。

本系统的目标是让玩家感到自己不是在管理冷却表或猜拳，而是在真正进行一场武侠对决：看对手气机，踏步入门，抓住一线空隙，一招定胜负。它向角色属性读取气血、内息、敏捷、攻击、防御等数值；向武学组合读取招式体系、范围、位移、消耗和特殊效果；向敌方 AI 提供棋盘状态与当前气机接口；向战斗 UI 输出行气、身位、破绽、气机和决胜窗口等信息。

## Player Fantasy

**“我不是比他更快按下招式，而是先看出他的气，踏进他的门，再让这一招落在该落的地方。”**

新版战斗的幻想不是旧版“预读下一招后按下反制”，而是更完整的武侠交锋：玩家先观察敌人的当前气机，判断此人此刻是刚劲外露、柔劲回护，还是巧劲游走；再通过步法、轻功、侧背身位和招式范围找到入门之处；最后用合适的内劲气质逼出破绽，在破绽大开的一瞬间发动决胜。

玩家应该感觉自己是在“行棋”，但不是冰冷的棋局，而是一场有呼吸、有身法、有气势流动的对决。一个好的回合不是“我选择了克制颜色”，而是“我看出他刚劲太盛，于是绕至侧身，以柔招卸力，逼他破绽半开；下一次行气轮到我时，再用一式穿身招贴近，终于让他露出死门”。

战斗的高光来自三层递进：

1. **观气**：敌人不会把完整行动计划告诉玩家，只显露当前内功气机。玩家通过多轮观察读出招路习惯，而不是背固定答案。
2. **取位**：移动不是走格子凑距离，而是武侠里的步法。侧击、背击、穿身、后撤、拉开和贴近都应让玩家感到“我站到了该站的位置”。
3. **决胜**：破绽大开不是普通眩晕条满，而是对手的防守、气息和身形同时露出一线空隙。玩家按下决胜一击时，应该觉得这不是大招冷却好了，而是“这一刻终于到了”。

理想体验是：玩家战后不说“我算赢了”，而说“我看懂了他的路数”。战斗胜利应来自观察、取位、内息节奏和出招时机的合一，而不是数值碾压或固定最优解。

## Detailed Design

### Core Rules

**1. 战斗触发与初始化**

- 战斗通过叙事事件、场景遭遇、Boss 关卡或特殊剧情条件触发。
- 战斗采用方格小棋盘。棋盘规模由关卡配置决定，MVP 建议控制在 8x8 到 12x12，保证移动、侧背击和招式范围可读。
- 玩家方上阵人数最多 5 人，主角必须上阵；可同行人数不足时允许少于 5 人进入战斗。
- 敌方人数不设硬上限，但普通战斗建议 1-6 名敌人；超过 6 名敌人时必须通过分批入场、棋盘布局或遭遇节奏控制信息量。敌人头顶状态不做差异化折叠，无论人数多少都完整展示。
- 初始化时：
  - 所有参战角色气血设为当前气血。
  - 内息设为当前内息，上限来自角色属性。
  - 破绽值设为 0。
  - 行气值可由关卡配置决定，默认从 0 开始。
  - 每个角色载入战前配置的招式、内功、轻功、战斗背包。
  - 每个角色从已装备内功中选定当前运转内功；当前内功决定该角色显露的气机属性。

**2. 战棋空间规则**

- 棋盘几何：**isometric diamond 投影**（菱形 tile，2:1 宽高比）。详见 [ADR-0022](../../docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md)。
- 逻辑坐标仍是 `(int X, int Y)` 方格；渲染层投影为屏幕菱形。
- 每个角色占据 1 格。
- 角色朝向 4 斜方向：屏幕 NE / SE / SW / NW（对应逻辑 X+ / Y+ / X- / Y-）。
- 不做高低差。
- 不做 ZOC。
- 障碍物只影响移动与部分招式视线/路径，不引入复杂地形高度。
- 角色出招后默认面向主要目标。
- 若本次行动没有攻击目标，则默认面向移动结束方向。

**3. 行气系统**

战斗不再按“整回合”同步结算。每个角色拥有独立行气条。

```text
每个战斗脉冲：
  -> 所有可行动角色增加行气值
  -> 若一个或多个角色行气值 >= 100，进入行动队列
  -> 行气值最高者先行动
  -> 若并列：玩家方优先；同阵营按敏捷高者优先；仍并列按站位从左到右、从上到下
```

- 行气速度只受敏捷与明确轻功词条影响。
- 洞察不影响行气速度。
- 行气满的角色获得一次行动。
- 行动结束后，该角色行气值回落到 0，重新积累。
- 特殊状态可暂停、降低或提高行气，但必须通过明确 buff/debuff 或轻功词条声明。
- 行气条是战斗 UI 的核心信息之一，应显示在单位头顶或选中单位详情中。

**4. 行动结构**

角色行气满后，执行一次行动。标准行动结构为：

```text
1. 选择移动
2. 移动到合法格
3. 选择出手动作
4. 结算动作
5. 行动结束，行气回落
```

出手动作包括：

| 动作 | 是否造成伤害 | 是否占用出手 | 说明 |
|---|---|---|---|
| 使用招式 | 是/视招式而定 | 是 | 从装备招式中选择，消耗内息，按范围和目标结算 |
| 切换内功 | 否 | 是 | 移动后切换当前运转内功，改变自身气机，有 CD |
| 使用轻功 | 通常否 | 是 | 移动后发动轻功，提供特殊位移与小效果，有 CD |
| 调息 | 否 | 是 | 回复内息，并获得短时防护或行气稳定效果 |
| 使用道具 | 视道具而定 | 是 | 从战斗背包消耗道具 |
| 决胜一击 | 是 | 是 | 目标破绽大开时可发动，造成高伤害和专属演出 |

角色必须先完成移动选择，再选择出手动作。若玩家不想移动，可选择原地停留。

**4.1 师门尺法战斗约束**

风止山庄的起手武学以 `ruler` 类武器为默认承载，叙事表现为玉尺或铁尺。该规则服务“出世不攻”的门派侧写，不是全游戏武器限制。

| 武器 / 武学 | 战斗定位 | 平衡约束 |
|---|---|---|
| `jade_ruler` | 主角序章默认武器，低杀伤、重守势与巧劲 | 伤害倍率不应高于同品级铁尺；可给予柔/巧招式命中稳定、破绽控制或防护收益 |
| `iron_ruler` | 同类武器的重型版本，更适合实战压迫 | 可提高攻击或破绽收益，但不得把风止尺法变成刚系爆发流 |
| 风止尺法 | 柔/巧两系招式，无刚 | 起手阶段必须存在体系缺口，推动玩家后续入江湖补齐刚系武学 |

尺法招式的战棋表达优先使用短距、牵引、隔开、绕身、封穴和借力打力：
- 柔系尺招偏向防护、拉开距离、降低自身破绽、打断敌方压迫路线。
- 巧系尺招偏向侧身入门、封穴、改变朝向、累积目标破绽。
- 尺法默认不提供大范围杀伤；若后期出现高阶尺法，必须由武学数据显式声明范围和代价。
- 玉尺的演出应克制、点到即止；铁尺可更有重量，但仍不同于刀剑劈砍。
- 主角后续可装备剑、刀等其他武器；战斗系统只按当前招式数据和武器标签结算，不把“剑”作为主角默认武器。

**5. 多内功与气机**

每个可玩角色战前配置：

- 1 个主内功槽。
- 2 个备用内功槽。
- 战斗开始时默认运转主内功。
- 战中可在已装备内功之间切换。
- 每种内功的气机属性固定不变：刚、柔、巧之一。
- 当前运转内功决定角色头顶显露的当前气机。
- 切换内功占用本次出手，但允许先移动再切换。
- 切换内功有冷却，默认 `inner_power_switch_cooldown = 2` 次该角色自然行动。
- 切换内功不直接造成伤害，但可能触发内功入场效果、短时防护、回息或气机变化。

设计意图：气机不再是敌方下一招预告，而是角色正在运转的内功状态。玩家看见的不是“他下一招是什么”，而是“他此刻以内功护住了哪一门”。

> **Cross-GDD Sync Status**：`martial-arts-system.md` 已同步为 1 主内功 + 2 备用内功槽，并定义招式 `qi_type`、内功固定气机与轻功词条契约。

**6. 轻功动作**

轻功是独立武学类，战前装备 1 个轻功。

- 使用轻功占用本次出手。
- 轻功通常不造成直接伤害。
- 轻功主要提供特殊移动与小效果：
  - 增加本次移动距离。
  - 穿越敌方身侧或障碍。
  - 调整朝向。
  - 获得短时闪避、防护、行气收益或破绽小幅收益。
- 轻功有冷却，默认 `qinggong_cooldown = 3` 次该角色自然行动。
- 只有明确写有 `xingqi_bonus_percent` 的轻功词条才影响行气增长；普通轻功不默认改变行动频率。

设计意图：轻功不是“移动按钮”，而是一次有代价的身法选择。使用轻功应让玩家获得更好的位置或安全性，但牺牲本次直接出招。

**7. 刚 / 柔 / 巧气机克制**

刚 / 柔 / 巧是内劲气质，不是元素属性。

```text
刚破巧
巧破柔
柔破刚
```

克制判定发生在招式命中时：

- 攻方招式体系克制目标当前气机：攻方获得克制收益。
- 攻方招式体系被目标当前气机克制：攻方收益降低，并可能暴露自身破绽。
- 同系：正常结算。

默认收益：

| 情况 | 伤害倍率 | 破绽变化 |
|---|---:|---:|
| 克制当前气机 | x1.3 | 目标破绽 +2 |
| 被当前气机克制 | x0.7 | 攻方破绽 +1 |
| 同系 | x1.0 | 无基础破绽变化 |

克制不需要独立“反制”按钮。只要招式命中并满足体系关系，即自然结算。

**8. 身位与朝向**

侧背击是破绽累积的第二主要来源。

| 攻击角度 | 伤害倍率 | 破绽变化 |
|---|---:|---:|
| 正面 | x1.0 | 无额外变化 |
| 侧面 | x1.1 | 目标破绽 +1 |
| 背面 | x1.2 | 目标破绽 +2 |

- 侧背击收益可与气机克制叠加。
- 侧背击加成应保持轻量，不得超过气机克制在战术中的主导地位。
- UI 必须在目标选择时预览正/侧/背结果，避免玩家误判。
- 角色出招后默认面向目标，防止背击收益无脑滚雪球。

**9. 破绽系统**

破绽是战斗中的可累积破防状态。

- 破绽初始值为 0。
- 破绽阈值默认 5。
- 破绽达到或超过阈值时，目标进入“破绽大开”状态。
- 破绽主要来源：
  - 克制当前气机。
  - 侧击 / 背击。
  - 招式特殊效果。
  - 轻功小效果。
  - 特定内功切换效果。
- 破绽衰减默认在角色自身行动结束时检查：
  - 若本次行动中该角色未受到任何破绽增加，则破绽 -1。
  - 最低为 0。
- 破绽不是眩晕。破绽大开的角色仍可行动，除非具体招式或状态另有控制效果。
- 破绽达到阈值后不会立即清空，直到被决胜一击消费或通过规则衰减。

设计意图：破绽表现的是防守门路、气息和身形被逐步撬开，而不是通用 stun bar。

**10. 决胜一击**

当目标破绽 >= 5 时，任一可合法攻击该目标的角色可选择发动决胜一击。

- 决胜一击占用出手。
- 决胜一击需要目标在招式范围或决胜范围内。
- 默认伤害倍率为 x2.0。
- 决胜一击无视破绽带来的额外破绽累积，只消费当前破绽状态。
- 成功命中后，目标破绽清空为 0。
- 决胜一击应触发专属演出、音效和镜头强调。
- Boss 可拥有决胜抗性或阶段规则，但必须显式写在 Boss 配置中。

设计意图：决胜一击不是冷却大招，而是对“破绽大开”的收束。玩家应感觉自己终于等到那个可以落手的瞬间。

**11. 内息与调息**

内息限制招式频率和连续压迫。

- 使用招式消耗内息。
- 切换内功默认不消耗内息，但占用出手并有 CD。
- 使用轻功可消耗内息或不消耗，取决于轻功定义。
- 调息占用出手，回复内息。
- 调息默认效果：
  - 回复 `meditation_recovery` 内息。
  - 本角色下一次受到攻击前防御 +20%。
  - 若原地调息，可额外获得破绽衰减 +1 或清除 1 点自身破绽，具体作为 tuning knob。

**12. 多人战斗规则**

- 玩家控制所有己方上阵角色。
- 每个角色独立行气，独立行动。
- 多名己方角色可以围攻同一目标。
- 协同不再基于“同回合同目标”，而基于连续压迫：
  - 若同一目标在其下一次行动前被两名及以上己方角色造成破绽增加，则触发“围势”提示。
  - 围势默认额外破绽 +1，每个目标每个行气周期最多触发一次。
- 敌方也可以围攻玩家角色，但敌方围势收益需谨慎使用，MVP 可只做视觉/警告，不给额外数值。

**13. 战斗结束条件**

| 条件 | 结果 |
|---|---|
| 敌方全部气血归零 | 玩家胜利 |
| 玩家方全部气血归零 | 战斗失败，按叙事处理，非 Game Over |
| 特殊剧情条件达成 | 剧情中断战斗 |
| Boss 阶段条件达成 | 进入阶段转换，不一定结束战斗 |

旧版固定 15 回合上限不再作为通用规则。若某些剧情战需要限时，应以关卡目标形式定义，例如“撑过 X 次敌方行动”或“在 Y 次主角行动内抵达出口”。

### States and Transitions

| 状态 | 说明 | 进入条件 | 退出条件 |
|---|---|---|---|
| 战斗外 | 非战斗状态 | 默认 | 触发战斗 |
| 战斗初始化 | 加载棋盘、角色、资源、内功、行气 | 战斗触发 | 初始化完成 |
| 行气推进 | 所有角色随战斗脉冲积累行气 | 初始化完成或行动结束 | 任一角色行气 >= 100 |
| 行动选择 | 行气满角色选择移动与出手 | 行气达到阈值 | 玩家/AI 确认行动 |
| 移动结算 | 角色移动到目标格 | 行动选择后 | 移动完成 |
| 出手结算 | 招式/内功/轻功/调息/道具/决胜生效 | 移动完成 | 效果完成 |
| 破绽检查 | 更新破绽、判断破绽大开与决胜可用 | 出手结算后 | 检查完成 |
| 行动结束 | 行气回落、CD 推进、状态持续时间更新 | 破绽检查后 | 回到行气推进或战斗结束 |
| 战斗结束 | 胜利、失败或剧情中断 | 结束条件满足 | 返回战斗外 |

### Interactions with Other Systems

| 系统 | 数据方向 | 接口/数据 | 说明 |
|---|---|---|---|
| 角色属性 | 属性 -> 战斗 | `hp`, `max_hp`, `neixi`, `agility`, `attack`, `defense`, `crit_rate` | 战斗读取基础与派生数值 |
| 角色属性 | 战斗 -> 属性 | `ApplyDamage`, `SpendNeiXi`, `RestoreNeiXi`, `AddStagger`, `ClearStagger` | 战斗写入临时资源状态 |
| 武学组合 | 武学 -> 战斗 | equipped moves, inner powers, qinggong | 读取招式、内功、轻功配置 |
| 武学组合 | 战斗 -> 武学 | cooldown tick, usage event | 推进内功/轻功/招式冷却 |
| 敌方 AI | 战斗 -> AI | board state, actor state, current qi, stagger values | AI 根据棋盘与气机决策 |
| 敌方 AI | AI -> 战斗 | movement target, action choice, selected move/inner power/qinggong | AI 输出行动 |
| 战斗 UI | 战斗 -> UI | xingqi, board highlights, qi, stagger, damage, decisive window | UI 展示战棋状态 |
| 战斗 UI | UI -> 战斗 | selected cell, selected action, target | 玩家输入 |
| 顿悟突破 | 战斗 -> 顿悟 | battle pressure, near defeat, decisive events | 触发顿悟判定 |
| 物品系统 | 物品 -> 战斗 | combat pouch | 读取战斗背包 |
| 队伍管理 | 队伍 -> 战斗 | deployed party | 读取上阵角色 |
| 心境双轴 | 战斗 -> 心境 | battle result, mercy/kill tags, decisive usage tags | 战斗结果可触发心境位移 |
| 存档系统 | 战斗 -> 存档 | pre-battle autosave only | 战斗中不允许手动存档 |

## Formulas

### Resolution Order

一次出手按固定顺序结算，所有实现和测试必须遵守同一顺序：

```text
1. Validate action legality
2. Pay action cost
3. Resolve movement
4. Resolve hit / target legality
5. Calculate base damage
6. Apply qi matchup multiplier
7. Apply facing multiplier
8. Apply critical multiplier
9. Apply damage variance
10. Clamp final damage
11. Apply damage
12. Apply stagger delta
13. Apply move special effects
14. Check decisive window
15. Tick cooldowns and action-end state
```

### F1. 行气增长

```text
xingqi_gain = base_xingqi_gain * agility_factor * qinggong_xingqi_multiplier * status_xingqi_multiplier
```

| Variable | Default / Range | Source | Description |
|---|---:|---|---|
| `base_xingqi_gain` | 20 | Combat tuning | 每个战斗脉冲的基础行气增长 |
| `agility_factor` | 0.75-1.35 | Character attributes | 敏捷相对章节基线的归一化结果 |
| `qinggong_xingqi_multiplier` | 0.85-1.30 | Equipped qinggong terms | 只有明确 `xingqi_bonus_percent` 的轻功词条参与 |
| `status_xingqi_multiplier` | 0.00-1.50 | Buff/debuff | 控制迟滞、加速、定身等状态 |

```text
agility_factor = clamp(agility / chapter_baseline_agility, 0.75, 1.35)

qinggong_xingqi_multiplier = clamp(
  1.00 + sum(qinggong_xingqi_bonus_percent),
  0.85,
  1.30
)
```

洞察不参与行气增长。洞察只能进入气机识破、敌方信息深度、探索隐藏线索和对话洞察。

### F2. 行气入队、行动顺序与行动后回落

```text
if actor.xingqi >= xingqi_threshold:
  enqueue(actor)

action_order = sort(
  ready_actors,
  by xingqi desc,
  then side_priority,
  then agility desc,
  then board_reading_order asc
)
```

| Variable | Default | Description |
|---|---:|---|
| `xingqi_threshold` | 100 | 达到或超过后进入行动队列 |
| `side_priority` | player before enemy | 同行气值时玩家方优先，降低并列不确定性 |
| `board_reading_order` | top-to-bottom, left-to-right | 最终稳定排序规则 |

行动结束后默认行气归零：

```text
actor.xingqi = 0
```

部分内功或轻功可通过明确词条保留部分行气：

```text
retained_xingqi = xingqi_threshold * retained_xingqi_percent
actor.xingqi = min(retained_xingqi, xingqi_threshold * retained_xingqi_cap)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `retained_xingqi_percent` | by inner power / qinggong term | 只有明确词条声明时生效 |
| `retained_xingqi_cap` | 0.30 / 0.00-0.30 | 行动后最多保留 30% 行气 |

没有保留词条时，不保留溢出行气。任何提高行动频率的设计必须通过敏捷、明确轻功行气词条或行气保留词条表达。

### F3. 有效攻击力

```text
effective_attack = attack_for_type * move.base_multiplier * move.completion * realm_scaling
```

| Variable | Source | Description |
|---|---|---|
| `attack_for_type` | Character attributes | 刚用力量系攻击，柔用内力系攻击，巧用敏捷系攻击 |
| `move.base_multiplier` | Martial move data | 招式基础倍率 |
| `move.completion` | Martial arts system | 残卷/拓本/完本/真传等完整度 |
| `realm_scaling` | Martial arts system | 境界缩放系数 |

### F4. 基础伤害

```text
base_damage = max(min_damage, effective_attack - defense)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `min_damage` | 1 / 0-3 | 防止完全不破防导致无反馈 |
| `defense` | from attributes | 目标防御力 |

### F5. 招式属性与气机克制倍率

每个招式都有固定招式属性：刚、柔、巧之一。每个正在运转的内功也有固定气机属性：刚、柔、巧之一。

克制判定使用：

```text
attacker_move.qi_type vs target.current_inner_power.qi_type
```

刚 / 柔 / 巧是内劲气质，不是元素属性。

```text
刚破巧
巧破柔
柔破刚
```

```text
qi_multiplier =
  1.30 if attacker_move.qi_type counters target.current_qi
  0.70 if target.current_qi counters attacker_move.qi_type
  1.00 otherwise
```

| Matchup | Damage Multiplier | Stagger Delta |
|---|---:|---:|
| 招式属性克制目标当前气机 | 1.30 | target stagger +2 |
| 招式属性被目标当前气机克制 | 0.70 | attacker stagger +1 |
| 同系或无关系 | 1.00 | 0 |

克制不看敌方下一招名称，也不看攻方当前内功气机。攻方当前内功可通过 buff、招式可用性或特殊效果影响战斗，但不替代招式自身的刚/柔/巧属性。

### F6. 身位倍率

```text
facing_multiplier =
  1.20 if attack_angle == back
  1.10 if attack_angle == side
  1.00 if attack_angle == front
```

| Attack Angle | Damage Multiplier | Stagger Delta |
|---|---:|---:|
| Front | 1.00 | 0 |
| Side | 1.10 | target stagger +1 |
| Back | 1.20 | target stagger +2 |

身位收益可与气机克制叠加，但不可超过气机克制的战术主导地位。

### F7. 暴击与伤害波动

```text
crit_multiplier = 1.50 if crit_roll_success else 1.00
variance_multiplier = random(0.95, 1.05)
```

```text
final_damage = round(
  base_damage
  * qi_multiplier
  * facing_multiplier
  * crit_multiplier
  * variance_multiplier
)
```

```text
final_damage = clamp(final_damage, min_damage, max_single_hit_damage)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `crit_multiplier` | 1.50 / 1.20-2.00 | 暴击倍率 |
| `damage_variance` | +/-5% / +/-2%-10% | 小幅波动，避免伤害完全机械 |
| `max_single_hit_damage` | no default cap | 若 Boss 或教学需要防秒杀，由关卡/Boss 配置声明，不在通用公式硬钳位 |

### F8. 破绽变化

```text
stagger_delta_to_target =
  qi_stagger_delta_to_target
  + facing_stagger_delta_to_target
  + move.stagger_delta_to_target
  + qinggong_stagger_delta_to_target
  + formation_stagger_delta_to_target

stagger_delta_to_attacker =
  qi_stagger_delta_to_attacker
  + move.stagger_delta_to_attacker
```

```text
target.stagger = clamp(target.stagger + stagger_delta_to_target, 0, target.stagger_threshold)
attacker.stagger = clamp(attacker.stagger + stagger_delta_to_attacker, 0, attacker.stagger_threshold)
```

| Source | Default Delta |
|---|---:|
| 克制目标当前气机 | target +2 |
| 被目标当前气机克制 | attacker +1 |
| 侧击 | target +1 |
| 背击 | target +2 |
| 围势 | target +1 |
| 招式特殊效果 | by move data |
| 轻功特殊效果 | by qinggong data |
| 内功切换入场效果 | by inner power data |

破绽阈值默认：

```text
stagger_threshold = 5
```

Boss 可覆盖 `stagger_threshold`，但必须在 Boss 配置中显式声明。

### F9. 破绽衰减

破绽不是眩晕条。破绽大开后仍可行动。

```text
if actor.received_stagger_delta_since_last_action == false:
  actor.stagger = max(0, actor.stagger - stagger_decay_on_own_action_end)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `stagger_decay_on_own_action_end` | 1 / 0-2 | 自身行动结束时的自然回稳 |
| `stagger_threshold` | 5 / 3-8 | 进入破绽大开的阈值 |

破绽达到阈值后不自动清空，只有决胜一击、特定状态或衰减规则会降低破绽。

### F10. 围势

```text
if target.received_stagger_delta_from_distinct_player_actors >= 2
  and target.formation_bonus_used_this_xingqi_cycle == false:
    target.stagger += formation_stagger_bonus
    target.formation_bonus_used_this_xingqi_cycle = true
```

| Variable | Default / Range | Description |
|---|---:|---|
| `formation_stagger_bonus` | 1 / 0-2 | 多人连续压迫同一目标的额外破绽收益 |

围势不是“同回合协同”。它基于目标下一次自然行动前受到的连续压迫。

### F11. 决胜一击

```text
can_decisive_strike =
  target.stagger >= target.stagger_threshold
  and target.hp > 0
  and attacker.can_act
  and target within decisive_range
```

```text
decisive_damage = round(base_damage * decisive_strike_multiplier)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `decisive_strike_multiplier` | 2.00 / 1.50-3.00 | 决胜一击倍率 |
| `decisive_range` | move range or 1 | 可由招式/角色配置覆盖 |

决胜一击命中后：

```text
target.stagger = 0
```

默认不叠加气机、身位、暴击或破绽额外收益。若特定武学允许叠加，必须在该武学数据中显式声明。

### F12. 内息消耗与调息

招式消耗：

```text
can_use_move = actor.neixi >= move.neixi_cost
actor.neixi = actor.neixi - move.neixi_cost
```

调息：

```text
meditation_recovery = ceil(neixi_recovery * meditation_efficiency)
actor.neixi = min(actor.max_neixi, actor.neixi + meditation_recovery)
```

| Variable | Default / Range | Description |
|---|---:|---|
| `meditation_efficiency` | 0.50 / 0.30-0.80 | 调息回复效率 |
| `meditation_defense_bonus` | 20% / 0%-40% | 调息后的短时防护 |
| `meditation_stagger_recovery` | 1 / 0-2 | 原地调息可恢复自身破绽，作为可调项 |

调息占用出手，不造成伤害。

### F13. 内功切换冷却

```text
can_switch_inner_power =
  selected_inner_power in equipped_inner_powers
  and inner_power_switch_cd_remaining == 0
```

切换后：

```text
actor.current_inner_power = selected_inner_power
actor.current_qi = selected_inner_power.qi_type
actor.inner_power_switch_cd_remaining = inner_power_switch_cooldown
```

| Variable | Default / Range | Description |
|---|---:|---|
| `inner_power_switch_cooldown` | 2 natural actions / 1-4 | 内功切换冷却 |
| `equipped_inner_powers` | 1 main + 2 backup | 战前配置 |

切换内功占用出手，但允许先移动再切换。

### F14. 轻功冷却

```text
can_use_qinggong =
  equipped_qinggong != null
  and qinggong_cd_remaining == 0
  and actor.neixi >= qinggong.neixi_cost
```

使用后：

```text
actor.neixi -= qinggong.neixi_cost
actor.qinggong_cd_remaining = qinggong_cooldown
```

| Variable | Default / Range | Description |
|---|---:|---|
| `qinggong_cooldown` | 3 natural actions / 1-5 | 轻功动作冷却 |
| `qinggong.neixi_cost` | by qinggong data | 轻功内息消耗 |

轻功占用出手。普通轻功不默认影响行气，只有明确 `xingqi_bonus_percent` 的词条才进入 F1。

### F15. 冷却推进

```text
on_actor_natural_action_end(actor):
  inner_power_switch_cd_remaining = max(0, inner_power_switch_cd_remaining - 1)
  qinggong_cd_remaining = max(0, qinggong_cd_remaining - 1)
  tick_move_cooldowns(actor)
```

“自然行动”指该角色因自身行气满而获得的行动。被动反应、受击、演出、剧情插入不推进冷却。

## Edge Cases

- **如果多个角色同时行气达到 100**：全部进入行动队列，按 `xingqi desc -> side_priority -> agility desc -> board_reading_order` 排序。排序结果必须稳定，同一输入状态每次得到相同顺序。
- **如果角色行气超过 100**：默认不保留溢出行气，行动结束后归零。只有明确内功或轻功词条可保留部分行气，且最多保留 30%。
- **如果行气满的角色在行动前被击败**：从行动队列移除，不执行移动、出招、调息、内功切换或轻功动作。
- **如果行动角色移动后目标不再合法**：玩家方必须重新选择合法目标或取消出手动作；AI 必须重新评估目标。若没有任何合法出手动作，角色可选择调息、使用道具或原地结束出手动作。
- **如果移动路径被其他角色或剧情障碍阻断**：该格视为不可达。路径预览必须提前排除不可达格，不能等确认后才失败。
- **如果招式命中多个目标**：每个目标独立计算气机克制、身位、伤害、破绽变化和决胜窗口。一次多目标招式不能对多个目标同时发动决胜一击，除非招式数据明确声明。
- **如果目标在多段招式中途气血归零**：后续段数不再对该目标造成伤害，但可继续命中其他合法目标。目标落败后不再继续累积破绽。
- **如果一击决胜未能击杀目标**：正常扣血，目标存活。一击决胜不保证击杀；命中后目标破绽清空为 0。
- **如果多个敌人同时破绽达标**：每个目标都显示破绽大开状态，但一次出手只能对一个目标发动决胜一击，除非具体招式明确支持多目标决胜。
- **如果目标破绽达标但当前角色不在合法范围内**：决胜窗口仍存在，但该角色不能发动决胜一击。玩家可选择移动接近、使用轻功、改用其他招式或等待其他角色行动。
- **如果目标在自身行动结束时仍处于破绽大开**：按破绽衰减规则处理。若衰减后仍达到阈值，破绽大开状态继续保留。
- **如果角色内息不足以使用某个招式或轻功**：该动作不可选并灰显。角色仍可选择其他内息足够的招式、零消耗招式、调息、使用合法道具、切换内功或原地结束出手动作。
- **如果内息为 0 且有零消耗招式**：零消耗招式可选，调息也可选。玩家可通过配置零消耗招式换取断息时的行动弹性。
- **如果内息为 0 且无零消耗招式**：强制调息。系统不提供“普通攻击”兜底；最简单的攻击也必须来自已配置招式。该规则用于推动玩家在战前配置中权衡高消耗爆发招式与低/零消耗保底招式。
- **如果内功切换或轻功仍在冷却**：对应动作不可选并显示剩余自然行动次数。受击、被动反应、演出和剧情插入不推进冷却。
- **如果角色切换内功后改变当前气机**：改变立即生效。后续所有针对该角色的气机克制判定使用新气机。
- **如果角色使用轻功后移动到目标侧背**：侧背击收益只在随后合法出招命中时生效。单独使用轻功不自动造成侧背伤害，除非轻功数据声明伤害或破绽效果。
- **如果围势条件在同一目标同一行气周期内多次满足**：每个目标每个行气周期最多触发一次围势额外破绽，防止多人队伍无上限堆叠。
- **如果 Boss 阶段转换发生**：阶段转换规则可重置气血、破绽、当前内功、行气或冷却，但每项重置必须在 Boss 配置中显式声明。默认只处理气血阶段变化，不隐式清空破绽。
- **如果玩家方全部气血归零**：战斗失败，按叙事处理，非 Game Over。失败处理不得回滚已永久记录的剧情选择，除非该战斗明确是可重试训练战。
- **如果敌方全部气血归零**：玩家胜利。若最后一击同时触发剧情阶段条件，以剧情阶段条件优先，避免胜利结算与剧情中断同时出现。
- **如果剧情目标达成但敌方仍存活**：战斗进入剧情中断或特殊胜利，不继续行气推进。典型目标包括撑过指定敌方行动次数、抵达出口、保护 NPC 至其行气行动完成。
- **如果伤害波动使最终伤害低于最低伤害**：钳位到 `min_damage`。若目标处于特殊无敌/剧情保护状态，必须由状态明确覆盖最低伤害规则。
- **如果同一来源的破绽变化重复应用**：同一结算步骤中同源效果只应用一次。不同来源可叠加，例如气机克制 + 背击 + 招式特效。
- **如果状态效果尝试修改行气速度**：只有明确标注为 `status_xingqi_multiplier`、`xingqi_bonus_percent` 或 `retained_xingqi_percent` 的效果可改变行气相关数值；洞察、信息识破和 UI 看破效果不得改变行气。

## Dependencies

### 依赖表

| 方向 | 系统 | 类型 | 契约 |
|---|---|---|---|
| 上游 | 角色属性 / 功力 | 硬依赖 | 提供 `hp`, `max_hp`, `neixi`, `max_neixi`, `agility`, `attack_for_type`, `defense`, `crit_rate`, `neixi_recovery`, `chapter_baseline_agility`。不拥有当前战斗中的 `stagger`。 |
| 上游 | 武学组合 | 硬依赖 | 提供已装备招式；每个招式必须定义 `qi_type`、范围、目标形状、`base_multiplier`、`completion`、`realm_scaling`、`neixi_cost`、冷却和特殊效果。 |
| 上游 | 武学组合 | 硬依赖 | 提供已装备内功：1 个主内功 + 2 个备用内功；每个内功必须定义固定 `qi_type`、切换效果和切换冷却修正。 |
| 上游 | 武学组合 | 硬依赖 | 提供已装备轻功：移动覆盖规则、`neixi_cost`、冷却、可选 `xingqi_bonus_percent`、可选 `retained_xingqi_percent` 和可选破绽效果。 |
| 上游 | 队伍管理 / 同伴成长 | 硬依赖 | 提供上阵队伍列表、玩家方最多 5 人、主角锁定上阵规则，以及每名角色的武学配置引用。 |
| 上游 | 物品 / 道具 | 软依赖 | 提供战斗背包内容和合法战斗道具效果。 |
| 下游 | 敌方 AI | 硬依赖 | 消费棋盘状态、角色状态、行气队列、当前内功气机、破绽值、冷却、范围和目标合法性；输出移动目标和出手选择。 |
| 下游 | 战斗 UI | 硬依赖 | 消费棋盘高亮、行气值、行动队列、当前气机、招式范围、目标预览、破绽状态、决胜窗口、伤害事件和冷却状态。 |
| 下游 | 顿悟突破 | 软依赖 | 消费战斗压力、濒败状态、决胜事件、持续劣势状态，以及可选的凝神行动窗口。 |
| 下游 | CG / 演出 | 软依赖 | 消费决胜一击事件、阶段转换事件和剧情战斗中断事件。 |
| 下游 | 音乐 / 音效 | 软依赖 | 消费战斗状态转换、命中质量、决胜一击、轻功使用、内功切换、破绽达标和胜负事件。 |
| 下游 | 存档系统 | 硬依赖 | 提供战前自动存档和战后持久化。MVP 不支持战斗中手动存档。 |

### 状态归属

| 状态 | 归属 | 是否持久化 | 说明 |
|---|---|---|---|
| `hp` 当前值 | 战斗中由 Combat 持有；战斗外由角色状态持有 | 战后持久化 | 战斗可修改 HP；战后按叙事规则回写。 |
| `neixi` 当前值 | 战斗中由 Combat 持有；战斗外由角色状态持有 | 按遭遇规则持久化或恢复 | 战斗拥有临时消耗和回复。 |
| `stagger` 当前值 | 仅 Combat 持有 | 不持久化 | 每场战斗默认从 0 开始，除非遭遇配置显式覆盖。 |
| `xingqi` 当前值 | 仅 Combat 持有 | 不持久化 | 从遭遇定义的初始值开始，默认 0。 |
| `current_inner_power` | 战斗中由 Combat 持有 | 通常不持久化 | 默认从主内功开始；战中切换不改变战前配置。 |
| `cooldowns` | 仅 Combat 持有 | 不持久化 | 内功、轻功和招式冷却只存在于当场战斗。 |
| `equipped_moves` / `equipped_inner_powers` / `equipped_qinggong` | 武学组合 + 队伍配置 | 战斗外持久化 | Combat 在战斗开始时读取不可变快照。 |

### 必需事件

| 事件 | 生产者 | 消费者 | 用途 |
|---|---|---|---|
| `BattleStarted` | Combat | UI, Audio, Save | 初始化棋盘、角色、资源，并触发战前自动存档。 |
| `XingqiAdvanced` | Combat | UI, AI | 更新单位头顶行气条和行动队列。 |
| `ActorTurnStarted` | Combat | UI, AI, Tutorial | 为当前行动角色开启移动 / 出手选择。 |
| `MovePreviewChanged` | Combat/UI | UI | 展示合法格、范围、气机克制、身位和预期破绽变化。 |
| `ActorMoved` | Combat | UI, Audio | 播放移动动画并更新朝向。 |
| `InnerPowerSwitched` | Combat | UI, AI, Audio | 更新当前气机和切换冷却。 |
| `QinggongUsed` | Combat | UI, Audio | 播放特殊移动并应用轻功效果。 |
| `MoveResolved` | Combat | UI, AI, Audio | 应用伤害、破绽、命中质量和特殊效果。 |
| `StaggerChanged` | Combat | UI, AI | 更新破绽 UI 和 AI 威胁评估。 |
| `DecisiveWindowOpened` | Combat | UI, AI, Audio | 标记目标进入可决胜窗口。 |
| `DecisiveStrikeResolved` | Combat | UI, CG, Audio, Epiphany | 结算决胜伤害、清空破绽并触发高光反馈。 |
| `ActorDefeated` | Combat | UI, AI, Narrative | 从行动队列移除角色并检查结束条件。 |
| `BattleEnded` | Combat | UI, Save, Narrative, Items, Mindset | 结算胜负、奖励、剧情标签和持久化。 |

### 跨 GDD 同步状态

以下文档已按 2026-06-16 行气战棋方向完成同步；进入 architecture 前只需继续追踪 registry 数据项：

| GDD | 同步状态 |
|---|---|
| `character-attributes.md` | 已将战斗时序输入改为 `xingqi_gain`，并明确 `insight` 只影响信息识破。 |
| `martial-arts-system.md` | 已支持 1 主内功 + 2 备用内功，招式 `qi_type`，内功固定气机，以及轻功行气词条。 |
| `party-management.md` | 已更新可玩角色结构，支持 6 招式 + 1 主内功 + 2 备用内功 + 1 轻功。 |
| `combat-ui.md` | 已改为棋盘优先的战术 UI：行气、移动预览、范围预览、当前气机、破绽、朝向、行动队列和决胜窗口。 |
| `enemy-ai.md` | 已改为行气调度的棋盘 AI，输入包括当前内功气机、移动、范围、冷却、破绽和可读 tell。 |
| `game-concept.md` | 已将核心战斗描述改为行气驱动的轻量武侠战棋。 |
| `systems-index.md` | 已将系统命名、MVP、风险和依赖迁移到行气战棋。 |
| `design/registry/entities.yaml` | 已完成 registry 数据项同步；旧战斗公式、旧行动类型和旧破绽归属条目以 `deprecated` 保留历史追踪。 |

### 下游设计约束

- 下游系统不得重新引入基础“反制按钮”。
- 下游系统不得把敌方“下一招意图”作为核心战斗信息模型。
- 下游系统必须将可见气机视为当前运转内功的气机。
- 下游系统不得让 `insight` 改变行动频率或行气增长。
- UI 可以通过洞察揭示更多信息，但只能影响显示深度，不影响行动顺序。
- AI 可以读取玩家模式，但必须通过移动、当前气机、冷却、目标选择和可见 tell 表达应对，而不是通过隐藏的同步结算反制。

## Tuning Knobs

### 行气与行动频率

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `base_xingqi_gain` | 20 | 10-30 | 过高会让行动过密、UI 压力增大；过低会让战斗拖沓。 |
| `xingqi_threshold` | 100 | 80-120 | 过低会让快角色连续出手；过高会削弱敏捷和轻功价值。 |
| `agility_factor_min` | 0.75 | 0.60-0.90 | 过低会让低敏角色几乎失去行动；过高会削弱敏捷差异。 |
| `agility_factor_max` | 1.35 | 1.15-1.50 | 过高会导致高速角色滚雪球；过低会让敏捷成长无感。 |
| `qinggong_xingqi_multiplier_min` | 0.85 | 0.70-1.00 | 过低会让负面轻功词条惩罚过重；过高则难以表达迟滞。 |
| `qinggong_xingqi_multiplier_max` | 1.30 | 1.10-1.40 | 过高会让轻功成为必选；过低会让行气词条缺乏吸引力。 |
| `retained_xingqi_cap` | 0.30 | 0.00-0.30 | 过高会制造连续行动；0 则完全关闭行气保留流派。 |

### 伤害与命中质量

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `min_damage` | 1 | 0-3 | 过高会让防御失去意义；0 可能导致完全无反馈。 |
| `qi_advantage_multiplier` | 1.30 | 1.10-1.50 | 过高会把克制变成唯一策略；过低会让观气收益不足。 |
| `qi_disadvantage_multiplier` | 0.70 | 0.50-0.90 | 过低会严重惩罚误判；过高会让被克也无所谓。 |
| `side_attack_multiplier` | 1.10 | 1.00-1.20 | 过高会让绕侧强于观气；过低则侧击无感。 |
| `back_attack_multiplier` | 1.20 | 1.05-1.35 | 过高会导致背击滚雪球；过低则站位收益不足。 |
| `crit_multiplier` | 1.50 | 1.20-2.00 | 过高会让暴击压过战术判断；过低则暴击无感。 |
| `damage_variance` | +/-5% | +/-2%-10% | 过高会破坏战术可预期性；过低则伤害过机械。 |

### 破绽与决胜

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `stagger_threshold` | 5 | 3-8 | 过低会频繁决胜；过高会让决胜窗口难以出现。 |
| `stagger_on_qi_advantage` | 2 | 1-3 | 过高会让克制过快破防；过低会削弱观气收益。 |
| `stagger_on_qi_disadvantage` | 1 | 0-2 | 过高会让误判惩罚过重；0 则缺少反噬感。 |
| `stagger_on_side_attack` | 1 | 0-2 | 过高会让侧击过强；0 则侧击只剩伤害。 |
| `stagger_on_back_attack` | 2 | 1-3 | 过高会鼓励无脑绕背；过低会削弱取位。 |
| `stagger_decay_on_own_action_end` | 1 | 0-2 | 0 会让破绽只增不减；2 会让多人压迫难以成形。 |
| `formation_stagger_bonus` | 1 | 0-2 | 过高会让五人围攻迅速破防；0 则取消围势收益。 |
| `decisive_strike_multiplier` | 2.00 | 1.50-3.00 | 过高会接近必杀；过低会让玩家不愿等待窗口。 |

### 内息、内功与轻功

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `meditation_efficiency` | 0.50 | 0.30-0.80 | 过高会让内息管理无压力；过低会导致断息后恢复太慢。 |
| `meditation_defense_bonus` | 20% | 0%-40% | 过高会让调息成为强防御；0 则调息风险过大。 |
| `meditation_stagger_recovery` | 1 | 0-2 | 过高会让调息过度安全；0 则无法作为破绽恢复手段。 |
| `inner_power_switch_cooldown` | 2 次自然行动 | 1-4 | 过短会频繁换气机；过长会让多内功配置失去意义。 |
| `qinggong_cooldown` | 3 次自然行动 | 1-5 | 过短会让轻功替代普通移动；过长会让轻功存在感不足。 |
| `qinggong_neixi_cost_default` | 由轻功数据定义 | 0-8 | 过高会抑制轻功使用；过低会让轻功成为默认选择。 |

### 棋盘与遭遇节奏

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `mvp_board_width` | 8-12 | 6-14 | 过大移动时间增加；过小站位和绕侧空间不足。 |
| `mvp_board_height` | 8-12 | 6-14 | 过大移动时间增加；过小站位和绕侧空间不足。 |
| `default_move_range` | 由角色 / 招式数据定义 | 2-5 | 过高会削弱棋盘空间；过低会导致追击困难。 |
| `max_player_actors` | 5 | 1-5 | 超过 5 会显著增加 UI 和 AI 复杂度；低于 3 会削弱围势玩法。 |
| `recommended_enemy_count_normal` | 1-6 | 1-8 | 过多会造成行动队列拥挤；过少会让多人围势过强。 |

### 信息揭示与洞察

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `qi_visibility_default` | 当前气机可见 | 固定 | 当前运转内功气机默认可见，是新版战斗的基础信息。 |
| `insight_detail_bonus` | 由洞察规则定义 | 待定 | 只影响信息深度，如显示内功名、招式倾向、隐藏数值；不得影响行气。 |
| `hidden_move_name_threshold` | 高洞察限定 | 待定 | 过低会让敌人行为太透明；过高会让洞察无感。 |

### 演出与反馈

| 参数 | 默认值 | 安全范围 | 过高 / 过低影响 |
|---|---:|---:|---|
| `decisive_hitstop_duration` | 0.30s | 0.10-0.60s | 过长会打断节奏；过短则缺少高光。 |
| `qinggong_animation_duration` | 0.35s | 0.15-0.80s | 过长拖慢战斗；过短看不清位移。 |
| `inner_power_switch_vfx_duration` | 0.40s | 0.20-0.80s | 过长频繁打断；过短气机变化不明显。 |
| `stagger_threshold_feedback_duration` | 0.60s | 0.30-1.20s | 过长遮挡信息；过短玩家容易错过决胜窗口。 |

### 调参原则

- 行气相关参数不得引用 `insight`。
- 破绽阈值、破绽衰减和决胜倍率由 `combat-system.md` 统一拥有。
- Boss 可以覆盖破绽阈值、阶段重置、初始行气和部分冷却，但必须在 Boss 配置中显式声明。
- 轻功可以影响移动、行气加成或行气保留，但必须通过明确词条表达，不能隐式加速。
- 所有影响行动频率的参数必须进入回归测试，避免高速角色连续行动失控。

## Visual/Audio Requirements

### 信息可读性原则

- **棋盘优先**：所有战斗特效不得遮挡当前行动角色、合法移动格、招式范围、目标朝向、当前气机和破绽状态。
- **信息分层**：常驻信息只显示必要状态；高光信息只在预览、命中、破绽大开和决胜窗口出现。
- **双通道表达**：刚 / 柔 / 巧不能只依赖颜色区分，必须同时使用形状、动效节奏和音色差异。
- **演出可跳过**：重复播放的轻功、内功切换和普通招式演出应支持快速化；首次决胜或剧情决胜可完整播放。
- **战术预览优先于演出**：玩家确认前必须清楚看到移动路径、攻击范围、克制关系、预计破绽变化和内息消耗。

### 刚 / 柔 / 巧视觉与音色母题

| 气质 | 视觉母题 | 动效节奏 | 音色母题 |
|---|---|---|---|
| 刚 | 山石、裂纹、重墨、短直冲击线 | 短促、沉重、爆发 | 低频鼓点、钝击、金石声 |
| 柔 | 水纹、回旋、衣袂、圆弧流线 | 连续、回护、缓急相生 | 弦乐滑音、流水、轻柔气息 |
| 巧 | 风痕、残影、细线、折返轨迹 | 快速、跳变、轻盈 | 竹笛短音、破空声、轻铃 |

### 行气与行动队列

- **行气条**：每个单位头顶显示细条行气，未满时低饱和，接近 100 时逐渐亮起。
- **行动队列**：屏幕侧边显示即将行动的 3-5 个单位头像，包含阵营、当前气机、破绽状态和预计顺序。
- **行气满提示**：角色行气满时，脚下出现短促气浪，音效以轻微“提气”提示，不打断玩家操作。
- **行气保留提示**：若内功或轻功保留行气，行动结束后行气条以残留亮段显示，并短暂标注“留气”。

### 棋盘、移动与轻功

- **合法移动格**：使用低亮度地面描边，避免遮挡地形和角色。
- **不可达格**：以暗色斜纹或破碎边缘显示，不只依赖红色。
- **移动路径**：预览时显示步法轨迹线；确认后角色按路径移动。
- **轻功移动**：使用更高层级的残影或跃迁轨迹，必须清楚显示起点、终点和落点朝向。
- **侧背击预览**：当移动后可形成侧击或背击时，目标脚下出现侧 / 背方向标记，并在预览面板显示对应倍率和破绽变化。

### 当前气机与内功切换

- **当前气机标识**：角色头顶或脚下显示当前运转内功的刚 / 柔 / 巧符号，玩家和敌人使用同一规则。
- **内功切换**：切换时以短暂环形气场包裹角色，气场按新气机母题变化。
- **切换冷却**：内功切换按钮显示剩余自然行动次数，冷却未好时灰显但保留 tooltip。
- **高洞察信息**：洞察只改变信息深度，例如显示内功名称、气机稳定度、可能招路倾向；不得通过视听表现暗示行气加速。

### 招式命中与破绽反馈

- **命中质量**：普通命中、克制命中、被克命中、侧击、背击使用不同强度的屏幕震动、伤害数字和音效层级。
- **克制命中**：使用攻方招式气质的高亮边缘，目标身上出现被破开的气机裂隙。
- **被克命中**：攻方动作收束受阻，伤害数字缩小，攻方自身出现短暂破绽提示。
- **破绽变化**：破绽增加时在目标脚下或资源条旁出现短促裂纹扩张；减少时裂纹收束。
- **破绽大开**：目标进入明确高光状态，脚下出现开放式裂纹环，UI 显示“破绽大开”，并播放一次可识别音效。
- **围势触发**：多人压迫同一目标触发围势时，目标周围出现短暂合围线，但不得遮挡棋盘格。

### 决胜一击

- **决胜窗口**：目标破绽达标且有合法攻击者时，目标身上显示决胜标识，行动队列与目标预览同时提示。
- **决胜确认**：选择决胜一击时，预览必须显示预计伤害、目标破绽清空规则和是否可能击杀。
- **决胜演出**：决胜一击使用短慢动作、镜头压近、命中特写和强化音效。MVP 中每种气质至少需要一套通用决胜表现。
- **决胜后反馈**：命中后目标破绽清空，显示裂纹闭合或气机崩散；若目标未败退，必须清楚表现“重整架势”。

### 调息与断息

- **调息**：角色原地收势，内息条回升，音效以呼吸和低频气流为主。
- **强制调息**：当内息为 0 且无零消耗招式时，UI 应明确显示“无可用招式，必须调息”，避免玩家误以为输入失效。
- **零消耗招式**：零消耗招式必须在 UI 上清晰标识，作为玩家配置保底招式的正反馈。

### 失败、胜利与剧情中断

- **战斗失败**：玩家方全灭时使用叙事失败反馈，不使用 Game Over 式断裂反馈。
- **战斗胜利**：胜利反馈应先完成关键命中 / 决胜表现，再进入奖励或剧情队列。
- **剧情中断**：剧情目标达成时，应使用明确的镜头和音效转场，避免玩家误以为敌方行动队列还会继续。

### Asset Spec 提示

Art Bible 审批后，需要运行 `/asset-spec system:行气战棋战斗` 生成以下资产规格：

- 刚 / 柔 / 巧气机图标与动效。
- 行气条、行动队列、破绽大开、决胜窗口 UI 素材。
- 轻功路径、内功切换、侧背击预览、围势线等棋盘特效。
- 招式命中、克制、被克、破绽变化、决胜一击音效包。
- MVP 通用决胜一击演出模板。

## UI Requirements

### UI 总原则

- **棋盘优先**：战斗 UI 的主视觉焦点是棋盘、角色、行动路径和攻击范围，而不是底部按钮栏。
- **键盘 / 手柄优先**：所有战斗操作必须可用键盘和手柄完成；鼠标点击是补充交互，不是唯一入口。
- **焦点栈合规**：所有可交互面板必须遵守 ADR-0002 的基于栈的焦点管理，悬停态不得覆盖键盘 / 手柄焦点。
- **预览先于确认**：任何移动、出招、轻功、内功切换、调息、道具和决胜操作都必须先进入预览态，再确认执行。
- **禁用项可见但不可聚焦**：内息不足、冷却未好、目标不合法的选项应灰显并说明原因，但不得进入焦点循环。
- **无普通攻击兜底**：UI 不显示“普通攻击”。最简单的攻击也必须是已配置招式。

### 屏幕区域

| 区域 | 内容 | 常驻性 |
|---|---|---|
| 棋盘层 | 方格、角色、移动路径、攻击范围、朝向、合法目标、不可达格 | 常驻 |
| 单位头顶层 | HP 简条、行气条、当前气机、破绽状态、关键状态图标 | 常驻且所有敌人完整显示 |
| 行动队列 | 即将行动的 3-5 个单位，显示阵营、气机、破绽和队列顺序 | 常驻 |
| 行动菜单 | 出招、切换内功、使用轻功、调息、使用道具、决胜一击 | 仅当前角色行动时显示 |
| 招式 / 轻功 / 内功详情 | 消耗、范围、冷却、气质、预计伤害、预计破绽变化 | 选中项目时显示 |
| 目标预览面板 | 克制关系、身位、命中质量、预计伤害、预计破绽、决胜可用性 | 选中目标时显示 |
| 战斗日志 | 最近 3-5 条关键事件，如克制、破绽大开、决胜、击败 | 可折叠 |
| 剧情 / 目标面板 | 特殊战斗目标，如撑过次数、抵达出口、保护 NPC | 仅特殊战斗显示 |

### 当前行动流程

```text
ActorTurnStarted
  -> SelectMoveCell / SelectActionCategory
  -> PreviewAction
  -> SelectTargetOrDestination
  -> ConfirmAction
  -> ResolveAction
  -> ReturnToBoardOrNextActor
```

- 行动开始时，焦点默认落在当前行动角色所在棋盘格。
- 玩家可先移动，也可直接选择出手动作。
- 若已移动但尚未确认出手，UI 必须允许撤销到行动开始状态。
- 若移动后目标不合法，UI 必须提示“目标不在范围内”或“路径被阻断”，而不是直接执行失败。
- 行动执行后，焦点转移到下一个行动角色；若进入剧情中断或战斗结束，则清空战斗焦点栈。

### 行动菜单

| 行动 | 显示条件 | 禁用条件 | 预览内容 |
|---|---|---|---|
| 出招 | 当前角色有已配置招式 | 全部招式内息不足或目标不合法时不可确认 | 招式范围、气质、消耗、冷却、预计伤害、预计破绽 |
| 切换内功 | 已配置 2 个以上可运转内功 | `inner_power_switch_cd_remaining > 0` | 新气机、入场效果、冷却 |
| 使用轻功 | 已装备轻功 | `qinggong_cd_remaining > 0` 或内息不足 | 目标格、路径、消耗、冷却、可能破绽效果 |
| 调息 | 始终可用 | 无 | 内息回复、短时防护、破绽恢复 |
| 使用道具 | 战斗背包有可用道具 | 道具不可用于当前目标或次数耗尽 | 道具效果、目标范围 |
| 决胜一击 | 至少一个合法目标破绽达标 | 当前目标不在决胜范围或角色不能行动 | 预计伤害、破绽清空、击败可能性 |

### 招式列表

每个招式条目必须显示：

- 招式名。
- 招式气质：刚 / 柔 / 巧。
- 内息消耗。
- 当前冷却。
- 范围形状摘要。
- 是否零消耗。
- 特殊效果摘要。
- 与当前目标的克制结果。
- 预计破绽变化。

招式列表不得用“品质颜色”替玩家判断强弱。玩家应根据气质、范围、消耗、冷却、破绽和特殊效果取舍。

### 内息为 0 的 UI 规则

- 若存在零消耗招式：零消耗招式可选，调息可选，其他内息不足招式灰显。
- 若不存在零消耗招式：行动菜单自动聚焦“调息”，并提示“无可用招式，必须调息”。
- UI 不提供普通攻击兜底。
- 该提示应被视为配置反馈，鼓励玩家在战前配置低 / 零消耗招式。

### 目标与预览

目标预览必须同时展示：

- 攻方招式气质。
- 目标当前内功气机。
- 克制关系：克制 / 被克 / 同系或无关系。
- 攻击角度：正面 / 侧面 / 背面。
- 预计伤害区间。
- 预计破绽变化。
- 是否触发围势。
- 是否打开或消费决胜窗口。
- 内息和冷却变化。

预览不得显示敌方“下一招意图”作为核心信息。高洞察时可以额外显示内功名称、招式倾向或隐藏数值，但不得改变行动顺序。

### 行气与行动队列 UI

- 每个单位头顶必须显示行气条。
- 行动队列必须显示即将行动的 3-5 个单位。
- 行气满但尚未行动的单位必须有明确等待状态。
- 行动队列变化必须有轻量动画，但不得抢夺当前焦点。
- 如果多个单位同时入队，UI 顺序必须与规则层排序一致。

### 破绽与决胜 UI

- 破绽显示应精简，默认以条、裂纹或层级图标表达，不需要大号数字常驻。
- 破绽大开时必须强提示，并在目标预览中显示“可决胜”。
- 决胜一击只在目标合法且当前角色可执行时进入可选焦点。
- 决胜不可用时，应显示原因：距离不足、目标未破绽大开、角色不能行动、动作被禁用。
- 决胜命中后，UI 必须清楚显示目标破绽清空。

### 键盘 / 手柄导航

- D-pad / 方向键在棋盘格之间移动焦点。
- 肩键或快捷键可在行动队列、当前角色、目标候选和行动菜单之间切换焦点域。
- A / Confirm 进入预览或确认；B / Cancel 返回上一层或撤销移动预览。
- 禁用项不得进入循环焦点。
- 焦点必须支持循环导航，但不得跳入隐藏面板。
- 鼠标悬停可以显示 tooltip，但不得改变当前键盘 / 手柄主焦点。
- 所有弹窗、确认框和目标选择层必须入栈；关闭时恢复上一焦点。

### 敌人头顶状态展示

- 敌人不做差异化头顶展示。
- 无论敌方人数多少，每个敌人都完整显示 HP 简条、行气条、当前气机、破绽状态和关键状态图标。
- 焦点目标和高威胁目标可以追加描边、高亮或目标框，但不得隐藏、折叠或降级其他敌人的头顶状态。
- 如果敌人数量导致画面拥挤，必须通过遭遇设计、分批入场、棋盘布局、镜头缩放或 UI 尺寸调节解决，不得通过减少敌方状态信息解决。

### 状态与错误提示

UI 必须为以下情况提供明确提示：

- 内息不足。
- 冷却未好。
- 目标不合法。
- 路径不可达。
- 没有零消耗招式且必须调息。
- 决胜不可用原因。
- 内功切换后气机改变。
- 轻功使用后冷却开始。
- 剧情目标达成导致战斗中断。

### UX 后续工作

本节只定义战斗 UI 的设计规格，不定义 Godot 节点结构。Pre-Production 阶段需要运行 `/ux-design` 为战斗界面创建完整 UX 规格，重点包括：

- 棋盘焦点与行动菜单焦点如何共存。
- 行动队列与目标预览的版式。
- 手柄 / 键盘快捷键映射。
- 高洞察信息的渐进显示。
- 多敌人完整头顶状态下的拥挤控制方案。

## Acceptance Criteria

1. **GIVEN** 多个角色同时行气达到 100，**WHEN** 进入行动队列，**THEN** 按 `xingqi desc -> side_priority -> agility desc -> board_reading_order` 稳定排序，且同一输入状态每次结果一致。
2. **GIVEN** 角色完成一次自然行动，**WHEN** 结算行动结束，**THEN** 该角色行气默认归零；若有明确内功或轻功保留行气词条，则最多保留 `xingqi_threshold * 0.30`。
3. **GIVEN** 角色敏捷高于章节基线，**WHEN** 计算行气增长，**THEN** `agility_factor` 通过 `agility / chapter_baseline_agility` 计算并钳位在 0.75-1.35，且 `insight` 不参与计算。
4. **GIVEN** 玩家使用柔系招式命中当前运转刚系内功的目标，**WHEN** 结算气机克制，**THEN** 伤害乘以 `qi_advantage_multiplier`，目标获得 `stagger_on_qi_advantage` 破绽。
5. **GIVEN** 玩家使用巧系招式命中当前运转刚系内功的目标，**WHEN** 结算气机克制，**THEN** 伤害乘以 `qi_disadvantage_multiplier`，攻方获得 `stagger_on_qi_disadvantage` 破绽。
6. **GIVEN** 攻方从目标背面命中，**WHEN** 结算身位，**THEN** 应用 `back_attack_multiplier`，并为目标增加 `stagger_on_back_attack` 破绽。
7. **GIVEN** 两个己方角色在同一目标的同一行气周期内形成围势，**WHEN** 第二个角色命中该目标，**THEN** 目标额外获得 `formation_stagger_bonus` 破绽，且该额外破绽本周期不重复触发。
8. **GIVEN** 目标破绽达到 `stagger_threshold` 且当前角色处于合法决胜范围，**WHEN** 玩家选择“决胜一击”，**THEN** 造成 `decisive_strike_multiplier` 倍伤害，播放决胜表现，并在命中后将目标破绽清空为 0。
9. **GIVEN** 目标破绽达到 `stagger_threshold` 但当前角色不在合法决胜范围，**WHEN** 玩家查看行动菜单或目标预览，**THEN** UI 显示破绽大开但不可决胜，并说明距离或范围原因。
10. **GIVEN** 角色内息为 0 且存在零消耗招式，**WHEN** 玩家查看招式面板，**THEN** 零消耗招式可选，调息也可选，其他内息不足招式灰显且不可聚焦。
11. **GIVEN** 角色内息为 0 且不存在零消耗招式，**WHEN** 进入当前角色行动，**THEN** UI 自动聚焦“调息”，提示“无可用招式，必须调息”，且不显示“普通攻击”兜底。
12. **GIVEN** 角色切换内功，**WHEN** 切换成功，**THEN** 该角色当前气机立即变为新内功的固定 `qi_type`，并设置 `inner_power_switch_cooldown`；后续针对该角色的克制预览使用新气机。
13. **GIVEN** 角色使用轻功，**WHEN** 轻功结算，**THEN** 消耗对应内息，设置 `qinggong_cooldown`，并只在轻功数据明确声明时影响行气、行气保留或破绽。
14. **GIVEN** 敌方人数超过 6，**WHEN** 战斗 UI 显示敌人，**THEN** 每个敌人仍完整显示 HP 简条、行气条、当前气机、破绽状态和关键状态图标，不折叠、不降级、不中断头顶状态显示。
15. **GIVEN** 玩家洞察不足，**WHEN** 查看敌方单位，**THEN** UI 可隐藏内功名称、招式倾向或隐藏数值；但当前气机显示规则和行气增长不受洞察影响。
16. **GIVEN** 玩家洞察达到高信息门槛，**WHEN** 查看敌方单位，**THEN** UI 可额外显示内功名称、招式倾向或隐藏数值；但不得改变敌方行气增长、行动顺序或冷却推进。
17. **GIVEN** `character-attributes.md`、`martial-arts-system.md`、`party-management.md`、`combat-ui.md` 或 `enemy-ai.md` 仍保留旧同步结算、基础反制按钮、洞察影响行气、单内功槽或旧破绽归属，**WHEN** 尝试进入 architecture，**THEN** gate 必须判定为阻塞，直到相关 GDD 同步完成。

## Open Questions

1. **洞察信息深度分层**：洞察在战斗中具体揭示哪些层级的信息？候选层级包括：只显示当前气机、显示内功名称、显示气机稳定度、显示可能招路倾向、显示隐藏数值。需要在 `character-attributes.md` 和 `combat-ui.md` 中同步定义。
2. **Boss 阶段转换规则**：Boss 阶段转换时是否重置破绽、行气、当前内功和冷却？默认规则是不隐式重置，但 Boss 配置可以显式覆盖。需要在 Boss / enemy AI GDD 中定义配置格式。
3. **顿悟在行气体系中的触发**：当前基线采用角色自然行动次数，即角色进入 `ActorTurnStarted` 的次数；`epiphany-breakthrough.md` 已定义凝神默认持续 3 个自身自然行动。战斗脉冲、濒危持续时间或剧情压力事件只作为后续平衡备选，不作为当前架构阻塞。
4. **战斗失败后的叙事分支**：玩家方全灭不是 Game Over，但具体失败分支、伤势、资源损失、剧情推进或重试规则仍需由主线叙事和存档系统定义。
5. **多敌人完整头顶状态的拥挤控制**：敌人不做差异化展示，所有敌人完整显示头顶状态。需要在后续 UX 规格中解决大量敌人时的镜头缩放、标牌尺寸、遮挡规避和分批入场建议。
6. **道具在 0 内息强制调息规则下的优先级**：当前规则是“0 内息且无零消耗招式时强制调息”。是否允许无内息时使用不消耗内息的道具，需要由 `item-system.md` 和战斗 UI 同步决定。若允许，道具会削弱强制调息的配置压力。
7. **行气保留词条的来源边界**：当前只允许明确内功或轻功词条保留最多 30% 行气。是否允许招式、装备、状态也提供行气保留，需要后续平衡验证后再开放。
8. **当前内功是否影响可用招式**：本 GDD 只规定招式自带刚 / 柔 / 巧属性，当前内功决定自身显露气机。是否存在“只有运转特定内功才能使用或强化某些招式”的规则，需要在 `martial-arts-system.md` 中定义。
9. **敌方 AI 的可读 tell 规格**：新版 AI 不再公开下一招意图，但仍需要玩家能读出敌方倾向。需要定义敌人如何通过走位、内功切换、轻功使用、目标选择和动作前摇表达可读模式。
10. **同步状态**：核心 GDD 与 `design/registry/entities.yaml` 已完成行气战棋口径同步；剩余 Open Questions 作为后续设计追踪，不再作为旧版 `/review-all-gdds` FAIL 的延续条件。
