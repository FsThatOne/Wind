# Story 003: 善恶档位与档位事件


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
> **Requirement**: `TR-mindset-dual-axis-003`

## Acceptance Criteria

- [x] AC-7: morality 从 -16 到 -14 时由 Evil 切换到 Neutral 并发布 `MoralityTierChangedEvent`。
- [x] AC-12: 转念事件可将 morality=-25 通过 +12 更新为 -13，并切换到 Neutral。
- [x] AC-13: 15 次善行加 1 次转念事件可让 morality 从 -25 到 +17。

## Implementation Notes

善恶档位由 `MindsetMath.GetMoralityTier` 统一判定，无滞后带。`MindsetService.ApplyShifts` 在批量位移后统一检查档位变化。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001
- Unlocks: ms-004, ms-005, ms-006
