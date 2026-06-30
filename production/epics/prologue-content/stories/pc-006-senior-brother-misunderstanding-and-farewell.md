# pc-006: 师兄误会、共同埋葬与临别传承

> **Epic**: 序章内容生产
> **Status**: Ready with Dependency Notes
> **Type**: Integration
> **Layer**: Content / Feature Integration
> **Estimate**: 1 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/main-narrative.md` 序章尾段；`design/gdd/misunderstanding-system.md`; `design/gdd/tutorial-onboarding.md`
> **TR-IDs**: TR-main-narrative-002, TR-dialogue-system-004, TR-dialogue-system-007, TR-combat-system-001
> **ADR**: ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture; ADR-0012: Misunderstanding UI
> **Manifest Version**: 2026-06-10
> **Depends On**: pc-001, pc-005; misunderstanding-system stories pending

## Context

本 story 制作 P00-10：师兄外出采买寿宴物资后回山，看到满门皆灭、主角独活，产生误会。误会通过对话和事实澄清，不进入生死战。误会解除后，两人共同埋葬门人，师兄临别教授风止尺法基础，并与主角约定书信联系。

这是序章情感收束点，也是战斗教学和书信系统的叙事入口。

## Scope

**In scope:**

- 编写师兄采买回山的登场和误会对峙。
- 配置误会实例或等价状态 key：`mis_senior_brother_survivor_suspicion`。
- 编写澄清路径，解释主角取酒、夜宿、回庄搜证事实。
- 编写误会解除后的共同埋葬内容，并设置或记录 `senior_brother_mis_resolved`。
- 编写临别传承的战斗教学包装文本。
- 编写书信约定，并设置或记录 `senior_brother_letter_contact_unlocked`。

**Out of scope:**

- 完整误会系统底层 FSM 实现。
- 完整战斗系统或战斗 UI 新功能。
- 长期书信内容。
- 师兄长期同伴线。

## Acceptance Criteria

- AC-1: 师兄误会来源只来自“为什么只有主角活着”，不得来自主角澜国身世。
- AC-2: 师兄不知道主角身世，不能围绕澜国身份质问。
- AC-3: 误会必须通过对话澄清，不进入真正生死战。
- AC-4: 澄清内容必须引用前置事实：取酒、夜宿崖洞、回庄后发现血书/师姐失踪。
- AC-5: 误会解除后，两人共同埋葬门人；此段是情感收束点，不被信息揭露抢走重心。
- AC-6: 战斗教学发生在共同埋葬之后，叙事包装为临别传承和自保需要。
- AC-7: 分别追查时自然约定书信联系，并解锁或记录 `senior_brother_letter_contact_unlocked`。
- AC-8: 若误会系统底层尚未实现，story 必须留下明确 dependency note 和后续接入点。

## Implementation Notes

- 师兄可以痛苦、失控、质问，但不能被写成不可理喻的敌人。
- 误会 UI 若尚未接入，可先通过对话措辞、状态 key 和 evidence 表达“首次误会体验”。
- 战斗教学内容可先以叙事入口和 placeholder 节点表达；真正教程 combat config 可拆后续 story。
- 书信约定不使用系统弹窗，应在分别台词中自然发生。

## Files to Create/Modify

**Likely create or modify:**

- `feng-zhi/assets/data/dialogues/chapter_00/senior_brother_return_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/senior_brother_misunderstanding_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/joint_burial_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/farewell_inheritance_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/letter_promise_01.yaml`
- chapter_00 narrative data from pc-001
- `production/qa/evidence/pc-006-senior-brother-misunderstanding-and-farewell-evidence.md`

## QA Test Cases

1. **误会来源**: 检查师兄台词，确认怀疑点是“唯主角独活”，不是身世或澜国身份。
2. **对话澄清**: 试玩或检查分支，确认误会能通过事实解释解除。
3. **无生死战**: 确认对峙不触发 lethal combat，也不要求玩家击败师兄。
4. **埋葬优先**: 确认误会解除后进入共同埋葬内容，且该段不是一句带过。
5. **教学位置**: 确认战斗教学入口发生在共同埋葬之后。
6. **书信解锁**: 确认分别时有书信约定，并记录 `senior_brother_letter_contact_unlocked`。
7. **依赖记录**: 若误会 FSM 未接入，evidence 中必须记录后续接入点和当前替代实现。

## Test Evidence

- Integration: `production/qa/evidence/pc-006-senior-brother-misunderstanding-and-farewell-evidence.md`

## Dependencies

- Depends on: pc-001, pc-005
- Soft blocked by: misunderstanding-system implementation for full FSM/UI semantics
- Unlocks: chapter_00 vertical content pass, first combat tutorial content pass
