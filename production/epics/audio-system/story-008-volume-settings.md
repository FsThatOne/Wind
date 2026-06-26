# Story 008: 设置音量响应 + 持久化

> **Epic**: 音乐 / 音效
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 2h
> **Manifest Version**: 2026-06-10
> **Last Updated**: —

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-008`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: 4 档音量滑块（Master/BGM/Ambient/SFX）通过 AudioServer bus volume 实时响应；设置持久化依赖存档系统。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: `AudioServer.SetBusVolumeDb(busIdx, db)` 实时生效。bus index 通过 `AudioServer.GetBusIndex(busName)` 获取。

**Control Manifest Rules (Presentation layer)**:
- Required: 音频总线层级 Master → BGM/Ambient/SFX → 子总线 — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC10: GIVEN 设置中调节 BGM 滑块, WHEN 滑块从 80 调至 30, THEN BGM 音量实时变化（无需重新加载），退出设置后保持新音量
- [ ] 4 档滑块（Master/BGM/Ambient/SFX）各自独立调节
- [ ] 音量值 0-100 映射到 -80dB ~ 0dB（对数曲线）
- [ ] 设置持久化到用户存档，重启游戏后恢复
- [ ] Master=0 时等效于全局静音但状态机正常运转（与 AC5 协同）

---

## Implementation Notes

*Derived from ADR-0009:*

1. `AudioDirector` 暴露 `SetVolume(track, value01)` 公开方法
2. `value01` (0.0-1.0) → dB: `value <= 0 ? -80f : 20f * Log10(value)`
3. 调用 `AudioServer.SetBusVolumeDb(busIdx, db)` 实时更新
4. `effective_volume = user_volume × state_attenuation` — 两者相乘后写入 bus
5. 订阅设置系统的 `volume_changed(track, value)` 事件
6. 初始化时从存档/设置系统读取音量配置并应用
7. 设置系统不存在时使用默认值 (Master=1.0, BGM=0.8, Ambient=0.7, SFX=1.0)

---

## Out of Scope

- 设置 UI 界面实现（属于设置/选项系统 epic）
- 存档系统序列化（只需实现 ISaveable 接口暴露 4 个 float）

---

## QA Test Cases

- **AC10**: 实时音量调节
  - Given: BGM bus volume = 0dB (user_volume=1.0)
  - When: SetVolume(BGM, 0.3) 调用
  - Then: AudioServer BGM bus volume 立即变为 ~-10.5dB
  - Edge cases: value=0（-80dB，等效静音）、value=1.0（0dB 满音量）

- **持久化**:
  - Given: 用户设置 BGM=0.5, SFX=0.8
  - When: 游戏重启，AudioDirector._Ready() 初始化
  - Then: BGM bus = -6dB, SFX bus = -1.9dB
  - Edge cases: 存档损坏时回退到默认值

- **与 state_attenuation 叠加**:
  - Given: user_volume(BGM)=0.8, 当前状态=Dialogue(attenuation=0.6)
  - When: 计算 effective
  - Then: effective = 0.8 × 0.6 = 0.48 → -6.4dB
  - Edge cases: 用户调节时也处于 Dialogue 状态（实时更新 effective）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/audio/volume_settings_test.cs` OR runtime evidence

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (AudioBus 初始化 + state_attenuation)
- Unlocks: None
