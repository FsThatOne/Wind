# Story: ns-002 — NPC State Model

> **Epic**: npc-state
> **Type**: Logic
> **Priority**: P0 — 多轴状态是全系统基础
> **Depends On**: ns-001
> **ADR Guidance**: ADR-0008 (FSM)
> **GDD Source**: design/gdd/npc-state.md §Core Rules 1-3
> **Status**: Done

## Goal

定义 NPC 多轴状态数据结构（8 维度枚举 + NpcState 类），支持状态标记和安全默认值恢复。

## Acceptance Criteria

- [ ] **AC1**: 定义 8 个状态维度枚举: LifeStatus, PresenceStatus, LocationStatus, InteractionStatus, JourneyStage, RelationshipStage, AttitudeLevel, 以及 StateFlags 字典
- [ ] **AC2**: `NpcState` 类持有所有维度 + TemplateId + LastChangeSource
- [ ] **AC3**: `NpcState.CreateDefault()` 返回安全默认值（生命=未知, 在场=不可达, 可交互=不可交互, 态度=萍水相逢）
- [ ] **AC4**: 每次状态变更必须附带 `source` 字符串（来源追踪）
- [ ] **AC5**: `StateChangeRecord` 记录变更历史（字段、旧值、新值、来源、时间戳），最多保留 20 条/NPC

## Test Evidence Path

`tests/Foundation/NpcState/NpcStateModelTests.cs`
