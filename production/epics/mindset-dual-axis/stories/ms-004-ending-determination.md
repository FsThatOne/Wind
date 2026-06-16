# Story 004: 结局判定算法与魔道 override


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
> **Requirement**: `TR-mindset-dual-axis-004`

## Acceptance Criteria

- [x] AC-8: resolve=+20, worldly=-25 判定为“白衣行天下”。
- [x] AC-9: resolve=+10, worldly=+8 时由 resolve 主导判定为“大隐于市”。
- [x] AC-10: resolve=+5, worldly=+5 时判定为“未定之人”。
- [x] AC-11: morality=+32 时输出 ExtremeGood 善恶装饰。
- [x] 魔道 override: morality≤-30 时基础结局强制为 `MoDao`。

## Implementation Notes

实现位于 `MindsetService.DetermineEnding`。终幕先检查 `ExtremeEvil` 魔道 override；未触发时再使用无滞后快照规则，确保相同坐标得到相同结局。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001, ms-002, ms-003
- Unlocks: main-narrative ending integration
