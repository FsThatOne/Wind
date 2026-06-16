# Story: cb-006 — 反制机制

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P1 — 核心策略深度
> **Estimate**: S（约 2-3h）
> **依赖**: cb-003, cb-004, cb-005
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 5, §Formulas F7
> **TR-ID**: TR-combat-system-006
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现反制的完整判定逻辑：消耗验证、克制检查、额外破绽和反制失败降级。

## 范围

### 包含
- 反制前置条件验证：内息 >= 3、选择的招式体系克制目标意图体系
- 反制成功效果：伤害×1.3 + 目标破绽+2 + 额外+1（总计+3）
- 反制失败（选错体系）：降级为普通攻击
- 每回合每角色只能反制一个目标

### 不包含
- AI 反应反制策略 → enemy-ai
- 反制 UI 按钮交互 → combat-ui

## 技术说明

- 反制在结算时检查：攻方招式体系 是否克制 目标意图体系
- 若不克制，降级为 action_basic 结算

## 验收标准

- [x] 反制消耗 3 内息
- [x] 反制成功：伤害×1.3 + 目标破绽总计 +3
- [x] 反制失败（体系不克制）：降级为普通攻击
- [x] 内息 < 3 时反制不可执行
- [x] 每回合同一角色不能反制两个目标

## 测试证据路径

`tests/unit/combat/counter_mechanism_test.cs`

## 依赖关系

- Depends on: cb-003, cb-004, cb-005
- Unlocks: cb-009

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

