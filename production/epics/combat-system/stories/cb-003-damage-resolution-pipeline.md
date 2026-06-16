# Story: cb-003 — 伤害结算管线（F1-F4）

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P0 — 核心结算
> **Estimate**: M（约 4-6h）
> **依赖**: cb-002, Sprint 1 (ma-001 克制矩阵, ma-003 MoveDamageCalculator)
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Formulas F1-F4, §Core Rules 4
> **TR-ID**: TR-combat-system-003
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现完整伤害结算管线：有效攻击力 → 基础伤害 → 波动 → 最终伤害（含克制/暴击倍率）。

## 范围

### 包含
- DamageResolutionPipeline 纯函数
- F1: effective_attack = attack_for_type × base_multiplier × completion × realm_scaling
- F2: base_damage = max(1, effective_attack - defense)
- F3: actual_damage = base_damage × random(0.95, 1.05)
- F4: final_damage = actual_damage × counter_multiplier × crit_multiplier
- 克制倍率读取（复用 Sprint 1 CounterMatrix）
- 暴击判定（暴击率 → bool → ×1.5）
- 伤害波动使用可注入的随机源（测试确定性）

### 不包含
- 破绽变化 → cb-004
- 特殊效果结算 → cb-005 (复用 Sprint 1 SpecialEffectResolver)

## 技术说明

- 随机源通过接口注入，测试时使用固定种子
- 复用 Sprint 1 的 MoveDamageCalculator 计算 effective_attack 部分
- min_damage = 1 硬编码

## 验收标准

- [x] effective_attack 计算正确（复用 ma-003 已验证公式）
- [x] base_damage 不低于 1（防御高于攻击时兜底）
- [x] 伤害波动在 ±5% 范围内
- [x] 克制命中时 final_damage = actual × 1.3
- [x] 暴击时 final_damage 额外 × 1.5
- [x] 所有组合（克制+暴击、被克+暴击、同系+未暴击等）结果正确

## 测试证据路径

`tests/unit/combat/damage_resolution_pipeline_test.cs`

## 依赖关系

- Depends on: cb-002, ma-001, ma-003
- Unlocks: cb-004, cb-006, cb-009

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

