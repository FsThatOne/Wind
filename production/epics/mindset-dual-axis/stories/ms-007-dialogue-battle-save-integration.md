# Story 007: 对话、心境战斗与存档集成


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
> **Requirement**: `TR-mindset-dual-axis-007`

## Acceptance Criteria

- [x] AC-15: 心境战斗失败后重试并胜利时，仅最终叙事结果的位移生效。
- [x] AC-16: 存档中 resolve=60 时加载钳位为 +50，并记录警告。
- [x] GDD interaction: 对话系统 `mindset_shift` 事件通过 EventBus 驱动心境变化，并在对话结束后批量应用。

## Implementation Notes

实现位于 `MindsetDialogueBridge`、`MindsetService.ApplyMindsetBattleResult` 与 `MindsetSaveLoader`。桥接层订阅 `DialogueMindsetShiftEvent`，先收集事件，对话结束后调用 `ApplyPending()` 批量提交。

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/unit/mindset/mindset_service_test.cs; tests/unit/mindset/mindset_dialogue_bridge_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: ms-001, ms-002, ms-003
- Unlocks: dialogue-system and save-system runtime wiring
