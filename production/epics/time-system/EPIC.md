# Epic: 自然日+体力

> **Layer**: Foundation
> **GDD**: design/gdd/natural-day-stamina.md
> **Architecture Module**: `Foundation/TimeSystem/`
> **Status**: Done
> **Stories**: 6/6 Done (ts-001 ~ ts-006)

## Overview

自然日+体力是《风止》的时间节奏引擎和行动资源管理器。它驱动整个游戏世界的时间推进——12 时辰制日历、季节流转、延迟事件触发——是 NPC 独立旅程、飞书到达、活江湖事件和场景环境变化的"心跳"来源。同时管理玩家体力池的消耗、恢复和状态效果。通过 ADR-0001 EventBus 发布时间推进事件，使用 ADR-0003 YAML 配置时间规则和旅行耗时。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Event Bus Architecture | C# EventBus Autoload 用于跨层通信 | HIGH |
| ADR-0003: Data Configuration Format | YAML 1.2 + YamlDotNet 配置时间/季节/旅行数据 | LOW |

## GDD Requirements (from Acceptance Criteria)

| Requirement | ADR Coverage |
|-------------|--------------|
| 12 时辰制日历推进 | ADR-0001 ✅ (时间事件) |
| 四季循环管理 | ADR-0003 ✅ (配置) + ADR-0001 ✅ (事件通知) |
| 体力池消耗/恢复/状态效果 | ⚠️ 基础设施覆盖 |
| 延迟事件调度（第 N 日触发） | ADR-0001 ✅ |
| 大地图移动时间推进 | ADR-0003 ✅ (旅行耗时配置) |
| 客栈休息规则和效果 | ⚠️ 基础设施覆盖 |
| 时段查询接口 (dawn/day/dusk/night) | ADR-0001 ✅ |

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/natural-day-stamina.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 12 时辰日历正确推进并发布时间事件
- 季节循环正确触发场景/事件变化
- 体力系统正确管理消耗和恢复
- 延迟事件在正确游戏日触发

## Stories

| Story | Name | Type | Status |
|-------|------|------|--------|
| ts-001 | Calendar Core | Logic | Done |
| ts-002 | Stamina System | Logic | Done |
| ts-003 | Delayed Event Scheduler | Logic | Done |
| ts-004 | Time Advance Integration | Integration | Done |
| ts-005 | Inn Rest Logic | Logic | Done |
| ts-006 | Time Config YAML | Integration | Done |

## 下一步

Foundation 自然日+体力 epic 已完成。下一步生产任务：使用 `/create-epics layer: core` 创建 Core 层 epics。
