# Story: it-001 — 物品 YAML 数据模型与加载校验

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Config/Data
> **Priority**: P0
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-1/2/6, §D, §H AC-9
> **TR-ID**: TR-item-system-001

## Context

物品系统必须先拥有稳定的静态数据契约，供背包、装备、消耗品、经济和叙事奖励统一查询。

**ADR Governing Implementation**: ADR-0003: Data Configuration Format
**Engine**: Godot 4.7-stable | **Risk**: LOW

## Acceptance Criteria

- [x] 定义 `assets/data/items/` 下 consumables、equipment_templates、key_items、recipes、affix_pools 的 YAML 兼容模型。
- [x] 加载时校验 id 唯一、分类合法、品级/稀有度枚举合法、max_stack 范围合法、效果和引用完整。
- [x] 基础材料稀有度可查询；物品信息允许显示稀有度标签但不引入装备品质颜色。
- [x] YAML 错误能报告文件定位信息，不静默吞错。

## Implementation Notes

- 配置使用 YAML 1.2 + YamlDotNet，路径保持 `assets/data/items/{table}.yaml`。
- Foundation/Core 逻辑保持 POCO，不依赖 Godot Node。
- 运行时查询接口应只读，后续 story 不应绕过 loader 直接解析 YAML。

## QA Test Cases

- **AC-1**: 合法 YAML 能加载为 item registry。
  - Given: consumable/equipment/key item/recipe/affix pool 各至少一条合法配置
  - When: 调用 item config loader
  - Then: 可按 id 查询定义，分类与字段保持原值
  - Edge cases: 空表、注释、嵌套效果列表
- **AC-2**: 非法配置快速失败。
  - Given: 重复 id、非法品级、非法 max_stack 或缺失引用
  - When: 调用 loader
  - Then: 返回明确错误并包含文件名或表名
  - Edge cases: 多错误聚合、未知枚举

## Test Evidence

**Required evidence**:
- `tests/unit/items/item_yaml_schema_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "ItemYamlSchemaTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `FengZhi.Foundation.Items` POCO definitions, YAML loader, cross-table validation and O(1) read-only tables.
- Added sample YAML files under `assets/data/items/`.
- Foundation full test suite passed: 1043/1043.

## Dependencies

- Depends on: None
- Unlocks: it-002, it-003, it-004, it-005, it-006, it-007
