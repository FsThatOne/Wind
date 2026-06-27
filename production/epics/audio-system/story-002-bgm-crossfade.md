# Story 002: BGM 管理 + 等功率 Crossfade

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 3-4h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-26

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-002`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: BGM Override 栈深度 3 + 等功率 crossfade(cos/sin) + 双 AudioStreamPlayer 交替播放。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 使用 AudioStreamPlayer + VolumeDb 属性逐帧更新实现 crossfade。OGG Vorbis 流式加载。

**Control Manifest Rules (Presentation layer)**:
- Required: BGM Override 栈深度 3，溢出替换栈顶 — source: ADR-0009
- Required: Crossfade 曲线必须等功率 fade_out=cos(t·π/2)、fade_in=sin(t·π/2) — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC1: GIVEN 玩家进入新场景, WHEN 场景 BGM 与前一场景不同, THEN 旧 BGM 在 1500ms 内淡出、新 BGM 在 800ms 内淡入，过渡期间无明显音频空白或爆音
- [ ] BGM Override 栈 push/pop 正确管理（Exploration → Combat → Cutscene 三层）
- [ ] 栈溢出时（已达 3 层）替换栈顶而非再压入（Edge Case E1）
- [ ] 同曲续播：bgm_id 匹配当前播放曲目时跳过 crossfade（Edge Case E7）
- [ ] 极短场景过渡：crossfade 未完成时新切换立即中断重新开始（Edge Case E2）
- [ ] SILENCE bgm_id：淡出当前 BGM 但不播放新曲

---

## Implementation Notes

*Derived from ADR-0009:*

1. 创建 `BgmManager` 类，持有 2 个 AudioStreamPlayer（BGM_Main / BGM_Crossfade）
2. `_overrideStack` Stack<BgmEntry> 最大深度 3
3. `CrossfadeTo(trackId, fadeOutMs, fadeInMs)` 方法在 `_Process` 中逐帧更新 VolumeDb
4. 使用 `Mathf.Cos(t / duration * Mathf.Pi / 2.0f)` 和 `Mathf.Sin(...)` 计算线性值后转 dB
5. `LinearToDb`: `volume <= 0 ? -80f : 20f * Mathf.Log10(volume)`
6. 同曲检测：比较 `_currentTrackId == newTrackId` 跳过
7. 极短场景处理：进入 CrossfadeTo 时若上一次 crossfade 未完成，立即 stop 旧 player

---

## Out of Scope

- Story 003: 战斗自适应音乐的段落切换逻辑
- Story 006: 演出系统 push/pop 触发时机

---

## QA Test Cases

- **AC1**: 场景 BGM crossfade
  - Given: 场景 A 正在播放 BGM_A
  - When: 切换到场景 B（BGM_B != BGM_A），调用 PlayBgm("BGM_B")
  - Then: BGM_A 在 1500ms 淡出（cos 曲线），BGM_B 在 800ms 淡入（sin 曲线），-3dB 中点在各自时长的 50%
  - Edge cases: fadeOutMs=200（最短）、fadeOutMs=3000（最长）

- **E1**: Override 栈溢出
  - Given: 栈已有 3 层 [Exploration, Combat, Cutscene]
  - When: 再次 PushBgm("new_cutscene_bgm")
  - Then: 栈顶被替换（栈仍为 3 层），crossfade 到新曲

- **E7**: 同曲续播
  - Given: 当前播放 "jiangnan_theme"
  - When: 新场景 audio_config 也是 "jiangnan_theme"
  - Then: 不触发 crossfade，播放位置不变

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/bgm_crossfade_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (AudioState FSM + Bus 初始化)
- Unlocks: Story 003, 006, 007

---

## Completion Notes
**Completed**: 2026-06-26
**Criteria**: 6/6 passing
**Deviations**: None
**Test Evidence**: Logic: `tests/unit/audio/bgm_crossfade_test.cs` (29 tests, all pass)
**Code Review**: Complete — 2 issues found and fixed (IsSameTrack pending-track detection + interrupt StopAndSwap)
**Effort**: estimate 3.5h / actual ~3h (variance -14%)
