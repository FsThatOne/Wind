# Story 007: 女主 Motif 叠加

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 2h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-27

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-007`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: 女主 Motif 使用独立 overlay player + BGM duck 至 30%，不入 override 栈。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 额外 AudioStreamPlayer 挂在 BGM Bus 下，独立于 BGM_Main/BGM_Crossfade。

**Control Manifest Rules (Presentation layer)**:
- Required: Motif 处理：女主 Motif 用独立 overlay player + BGM duck 至 30%（不入栈） — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [x] AC9: GIVEN 女主偶遇触发, WHEN romance_motif(character_id, "enter") 到达, THEN 场景 BGM 渐弱至 30%、女主 motif 淡入播放。偶遇结束后 BGM 恢复 100%、motif 淡出
- [x] Motif 不进入 override 栈（是叠加层，非替代层）
- [x] Motif 与场景 BGM 冲突时 duck（Edge Case E10）
- [x] 多个偶遇不会同时触发多个 motif（同时只有 1 个 motif 活跃）

---

## Implementation Notes

*Derived from ADR-0009:*

1. `BgmManager` 增加 `_motifPlayer` (AudioStreamPlayer) 和 `_motifActive` 状态
2. `PlayMotif(characterId)`: 加载对应角色 motif track，淡入 motif player，同时将 BGM_Main volume duck 至 30%（使用 motif_bgm_duck_ratio 配置）
3. `StopMotif()`: 淡出 motif player，恢复 BGM_Main 至 100%
4. duck 过渡使用线性插值（非等功率，因为只是音量调节不是 crossfade）
5. 若 motif 正在播放时收到新 motif 请求，先淡出旧 motif 再淡入新 motif

---

## Out of Scope

- 感情系统何时触发 romance_motif（由感情系统 epic 负责）
- Motif 音乐资产制作

---

## QA Test Cases

- **AC9**: Motif 叠加与 duck
  - Given: 探索中 BGM "jiangnan_theme" 播放中，volume=1.0
  - When: romance_motif("heroine_a", "enter") 触发
  - Then: BGM volume 渐变至 0.3，motif "heroine_a_motif" 淡入播放
  - Edge cases: 触发时 BGM 已被 state_attenuation 衰减至 0.6（duck 应基于当前有效值：0.6×0.3=0.18）

- **E10**: Motif 结束恢复
  - Given: motif 正在播放，BGM ducked at 30%
  - When: romance_motif("heroine_a", "exit") 触发
  - Then: motif 淡出，BGM 恢复到 duck 前的值（不一定是 1.0，可能是 state_attenuation 后的值）
  - Edge cases: motif 播放中切换状态（如进入 Dialogue），duck 值应叠加 dialogue attenuation

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/motif_overlay_test.cs` — must exist and pass

**Status**: [x] Created — `dotnet test tests/Foundation/Foundation.Tests.csproj` passing

---

## Dependencies

- Depends on: Story 002 (BgmManager)
- Unlocks: None

---

## Completion Notes

**Completed**: 2026-06-27
**Criteria**: 4/4 passing
**Deviations**: None blocking. Advisory: no Godot in-editor audio audition was captured; Logic evidence and C# build passed.
**Test Evidence**: Logic — `tests/unit/audio/motif_overlay_test.cs` (11 tests, all green); full Foundation suite 1550 tests green.
**Code Review**: Complete — `/code-review` found 2 blocking issues, both fixed (Godot resource path; missing motif load fallback).
**Effort**: estimate 2.00h / actual 2.75h (variance +38%)
