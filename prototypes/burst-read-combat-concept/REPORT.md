# Concept Prototype Report: Burst+Read Combat

> **Date**: 2026-06-02
> **Prototype Path**: Paper
> **Concept File**: `design/gdd/game-concept.md`

---

## Hypothesis

> 如果玩家在每回合开始能看清对手意图（刚/柔/巧 attack tell），并从自己的 build（2-3 套武学 + 1 套心法）中选择克制策略，他们会感受到"读人取胜"的满足感（≠ 机械 RPS 计算）—— 类似实际武侠对决的智力博弈。
>
> **可证伪信号**：在 3 场连续战斗中，玩家
> - 在每次决策前自然停顿思考（engagement signal）
> - 成功反制时有可见正面反应（言语 / 想再打一次）
> - 战斗失败后主动想再尝试

---

## Riskiest Assumption Tested

> "在回合制 + 信息透明的设定下，'读人 + 反制 + 一击决胜'的循环不会沦为'必胜公式'或'枯燥 RPS' —— 玩家在 5-15 回合的小窗口内仍有有意义的策略空间。"

**结果**：✅ 假设**总体成立**。3 场战斗都产生了真实的"读 → 反制 → 寻找一击决胜窗口"循环。没有出现"必胜公式"（每场战斗都需根据情境调整），但**也暴露了 build 平衡的具体缺陷**（详见 Pivoting 部分）。

---

## Approach

**Path chosen:** Paper（规则文档 + 模拟战斗 play log）

**Reason for path:** 回合制战斗的核心是**决策与规则**，不是 timing 与 feel。Paper 在 1 天内可完整验证 RPS + 心法 + 一击决胜的决策空间是否有趣，**便宜、快速、可重复迭代**。建在 Godot 之前 paper 的规则必须先稳——先写代码 = 在不知道规则是否有趣的情况下开始建实现 = 浪费。

**Shortcuts taken (intentional):**
- 没有伤害随机数（所有伤害固定，避免骰子运气干扰策略判断）
- 无视觉/听觉反馈（留给后续 Engine spike）
- 无动作动画 timing（纯回合制）
- AI 是死的 if-else（不验证 AI 质量）
- 武学只 5 招（先验证 5 招够不够）
- 心境系统不影响战斗（留给后续设计）
- 无顿悟机制（留给后续设计）
- **设计师同时扮演玩家**（已在 play-log 中标注此 bias）

---

## Result

3 场战斗的关键观察（详见 `play-log.md`）：

### Battle 1: 1v1 vs 李无双（5 回合，玩家胜）

- **核心循环干净有效**：玩家在 R1-R2 连续反制建立优势，R5 触发一击决胜完美收尾
- **决策深度真实**：每回合花 30s-1min 思考；R3 "听风辨形 vs 守拙 vs 硬攻" 是真纠结
- **一击决胜定位准确**：R5 触发产生"高手过招"叙事感
- **避影身 + 千里追风 combo** 让玩家一回合无反制机会 —— 防止"无脑反制赢"

### Battle 2: 1v2 vs 李无双 + 孤山弟子（9 回合，双败）

- **核心循环 R1-R3 仍然成立**（R3 双反制让破绽推到 3 触发一击决胜窗口）
- **关键缺陷暴露**：玩家 build **无反巧方案**——弟子持续巧 attack 玩家"无解"，R5 后陷入 stagger 滚雪球
- **斜挑（破绽 +1 自带）过强**：1v2 节奏崩溃的主因
- **反制是 dominant strategy**：玩家在 R1-3 都选反制，缺乏战术多样性

### Battle 3: 2v2 玩家 + 谢残翁 vs 李无双 + 师弟（4 回合，玩家胜）

- **协同 burst 自然涌现**：R1 双反制 / R3 集火击杀 / R4 集火收割 boss
- **角色分工自然形成**：主角主攻 boss / 谢残翁防控
- **AI 一致用刚** 让反制太容易 —— 实际游戏 AI 应该会动态调整
- **战斗 4 回合太快**：协同太强让一击决胜无机会触发

### Q5 答案：**Y** —— 想再打，但要测不同 build 组合 + 多巧系敌方 + 反反制

---

## Metrics

| Metric | Value |
|---|---|
| Path used | Paper |
| Iterations to playable | N/A（Paper 一稿即可玩）|
| Prototype duration | ~3 小时（规则设计 + 3 场模拟 + 报告）|
| Playtesters | 1 internal（designer self-simulation；non-ideal methodology，已声明）|
| Feel assessment | N/A（feel 是 Engine spike 阶段任务）|
| Hypothesis verdict | **PARTIALLY CONFIRMED** —— 核心循环成立，但 build 平衡有具体缺陷 |

---

## Recommendation: PROCEED with refinements

Burst+Read 战斗的**核心哲学**被 paper prototype 验证：
- 信息透明 + 反制窗口 = 真实策略深度
- 内息 / 一击决胜 / 破绽 三资源系统制造有意义紧张
- 1v1 干净，2v2 协同自然涌现

