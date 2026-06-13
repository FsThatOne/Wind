# Story: cb-002 — 战斗角色模型与资源系统

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P0 — 战斗数据载体
> **Estimate**: S（约 2-3h）
> **依赖**: 无
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 1, §Formulas F1
> **TR-ID**: TR-combat-system-002
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

定义战斗中角色的运行时数据模型，包含 HP/内息/破绽/攻防/速度/暴击等资源，以及资源修改方法和边界保护。

## 范围

### 包含
- BattleCombatant 类（HP、MaxHP、Neixi、MaxNeixi、Stagger、Attack per type、Defense、Speed、CritRate、InsightStat）
- 资源修改方法（ApplyDamage、SpendNeixi、RecoverNeixi、AddStagger、DecayStagger）
- 最低/最高值钳位保护
- 状态查询（IsAlive、IsStaggerExposed、CanAfford）
- NeixiRecovery 回合自然回复量

### 不包含
- 从角色属性系统读取初始值 → cb-010
- 伤害计算公式 → cb-003

## 技术说明

- POCO 结构，不继承 Node
- 破绽阈值默认 5（可配置）
- HP 最低 0，内息最低 0，破绽最低 0

## 验收标准

- [x] BattleCombatant 初始化后 HP=MaxHP、Neixi=MaxNeixi、Stagger=0
- [x] ApplyDamage 正确扣血并钳位到 0
- [x] SpendNeixi 内息不足时返回失败
- [x] DecayStagger 每次 -1 最低到 0
- [x] IsStaggerExposed 在破绽 >= 5 时返回 true

## 测试证据路径

`tests/unit/combat/battle_combatant_model_test.cs`

## 依赖关系

- Depends on: (none)
- Unlocks: cb-003, cb-004, cb-005, cb-006, cb-007

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

