# Epic: 感情系统（彗星模型）

> **Layer**: Feature
> **GDD**: design/gdd/romance-system.md
> **Architecture Module**: `Feature/Romance/`
> **Status**: Ready
> **Stories**: 7 stories (rs-001 ~ rs-007)

## Overview

感情系统实现女主关系的彗星模型、里程碑地板、结缘互斥、诀别覆写和结局变体查询。系统以 NPC State 作为关系数据 owner，Romance 只拥有规则逻辑，并通过活江湖、对话、主线叙事和朦胧化 UI 间接呈现关系变化。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0015: Romance System | 采用 RomanceService + MilestoneRegistry + CometPresenceTracker + EndingResolver，数据寄存于 NPC State | LOW |
| ADR-0001: EventBus | 态度变化、里程碑解锁、结缘/诀别通过事件发布 | LOW |
| ADR-0003: Data Configuration Format | 女主兼容区、传闻概率、里程碑配置走数据表 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 态度变化受里程碑地板钳位 | ADR-0015 ✅ |
| `force_break()` 无视地板并设置 M_BREAK | ADR-0015 ✅ |
| 同一周目最多结缘一人 | ADR-0015 ✅ |
| 里程碑不可跳级 | ADR-0015 ✅ |
| 魔道结局 override 不受结缘影响 | ADR-0015 ✅ |
| 传闻触发概率正确计算且不超过 0.8 | ADR-0015 ✅ |
| 存档读取后 last contact / days_since_last_contact 续算 | ADR-0015 + ADR-0004 ✅ |
| 人物图鉴仅显示文学化关系描述 | ADR-0015 + ADR-0002 ⚠️ 需 UI story 验证 |
| 拒绝结缘后节点不再重复触发 | ADR-0015 ✅ |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-romance-*` 条目。创建 stories 时应从 `romance-system.md` 的 AC1-AC9 生成 story 级追踪，并在需要正式 TR-ID 时补齐 registry。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/romance-system.md` are verified
- Milestone floor, force_break, bond exclusivity, ending variant, rumor chance and save continuation have automated tests
- UI-facing relationship outputs remain literary and do not expose numerical affection/progress
- Romance data ownership remains in NPC State; Romance module owns rules only

## Next Step

Run `/story-readiness production/epics/romance-system/stories/rs-001-romance-state-and-milestone-floor.md` before implementation.

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| rs-001 | [感情状态与里程碑地板钳位](stories/rs-001-romance-state-and-milestone-floor.md) | Logic | P0 | NPC State, EventBus | Ready |
| rs-002 | [里程碑解锁顺序与诀别覆写](stories/rs-002-milestone-unlock-and-force-break.md) | Logic | P0 | rs-001 | Ready |
| rs-003 | [结缘流程互斥与拒绝锁定](stories/rs-003-bond-flow-exclusivity-and-decline.md) | Integration | P0 | rs-001, rs-002 | Ready |
| rs-004 | [结局变体解析器](stories/rs-004-ending-variant-resolver.md) | Integration | P1 | rs-002, rs-003 | Ready |
| rs-005 | [彗星存在感与传闻概率](stories/rs-005-comet-presence-and-rumor-chance.md) | Logic | P1 | rs-001 | Ready |
| rs-006 | [感情存档与联系计时续算](stories/rs-006-romance-save-and-contact-continuation.md) | Integration | P1 | rs-001, rs-003, rs-004, rs-005 | Ready |
| rs-007 | [文学化关系展示契约](stories/rs-007-literary-relationship-presentation-contract.md) | UI | P1 | rs-001, rs-002, rs-003 | Ready |
