# Story: ns-001 — Generic FSM

> **Epic**: npc-state
> **Type**: Logic
> **Priority**: P0 — ADR-0008 核心基础设施
> **Depends On**: None
> **ADR Guidance**: ADR-0008 (泛型 FSM 基类)
> **GDD Source**: design/gdd/npc-state.md §States and Transitions
> **Status**: Done

## Goal

实现纯 C# 泛型有限状态机基类 `StateMachine<TState>`，为 NPC 态度 FSM、场景过渡状态机等提供统一基础。

## Acceptance Criteria

- [ ] **AC1**: `StateMachine<TState>` 支持注册状态和转换规则
- [ ] **AC2**: 转换时执行 guard 条件检查，guard 返回 false 时转换被拒绝
- [ ] **AC3**: 转换成功时触发 OnExit → OnEnter 回调
- [ ] **AC4**: 尝试未注册的转换 → 返回 false，不改变当前状态
- [ ] **AC5**: 支持 `ForceTransition` 跳过 guard（用于主线强制推进）
- [ ] **AC6**: 提供 `CurrentState` 只读属性和 `History` 最近 N 条转换记录

## Test Evidence Path

`tests/Foundation/StateMachine/GenericFsmTests.cs`
