# 敌方 AI

> **Status**: Designed
> **Author**: user + game-designer + ai-programmer
> **Last Updated**: 2026-06-16
> **Implements Pillar**: Pillar 3 — 武侠味先于游戏味（AI 行为像"高手过招"，不是数值机器）

## Overview

敌方 AI 是所有非玩家战斗角色的"大脑"。在新版行气战棋战斗中，它不再预先公开下一招意图，也不参与同步结算。AI 只在自身行气满并进入行动队列后，基于当前棋盘、当前内功气机、移动范围、招式范围、内息、冷却、破绽、目标身位和可见 tell 做即时决策。

AI 的核心目标是制造"可读但不可背答案"的武侠对局：
- **可读**：敌人有当前气机、取位偏好、常用招路、蓄势动作和阶段性 tell，玩家能通过观察建立假说。
- **不可背答案**：敌人不会按固定答案出招；同一性格在当前棋盘、资源和阶段不同的情况下会做不同选择。
- **不作弊式反制**：AI 可以适应玩家模式，但必须通过移动、当前气机、冷却、目标选择和可见 tell 表达，不允许隐藏地读取玩家选择并在同步结算前改招。

系统分为两个层级：
- **普通敌人**：使用性格模板 + 棋盘启发式评分，行为简洁、稳定、可读。
- **Boss**：使用阶段脚本 + 行气调度 + 专属 tell，每个 Boss 是一道逐步揭开的战术题。

AI 从角色属性系统读取自身基础数值，从行气战棋战斗系统获取场上状态，从武学/敌人招式配置获取可用动作，输出移动目标与出手动作给战斗系统执行。

## Player Fantasy

**"这个对手不是随机出招的木桩——他有气机、有步法、有偏好、有弱点，而我正在一招一招地读懂他。"**

在武侠小说中，高手对决最精彩的部分不是对方把下一招报给你，而是你从他的气息、站位、起手和取位中读出路数。敌方 AI 要让玩家获得三层体验：
- **普通敌人是可读的性格**：刚猛敌人更愿意贴近压迫，柔韧敌人更愿意守位调息，诡诈敌人更愿意绕侧穿身。
- **Boss 是层层揭开的谜题**：阶段越深，tell 越明确，但威胁也越强。玩家的进步来自"我知道他为什么这样走"。
- **被 AI 读懂的压迫感**：Boss 可以通过可见 tell 表达"他开始针对我的站位/招路"，但这种针对必须可观察、可回应。

## Detailed Design

### Core Rules

**1. 行气触发的即时决策**

AI 不在战斗脉冲开始时提前决定整回合行为。只有当某个敌方单位进入 `ActorTurnStarted` 时，才执行决策管线：

```text
ActorTurnStarted(enemy)
  -> RefreshBattleContext
  -> CheckForcedState
  -> SelectInnerPowerOrKeepCurrent
  -> ScoreMoveCells
  -> ScoreActions
  -> SelectMoveAndAction
  -> EmitEnemyPlanTell(optional)
  -> ExecuteMoveAndAction
```

不变量：
- AI 决策只对当前行动单位生效，不为未来单位预排完整计划。
- AI 可读信息来自当前气机、站位、蓄势、锁定、冷却和阶段 tell，不来自公开下一招。
- AI 不得读取玩家尚未确认的 UI 选择。

**2. 性格模板**

每个普通敌人定义一个性格模板，决定取位、气机、目标和行动偏好。

| 模板名 | 气机偏好 | 取位偏好 | 行动偏好 | 玩家可读信号 |
|---|---|---|---|---|
| 刚猛型 | 刚 | 正面贴近 | 高伤、破防、追击低血目标 | 常向最近目标压迫，少绕路 |
| 柔韧型 | 柔 | 保持中距 | 调息、防护、消除自身破绽、反打 | 血低或破绽高时后撤守位 |
| 诡诈型 | 巧 | 侧背绕位 | 位移、封穴、破绽累积、攻击脆弱目标 | 常避开正面，偏爱侧击 |
| Boss-自定义 | 按阶段变化 | 按阶段变化 | 阶段脚本 + 专属机制 | 专属 tell |

性格模板不等于固定行动表。它只提供评分权重，最终动作仍由棋盘和资源状态决定。

**3. 当前气机与内功选择**

AI 当前气机来自正在运转的内功。普通敌人通常只有 1 个内功；精英和 Boss 可拥有主内功 + 备用内功。

