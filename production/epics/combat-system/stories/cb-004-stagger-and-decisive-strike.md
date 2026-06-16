# Story: cb-004 — 破绽系统与一击决胜

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P0 — 战斗高潮机制
> **Estimate**: S（约 3-4h）
> **依赖**: cb-002, cb-003
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 6, §Formulas F6-F7
> **TR-ID**: TR-combat-system-004
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现破绽累积规则、衰减逻辑和一击决胜判定与伤害公式。

## 范围

### 包含
- StaggerService：累积规则（克制+2、被克攻方+1、反制额外+1、协同+1）
- 衰减规则：每回合 -1，最低 0
- 一击决胜判定：目标 stagger >= threshold
- F6: decisive_damage = base_damage × 2.0（无视克制和暴击）
- 决胜一击后清空目标破绽

### 不包含
- 多人协同破绽的具体触发时机 → cb-009
- UI 高亮和动画 → combat-ui

## 技术说明

- threshold 默认 5，可配置（Tuning Knob）
- decisive strike 无视 counter_multiplier 和 crit_multiplier

## 验收标准

- [x] 克制命中：守方 +2 破绽
- [x] 被克制命中：攻方 +1 破绽
- [x] 破绽 >= 5 时 IsStaggerExposed = true
- [x] 决胜一击伤害 = base_damage × 2.0
- [x] 决胜一击后目标破绽清零
- [x] 每回合衰减 -1 不低于 0

## 测试证据路径

`tests/unit/combat/stagger_and_decisive_test.cs`

## 依赖关系

- Depends on: cb-002, cb-003
- Unlocks: cb-006, cb-009

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

