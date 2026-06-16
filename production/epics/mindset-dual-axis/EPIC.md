# Epic: 心境双轴

> **Layer**: Core
> **GDD**: design/gdd/mindset-dual-axis.md
> **Architecture Module**: `Core/Mindset/`
> **Status**: Complete
> **Stories**: 7/7 Complete

## Overview

心境双轴是《风止》的隐式人格与结局判定系统，负责维护执念↔释怀、入世↔出世和隐藏善恶轴，处理对话、叙事和心境战斗带来的位移，并向主线叙事、感情系统、NPC 反应、奇遇和朦胧化 UI 提供心境查询与事件通知。它按照 `Core/Mindset/` 模块边界实现坐标状态、区域判定、善恶档位、结局路径输入和 EventBus 位移广播；它不提供战斗属性加成。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Event Bus Architecture | 心境位移通过 `MindsetShiftedEvent` 通知 UI、叙事和其他下游系统 | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 三轴坐标：resolve、worldly、morality，范围 -50 至 +50 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 初始位置、位移夹取、日常位移、关键位移和战斗结果位移 | ADR-0001 ✅ / ⚠️ 数值规则需 stories 验证 |
| 双轴区域、未定之人、魔道 override、善恶档位和终幕旁白色调 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 对话系统通过 `mindset_shift` 事件驱动心境变化 | ADR-0001 ✅ |
| 心境战斗胜/败/惜败由战后叙事序列触发心境位移 | ADR-0001 ✅ / ⚠️ 需与 combat-system story 对齐 |
| 心境不影响战斗属性或加成 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 查询接口供主线、感情、NPC 反应、奇遇和 UI 使用 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `mindset-dual-axis.md` 标为 `⚠️ Partial`：ADR-0001 覆盖跨系统通知，但未覆盖坐标数学、区域矩阵、结局归类和隐藏信息展示策略。创建 stories 时需要补齐真实 TR-ID，并与 `dialogue-system` 已完成的心境事件、`main-narrative` 结局收敛保持一致。

## Stories

| # | Story | Type | Status | ADR | Evidence |
|---|-------|------|--------|-----|----------|
| 001 | [三轴坐标与位移](stories/ms-001-coordinate-shifts.md) | Logic | Complete | ADR-0001 | `tests/unit/mindset/mindset_service_test.cs` |
| 002 | [区域判定与滞后带](stories/ms-002-zone-hysteresis.md) | Logic | Complete | ADR-0001 | `tests/unit/mindset/mindset_service_test.cs` |
| 003 | [善恶档位与档位事件](stories/ms-003-morality-tier.md) | Logic | Complete | ADR-0001 | `tests/unit/mindset/mindset_service_test.cs` |
| 004 | [结局判定算法与魔道 override](stories/ms-004-ending-determination.md) | Logic | Complete | ADR-0001 | `tests/unit/mindset/mindset_service_test.cs` |
| 005 | [声望追赶系统](stories/ms-005-reputation-system.md) | Logic | Complete | ADR-0001 | `tests/unit/mindset/mindset_service_test.cs` |
| 006 | [查询接口与朦胧化展示契约](stories/ms-006-query-and-presentation-contracts.md) | Integration | Complete | ADR-0001 | `tests/unit/mindset/mindset_presentation_service_test.cs` |
| 007 | [对话、心境战斗与存档集成](stories/ms-007-dialogue-battle-save-integration.md) | Integration | Complete | ADR-0001 | `tests/unit/mindset/mindset_dialogue_bridge_test.cs` |

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/mindset-dual-axis.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 三轴位移、夹取、区域判定、善恶档位、魔道 override 和结局输入有自动化测试覆盖
- `MindsetShiftedEvent`、查询接口和对话/叙事触发路径可被集成测试验证
- 朦胧化 UI 所需的非数值化描述和隐藏善恶策略有 UX spec 或 QA evidence 签收

## Next Step

Epic 已实现并通过测试。后续在主线叙事、感情系统、NPC 反应和朦胧化 UI 接入时复用当前查询与事件契约。
