# Story: it-002 — 背包堆叠、拾取与关键物品约束

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Priority**: P0
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-1/6, §E, §H AC-1
> **TR-ID**: TR-item-system-002

## Context

背包是物品系统的运行时核心。它需要支持无限容量、有限堆叠、自动溢出新堆叠、关键物品不可丢弃/出售，并接收战斗、探索和叙事奖励。

**ADR Governing Implementation**: N/A — 纯运行时 POCO 背包规则
**Engine**: Godot 4.6.3 | **Risk**: LOW

## Acceptance Criteria

- [x] `GrantItem(item_id, qty)` 支持战斗奖励、探索拾取和剧情奖励写入背包。
- [x] 可堆叠物品达到 `max_stack` 后自动创建新堆叠，永不因背包容量拒绝拾取。
- [x] 关键物品进入独立列表，不与消耗品/装备混排。
- [x] 关键物品默认不可丢弃、不可出售；仅 `auctionable=true` 可进入拍卖流程。
- [x] 背包排序遵守关键物品 > 装备 > 战斗消耗品 > 探索消耗品，同类内按获取时间逆序。

## Implementation Notes

- 背包状态必须按 item definition 的分类和 max_stack 执行，不在调用方复制规则。
- 物品实例 id 仅装备类需要；堆叠类使用 stack id 或内部序号即可。
- 失败结果使用结构化 result，避免 UI 依赖异常文本。

## QA Test Cases

- **AC-1**: 战斗奖励加入背包。
  - Given: loot table 解析出 `healing_pill x3`
  - When: 调用 `GrantItem`
  - Then: 背包包含对应堆叠且总数为 3
  - Edge cases: qty=0、未知 item id
- **AC-2**: 满堆叠溢出新堆叠。
  - Given: `max_stack=2`
  - When: 获得 5 个同一物品
  - Then: 生成 3 个堆叠，数量为 2/2/1
  - Edge cases: 默认 max_stack=99
- **AC-3**: 关键物品不可丢弃。
  - Given: 玩家拥有一封关键书信
  - When: 调用 discard/sell
  - Then: 返回失败且物品仍在关键物品列表
  - Edge cases: auctionable=true 不允许普通出售

## Test Evidence

**Required evidence**:
- `tests/unit/items/inventory_stacking_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InventoryStackingTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `InventoryService` with stack overflow, equipment instances, independent key item storage, key item discard/sell restrictions and sorted inventory query.
- Added structured `InventoryOperationResult` for caller-safe failure handling.
- Foundation full test suite passed: 1052/1052.

## Dependencies

- Depends on: it-001
- Unlocks: it-004, it-006, it-007, it-008