内功切换规则：
- 只有当 `inner_power_switch_cooldown == 0` 时可切换。
- 切换内功占用出手动作，除非 Boss 阶段脚本明确声明为阶段转换效果。
- AI 切换后头顶当前气机立即更新，供玩家后续读气机。
- AI 不会为了克制玩家尚未确认的当前招式而切换，只会基于已发生的玩家模式、当前棋盘压力和阶段脚本切换。

**4. 移动评分**

AI 为所有合法移动格计算分数：

```text
move_score =
  target_access_score
  + facing_score
  + safety_score
  + personality_position_score
  + objective_score
```

| 分量 | 说明 |
|---|---|
| `target_access_score` | 移动后可命中的目标数量与优先级 |
| `facing_score` | 是否能形成侧击/背击，或避免自己被背击 |
| `safety_score` | 是否进入敌方危险范围，是否暴露高破绽单位 |
| `personality_position_score` | 刚猛偏贴近，柔韧偏中距，诡诈偏侧背 |
| `objective_score` | 保护目标、堵路、撤退、剧情战目标等 |

AI 可选择原地停留。原地不是失败动作，柔韧型和蓄势 Boss 可主动原地调息或守位。

**5. 行动评分**

移动格确定后，AI 对可用出手动作评分：

```text
action_score =
  damage_score
  + stagger_score
  + qi_advantage_score
  + resource_score
  + cooldown_score
  + personality_action_score
  + boss_script_score
```

可选动作包括：
- 出招
- 调息
- 使用轻功
- 切换内功
- 使用道具（若敌人配置允许）
- 决胜一击
- 原地结束出手动作（仅当无合法动作且不能调息时）

AI 不使用"普通攻击"兜底。最简单的攻击也必须来自敌人已配置招式。若内息为 0 且无零消耗招式，默认优先调息。

**6. 目标选择**

目标选择不是单独固定表，而是行动评分的一部分。默认优先级：

| 优先级 | 条件 | 说明 |
|---|---|---|
| 1 | 可触发决胜一击 | 若目标破绽达标且范围合法，优先收束 |
| 2 | 可击败目标 | 若预计伤害足以击败目标，优先补刀 |
| 3 | 目标正在威胁己方关键单位 | 包括保护剧情 NPC 或 Boss 本体 |
| 4 | 上次对自己造成高伤害者 | 保留仇恨感，但不强制锁死 |
| 5 | 性格偏好目标 | 刚猛追近，诡诈打脆弱，柔韧打过度前压者 |

多人敌方决策按行动队列顺序执行。后行动 AI 可读取先行动 AI 已选择的目标，避免所有普通敌人无脑集火同一非关键目标；Boss 可覆盖此规则。

**7. Boss 阶段脚本**

Boss 使用阶段脚本，但阶段脚本仍通过行气行动执行。

```text
Boss Phase
  -> phase_traits
  -> available_moves
  -> inner_power_pool
  -> special_tells
  -> forced_events
```

Boss 可用机制：
- **气机锁定**：短期偏向某一内功气机，但不是 100% 硬锁；用于制造可读窗口。
- **蓄势预告**：本次行动执行蓄势或守位，下次该 Boss 行气满时优先执行已承诺招式；预告必须通过 UI tell 表达。
- **阶段转换**：清除或调整破绽，切换内功池，刷新冷却，并播放演出。
- **取位压迫**：强制偏好包抄、堵路、贴身或拉开。
- **弱点暴露**：阶段中可短暂暴露对某类气机或身位的弱点。

Boss 不使用"无视行气的优先攻击"作为基础机制。若需要抢节奏，应通过行气保留、蓄势预告、阶段转换或高行气增长状态表达。

**8. 玩家模式适应**

AI 可读取玩家已发生的历史行为：
- 常用招式气质
- 常用站位距离
- 是否频繁绕背
- 是否常在断息时依赖调息

适应规则：
- 普通敌人默认不使用模式适应。
- 精英敌人只做轻量偏移，例如更频繁转身防侧背。
- Boss 可在阶段 tell 后调整取位或气机偏好，但必须先给玩家可见信号。

适应不得改变玩家已经确认的行动结算结果。

**9. 可见 tell**

AI 行为品质必须被玩家感知。以下 tell 由 AI 触发语义，UI/演出负责表现：

