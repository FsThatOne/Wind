# Epic: 敌方 AI

> **Layer**: Core
> **GDD**: design/gdd/enemy-ai.md
> **Architecture Module**: `Core/EnemyAi/`
> **Status**: Complete
> **Stories**: 8/8 Complete

## Overview

敌方 AI 是非玩家战斗角色的决策系统，负责在 Burst+Read 战斗中生成敌方意图、选择体系、选择目标、处理调息、反读、弱点、蓄力、Boss 阶段和特殊机制。它按照 `Core/EnemyAi/` 模块边界读取战斗上下文、角色属性和武学可用性，输出可公开的体系意图与可执行的行动决策；它不直接结算伤害，也不控制 UI 表现。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Data Configuration Format | AI 性格模板、行为权重、Boss 阶段和特殊机制使用 YAML 配置加载 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 普通敌人使用性格模板和加权随机决策 | ADR-0003 ✅ / ⚠️ 决策算法需 stories 验证 |
| Boss 使用多阶段行为模式、蓄力、体系锁定、破绽重置等机制 | ADR-0003 ✅ / ⚠️ 行为语义需 stories 验证 |
| 两阶段管线：选体系，再做否决与选招 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 调息检查、反读否决、目标选择、体系内选招和回退策略 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 敌方意图在回合开始公开体系，不公开具体招式和数值 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 读取战斗上下文、玩家行动历史、角色属性、可用招式和弱点信息 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 敌方可切换内功，遵守与玩家一致的冷却与行动代价 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 多敌人协同以目标选择自然涌现，不享受玩家方协同破绽加成 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `enemy-ai.md` 标为 `⚠️ Partial`：ADR-0003 覆盖配置表格式，但未覆盖 AI 决策数学、随机可复现、Boss 状态迁移和战斗接口契约。创建 stories 时需要补齐真实 TR-ID，并与 `combat-system` 的回合上下文和行动模型保持一致。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/enemy-ai.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- AI 模板、Boss 配置和行为表可加载、校验，并能稳定复现 seeded 随机决策
- 普通敌人、Boss 阶段、调息、反读、蓄力、弱点和内功切换均有自动化测试覆盖
- 战斗系统可在回合开始获取敌方公开意图，并在结算时获取完整可执行行动

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| ai-001 | 性格模板与体系选择 (Phase A) | Logic | P0 | — | Complete |
| ai-002 | 状态机与综合修正系统 | Logic | P0 | ai-001 | Complete |
| ai-003 | 招式预兆序列 | Logic | P1 | ai-001 | Complete |
| ai-004 | 调息决策与选招逻辑 | Logic | P0 | ai-001 | Complete |
| ai-005 | 反读系统 (Phase B) | Logic | P0 | ai-001 | Complete |
| ai-006 | 目标选择 (F3) | Logic | P0 | — | Complete |
| ai-007 | Boss 阶段与特殊机制 | Logic | P1 | ai-001, ai-005 | Complete |
| ai-008 | AI 管线集成与验证 | Integration | P0 | ai-001~ai-007 | Complete |

## Next Step

Enemy AI is complete.
