# Story rs-005: 彗星存在感与传闻概率

> **Epic**: 感情系统（彗星模型）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-06-10
> **Last Updated**: —

## Context

**GDD**: `design/gdd/romance-system.md`
**Requirement**: `TR-romance-system-???`

`docs/architecture/tr-registry.yaml` 尚无 `TR-romance-*` 条目；本 story 临时追踪 GDD AC6，补齐 registry 后需替换为稳定 TR-ID。

**ADR Governing Implementation**: ADR-0015: Romance System; ADR-0003: Data Configuration Format
**ADR Decision Summary**: `CometPresenceTracker` 追踪暗号、书信、传闻和偶遇计数，并按 `0.3 + same_region 0.4 + absence>7 0.2` 计算传闻概率，结果 Clamp 到 `[0, 0.8]`。调参数据遵循 YAML 配置规范。

**Engine**: Godot 4.6.3 | **Risk**: LOW
**Engine Notes**: 纯逻辑概率计算；无 post-cutoff API。

**Control Manifest Rules (Feature Layer)**:
- Required: 传闻概率为 `base(0.3) + same_region(+0.4) + absence>7天(+0.2)`，Clamp(0, 0.8)
- Required: Romance Flag 前缀必须为 `romance_`
- Required: Flag 一旦命名进入存档不可重命名
- Required: 静态配置使用 YAML 1.2，路径 `assets/data/{domain}/{table}.yaml`
- Forbidden: Never 暴露任何数值给玩家

---

## Acceptance Criteria

*From GDD `design/gdd/romance-system.md`, scoped to this story:*

- [ ] AC6: 传闻触发概率正确计算且不超上限
- [ ] 所有加成叠满时传闻概率必须 `<= 0.8`
- [ ] 暗号/书信/传闻/偶遇计数写回 NPC State 的 RomanceData
- [ ] 暗号和书信触发必须更新 `romance_last_contact_{npcId}` 供后续续算

---

## Implementation Notes

*Derived from ADR-0015 Implementation Guidelines:*

Implement `CometPresenceTracker.CalcRumorChance(npcId, region)` with injected world day, journey region, and last-contact flag query seams for unit testing.

Use configurable tuning values for `rumor_base_chance`, `rumor_same_region_bonus`, `rumor_absence_bonus`, `rumor_absence_threshold_days`, and `rumor_cap`. If YAML is introduced, follow ADR-0003: UTF-8 BOM-free `.yaml`, loaded once into DataRegistry, runtime read-only.

Comet presence tracking updates counters only; concrete presentation remains in Living Jianghu, Dialogue, UI, or Cutscene systems.

---

## Out of Scope

- Living Jianghu event scheduling and delivery
- Dialogue letter UI and unread reminders
- Scene-level sign placement
- rs-006 save/load round-trip

---

## QA Test Cases

- **AC-1**: 传闻触发概率正确计算且不超上限
  - Given: base chance 0.3, same-region bonus 0.4, absence bonus 0.2, cap 0.8
  - When: heroine is in the same region and days since last contact is greater than 7
  - Then: calculated chance is 0.8, not 0.9
  - Edge cases: no bonuses returns 0.3; same-region only returns 0.7; absence at exactly 7 days does not trigger `> 7`

- **AC-2**: 彗星存在感计数写回 NPC State
  - Given: NPC RomanceData has zero counters
  - When: sign, letter, rumor, and encounter tracking methods are invoked
  - Then: corresponding counters increment exactly once
  - Edge cases: repeated calls increment linearly; unknown NPC fails explicitly or uses NPC State validation

- **AC-3**: last contact flag 更新
  - Given: current world day is 12
  - When: sign discovered or letter received is recorded
  - Then: `romance_last_contact_{npcId}` is set to day 12
  - Edge cases: rumor heard may increment rumor count without forcing last contact if design treats it as indirect contact

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `tests/unit/romance/comet_presence_and_rumor_chance_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: rs-001
- Unlocks: rs-006, living-jianghu integration stories
