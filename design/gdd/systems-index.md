# Systems Index: 《风止》

> **Status**: Draft
> **Created**: 2026-06-02
> **Last Updated**: 2026-06-08
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

《风止》是一款 2D 像素武侠叙事 RPG，以"一读定生死"的 Burst+Read 回合制战斗为核心差异化体验，融合心境双轴道德系统、彗星模型感情系统和活江湖世界层。

项目需要 25 个系统，覆盖：武侠战斗（刚/柔/巧克制 + 一击决胜）、队伍管理（5 人上阵 + 同伴成长 + 板凳追赶）、深度叙事分支（对话 + 章节 + 5 结局）、角色关系（同伴独立旅程 + 误会 + 书信）、世界模拟（自然日 + 传闻 + 暗号）、以及"朦胧化"文学 UI 包装。核心循环是 **紧张（战斗 + 抉择）→ 呼吸（探索 + 关系）→ 宏观（心境演变 + 活江湖）**。

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | 角色属性 / 功力 | Core | MVP | Designed | [character-attributes.md](character-attributes.md) | — |
| 2 | 回合制战斗（Burst+Read） | Gameplay | MVP | Designed | [combat-system.md](combat-system.md) | 角色属性 |
| 3 | 武学组合 | Gameplay | MVP | Designed | [martial-arts-system.md](martial-arts-system.md) | 角色属性 |
| 4 | 敌方 AI | Gameplay | MVP | Approved | [enemy-ai.md](enemy-ai.md) | 回合制战斗, 角色属性, 武学组合(软) |
| 5 | 对话系统 | Narrative | MVP | Designed | [dialogue-system.md](dialogue-system.md) | — |
| 6 | 心境双轴 | Gameplay | MVP | Designed | [mindset-dual-axis.md](mindset-dual-axis.md) | NPC 状态管理, 对话系统 |
| 7 | 战斗 UI | UI | MVP | Designed | [combat-ui.md](combat-ui.md) | 回合制战斗, 武学组合 |
| 8 | 存档系统 | Persistence | MVP | Designed | [save-system.md](save-system.md) | — |
| 9 | 主线叙事 / 章节推进 | Narrative | Vertical Slice | Designed | [main-narrative.md](main-narrative.md) | 对话系统, 地图/场景管理 |
| 10 | NPC 状态管理 (inferred) | Core | Vertical Slice | Designed | [npc-state.md](npc-state.md) | — |
| 11 | 自然日 + 体力 | Gameplay | Vertical Slice | Designed | [natural-day-stamina.md](natural-day-stamina.md) | 角色属性, 地图/场景管理 |
| 12 | 地图 / 场景管理 | Core | Vertical Slice | Designed | [map-scene-management.md](map-scene-management.md) | — |
| 13 | 感情系统（彗星模型） | Gameplay | Vertical Slice | Designed | [romance-system.md](romance-system.md) | 心境双轴, NPC 状态管理, 对话系统 |
| 14 | 朦胧化 UI | UI | Vertical Slice | Designed | [blurred-ui.md](blurred-ui.md) | 心境双轴, 角色属性, 感情系统 |
| 15 | 物品 / 道具 (inferred) | Economy | Vertical Slice | Designed | [item-system.md](item-system.md) | 角色属性, 武学组合 |
| 16 | 活江湖层 | Gameplay | Alpha | Designed | [living-jianghu-layer.md](living-jianghu-layer.md) | 自然日+体力, NPC 状态管理, 主线叙事 |
| 17 | 顿悟突破 | Gameplay | Alpha | Designed | [epiphany-breakthrough.md](epiphany-breakthrough.md) | 回合制战斗, 角色属性, 主线叙事 |
| 18 | 误会系统 | Narrative | Alpha | Designed | [misunderstanding-system.md](misunderstanding-system.md) | 感情系统, NPC 状态管理, 活江湖层 |
| 19 | 探索 / 洞察 (inferred) | Gameplay | Alpha | Designed | [exploration-insight.md](exploration-insight.md) | 地图/场景管理, 主线叙事 |
| 20 | CG / 演出 (inferred) | UI | Alpha | Designed | [cutscene-system.md](cutscene-system.md) | 主线叙事, 回合制战斗 |
| 21 | 音乐 / 音效 (inferred) | Audio | Alpha | Designed | [audio-system.md](audio-system.md) | 地图/场景管理, 回合制战斗 |
| 22 | 教学 / 引导 (inferred) | Meta | Full Vision | Designed | [tutorial-onboarding.md](tutorial-onboarding.md) | 几乎全部 Core + Feature |
| 23 | 设置 / 选项 (inferred) | Meta | Full Vision | Designed | [settings-options.md](settings-options.md) | — |
| 24 | 成就 / Steam 集成 (inferred) | Meta | Full Vision | Designed | [achievement-steam.md](achievement-steam.md) | 心境双轴, 感情系统, 主线叙事 |
| 25 | 队伍管理 / 同伴成长 | Gameplay | Vertical Slice | Designed | [party-management.md](party-management.md) | 角色属性, 回合制战斗, 武学组合, 物品/道具, NPC 状态管理, 顿悟突破, 活江湖层 |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | 所有系统依赖的地基 — 属性、NPC 状态、场景管理 |
| **Gameplay** | 让游戏好玩的系统 — 战斗、武学、心境、日历、活江湖 |
| **Narrative** | 故事和对话 — 对话系统、主线叙事、误会系统 |
| **Economy** | 资源产消 — 物品/道具 |
| **Persistence** | 存档和状态持久化 |
| **UI** | 玩家信息展示 — 战斗 UI、朦胧化 UI、CG/演出 |
| **Audio** | 音乐和音效 |
| **Meta** | 游戏外围 — 教学、设置、成就 |

