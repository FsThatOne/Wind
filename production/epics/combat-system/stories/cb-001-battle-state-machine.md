# Story: cb-001 — 战斗状态机与回合流程框架

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P0 — 战斗核心骨架
> **Estimate**: M（约 4-6h）
> **依赖**: 无（Sprint 2 起点）
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 1-2, §States and Transitions
> **TR-ID**: TR-combat-system-001
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现战斗状态机和回合流程的核心骨架，定义阶段枚举、BattleInstance 生命周期和回合循环。

## 范围

### 包含
- BattleState 枚举（Initializing, RoundStart, IntentReveal, PlayerDecision, Resolution, RoundEnd, BattleOver）
- BattleInstance 生命周期管理
- 回合循环驱动（AdvancePhase）
- 回合开始时内息回复和破绽衰减触发点
- 回合计数和最大回合数判定

### 不包含
- 具体伤害结算逻辑 → cb-003
- AI 决策和意图系统 → cb-007
- 事件广播 → cb-008

## 技术说明

- 纯状态机，不依赖引擎 Node
- 外部通过 AdvancePhase() 驱动，适配同步测试和异步 UI
- 战斗结束条件检查在 RoundEnd 阶段执行

## 验收标准

- [x] BattleInstance 创建后处于 Initializing 状态
- [x] AdvancePhase 按正确顺序推进所有阶段
- [x] 回合开始时触发内息回复和破绽衰减
- [x] 达到 max_rounds 后状态变为 BattleOver
- [x] 一方全灭时状态变为 BattleOver

## 测试证据路径

`tests/unit/combat/battle_state_machine_test.cs`

## 依赖关系

- Depends on: (none)
- Unlocks: cb-003, cb-005, cb-007, cb-008, cb-010

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

