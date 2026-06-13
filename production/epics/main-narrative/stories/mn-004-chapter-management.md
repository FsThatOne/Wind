# Story 004: 章节切换与线性骨架


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

> **Type**: Integration
> **Requirement**: `TR-main-narrative-004`

## Acceptance Criteria

- [x] AC-4: 章节门通过后切换章节，触发色调变更和区域解锁事件。
- [x] AC-5: 不可回退到已完成章节。
- [x] AC-6: 章节过渡演出由事件触发，不在本系统承载表现。

## Implementation Notes

实现保持纯 C# POCO，不继承 Godot Node。节点图数据与对话系统保持 YAML/图结构语义一致；跨系统只发布 typed `GameEvent`，不直接引用 UI/场景上层。

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/unit/narrative/narrative_chapter_manager_test.cs`

**Status**: [x] Created and passing

## Dependencies

- Depends on: mn-003
- Unlocks: scene-management integration
