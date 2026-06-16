# Story: it-007 — 银两、商店、黑市与拍卖

> **Epic**: item-system
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Priority**: P1
> **Manifest Version**: 2026-06-10
> **GDD 来源**: design/gdd/item-system.md §C CR-5, §D F5, §E, §H AC-7/10
> **TR-ID**: TR-item-system-007

## Context

经济层保持轻量：银两是唯一通货，普通商店固定价格，黑市支持以物易物，拍卖会按事件触发。

**ADR Governing Implementation**: ADR-0003: Data Configuration Format
**Engine**: Godot 4.6.3 | **Risk**: LOW

## Acceptance Criteria

- [x] 银两使用非负整数，任何购买、竞拍、锻造扣款不得导致负数。
- [x] 商店买入价固定，卖出价按 `base_price × sell_ratio` 计算，默认 0.3。
- [x] 关键物品默认不可售卖，`auctionable=true` 的关键物品只允许进入拍卖流程。
- [x] 玩家出价必须高于当前最高价且银两足够。
- [x] NPC 未跟价时，玩家获得拍品并扣除银两。
- [x] 银两不足时，竞拍返回失败原因，不改变拍卖状态。

## Implementation Notes

- 拍卖 NPC AI 保持简单 deterministic 或可注入 random，避免测试不稳定。
- 交易失败必须原子化，不可部分扣款或部分移物。
- 黑市以物易物只验证指定物品组合，不在本 story 实现复杂议价。

## QA Test Cases

- **AC-1**: 商店卖价按比例计算。
  - Given: base_price=100, sell_ratio=0.3
  - When: 查询卖价
  - Then: 返回 30
  - Edge cases: floor 取整、低价物品
- **AC-2**: 拍卖成功。
  - Given: 玩家银两足够，出价高于当前最高，NPC 不跟价
  - When: 提交出价
  - Then: 玩家获得拍品且银两扣除
- **AC-3**: 银两不足禁止出价。
  - Given: 玩家银两低于出价
  - When: 提交出价
  - Then: 返回失败，最高价和库存不变

## Test Evidence

**Required evidence**:
- `tests/unit/items/economy_shop_auction_test.cs`

**Status**: [x] Passing

**Verified**:
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "EconomyShopAuctionTest" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter "Items" --no-restore -v q`
- `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q`

## Completion Notes

- Implemented `EconomyService` for non-negative silver, fixed shop buy price, default sell ratio `0.3`, black market barter and deterministic auction settlement.
- Added `IAuctionNpcBidStrategy` so auction NPC follow/no-follow behavior can be deterministic in tests and extensible later.
- Key items remain non-sellable; `auctionable=true` key items are exposed only through auction eligibility queries.
- All transaction and bid failure paths precheck state before spending silver or moving items.
- Foundation full test suite passed: 1097/1097.

## Dependencies

- Depends on: it-001, it-002, it-005
- Unlocks: it-008
