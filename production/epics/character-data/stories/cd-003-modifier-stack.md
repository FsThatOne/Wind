# Story: cd-003 — Modifier Stack

> **Epic**: character-data
> **Type**: Logic
> **Priority**: P1 — 装备/心法/buff 的属性叠加基础
> **Depends On**: cd-001, cd-002
> **Blocked By**: cd-001
> **ADR Guidance**: ADR-0003
> **GDD Source**: design/gdd/character-attributes.md §Detailed Design (修改器层), §Edge Cases
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

实现三层属性修改器系统，支持来源管理、同源去重和正确的叠加计算。

## Scope

### In Scope
- `AttributeModifier` 数据结构:
  - `Source` (string, 来源标识)
  - `Layer` (enum: Permanent, SemiPermanent, Temporary)
  - `Attribute` (enum: 对应五维+派生)
  - `Value` (int/float, 加算值)
  - `Duration` (int, 仅 Temporary 层使用, -1=永久)
- `ModifierStack` 类:
  - `Add(modifier)` — 添加修改器，同源检查
  - `Remove(source)` — 按来源移除
  - `GetSum(attribute)` — 计算某属性的 modifierSum
  - `TickTurn()` — 临时层 duration 递减，到期移除
  - `ClearTemporary()` — 战斗结束清理
- 同源不叠加规则: 同一 source 只保留最高 value
- 钳位: 计算后属性 < 1 则钳位到 1
- 三层语义:
  - Permanent: 章节成长/顿悟（不可移除）
  - SemiPermanent: 心法/装备（卸下时移除）
  - Temporary: 战斗 buff（按回合衰减或战斗结束清除）

### Out of Scope
- 具体的装备/心法数据 → 后续 Feature 层
- 成长节点的修改器写入 → cd-007

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/ModifierStack.cs`
- 与 FormulaEngine 集成: `CharacterAttributes` 的派生属性 getter 调用 `_modifiers.GetSum(attr)` 作为 modifierSum 参数
- 同源判定用 string 精确匹配（如 `"equipment:iron_sword"`, `"buff:tiger_stance"`）

## Acceptance Criteria

- [ ] **AC1**: GDD AC#10 — 同一装备修改器施加两次，GetSum 只取一次最高值
- [ ] **AC2**: 不同来源的修改器正常叠加（来源A +5, 来源B +3 → GetSum=8）
- [ ] **AC3**: Temporary 修改器 duration=2, TickTurn() 两次后自动移除
- [ ] **AC4**: ClearTemporary() 只清除 Temporary 层，Permanent/SemiPermanent 保留
- [ ] **AC5**: 修改器使属性降至 0 以下时，最终值钳位到 1
- [ ] **AC6**: Remove("source_x") 精确移除该来源的所有修改器

## Test Evidence Path

`tests/Foundation/CharacterData/ModifierStackTests.cs`
