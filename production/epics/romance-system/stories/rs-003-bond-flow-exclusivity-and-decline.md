# Story rs-003: 结缘流程互斥与拒绝锁定

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-13

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-003`

Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.

**ADR Governing Implementation**: ADR-0015: Romance System; ADR-0001: Event Bus Architecture
**ADR Decision Summary**: 结缘必须全局互斥，且采用 TryBond → AwaitingChoice → ConfirmBond 三步流程；拒绝结缘必须设置 `romance_bond_declined_{npcId}` flag 防止同一节点重复触发。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯逻辑 + 跨系统事件；不依赖 post-cutoff Godot API。

**Control Manifest Rules (Feature Layer)**:
- Required: `bonded_heroine` 全局唯一
- Required: 结缘流程必须三步 TryBond → AwaitingChoice → ConfirmBond
- Required: 拒绝结缘必须设 `romance_bond_declined_{npcId}` flag 防重触发
- Required: Romance Flag 前缀必须为 `romance_`
- Forbidden: Never 允许同时与多位女主结缘
- Forbidden: Never 暴露任何数值给玩家

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [x] AC3: 结缘互斥：已结缘后第二位女主节点走 "时机已过" / `already_bonded` 变体
- [x] AC9: 拒绝结缘后节点不再重复触发
- [x] `TryBond` 在条件满足时只进入 AwaitingChoice，不直接确认结缘
- [x] `ConfirmBond` 后设置 M_BOND 和全局 `bonded_heroine`

---

## Implementation Notes

*Derived from ADR-0015 Implementation Guidelines:*

Implement `TryBond(npcId)` as a pure eligibility gate that returns `AwaitingChoice`, `AlreadyBonded`, `DeclinedPreviously`, or `ConditionsNotMet`. It must not mutate `bonded_heroine` until `ConfirmBond(npcId)`.

Implement `ConfirmBond(npcId)` to set M_BOND for that NPC, set global `bonded_heroine`, publish `BondConfirmedEvent`, and lock other bond nodes through narrative-facing result values rather than direct narrative mutation.

Implement `DeclineBond(npcId)` to set the stable flag `romance_bond_declined_{npcId}` and publish `BondDeclinedEvent`. Flags must use the `romance_` prefix because flag names enter save data.

---

## Out of Scope

- rs-002: lower-level M_BOND predecessor checks
- rs-004: final ending variant resolution
- Presentation of the bond choice cutscene or dialogue UI

---

## QA Test Cases

- **AC-1**: 已结缘后第二位女主节点走 `already_bonded`
  - Given: heroine A has confirmed bond and `bonded_heroine = A`
  - When: `TryBond(heroine_b)` or `EnterBondNode(heroine_b)` is called
  - Then: result is `AlreadyBonded` and narrative variant key is `already_bonded`
  - Edge cases: second heroine satisfying all personal prerequisites still cannot bond

- **AC-2**: 拒绝结缘后节点不再重复触发
  - Given: heroine A is eligible for bond
  - When: player declines and enters the same bond node again
  - Then: result skips bond dialogue and does not return `AwaitingChoice`
  - Edge cases: decline flag persists across save/load through the flag system

- **AC-3**: `TryBond` 不直接确认结缘
  - Given: heroine A meets M_HEART, attitude threshold, and no existing bond
  - When: `TryBond(heroine_a)` is called
  - Then: result is `AwaitingChoice`, M_BOND remains false, and `bonded_heroine` remains None
  - Edge cases: `ConfirmBond` must be required to mutate global bond state

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: `tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs` — must exist and pass

**Status**: [x] Created and passing

---

## Dependencies

- Depends on: rs-001, rs-002
- Unlocks: rs-004, rs-006, rs-007

## Completion Notes

**Completed**: 2026-06-13
**Criteria**: 4/4 passing
**Deviations**: None. Scope note: added `INpcStateManager.GetAll()` as a read-only support query so `NpcStateRomancePort` can resolve the NPC-owned global `romance_bonded_heroine` flag.
**Test Evidence**: Integration: `tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs`
**Code Review**: Complete — CHANGES REQUIRED -> fixed global bond state ownership, atomic `ConfirmBond`, decline gating, and event coverage -> APPROVED
**Verification**: `Romance` filtered suite 46/46 passed; Foundation full suite 1151/1151 passed; `GetDiagnostics` clean; `git diff --check` clean