| AI 行为 | 玩家侧 Tell | 信号时机 |
|---|---|---|
| 气机锁定 | Boss 气机图标脉动，身上出现"执念"状态 | 锁定开始 |
| 蓄势预告 | 显示蓄势姿态、目标倾向和下次行动危险范围 | 蓄势行动时 |
| 模式适应 | Boss 凝视 / 侧身 / 收势，提示其开始针对某类站位或气质 | 适应启用前 |
| 弱点暴露 | 短暂破绽裂纹或气机不稳 | 弱点窗口开启 |
| 绝境阶段 | 色调压低、气机暴涨、行气条高亮 | 阶段转换 |

信号不是答案。tell 只提示行为模式或风险方向，玩家仍需通过棋盘与气机判断具体应对。

### States and Transitions

普通敌人状态机：

| 状态 | 进入条件 | 行为修正 |
|---|---|---|
| 正常 | 战斗开始 | 使用性格模板 |
| 警觉 | 连续被克制或被侧背击 | 提高安全评分，降低重复路线 |
| 防御 | HP < 40% 或自身破绽高 | 提高调息、防护、后撤评分 |
| 绝境 | HP < 15% | 提高高风险出招与决胜评分 |

```text
[正常] -> [警觉]
[正常/警觉] -> [防御]
[防御] -> [绝境]
```

Boss 状态机：

```text
[Phase 1] -> [Phase Transition] -> [Phase 2] -> ... -> [Defeated]
```

阶段转换时可：
- 清空或调整破绽。
- 切换当前内功气机。
- 刷新或锁定冷却。
- 播放短演出。
- 注入下一阶段 tell。

## Interactions with Other Systems

| 系统 | 接口 | 数据方向 | 说明 |
|---|---|---|---|
| **行气战棋战斗** | `GetBattleContext()` | 战斗→AI | 获取棋盘、行气队列、单位状态、目标合法性、危险范围 |
| | `RequestEnemyDecision(actor_id)` | 战斗→AI | 当前敌人行气满时请求移动与出手决策 |
| | `EnemyDecision(move_cell, action_id, target_or_cell)` | AI→战斗 | 输出移动格和出手动作 |
| | `EmitEnemyTell(tell)` | AI→战斗/UI | 输出气机锁定、蓄势、弱点暴露等 tell |
| **角色属性** | `GetCurrentHP()` / `GetMaxHP()` | 属性→AI | 读取 HP 比值 |
| | `GetCurrentNeiXi()` / `GetNeiXiRecovery()` | 属性→AI | 判断调息与资源压力 |
| | `GetDefense()` / `GetAttackForType(type)` | 属性→AI | 估算伤害与风险 |
| **武学组合 / 敌人招式配置** | `GetAvailableMoves(actor_id)` | 武学→AI | 获取招式气质、范围、位移、消耗、冷却和特殊效果 |
| **战斗 UI** | `EnemyTell(tell)` | AI→UI | UI 展示可见 tell，不展示完整隐藏评分 |
| **教学/引导** | `SetBehaviorMode("tutorial")` | 教学→AI | 教学战斗限制 AI 招式池和复杂 tell |

## Formulas

### F1. 移动评分

```text
move_score =
  target_access_score
  + facing_score
  + safety_score
  + personality_position_score
  + objective_score
```

评分项由战斗系统提供合法格、危险范围和目标预览 DTO，AI 只做加权选择。

### F2. 行动评分

```text
action_score =
  damage_score
  + stagger_score
  + qi_advantage_score
  + resource_score
  + cooldown_score
  + personality_action_score
  + boss_script_score
```

### F3. 调息倾向

```text
if current_neixi <= meditation_threshold:
  meditation_score += meditation_base_score + (meditation_threshold - current_neixi) * meditation_per_missing_neixi
```

AI 调息占用出手动作。调息回复量、短时防护和破绽恢复由 `combat-system.md` 结算。

## Edge Cases

- **If 所有攻击招式内息不足且无零消耗招式**：AI 选择调息；若因特殊状态无法调息，原地结束出手动作。
- **If 移动后原目标不再合法**：AI 重新评分当前格可用动作；若无合法攻击，选择调息、切换内功、合法道具或原地结束。
- **If 多个敌人能决胜同一目标**：按行动队列顺序执行；后行动敌人若目标已落败，重新决策。
- **If Boss 蓄势期间被打入阶段转换**：清除蓄势承诺，执行阶段转换脚本。
- **If 气机锁定与内功切换冷却冲突**：阶段脚本可覆盖冷却；普通 AI 不可覆盖冷却。
- **If 评分完全相同**：使用 seeded random 打破平手，保证同一输入和 seed 下结果稳定。
- **If 玩家频繁利用单一站位策略**：只有精英/Boss 可提高对应安全或转身评分；普通敌人不进行复杂适应。

