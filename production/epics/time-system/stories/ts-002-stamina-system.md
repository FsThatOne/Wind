# Story: ts-002 — Stamina System

> **Epic**: time-system
> **Type**: Logic
> **Priority**: P0
> **Depends On**: ts-001
> **GDD Source**: design/gdd/natural-day-stamina.md §Core Rules 4, §F1, §F3, §F5
> **Status**: Ready

## Goal

实现体力池管理系统：上限计算(F1)、消耗/恢复、状态判定(F5)、客栈恢复公式(F3)。

## Acceptance Criteria

- [ ] **AC1**: F1 `max_stamina = base + constitution * stamina_per_con`
- [ ] **AC2**: `ConsumeStamina(amount)` 钳位到 0，力竭后不再扣除
- [ ] **AC3**: `RestoreStamina(amount)` 钳位到 max，不超上限
- [ ] **AC4**: F5 状态判定：>50% 充沛, >0 疲惫, =0 力竭
- [ ] **AC5**: F3 休息恢复：`rest_ratio * max_stamina`，不超过缺口
- [ ] **AC6**: 体魄变化时重算上限，当前体力不主动削减

## Test Evidence Path

`tests/Foundation/TimeSystem/StaminaSystemTests.cs`