**6 个具体设计问题**需在 GDD/MVP 阶段解决（详见 Lessons Learned），但都是 "tuning / build" 级别，**不是 "concept" 级别**。这表示概念哲学可以投入正式 GDD 编写，但需在 GDD 中明确解决这些遗留问题。

---

## If Proceeding

**Core tuning values discovered:**

| 参数 | Paper 中验证值 | 备注 |
|---|---|---|
| 战斗回合数 | 5-15（最短 4，最长 9，Battle 4 估计 12-15）| 与 game pillar 一致 |
| 克制方向加成 | 1.5×（克制方造成）/ 0.5×（被克制方）| Paper 验证有效，但 **Engine spike 反馈 3:1 差距过大**；GDD 建议调为 1.3×/0.7×（~1.9:1）|
| 反制成本 | 2 内息 / 6 伤害 | **略 OP，应提至 3 内息** |
| 内息回复 | +1 基础 + 心法 加成 | 浩然心法 +1 让玩家充能优势明显 |
| 一击决胜 cost | 3 内息（含招式 cost）| 已澄清 |
| 一击决胜 倍率 | 2.5× + 无视防御 | Battle 1 验证：白虹 4×2.5=10 击杀有"分胜负"感 |
| 破绽 cap | 3 → 跳过 + 受击 +50% | 触发后**重置为 0**（prototype 中决定）|
| 玩家初始 build | 5 招 + 1 心法 | **太少**——缺反巧选项；建议 6-7 招 + 2 心法 槽 |

**Assumptions confirmed (来自 concept doc):**
- ✅ 短回合数（5-15 回合）真实可达且不冗长
- ✅ 信息透明（intent tell）确实创造"读人"快感而非乏味 RPS
- ✅ 克制三系（刚/柔/巧）数量适中，不超载玩家心智
- ✅ "一击决胜"作为偶发性 burst，1-2 场 1-2 次的节奏正确
- ✅ 武学 + 心法 分离让 build 有差异化空间
- ✅ 同伴系统天然支撑协同战斗（无需额外组合技规则）

**Assumptions disproved:**
- ❌ "玩家只需 5 招就够"——缺反巧让 1v2+ 不平衡
- ❌ "反制是中等强度选项"——实际 reveal 为 dominant strategy
- ❌ "破绽是简单的累加 → cap" 机制——需要衰减规则避免不必要永锁

**Emergent mechanics（值得写入 GDD）:**
- 💡 **谢残翁 防御充能** 节奏（R2 用铁布衫卸力 + 充能）—— 这是"剑客防御"和"拳师重拳"的角色感来源
- 💡 **协同 burst 双反制** = 1 回合击杀弱方（Battle 3 R3）—— 应明确为"配合反制"叙事点
- 💡 **避影身 + 优先攻击** = boss 阶段 timing 装备 —— 应明确为 boss 设计语言
- 💡 **听风辨形（情报招）** 在被动回合的价值 —— 可发展为"心法满血 + 听风 = 看下回合 + 节省内息" 的协同设计

**Next steps:**

1. **修订 game-concept.md**（5 分钟）—— 在 "战斗" 段标注本 prototype 学到的 6 项 tuning 调整
2. **(可选) 跑第二轮 paper prototype**（半天）—— 应用 6 项修正，再跑 1 场 1v2 验证反巧方案 + 调整后反制不再 dominant
3. **Engine spike**（半天-1 天）—— 在 Godot 中实现最简 1v1 战斗场景，验证：
   - "一招分胜负"的视觉爆发感是否成立
   - "intent tell"的 UI 呈现是否清晰
   - 回合切换的 pacing 是否符合"短而重"
4. `/design-review design/gdd/game-concept.md` —— 用本 prototype 学习重新审视 concept doc
5. `/gate-check` —— 确认是否准备好推进 Systems Design 阶段
6. `/art-bible` —— 定义视觉风格（建议先做，再写 GDD）
7. `/map-systems` —— 将概念分解为所有 game systems
8. `/design-system 战斗` —— 战斗系统 GDD（嵌入本 prototype 学习于 Tuning Knobs 和 Formulas 段）

---

## If Pivoting

（不适用 —— 本次为 PROCEED with refinements，但保留以下信息以备 Round 2 paper prototype 参考）

**6 项具体修订建议**（按严重度排序）：

| # | 问题 | 修订建议 | 严重度 |
|---|---|---|---|
| 1 | 玩家无反巧方案 | 增加反巧招式"听风诀"（巧 2 内息，反制巧系 → 4 伤害）或心法槽"御风心法"被动反制 | 🔴 高 |
| 2 | 反制是 dominant strategy | 反制 内息 cost 从 2 提至 **3**；OR 限制"连续 2 回合反制后第 3 回合冷却" | 🟡 中 |
| 3 | 斜挑破绽 +1 自带过强 | 改为"hit success 时 +1"或基础 attack（去掉 stagger 效果）| 🟡 中 |
| 4 | 破绽不衰减 | 加规则：每回合 end，如本回合无新增 stagger 则 -1 | 🟡 中 |
| 5 | 一击决胜 cost 歧义 | 明确"3 内息 total（含招式 cost）" —— 已在 rules.md 中决议 | 🟢 低 |
| 6 | 避影身 + 优先攻击 edge case | 规则更明确："优先攻击 替代 normal action，clash 不适用" | 🟢 低 |

