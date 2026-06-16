# Story 002: 节点激活与完成推进


> **Epic**: 主线叙事 / 章节推进
> **Status**: Complete
> **Layer**: Core
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-12

## Context

**GDD**: `design/gdd/main-narrative.md`
**Governing ADRs**: ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture
**Engine**: Godot 4.6.3 / C# .NET 8 | **Risk**: LOW/HIGH mixed

**Control Manifest Rules**:
- Required: Core 对话/叙事数据使用 YAML 图节点格式。
- Required: 跨层通知必须走 `EventBus.Publish<T>()`。
- Forbidden: 不得引入 Ink/Yarn Spinner 或 static event。
- Guardrail: 主线骨架只保存状态和触发事件，不承载具体对白文本和演出表现。

> **Type**: Logic
> **Requirement**: `TR-main-narrative-002`

## Acceptance Criteria

- [x] AC-1: 前置满足后自动激活下一节点。
- [x] AC-2: 五种类型均能正确触发和完成。
- [x] Event: 节点激活/完成通过 EventBus 通知下游。

## Implementation Notes

实现保持纯 C# POCO，不继承 Godot Node。节点图数据与对话系统保持 YAML/图结构语义一致；跨系统只发布 typed `GameEvent`，不直接引用 UI/场景上层。

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/narrative/narrative_progression_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: mn-001
- Unlocks: mn-003, mn-005, mn-006, mn-007
