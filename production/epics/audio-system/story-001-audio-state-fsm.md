# Story 001: 音频状态 FSM + AudioBus 初始化

> **Epic**: 音乐 / 音效
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: 3-4h
> **Manifest Version**: 2026-06-10
> **Last Updated**: 2026-06-26

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: `TR-audio-001`

**ADR Governing Implementation**: ADR-0009: Dynamic Music System (primary), ADR-0008: Finite State Machine (secondary)
**ADR Decision Summary**: 使用自研 C# 音频状态机 + Godot AudioServer 总线；FSM 复用 ADR-0008 泛型 StateMachine。

**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: Godot Audio API 无 post-cutoff 破坏性变更。AudioServer bus layout 通过项目 default_bus_layout.tres 配置。

**Control Manifest Rules (Presentation layer)**:
- Required: 必须使用自研 C# 音频状态机 + Godot AudioServer 总线 — source: ADR-0009
- Required: 必须复用 ADR-0008 泛型 FSM (6 状态) — source: ADR-0009
- Required: 音频总线层级 Master → BGM/Ambient/SFX → 子总线 — source: ADR-0009

---

## Acceptance Criteria

*From GDD `design/gdd/audio-system.md`:*

- [ ] AC5: GIVEN Master 音量 = 0, WHEN 场景切换/战斗/演出正常进行, THEN 音频状态机正常运转（状态切换、crossfade 计时），恢复音量后从当前正确状态继续播放
- [ ] AC6: GIVEN 对话开始, WHEN dialogue_state("started") 触发, THEN BGM 在 500ms 内降至 60% 音量、Ambient 降至 40% 音量。对话结束后各轨恢复
- [ ] AudioState 枚举 6 种状态（Exploration/Combat/Cutscene/Dialogue/Menu/Silence）正确注册
- [ ] AudioServer 总线架构 Master → BGM(BGM_Main + BGM_Crossfade) → Ambient(Terrain + Weather + TimeOfDay) → SFX(Pool×8) 初始化成功
- [ ] 状态转换规则完整配置（见 GDD States and Transitions 表）
- [ ] state_attenuation 矩阵正确应用（见 GDD F2 公式）

---

## Implementation Notes

*Derived from ADR-0009:*

1. 创建 `AudioState` enum 和 `AudioTrigger` 字符串常量集
2. 实例化 `StateMachine<AudioState>(AudioState.Exploration)`
3. 逐一注册转换规则（GDD 表中的 9 条 from→to）
4. 创建 `AudioDirector` Godot Autoload Node 作为系统入口
5. `_Ready()` 中配置 AudioServer bus（或使用 .tres 配置文件）
6. `OnStateEnter` 回调中应用 state_attenuation（F2 公式）
7. `OnStateExit` 回调中恢复前态音量
8. Foundation 层创建 `AudioConfig` 纯数据类存储所有 tuning knobs

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: BGM 实际播放和 crossfade 逻辑
- Story 004: Ambient 三层具体播放
- Story 005: SFX 池管理

---

## QA Test Cases

- **AC5**: Master=0 状态机运转
  - Given: AudioDirector 初始化完成，Master bus volume = -80dB
  - When: 触发 EnterCombat → ExitCombat → StartDialogue → EndDialogue 序列
  - Then: FSM.CurrentState 依次为 Combat → Exploration → Dialogue → Exploration
  - Edge cases: Master=0 时中途恢复音量，验证当前状态的 attenuation 立即生效

- **AC6**: 对话衰减
  - Given: AudioState = Exploration，BGM/Ambient 各 1.0
  - When: TryTransition("StartDialogue")
  - Then: BGM effective = base × 0.6，Ambient effective = base × 0.4，500ms 内完成渐变
  - Edge cases: 对话中再开菜单（嵌套衰减规则）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/audio/audio_state_fsm_test.cs` — must exist and pass

**Status**: [x] `tests/unit/audio/audio_state_fsm_test.cs` — 30 tests passing

---

## Dependencies

- Depends on: None（ADR-0008 StateMachine 基类已实现）
- Unlocks: Story 002, 003, 004, 005, 006, 007, 008

---

## Completion Notes

**Completed**: 2026-06-26
**Criteria**: 6/6 passing
**Deviations**: TR-audio-001 未注册（ADVISORY — 等待 /architecture-review 补入）
**Test Evidence**: Logic — `tests/unit/audio/audio_state_fsm_test.cs` (30 tests)
**Code Review**: Complete (APPROVED — 含嵌套状态 _returnStack bug fix)
**Effort**: estimate 3-4h / actual ~1.5h (variance -50%)
