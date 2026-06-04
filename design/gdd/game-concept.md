# Game Concept: 《风止》

*Created: 2026-06-02*
*Status: Draft*

---

## Elevator Pitch

> 你是风止山庄的最后弟子。一夜之间师门尽灭，你披白衣下山，沿着三个仇人的踪迹一路追查 —— 江南烟雨、塞北风雪、戈壁孤城。但越追越发现，这不是一桩简单的师门血案：有人在用整个江湖做局，而你的师门，不过是开局的一颗弃子。

**这是一款 2D 像素武侠 RPG，主线剧情驱动，回合制战斗，强调侠义抉择、忠贞感情与江湖人情，提供 5 种结局（含 1 个隐藏魔道结局）。**

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 2D 像素武侠 RPG / 叙事驱动 / 回合制战斗 |
| **Platform** | PC (Steam) v1.0，后续考虑 Switch / 移动端 |
| **Target Audience** | 18-40 岁中文武侠 RPG 玩家、慢游戏叙事爱好者、多周目 RPG 玩家（详见 Player Profile） |
| **Player Count** | 单机单人 |
| **Session Length** | 1-2 小时为主，可灵活拆为 30 分钟片段 |
| **Monetization** | 买断制（Premium）|
| **Estimated Scope** | Large (8-14 months, solo + AI-assist) |
| **Comparable Titles** | 《逸剑风云决》《仙剑奇侠传 4-5》《极乐迪斯科》《Pentiment》 |

---

## Core Fantasy

**"我从一个名不见经传的末代弟子，走到了改变江湖走向的位置 —— 而我是谁，由我自己的每一次选择决定。"**

玩家体验的核心情感，是宇文逸独斩南疆和、白唯一弹琴待援的那种**「以小人物之力撬动大格局」**的史诗瞬间，但**镶嵌在玩家亲手做出的、不可撤销的抉择链条之中**。

不是"成为最强高手"的权力快感，而是：
- **以剑代言的情感重量** —— 师门血仇是动机，但你如何完成它定义你
- **江湖里被记住的方式** —— 你最终是被江湖敬仰的侠者、被惧怕的复仇者、被遗忘的隐士，还是被警惕的魔道？
- **得之我幸、失之我命的爱情** —— 三位女主的彗星轨迹，单一选择的承诺，与遗憾的尊严

---

## Unique Hook

**像《最后生还者》般情感聚焦的复仇主线，AND ALSO 武侠层层揭开的阴谋调查感 + 烟雨江湖式的"活江湖"奇遇密度 + 五结局心境演变的视觉化。**

具体差异化点：

1. **「彗星模型」感情系统** —— 三位女主与三位同伴都有独立旅程，与主角的相遇是交叉而非陪伴。每位长期"不在场"通过暗号/书信/传闻/撞见维持存在感
2. **「心境双轴 + 魔道触发」** —— 不是简单的善恶值，而是 2D 双轴（执念↔释怀 × 入世↔出世）+ 1 个隐藏深渊
3. **「冷峻像素 + 视觉演变」** —— 视觉风格本身演绎心境（江南温暖 → 北方过渡 → 塞外冷峻 → 终幕色调随结局变化）
4. **「Content Overflow Principle」** —— 未玩到的支线可由离队同伴代办，玩家通过书信/重逢得知，江湖在玩家不在场时也在转动
5. **「朦胧化 UI（战斗外 / 战斗内分治）」** —— 长期成长与关系数据去数据化以文学/视觉表达（"功力"是境界文学、好感度完全隐式、心境为视觉双轴、顿悟概率不显示）；战斗内保留完整数值化（HP、伤害、心法效果、buff 时长）以服务战术决策。**"战斗是棋局，让棋手看到棋盘；江湖是诗，让游人忘掉数字。"**

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 5 | 冷峻像素的强对比色调、boss 战的爆发瞬间演出、关键剧情的 CG 插画 |
| **Fantasy** (make-believe, role-playing) | 2 | 末代弟子的复仇之旅、侠义/魔道的身份选择、忠贞武侠的爱情 |
| **Narrative** (drama, story arc) | **1** | 复仇 → 阴谋揭示的双层叙事、5 结局、误会与遗憾、感情线 |
| **Challenge** (obstacle course, mastery) | 4 | Burst+Read 战斗的预判挑战、boss 战的策略组合 |
| **Fellowship** (social connection) | N/A | 单机叙事，无社交 |
| **Discovery** (exploration, secrets) | **1** | 伏笔型阴谋谜案支线、暗号系统、洞察机制、隐藏武学秘籍 |
| **Expression** (self-expression, creativity) | 3 | 武学+心法 build 搭配、心境路径选择、四种结局的玩家自定义 |
| **Submission** (relaxation, comfort zone) | 6 | 客栈休息节奏、自然日推进、呼吸期的从容探索 |