---

## If Killing

（不适用）

---

## Lessons Learned

### What assumptions were broken by actually building this?

1. **"玩家初始 5 招够用"** —— 缺反巧方案让 1v2 不平衡。这个发现如果直到 Godot 实现后才出现，会浪费 1-2 周代码工作
2. **"反制是中等强度选项"** —— 反制 6 伤害 / 2 内息 实际是 dominant strategy。需要在 GDD 中调到 3 内息 OR 引入"敌方反反制" AI
3. **"破绽简单累加"** —— 不衰减就会让 1v2+ 玩家陷入 stagger 死亡螺旋。需要衰减或重置规则

### What surprised us that didn't show up in the brainstorm?

- ✨ **协同 burst 自然涌现**：Battle 3 双反制 1 回合击杀师弟，完全没设计"组合技"机制就出现了"配合"感
- ✨ **防御充能节奏（柔卸力 + 内息 +1）** 是真正的角色感来源 —— 不是单纯"防御"，而是"老侠剑客的从容"
- ✨ **听风辨形（情报招）** 在被动回合（敌方防御 phase）变成关键选择 —— 没攻击对象时玩家仍有有意义决策
- ✨ **一击决胜窗口的"计算感"** —— 玩家在 Battle 1 R4 主动推破绽到 3 是真"读未来 2 回合"的策略，超出原本 brainstorm 预期

### What would we test differently next time?

1. **找真人测试** —— Designer self-simulation 有 bias。本 prototype 的"决策时长" / "Q5 想再打吗" 等信号需要真人验证
2. **测试 multiple build 组合** —— 本次只测了"主角浩然心法 + 5 招"一种 build，没测 build 差异化体验
3. **测试敌方 AI 多样性** —— 本次 AI 几乎全用刚系（反制 dominant 部分原因），实际游戏需要 AI 动态选择系
4. **测试"反反制"层** —— 师弟有"后发剑（反制刚）"但 prototype 中没触发；这层防御应该单独验证
5. **测试 Engine spike** —— Paper 验证决策深度，但"一招分胜负"的视觉爆发感、回合切换的 pacing、UI 信息密度 都需要 Engine 验证
6. **加入轻度伤害随机数**（±1）—— 当前固定值让 sim 过于"确定"，实际游戏的轻微 RNG 会改变 read 决策

---

## Engine Spike Findings (2026-06-02)

> **Path**: Engine (Godot 4.6.3 + C# / .NET 10)
> **Scope**: 1v1 placeholder spike (主角 3 招 vs 李无双 3 招), 全代码生成 UI, 无美术资产
> **One-shot success**: ✓ 首次编译运行通过, 0 iteration rounds

### Feel Question Results (开发者实测)

| Q | 结论 | 细节 |
|---|---|---|
| **Q1** 一击决胜视觉爆发 | ✅ 窗口时机成立 | 满血+反制条件被接受为"先发制人奖励" |
| **Q2** Intent tell 清晰度 | ✅ 清晰 | 系颜色编码（刚红/柔蓝/巧绿）一眼可辨 |
| **Q3** Pacing 短而重 | ✅ 成立 | 每回合 5-10s 节奏对 |
| **Q4** 同时结算清晰度 | ✅ 看得到 | 双方伤害同时飘出 + 双侧 shake |

### Engine Spike 新增发现

| # | 发现 | 严重度 | 建议 |
|---|---|---|---|
| 7 | **克制方向 1.5×/0.5× 差距过大**（开发者反馈 3:1 比率让猜错等于白打） | 🟡 中 | GDD 调为 **1.3×/0.7×**（~1.9:1），保持"猜对有明显优势"但不碾压 |

### Engine Spike 结论

Paper prototype 验证了**决策深度**，Engine spike 验证了**交互节奏**。
两者结合给出完整的 PROCEED 信号：

- ✅ 核心 feel 成立 —— intent tell + 反制 + 一击决胜的时机感都通过
- ✅ Godot 4.6.3 + C# 技术栈**首次编译运行通过**，stack 可行性确认
- ⚠️ 克制倍率需调整（从 3:1 降至 ~2:1），留给战斗 GDD
- ⚠️ 视觉/音效/动画是 production 阶段工作，spike 的 placeholder UI 符合预期

---

> *Prototype code location: `prototypes/burst-read-combat-concept/`*
> *Engine spike code location: `prototypes/burst-read-combat-concept/engine/`*
> *This code is throwaway. Never refactor into production.*
