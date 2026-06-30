# pc-005: 灭门后回庄搜证路径

> **Epic**: 序章内容生产
> **Status**: Complete
> **Last Updated**: 2026-06-30
> **Type**: Config/Data
> **Layer**: Content / Feature Integration
> **Estimate**: 0.75 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/main-narrative.md` 灭门事件；序章：风止
> **TR-IDs**: TR-main-narrative-002, TR-exploration-insight-005, TR-dialogue-system-008
> **ADR**: ADR-0018: Exploration / Insight; ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture
> **Manifest Version**: 2026-06-30
> **Depends On**: pc-001, pc-004

## Context

本 story 制作 P00-08 和 P00-09：主角从崖洞醒来，感到异常安静，回庄后通过探索和搜证确认灭门。此段不是战斗关，不能出现可击败敌人或追杀战。

玩家应通过自己的移动和调查确认事实，而不是仅观看过场动画。

## Scope

**In scope:**

- 配置“异常安静，回庄”的低速探索/独白内容。
- 配置灭门后山庄最小搜证路径。
- 添加或记录关键线索：无人拔剑、师父倒在庄训前、“风止”二字被血划过、书房暗格空、半封血书、师姐不在尸体中。
- 设置或记录 `prologue_massacre_discovered`、`prologue_blood_letter_obtained`、`prologue_sister_missing_known`。
- 写入搜证 evidence。

**Out of scope:**

- 大型灭门演出 CG。
- 追杀战、逃亡战或任何可打赢敌人的战斗。
- 直接揭示凶手身份。
- 师兄回山段。

## Acceptance Criteria

- AC-1: 回庄前有异常安静的过渡内容：虫鸣消失、风声不动、山门敞开或等价信号。
- AC-2: 玩家能主动调查并确认至少 5 个关键线索。
- AC-3: 必须包含半封血书文本：“风起渊底，鹤归无枝。”
- AC-4: 必须明确“师姐不在尸体中”，但不解释她被掳、逃脱或主动离开。
- AC-5: 必须明确书房暗格空了，但不出现暗令、令牌、门派徽记或可直接指认凶手的信物。
- AC-6: 灭门搜证段不触发战斗，不出现可被玩家击败的敌人。
- AC-7: 完成后能设置或记录 `prologue_massacre_discovered`、`prologue_blood_letter_obtained`、`prologue_sister_missing_known`。

## Implementation Notes

- 语气应以混乱、否认、寻找师姐、无法理解为主，不写成熟复仇宣言。
- 搜证可以先做最小路径：山门/练武场 → 正堂 → 书房。
- 关键线索可用 interactable、InsightNode、dialogue narration 或场景 trigger 表达，但需 evidence 清楚记录。
- 搜证顺序允许轻微自由，但血书和师姐失踪应是推进到师兄回山的必要事实。

## Files to Create/Modify

**Likely create or modify:**

- chapter_00 narrative data from pc-001
- `feng-zhi/assets/data/dialogues/chapter_00/massacre_return_*.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/massacre_evidence_*.yaml`
- future destroyed manor scene / placeholder scene script if available
- `production/qa/evidence/pc-005-massacre-evidence-path-evidence.md`

## QA Test Cases

1. **异常安静**: 试玩或检查内容，确认回庄前有无声/空场过渡。
2. **线索覆盖**: 检查至少 5 个关键线索可被玩家主动发现。
3. **血书准确性**: 确认血书八字完全一致，不增添直接凶手信息。
4. **师姐悬念**: 确认文本只表达“不在尸体中”，不提前解释去向。
5. **无直接指凶**: 搜索内容，不出现凶手门派徽记、暗令或令牌。
6. **无战斗**: 试玩路径或配置检查，确认灭门搜证不触发 combat。

## Test Evidence

- Config/Data: `production/qa/evidence/pc-005-massacre-evidence-path-evidence.md`

## Dependencies

- Depends on: pc-001, pc-004
- Unlocks: pc-006

## Completion Notes

**Completed**: 2026-06-30  
**Criteria**: 7/7 passing.  
**Deviations**: None.  
**Test Evidence**: `tests/unit/narrative/prologue_massacre_evidence_dialogue_test.cs`; `production/qa/evidence/pc-005-massacre-evidence-path-evidence.md`.  
**Code Review**: Skipped in lean mode; content/config closure verified by automated tests.  
**Effort**: estimate 6.00 h / actual approx. 0.75 h (batch implementation and closure; variance -88%).
