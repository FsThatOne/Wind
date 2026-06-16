# Story: ns-004 — NPC State Manager

> **Epic**: npc-state
> **Type**: Integration
> **Priority**: P1 — 下游系统统一查询入口
> **Depends On**: ns-002, ns-003
> **ADR Guidance**: ADR-0001 (EventBus)
> **GDD Source**: design/gdd/npc-state.md §Core Rules 2-3, §Interactions
> **Status**: Done

## Goal

实现 `INpcStateManager` 接口，提供 NPC 状态查询/修改的统一入口，通过 EventBus 发布状态变更事件。

## Acceptance Criteria

- [ ] **AC1**: `GetState(npcId)` 返回该 NPC 当前完整状态；不存在时返回 null
- [ ] **AC2**: `UpdateState(npcId, field, value, source)` 成功后发布 `NpcStateChangedEvent`
- [ ] **AC3**: 对话进行中（`IsDialogueLocked=true`）状态变更排队，对话结束后批量生效
- [ ] **AC4**: `GetByPresence(PresenceStatus)` 返回指定在场状态的所有 NPC
- [ ] **AC5**: `GetByAttitude(AttitudeLevel)` 返回指定态度的所有 NPC
- [ ] **AC6**: 死亡 NPC 的状态变更被拒绝（生命状态除外的字段不可修改）

## Test Evidence Path

`tests/Foundation/NpcState/NpcStateManagerTests.cs`
