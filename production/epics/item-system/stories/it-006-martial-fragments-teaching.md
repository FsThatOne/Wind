# Story: it-006 — 残卷秘籍自学与传授契约

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Priority**: P1
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-6, §F, §H AC-6
> **TR-ID**: TR-item-system-006

## Context

残卷和秘籍是物品系统与武学成长的桥梁。玩家未学会时只能自学，已学会后可以消耗残卷传授给同伴。

**ADR Governing Implementation**: ADR-0001: Event Bus Architecture / ADR-0003: Data Configuration Format
**Engine**: Godot 4.7-stable | **Risk**: MEDIUM

## Acceptance Criteria

- [x] 残卷/秘籍作为关键物品保存，不可丢弃、不可出售。
- [x] 主角未学会对应招式时，残卷只能用于自学。
- [x] 主角已学会对应招式时，可指定同伴传授。
- [x] 传授成功后残卷消耗，同伴获得该招式的结构化请求被发布或返回。
- [x] 同伴已学会该招式时禁止传授，残卷不消耗。

## Implementation Notes

- 不直接调用 MartialArtsService 具体实现；输出 `MartialFragmentLearnRequest` 或等价 result，由武学系统消费。
- 保留 target_character 参数，满足 GDD 已确认的传授需求。
- 失败时不得改变库存。

## QA Test Cases

- **AC-1**: 未学会时只能自学。
  - Given: 主角未学会 move_a 且持有残卷
  - When: 尝试传授给同伴
  - Then: 返回失败且提示需先自学
  - Edge cases: target 为空时执行自学
- **AC-2**: 已学会时可传授。
  - Given: 主角已学会 move_a，同伴未学会，背包含残卷
  - When: 传授给同伴
  - Then: 残卷数量减少 1，输出目标同伴学习请求
- **AC-3**: 已学会同伴拒绝重复传授。
  - Given: 同伴已学会 move_a
  - When: 再次传授
  - Then: 返回失败且残卷不消耗

## Test Evidence

**Required evidence**:
- `tests/integration/items/martial_fragments_teaching_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "MartialFragmentsTeachingTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `MartialFragmentTeachingService` to convert linked key items into `MartialFragmentLearnRequest` results without directly mutating Martial Arts state.
- Added optional EventBus publication via `MartialFragmentLearnRequestedEvent` for event-driven integrations.
- Added `InventoryService.ConsumeKeyItem` as a reason-gated system consumption path while keeping normal discard/sell forbidden for key items.
- Foundation full test suite passed: 1086/1086.

## Dependencies

- Depends on: it-001, it-002
- Unlocks: martial-arts-system teaching integration
