# Story: ns-005 — Delegate & Journey Formulas

> **Epic**: npc-state
> **Type**: Logic
> **Priority**: P1 — 代办和旅程是"活江湖"的数值基础
> **Depends On**: ns-002
> **GDD Source**: design/gdd/npc-state.md §Formulas F2-F5
> **Status**: Done

## Goal

实现代办资格(F2)、旅程触发(F3)、求援概率(F4)、回信指点加成(F5) 四个纯函数公式。

## Acceptance Criteria

- [ ] **AC1**: `DelegateFormula.IsEligible(...)` 正确实现 F2 全部 5 条件 AND 逻辑
- [ ] **AC2**: `JourneyFormula.IsTriggered(...)` 正确实现 F3（chapter OR day）AND NOT already_triggered
- [ ] **AC3**: `HelpRequestFormula.Calculate(base, abilityGap, attitudeMod)` 返回 clamp(0,100)
- [ ] **AC4**: `GuidedDelegateFormula.Calculate(delegatePower, guidanceBonus)` 正确求和
- [ ] **AC5**: GDD 示例验证：base=20, gap=30, attitude=20 → 70
- [ ] **AC6**: help_request_allowed=false 时 base=0，结果始终为 0

## Test Evidence Path

`tests/Foundation/NpcState/DelegateJourneyFormulaTests.cs`
