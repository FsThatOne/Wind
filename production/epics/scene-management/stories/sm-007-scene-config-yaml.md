# Story: sm-007 — Scene Config YAML

> **Epic**: scene-management
> **Type**: Integration
> **Priority**: P2
> **Depends On**: sm-001, sm-003, DataRegistry (cd-005)
> **GDD Source**: design/gdd/map-scene-management.md §Tuning Knobs
> **Status**: Done

## Goal

场景管理配置 YAML：场景连接图、驿站列表、天气权重表、色调调色板。

## Acceptance Criteria

- [ ] AC1: SceneConfig YAML 包含 transition_fade_duration, transition_ink_duration, preload_retry_count 等参数
- [ ] AC2: WeatherWeights YAML 按季节×天气矩阵加载
- [ ] AC3: RegionFilter YAML 按区域×天气掩码加载
- [ ] AC4: DataRegistry 正确注册所有配置表
- [ ] AC5: 格式错误 → DataLoadException
- [ ] AC6: 缺少可选字段使用 GDD 默认值

## Test Evidence Path

`tests/Foundation/SceneManagement/SceneConfigTests.cs`
