# Story: cb-009 — 多人战斗与协同破绽

> **Epic**: combat-system
> **类型**: Integration
> **优先级**: P1 — 多人战斗支撑
> **Estimate**: M（约 4-6h）
> **依赖**: cb-003, cb-004, cb-005, cb-006
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 7, §Formulas F7
> **TR-ID**: TR-combat-system-009
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现多人战斗框架：多角色目标选择、协同攻击判定和协同破绽加成。

## 范围

### 包含
- 多 BattleCombatant 管理（己方队列 + 敌方队列）
- 同回合同目标协同判定逻辑
- 协同加成：两人同回合克制同目标 → 额外 +1 破绽
- 敌方不享受协同加成（非对称设计）
- 角色落败后从行动队列移除
- 胜负判定：一方全灭

### 不包含
- 敌方 AI 目标选择策略 → enemy-ai
- 5人上阵 UI → combat-ui
- 凝神保护 → 后续 Sprint

## 技术说明

- 协同判定在 Resolution 阶段所有行动收集后统一检查
- 敌方协同不加成是硬编码规则

## 验收标准

- [x] 两己方角色同回合克制同目标：目标额外 +1 破绽
- [x] 两敌方角色同回合攻击同目标：不触发协同加成
- [x] 一方全灭时战斗结束
- [x] 落败角色不再参与行动
- [x] 双方同回合全灭判定为"惜败"

## 测试证据路径

`tests/unit/combat/multi_combatant_coop_test.cs`

## 依赖关系

- Depends on: cb-003, cb-004, cb-005, cb-006
- Unlocks: cb-010

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