### Key Dynamics (Emergent player behaviors)

- 玩家会主动**追查伏笔** —— 看到某 NPC 的奇怪行为时，触发洞察、记下细节，期待主线终幕揭示
- 玩家会**在多个钩子之间做时间分配** —— 限时支线 + 同伴等待 + 主线推进的张力让每次选择有重量
- 玩家会**与同伴/女主"通信"维持关系** —— 收到书信后斟酌回信内容，因不在场而珍惜每次重逢
- 玩家会**因误会而后悔** —— 错过澄清的瞬间会成为长久遗憾，驱动二周目"做得更好"
- 玩家会**多周目尝试 5 种结局** —— 心境系统的隐式 + 不同感情线的尾声，让每次重玩都有新发现
- 玩家会**口耳相传顿悟时刻** —— "我在打第二个 boss 时绝境触发了顿悟，学会了第四招" 类的玩家社区话题

### Core Mechanics (Systems we build)

1. **回合制武学战斗（Burst + Read）** —— 短回合数（5-15 回合）、信息透明（敌方 intent 公开 / 己方隐藏，机械化"高手过招"的信息不对称）、克制三系（刚/柔/巧）、一击决胜的爆发节点。**协同 burst（多角色同回合反制）作为涌现机制**自然形成于多人战斗，无需额外组合技规则。详见 [`prototypes/burst-read-combat-concept/REPORT.md`](../../prototypes/burst-read-combat-concept/REPORT.md)
2. **武学组合系统** —— 6-7 套武学（残片/拓本/完本三种获取形态）、5-6 套心法（自由搭配），玩家自由组合 build
3. **心境双轴系统** —— 隐式 2D 心境图（执念↔释怀 × 入世↔出世），决定 5 结局走向 + NPC 反应 + 武学获取倾向
4. **彗星模型感情系统** —— 三女主（白苓 / 沈夜雪 / 柳惊鸿）轮流陪伴 + 暗号 / 书信 / 传闻 / 撞见的"活江湖"层 + 单点承诺机制
5. **线性骨架 + 呼吸期探索** —— 主线节点开关式推进，每节点解锁呼吸期，期间自由探索（带"洞察"机制揭示隐藏内容）
6. **自然日 + 体力软门槛** —— 体力消耗驱动玩家访问客栈，客栈休息推进日历，日历触发活江湖事件
7. **顿悟突破机制** —— 概率触发的成长惊喜，玩家可选择"凝神顿悟"或"稳妥取胜"，章节门限防止前期破坏

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | 5 结局、3 感情承诺选项、心境抉择不可撤销、洞察是否触发、是否凝神顿悟、追支线 vs 推主线 | **Core** |
| **Competence** (mastery, skill growth) | Burst+Read 战斗的预判精进、武学 build 优化、顿悟突破的领悟感、5 boss 战的策略深化 | Supporting |
| **Relatedness** (connection, belonging) | 三女主 + 三同伴的人物弧、彗星模型的重逢温度、误会与释怀、终幕的"她的下落" | **Core** |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Explorers** (discovery, understanding systems, finding secrets) — How: 伏笔型阴谋谜案支线、暗号系统、洞察机制、隐藏武学、5 结局解锁
- [x] **Achievers** (goal completion, collection, progression) — How: 主线推进、武学收集、5 结局收集、关系线达成、所有同伴信件归档
- [ ] **Socializers** — N/A 单机
- [ ] **Killers/Competitors** — N/A 无 PvP

