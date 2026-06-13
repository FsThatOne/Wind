# Epic: 物品 / 道具

> **Layer**: Core
> **GDD**: design/gdd/item-system.md
> **Architecture Module**: `Core/Items/`
> **Status**: Done
> **Stories**: 8 stories (it-001 ~ it-008)

## Overview

物品 / 道具系统是《风止》的资源管理与轻量经济层，负责物品定义、背包、堆叠、装备、消耗品、战斗背包、关键物品、秘籍/残卷、商店、拍卖和道具效果查询。它按照 `Core/Items/` 模块边界维护物品数据与库存状态，向战斗、武学、自然日、主线叙事和 UI 提供可查询接口；它不直接处理战斗回合和剧情推进。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Data Configuration Format | 物品品级、模板、配方和效果使用 YAML 配置表驱动 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 物品分类：战斗消耗品、探索消耗品、装备、关键物品 | ADR-0003 ✅ / ⚠️ 字段 schema 需 stories 验证 |
| 无限背包、有限堆叠和关键物品不可丢弃 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 战斗背包容量、战前配置和战中使用道具占回合 | ⚠️ GDD 覆盖，需与 combat-system stories 对齐 |
| 装备品级、基础属性、词条数量上限和随机词条 | ADR-0003 ✅ / ⚠️ 词条生成需 stories 验证 |
| 消耗品效果、丹药品质、食物/干粮恢复体力 | ADR-0003 ✅ / ⚠️ 效果语义需 stories 验证 |
| 残卷、秘籍、信物、书信等关键物品推动叙事和武学学习 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 银两、基础商店、黑市、江湖拍卖和以物易物 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 物品状态需要被存档系统序列化恢复 | ⚠️ GDD 覆盖，需与 save-system 契约对齐 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `item-system.md` 标为 `⚠️ Partial`：ADR-0003 覆盖配置表格式，但未覆盖背包规则、词条生成、战斗背包、经济交易和关键物品约束。创建 stories 时需要补齐真实 TR-ID，并先实现可供战斗与武学系统读取的最小物品契约。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/item-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 物品定义、库存、堆叠、装备、战斗背包、消耗品效果和关键物品规则有自动化测试覆盖
- 物品配置表可加载、校验，并能拒绝非法品级、非法堆叠、缺失效果和不可交易关键物品错误
- 战斗、武学、主线叙事、自然日和存档系统可通过稳定接口读取或恢复物品状态

## Next Step

Run `/story-readiness production/epics/item-system/stories/it-001-item-yaml-schema-and-loader.md` before implementation.

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| it-001 | [物品 YAML 数据模型与加载校验](stories/it-001-item-yaml-schema-and-loader.md) | Config/Data | P0 | — | Complete |
| it-002 | [背包堆叠、拾取与关键物品约束](stories/it-002-inventory-stacking-and-key-items.md) | Logic | P0 | it-001 | Complete |
| it-003 | [装备品级、词条生成与五槽互斥](stories/it-003-equipment-generation-and-slots.md) | Logic | P0 | it-001, it-002 | Complete |
| it-004 | [战斗消耗品与战斗背包契约](stories/it-004-combat-consumables-and-battle-bag.md) | Integration | P0 | it-001, it-002 | Complete |
| it-005 | [炼丹、锻造与精炼规则](stories/it-005-crafting-alchemy-refine.md) | Logic | P1 | it-001, it-002, it-003 | Complete |
| it-006 | [残卷秘籍自学与传授契约](stories/it-006-martial-fragments-teaching.md) | Integration | P1 | it-001, it-002 | Complete |
| it-007 | [银两、商店、黑市与拍卖](stories/it-007-economy-shop-and-auction.md) | Logic | P1 | it-001, it-002, it-005 | Complete |
| it-008 | [物品存档与跨系统查询契约](stories/it-008-item-save-and-query-contracts.md) | Integration | P1 | it-002, it-003, it-004, it-006, it-007 | Complete |
