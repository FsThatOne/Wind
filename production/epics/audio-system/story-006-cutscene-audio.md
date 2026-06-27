# Story 006: 演出音频接管与跳过恢复

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 3h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-27

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-006`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System (primary), ADR-0013: Cutscene System (secondary)
**ADR Decision Summary**: 演出通过 PLAY_BGM 压栈 / PLAY_SFX(P0) 播放；跳过时 200ms 淡出全部演出音频并回退 override 栈。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 演出 FSM 在 Skipping → CompletingEffects 路径中调用音频清理。

**Control Manifest Rules (Presentation layer)**:
- Required: BGM Override 栈深度 3，溢出替换栈顶 — source: ADR-0009
- Required (Core): 演出跳过必须经 Skipping → CompletingEffects 路径 — source: ADR-0013

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [x] AC4: GIVEN 演出正在播放, WHEN 玩家长按跳过, THEN 所有演出音频在 200ms 内淡出，BGM 栈正确回退到演出前状态，无 2 秒以上静默间隙
- [x] PLAY_BGM 将演出 BGM 压入 override 栈，演出结束自动 restore
- [x] PLAY_SFX 使用 P0 优先级，不受 cooldown 限制
- [x] 演出被跳过时 fade_out_ms = 200 强制中断所有演出音频
- [x] 战斗中触发演出时，战斗 BGM 栈保留，演出结束 restore 到战斗段落当前位置（Edge Case E3）
- [x] PARALLEL 步骤中多个 SFX 不受 sfx_cooldown 限制

---

## Implementation Notes

*Derived from ADR-0009 + ADR-0013:*

1. `AudioDirector` 提供 `CutscenePlayBgm(trackId, fadeInMs)` 和 `CutscenePlaySfx(sfxId, volume)` 接口
2. `CutscenePlayBgm` 调用 `BgmManager.PushBgm()` + FSM 转 Cutscene 状态
3. `CutscenePlaySfx` 调用 `SfxPool.PlaySfx(sfxId, Priority.P0, sourceId: "cutscene", bypassCooldown: true)`
4. 演出跳过处理：`OnCutsceneSkipped()`:
   - 停止所有 P0 源为 "cutscene" 的 SFX（200ms 快速淡出）
   - `BgmManager.PopBgm()` 回退栈
   - FSM 回退到前状态
5. 战斗中演出（E3）：pop 后 CombatMusicController 恢复到当前段的播放位置

---

## Out of Scope

- 演出系统本身的 FSM 实现（ADR-0013，另一个 epic）
- 演出脚本编写

---

## QA Test Cases

- **AC4**: 跳过恢复
  - Given: 探索中触发演出，演出 BGM "cutscene_01" 正在播放
  - When: 玩家长按跳过，CutsceneService 发出 skip 信号
  - Then: 演出 BGM 在 200ms 淡出，override 栈 pop，探索 BGM 恢复播放（位置延续），无 >200ms 静默间隙
  - Edge cases: 跳过时 crossfade 正在进行（应立即中断 crossfade）

- **E3**: 战斗中演出
  - Given: 战斗 BGM（Advantage 段）正在播放，触发演出
  - When: 演出结束/跳过
  - Then: 恢复到战斗 BGM Advantage 段的当前播放位置（非从头）
  - Edge cases: 演出期间战斗局势变化（栈保留的是段 ID，恢复后重新评估段）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/audio/cutscene_audio_test.cs` OR runtime evidence document

**Status**: [x] 12 tests passing (2026-06-27)

---

## Dependencies

- Depends on: Story 002 (BGM Override 栈), Story 005 (SFX Pool P0 bypass)
- Unlocks: None
