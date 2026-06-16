# Story: it-005 — 炼丹、锻造与精炼规则

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Priority**: P1
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-4, §D F3/F4, §E, §H AC-5/8
> **TR-ID**: TR-item-system-005

## Context

制作系统负责把材料转化为丹药或装备，并限制精炼不会成为无限强化循环。

**ADR Governing Implementation**: ADR-0003: Data Configuration Format
**Engine**: Godot 4.6.3 | **Risk**: LOW

## Acceptance Criteria

- [x] 炼丹配方消耗草药材料并产出丹药。
- [x] 丹药品质为极/上/中/下之一，药效系数分别为 1.2/1.0/0.8/0.6。
- [x] 锻造配方消耗材料和银两，产出固定品级装备实例。
- [x] 精炼提升属性但不得超过该品级 cap × 0.95。
- [x] 单件装备精炼次数不得超过 `refine_max_count` 默认 3 次。
- [x] 材料不足或已达精炼上限时返回可 UI 表达的失败原因。

## Implementation Notes

- 熟练度公式仍是 open question，本 story 先以配方/材料/可注入品质输入实现稳定契约。
- 精炼只作用于装备实例，不修改装备模板。
- 金钱扣除必须保持非负，失败时不得部分扣材料或银两。

## QA Test Cases

- **AC-1**: 丹药品质影响药效。
  - Given: 面板药效 100
  - When: 分别以极/上/中/下品质生成丹药效果
  - Then: 药效为 120/100/80/60
  - Edge cases: 未知品质拒绝
- **AC-2**: 精炼封顶。
  - Given: 当前属性接近 cap×0.95
  - When: 执行精炼
  - Then: 新值不超过 cap×0.95
  - Edge cases: 已达上限时失败且不扣材料
- **AC-3**: 锻造材料不足失败。
  - Given: 背包缺少矿石或银两
  - When: 调用 forge
  - Then: 返回失败原因且库存不变

## Test Evidence

**Required evidence**:
- `tests/unit/items/crafting_alchemy_refine_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "CraftingAlchemyRefineTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `CraftingService` for alchemy, forging and equipment refinement.
- Added alchemy quality coefficients, silver/material precheck, fixed-grade forge output, refine cap at `grade_cap × 0.95`, and default refine max count 3.
- All failure paths are prechecked before spending materials or silver.
- Foundation full test suite passed: 1080/1080.

## Dependencies

- Depends on: it-001, it-002, it-003
- Unlocks: it-007
