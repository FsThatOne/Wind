# Systems Index: 《孤山遗剑》

> **Status**: Draft
> **Created**: 2026-06-02
> **Last Updated**: 2026-06-02
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

《孤山遗剑》是一款 2D 像素武侠叙事 RPG，以"一读定生死"的 Burst+Read 回合制战斗为核心差异化体验，融合心境双轴道德系统、彗星模型感情系统和活江湖世界层。

项目需要 24 个系统，覆盖：武侠战斗（刚/柔/巧克制 + 一击决胜）、深度叙事分支（对话 + 章节 + 5 结局）、角色关系（同伴独立旅程 + 误会 + 书信）、世界模拟（自然日 + 传闻 + 暗号）、以及"朦胧化"文学 UI 包装。核心循环是 **紧张（战斗 + 抉择）→ 呼吸（探索 + 关系）→ 宏观（心境演变 + 活江湖）**。

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
| 7 | 战斗 UI | UI | MVP | Not Started | — | 回合制战斗, 武学组合 |
| 8 | 存档系统 | Persistence | MVP | Not Started | — | — |
| 9 | 主线叙事 / 章节推进 | Narrative | Vertical Slice | Not Started | — | 对话系统, 地图/场景管理 |
| 10 | NPC 状态管理 (inferred) | Core | Vertical Slice | Not Started | — | — |
| 11 | 自然日 + 体力 | Gameplay | Vertical Slice | Not Started | — | 角色属性, 地图/场景管理 |
| 12 | 地图 / 场景管理 (inferred) | Core | Vertical Slice | Not Started | — | — |
| 13 | 感情系统（彗星模型） | Gameplay | Vertical Slice | Not Started | — | 心境双轴, NPC 状态管理, 对话系统 |
| 14 | 朦胧化 UI | UI | Vertical Slice | Not Started | — | 心境双轴, 角色属性, 感情系统 |
| 15 | 物品 / 道具 (inferred) | Economy | Vertical Slice | Not Started | — | 角色属性, 武学组合 |
| 16 | 活江湖层 | Gameplay | Alpha | Not Started | — | 自然日+体力, NPC 状态管理, 主线叙事 |
| 17 | 顿悟突破 | Gameplay | Alpha | Not Started | — | 回合制战斗, 角色属性, 主线叙事 |
| 18 | 误会系统 | Narrative | Alpha | Not Started | — | 感情系统, NPC 状态管理, 活江湖层 |
| 19 | 探索 / 洞察 (inferred) | Gameplay | Alpha | Not Started | — | 地图/场景管理, 主线叙事 |
| 20 | CG / 演出 (inferred) | UI | Alpha | Not Started | — | 主线叙事, 回合制战斗 |
| 21 | 音乐 / 音效 (inferred) | Audio | Alpha | Not Started | — | 地图/场景管理, 回合制战斗 |
| 22 | 教学 / 引导 (inferred) | Meta | Full Vision | Not Started | — | 几乎全部 Core + Feature |
| 23 | 设置 / 选项 (inferred) | Meta | Full Vision | Not Started | — | — |
| 24 | 成就 / Steam 集成 (inferred) | Meta | Full Vision | Not Started | — | 心境双轴, 感情系统, 主线叙事 |

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
| **Vertical Slice** | "第一章：江南水乡"完整体验 — 战斗+叙事+关系+日历+朦胧化 UI | 7 |
| **Alpha** | 所有 gameplay 系统就位 — 活江湖、顿悟、误会、探索、CG、音乐 | 6 |
| **Full Vision** | 最终润色 — 教学、设置、成就 | 3 |

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
| Total systems identified | 24 |
| Design docs started | 6 |
| Design docs reviewed | 1 |
| Design docs approved | 1 |
| MVP systems designed | 6/8 |
| Vertical Slice systems designed | 0/7 |

---

## Next Steps

- [ ] Design MVP-tier systems first — run `/design-system 角色属性` to start
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/review-all-gdds` after all MVP GDDs are complete
- [ ] Run `/gate-check pre-production` when MVP + VS systems are designed
- [ ] Validate highest-risk systems with `/vertical-slice` before committing to Production
