# Story: ms-002 — Mod Calculation & NPC State Write

> **Epic**: misunderstanding-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1 day
> **Manifest Version**: 2026-06-30
> **GDD 来源**: design/gdd/misunderstanding-system.md §Formulas F1, §Core Rules 约束, §Edge Cases E1
> **TR-ID**: 待 architecture-review 分配

## Context

`misunderstanding_mod` 是误会系统对 NPC 态度公式的唯一输出通道。此 story 实现 mod 计算逻辑（取所有活跃误会的最大 severity 贡献值，钳制到 [-2, 0]）和 `max_active_per_npc` 限制。NPC 状态写入接口通过 interface mock 验证。

**ADR Governing Implementation**: 通用架构（无专属 ADR）
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑层，NPC 状态写入通过 `INpcStateWriter` 接口抽象。

## Acceptance Criteria

- [ ] AC-1: `compute_misunderstanding_mod(npc_id)` 在同 NPC 有 MINOR+MODERATE 两条活跃误会时返回 -2（取最大贡献值）。（GDD AC4, Edge Case E1）
- [ ] AC-2: mod 结果始终在 [-2, 0] 范围内，无论活跃误会数量和组合。
- [ ] AC-3: 同一 NPC 活跃误会数不超过 `max_active_per_npc`（默认 3）。超限时应拒绝注册或替换最轻的那条（OQ2 — 当前实现为拒绝注册，后续可调整）。（GDD AC11）
- [ ] AC-4: 误会状态变化（创建/恶化/解除/定型）后自动调用 `recompute_mod` 并通过 `INpcStateWriter.SetMisunderstandingMod(npc_id, value)` 写出。
- [ ] AC-5: 所有活跃实例（ACTIVE + ESCALATED + PERMANENT）均参与 mod 计算；DORMANT、RESOLVED、BROKEN 不参与。

## Implementation Notes

**Control Manifest Rules (Foundation Layer)**:
- Required: `INpcStateWriter` 接口定义 `SetMisunderstandingMod(string npcId, int value)`，本 story 仅依赖此接口，不引入 NPC State 具体实现。
- Required: mod 重算为幂等操作——调用多次结果不变。
- Required: `max_active_per_npc` 可通过 Tuning Knob 配置（GDD 默认 3，范围 1-5）。
- Forbidden: 不在此 story 实现地板保护（ms-007 负责）。

**GDD 关键公式引用**:
- F1: `max(inst.severity_to_mod() for inst in active_instances)`, clamp(-2, 0)
- severity_to_mod: MINOR→-1, MODERATE→-2, SEVERE→-2

## Files to Create/Modify

- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingModCalculator.cs`
- `src/FengZhi.Foundation/Misunderstanding/INpcStateWriter.cs`
- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingConfig.cs` (tuning knobs)

## Test Evidence

**Required evidence**:
- `tests/unit/misunderstanding/MisunderstandingModCalculatorTest.cs`

**Test cases**:
- 无活跃误会 → mod=0
- 单条 MINOR → mod=-1
- 单条 MODERATE → mod=-2
- MINOR + MODERATE 同时存在 → mod=-2（取 max 贡献）
- SEVERE → mod=-2
- 3 条活跃 + 第 4 条尝试注册 → 拒绝
- RESOLVED 实例不参与计算
- 状态变化后 mock writer 被调用且值正确

## Out of Scope

- 地板保护逻辑（ms-007）
- NPC State 系统的具体实现
- 触发源对接（ms-006）

## Dependencies

- Depends on: ms-001
- Unlocks: ms-006, ms-007
