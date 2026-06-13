# Story rs-001: 感情状态与里程碑地板钳位

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-13

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-001`

Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.

**ADR Governing Implementation**: ADR-0015: Romance System; ADR-0001: Event Bus Architecture
**ADR Decision Summary**: 感情数据寄存于 NPC State，`RomanceService` 只拥有规则；所有态度变化必须经 `OnAttitudeChangeRequest` 拦截器应用里程碑地板钳位，并通过 EventBus 通知跨层消费者。

**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: Romance 核心为纯逻辑；需验证 NPC State 可在对话进行中排队态度变更。No performance impact expected — pure rule evaluation, no frame loop or rendering path involved.

**Control Manifest Rules (Feature Layer)**:
- Required: 感情数据必须寄存于 NPC State，Romance Service 仅拥有规则
- Required: 所有态度变更必须经 `OnAttitudeChangeRequest` 拦截器应用地板钳位
- Required: 地板表为 M_BREAK=-4 / M_BOND=3 / M_HEART=2 / M_CRISIS=2 / M_TRUST=1 / M_ACQUAINTED=0 / 无里程碑=-4
- Required: 被地板吞掉的 delta 必须记录审计日志
- Forbidden: Never 态度与里程碑合一为单一进度条
- Forbidden: Never Romance Service 拥有数据

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [ ] AC1: 态度变化被里程碑地板正确钳位
- [ ] 设置 M_TRUST 后尝试将态度降至 0 以下，结果必须保持为 +1
- [ ] 普通负面 delta 被地板吞掉时必须记录调试/审计信息
- [ ] Romance 模块不得成为感情数据 owner；状态读取/写入必须经 NPC State 契约

---

## Implementation Notes

*Derived from ADR-0015 Implementation Guidelines:*

Implement `RomanceService` as the rules entry point and `MilestoneRegistry.GetFloor(npcId)` as the floor calculator. NPC State remains the storage owner for attitude and `RomanceData`.

`RomanceService` must subscribe to attitude change requests, calculate `proposed = current + delta`, clamp with `Math.Max(proposed, floor)`, write the final attitude through NPC State, and log swallowed negative delta when `clamped != proposed`.

Use typed EventBus events for cross-layer notifications. Do not introduce static C# events or string-based event names.

---

## Out of Scope

- rs-002: milestone unlock sequencing and `force_break`
- rs-003: bond flow, exclusivity, and declined-node lockout
- rs-006: save/load continuation
- rs-007: UI-facing literary descriptions

---

## QA Test Cases

- **AC-1**: 态度变化被里程碑地板正确钳位
  - Given: NPC `heroine_a` has `M_TRUST` and current attitude `+1`
  - When: an attitude change request applies delta `-3`
  - Then: final attitude remains `+1`
  - Edge cases: no milestone floor clamps to `-4`; M_HEART and M_CRISIS both floor to `+2`; M_BOND floors to `+3`

- **AC-2**: 普通负面 delta 被地板吞掉时记录审计信息
  - Given: NPC has `M_TRUST` and a negative delta would fall below floor
  - When: the attitude request is processed
  - Then: the logger receives a record containing npc id, original delta, proposed value, and clamped value
  - Edge cases: no log should be emitted when proposed value is already above floor

- **AC-3**: Romance 不拥有感情数据
  - Given: a fake NPC State service owns `RomanceData`
  - When: floor calculation and attitude clamp run
  - Then: reads and writes go through NPC State contract only
  - Edge cases: missing NPC data returns a structured failure or explicit default, not silent in-memory ownership inside Romance

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `tests/unit/romance/romance_state_and_milestone_floor_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Foundation/Core NPC State, EventBus, and prior `npc-state` completion
- Unlocks: rs-002, rs-003, rs-006

## Completion Notes

**Completed**: 2026-06-13
**Criteria**: 4/4 passing
**Deviations**: None
**Test Evidence**: Logic test at `tests/unit/romance/romance_state_and_milestone_floor_test.cs`
**Code Review**: Complete — first pass CHANGES REQUIRED; fixes applied; re-review APPROVED
**Validation**:
- `RomanceStateAndMilestoneFloorTest` passed: 15/15
- Foundation full suite passed: 1120/1120
- `GetDiagnostics` reported no errors
