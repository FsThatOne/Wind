# Story rs-007: 文学化关系展示契约

> **Epic**: 感情系统（彗星模型）
> **Status**: Complete
> **Layer**: Feature
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-14

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-007`

需求正文以 `docs/architecture/tr-registry.yaml` 为准；评审和完成检查时请读取最新 registry 内容。

**ADR Governing Implementation**: ADR-0015: Romance System; ADR-0002: UI Framework & Dual-Focus Adaptation
**ADR Decision Summary**: 感情系统绝不向玩家暴露数值，UI 只能消费文学化关系描述和回忆片段；具体面板应基于 Godot Control + `BaseUiPanel`，但本 story 只定义 Feature 层展示查询契约。

**Engine**: Godot 4.7-stable | **Risk**: HIGH
**Engine Notes**: UI 实现受 Godot 4.6 dual-focus 影响；本 story 的 Feature 查询契约为低风险，最终 UI evidence 需在 Presentation 层验证。

**Control Manifest Rules (Feature Layer)**:
- Required: Romance Service 仅拥有规则，其他系统只读取 NPC State 中态度/里程碑值
- Forbidden: Never 暴露任何数值给玩家

**Control Manifest Rules (Presentation Layer)**:
- Required: 所有 UI 面板必须继承 `BaseUiPanel`
- Required: 所有可交互 Control 必须设 `focus_mode = FOCUS_ALL`
- Required: UI 刷新必须使用脏标记模式
- Forbidden: Never 使用 hover-only 交互

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [ ] AC8: 人物图鉴正确显示文学化关系描述
- [ ] 输出契约不得包含好感度数字、进度条、攻略进度百分比或结缘状态直白提示
- [ ] 各态度档位和里程碑组合必须能映射为文学化描述 key
- [ ] 已达成里程碑可作为回忆片段查询，不暴露内部 flag 名称

---

## Implementation Notes

*Derived from ADR-0015 and ADR-0002 Implementation Guidelines:*

Provide a query contract such as `GetRelationshipDescriptor(npcId)` or `GetRelationshipPresentation(npcId)` returning localization/description keys, memory fragment keys, and tone tags. Do not return raw attitude integers, progress percentages, or internal milestone enum names to UI.

The actual character encyclopedia panel belongs to Presentation or Blurred UI work. This story should make it impossible for UI consumers to accidentally display numeric affection by exposing only literary-safe data.

If a minimal manual evidence doc is created during this story, it should verify sample outputs across attitude tiers and milestone states.

Performance: no runtime UI scene/rendering impact expected; this story only exposes a small Feature-layer query DTO containing stable localization keys, tone tags, and memory fragment ids.

---

## Out of Scope

- Full character encyclopedia UI implementation
- Godot Control scene creation
- Blurred UI shader and relation panel visual polish
- Misunderstanding cloud marker presentation

---

## QA Test Cases

- **AC-1**: 人物图鉴显示文学化关系描述
  - Setup: Configure NPC states covering陌路、相识、信任、交心、结缘、诀别
  - Verify: presentation contract returns description keys or text snippets such as "彼此已推心置腹" instead of numbers
  - Pass condition: every tested state has a non-empty literary descriptor and no raw numeric affection value

- **AC-2**: 不暴露数值或进度
  - Setup: Inspect return DTO/model and sample UI evidence
  - Verify: no fields named `affection`, `progress`, `percentage`, `score`, or raw attitude integer are exposed to Presentation consumers
  - Pass condition: UI-facing contract contains only literary keys, tone tags, and memory fragment identifiers

- **AC-3**: 回忆片段不暴露内部 flag 名称
  - Setup: NPC has `M_TRUST`, `M_CRISIS`, and `M_HEART`
  - Verify: returned memory fragments use narrative ids/descriptions, not internal boolean names
  - Pass condition: no user-facing string contains `M_TRUST`, `M_CRISIS`, `M_HEART`, or enum names

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- UI: `production/qa/evidence/rs-007-literary-relationship-presentation-contract-evidence.md` or interaction test

**Status**: [x] Created at `production/qa/evidence/rs-007-literary-relationship-presentation-contract-evidence.md`

---

## Dependencies

- Depends on: rs-001, rs-002, rs-003
- Unlocks: `blurred-ui` relationship panel stories and `misunderstanding-system` relation signal stories

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 4/4 passing
**Deviations**: None blocking. Code review suggestions accepted as non-blocking: `MemoryFragmentIds` could be made stricter-readonly, and `shared_vow` / `vowed` wording can be made more oblique in a later polish pass if desired.
**Test Evidence**: UI evidence at `production/qa/evidence/rs-007-literary-relationship-presentation-contract-evidence.md`; automated contract test at `tests/unit/romance/relationship_presentation_contract_test.cs`.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS.
**Verification**: `dotnet test --filter RelationshipPresentationContractTest` passed 16/16; `dotnet test` passed 1214/1214.