### Flow State Design

- **Onboarding curve**：起点章节（落尘谷，约 1 小时）作为完整教学，包含一场战斗 + 一段对话 + 一个支线 + 第一次心境选择。玩家在结束时已学会全部基础系统
- **Difficulty scaling**：boss 战难度随章节递进，但 build 自由度让玩家有多种应对方式；顿悟机制让卡关玩家有"绝境突破"出路
- **Feedback clarity**：朦胧化 UI 用文学语言反馈（"内息渐充" / "已可与一流高手相搏"），玩家凭叙事感知成长
- **Recovery from failure**：战斗失败回到上次客栈休息，无 perma-death；剧情选择不可回溯但每条路径都有完整叙事价值（包括"魔道"结局）

---

## Core Loop

### Moment-to-Moment (30 秒) —— 战斗回合内

读对手亮出的招式 → 识别属性（刚/柔/巧）→ 在自己的 build 里选择克制招式或心法增益 → 寻找"一击决胜"时机 → 看到 boss 蓄力大招时考虑是否用克制武学打断 / 触发顿悟。

**核心快感：高手过招的"读人"与"一招分胜负"的爆发瞬间。**

### Short-Term (5-15 分钟) —— 一个呼吸期片段

进入新场景 → 接收 3-5 个并行钩子（告示 / 暗号 / NPC 行为 / 同伴书信 / 江湖传闻 / 洞察发现）→ 选择追一个钩子 → 经历一段对话或战斗 → 获得反馈（武学残片 / 物品 / 剧情线索 / 心境位移 / 关系节点 / 顿悟）

**"我必须在多个有意义的事情之间做选择，而我的选择会塑造我之后的体验。"**

### Session-Level (30-120 分钟) —— 一次会话

推进 1-2 个主线节点，或完成 1 个呼吸期内的大部分支线。Session 通常结束在客栈休息（自然日推进）或剧情转场。**每次会话都有明确的"我今天完成了什么"反馈** —— 一桩谜案解开了、一封信收到了、一段感情节点推进了、一套武学完本到手了。

**留存钩子**：剧情转折、未解的支线、同伴最新书信中的暗示、刚解锁的新区域。

### Long-Term Progression —— 数日至数周

| 维度 | 长期成长 |
|---|---|
| **功力** | 通过剧情节点、boss 战、秘境、长期修炼递进。从"初窥门径"至"功参造化" |
| **武学库** | 6-7 套武学逐章解锁，玩家组合 build 越来越精 |
| **心境** | 2D 心境图上的位置缓慢漂移，5 结局逐渐显现 |
| **感情线** | 三女主与三同伴的关系深化，过渡章节"三女主共聚"的承诺时刻成为情感巅峰 |
| **江湖图景** | 玩家对主线阴谋的理解层层加深，所有伏笔在终幕汇聚 |

### Retention Hooks

- **Curiosity**: 主线阴谋的真相、各支线"江湖百晓生"更新、错过支线的同伴代办结果、未解开的误会真相
- **Investment**: 已建立的感情线、心境演变的累积、收集到一半的武学完本
- **Mastery**: Burst+Read 战斗的精进、顿悟时机的把握、5 结局的解锁挑战
- **Social** (轻度)：玩家社区讨论各自的结局、各自的女主选择、各自的"我那时候是怎么选的"

---

## Game Pillars

### Pillar 1: **江湖是活的，不止围绕你而转**
同伴有自己的旅程，NPC 有自己的命运，时间在你不在场时也在推进。误会会因信息不对称而自然产生，支线会因同伴代办而继续完成。

*Design test*：当我们在"加一个新支线"和"让现有同伴的离队期间更生动"之间犹豫时，这条支柱说选**后者**。

### Pillar 2: **每个选择必须有重量，包括错过的**
玩家做出的每个抉择 —— 杀仇人 vs 留活口、追支线 vs 推主线、选白苓 vs 沈夜雪 —— 都不可撤销，且通过明显的叙事反馈让玩家感受到分量。错过同样有重量。

