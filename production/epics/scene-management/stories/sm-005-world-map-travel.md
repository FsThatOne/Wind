# Story: sm-005 — World Map Travel

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P1
> **Depends On**: ts-002 (Stamina), ts-004 (TimeManager)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 7c, §Formulas 2+3 (大地图旅行+驿站)
> **Status**: Done

## Goal

大地图旅行计算：每格消耗、速度修正、驿站时间/费用公式。

## Acceptance Criteria

- [ ] AC1: CalcGridCost → time=0.01天, stamina=0.2点 (可配置)
- [ ] AC2: 速度修正: stamina>0 → 1.0, stamina=0 → 0.5 (effective time per grid doubled)
- [ ] AC3: 驿站公式: time = distance*0.01*0.5, stamina=0, fee = base + distance*rate
- [ ] AC4: 多格移动正确逐格累计
- [ ] AC5: 驿站费用不足时返回 CannotAfford 结果
- [ ] AC6: 配置参数从 TimeConfig/SceneConfig 读取

## Test Evidence Path

`tests/Foundation/SceneManagement/WorldMapTravelTests.cs`
