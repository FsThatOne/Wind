# Story 003: 自适应战斗音乐（6 段水平分层）

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 4-5h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-26

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-003`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System
**ADR Decision Summary**: 每段为独立 OGG 文件，切换在小节线（bar boundary）对齐；通过 BPM 配置计算 bar_length_ms。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: AudioStreamPlayer.GetPlaybackPosition() 可获取当前播放位置（秒），用于计算距下一小节的剩余时间。

**Control Manifest Rules (Presentation layer)**:
- Required: 战斗段切换必须等待小节线对齐（`bar_length_ms`） — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC2: GIVEN 玩家进入战斗, WHEN battle_state 从 Prep → Clash → Advantage 依次变化, THEN 音乐段落在小节线处无缝切换，无明显卡顿或重复
- [ ] AC3: GIVEN 一击决胜触发, WHEN finisher_triggered = true, THEN Finisher 段落在 bar_boundary_tolerance_ms(100ms) 内开始播放，同时播放 P1 优先级音效
- [ ] CombatSegment 判定逻辑实现（F3 公式 6 条按优先级从高到低判定）
- [ ] 段落切换时使用短 crossfade（fadeOut=500ms, fadeIn=300ms）
- [ ] Boss 战使用独立 BGM 资源但遵循相同 6 段结构
- [ ] 战斗结束后 fade_out_ms=2000 淡出战斗 BGM，restore 场景 BGM

---

## Implementation Notes

*Derived from ADR-0009:*

1. 创建 `CombatMusicController` 类
2. `CombatSegment` enum: Prep/Clash/Advantage/Disadvantage/Desperation/Finisher
3. 每首战斗曲配置 `CombatMusicConfig`：包含 6 段 track path + BPM + bar_length_ms
4. `EvaluateCombatState(hpRatio, staminaRatio, advantageStreak, finisherTriggered)` 返回 CombatSegment
5. `GetMsUntilNextBar()`: `bar_length_ms - (playback_pos_ms % bar_length_ms)`
6. 若 remainMs <= bar_boundary_tolerance_ms(100ms) 则立即切换，否则等待
7. 订阅 `BattleEventBus` 的 `DamageDealtEvent`/`BattleEndEvent` 等更新战斗状态
8. Finisher 触发时跳过 bar-boundary 等待直接切换（优先级最高）

---

## Out of Scope

- Story 002: BGM crossfade 基础设施（本 story 复用）
- Story 005: Finisher 触发的 P1 SFX 播放（本 story 只切段落）

---

## QA Test Cases

- **AC2**: 段落切换小节线对齐
  - Given: 战斗中 Prep 段正在播放，BPM=120，bar_length_ms=2000
  - When: advantage_streak 达到 2（触发 Advantage 段）
  - Then: 等待当前小节结束后 crossfade 到 Advantage 段
  - Edge cases: 触发时刚好在 bar boundary ±100ms 内

- **AC3**: Finisher 立即切换
  - Given: 战斗中任意段播放
  - When: finisher_triggered = true
  - Then: 在 bar_boundary_tolerance_ms(100ms) 内开始播放 Finisher 段
  - Edge cases: 同时满足 Desperation 和 Finisher 条件（Finisher 优先）

- **F3**: 判定逻辑优先级
  - Given: hp_ratio=0.10, stamina_ratio=0.15, finisher_triggered=false
  - When: EvaluateCombatState()
  - Then: 返回 Desperation（hp<0.15 AND stamina<0.20）
  - Edge cases: hp=0.30 exactly（边界值 → Disadvantage）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/combat_music_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (BGM crossfade 基础设施)
- Unlocks: None

---

## Completion Notes
**Completed**: 2026-06-26
**Criteria**: 6/6 passing
**Deviations**: OUT OF SCOPE (合理) — BgmManager.cs + BgmCrossfadeEngine.cs 新增 CrossfadeBgm/StartCrossfadeOnly (code review bug fix)
**Test Evidence**: Logic: tests/unit/audio/combat_music_test.cs — 31 facts, all passing
**Code Review**: Complete — 3 issues fixed (GetActivePlayer node names, PlayBgm stack clear, F3 counterStreak)
**Effort**: estimate 4-5h / actual ~3.5h (variance -22%)