*Design test*：当我们在"允许玩家回溯重选"和"让某个决定永久改变后续"之间犹豫时，这条支柱说选**后者**。

### Pillar 3: **武侠味先于游戏味**
当机制设计与武侠传统冲突时，选武侠。不做"剑法升到 5 级"，因为武侠不是这样讲故事的；做"功力提升让所有武学发光"。系统去数据化，用文学语言表达数据。

*Design test*：当我们在"经典 RPG 设计"和"武侠原汁原味的表达"之间犹豫时，这条支柱说选**后者**。

### Pillar 4: **情感深度优先于内容广度**
5 个让你哭的角色 > 50 个名字都记不住的角色。3 个写到极致的女主 > 10 个浅描的"恋爱对象"。每位有名有姓的角色都必须有完整人物弧。

*Design test*：当我们在"再加一个新区域/新角色"和"把现有的人物再写深"之间犹豫时，这条支柱说选**后者**。

### Anti-Pillars (What This Game Is NOT)

- **NOT 开放世界**：不做"百平方公里地图任你跑"。我们做线性骨架 + 呼吸期。开放世界的内容密度需求会摧毁体量，并违反支柱 4
- **NOT 后宫系统**：一周目最多一位伴侣。不允许"集齐所有女主"。后宫感会稀释忠贞武侠的情感重量，摧毁支柱 2 与支柱 4
- **NOT 高频战斗/数值刷怪**：不做"打 100 个山贼涨等级"。战斗少而重。与回合制 Burst 哲学和剧情为主的体验冲突

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| 《逸剑风云决》 | 江湖氛围、回合制武学对决、奇遇与多结局 | 更聚焦剧情（不做开放世界）、忠贞感情系统、心境系统决定结局、视觉演变 | 验证了"中文 2D 武侠 RPG"在 Steam 的市场（~400K+） |
| 《仙剑奇侠传 4-5》 | 命运感、史诗瞬间、感情线的厚度 | 五结局而非线性悲剧、玩家选择决定走向 | 验证了"情感驱动 RPG"在中文圈的长期生命力 |
| 《极乐迪斯科》 | 朦胧化系统、文学语言、心境/技能的叙事化 | 武侠语境、回合制战斗、感情线 | 验证了"去数据化 UI"的成熟可行性 |
| 《Pentiment》 | 单一场景的深度调查、中世纪推理 + 现代叙事手法 | 武侠语境、不限于密闭场景 | 验证了"叙事推理"游戏的小众但忠实受众 |
| 《最后生还者》 | 情感聚焦的复仇主线、伴侣关系的深度 | 多结局、玩家抉择系统、武侠题材 | 验证了"复仇情感"作为主线驱动的有效性 |
| 《巫师 3》 | 怪物契约式支线（独立完整 + 间接联系主线）、伴侣选择前置 | 像素武侠、回合制、更小体量 | 验证了"支线作为微型完整故事"的设计模式 |
| 《Hyper Light Drifter》 | 冷峻像素美学、低饱和高对比 | 武侠语境 + 局部温暖色调（B 融合） | 验证了像素艺术的高级表现力 |

**Non-game inspirations**：
- **金庸小说群体**（《射雕》《神雕》《天龙八部》《笑傲江湖》）—— 人物弧、误会、命运感、各种侠者形态
- **古龙小说**（《楚留香》《陆小凤》《英雄无泪》）—— 一击决胜的高手对决美学、神秘氛围
- **黑泽明电影**（《七武士》《椿三十郎》《用心棒》）—— 孤剑客的镜头语言、寂静中爆发的张力
- **水墨与国画传统** —— 视觉锚点的源头、空山新雨后的留白美学

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 22-40 岁 |
| **Gaming experience** | Mid-core（有 RPG 经验，喜欢深度剧情，能接受多周目） |
| **Time availability** | 工作日晚 1-2 小时为主，周末可 3-5 小时长会话 |
| **Platform preference** | PC（Steam），偏好 16:9 显示器，键盘 + 手柄都可 |
| **Current games they play** | 《逸剑风云决》《极乐迪斯科》《巴尔德之门 3》《Pentiment》《仙剑系列》《古剑系列》《幽魂故事》 |
| **What they're looking for** | 一款能让他们"为角色掉眼泪"的中文武侠 RPG。市场上《逸剑》之后他们渴望下一款类似的、但情感更深、剧情更精的作品 |
| **What would turn them away** | 强动作要求（不是动作玩家）、过度数值化系统、廉价剧情、强制多人 / 在线、单一结局 / 完美主义 frustration |

