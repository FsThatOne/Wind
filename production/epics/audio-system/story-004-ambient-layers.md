# Story 004: 环境音三层系统

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 2-3h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-27

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-004`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: Ambient 轨 3 层（Terrain/Weather/TimeOfDay）独立淡入淡出，各层使用独立 AudioStreamPlayer 循环播放。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: AudioStreamPlayer loop 属性由 AudioStream resource 控制（OGG 勾选 Loop）。

**Control Manifest Rules (Presentation layer)**:
- Required: 音频总线层级 Master → BGM/Ambient/SFX → 子总线 — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC8: GIVEN 江南场景 + 小雨天气 + 午时, WHEN 三层 Ambient 同时激活, THEN 可同时听到河流水声（地形基底）+ 轻柔雨声（天气叠加）+ 蝉鸣（时辰叠加），三层独立可辨
- [ ] 各层独立淡入淡出，默认 ambient_fade_ms = 2000
- [ ] 场景切换时地形基底必换，天气/时辰层若无变化则保持
- [ ] 某层音频资源 ID 为空/null 时该层静默，其他层正常（Edge Case E8）
- [ ] 战斗状态下 Ambient 衰减至 20%（state_attenuation from Story 001）

---

## Implementation Notes

*Derived from ADR-0009:*

1. 创建 `AmbientManager` 类，持有 3 个 AudioStreamPlayer（Terrain / Weather / TimeOfDay）
2. 各 player 连接到 Ambient Bus 下对应子总线
3. `SetLayer(layer, trackId, fadeMs=2000)`: 若 trackId 不同则 crossfade 切换；为 null 则淡出
4. `OnSceneAudioConfig(config)`: 设置地形层；检查天气/时辰是否变化，变化才切换
5. `OnTimePeriodChanged(period)`: 切换时辰层
6. state_attenuation 由 AudioDirector FSM 回调统一管理（Story 001），AmbientManager 只管播放

---

## Out of Scope

- Story 001: state_attenuation 衰减逻辑
- 天气系统自身逻辑（本 story 只处理接收 weather_ambient ID 并播放）

---

## QA Test Cases

- **AC8**: 三层同时可辨
  - Given: 场景加载完成
  - When: SetLayer(Terrain, "river")、SetLayer(Weather, "light_rain")、SetLayer(TimeOfDay, "cicada_noon") 依次调用
  - Then: 3 个 AudioStreamPlayer 各自独立播放，bus 混音输出三者叠加
  - Edge cases: 其中一层 trackId=null（该层静默但其他两层正常）

- **E8**: 层缺失
  - Given: 场景配置中 weather_ambient = null
  - When: OnSceneAudioConfig 触发
  - Then: Weather player 淡出至静默，Terrain 和 TimeOfDay 正常
  - Edge cases: 所有三层都为 null（全静默但系统不崩溃）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/ambient_layers_test.cs` — must exist and pass

**Status**: [x] Exists and passes (21 tests)

---

## Dependencies

- Depends on: Story 001 (AudioBus 初始化)
- Unlocks: None

---

## Completion Notes
**Completed**: 2026-06-27
**Criteria**: 4/5 passing (1 deferred — combat attenuation handled by Story 001)
**Deviations**: None
**Test Evidence**: Logic: `tests/unit/audio/ambient_layers_test.cs` — 21 tests passing
**Code Review**: Complete — /code-review run twice, 3 issues found and fixed
**Effort**: estimate 2.50 h / actual 2.75 h (variance +10%)
