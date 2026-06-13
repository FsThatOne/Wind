# Story: it-008 — 物品存档与跨系统查询契约

> **Epic**: item-system
> **Status**: Complete
> **Last Updated**: 2026-06-13
> **Layer**: Core
> **Type**: Integration
> **Priority**: P1
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C States, §F, §H
> **TR-ID**: TR-item-system-008

## Context

物品运行时状态需要被存档系统序列化恢复，并向战斗、主线叙事、自然日、武学和 UI 提供稳定查询接口。

**ADR Governing Implementation**: ADR-0004: Save System Architecture / ADR-0001: Event Bus Architecture
**Engine**: Godot 4.6.3 | **Risk**: MEDIUM

## Acceptance Criteria

- [x] 物品系统实现 `ISaveable`，能保存和恢复库存堆叠、装备实例、装备归属、银两和关键物品。
- [x] 已消耗/传授/售出/分解的物品从背包移除，并可保留操作 log 供叙事或审计使用。
- [x] 读档后装备互斥关系保持，不会产生同一实例多角色穿戴。
- [x] 主线叙事可通过 `GrantItem(item_id, qty, quality_override?)` 或等价接口发放奖励。
- [x] 自然日系统可查询探索消耗品的体力恢复效果。
- [x] UI 查询接口不暴露装备词条百分比，只返回文学描述 key 和结构化标签。

## Implementation Notes

- SaveSnapshot 只保存运行时状态，不保存 YAML 静态定义。
- 恢复时必须重新校验实例归属和模板引用，非法旧存档应给出 warning 并尽量恢复可用状态。
- 查询接口应聚合 definition + runtime state，避免 UI 拼装规则。

## QA Test Cases

- **AC-1**: 存档恢复库存和装备归属。
  - Given: 背包有堆叠物品和一件已装备武器
  - When: Serialize 后 Deserialize 到新服务
  - Then: 数量、实例 id、owner 和 slot 均一致
  - Edge cases: 旧存档缺失可选字段
- **AC-2**: 读档后保持装备互斥。
  - Given: 存档含同一装备实例双 owner 异常数据
  - When: Deserialize
  - Then: 返回 warning 并只保留一个合法 owner
- **AC-3**: 叙事奖励发放。
  - Given: 主线节点配置奖励 item_id 和 qty
  - When: 调用 GrantItem
  - Then: 背包数量增加并可查询

## Test Evidence

**Required evidence**:
- `tests/integration/items/item_save_query_contracts_test.cs`

**Status**: [x] Created and passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "ItemSaveQueryContractsTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `ItemSystemService` as the item-system `ISaveable` facade using `SaveSnapshot` + `JsonElement` runtime payloads.
- Added inventory, equipment ownership and economy snapshot/restore paths for stacks, generated equipment instances, equipped owner slots, key items, silver and operation logs.
- Restore paths validate unknown item/template ids, mismatched equipment instance ids, incompatible slots, duplicate instance owners and duplicate owner-slot assignments, returning structured warnings instead of corrupting runtime state.
- Added stable query boundaries for narrative reward grants, natural-day stamina recovery consumables, combat usable items, martial fragment availability and UI-safe equipment display metadata.
- Removed the unused `qualityOverride` reward parameter from the runtime facade; reward quality is currently represented by concrete `item_id` / equipment template selection rather than a no-op argument.
- Post-review fix strengthened black market barter atomicity so invalid output items fail before any payment item is discarded.
- Code review result: CHANGES REQUIRED -> fixes applied -> APPROVED.
- Foundation full test suite passed: 1104/1104.

## Dependencies

- Depends on: it-002, it-003, it-004, it-006, it-007
- Unlocks: item-system completion
