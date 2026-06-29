# Epic: 行气战棋战斗

> **Layer**: Core
> **GDD**: design/gdd/combat-system.md
> **Architecture Module**: `Core/Combat/`
> **Status**: Complete
> **Stories**: 10/10 Complete

## Overview

行气战棋战斗是《风止》的核心玩法闭环，负责实现战棋式多人战斗、行气推进、行动队列、移动 + 出招、刚柔巧克制、伤害 / 破绽 / 内息结算和战斗结束事件。它按照 `Core/Combat/` 模块边界组织战斗阶段状态机，读取角色属性、武学招式、敌方 AI、物品和顿悟相关输入，并通过 EventBus 向 UI、音频、心境、顿悟和存档等系统广播战斗结果；战斗系统不持有 UI 表现和叙事内容本身。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Event Bus Architecture | 战斗结算后通过 `TurnResolvedEvent`、`BattleEndedEvent` 等事件通知 UI、AI、顿悟和其他系统 | HIGH |
| ADR-0008: Finite State Machine | 战斗阶段管理使用可测试的强类型 FSM | LOW |
| ADR-0020: Pure 2D Wuxia Rendering Direction | 战棋可读性、多人站位、招式范围和意图表达作为战斗视觉最高优先级（纯 2D 路线，Q 版 sprite + 立绘双轨制） | MEDIUM |
| ~~ADR-0019: 2D Wuxia Tactics Rendering Direction~~ | Superseded by ADR-0020（伪 2.5D / 《逸剑风云决》方向已废止，仅保留历史） | — |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 战斗触发、初始化、参战规模、主角必上阵和敌方人数不设 5 人硬上限 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 行气推进、行气满入队、玩家 / AI 行动选择、行动结算、行动后回落的完整流程 | ADR-0008 ✅ / ⚠️ 细节需 stories 验证 |
| 刚 / 柔 / 巧循环克制与破绽累积 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 行动列表：移动、出招、调息、轻功、切换内功、决胜一击、顿悟相关行动 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 敏捷、轻功词条和状态效果按明确规则影响行气增长、行动顺序和行气保留 | ADR-0008 ✅ / ⚠️ 需实现层验证 |
| 战棋移动、行动力、定身、再动和可移动范围 | ADR-0020 ✅ / ⚠️ 需 Presentation 证据验证 |
| 战斗事件发布、战斗结束、心境战斗标记和战后系统联动 | ADR-0001 ✅ |
| 与武学、敌方 AI、物品、顿悟、存档、战斗 UI 的接口边界 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `combat-system.md` 标为 `⚠️ Partial`：当前 ADR 覆盖了事件、状态机和战棋可读性方向，但未完整覆盖全部战斗数值和行动规则。`docs/architecture/tr-registry.yaml` 中战斗 TR 仍是示例注释，创建 stories 时需要登记真实 TR-ID，并把公式、行动、冷却、内功切换、再动和临阵突破刷新 CD 等规则逐条追踪。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/combat-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 行气推进、行动提交、冷却、行动队列排序、克制、破绽、内息、伤害和战斗结束判定均有自动化测试覆盖
- 战棋移动、范围显示、意图展示、再动和多人站位有 QA evidence 或 UI/UX 证据
- EventBus 战斗事件可被 UI、心境、顿悟、存档和音频系统稳定订阅

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| cb-001 | 战斗状态机与回合流程框架 | Foundation | P0 | — | Complete |
| cb-002 | 战斗角色模型与资源系统 | Logic | P0 | cb-001 | Complete |
| cb-003 | 伤害结算管线（F1-F4） | Logic | P0 | cb-002, ma-001, ma-003 | Complete |
| cb-004 | 破绽系统与一击决胜 | Logic | P0 | cb-002, cb-003 | Complete |
| cb-005 | 行动注册表与执行框架 | Logic | P0 | cb-001, cb-002 | Complete |
| cb-006 | 反制机制 | Logic | P0 | cb-003, cb-004, cb-005 | Complete |
| cb-007 | 意图公开与洞察概率系统 | Logic | P1 | cb-002 | Complete |
| cb-008 | 战斗事件总线 | Integration | P0 | cb-001 | Complete |
| cb-009 | 多人战斗与协同破绽 | Integration | P1 | cb-003, cb-004, cb-005, cb-006 | Complete |
| cb-010 | 战斗入口与配置聚合 | Integration | P0 | cb-001, cb-002, cb-005, cb-008 | Complete |

## Next Step

Combat system is complete.
