# Story 006: 查询接口与朦胧化展示契约


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

> **Type**: Integration
> **Requirement**: `TR-mindset-dual-axis-006`

## Acceptance Criteria

- [x] AC-14: NPC 配置兼容区域为“白衣入世”“大隐于市”时，当前“孤剑入世”返回 false。
- [x] GDD UI requirements: 提供区域、善恶档位、文学化描述和视觉参数查询。
- [x] GDD hidden-axis rule: 朦胧化展示不暴露善恶数值，也不暴露坐标数字。

## Implementation Notes

实现位于 `MindsetCompatibilityService`、`MindsetPresentationService` 以及 `MindsetService` 的只读查询方法。UI 获取文学描述和视觉参数，不直接读取数值。

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs; tests/unit/mindset/mindset_presentation_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001, ms-002, ms-003
- Unlocks: romance, NPC reaction, blurred UI