---

## Visual Identity Anchor

**「冷峻苍凉 + 江南细腻局部融合，视觉本身演绎心境演变」**

### One-Line Visual Rule

> **"白雪压国土，孤剑生血光 —— 但江南有春日，告诉你曾有过的暖。"**

### Three Supporting Visual Principles

1. **基调冷峻**：低饱和度，雪白/墨黑/暗灰为主色，关键时刻（boss 战 / 心境爆发）爆发血红/朱砂红
   - *Design test*：当我们在场景中犹豫"加一抹饱和色"还是"保留低饱和"时，**默认低饱和**。亮色只为关键瞬间留

2. **章节色调演变**：每章节的主色调随玩家在旅程中的心境推进缓慢变化
   - 起点落尘谷：墨黑山影 + 一袭白衣
   - 江南章：方向 B（仙剑式细腻、暖色调）—— 让玩家**记得世界曾经的温度**
   - 北方章：B 转 C 过渡 —— 灰冷渐入
   - 塞外章：方向 C 极致（雪白墨黑朱砂三色）—— 终末的清冷
   - *Design test*：场景过渡的色调变化必须可被玩家察觉，但不能突兀

3. **终幕色调随心境结局变化**：
   - 〈白衣行天下〉= 江南细腻回暖
   - 〈孤剑斩世〉= 朱红压顶
   - 〈大隐于市〉= 烟雨灰蓝
   - 〈风止剑鸣〉= 雪白墨黑
   - 〈剑覆苍生〉= 全黑 + 一点孤血
   - *Design test*：终幕的视觉本身要让玩家"看出"自己走到了哪个结局，无需文字解释

### Color Philosophy Summary

**"色彩是叙事，不是装饰。"**

主色板控制在 8-12 色，每色都有明确的情感意义。武侠的视觉真理是"留白与对比"，不是"信息密度"。每张场景图都应当像一幅可揭下来的国画 —— 但加入冷峻苍凉的现代像素工艺质感。

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | 待 `/setup-engine` 详谈。基于：纯 2D 像素 + PC Steam + 独立 + AI 主力，初步候选倾向 Godot 4 或 Unity。最终决策依据：开发者经验、AI 工具链匹配度、像素工艺友好度 |
| **Key Technical Challenges** | (1) Burst+Read 战斗的回合制 AI + 平衡；(2) 朦胧化 UI 的实现质感；(3) 心境双轴的隐式 UI；(4) 自然日 + 体力 + 日历事件的耦合；(5) "活江湖"层的事件调度系统；(6) 同伴代办支线的逻辑链 |
| **Art Style** | 2D 像素武侠（冷峻苍凉为主 + 江南细腻局部融合） |
| **Art Pipeline Complexity** | Medium-High（自定义像素 + AI 辅助 + 关键 CG 插画手工打磨） |
| **Audio Needs** | Music-heavy（中国乐器：古琴/笛/箫/二胡/琵琶 + 现代电子点缀） + Adaptive（战斗 / 江南 / 北方 / 塞外 / 终幕 各有主题） |
| **Networking** | None |
| **Content Volume** | ~18 个命名地点 / ~20 个命名角色 / 主线 6-8 小时 / 单周目 17-20 小时 / 多周目 40-60 小时 / 总写作量 ~12-15 万字 |
| **Procedural Systems** | 无显式程序生成。但"活江湖"事件调度系统（日历驱动 + 同伴独立线 + 玩家心境响应）会产生类似涌现叙事的效果 |

---

## Risks and Open Questions

### Design Risks

