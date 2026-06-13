# Story: it-003 — 装备品级、词条生成与五槽互斥

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Priority**: P0
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-2, §D F1/F2, §H AC-3/4/11/12
> **TR-ID**: TR-item-system-003

## Context

装备是轻量个性化与叙事锚点，不应成为刷数值核心。系统必须稳定生成基础属性、词条和装备槽占用，并防止同一实例被多个角色同时装备。

**ADR Governing Implementation**: ADR-0003: Data Configuration Format
**Engine**: Godot 4.6.3 | **Risk**: LOW

## Acceptance Criteria

- [x] 装备基础属性按 `base_attr = template_attr × grade_multiplier` 计算。
- [x] 词条数量由品级固定决定；词条数值随机范围为满值 40%–100%。
- [x] 传说装备使用手工固定属性和词条，不走随机生成。
- [x] 主角与同伴均显示主手、身甲、足具、饰品 1、饰品 2 五个装备槽。
- [x] 同一装备实例不能同时装备给多个角色，也不能同时占用同一角色两个饰品槽。
- [x] 装备信息只输出文学描述 key，不显示品质颜色或百分比。

## Implementation Notes

- 装备实例必须具有唯一 `item_instance_id`。
- 装备归属使用单一 owner 映射，转移前必须先卸下。
- 随机生成应注入 deterministic random source，保证测试可复现。
- 属性修改器输出为结构化查询结果；不要直接修改角色属性服务。

## QA Test Cases

- **AC-1**: 五品武器基础攻击乘以 2.2。
  - Given: template_attack=10, grade=五品
  - When: 生成装备实例
  - Then: attack modifier 为 22
  - Edge cases: 传说 ×7.0、九品 ×1.0
- **AC-2**: 六品装备生成 2 条词条。
  - Given: 六品装备和词条池
  - When: 使用 seeded random 生成实例
  - Then: 词条数量为 2，值都在 40%–100%
  - Edge cases: 一品 4 条、传说固定
- **AC-3**: 装备互斥。
  - Given: 装备实例已由主角穿戴
  - When: 同伴尝试直接穿戴同一实例
  - Then: 返回失败且 owner 不变
  - Edge cases: 饰品 1/2 重复穿戴同一实例

## Test Evidence

**Required evidence**:
- `tests/unit/items/equipment_generation_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "EquipmentGenerationTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `EquipmentService` with grade multipliers, deterministic random source injection, affix rolling, fixed legendary affixes and five-slot ownership.
- Integrated generated equipment instances into `InventoryService`.
- Foundation full test suite passed: 1061/1061.

## Dependencies

- Depends on: it-001, it-002
- Unlocks: it-005, it-008
