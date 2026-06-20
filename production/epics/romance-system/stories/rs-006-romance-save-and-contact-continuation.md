# Story rs-006: 感情存档与联系计时续算

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-14

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-006`

需求正文以 `docs/architecture/tr-registry.yaml` 为准；评审和完成检查时请读取最新 registry 内容。

**ADR Governing Implementation**: ADR-0015: Romance System; ADR-0004: Save Encryption & Persistence Strategy
**ADR Decision Summary**: 大部分感情数据随 NPC State 持久化，RomanceService 只保存极简全局状态如 `bonded_heroine`；`days_since_last_contact` 通过 flag day 与当前世界日续算，不在读取后重置。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 使用 .NET 标准持久化契约；不依赖 Godot-specific API。

**Control Manifest Rules (Feature Layer)**:
- Required: 感情数据归属 NPC State，Romance Service 仅拥有规则
- Required: 周目隔离，感情数据不跨周目继承
- Required: Romance Flag 前缀必须为 `romance_`

**Control Manifest Rules (Platform Layer)**:
- Required: 系统必须实现 `ISaveable`（SaveKey / Serialize / Deserialize）或通过 owner 系统参与保存
- Required: 存档只持久化运行时状态，不持久化 Script 定义
- Forbidden: Never 跳版本迁移

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [ ] AC7: 存档读取后 `days_since_last_contact` 续算
- [ ] 保存/加载后 last contact day 必须恢复，当前天数推进后能计算正确间隔
- [ ] `bonded_heroine` 必须可保存和恢复
- [ ] 新周目感情数据不继承，全部重置

---

## Implementation Notes

*Derived from ADR-0015 and ADR-0004 Implementation Guidelines:*

Prefer storing heroine `RomanceData` inside NPC State snapshots. RomanceService may expose a minimal save contract only for global singleton state such as `bonded_heroine`.

Last contact should be represented by a stable `romance_last_contact_{npcId}` flag day or equivalent runtime state owned by the flag/NPC State save boundary. On load, compute `days_since_last_contact = currentDay - storedLastContactDay`; do not reset last contact to current day.

New game plus/new run initialization must explicitly clear romance runtime data unless a future GDD changes inheritance policy.

---

## Out of Scope

- Save encryption and file IO internals
- NPC State base serialization implementation
- rs-005 probability formula beyond the loaded last-contact value

---

## QA Test Cases

- **AC-1**: 存档读取后 `days_since_last_contact` 续算
  - Given: last contact flag day is 10 and save is loaded at world day 14
  - When: `GetDaysSinceLastContact(npcId)` is called
  - Then: result is 4
  - Edge cases: missing contact flag returns explicit "never contacted" behavior; future-dated flag is clamped or rejected with warning

- **AC-2**: `bonded_heroine` 保存和恢复
  - Given: `bonded_heroine = A`
  - When: romance save data is serialized and deserialized
  - Then: restored state reports `bonded_heroine = A`
  - Edge cases: invalid enum value in old/corrupt save fails safely or maps to None with warning according to save policy

- **AC-3**: 新周目感情数据不继承
  - Given: previous run has bond, milestones, comet counters, and last-contact flags
  - When: new run initialization executes
  - Then: all romance data returns to defaults
  - Edge cases: non-romance NPC State fields are not accidentally cleared

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: `tests/integration/romance/romance_save_and_contact_continuation_test.cs` — must exist and pass

**Status**: [x] Created — pending review closure

---

## Dependencies

- Depends on: rs-001, rs-003, rs-004, rs-005, save-system complete
- Unlocks: final narrative and UI relationship consumers

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 4/4 passing
**Deviations**: None blocking. Scope expanded during code review to harden NPC State immediate writes, bond uniqueness, force_break immediate semantics, and the normal milestone Bond bypass.
**Test Evidence**: Integration: `tests/integration/romance/romance_save_and_contact_continuation_test.cs`; supporting regressions in `tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs`, `tests/unit/romance/milestone_unlock_and_force_break_test.cs`, and `tests/Foundation/NpcState/NpcStateManagerTests.cs`.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS after fixes.
**Verification**: `dotnet test --filter "RomanceSaveAndContactContinuationTest|BondFlowExclusivityAndDeclineTest|MilestoneUnlockAndForceBreakTest|NpcStateManagerTests"` passed 63/63; `dotnet test` passed 1198/1198.
