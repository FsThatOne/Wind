# Story: sm-003 — Weather System

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P1
> **Depends On**: ts-001 (Calendar)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 5b, §Formulas 4 (天气日刷新概率)
> **Status**: Done

## Goal

天气系统纯逻辑：7 种天气枚举、概率池 + 区域过滤归一化、日刷新、剧情强制天气、持续天数。

## Acceptance Criteria

- [ ] AC1: WeatherType 枚举 7 种 (Clear/Cloudy/LightRain/HeavyRain/Snow/Fog/Sandstorm)
- [ ] AC2: 季节基础权重表 × 区域过滤掩码 → 归一化概率池
- [ ] AC3: DailyRefresh 按概率池随机选取天气 + 持续 1-3 天
- [ ] AC4: ForceWeather 覆盖普通刷新直到结束
- [ ] AC5: 持续天数倒计时归零后触发下次刷新
- [ ] AC6: 不合理天气被区域正确过滤 (如江南无风沙)

## Test Evidence Path

`tests/Foundation/SceneManagement/WeatherSystemTests.cs`
