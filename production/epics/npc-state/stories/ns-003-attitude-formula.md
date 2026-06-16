# Story: ns-003 — Attitude Formula

> **Epic**: npc-state
> **Type**: Logic
> **Priority**: P1 — 态度计算是对话路由的基础
> **Depends On**: ns-002
> **GDD Source**: design/gdd/npc-state.md §Formulas F1
> **Status**: Done

## Goal

实现 F1 态度分数计算公式，将 4 个修正值映射为 8 档态度。

## Acceptance Criteria

- [ ] **AC1**: `NpcAttitudeFormula.Calculate(base, mindset, morality, misunderstanding)` 返回 clamp(-4, +3) 的分数
- [ ] **AC2**: 分数正确映射为 AttitudeLevel 枚举（+3=生死相托, 0=萍水相逢, -4=拔剑相向）
- [ ] **AC3**: GDD 示例验证：base=+2, mindset=+1, morality=0, misunderstanding=0 → +3 → 生死相托
- [ ] **AC4**: 输入超出范围时钳位，不抛异常
- [ ] **AC5**: NPC 未配置心境态度表时 mindset_mod 默认 0，记录 warning（返回布尔标记）

## Test Evidence Path

`tests/Foundation/NpcState/AttitudeFormulaTests.cs`
