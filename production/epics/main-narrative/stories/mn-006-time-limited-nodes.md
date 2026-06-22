# Story 006: 限时主线与强制推进


> **Epic**: 主线叙事 / 章节推进
> **Status**: Complete
> **Layer**: Core
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-12

## Context

**GDD**: `design/gdd/main-narrative.md`
**Governing ADRs**: ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture
**Engine**: Godot 4.7-stable / C# .NET 8 | **Risk**: LOW/HIGH mixed

**Control Manifest Rules**:
- Required: Core 对话/叙事数据使用 YAML 图节点格式。
- Required: 跨层通知必须走 `EventBus.Publish<T>()`。
- Forbidden: 不得引入 Ink/Yarn Spinner 或 static event。
- Guardrail: 主线骨架只保存状态和触发事件，不承载具体对白文本和演出表现。

> **Type**: Integration
> **Requirement**: `TR-main-narrative-006`

## Acceptance Criteria

- [x] AC-10: 限时倒计时从节点激活时开始，非呼吸期内。
- [x] AC-11: 超时后正确触发强制推进事件。
- [x] AC-12: NPC 对话可按剩余时间查询 3 档文本键。

## Implementation Notes

实现保持纯 C# POCO，不继承 Godot Node。节点图数据与对话系统保持 YAML/图结构语义一致；跨系统只发布 typed `GameEvent`，不直接引用 UI/场景上层。

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/unit/narrative/narrative_time_limit_test.cs`

**Status**: [x] Passing

**Verified**:
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "NarrativeTimeLimitTest" --no-restore -v q`
- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Narrative" --no-restore -v q`
- `dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Dependencies

- Depends on: mn-002, mn-005
- Unlocks: time-system integration