---

## Priority Tiers

| Tier | Definition | System Count |
|------|------------|--------------|
| **MVP** | 核心循环可运转 — 能打一场 Burst+Read 战斗 + 做一次心境选择 + 存档读档 | 8 |
| **Vertical Slice** | "第一章：江南水乡"完整体验 — 战斗+叙事+关系+日历+朦胧化 UI+队伍管理 | 8 |
| **Alpha** | 所有 gameplay 系统就位 — 活江湖、顿悟、误会、探索、CG、音乐 | 6 |
| **Full Vision** | 最终润色 — 教学、设置、成就 | 3 |

---

## System Unlock Timeline（玩家视角解锁顺序）

> **原则**：不超过第二章全部解锁。通过叙事锚点自然引入，避免一次性信息轰炸。

| 阶段 | 叙事场景 | 新增系统 | 叙事锚点 |
|------|---------|---------|---------|
| **序章前半** | 师门生活 → 灭门事件 | #12 地图/场景、#5 对话、#2 战斗（基础）、#13 感情系统、#10 NPC 态度 | 与师兄弟/师傅/师姐互动教学；将死之人态度死后固定 |
| **序章尾段** | 师兄归来 → 误会 → 分别 | #18 误会系统、#10 飞书/书信 | 师兄误会主角独活=内奸（对话解除）；分别时约定通信 |
| **章外章** | 初入世间（镖局岁月） | #6 心境双轴、#11 自然日/体力、#15 物品/装备、#15 锻造/炼丹、#16 活江湖层·传闻/暗号、#25 队伍管理（基础） | 老镖师教授江湖规矩；镖局采药采矿锻造兵器/炼药；首次出现可同行角色后开放队伍配置 |
| **第一章** | 江南 | （本阶段无新增系统首次引入——感情系统和 NPC 态度已在序章激活，彗星模型自然体验于此阶段，教学在此触发） | 首次遇见女主后感情系统教学自然触发；NPC 态度变化自然引起关注 |
| **第二章** | 独行江湖 | #17 顿悟突破、#19 探索/洞察、#25 同伴成长（完整） | 生死危局触发突破并顺势教学；同伴观战、追赶、离队历练和个人旅程成长开始完整运作 |

