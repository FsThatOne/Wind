# Story: sm-002 — Location Access Control

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P0
> **Depends On**: sm-001, EventBus (cd-006)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 4 (区域解锁), §States (区域访问状态)
> **Status**: Done

## Goal

地点访问状态机 (locked→known→unlocked) + EventBus 事件发布。

## Acceptance Criteria

- [ ] AC1: LocationState 枚举 (Locked/Known/Unlocked)
- [ ] AC2: RevealLocation(id) → Locked 变 Known，发布 LocationRevealedEvent
- [ ] AC3: UnlockLocation(id) → Known 变 Unlocked，发布 LocationUnlockedEvent
- [ ] AC4: 直接解锁 (Locked→Unlocked) 有效
- [ ] AC5: 逆向状态变化被拒绝
- [ ] AC6: 查询接口 GetAccessState(locationId) 正确返回

## Test Evidence Path

`tests/Foundation/SceneManagement/LocationAccessTests.cs`
