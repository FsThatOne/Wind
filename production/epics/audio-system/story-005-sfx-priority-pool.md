# Story 005: SFX 优先级仲裁 + 并发池

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-27

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-005`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: SFX Pool 8 路 AudioStreamPlayer；5 级优先级仲裁；P0/P1 不可淘汰可临时突破上限；同 SFX 50ms cooldown 防叠音。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 使用 AudioStreamPlayer pool pattern，每路设置不同的 volume/pan。

**Control Manifest Rules (Presentation layer)**:
- Required: SFX 并发 8 路；同 SFX 50ms cooldown；同源最多 2 路 — source: ADR-0009
- Required: SFX 优先级 P0/P1 不可淘汰，P2-P4 8 路满时按优先级淘汰 — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC7: GIVEN 单帧内 12 个 SFX 请求同时到达, WHEN 其中 2 个为 P0、3 个为 P1, THEN P0 和 P1 全部播放（5 个），剩余从 P2-P4 按优先级选取至 sfx_max_concurrent 上限
- [ ] 8 路满时淘汰正在播放的最低优先级 SFX
- [ ] P0/P1 SFX 临时突破 8 路上限（Edge Case E6）
- [ ] 同一 SFX 在 sfx_cooldown_ms(50ms) 内不重复触发
- [ ] 同一来源 SFX 最多同时 2 路

---

## Implementation Notes

*Derived from ADR-0009:*

1. 创建 `SfxPool` 类，预分配 8 个 AudioStreamPlayer（可扩展至 12 应对 P0/P1 突破）
2. 每路记录：sfxId、priority、startTime、sourceId
3. `PlaySfx(sfxId, priority, sourceId)`:
   - cooldown 检查：`_lastPlayTime[sfxId]` + 50ms
   - 同源检查：当前活跃中 sourceId 匹配数 < 2
   - 空闲路分配 or 按优先级淘汰最低的
   - P0/P1 无空闲路时创建临时 player
4. `_Process` 中回收已播放完毕的路（`!player.Playing`）
5. 暴露 `IAudioDirector.PlaySfx(sfxId, priority, sourceId)` 供上游系统调用

---

## Out of Scope

- 具体 SFX 资源制作/加载（本 story 只管仲裁和播放）
- 战斗系统何时调用 PlaySfx（由战斗系统集成 story 负责）

---

## QA Test Cases

- **AC7**: SFX 洪水
  - Given: SfxPool 所有 8 路空闲
  - When: 单帧内收到 12 请求：2×P0 + 3×P1 + 4×P2 + 3×P4
  - Then: P0(2) + P1(3) 全部播放（5 个）；P2 取前 3 个填满 8 路；剩余 P2(1) + P4(3) 被丢弃
  - Edge cases: P0/P1 超过 8 路的情况（应额外分配临时 player）

- **Cooldown**: 同 SFX 防叠音
  - Given: "hit_gang" 刚播放
  - When: 30ms 后再次请求 "hit_gang"
  - Then: 被拒绝（50ms cooldown 未过）
  - Edge cases: 不同 sfxId 不受彼此 cooldown 影响

- **同源限制**:
  - Given: sourceId="player_protagonist" 已有 2 路 SFX 播放
  - When: 同源第 3 个请求到达
  - Then: 被拒绝
  - Edge cases: 第 1 路刚结束（回收后应允许新请求）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/sfx_pool_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (AudioBus 初始化)
- Unlocks: Story 006（演出 SFX 接管）

---

## Completion Notes

**Completed**: 2026-06-27
**Criteria**: 5/5 passing
**Deviations**: None
**Test Evidence**: Logic — `tests/unit/audio/sfx_pool_test.cs` (14 tests, all green)
**Code Review**: Complete — /code-review 0 issues
**Effort**: estimate 3.00h / actual 2.50h (variance -17%)
