# Story rs-002: 里程碑解锁顺序与诀别覆写

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-13

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-002`

Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.

**ADR Governing Implementation**: ADR-0015: Romance System
**ADR Decision Summary**: `MilestoneRegistry` 管理里程碑顺序、门槛和 `force_break`；里程碑按 Acquainted → Trust → Crisis → Heart → Bond 解锁，`force_break` 是唯一可无视地板与顺序的覆写操作。

**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑；需验证 `force_break` 在任何里程碑组合下覆写正确。

**Control Manifest Rules (Feature Layer)**:
- Required: 里程碑顺序为 Acquainted→Trust→Crisis→Heart→Bond，不可跳级
- Required: 解锁门槛为 Trust≥1 / Crisis≥1 / Heart≥2 / Bond≥2
- Required: `force_break` 必须无视地板，直接 M_BREAK + attitude=-4
- Forbidden: Never 态度与里程碑合一为单一进度条
- Guardrail: Romance 核心逻辑必须保持可单元测试，不依赖 Godot 场景树

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [ ] AC2: `force_break()` 无视地板，态度设为 -4
- [ ] AC4: 里程碑不可跳级
- [ ] 未设 M_TRUST 时尝试解锁 M_CRISIS 必须失败
- [ ] `force_break()` 后 M_BREAK 为 true，且该 NPC 后续不再参与普通感情判定

---

## Implementation Notes

*Derived from ADR-0015 Implementation Guidelines:*

Implement `MilestoneRegistry.CanUnlock(npcId, milestone)` with explicit predecessor checks and attitude thresholds. `M_ACQUAINTED` is always unlockable; `M_BREAK` is only set by the force path and should not be treated as a normal progression milestone.

Implement `ForceBreak(npcId)` so it writes `Broken = true` into NPC State romance data and forces attitude to `SwordDrawn` / `-4`. If positive milestone unlock and `force_break` are requested in the same resolution window, `force_break` wins.

---

## Out of Scope

- rs-001: floor clamp behavior for normal attitude changes
- rs-003: `M_BOND` player choice and global bond exclusivity
- rs-004: ending variant selection

---

## QA Test Cases

- **AC-1**: `force_break()` 无视地板，态度设为 -4
  - Given: NPC has `M_BOND` and attitude `+3`
  - When: `force_break(npcId)` is called
  - Then: NPC romance data has `M_BREAK = true` and attitude equals `-4`
  - Edge cases: force break from no milestone, M_TRUST, M_HEART, and M_BOND all produce the same terminal state

- **AC-2**: 里程碑不可跳级
  - Given: NPC has `M_ACQUAINTED` but not `M_TRUST`
  - When: `CanUnlock(M_CRISIS)` is evaluated
  - Then: it returns false
  - Edge cases: `M_HEART` requires `M_CRISIS` and attitude `+2`; `M_BOND` requires `M_HEART` and attitude `+2`

- **AC-3**: `force_break` 优先级最高
  - Given: a positive milestone unlock and force break are both requested for the same NPC
  - When: both are processed in the same transaction or ordered queue
  - Then: final state remains `M_BREAK = true`, attitude `-4`
  - Edge cases: no later normal milestone unlock can clear `M_BREAK`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `tests/unit/romance/milestone_unlock_and_force_break_test.cs` — must exist and pass

**Status**: [x] Complete — `MilestoneUnlockAndForceBreakTest` passed

---

## Dependencies

- Depends on: rs-001
- Unlocks: rs-003, rs-004

## Completion Notes

**Completed**: 2026-06-13
**Criteria**: 4/4 passing
**Deviations**: None
**Test Evidence**: Logic: `tests/unit/romance/milestone_unlock_and_force_break_test.cs`
**Code Review**: Complete — CHANGES REQUIRED -> fixed `force_break` partial-write risk -> APPROVED
**Verification**: `MilestoneUnlockAndForceBreakTest` 21/21 passed; Foundation full suite 1141/1141 passed
