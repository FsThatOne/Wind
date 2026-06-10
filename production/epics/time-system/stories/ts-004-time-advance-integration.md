# Story: ts-004 — Time Advance Integration

> **Epic**: time-system
> **Type**: Integration
> **Priority**: P1
> **Depends On**: ts-001, ts-002, ts-003
> **ADR Guidance**: ADR-0001 (EventBus)
> **GDD Source**: design/gdd/natural-day-stamina.md §States and Transitions, §Interactions
> **Status**: Ready

## Goal

实现 `TimeSystem` 统一入口类，组装日历+体力+调度器，通过 EventBus 发布时间事件。

## Acceptance Criteria

- [ ] **AC1**: `AdvanceTime(delta)` 推进日历并触发事件链
- [ ] **AC2**: 时辰切换时发布 `ShichenChangedEvent`
- [ ] **AC3**: 日切换时发布 `DayAdvancedEvent` + 触发延迟事件
- [ ] **AC4**: 季节切换时发布 `SeasonChangedEvent`
- [ ] **AC5**: `ConsumeStamina(amount)` 委托体力系统并发布状态变更事件
- [ ] **AC6**: 防递归标志：推进中再调用 AdvanceTime 返回 false

## Test Evidence Path

`tests/Foundation/TimeSystem/TimeAdvanceIntegrationTests.cs`