- **D1**：心境双轴的"隐式 UI"如果玩家不能感知，5 结局机制就失去意义 → 缓解：早期通过明确剧情反馈让玩家学会"看心境图位移"
- **D2**：朦胧化 UI 可能让部分玩家感到信息不足、不知所措 → 缓解：MVP 阶段邀请多种玩家盲测，确认"模糊 vs 清晰"的平衡
- **D3**：~~Burst + Read 战斗如果平衡不当，会成为"必胜公式"或"无解之困"~~ → **(2026-06-02 已 paper prototype 验证) 哲学成立无"必胜公式"**，但暴露 6 项 build/balance 缺陷，已捕获于下方 "Combat Prototype Findings" 段，留待 `/design-system 战斗` 解决
- **D4**：误会系统可能让玩家觉得"被剧情坑" → 缓解：每个误会必须有合理性根据，且至少有一次"补救机会"

### Technical Risks

- **T1**：活江湖层（书信 + 暗号 + 传闻 + 同伴代办）的事件调度逻辑链较复杂 → 缓解：用状态机 + 简单条件树，避免过度复杂；先实现 MVP 的子集
- **T2**：心境系统与 NPC 反应 / 武学获取 / 支线分支多重耦合 → 缓解：用数据驱动设计，所有耦合关系写在 JSON 配置而非代码中
- **T3**：Save 系统需要持久化大量叙事状态（心境 + 关系 + 误会 + 暗号发现历史 + 同伴书信内容 + 错过支线） → 缓解：早期建立可扩展的存档 schema

### Market Risks

- **M1**：中文武侠 RPG 市场虽有，但小（核心 5-50 万玩家） → 缓解：差异化点明确（冷峻像素 + 忠贞 + 5 结局 + 心境演变）；海外发行支持多语言（v1.1）
- **M2**：《逸剑风云决》的成功可能带来一批后续作品瓜分市场 → 缓解：聚焦于"剧情为王 + 情感深度"，避免与开放世界类直接竞争

### Scope Risks

- **S1**：12-15 万字写作量对独立开发是巨大挑战 → 缓解：AI 助力 + Tier 分层 + Content Overflow Principle（做不完的支线转交同伴代办）
- **S2**：像素美术资产 100+ 张 → 缓解：AI 美术工具 + 模板复用 + 关键场景手工
- **S3**：8-14 个月时间表对首作开发偏激进 → 缓解：留 20% buffer；v1.0 后规划 v1.1 patch 窗口补完 SHOULD HAVE 内容

### Open Questions

- ~~**Q1**：Burst + Read 战斗的具体节拍如何？~~ → **(2026-06-02 已解决)** 1v1: 5-10 回合 / 1v2: 8-12 / 2v2: 4-10（协同太强会缩短）/ 3v3: 12-15 估计；每回合决策时长 30s-2min（真实策略思考）。详见 prototype REPORT
- **Q2**：心境双轴的"位移可感性"如何？玩家是否能在不显示数字的情况下感知到自己心境在变？→ 由 MVP 玩家测试验证
- **Q3**：误会系统的"难以释怀"感是否会让玩家觉得"游戏惩罚我"？→ 由 MVP 玩家盲测验证
- **Q4**：朦胧化 UI 是否会让部分玩家流失？→ 由 MVP 玩家测试 + 设计师可选的"经典 UI 模式"（v1.1 考虑）
- **Q5**：引擎选择（Godot / Unity） → 由 `/setup-engine` 决策

---

## Combat Prototype Findings (2026-06-02)

> **Status**: Paper prototype 完成 · Verdict = **PROCEED with refinements**
> **完整报告**: [`prototypes/burst-read-combat-concept/REPORT.md`](../../prototypes/burst-read-combat-concept/REPORT.md)

本段封存 paper prototype 学到的 **concept-level 学习**，供 `/design-system 战斗` 编写
GDD 时直接引用。granular 数值（具体伤害值 / 内息成本）不写在此 —— 由战斗 GDD 的
Tuning Knobs 与 Formulas 段承载。

### ✅ Validated Design Decisions（可直接进 GDD）

