# Story: sm-004 — Color Tone Calculator

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P1
> **Depends On**: ts-001 (LightPhase)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 6, §Formulas 5 (章节色调叠加)
> **Status**: Done

## Goal

HSV 三层叠加公式：章节基础色调 + 季节偏移(按 sensitivity) + 光照偏移。

## Acceptance Criteria

- [ ] AC1: ChapterTone 数据类 (H, S, V 基础值)
- [ ] AC2: SeasonOffset 按 sensitivity 等级 (none/low/medium/high) 计算偏移量
- [ ] AC3: LightOffset 按光照分类 (Dawn/Day/Dusk/Night) 计算 S/V 偏移
- [ ] AC4: CalcFinalTone 三层叠加 + S/V 钳位 [0,1]
- [ ] AC5: 终幕色调根据心境区间选取 5 种方案
- [ ] AC6: night_darken 参数正确影响夜间 V 偏移

## Test Evidence Path

`tests/Foundation/SceneManagement/ColorToneTests.cs`
