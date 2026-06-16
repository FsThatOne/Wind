# Story: cb-005 — 行动注册表与执行框架

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P0 — 行动类型 SSoT
> **Estimate**: M（约 4-6h）
> **依赖**: cb-001, cb-002
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Action Registry, §Core Rules 2-③
> **TR-ID**: TR-combat-system-005
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

定义战斗行动类型注册表和统一执行框架，让所有行动（出招/反制/决胜/调息/普攻/道具）通过同一管线执行。

## 范围

### 包含
- ActionType 枚举：Move, Counter, Decisive, Breathe, BasicAttack, Item
- BattleAction DTO：actor、actionType、targetId、moveId、itemId
- IActionExecutor 接口和基础实现
- 内息可用性检查（CanAfford）
- 调息回复公式 F8：meditation_recovery = ceil(neixi_recovery × 0.5)
- 普通攻击公式：attack × 0.3，无体系

### 不包含
- 反制的完整判定逻辑 → cb-006
- 顿悟行动 → 后续 Sprint
- 道具效果执行 → item-system

## 技术说明

- 行动执行返回 ActionResult DTO（伤害值、状态变化、是否命中等）
- 道具行动暂时只做占位，返回 NotImplemented

## 验收标准

- [x] ActionType 枚举包含全部 6 种行动
- [x] 使用招式时正确扣除内息
- [x] 内息不足时行动执行返回失败
- [x] 调息正确回复 ceil(neixi_recovery × 0.5) 内息
- [x] 普通攻击造成 attack × 0.3 伤害，无体系
- [x] 行动执行框架可扩展新行动类型

## 测试证据路径

`tests/unit/combat/action_registry_execution_test.cs`

## 依赖关系

- Depends on: cb-001, cb-002
- Unlocks: cb-006, cb-009, cb-010

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

