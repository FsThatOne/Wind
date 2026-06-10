# Story: ts-003 — Delayed Event Scheduler

> **Epic**: time-system
> **Type**: Logic
> **Priority**: P1
> **Depends On**: ts-001
> **GDD Source**: design/gdd/natural-day-stamina.md §Core Rules 5, §Edge Cases (延迟事件)
> **Status**: Ready

## Goal

实现延迟事件调度器：注册/触发/优先级排序/防递归。

## Acceptance Criteria

- [ ] **AC1**: `RegisterDelayedEvent(id, triggerDay, callback, priority)` 正确注册
- [ ] **AC2**: 日推进时按优先级升序执行到期事件
- [ ] **AC3**: 同优先级按注册顺序(FIFO)执行
- [ ] **AC4**: 回调内调用 `advance_time()` 被拒绝（防递归）
- [ ] **AC5**: 取消事件 `CancelEvent(id)` 正确移除
- [ ] **AC6**: 驿站传送跨多日时逐日触发延迟事件

## Test Evidence Path

`tests/Foundation/TimeSystem/DelayedEventSchedulerTests.cs`
