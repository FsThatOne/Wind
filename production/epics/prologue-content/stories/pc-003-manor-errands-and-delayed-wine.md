# pc-003: 山庄寿宴跑腿与拖延取酒

> **Epic**: 序章内容生产
> **Status**: In Progress
> **Last Updated**: 2026-06-30
> **Type**: Config/Data
> **Layer**: Content / Core Integration
> **Estimate**: 0.75 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/main-narrative.md` 序章：风止
> **TR-IDs**: TR-main-narrative-002, TR-main-narrative-003, TR-dialogue-system-004
> **ADR**: ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture
> **Manifest Version**: 2026-06-10
> **Depends On**: pc-001, pc-002

## Context

本 story 制作 P00-04、P00-05 和 P00-06：山庄寿宴跑腿、庄主书房小事、师姐多次催取酒，以及主角仗着路熟拖到天黑的事实。

这段的目标不是做复杂支线，而是让玩家在灭门前熟悉山庄、记住几名门人，并亲手参与寿宴准备。

## Scope

**In scope:**

- 设计并配置 3-4 个寿宴跑腿小任务或等价主线互动。
- 编写至少 3 名山庄群像 NPC 的短对话。
- 编写庄主书房短谈与暗格伏笔。
- 编写师姐催取酒的多阶段提醒。
- 设置或记录 `prologue_wine_delayed`。

**Out of scope:**

- 大量可选支线。
- 完整山庄所有 NPC 内容。
- 灭门后场景。
- 战斗教学。

## Acceptance Criteria

- AC-1: 玩家在灭门前至少与师姐、庄主、2 名门人产生可记忆互动。
- AC-2: 寿宴跑腿内容能让玩家经过厨房/药房/练武场/正堂周边中的至少 3 个区域或等价空间。
- AC-3: 师姐至少两次提醒主角去崖洞取酒，语气温和但逐渐催促。
- AC-4: 主角拖延取酒被表达为角色性格和山庄温柔乡共同造成，不是玩家失败或倒计时惩罚。
- AC-5: 书房小事让玩家见过暗格位置，但不解释暗格内容和主角身世。
- AC-6: 入夜出发前设置或记录 `prologue_wine_delayed`。

## Implementation Notes

- “拖到天黑”不使用硬倒计时；推荐用节点推进或 NPC 对话阶段变化表达。
- 庄主台词保持温柔克制，不写成谜语式预言。
- 群像 NPC 应有生活细节：寿宴、厨房、药草、红绳、酒盏、练武场闲话。
- 现有主线文档中“陪师弟练功”在本 story 中应处理为“师弟日常互动/跑腿玩闹”，不得进入正式战斗教学。

## Files to Create/Modify

**Likely create or modify:**

- `feng-zhi/assets/data/dialogues/chapter_00/manor_errands_*.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/master_study_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/sister_wine_reminder_*.yaml`
- chapter_00 mainline content data from pc-001
- `production/qa/evidence/pc-003-manor-errands-and-delayed-wine-evidence.md`

## QA Test Cases

1. **群像记忆点**: 检查内容中至少有师姐、庄主、2 名门人的独立互动。
2. **空间走读**: 试玩或检查节点，确认玩家会经过至少 3 个山庄生活区域。
3. **催取酒递进**: 检查师姐催促至少两次，且不转为责骂或危险预言。
4. **无倒计时惩罚**: 确认拖延不导致 game over、扣资源或失败分支。
5. **暗格伏笔**: 书房内容显示暗格位置，但不揭露文书、身世或凶手。
6. **战斗排除**: 灭门前山庄跑腿内容不触发正式战斗教学。

## Test Evidence

- Config/Data: `production/qa/evidence/pc-003-manor-errands-and-delayed-wine-evidence.md`

## Dependencies

- Depends on: pc-001, pc-002
- Unlocks: pc-004
