# Story: it-004 — 战斗消耗品与战斗背包契约

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Priority**: P0
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-3, §E, §H AC-2
> **TR-ID**: TR-item-system-004

## Context

战斗道具是 Burst+Read 的保守选择：可以续命，但必须消耗当前回合行动机会，不能同时出招、调息或反制。

**ADR Governing Implementation**: ADR-0001: Event Bus Architecture / ADR-0003: Data Configuration Format
**Engine**: Godot 4.7-stable | **Risk**: MEDIUM

## Acceptance Criteria

- [x] 战斗行动阶段可查询 `combat_usable=true` 且数量大于 0 的道具列表。
- [x] 当前持有 0 个可用战斗道具时，使用道具行动不可用。
- [x] 使用战斗道具会消耗 1 个堆叠数量并返回结构化效果结果。
- [x] 使用道具占用本回合行动机会，不能与出招、调息或反制同回合并存。
- [x] 最后一件消耗品用完后，战斗背包可用性立即更新。

## Implementation Notes

- 物品系统只提供 `CanUseCombatItem` / `UseCombatItem` / `GetCombatUsableItems` 契约，不直接推进 Combat FSM。
- 战斗系统应通过 ActionRegistry 注册 `use_item`，本 story 只保证物品侧 result 可被注册动作消费。
- 道具效果保持结构化，例如 heal_hp、restore_qi、cleanse、buff。

## QA Test Cases

- **AC-1**: 战斗背包只显示战斗可用道具。
  - Given: 背包含伤药、干粮、关键书信
  - When: 查询战斗可用道具
  - Then: 只返回伤药
  - Edge cases: 数量为 0 的战斗道具不返回
- **AC-2**: 使用道具消耗回合机会。
  - Given: 战斗行动上下文未提交行动
  - When: 使用伤药
  - Then: 返回 action_consumed=true 且物品数量减少 1
  - Edge cases: 已提交出招后再次使用道具应失败
- **AC-3**: 最后一件用完后禁用。
  - Given: 仅剩 1 个可用战斗道具
  - When: 使用该道具
  - Then: 再次查询 `HasCombatUsableItem` 为 false

## Test Evidence

**Required evidence**:
- `tests/integration/items/combat_consumables_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "CombatConsumablesTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `GetCombatUsableItems`, `HasCombatUsableItem`, `CanUseCombatItem` and `UseCombatItem`.
- Added `CombatItemActionContext` and `CombatItemUseResult` so combat can consume item actions through ActionRegistry without item code advancing Combat FSM.
- Foundation full test suite passed: 1068/1068.

## Dependencies

- Depends on: it-001, it-002
- Unlocks: combat-system item action integration
