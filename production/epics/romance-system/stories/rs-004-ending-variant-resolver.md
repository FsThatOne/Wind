# Story rs-004: 结局变体解析器

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-13

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-004`

Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.

**ADR Governing Implementation**: ADR-0015: Romance System
**ADR Decision Summary**: `EndingResolver` 负责读取心境区域、善恶值和 `bonded_heroine`，先处理 morality ≤ -30 的魔道 override，否则输出 zone × companion/farewell/solo × narrator tone 的稳定结局变体。

**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: 纯逻辑解析；无 Godot-specific API。

**Control Manifest Rules (Feature Layer)**:
- Required: 结局选取 moralityTier ≤ -30 走魔道
- Required: 非魔道结局按心境 zone × {companion/farewell/solo} × narrator_tone 输出
- Required: 结缘互斥保证 `bonded_heroine` 全局唯一
- Forbidden: Never 允许同时与多位女主结缘
- Forbidden: Never 暴露任何数值给玩家

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [x] AC5: 魔道结局 override 不受 `bonded_heroine` 影响
- [x] `morality <= -30` 且已结缘 A 时必须返回 `ENDING_6_DEMONIC`
- [x] 非魔道且未结缘时返回 solo 变体
- [x] 非魔道且已结缘时按心境兼容性返回 companion 或 farewell 变体

---

## Implementation Notes

*Derived from ADR-0015 Implementation Guidelines:*

Implement `EndingResolver.Resolve(mindsetZone, moralityTier, bondedHeroine)` as a deterministic pure function where the first branch checks demonic override. Do not let bond state alter `ENDING_6_DEMONIC`.

Compatibility data may initially use a configuration table or injected lookup with fallback behavior. If OQ1 compatibility mappings are incomplete, fallback must be explicit and testable; do not silently produce companion variants without configuration.

Narrator tone derives from morality independently and must not affect branch identity.

---

## Out of Scope

- rs-003: setting `bonded_heroine`
- Main narrative ending script playback
- Cutscene selection or Presentation-layer ending UI

---

## QA Test Cases

- **AC-1**: 魔道结局 override 不受 `bonded_heroine` 影响
  - Given: `moralityTier = -30` and `bonded_heroine = A`
  - When: `EndingResolver.Resolve(...)` runs
  - Then: result script or key is `ENDING_6_DEMONIC`
  - Edge cases: morality below -30 also returns demonic; no bonded heroine also returns demonic

- **AC-2**: 非魔道未结缘返回 solo
  - Given: non-demonic morality and `bonded_heroine = None`
  - When: resolver runs for any valid mindset zone
  - Then: result variant is `solo`
  - Edge cases: narrator tone may vary by morality but variant remains solo

- **AC-3**: 非魔道已结缘按兼容性分 companion/farewell
  - Given: non-demonic morality and `bonded_heroine = A`
  - When: resolver runs with a compatible zone
  - Then: result variant is `companion`
  - Edge cases: incompatible zone returns `farewell`; missing compatibility config falls back deterministically

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: `tests/integration/romance/ending_variant_resolver_test.cs` — must exist and pass

**Status**: [x] Created and passing

---

## Dependencies

- Depends on: rs-002, rs-003
- Unlocks: rs-006, rs-007

## Completion Notes

**Completed**: 2026-06-13
**Criteria**: 4/4 passing
**Deviations**: None
**Test Evidence**: Integration: `tests/integration/romance/ending_variant_resolver_test.cs`
**Code Review**: Complete — CHANGES REQUIRED -> fixed 9-zone to 5-core-script normalization and 16-variant coverage -> APPROVED
**Verification**: `EndingVariantResolverTest` 13/13 passed; Romance filtered suite 59/59 passed; Foundation full suite 1164/1164 passed
