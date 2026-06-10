# Epic: NPC 状态管理

> **Layer**: Foundation
> **GDD**: design/gdd/npc-state.md
> **Architecture Module**: `Foundation/NpcState/`
> **Status**: Done
> **Stories**: 7 stories (ns-001 through ns-007)

## Overview

NPC 状态管理是《风止》的角色世界状态层，负责记录每个重要 NPC 当前"是谁、在哪里、处于什么剧情阶段、是否可交互、与主角的关系处于何种叙事状态"。它为对话、心境反应、感情线、活江湖事件和误会系统提供 NPC 当前事实的统一查询来源。实现 `INpcStateManager`、基于 ADR-0008 泛型 FSM 的 `NpcFsm`（6 状态态度 FSM），通过 ADR-0001 EventBus 发布状态变更事件。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Event Bus Architecture | C# EventBus Autoload 用于跨层通信 | HIGH |
| ADR-0008: Generic Finite State Machine | 纯 C# 泛型 FSM 基类，不依赖 Godot 节点 | LOW |

## GDD Requirements (from Acceptance Criteria)

| Requirement | ADR Coverage |
|-------------|--------------|
| 6 状态 NPC 态度 FSM | ADR-0008 ✅ |
| 态度值数值计算和衰减 | ADR-0008 ✅ |
| NPC 独立旅程状态追踪 | ADR-0001 ✅ |
| NPC 位置/可用性查询接口 | ADR-0001 ✅ |
| 状态变更事件发布到 EventBus | ADR-0001 ✅ |
| 飞书/书信状态管理 | ADR-0001 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/npc-state.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- `NpcFsm` 正确实现 6 状态机转换和守卫条件
- 状态变更通过 EventBus 正确发布
- NPC 位置和态度可被下游系统正确查询

## Stories

| Story | Name | Type | Status |
|-------|------|------|--------|
| ns-001 | Generic FSM | Logic | Done |
| ns-002 | NPC State Model | Logic | Done |
| ns-003 | Attitude Formula | Logic | Done |
| ns-004 | NPC State Manager | Integration | Done |
| ns-005 | Delegate & Journey | Logic | Done |
| ns-006 | Letter Queue | Logic | Done |
| ns-007 | NPC YAML Config | Integration | Done |

## Next Step

Run `/dev-story ns-001` to begin implementing the Generic FSM.