- **信息不对称模型**：敌方下回合 intent 公开 type（刚/柔/巧）+ target，**不公开具体招式名** —— 玩家知道"会被刚系打"但不知威力。这是"高手过招"机械化身
- **三资源系统**：HP + 内息（招式成本 + 一击决胜 cost）+ 破绽（stagger）—— 三者共同制造"何时全力一击"的紧张
- **一击决胜作为偶发 burst**：每场每角色 1 次，触发条件 = 敌破绽 cap / 敌 HP <30% / 心法满血+反制成功（任一）
- **同时结算（simultaneous resolution）**：没有"谁先动"的争议，反制方向决定 clash 结果
- **协同 burst 涌现**：多角色同回合反制同一敌人 = 一回合击杀 —— 不需要"组合技"机制
- **角色分工自然形成**：剑客（输出）/ 拳师（控场+防御充能）—— 通过资源 cost 差异和招式组合涌现，不需要"职业"标签

### ⚠️ Required Refinements（战斗 GDD 必须解决）

| # | Concept-level 学习 | 战斗 GDD 须明确 |
|---|---|---|
| 1 | **每个 build 必须可应对所有 3 系敌方** | 玩家初始 build 必须包含至少 1 个反巧方案（招式或心法被动）。不允许 ship 只能"反刚"的 build |
| 2 | **反制不应是 dominant strategy** | 反制成本须高于初始 sim（≥ 招式基础成本 +50%）OR 引入"连续反制冷却"OR 让敌方 AI 主动避免连续刚系 attack |
| 3 | **破绽必须有衰减机制** | 每回合 end，如本回合无新增 stagger 则 -1（避免 1v2+ 死亡螺旋）|
| 4 | **一击决胜成本 = 3 内息 total**（含招式 cost，不是 3 + 招式 cost）| 战斗 GDD 明确写在 Formulas 段 |
| 5 | **特殊招式的 status effect 须有"hit success"门槛** | 例如"破绽 +1"应在攻击命中时触发，不是无条件附带 |
| 6 | **boss "优先攻击"机制需明确边界** | 优先攻击招式（如"避影身"trigger 的下回合 first strike）替代 normal action，clash 规则不适用于该次攻击 |

### 💡 Emergent Mechanics Worth Formalizing

- **柔系防御 + 内息回充**：用柔系防御招卸力后获得 +1 内息 —— 这是"老侠剑客的从容"角色感来源，**应保留并形式化**
- **情报招式在被动回合的价值**：当敌方全部防御 phase 时，"听风辨形"类情报招式让玩家仍有有意义决策（不浪费回合）
- **避影身 + 优先攻击 combo**：boss 阶段"timing 装备"的设计语言 —— 让"无脑反制赢"不成立，应作为 **boss 设计模式**沿用
- **心法满血加成**：作为"健康状态奖励"鼓励玩家不被消耗，与一击决胜触发条件耦合

### ~~🔮 Pending Engine-Spike Validations~~ → ✅ Engine Spike 完成 (2026-06-02)

| Feel 问题 | 结果 |
|---|---|
| 一招分胜负的视觉爆发感 | ✅ 窗口时机成立（满血+反制条件可接受） |
| intent tell 的 UI 呈现 | ✅ 系颜色编码（刚红/柔蓝/巧绿）清晰 |
| 回合切换 pacing | ✅ 短而重 |
| simultaneous resolution | ✅ 清晰（双方同时飘字） |

**新增发现**：克制方向 1.5×/0.5× 差距过大（3:1 比率），GDD 调为 **1.3×/0.7×**（~1.9:1）。

详见 [`prototypes/burst-read-combat-concept/REPORT.md`](../../prototypes/burst-read-combat-concept/REPORT.md) Engine Spike Findings 段。

---

## MVP Definition

**核心假设**：「Burst + Read 回合制武学战斗 + 武学残片获取 + 朦胧化 UI + 一位女主彗星模型陪伴」的组合，能在 3-4 小时内让玩家**爱上《风止》的世界与角色**。

