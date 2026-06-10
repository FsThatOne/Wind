# Story: cd-001 — Core Data Model

> **Epic**: character-data
> **Type**: Logic
> **Priority**: P0 — Foundation dependency for all other stories
> **Depends On**: (none)
> **Blocked By**: (none)
> **ADR Guidance**: ADR-0003 (YAML data backing)
> **GDD Source**: design/gdd/character-attributes.md §Overview, §Detailed Design
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

定义角色属性系统的核心数据模型，包含三层属性结构和钳位规则，作为 character-data 模块的基础类型。

## Scope

### In Scope
- `CharacterAttributes` 类（或 record），包含：
  - **资源属性**: HP (current/max), NeiXi (current/max), Stagger (current/threshold)
  - **基础属性**: Strength, Agility, InnerPower, Insight, Constitution（五维，int，范围 5-50）
  - **派生属性**: Attack, Defense, Speed, CritRate, NeiXiRecovery, TotalPower（只读计算属性）
- 属性钳位规则：所有基础/派生属性 min=1（HP 例外可为 0）
- `CharacterType` 枚举: Player, Companion, Enemy
- 资源属性战斗状态流转接口: `ApplyDamage(amount)`, `SpendNeiXi(amount)`, `AddStagger(amount)`, `ResetForCombat()`
- 破绽衰减钳位到 0

### Out of Scope
- 公式计算逻辑 → cd-002
- Modifier 叠加系统 → cd-003
- 境界/功力判定 → cd-004
- YAML 序列化/反序列化 → cd-005
- 成长接口 → cd-007

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/CharacterAttributes.cs`
- 派生属性的实际计算由 FormulaEngine 提供，此 Story 仅定义属性字段和基本 getter stub
- 遵循 ADR-0003: 数据结构需与 YAML schema 对齐（但反序列化在 cd-005）
- 五维属性 cap = 50（`attr_cap_per_stat`）

## Acceptance Criteria

- [ ] **AC1**: `CharacterAttributes` 包含全部三层属性字段，类型和范围与 GDD 一致
- [ ] **AC2**: `ApplyDamage(30)` 使 HP 从 100 降为 70；HP=10 时 `ApplyDamage(20)` 使 HP=0（不为负）
- [ ] **AC3**: `SpendNeiXi(5)` 使 NeiXi 从 20 降为 15；NeiXi=3 时 `SpendNeiXi(5)` 返回 false 或抛出异常
- [ ] **AC4**: `AddStagger(3)` 使 Stagger 从 2 变为 5；衰减 `DecayStagger()` 后 Stagger=4；Stagger=0 时衰减仍为 0
- [ ] **AC5**: `ResetForCombat()` 将 HP/NeiXi 设为当前 max，Stagger 设为 0
- [ ] **AC6**: 基础属性赋值为 -5 时，钳位到 1 (GDD: "任何基础/派生属性最小值为 1")
- [ ] **AC7**: GDD AC#9 验证 — 体魄=10 时，HP 上限字段值 = 140（暂 stub，cd-002 接入后完整验证）

## Test Evidence Path

`tests/Foundation/CharacterData/CharacterAttributesTests.cs`
