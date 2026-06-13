# Epic: 顿悟突破

> **Layer**: Feature
> **GDD**: design/gdd/epiphany-breakthrough.md
> **Architecture Module**: `Feature/Epiphany/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories epiphany-breakthrough`

## Overview

顿悟突破实现战斗绝境、叙事脚本和呼吸期冥想三条路径的成长跃迁框架。它管理顿悟事件生命周期、凝神风险抉择、章节配额、跳过衰减、奖励派发和境界突破串联演出，同时保持概率与奖励预览对玩家朦胧化。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0017: Epiphany Breakthrough | 采用 EpiphanyRegistry + EpiphanyEvaluator + FocusingController + RewardDispatcher | LOW |
| ADR-0013: Cutscene System | 顿悟与境界突破演出通过 cutscene chain 串联 | MEDIUM |
| ADR-0014: Living Jianghu Layer | 复用 ConditionEvaluator 评估前置条件 | LOW |
| ADR-0008: Finite State Machine | 顿悟事件和凝神状态使用 FSM 约束生命周期 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 战斗绝境每回合概率检查且同战最多一次 | ADR-0017 ✅ |
| 凝神顿悟持续自然行动轮且限制行动 | ADR-0017 ✅ |
| 稳妥取胜发放即时奖励并回到战斗决策 | ADR-0017 ✅ |
| 属性、招式、叙事选项和境界突破奖励正确分派 | ADR-0017 ✅ |
| 章节顿悟次数受 chapter_caps 限制 | ADR-0017 ✅ |
| 叙事脚本顿悟不走抉择窗口且不受配额限制 | ADR-0017 ✅ |
| 冥想天数累计且满足条件时 100% 触发 | ADR-0017 ✅ |
| skip_count / skip_limit / failed 状态正确 | ADR-0017 ✅ |
| 存档恢复所有顿悟状态、skip_count、配额和冥想天数 | ADR-0017 + ADR-0004 ✅ |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-epiphany-*` 条目。创建 stories 时应从 `epiphany-breakthrough.md` AC-1 至 AC-9 生成 story trace，并把战斗触发、冥想触发和奖励派发拆成可独立验证的 Logic/Integration stories。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/epiphany-breakthrough.md` are verified
- Combat chance, focusing, skip handling, chapter cap, meditation, reward dispatch and save restore have automated tests
- UI-facing decision text does not expose hidden chance, future reward table or exact optimization hints
- Cutscene chain integration does not duplicate gameplay effects when skipped

## Next Step

Run `/create-stories epiphany-breakthrough` to break this epic into implementable stories.
