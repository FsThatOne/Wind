# Story 002: 区域判定与滞后带


> **Epic**: 心境双轴
> **Status**: Complete
> **Layer**: Core
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-12

## Context

**GDD**: `design/gdd/mindset-dual-axis.md`
**Governing ADRs**: ADR-0001: Event Bus Architecture
**Engine**: Godot 4.7-stable / C# .NET 8 | **Risk**: HIGH

**Control Manifest Rules**:
- Required: 跨层通知必须走 `EventBus.Publish<T>()`。
- Required: 事件类型必须为继承 `GameEvent` 的不可变 record。
- Forbidden: 不得用 static event 或 string-based 事件名连接。
- Guardrail: EventBus 发布应保持轻量，业务状态保留在纯 C# 服务内。

> **Type**: Logic
> **Requirement**: `TR-mindset-dual-axis-002`

## Acceptance Criteria

- [x] AC-4: neutral 中 resolve 从 14 到 16 时切换 positive 并发布 `MindsetZoneChangedEvent`。
- [x] AC-5: positive 中 resolve 从 15 到 14 时因滞后保持 positive。
- [x] AC-6: positive 中 resolve 从 14 到 12 时退出到 neutral 并发布事件。
- [x] AC-21: 旧存档缺少区域字段时使用 `zone_initial()` 重新计算。

## Implementation Notes

实现位于 `MindsetMath.GetInitialPolarity`、`MindsetMath.ApplyHysteresis` 与 `MindsetMath.CombineZone`。终幕判定不复用带滞后的日常区域状态。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001
- Unlocks: ms-004, ms-006
