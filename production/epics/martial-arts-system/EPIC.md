# Epic: 武学组合

> **Layer**: Core
> **GDD**: design/gdd/martial-arts-system.md
> **Architecture Module**: `Core/MartialArts/`
> **Status**: Ready
> **Stories**: 8 stories (ma-001 ~ ma-008)

## Overview

武学组合是《风止》的战斗工具箱构建系统，负责招式、心法、轻功、完整度、境界缩放、特殊条件、特殊效果和战前装备槽的核心数据契约。它按照 `Core/MartialArts/` 模块边界提供刚柔巧招式定义、克制矩阵、冷却规则、完整度成长和角色可装备武学查询，供战斗系统结算使用；它不直接执行战斗回合，也不管理角色基础属性。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: Data Configuration Format | 武学招式与克制矩阵使用 YAML 1.2 + YamlDotNet 配置加载 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 所有可玩角色拥有相同招式槽、心法槽和轻功槽结构 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 6 个招式装备槽、1 个心法槽、1 个轻功槽 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 招式字段：名称、体系、类别、内息消耗、基础倍率、触发条件、特殊效果、类型标签 | ADR-0003 ✅ / ⚠️ 字段 schema 需 stories 验证 |
| 招式强度由完整度 × 境界缩放 × 触发价值决定 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 普通 / 高级 / 绝学的成长路径、残卷、拓本、完本和高手批注版本 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 刚 / 柔 / 巧克制矩阵供战斗系统查询 | ADR-0003 ✅ |
| 所有招式至少 1 回合冷却，再动不能重复同一招式 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 轻功作为独立武学类和装备位，影响战棋可移动范围或位移招式 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |

## Trace Notes

`docs/architecture/traceability-index.md` 将 `martial-arts-system.md` 标为 `⚠️ Partial`：ADR-0003 只覆盖数据格式与加载方式，不覆盖武学成长、冷却、装备槽、特殊效果执行语义和轻功规则。创建 stories 时需要补齐真实 TR-ID，并优先产出可供 `Core/Combat/` 调用的稳定接口。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/martial-arts-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 招式、心法、轻功和克制矩阵 YAML 可加载、校验、查询，并能给出清晰错误信息
- 完整度、境界缩放、特殊条件、特殊效果、冷却和装备槽规则有自动化测试覆盖
- 战斗系统可通过稳定接口读取角色当前可用招式、招式体系、内息消耗、冷却状态和特殊效果

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| ma-001 | 武学 YAML 数据模型与克制矩阵加载 | Config/Data | P0 | — | Ready |
| ma-002 | 招式成长路径、残卷合成与批注版替换 | Logic | P0 | ma-001 | Ready |
| ma-003 | 招式伤害、完整度、境界缩放与批注倍率公式 | Logic | P0 | ma-001, ma-002 | Ready |
| ma-004 | 特殊触发条件与特殊效果解析契约 | Logic | P0 | ma-001, ma-002 | Ready |
| ma-005 | 角色武学配置槽、体系覆盖警告与同伴锁定 | Integration | P0 | ma-001, ma-002 | Ready |
| ma-006 | 心法装备门槛、被动加成与专属招式 | Integration | P1 | ma-001, ma-003, ma-005 | Ready |
| ma-007 | 轻功装备位与战棋移动契约 | Integration | P1 | ma-001, ma-005 | Ready |
| ma-008 | 武学管理与战斗招式面板呈现规则 | UI | P1 | ma-001, ma-002, ma-004, ma-005, ma-006, ma-007 | Ready |

## Next Step

Run `/story-readiness production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md` to validate the first story before implementation.
