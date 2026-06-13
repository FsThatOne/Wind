# Story: ts-005 — Inn Rest Logic

> **Epic**: time-system
> **Type**: Logic
> **Priority**: P2
> **Depends On**: ts-001, ts-002
> **GDD Source**: design/gdd/natural-day-stamina.md §Core Rules 6, §Edge Cases (客栈)
> **Status**: Done

## Goal

实现三种客栈休息方式的时间推进计算和恢复逻辑。

## Acceptance Criteria

- [ ] **AC1**: 小憩：推进 1 时辰，恢复 30% max
- [ ] **AC2**: 过夜：推进至次日卯时，完全恢复
- [ ] **AC3**: 自选：推进至目标时辰，按比例恢复
- [ ] **AC4**: 休息期间延迟事件批量触发
- [ ] **AC5**: 需要交互的事件标记为"休息后待送达"
- [ ] **AC6**: 气血/内息在过夜时完全恢复

## Test Evidence Path

`tests/Foundation/TimeSystem/InnRestTests.cs`