**Required for MVP** (3-4 小时可玩):
1. **起点落尘谷章节 + 江南章前半** —— 完整教学 + 第一段冒险 + 第一个 boss 战
2. **2 套武学**（风止剑法起手 + 1 套来自击败第 1 个仇人）+ 2 心法 + 残片机制
3. **1 女主线（白苓）**与 1 男同伴线（谢残翁）的彗星模型 + 暗号 + 书信
4. **5-7 支线**（含 2 个伏笔型阴谋谜案）
5. **完整心境系统底层**（即使 5 结局未全实现，跟踪机制必须跑通）
6. **朦胧化 UI 核心**（战斗外文学化：功力境界、心境图视觉；战斗内数值化：HP / 伤害 / 心法效果）
7. **自然日 + 体力机制**
8. **1 段完整的 boss 战展现 Burst + Read 哲学**

**Explicitly NOT in MVP** (defer):
- 全部 5 结局的终幕内容（只需第一章触达"心境系统在跟踪你"的反馈）
- 沈夜雪 / 柳惊鸿 / 沈从渊 / 韩九江（仅在 MVP 中以"传闻"出现）
- 完整顿悟机制（仅触发功力小突破，不解锁招式 / 融会）
- 江湖百晓生
- 完整暗号系统（仅一两个白苓暗号示例）
- 飞鸽传书定期日历事件

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 起点 + 江南前半（3-4h 可玩） | 核心战斗 + 白苓 + 谢残翁 + 5 支线 + 心境跟踪底层 | 2-3 个月 |
| **Vertical Slice** | 完整江南章节（6-8h 可玩） | + 沈夜雪 + 8-10 支线 + 心境系统玩家可见 + 1 完整结局 | +2 个月（累计 4-5 月） |
| **Alpha** | 全 4 章节骨架（15h 可玩，rough） | + 全 3 女主 + 全 3 同伴 + 全 6 武学 + 全 5 结局骨架 + 全部活江湖层 | +4 个月（累计 8-9 月） |
| **Full Vision (v1.0)** | 完整内容 + 抛光 | + 全部 25-30 支线 + 完整暗号 + 顿悟机制完整 + 5 结局完整 + 误会系统 + 朦胧化 UI 完整 + 中文配音（可选） | +4 个月（累计 12-13 月） |
| **v1.1 Patch** | 后续增量 | + 多周目继承内容 + 隐藏 boss + 季节事件 + 多语言（英 / 日） | +3 个月（累计 15-16 月） |

---

## Next Steps

- [x] Game concept approved（与开发者共同设计完成）
- [x] **Run `/setup-engine`** —— **(2026-06-02 完成)** 选定 Godot 4.6.3 + C# (.NET 8+)
- [x] **Run `/prototype` for the core combat loop** —— **(2026-06-02 完成 Paper path)** Verdict: PROCEED with refinements；6 项 build/balance 调整已捕获于上方 Combat Prototype Findings 段
- [x] (完成) Round 2 Paper prototype —— **(2026-06-02)** 3 项修正方向都奏效
- [x] (完成) Engine spike —— **(2026-06-02)** 4/4 feel 问题通过；新增发现克制倍率需调整
- [ ] **Run `/art-bible`** to formalize visual identity from the Visual Identity Anchor above
- [ ] **Run `/design-review design/gdd/game-concept.md`** to validate concept completeness
- [ ] **Discuss vision with `creative-director`** for pillar refinement (lean mode 默认推迟，需要时再触发)
- [ ] If prototype refinements 落地: Run `/map-systems` to decompose this concept into individual systems
- [ ] Author per-system GDDs with `/design-system` (按依赖顺序：战斗 → 武学 → 心境 → 感情 → 活江湖 → 探索/呼吸期 → 时间/体力)
- [ ] Plan technical architecture with `/create-architecture`
- [ ] Record key architectural decisions with `/architecture-decision (×N)`
- [ ] Run `/architecture-review` to bootstrap TR registry
- [ ] Run `/gate-check` for pre-production validation
- [ ] Build `/vertical-slice` in Pre-Production
- [ ] Validate core loop with `/playtest-report (×1+)`
- [ ] Plan first sprint with `/sprint-plan`

---

*文档版本 v1.0 —— 由 brainstorm 工作流于 2026-06-02 与开发者共同生成。*