> **注**：教学/引导(#22)、设置/选项(#23)、成就(#24) 为 Full Vision 层，不在游戏叙事中解锁。CG/演出(#20)、音乐/音效(#21) 随叙事自然出现，无需显式教学。

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **角色属性 / 功力** — 所有战斗公式和成长数值的数据根基
2. **对话系统** — 叙事 RPG 的骨架，所有剧情/感情/心境交互的管道
3. **NPC 状态管理** — 同伴独立旅程 + NPC 反应 + 活江湖事件的状态机
4. **存档系统** — 心境/关系/暗号/误会/书信 = 大量叙事状态需要持久化
5. **地图 / 场景管理** — 18 个场景的加载/跳转/解锁/色调控制

### Core Layer (depends on Foundation)

1. **回合制战斗** — depends on: 角色属性
2. **武学组合** — depends on: 角色属性
3. **敌方 AI** — depends on: 回合制战斗, 角色属性
4. **心境双轴** — depends on: NPC 状态管理, 对话系统
5. **主线叙事 / 章节推进** — depends on: 对话系统, 地图/场景管理
6. **自然日 + 体力** — depends on: 角色属性, 地图/场景管理

### Feature Layer (depends on Core)

1. **感情系统（彗星模型）** — depends on: 心境双轴, NPC 状态管理, 对话系统
2. **活江湖层** — depends on: 自然日+体力, NPC 状态管理, 主线叙事
3. **顿悟突破** — depends on: 回合制战斗, 角色属性, 主线叙事
4. **误会系统** — depends on: 感情系统, NPC 状态管理, 活江湖层
5. **物品 / 道具** — depends on: 角色属性, 武学组合
6. **探索 / 洞察** — depends on: 地图/场景管理, 主线叙事
7. **队伍管理 / 同伴成长** — depends on: 角色属性, 回合制战斗, 武学组合, 物品/道具, NPC 状态管理, 顿悟突破, 活江湖层

### Presentation Layer (depends on Features)

1. **战斗 UI** — depends on: 回合制战斗, 武学组合
2. **朦胧化 UI** — depends on: 心境双轴, 角色属性, 感情系统
3. **CG / 演出** — depends on: 主线叙事, 回合制战斗
4. **音乐 / 音效** — depends on: 地图/场景管理, 回合制战斗

### Polish Layer (depends on everything)

1. **教学 / 引导** — depends on: 几乎全部 Core + Feature
2. **设置 / 选项** — depends on: —
3. **成就 / Steam 集成** — depends on: 心境双轴, 感情系统, 主线叙事

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | 角色属性 / 功力 | MVP | Foundation | game-designer, systems-designer | M |
| 2 | 回合制战斗（Burst+Read） | MVP | Core | game-designer, systems-designer | L |
| 3 | 武学组合 | MVP | Core | game-designer, systems-designer | M |
| 4 | 敌方 AI | MVP | Core | game-designer, ai-programmer | M |
| 5 | 对话系统 | MVP | Foundation | game-designer, narrative-director | M |
| 6 | 心境双轴 | MVP | Core | game-designer, narrative-director | M |
| 7 | 战斗 UI | MVP | Presentation | game-designer, ux-designer | M |
| 8 | 存档系统 | MVP | Foundation | game-designer, lead-programmer | S |
| 9 | 主线叙事 / 章节推进 | VS | Core | narrative-director, game-designer | L |
| 10 | NPC 状态管理 | VS | Foundation | game-designer, ai-programmer | M |
| 11 | 自然日 + 体力 | VS | Core | game-designer, systems-designer | S |
| 12 | 地图 / 场景管理 | VS | Foundation | game-designer, level-designer | M |
| 13 | 感情系统（彗星模型） | VS | Feature | game-designer, narrative-director | L |
| 14 | 朦胧化 UI | VS | Presentation | ux-designer, game-designer | M |
| 15 | 物品 / 道具 | VS | Feature | game-designer, economy-designer | S |
| 16 | 活江湖层 | Alpha | Feature | game-designer, systems-designer | L |
| 17 | 顿悟突破 | Alpha | Feature | game-designer, systems-designer | M |
| 18 | 误会系统 | Alpha | Feature | game-designer, narrative-director | M |
| 19 | 探索 / 洞察 | Alpha | Feature | game-designer, level-designer | S |
| 20 | CG / 演出 | Alpha | Presentation | art-director, narrative-director | M |
| 21 | 音乐 / 音效 | Alpha | Presentation | audio-director, sound-designer | M |
| 22 | 教学 / 引导 | Full | Polish | game-designer, ux-designer | M |
| 23 | 设置 / 选项 | Full | Polish | ui-programmer | S |
| 24 | 成就 / Steam 集成 | Full | Polish | game-designer, devops-engineer | S |
| 25 | 队伍管理 / 同伴成长 | VS | Feature | game-designer, systems-designer | M |

> **Effort**: S = 1 session, M = 2-3 sessions, L = 4+ sessions

---

## Circular Dependencies

None found.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| 回合制战斗 | Design | Burst+Read 核心在 prototype 验证通过，但多人协同 burst + 连战体力管理仍需 GDD 阶段细化 | Paper prototype Round 2 已验证；GDD 阶段用 systems-designer 细化公式 |
| 心境双轴 | Scope | 双轴 × 5 结局 × N 个分支 = 组合爆炸风险 | 用心境区域（而非连续值）控制分支数；每章只允许 2-3 个心境关键选择 |
| 误会系统 | Design | "玩家不知道的误解"易造成挫败感而非叙事深度 | Concept doc 已设定"误会透明度"原则——玩家能察觉误会存在，只是解法不明 |
| 感情系统 | Scope | 彗星模型（渐远→渐近循环）+ 5 NPC 关系线 = 内容量巨大 | MVP 只做 1 条核心关系线；Vertical Slice 做 2 条 |
| 活江湖层 | Technical | 书信/传闻/暗号/代办 = NPC 状态 × 时间 × 玩家行为的组合模拟 | 用事件表 + 规则引擎而非穷举；Alpha 阶段做，不进 MVP |
| 敌方 AI | Design | Intent tell（亮出意图）要求 AI 行为既可读又不可预测，平衡点难找 | Prototype 验证了基本意图显示；GDD 阶段用 ai-programmer 设计多阶段 boss 行为 |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 25 |
| Design docs written | 25 |
| Cross-GDD reviews completed | 5 |
| Design docs approved | 1 |
| MVP systems designed | 8/8 |
| Vertical Slice systems designed | 8/8 |
| Alpha systems designed | 6/6 |
| Full Vision systems designed | 3/3 |

---

## Next Steps

- [x] ~~Design MVP-tier systems (8/8)~~
- [x] ~~Design Vertical Slice systems (8/8)~~
- [x] ~~Resolve cross-review CRITICAL issues (G-3 物品武侠化已执行；C-1~C-4 已验证修复)~~
- [x] ~~Update GDD Status headers (#1~#6 → Designed)~~
- [x] ~~Design #19 探索/洞察 GDD~~
- [x] ~~Run `/design-review` on #19 探索/洞察 — 2 blocking fixed, verdict: Designed~~
- [x] ~~Design #20 CG/演出 GDD~~
- [x] ~~Design remaining Alpha systems — next: 音乐/音效 (#21)~~
- [x] ~~Sync 风止尺法招式体系到 martial-arts-system GDD~~
- [x] ~~Run `/design-review` on remaining GDDs (#15 物品, #18 误会)~~
- [x] ~~Fix combat-system.md F6 编号重复~~
- [x] ~~Run `/review-all-gdds` after all Alpha GDDs are complete~~ → [gdd-cross-review-2026-06-07.md](gdd-cross-review-2026-06-07.md) — Verdict: CONCERNS (7 BLOCKING)
- [x] ~~Fix 7 BLOCKING issues identified in cross-review~~ ✅ All 7 BLOCKING + 15 WARNING fixed 2026-06-07
- [x] ~~Design #24 成就/Steam 集成 GDD (last undesigned system)~~ ✅ achievement-steam.md written 2026-06-08
- [x] ~~Run `/design-review` on #24 成就/Steam 集成 — 5 blocking + 12 warning fixed, verdict: Designed~~
- [ ] Run `/gate-check pre-production` when all 25 GDDs reviewed
