# Epic: 角色属性/功力

> **Layer**: Foundation
> **GDD**: design/gdd/character-attributes.md
> **Architecture Module**: `Foundation/CharacterData/`
> **Status**: Ready
> **Stories**: 7 stories created (see `stories/` directory)

## Overview

角色属性/功力是整个游戏的数值根基——一个集中管理所有可战斗角色（玩家、同伴、敌人）核心数据的系统。它为下游系统（回合制战斗、武学组合、队伍管理、敌方 AI、自然日+体力、物品/道具、朦胧化 UI）提供统一的属性定义、数值访问和成长规则。实现 `ICharacterRegistry` 接口、`FormulaEngine`（F1-F9 公式）和 YAML 驱动的角色数据模型。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Data Configuration Format | YAML 1.2 + YamlDotNet，启动时加载到 DataRegistry | LOW |

## GDD Requirements (from Acceptance Criteria)

| Requirement | ADR Coverage |
|-------------|--------------|
| 属性数据模型 (HP/内息/破绽/功力等) | ADR-0003 ✅ |
| 功力曲线和成长公式 (F1-F9) | ⚠️ 基础设施覆盖 |
| 角色工厂方法 (玩家/同伴/敌人) | ⚠️ 基础设施覆盖 |
| 属性修改器系统 (buff/debuff) | ⚠️ 基础设施覆盖 |
| YAML 配置驱动的角色数据 | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/character-attributes.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- `ICharacterRegistry` 和 `FormulaEngine` 通过单元测试验证所有 9 个公式
- YAML 角色配置可被 DataRegistry 正确加载和查询

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| cd-001 | Core Data Model | Logic | P0 | — | Done |
| cd-002 | Formula Engine (F1-F9) | Logic | P0 | cd-001 | Done |
| cd-003 | Modifier Stack | Logic | P1 | cd-001 | Done |
| cd-004 | Realm & Power Comparison | Logic | P1 | cd-002 | Done |
| cd-005 | YAML Character Config | Integration | P1 | cd-001 | Ready |
| cd-006 | Character Registry | Integration | P1 | cd-005 | Ready |
| cd-007 | Growth System | Logic | P1 | cd-003, cd-004 | Done |

**Implementation Order**: cd-001 → cd-002 → cd-003 → cd-004/cd-005 (parallel) → cd-006 → cd-007

## Next Step

Run `/dev-story cd-001` to begin implementation.
