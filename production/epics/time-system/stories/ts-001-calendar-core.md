# Story: ts-001 — Calendar Core

> **Epic**: time-system
> **Type**: Logic
> **Priority**: P0
> **Depends On**: None
> **GDD Source**: design/gdd/natural-day-stamina.md §Core Rules 1-3, §F2, §F4
> **Status**: Ready

## Goal

实现 12 时辰日历核心状态类和时间/季节推进纯函数。

## Acceptance Criteria

- [ ] **AC1**: `Shichen` 枚举定义 12 时辰（子丑寅卯辰巳午未申酉戌亥）+ `LightPhase` 枚举（Dawn/Day/Dusk/Night）
- [ ] **AC2**: `Season` 枚举（Spring/Summer/Autumn/Winter）
- [ ] **AC3**: `GameCalendar` 持有 currentDay, currentShichen, dayProgress, currentSeason, seasonDay
- [ ] **AC4**: F2 时间推进：`AdvanceProgress(delta)` 正确处理时辰溢出和日切换
- [ ] **AC5**: F4 季节计算：`GetSeasonForDay(day, daysPerSeason)` 返回正确季节
- [ ] **AC6**: 光照分类映射正确：日(辰巳午未申), 晨(寅卯), 昏(酉戌), 夜(子丑亥)
- [ ] **AC7**: 安全默认值：day=1, shichen=卯, season=春

## Test Evidence Path

`tests/Foundation/TimeSystem/CalendarCoreTests.cs`
