# Story: ms-005 — Resolution & Bounce-back

> **Epic**: misunderstanding-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1 day
> **Manifest Version**: 2026-06-30
> **GDD 来源**: design/gdd/misunderstanding-system.md §Core Rules 5, §Formulas F5, §Edge Cases E3/E7/E10
> **TR-ID**: 待 architecture-review 分配

## Context

澄清是误会系统的"正面出口"——当玩家满足 resolution_conditions 时，误会转为 RESOLVED，mod 重算归零，并给予短暂的态度弹回加成。此 story 实现条件检查、状态转换、弹回加成及其自动过期逻辑。条件满足判定通过 `IConditionEvaluator` 接口抽象。

**ADR Governing Implementation**: 通用架构
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑层。

## Acceptance Criteria

- [ ] AC-1: 当所有 `resolution_conditions` 被满足时，误会实例转为 RESOLVED 状态，`misunderstanding_mod` 重算。（GDD AC8）
- [ ] AC-2: 澄清成功后，通过 `INpcStateWriter.ApplyTemporaryBonus(npc_id, +1, duration)` 施加弹回加成（默认 +1，持续 3 天）。（GDD AC9）
- [ ] AC-3: 弹回加成到期后自动清除（bonus=0）。（GDD AC9）
- [ ] AC-4: Edge Case E3 — 同一天同时满足澄清条件和恶化事件时，澄清优先：误会转 RESOLVED，恶化事件被忽略。
- [ ] AC-5: Edge Case E7 — PERMANENT 误会在特定剧情节点解锁（`unlock_permanent_mis` 标记）时可再次解除 → RESOLVED。
- [ ] AC-6: Edge Case E10 — 玩家在 HIDDEN 阶段（未感知误会）若恰好满足 resolution_conditions，仍然结算成功。

## Implementation Notes

**Control Manifest Rules (Foundation Layer)**:
- Required: `IConditionEvaluator` 接口提供 `AreConditionsMet(List<Condition> conditions) -> bool`。本 story 用 mock 验证。
- Required: 弹回加成为临时 buff（GDD OQ4 当前决定），不计入 NPC 态度公式正式项。
- Required: 弹回过期由 day tick 驱动（复用 ms-003 的 OnDayAdvanced 机制）。
- Required: 澄清优先级高于恶化——同 tick 内先检查澄清再检查恶化。
- Forbidden: 不在此 story 实现 PERMANENT 解锁节点的注册逻辑（属于内容配置层）。

**GDD 关键公式引用**:
- F5: resolve_misunderstanding → RESOLVED + recompute_mod + apply_temporary_bonus(+1, 3_days)

**Tuning Knobs 相关**:
- `resolve_attitude_bonus` (默认 +1，范围 0-+2)
- `resolve_bonus_duration_days` (默认 3，范围 1-7)

## Files to Create/Modify

- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingResolver.cs`
- `src/FengZhi.Foundation/Misunderstanding/IConditionEvaluator.cs`
- `src/FengZhi.Foundation/Misunderstanding/ResolutionBonusTracker.cs`

## Test Evidence

**Required evidence**:
- `tests/unit/misunderstanding/MisunderstandingResolutionTest.cs`

**Test cases**:
- 创建 ACTIVE MODERATE → mock 条件满足 → 状态=RESOLVED, mod 重算=0
- 解除后 bonus=+1 → tick 3 天 → bonus=0
- 同天澄清+恶化 → 只执行澄清，severity 不变
- PERMANENT 实例 + unlock_permanent_mis 标记 → 可再次解除 → RESOLVED
- HIDDEN 阶段满足条件 → 仍结算成功
- 条件部分满足（非全部）→ 不触发解除
- bonus 已有时再次解除另一条误会 → bonus 重置计时（不叠加数值）

## Out of Scope

- resolution_conditions 的具体内容定义（属于内容/配置层 ms-009）
- 对话系统的条件传递（ms-006）
- force_break 相关逻辑（ms-003/ms-007）

## Dependencies

- Depends on: ms-001, ms-002（recompute_mod）
- Unlocks: ms-009
