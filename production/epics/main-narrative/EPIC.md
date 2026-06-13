# Epic: 主线叙事 / 章节推进

> **Layer**: Core
> **GDD**: design/gdd/main-narrative.md
> **Architecture Module**: `Core/Narrative/`
> **Status**: Complete
> **Stories**: 8/8 Complete

## Overview

主线叙事 / 章节推进系统是《风止》的剧情骨架，负责章节、主线节点图、前置条件、呼吸期、限时推进、分支记录、章节切换和终幕结局收敛。它按照 `Core/Narrative/` 模块边界读取对话结果、场景状态、时间、心境、NPC 状态和物品条件，推进主线节点并通过 EventBus 触发下游系统；它不承载具体对白文本和演出表现。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: Dialogue Data Format | 主线节点可复用对话节点图、条件三元组和事件附加机制表达叙事推进的一部分 | LOW |
| ADR-0001: Event Bus Architecture | 主线节点完成、章节切换和叙事事件通过类型安全事件通知下游系统 | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 线性章节骨架与章内有向节点图 | ADR-0005 ✅ / ⚠️ 章节语义需 stories 验证 |
| 主线节点前置条件、活跃节点、完成节点和分支记录 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 呼吸期打开/关闭与自由探索窗口 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 限时主线、超时强制推进和节点截止检查 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 章节切换、区域解锁、色调变化和场景系统联动 | ADR-0001 ✅ / ⚠️ 需与 scene-management stories 对齐 |
| 终幕结局由心境 6 分支、伴侣状态和善恶旁白色调收敛 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 主线与支线交叉：支线完成影响主线对话、揭示和条件 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `main-narrative.md` 标为 `⚠️ Partial`：ADR-0005 覆盖对话节点图的一部分表达能力，但主线章节图、时间压力、呼吸期和结局收敛仍需 story 层补齐 TR 与验收。创建 stories 时要避免过早写完整剧情内容，优先实现节点推进骨架和可测试的状态机/条件查询。

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [主线节点图模型与加载校验](stories/mn-001-node-graph-schema-and-loader.md) | Logic | Complete | ADR-0005 / ADR-0001 |
| 002 | [节点激活与完成推进](stories/mn-002-node-activation-and-completion.md) | Logic | Complete | ADR-0001 |
| 003 | [分支记录与章节门合流](stories/mn-003-branches-choice-log-and-gates.md) | Logic | Complete | ADR-0005 |
| 004 | [章节切换与线性骨架](stories/mn-004-chapter-management.md) | Integration | Complete | ADR-0001 |
| 005 | [呼吸期控制](stories/mn-005-breathing-period.md) | Logic | Complete | ADR-0001 |
| 006 | [限时主线与强制推进](stories/mn-006-time-limited-nodes.md) | Integration | Complete | ADR-0001 |
| 007 | [战斗结果路由](stories/mn-007-combat-outcome-routing.md) | Integration | Complete | ADR-0001 |
| 008 | [终幕结局收敛与存档恢复](stories/mn-008-ending-and-save-contracts.md) | Integration | Complete | ADR-0001 |

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/main-narrative.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 节点图加载、前置条件、节点推进、呼吸期、限时推进、分支记录和章节切换有自动化测试覆盖
- 结局收敛可读取心境、伴侣状态和关键分支记录，并稳定输出终幕脚本 key
- 主线节点事件可被对话、场景、时间、心境、NPC 状态、物品和存档系统集成

## Next Step

Main narrative is complete. Next dependency candidate: connect data-authored mainline YAML content or start the next Core epic.
