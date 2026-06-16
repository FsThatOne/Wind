# Story: ts-006 — Time Config YAML

> **Epic**: time-system
> **Type**: Integration
> **Priority**: P2
> **Depends On**: ts-001, DataRegistry (cd-005)
> **ADR Guidance**: ADR-0003 (YAML + DataRegistry)
> **GDD Source**: design/gdd/natural-day-stamina.md §Tuning Knobs
> **Status**: Done

## Goal

将时间系统调优参数外部化为 YAML 配置并集成 DataRegistry。

## Acceptance Criteria

- [ ] **AC1**: `TimeConfig` YAML 包含 days_per_season, base_stamina, stamina_per_con, rest_ratio_nap, fatigue_threshold, zero_stamina_speed_mult
- [ ] **AC2**: DataRegistry 正确加载 TimeConfig
- [ ] **AC3**: 格式错误 → DataLoadException
- [ ] **AC4**: 缺少可选字段时使用 GDD 默认值

## Test Evidence Path

`tests/Foundation/TimeSystem/TimeConfigTests.cs`
