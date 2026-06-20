# Story 001: 三轴坐标与位移


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
> **Requirement**: `TR-mindset-dual-axis-001`

## Acceptance Criteria

- [x] AC-1: 初始化坐标为 resolve=-5, worldly=0, morality=0。
- [x] AC-2: `mindset_shift(resolve, +8)` 将 resolve 从 -5 更新为 +3。
- [x] AC-3: morality=+48 应用 +5 后钳位为 +50。
- [x] AC-19: 多轴位移按顺序执行，区域切换在批量位移后检查。
- [x] AC-20: 首次启动时坐标、声望和双轴初始区域正确。

## Implementation Notes

实现位于 `MindsetState`、`MindsetMath.ClampAxis` 与 `MindsetService.ApplyShift/ApplyShifts`。状态对象保持纯 POCO，不继承 Godot Node。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: None
- Unlocks: ms-002, ms-003, ms-004, ms-005, ms-006, ms-007
