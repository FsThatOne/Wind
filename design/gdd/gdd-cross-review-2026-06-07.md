# GDD Cross-Review Report — 2026-06-07

> **Scope**: All 21 designed GDDs (systems #1–#21)
> **Entity Registry**: `design/registry/entities.yaml` v1 (2026-06-05)
> **Design Pillars**: P1(江湖是活的) P2(选择有重量) P3(武侠味先于游戏味) P4(情感深度优先)
> **Anti-Pillars**: NOT 开放世界, NOT 后宫系统, NOT 高频战斗/数值刷怪
> **Verdict**: ✅ PASS — 7/7 BLOCKING + 15/15 WARNING issues fixed 2026-06-07; 4 INFO remain (recommendations only)

---

## Summary

| Severity | Count | Category |
|----------|-------|----------|
| 🔴 BLOCKING | 7 | Must fix before architecture |
| ⚠️ WARNING | 15 | Should fix before vertical slice |
| ℹ️ INFO | 4 | Recommendations / future consideration |

---

## Phase 2: Cross-GDD Consistency

### 🔴 BLOCKING

| # | Issue | Systems Involved | Description |
|---|-------|-----------------|-------------|
| B1 | AI 接口未被战斗系统承认 | enemy-ai, combat-system | enemy-ai 定义了 6 条接口（`EvaluateBreathePriority`, `EvaluateDecisiveTiming`, `ReactToPlayerCounter` 等），但 combat-system 的 Interactions 表只列出 `DecideAction(context)` 和 `GetIntentType()`。战斗系统不知道 AI 拥有这些细粒度决策能力。 |
| B2 | 结局变体数量三系统冲突 | mindset-dual-axis, romance-system, main-narrative | mindset 定义 5 基础结局 × 5 善恶变体 = 25 种；romance 提及 4 女主 × 4 结局类型 = 16 种变体；main-narrative 引用"25种结局变体"。三者未统一：romance 的 16 是子集还是独立维度？ |
| B3 | enemy-ai 引用 F6 实为 F8 | enemy-ai, character-attributes | enemy-ai 引用"角色属性 F6（相对强度比较）"，但 character-attributes 中相对强度比较是 **F8**。F6 实际是 `neixi_recovery_formula`。 |
| B4 | 调息防御 +20% 所有权未定 | combat-system, enemy-ai | combat-system 的 `action_breathe` 仅定义"回复内息"。enemy-ai 的 AI 对 breathe 的评估提到"防御 +20%"加成，但 combat-system 从未定义此规则。规则所有权不清。 |
| B5 | 顿悟凝神免死无对接 AC | epiphany-breakthrough, combat-system | epiphany 规定选择"凝神顿悟"时跳过行动并承受攻击。若此时 HP 已极低（触发条件要求 ≤20%），敌人攻击可能致死。是否有免死保护？epiphany GDD 未明确，combat-system 无相关规则。 |

### ⚠️ WARNING

| # | Issue | Systems | Brief |
|---|-------|---------|-------|
| W1 | 调息回复量不一致 | combat, character-attributes | combat 的 Action Registry 写 `breathe_recover` 默认 4；character-attributes F8 定义 `neixi_recovery = 2 + inner_power×0.2`。二者哪个是调息回复量？ |
| W2 | 境界突破缺少叙事前置条件清单 | character-attributes, epiphany, main-narrative | character-attributes 规定境界突破需"满足叙事前置条件"，但无具体条件列表。epiphany 的 `realm_thresholds` 联动机制也未指定叙事侧的 AC。 |
| W3 | 误会 `force_break()` 与感情系统地板冲突未显式交叉引用 | misunderstanding, romance | 误会 GDD 的 SEVERE 等级可调用 `force_break()`，romance 也定义了 `force_break()`。但 MINOR/MODERATE 的 `misunderstanding_mod` 是否受地板约束？未明确。 |
| W4 | 活江湖 `npc_state_change` 事件格式未与 NPC State 对齐 | living-jianghu, npc-state | living-jianghu 的 `on_trigger` 使用 `npc_state_change: { npc, attitude: -1 }`，NPC State 的合法来源列表包含"活江湖事件"但未定义 attitude 修改的精确协议。 |
| W5 | 战斗"心境战斗"概念仅心境 GDD 提及 | mindset, combat | 心境 GDD 定义了战斗结果触发心境位移（胜/败/惜败），但 combat-system 无"心境战斗"标记或战后事件接口。 |
| W6 | 冥想体力消耗仅在 epiphany 提及 | epiphany, natural-day-stamina | epiphany 称"冥想消耗半天体力"，但 natural-day-stamina GDD 的行动消耗表中未列出"冥想"作为合法行动类型。 |
| W7 | 传闻内容格式未定义 | living-jianghu, dialogue | living-jianghu 有 `content_key` 引用文本，但未说明文本格式（纯文本？对话节点？多语言 key？）。dialogue 的 7 种节点类型中无"传闻"类型。 |
| W8 | 物品系统战斗背包容量未被战斗 GDD 引用 | item-system, combat | combat 提到 `action_item` 从"战斗背包"选取，但未定义背包容量上限或进入战斗时的背包初始化规则。 |
| W9 | 顿悟 "悟后状态" buff 未在修改器层注册 | epiphany, character-attributes | epiphany 说获得"全属性 +10% buff"，但 character-attributes 的修改器层未列出 epiphany 作为临时修改器来源。 |
| W10 | 协同攻击缺少 AI 侧实现约定 | combat, enemy-ai | combat 定义了"两个己方角色攻击同一目标"的协同加成，但 enemy-ai 的多敌人协同策略未提及类似的协同规则。 |
| W11 | 顿悟的 Action 未注册 | epiphany, combat | 选择"凝神顿悟"相当于一个战斗行动（跳过回合），但 combat 的 Action Registry 无 `action_epiphany` 条目。 |
| W12 | 探索系统与活江湖钩子重叠 | exploration-insight, living-jianghu | 两系统都提供"发现世界内容"的入口（探索=主动，传闻=被动），但触发优先级和内容冲突解决规则未定义。 |

---

## Phase 3: Game Design Holism

### 🔴 BLOCKING

| # | Issue | Affected Systems | Analysis |
|---|-------|-----------------|----------|
| B6 | `total_power` 常规增长路径完全未定义 | character-attributes, epiphany, martial-arts | `total_power` = 五维之和，驱动境界系统。但 GDD 仅定义了顿悟奖励（+2~+4/次，12-18 次）为增长源。常规战斗/章节推进是否给予属性点？若仅靠顿悟，总增长约 24-72 点，从起始 40 到终章需 130 点——缺口巨大。 |
| B7 | 战斗绝境中三重决策超出注意力预算 | combat, epiphany, item | 当 HP ≤ 20% 时可能同时面临：(a) 顿悟窗口（凝神 vs 稳妥），(b) 使用道具回血，(c) 利用克制关系翻盘。三重决策在时间压力下对玩家认知负荷过高，与"5-15 回合短战斗"的快节奏设计冲突。 |

### ⚠️ WARNING

| # | Issue | Brief |
|---|-------|-------|
| W13 | 成长平台期风险 | ~~章节间若无顿悟，total_power 可能长期停滞~~ ✅ FIXED (B6) — 19 叙事节点分布于各章，无空窗期 |
| W14 | "稳妥取胜"零风险循环 | ~~选稳妥获得回复+必暴击，凝神成为傻瓜选择~~ ✅ FIXED — skip_chance_decay 衰减机制 |
| W15 | 冥想路径优于战斗路径 | ~~冥想100%零风险，理性玩家永远选冥想~~ ✅ FIXED — cap=4、不解锁招式/高阶境界 |
| W16 | 装备词条趋向数值刷怪 | ~~装备数值词条可能偏离Anti-Pillar~~ ✅ FIXED — 6条Anti-Pillar约束 |
| W17 | 误会窗口期 vs 呼吸期节奏不透明 | ~~玩家不知不觉耗尽误会窗口~~ ✅ FIXED — URGENT透明度阶段 |
| W18 | 5 结局 × 5 善恶 = 25 变体内容量巨大 | ~~25变体不可持续~~ ✅ FIXED (B2) — 已收敛为16演出脚本 |
| W19 | 破绽衰减 vs 短战斗设计 | ~~连续3-4回合克制才能触发决胜~~ ✅ FIXED — 新增设计意图分析 |
| W20 | 多人战斗（2v3）UI 复杂度爆炸 | ~~5单位信息密度过高~~ ✅ FIXED — 三层信息分级 |

### ℹ️ INFO

| # | Note |
|---|------|
| I1 | 心境双轴的"深渊"轴 (`morality ≤ -30`) 与 romance 的"魔道 override"联动良好。 |
| I2 | 活江湖 `breathing_only` 标记与自然日呼吸期定义对齐。 |
| I3 | 存档系统"战斗中不可存档"与顿悟"同一战斗只一次机会"配合良好（防 save-scum）。 |
| I4 | 音乐系统的战斗 BGM 层级切换点与战斗状态机转换对齐。 |

---

## Phase 4: Cross-System Scenario Walkthrough

### Scenario 1: 绝境顿悟决策窗口

**涉及系统**: combat-system, epiphany-breakthrough, item-system, enemy-ai

**场景**: 第二章 Boss 战，第 7 回合。玩家 HP = 18%，内息 = 5。敌人亮出"刚系"意图。

**走查**:
1. 回合结束 → epiphany 检查触发条件（HP≤20%, round≥5）→ 满足 → 概率判定成功
2. 系统呈现抉择窗口："凝神顿悟" vs "稳妥取胜"
3. **问题 A**: 此抉择窗口出现在玩家决策阶段的**哪个时机**？combat 的回合流程是"回合开始→意图公开→玩家决策→结算→回合结束"。epiphany 说"每回合结束时检查"——但如果在回合结束时才判定成功，玩家**已经出过招了**。凝神要求"跳过行动"，但行动已经执行。**时序矛盾**。
4. **问题 B**: 若抉择窗口在下一回合的"玩家决策"阶段呈现，此时玩家同时看到：正常行动菜单（6 招式 + 反制 + 决胜 + 调息 + 普攻 + 道具）**加上**顿悟选择。是替换菜单还是叠加？UI 交互未定义。
5. **问题 C**: 玩家此时 HP=18% 且只有 5 内息，enemy-ai 的刚系攻击即将命中。若选"凝神"则跳过行动承受攻击——按 character-attributes 公式，boss 一招可能打掉 30+ HP。剩余 HP 约 18 点（假设 max HP=100），有极高概率被击杀。**免死规则缺失 = 凝神等于自杀**。

**🔴 发现**: 顿悟触发时序与战斗回合流程存在结构性矛盾（Phase 2 B5 的根本原因）。需要明确：顿悟判定在回合结束，但抉择应在**下一回合**的决策阶段替换正常行动菜单。同时必须定义"凝神免死"保护（至少保留 1 HP）。

---

### Scenario 2: 终幕结局判定

**涉及系统**: mindset-dual-axis, romance-system, main-narrative

**场景**: 终章最终节点。玩家心境坐标 = (resolve: +30, worldly: -20, morality: +15)，已与女主 B 结缘。

**走查**:
1. mindset 系统计算区域：resolve=+30 → 释怀侧, worldly=-20 → 入世侧 → 结局区域="入世释怀"（侠之大者）
2. morality=+15 → 善恶档位 = "善·明"（+10~+25 区间）
3. 基础结局 = "入世释怀·善明" = 25 变体之一
4. romance 系统检查 `is_zone_compatible(heroine_B, current_zone)` — 判定女主 B 是否与"入世释怀"兼容
5. **问题 A**: `is_zone_compatible` 的判定规则在 romance GDD 中仅提及存在，未定义具体兼容矩阵（哪个女主兼容哪些区域？）。
6. **问题 B**: 若不兼容 → "道别"变体。这个变体是 25 种之一还是额外的第 26 种？结局变体的组合数学未收敛。
7. **问题 C**: main-narrative 需要提前准备所有变体的叙事内容。当前"25 变体"的定义未考虑 romance 的"同行/道别"维度，实际可能是 25 × 2 = 50 种（或至少 25 + 4 额外道别变体 = 29 种）。

**⚠️ 发现**: 结局系统的组合数学未收敛。建议统一为：mindset 决定基础走向（5种），善恶决定色调（3-5种文本变化而非独立结局），romance 决定尾声场景（同行/道别），总量控制在 5 × 1 × 2 = 10 种核心变体 + 文本色调微调。

---

### Scenario 3: 活江湖传闻→误会→态度→地板冲突

**涉及系统**: living-jianghu-layer, misunderstanding-system, npc-state, romance-system

**场景**: 呼吸期第 3 天。活江湖 tick 触发传闻"停云在绝江渡见死不救"。白苓注册了对此敏感的 misunderstanding_trigger。

**走查**:
1. living-jianghu tick → 事件 `rumor_juejiang_betrayal` 满足前置条件 → 触发
2. 误会系统监听到触发事件 → 创建 MisunderstandingInstance(target=白苓, severity=MODERATE, window=5天)
3. `misunderstanding_mod` = -2 → 写入 NPC State
4. NPC State 计算白苓最终态度 = base_attitude + milestone_floor + misunderstanding_mod
5. **问题 A**: 假设白苓当前已达里程碑 M_CRISIS（地板 = "推心置腹" = +2）。base_attitude = +2。应用 misunderstanding_mod = -2 后，计算结果 = 0（"萍水相逢"）。但地板机制要求态度不低于 +2。**谁优先？**
6. 误会 GDD 说 `misunderstanding_mod ∈ [-2, 0]` 影响态度档位。romance GDD 说"普通负面事件不可将态度打至地板以下"。
7. **问题 B**: `misunderstanding_mod` 是"普通负面事件"还是特殊类别？如果地板保护生效，则 MODERATE 误会对已达 M_CRISIS 里程碑的 NPC **无实际态度效果**——误会系统对深度关系 NPC 失去了意义。
8. **问题 C**: 若地板不保护（误会覆写地板），则误会系统实际上拥有 `force_break` 之外的第二种绕过地板机制，与 romance GDD"仅 force_break 可无视地板"矛盾。

**🔴 发现**: 误会系统的 `misunderstanding_mod` 与感情系统地板机制存在语义冲突。需要明确规则：建议方案是**地板保护态度档位显示，但 misunderstanding_mod 独立影响对话可用性和措辞**——即玩家看到的关系标签仍是"推心置腹"，但 NPC 的对话语气和可用选项因误会而临时受限。

---

### Scenario 4: 呼吸期冥想 vs 误会窗口倒计时

**涉及系统**: epiphany-breakthrough, natural-day-stamina, misunderstanding-system, living-jianghu-layer

**场景**: 呼吸期。玩家发现冥想顿悟条件已满足（累计冥想 3 天 + flag 齐备），需再冥想 1 次确定触发。同时白苓的 MODERATE 误会剩余窗口 = 3 天。

**走查**:
1. 玩家选择冥想 → 消耗半天体力
2. 自然日推进 → 活江湖 tick → 误会窗口 -1 天 → 剩余 2 天
3. 冥想成功触发顿悟 → 获得永久属性 +3 + 解锁新招式
4. 玩家满足感很高，继续旅程
5. 2 天后误会窗口关闭 → severity 定型 → 白苓态度永久下移
6. **问题**: 玩家**完全不知道**冥想这 1 天的选择间接导致了误会无法澄清。误会系统的透明度此时可能仍在 HINTED 阶段（仅有间接信号）。

**⚠️ 发现**: 此场景是 Pillar 2"选择有重量"的良好体现（成长 vs 关系的隐性权衡），但**信息不对称程度过高**可能让玩家觉得不公平。建议：当误会处于 PERCEIVED 阶段且窗口 ≤ 3 天时，朦胧化 UI 应给出更明确的"此人的疑虑正在加深"类信号，让玩家感知到时间压力。

---

### Scenario 5: 战斗失败级联→关系摧毁

**涉及系统**: combat-system, mindset-dual-axis, misunderstanding-system, romance-system, main-narrative

**场景**: 第三章叙事战斗"护送白苓过绝江渡"。玩家战败。

**走查**:
1. 战斗失败 → combat-system 触发 `OnBattleEnd(result=DEFEAT)`
2. 心境系统接收战斗结果 → resolve -5（保护失败→执念加深）, morality -1
3. 叙事处理：玩家未能及时赶到 → 同伴代办（活江湖层规则）→ **但此事件未标记 `delegate_allowed`**
4. 白苓等待未果 → 缺席误解触发 → severity=SEVERE → 3 天诀别倒计时启动
5. 玩家刚战败，可能需要疗伤/回复 → 无法立即赶赴澄清地点
6. 3 天后 → `force_break(白苓)` → 诀别 → bonded_heroine 路径永久锁定
7. **问题**: 单次战斗失败 → 心境恶化 + 关系摧毁 + 结局路径锁定。级联惩罚极度不成比例。

**🔴 发现**: 需要设计"级联熔断器"：(a) SEVERE 误会的诀别倒计时不应在战败恢复期内启动（设定"战败后 2 天保护期"）；(b) 或在叙事战斗失败时不触发缺席误解（因为不是玩家选择不去，而是能力不足）。核心原则：**不可控的失败（战力不足）不应触发可控失败才应承担的惩罚（缺席=选择不去）**。

---

## Prioritized Fix List

### Must-Fix Before Architecture (BLOCKING)

| Priority | Issue | Owner GDD | Suggested Fix |
|----------|-------|-----------|---------------|
| 1 | B6: total_power 增长路径 | character-attributes | ~~补充"章节成长节点"机制~~ ✅ FIXED 2026-06-07 — 新增 Section 8 "成长系统"，含 19 节点 / 107 点预算表 |
| 2 | B1+B11: 顿悟行动+AI接口注册 | combat-system | ~~在 Action Registry 注册 `action_epiphany`；在 Interactions 表补充 enemy-ai 的 6 条接口~~ ✅ FIXED 2026-06-07 — Action Registry 新增 `action_epiphany` + `action_epiphany_skip`；Interactions 表扩展至 enemy-ai 8 条 + epiphany 3 条双向协议 |
| 3 | B5+S1: 顿悟时序+免死 | epiphany, combat | ~~明确：判定在回合结束，抉择在下一回合开始时呈现（替代正常决策）；凝神期间 HP 不可降至 0（免死 1 轮）~~ ✅ FIXED 2026-06-07 — 新增 "Epiphany Integration Protocol" 章节：回合末判定→下回合初排他抉择；凝神免死（HP 钳位 1）；稳妥取胜立即执行 |
| 4 | B2+S2: 结局变体收敛 | mindset, romance, main-narrative | ~~统一定义为：5 基础走向 × 善恶色调（文本变化）× romance 尾声（同行/道别）= 10-15 核心变体~~ ✅ FIXED 2026-06-07 — 三方统一为 16 种演出脚本（6 结局分支 × 3 伴侣状态 - 2 魔道限制）；善恶 5 档仅为旁白色调层，不产生独立分支。SSoT: romance-system C4 |
| 5 | B3: F6→F8 编号错误 | enemy-ai | ~~纠正所有对 character-attributes 公式的引用编号~~ ✅ FIXED 2026-06-07 — enemy-ai 中 combat-system 引用修正：F6→F8（调息回复）、F7→F9（意图洞察概率） |
| 6 | B4: 调息防御+20% | combat-system 或 enemy-ai | ~~决定规则归属：若为正式规则则写入 combat Action Registry；若仅为 AI 评估权重则从规则描述中移除"防御+20%"~~ ✅ FIXED 2026-06-07 — 规则归属 combat-system：Action Registry `action_breathe` 明确"防御 +20% 持续至下回合结算前" |
| 7 | B7+S1: 绝境三重决策 | epiphany, combat | ~~当顿悟窗口呈现时，道具和正常行动**不可选**——凝神 vs 稳妥是排他性二选一，消除认知过载~~ ✅ FIXED 2026-06-07 — Epiphany Integration Protocol 明确：顿悟抉择状态下正常行动菜单不可用，排他性二选一替代 |

### Should-Fix Before Vertical Slice (WARNING)

| Priority | Issues | Brief |
|----------|--------|-------|
| 8 | W1 | ~~统一调息回复量：breathe 应使用 character-attributes 的 `neixi_recovery` 公式，删除硬编码 4~~ ✅ FIXED 2026-06-07 — `action_breathe` 已改为引用 F8 `meditation_recovery` 公式 |
| 9 | W3+S3 | ~~明确 misunderstanding_mod 与地板机制交互规则~~ ✅ FIXED 2026-06-07 — 新增"地板保护下的独立效果层"：地板保护态度档位，误会通过独立对话效果层继续影响 |
| 10 | W5 | ~~combat-system 补充"心境战斗"标记和战后事件发射接口~~ ✅ FIXED 2026-06-07 — Interactions 表新增 `OnBattleEnd(result)` 战斗→心境接口，含位移规则和 battle_tag |
| 11 | W6 | ~~natural-day-stamina 行动表补充"冥想"条目~~ ✅ FIXED 2026-06-07 — 体力消耗列表新增冥想条目（max_stamina × 0.5） |
| 12 | W9 | ~~character-attributes 修改器层补充 epiphany 临时 buff 来源~~ ✅ FIXED 2026-06-07 — 临时来源新增"顿悟悟后状态 buff（全属性 ×1.1）" |
| 13 | W11+Phase4-S1 | ~~combat Action Registry 注册 `action_epiphany`~~ ✅ FIXED 2026-06-07 — 已在 B1 修复中完成 |
| 14 | W14+W15 | ~~平衡凝神 vs 稳妥 vs 冥想的风险-收益曲线~~ ✅ FIXED 2026-06-07 — 新增路径收益分层表 + 稳妥取胜机会衰减（skip_chance_decay=0.15）+ 冥想约束（cap=4、不解锁招式/高阶境界） |
| 15 | S5 | ~~设计级联熔断器~~ ✅ FIXED 2026-06-07 — 战败后 2 天 `absence_immunity` 保护期；核心原则"不可控失败不触发可控选择惩罚" |
| 16 | W2 | ~~境界突破叙事前置条件清单~~ ✅ FIXED 2026-06-07 — character-attributes 新增 8 行境界→叙事节点映射表 |
| 17 | W4 | ~~活江湖 npc_state_change 协议格式~~ ✅ FIXED 2026-06-07 — 定义精确 YAML 签名（npc_id/field/value/source_event）+ 合法 field 枚举 |
| 18 | W7 | ~~传闻 content_key 格式未定义~~ ✅ FIXED 2026-06-07 — 定义多语言 key 命名规范（rumor_/letter_/delegation_/ambient_前缀）+ 文本表构建期校验 |
| 19 | W8 | ~~物品系统战斗背包容量未被 combat 引用~~ ✅ FIXED 2026-06-07 — Interactions 表新增 `GetCombatPouch()` 物品→战斗接口 |
| 20 | W10 | ~~协同攻击缺少 AI 侧实现约定~~ ✅ FIXED 2026-06-07 — 多人战斗规则新增"敌方协同"条目（非对称设计） |
| 21 | W12 | ~~探索系统与活江湖钩子重叠~~ ✅ FIXED 2026-06-07 — 新增探索-传闻内容触发优先级规则（探索优先+传闻兜底+共享flag协调） |
| 22 | W16 | ~~装备词条趋向数值刷怪~~ ✅ FIXED 2026-06-07 — item-system 新增 Anti-Pillar 约束 6 条（贡献上限 40%、无套装、章节门控等） |
| 23 | W17 | ~~误会窗口期透明度不足~~ ✅ FIXED 2026-06-07 — 新增 URGENT 透明度阶段（window≤3 时脉动+朦胧化文学信号） |
| 24 | W19 | ~~破绽衰减 vs 短战斗设计~~ ✅ FIXED 2026-06-07 — combat-system F7 后新增设计意图说明（连续3轮克制达标分析） |
| 25 | W20 | ~~多人战斗 UI 信息密度爆炸~~ ✅ FIXED 2026-06-07 — 新增信息分层规则（焦点层/概览层/告警层）+ 密度上限 6 条 |

---

## Traceability to Design Pillars

| Pillar | Alignment Score | Notes |
|--------|----------------|-------|
| P1 (江湖是活的) | ✅ Strong | 活江湖层 + 误会系统 + NPC 独立旅程配合良好；探索-传闻优先级已明确 |
| P2 (选择有重量) | ✅ Strong | 凝神收益分层 + 稳妥机会衰减解决了"傻瓜选择"问题；级联熔断器保护不可控失败 |
| P3 (武侠味先于游戏味) | ✅ Strong | 朦胧化 UI + 境界文学 + 一击决胜 + 战斗 UI 信息分层设计一致 |
| P4 (情感深度优先) | ✅ Strong | 16 演出脚本（非 25 独立结局）确保每个变体有足够叙事深度 |

---

## Anti-Pillar Compliance

| Anti-Pillar | Status | Risk |
|-------------|--------|------|
| NOT 开放世界 | ✅ Compliant | 地图/场景管理明确为区域制 |
| NOT 后宫系统 | ✅ Compliant | 结缘互斥规则 + 每周目一人约束 |
| NOT 高频战斗/数值刷怪 | ✅ Compliant | W16 修复：装备贡献上限 40%、无套装、确定性掉落、精炼上限 3 次 |

---

## Recommendations

1. **立即行动**: 修复 B6（total_power 增长）最为紧急——它影响整个数值基线，架构阶段需要明确增长曲线。
2. **结局系统工作坊**: B2 需要 mindset/romance/main-narrative 三个 GDD 的作者共同确定最终变体矩阵。
3. **战斗-顿悟接口规范**: B1/B5/B7/W11 可通过一次 combat-system 修订集中解决——定义完整的顿悟交互协议。
4. **误会-感情交互规则**: W3/S3 需要一份短文档明确 `misunderstanding_mod` 如何与里程碑地板共存。
5. **级联惩罚审查**: S5 暴露的问题需要一条全局规则——"不可控失败不触发可控选择的惩罚"。

---

## Next Steps

- [ ] Fix all BLOCKING issues (B1–B7)
- [ ] Run `/design-review` on each modified GDD
- [ ] Update `entities.yaml` after formula reconciliation
- [ ] Run `/gate-check` when all BLOCKING resolved
- [ ] Consider `/architecture-decision` for "结局变体矩阵" once consensus reached
