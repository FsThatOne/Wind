# Story 005: 声望追赶系统


> **Epic**: 心境双轴
> **Status**: Complete
> **Layer**: Core
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-12

## Context

**GDD**: `design/gdd/mindset-dual-axis.md`
**Governing ADRs**: ADR-0001: Event Bus Architecture
**Engine**: Godot 4.6.3 / C# .NET 8 | **Risk**: HIGH

**Control Manifest Rules**:
- Required: 跨层通知必须走 `EventBus.Publish<T>()`。
- Required: 事件类型必须为继承 `GameEvent` 的不可变 record。
- Forbidden: 不得用 static event 或 string-based 事件名连接。
- Guardrail: EventBus 发布应保持轻量，业务状态保留在纯 C# 服务内。

> **Type**: Logic
> **Requirement**: `TR-mindset-dual-axis-005`

## Acceptance Criteria

- [x] AC-17: morality=-20, reputation=-10, rate=0.2 时章节结束后 reputation=-12。
- [x] AC-18: morality=+5, reputation=-20, rate=0.2 时章节结束后 reputation=-15。

## Implementation Notes

实现位于 `MindsetService.UpdateReputation`。声望使用与善恶相同的 5 档阈值，但作为延迟派生值。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001, ms-003
- Unlocks: jianghu event hooks
