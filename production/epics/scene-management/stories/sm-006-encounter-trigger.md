# Story: sm-006 — Encounter Trigger

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P2
> **Depends On**: sm-003 (Weather)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 7d, §Formulas 6 (奇遇触发判定)
> **Status**: Done

## Goal

奇遇触发纯逻辑：条件检查 + 优先级排序 + 概率判定 + consumed 标记。

## Acceptance Criteria

- [ ] AC1: EncounterCondition 包含 season/weather/shichen/progress/mindset 条件
- [ ] AC2: 多个奇遇按 priority 排序，取第一个满足条件的
- [ ] AC3: 满足条件后按 trigger_chance 概率判定
- [ ] AC4: repeatable=false 触发后标记 consumed，不再参与
- [ ] AC5: 所有条件为空 = 无条件满足
- [ ] AC6: 无满足条件奇遇时返回 null

## Test Evidence Path

`tests/Foundation/SceneManagement/EncounterTriggerTests.cs`
