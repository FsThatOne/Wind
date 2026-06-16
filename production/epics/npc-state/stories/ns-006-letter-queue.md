# Story: ns-006 — Letter Queue

> **Epic**: npc-state
> **Type**: Logic
> **Priority**: P2 — 飞书排队是 Presentation 的前置逻辑
> **Depends On**: ns-002
> **GDD Source**: design/gdd/npc-state.md §Formulas F6, §Edge Cases (飞书相关)
> **Status**: Done

## Goal

实现飞书排队送达判定(F6) + 优先级排序 + 队列管理（含死亡取消和排队上限）。

## Acceptance Criteria

- [ ] **AC1**: `LetterQueue.Enqueue(letter)` 正确入队，按优先级排序
- [ ] **AC2**: `IsDeliveryReady(freeRoam, stepsAfterUnblock, stepDelay)` 正确实现 F6
- [ ] **AC3**: 战斗中入队的飞书不触发送达，等待自由活动
- [ ] **AC4**: 送达时取出最高优先级的一封，其余保留
- [ ] **AC5**: NPC 死亡时取消该 NPC 所有非 `posthumous_allowed` 的飞书
- [ ] **AC6**: 队列上限 5 封（multi_letter_queue_cap），超出时最低优先级被挤出

## Test Evidence Path

`tests/Foundation/NpcState/LetterQueueTests.cs`
