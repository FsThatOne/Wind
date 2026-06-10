# Story: cd-002 — Formula Engine (F1-F9)

> **Epic**: character-data
> **Type**: Logic
> **Priority**: P0 — 战斗系统的直接数值依赖
> **Depends On**: cd-001
> **Blocked By**: cd-001 (需要属性类型定义)
> **ADR Guidance**: ADR-0003 (tuning knobs 可配置化)
> **GDD Source**: design/gdd/character-attributes.md §Formulas
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

实现 9 个纯函数公式（F1-F8 + 功力总值），将基础属性映射为派生属性。所有公式零副作用、可独立单测。

## Scope

### In Scope
- `FormulaEngine` 静态类或服务，方法签名：
  - `F1_AttackForType(baseAttack, primaryAttr, scalingFactor, modifierSum)` → int
  - `F2_Defense(constitution, strength, modifierSum)` → int
  - `F3_Speed(agility, insight, modifierSum)` → int
  - `F4_MaxHp(baseHp, constitution, modifierSum)` → int
  - `F5_MaxNeiXi(baseNeiXi, innerPower, modifierSum)` → int
  - `F6_NeiXiRecovery(innerPower, modifierSum)` → int (min=2)
  - `F7_CritRate(agility, modifierSum)` → float (cap=30%)
  - `F8_PowerComparison(selfPower, targetPower)` → PowerLevel enum
  - `TotalPower(五维之和)` → int
- `PowerLevel` 枚举: FarWeaker, Weaker, Comparable, Stronger, FarStronger
- 与 `CharacterAttributes` 集成: 派生属性 getter 调用 FormulaEngine

### Out of Scope
- Modifier 叠加的 modifierSum 计算 → cd-003
- 境界映射 → cd-004
- Tuning knobs 外部化到 YAML → cd-005 (本 Story 先用 const)

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/FormulaEngine.cs`
- 所有方法为 `public static`，纯函数
- F6 下限硬锁 2: `Math.Max(2, ...)`
- F7 上限硬锁 30%: `Math.Min(0.30f, ...)`
- F8 ratio 区间: <0.5/0.5-0.8/0.8-1.2/1.2-2.0/>2.0

## Acceptance Criteria

- [ ] **AC1**: GDD AC#2 — 力量=20, base=10, sf=1.0, mod=5 → F1 返回 35
- [ ] **AC2**: GDD AC#9 — base_hp=100, 体魄=10, mod=0 → F4 返回 140
- [ ] **AC3**: GDD AC#7 — selfPower=80, targetPower=160 → F8 返回 FarWeaker (ratio=0.5)
- [ ] **AC4**: GDD AC#8 — 速度相同时不由本公式处理（F3 仅计算值，先手由战斗系统裁定）
- [ ] **AC5**: F6 内力=0, mod=0 → 返回 2（下限保护）
- [ ] **AC6**: F7 敏捷=100(超上限测试), mod=0 → 返回 30%（上限保护）
- [ ] **AC7**: 所有 9 个公式的 GDD 示例值通过单测

## Test Evidence Path

`tests/Foundation/CharacterData/FormulaEngineTests.cs`