## Dependencies

| 方向 | 系统 | 依赖类型 | 说明 |
|---|---|---|---|
| 上游（硬） | 行气战棋战斗 | 硬 | AI 在 `ActorTurnStarted` 时读取棋盘状态并输出移动/出手决策 |
| 上游（硬） | 角色属性 | 硬 | 读取 HP、内息、攻击、防御等基础状态 |
| 上游（软） | 武学组合 / 敌人招式配置 | 软 | 读取可用招式、内功、轻功和冷却 |
| 下游（软） | 战斗 UI | 软 | 消费 AI tell，用于展示气机锁定、蓄势、弱点暴露等 |

## Tuning Knobs

| 参数 | 默认值 | 安全范围 | 过高/过低影响 |
|---|---:|---:|---|
| `meditation_threshold` | 2 | 1-4 | 过高→AI 太保守；过低→AI 频繁断息 |
| `meditation_base_score` | 30 | 10-60 | 过高→调息过多；过低→资源管理失效 |
| `side_position_weight` | 15 | 5-30 | 过高→AI 过度绕侧；过低→站位无感 |
| `back_position_weight` | 25 | 10-40 | 过高→AI 过度绕背；过低→背击无威胁 |
| `safety_weight` | 20 | 0-40 | 过高→AI 不敢进攻；过低→AI 送死 |
| `qi_advantage_weight` | 30 | 10-50 | 过高→AI 只追克制；过低→气机无意义 |
| `stagger_target_weight` | 35 | 15-60 | 过高→AI 只打破绽目标；过低→决胜窗口不被利用 |
| `boss_adaptation_weight` | 20 | 0-40 | 过高→Boss 像作弊；过低→Boss 不会学习 |
| `tell_lead_time_actions` | 1 | 1-2 | 过短→玩家来不及回应；过长→Boss 太好解 |

## Visual/Audio Requirements

AI 自身不定义视觉资源，但必须向 UI/演出提供 tell 语义：
- 气机锁定：气机图标脉动。
- 蓄势预告：蓄势姿态、危险范围和目标倾向。
- 模式适应：凝视或转身 tell。
- 弱点暴露：短暂裂纹或气机不稳。
- 绝境阶段：气机暴涨和色调压低。

## UI Requirements

- UI 不显示 AI 的完整评分、随机权重或下一招名称。
- UI 显示当前气机、行气条、行动队列、蓄势 tell、危险范围、弱点窗口和目标倾向。
- 洞察可提升信息深度，例如显示内功名称、可能招路或隐藏状态，但不得改变 AI 行气或决策。

## Acceptance Criteria

1. **GIVEN** 敌方单位行气未满，**WHEN** 战斗脉冲推进，**THEN** AI 不产生移动或出手决策。
2. **GIVEN** 敌方单位进入 `ActorTurnStarted`，**WHEN** AI 决策，**THEN** 输出一个合法移动格和一个合法出手动作，或合法调息/原地结束。
3. **GIVEN** 敌方内息为 0 且无零消耗招式，**WHEN** AI 进入行动，**THEN** AI 选择调息，且不使用普通攻击兜底。
4. **GIVEN** 目标当前气机被 AI 招式克制，**WHEN** AI 评分行动，**THEN** `qi_advantage_score` 提升该动作优先级，但不会绕过合法范围和内息检查。
5. **GIVEN** Boss 进入气机锁定，**WHEN** UI 接收 tell，**THEN** 只显示锁定倾向和当前气机，不公开下一招名称。
6. **GIVEN** Boss 执行蓄势预告，**WHEN** UI 显示危险范围，**THEN** 玩家在下一次相关行动前至少有 1 次可回应行动窗口，除非剧情战明确锁定。
7. **GIVEN** AI 评分出现完全平手，**WHEN** 使用同一 seed 重放，**THEN** 选择结果稳定一致。
8. **GIVEN** 玩家已确认行动，**WHEN** AI 后续行动，**THEN** AI 可基于已发生历史适应，但不得修改玩家已确认行动的结算。

## Open Questions

1. 具体 Boss 行为脚本内容：每个 Boss 的 Phase、专属 tell、气机池和取位偏好需要在关卡设计阶段逐个设计。
2. 同伴 AI 是否复用本系统：同伴在战斗中由玩家控制，但同伴代办支线可能需要简化 AI。
3. 难度调节是否影响 AI 权重：暂定只通过 tuning knobs 和关卡配置调整，不做动态作弊。
